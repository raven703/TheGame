using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Written and run against the Unity implementation before stage-2 replacement.
public class StatsCharacterizationTests
{
    private static Type T(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    private static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method).Invoke(o,args);
    private static float F(object o,string name)=>(float)(o.GetType().GetField(name)!=null?o.GetType().GetField(name).GetValue(o):o.GetType().GetProperty(name).GetValue(o));
    private static float P(object o,string name)=>(float)o.GetType().GetProperty(name).GetValue(o);
    private static void Set(object o,string name,object value) { var field=o.GetType().GetField(name); if(field!=null)field.SetValue(o,value);else o.GetType().GetProperty(name).SetValue(o,value); }
    private GameObject go;
    private object oldSession;
    [SetUp] public void Setup(){oldSession=T("CombatSession").GetProperty("Instance").GetValue(null);T("CombatSession").GetProperty("Instance").SetValue(null,null);go=new GameObject("stats characterization");}
    [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(go);T("CombatSession").GetProperty("Instance").SetValue(null,oldSession);}
    [Test] public void ShipTotalsAreAdditiveClampedAndIgnoreInventory()
    {
        var def=ScriptableObject.CreateInstance(T("ShipDefinition"));
        try
        {
            object load=JsonUtility.FromJson("{\"weaponSlots\":[null,{\"id\":\"saved\",\"shieldBonus\":30,\"massBonus\":-9,\"speedBonus\":3,\"accelBonus\":6,\"lockTimeBonus\":-2,\"heatResistanceBonus\":0.7}],\"engineSlots\":[{\"id\":\"e2\",\"heatResistanceBonus\":0.5}],\"inventory\":[{\"id\":\"free\",\"speedBonus\":999}]}",T("ShipLoadout"));
            var calc=T("ShipStatsCalculator").GetMethod("Calculate");
            for(int i=0;i<3;i++)
            {
                var s=calc.Invoke(null,new[]{def,load});Assert.AreEqual(120,F(s,"hull"));Assert.AreEqual(90,F(s,"shield"));
                Assert.AreEqual(.5f,F(s,"mass"),.0001f);Assert.AreEqual(9,F(s,"speed"));Assert.AreEqual(20,F(s,"thrust"));
                Assert.AreEqual(.05f,F(s,"lockTime"),.0001f);Assert.AreEqual(.75f,F(s,"resistance"),.0001f);
            }
        }finally{UnityEngine.Object.DestroyImmediate(def);}
    }
    [TestCase("w1",0,7f,9f,.4f)] [TestCase("w2",1,16f,22f,1f)] [TestCase("w3",2,22f,30f,1.6f)]
    public void WeaponLegacyKindsRetainCostsAndCooldowns(string id,int kind,float energy,float heat,float cooldown)
    {
        var item=JsonUtility.FromJson("{\"id\":\""+id+"\",\"damageBonus\":17,\"cooldownBonus\":-0.2}",T("FittingItem"));
        var s=T("WeaponStatsCalculator").GetMethod("Calculate").Invoke(null,new[]{item,(object).6f,(object).5f});
        Assert.AreEqual(kind,Convert.ToInt32(s.GetType().GetField("kind").GetValue(s)));Assert.AreEqual(17,F(s,"damage"));
        Assert.AreEqual(energy,F(s,"energy"));Assert.AreEqual(heat,F(s,"heat"));Assert.AreEqual(cooldown,F(s,"cooldown"),.0001f);
    }
    [Test] public void ReactorConfigurationPreservesDeficitAndHeat_ExactBoundaries()
    {
        var reactor=go.AddComponent(T("ShipReactor"));
        Call(reactor,"TrySpend",40f,100f);Assert.True((bool)reactor.GetType().GetProperty("Overheated").GetValue(reactor));
        Call(reactor,"Configure",150f,12f,15f,.25f);Assert.AreEqual(110,P(reactor,"Energy"));Assert.AreEqual(100,P(reactor,"Heat"));
        Call(reactor,"Tick",0f);Assert.AreEqual(110,P(reactor,"Energy"));Assert.AreEqual(100,P(reactor,"Heat"));
        Call(reactor,"Tick",4f);Assert.AreEqual(40,P(reactor,"Heat"));
        Call(reactor,"Tick",1f/3f);Assert.AreEqual(35,P(reactor,"Heat"),.0001f);Assert.False((bool)reactor.GetType().GetProperty("Overheated").GetValue(reactor));
        Call(reactor,"Configure",50f,0f,0f,0f);Assert.AreEqual(50,P(reactor,"Energy"));
        Assert.True((bool)Call(reactor,"TrySpend",50f,0f));Assert.AreEqual(0,P(reactor,"Energy"));
        Assert.False((bool)Call(reactor,"TrySpend",1f,90f));Assert.AreEqual(35,P(reactor,"Heat"),.0001f);
    }
    [Test] public void ApplyAddsMaximumDifferenceRatherThanPreservingFraction()
    {
        var session=new GameObject("preparation");var def=ScriptableObject.CreateInstance(T("ShipDefinition"));
        try
        {
            var component=session.AddComponent(T("CombatSession"));T("CombatSession").GetProperty("Instance").SetValue(null,component);
            var health=go.AddComponent(T("ShipHealth"));health.GetType().GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(health,null);
            var data=health.GetType().GetProperty("Data").GetValue(health);Set(data,"currentHullHP",70f);Set(data,"currentShieldHP",20f);
            Set(def,"maxHullHP",150f);Set(def,"baseShieldHP",80f);
            T("ShipStatsCalculator").GetMethod("Apply").Invoke(null,new object[]{health,def,null});
            Assert.AreEqual(120,F(data,"currentHullHP"));Assert.AreEqual(50,F(data,"currentShieldHP"));
            T("ShipStatsCalculator").GetMethod("Apply").Invoke(null,new object[]{health,def,null});
            Assert.AreEqual(120,F(data,"currentHullHP"));Assert.AreEqual(50,F(data,"currentShieldHP"));
        }finally{UnityEngine.Object.DestroyImmediate(session);UnityEngine.Object.DestroyImmediate(def);}
    }
    [Test] public void ConfigureBeforeAwakeDoesNotResetSpentResources()
    {
        go.SetActive(false);var reactor=go.AddComponent(T("ShipReactor"));
        Call(reactor,"Configure",150f,12f,15f,.25f);Assert.True((bool)Call(reactor,"TrySpend",40f,20f));
        reactor.GetType().GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(reactor,null);
        Assert.AreEqual(110,P(reactor,"Energy"));Assert.AreEqual(15,P(reactor,"Heat"));
    }
    [Test] public void WeaponEnumValuesMatchCoreAndFallbackKeepsHalfDamage()
    {
        var core=Assembly.Load("Game.Core").GetType("Game.Core.WeaponKind",true);
        foreach(string name in new[]{"Laser","Plasma","Railgun"})Assert.AreEqual(Convert.ToInt32(Enum.Parse(T("WeaponKind"),name)),Convert.ToInt32(Enum.Parse(core,name)));
        var s=T("WeaponStatsCalculator").GetMethod("Calculate").Invoke(null,new object[]{null,.6f,.5f});
        Assert.AreEqual(.5f,F(s,"damage"));Assert.AreEqual(.6f,F(s,"cooldown"),.0001f);
    }
}