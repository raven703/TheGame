using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ShipOrderTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object o, string name) => o.GetType().GetProperty(name).GetValue(o);
    private static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name).Invoke(o,args);
    private Component Add(GameObject go, string name)
    {
        var c=go.AddComponent(T(name));
        T(name).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(c,null);
        return c;
    }
    private GameObject Node(string name) {var go=new GameObject(name);objects.Add(go);return go;}
    private Component Ship(string name)
    {
        var go=Node(name);Add(go,"ShipHealth");Add(go,"ShipMovement");return Add(go,"ShipWeapon");
    }
    [TearDown] public void Cleanup(){foreach(var go in objects)if(go!=null)UnityEngine.Object.DestroyImmediate(go);objects.Clear();}
    [Test] public void MountCompatibility_AllSixCombinations()
    {
        for(int mount=0;mount<3;mount++)for(int type=0;type<2;type++)
        {
            bool actual=(bool)T("WeaponMountRules").GetMethod("Accepts").Invoke(null,new[]{Enum.ToObject(T("MountCompatibility"),mount),Enum.ToObject(T("WeaponMountType"),type)});
            Assert.AreEqual(mount==2 || mount==type,actual);
        }
    }
    [Test] public void MountCompatibility_AdapterPreservesSerializedValuesAndCoreBoundary()
    {
        var core = Assembly.Load("Game.Core");
        var rule = core.GetType("Game.Core.MountCompatibilityRules", true);
        var coreMount = core.GetType("Game.Core.MountCompatibility", true);
        var coreType = core.GetType("Game.Core.WeaponMountType", true);
        Assert.AreEqual("Game.Core", rule.Assembly.GetName().Name);
        foreach (var reference in core.GetReferencedAssemblies())
            Assert.False(reference.Name.StartsWith("Unity", StringComparison.Ordinal), reference.FullName);
        foreach (string name in Enum.GetNames(T("MountCompatibility")))
            Assert.AreEqual(Convert.ToInt32(Enum.Parse(T("MountCompatibility"), name)), Convert.ToInt32(Enum.Parse(coreMount, name)));
        foreach (string name in Enum.GetNames(T("WeaponMountType")))
            Assert.AreEqual(Convert.ToInt32(Enum.Parse(T("WeaponMountType"), name)), Convert.ToInt32(Enum.Parse(coreType, name)));
        foreach (int mount in new[] { 0, 1, 2, 99 }) foreach (int type in new[] { 0, 1, 99 })
        {
            bool actual = (bool)T("WeaponMountRules").GetMethod("Accepts").Invoke(null,
                new[] { Enum.ToObject(T("MountCompatibility"), mount), Enum.ToObject(T("WeaponMountType"), type) });
            bool expected = (bool)rule.GetMethod("Accepts").Invoke(null,
                new[] { Enum.ToObject(coreMount, mount), Enum.ToObject(coreType, type) });
            Assert.AreEqual(expected, actual);
        }
    }
    [Test] public void Migration_IsIdempotent_PreservesHolesAndItems_AndSwapChecksBothSides()
    {
        var definition=ScriptableObject.CreateInstance(T("ShipDefinition"));
        try
        {
            T("ShipDefinition").GetField("shipId").SetValue(definition,"migration_test");
            T("ShipDefinition").GetField("weaponSlots").SetValue(definition,4);
            var points=Array.CreateInstance(T("WeaponHardpoint"),4);
            for(int i=0;i<4;i++)
            {
                var point=Activator.CreateInstance(T("WeaponHardpoint"));
                point.GetType().GetField("id").SetValue(point,"stable"+i);
                point.GetType().GetField("slotIndex").SetValue(point,i);
                point.GetType().GetField("compatibility").SetValue(point,Enum.ToObject(T("MountCompatibility"),i==2?1:i==3?2:0));points.SetValue(point,i);
            }
            T("ShipDefinition").GetField("hardpoints").SetValue(definition,points);
            var load=JsonUtility.FromJson("{\"shipId\":\"migration_test\",\"weaponSlots\":[{\"id\":\"a\"},{},{\"id\":\"b\"},{\"id\":\"t\",\"mountType\":1}],\"inventory\":[]}",T("ShipLoadout"));
            var migrate=T("WeaponMountRules").GetMethod("Migrate");
            Assert.AreEqual(1,migrate.Invoke(null,new[]{definition,load}));
            var slots=(IList)T("ShipLoadout").GetField("weaponSlots").GetValue(load);
            var inv=(IList)T("ShipLoadout").GetField("inventory").GetValue(load);
            Assert.AreEqual(4,slots.Count);Assert.IsNull(slots[1]);Assert.IsNull(slots[2]);Assert.AreEqual(1,inv.Count);
            Assert.AreEqual(0,migrate.Invoke(null,new[]{definition,load}));Assert.AreEqual(1,inv.Count);
            Assert.False((bool)T("WeaponMountRules").GetMethod("CanSwap").Invoke(null,new[]{definition,(object)0,slots[0],3,slots[3]}));
            string json=JsonUtility.ToJson(load);var round=JsonUtility.FromJson(json,T("ShipLoadout"));
            var restored=(IList)T("ShipLoadout").GetField("weaponSlots").GetValue(round);
            // JsonUtility may instantiate null class entries: sanitizer must normalize, never compact.
            T("ShipLoadoutManager").GetMethod("Sanitize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{round});
            Assert.AreEqual(4,restored.Count);Assert.IsNull(restored[1]);Assert.IsNull(restored[2]);
            slots[2]=slots[0];
            Assert.False((bool)T("WeaponMountRules").GetMethod("Validate").Invoke(null,new[]{definition,load}));
            Assert.AreSame(slots[0],slots[2],"validation must not partially repair a preset");
        }
        finally{UnityEngine.Object.DestroyImmediate(definition);}
    }
    [Test] public void DirectionalForce_UsesSharedBudget_AndBoost()
    {
        var movement=Ship("thrust").GetComponent(T("ShipMovement"));
        float force=(float)Get(movement,"AvailableForce");
        Vector2 forward=(Vector2)Call(movement,"LimitThrust",Vector2.up*10000f);
        Vector2 reverse=(Vector2)Call(movement,"LimitThrust",Vector2.down*10000f);
        Vector2 side=(Vector2)Call(movement,"LimitThrust",Vector2.right*10000f);
        Assert.That(forward.magnitude,Is.EqualTo(force).Within(.001f));
        Assert.That(reverse.magnitude,Is.EqualTo(force*.5f).Within(.001f));
        Assert.That(side.magnitude,Is.EqualTo(force*.35f).Within(.001f));
        Vector2 diagonal=(Vector2)Call(movement,"LimitThrust",Vector2.one*10000f);
        Assert.That(Mathf.Pow(diagonal.y/force,2)+Mathf.Pow(diagonal.x/(force*.35f),2),Is.EqualTo(1f).Within(.001f));
        T("ShipMovement").GetProperty("SpeedMultiplier").SetValue(movement,2f);
        Assert.That((float)Call(movement,"ForceAlong",Vector2.down),Is.EqualTo(force).Within(.001f));
    }
    [Test] public void Mounts_FixedSector_AndTurretRate_AreIndependent()
    {
        var weapon=Ship("mounts");
        T("ShipWeapon").GetField("firingArcAngle").SetValue(weapon,360f);
        var item=Activator.CreateInstance(T("FittingItem"), new object[]{"test","test",Enum.ToObject(T("SlotGroup"),0),Color.white,"",0f,0f,0f,0f,0f,0f,0f,Enum.ToObject(T("WeaponVisualType"),0)});
        T("ShipWeapon").GetField("firingArcAngle").SetValue(weapon,60f);
        Assert.False((bool)Call(weapon,"CanAim",0,item,Vector2.right*3f,.1f));
        T("ShipWeapon").GetField("firingArcAngle").SetValue(weapon,360f);
        T("FittingItem").GetField("mountType").SetValue(item,Enum.Parse(T("WeaponMountType"),"Turret"));
        T("FittingItem").GetField("trackingSpeed").SetValue(item,90f);
        Assert.False((bool)Call(weapon,"CanAim",0,item,Vector2.right*3f,.1f));
        Assert.That(Mathf.Abs((float)Call(weapon,"MountAngle",0)),Is.EqualTo(9f).Within(.001f));
        Assert.That((float)Call(weapon,"MountAngle",1),Is.EqualTo(0f));
        Assert.True((bool)Call(weapon,"CanAim",0,item,Vector2.right*3f,1f));
    }
    [Test] public void Turret_WrapsFullCircle_ButNeverCrossesForbiddenSector()
    {
        var weapon=Ship("wrap");
        var item=JsonUtility.FromJson("{\"id\":\"turret\",\"mountType\":1,\"trackingSpeed\":90,\"aimTolerance\":3}",T("FittingItem"));
        T("ShipWeapon").GetField("firingArcAngle").SetValue(weapon,360f);
        Vector2 leftRear=new Vector2(-.05f,-3f), rightRear=new Vector2(.05f,-3f);
        Call(weapon,"CanAim",0,item,leftRear,3f);
        float before=(float)Call(weapon,"MountAngle",0);
        Call(weapon,"CanAim",0,item,rightRear,.1f);
        float after=(float)Call(weapon,"MountAngle",0);
        Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(before,after)),9.001f);
        T("ShipWeapon").GetField("firingArcAngle").SetValue(weapon,300f);
        Call(weapon,"CanAim",1,item,leftRear,3f);
        before=(float)Call(weapon,"MountAngle",1);
        Assert.False((bool)Call(weapon,"CanAim",1,item,rightRear,.1f));
        after=(float)Call(weapon,"MountAngle",1);
        Assert.That(after,Is.EqualTo(before-9f).Within(.001f));
    }
    [Test] public void LegacyWeaponSnapshot_RemainsFixed_AndLegacyPointsRemainReferenced()
    {
        var legacy=JsonUtility.FromJson("{\"id\":\"w1\",\"damageBonus\":15}",T("FittingItem"));
        Assert.AreEqual("Fixed",T("FittingItem").GetField("mountType").GetValue(legacy).ToString());
        var weapon=Ship("legacy");var point=Node("point");point.transform.SetParent(weapon.transform);point.transform.localPosition=Vector3.right;
        T("ShipWeapon").GetField("firePoints").SetValue(weapon,new[]{point.transform});
        Assert.False((bool)Call(weapon,"CanAim",0,legacy,new Vector2(-1,1),.1f));
        Assert.AreSame(point.transform,((Transform[])T("ShipWeapon").GetField("firePoints").GetValue(weapon))[0]);
        Assert.AreEqual(60f,Call(weapon,"MountSector",0));
    }
    [Test] public void Selection_IsIndependent_AndRepeatedPendingLockDoesNotReset()
    {
        var weapon=Ship("player");var enemy=Ship("enemy").GetComponent(T("ShipHealth"));
        var movement=weapon.GetComponent(T("ShipMovement"));var orders=Get(movement,"Orders");
        var input=Add(Node("input"),"PlayerInputHandler");T("PlayerInputHandler").GetField("playerMovement").SetValue(input,movement);T("PlayerInputHandler").GetField("playerWeapon").SetValue(input,weapon);
        Call(orders,"MoveTo",new Vector2(8,2),false);
        Call(input,"IssueClick",true,Vector2.zero,enemy,false,false);
        Assert.AreEqual("MoveToPoint",Get(orders,"Mode").ToString());
        T("ShipWeapon").GetField("lockProgress",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(weapon,.4f);
        Call(input,"IssueClick",true,Vector2.zero,enemy,false,false);
        Assert.AreEqual(.4f,Get(weapon,"LockProgress"));
        Call(input,"IssueClick",true,Vector2.one,null,false,false);
        Assert.AreSame(enemy,Get(weapon,"PendingTarget"));
        Assert.AreEqual(new Vector2(8,2),Get(orders,"Destination"));
    }
    [Test] public void Route_AppendOrder_ExplicitTactics_AndUIBlock()
    {
        var weapon=Ship("player");var enemy=Ship("enemy").GetComponent(T("ShipHealth"));
        var other=Ship("other").GetComponent(T("ShipHealth"));
        var movement=weapon.GetComponent(T("ShipMovement"));var orders=Get(movement,"Orders");
        var input=Add(Node("input"),"PlayerInputHandler");T("PlayerInputHandler").GetField("playerMovement").SetValue(input,movement);T("PlayerInputHandler").GetField("playerWeapon").SetValue(input,weapon);
        Call(weapon,"SetTarget",enemy);Call(input,"IssueClick",false,Vector2.zero,enemy,false,false);
        Assert.AreEqual("EngageTarget",Get(orders,"Mode").ToString());
        Call(weapon,"SetTarget",other);Assert.AreSame(enemy,Get(orders,"OrderTarget"));
        Call(input,"IssueClick",false,new Vector2(2,3),null,true,false);
        Call(input,"IssueClick",false,new Vector2(4,5),null,true,false);
        var points=(IList)T("ShipOrderController").GetProperty("Waypoints").GetValue(orders);
        Assert.AreEqual(2,points.Count);Assert.AreEqual(new Vector2(2,3),points[0]);Assert.AreEqual(new Vector2(4,5),points[1]);
        Call(input,"IssueClick",false,Vector2.zero,null,false,true);Assert.AreEqual(2,points.Count);
        Call(orders,"Stop");Assert.AreEqual(0,((IList)Get(orders,"Waypoints")).Count);Assert.IsTrue((bool)Get(orders,"IsBraking"));
        Assert.AreSame(other,Get(weapon,"PendingTarget"));
    }
    [Test] public void UnselectedEnemyRmb_IsPositionSnapshot_AndSpeedLimitDoesNotEditEquipment()
    {
        var weapon=Ship("player");var enemy=Ship("enemy").GetComponent(T("ShipHealth"));enemy.transform.position=new Vector3(7,4);
        var movement=weapon.GetComponent(T("ShipMovement"));var orders=Get(movement,"Orders");
        var input=Add(Node("input"),"PlayerInputHandler");T("PlayerInputHandler").GetField("playerMovement").SetValue(input,movement);T("PlayerInputHandler").GetField("playerWeapon").SetValue(input,weapon);
        Call(input,"IssueClick",false,Vector2.zero,enemy,false,false);enemy.transform.position=Vector3.zero;
        Assert.AreEqual(new Vector2(7,4),Get(orders,"Destination"));Assert.IsNull(Get(weapon,"PendingTarget"));
        Call(orders,"SetSpeedLimit",.25f);Assert.AreEqual(5f,T("ShipMovement").GetField("maxSpeed").GetValue(movement));
    }
    [Test] public void LostTacticalTarget_Brakes_AndEngineDestroyedHasZeroAvailableForce()
    {
        var weapon=Ship("player");var enemy=Ship("enemy").GetComponent(T("ShipHealth"));
        var movement=weapon.GetComponent(T("ShipMovement"));var orders=Get(movement,"Orders");
        Call(orders,"Tactical",Enum.Parse(T("ShipOrderMode"),"Orbit"),enemy);
        enemy.gameObject.SetActive(false);
        var args=new object[]{Vector2.zero,Vector2.right,true};Call(orders,"Evaluate",args);
        Assert.AreEqual("HoldPosition",Get(orders,"Mode").ToString());Assert.IsTrue((bool)Get(orders,"IsBraking"));
        var data=Get(weapon.GetComponent(T("ShipHealth")),"Data");Call(data,"TakeModuleDamage",Enum.Parse(T("ModuleType"),"Engine"),1000f);
        Assert.AreEqual(0f,Get(movement,"AvailableForce"));
    }
    [Test] public void OrderCharacterization_InitialStopClampAndFlankHysteresis()
    {
        var weapon=Ship("characterization");var movement=weapon.GetComponent(T("ShipMovement"));var orders=Get(movement,"Orders");
        Assert.AreEqual("HoldPosition",Get(orders,"Mode").ToString());Assert.IsFalse((bool)Get(orders,"IsBraking"));
        Call(orders,"SetSpeedLimit",0f);Assert.AreEqual(.25f,Get(orders,"SpeedLimit"));
        Call(orders,"SetSpeedLimit",2f);Assert.AreEqual(1f,Get(orders,"SpeedLimit"));
        var enemy=Ship("characterization target").GetComponent(T("ShipHealth"));
        T("ShipOrderController").GetProperty("PreferredDistance").SetValue(orders,4f);
        Call(orders,"Tactical",Enum.Parse(T("ShipOrderMode"),"Flank"),enemy);
        foreach(var sample in new[]{new Vector2(0,-3.5f),new Vector2(0,-3f),new Vector2(0,-2.5f)})
        {
            Call(orders,"Evaluate",new object[]{sample,Vector2.zero,true});
            Assert.AreEqual(sample.y>-2.6f?"Course":"OrderTarget",Get(orders,"Orientation").ToString());
        }
        Call(orders,"Stop");Assert.IsTrue((bool)Get(orders,"IsBraking"));
        Call(orders,"Evaluate",new object[]{new Vector2(2,3),Vector2.zero,true});
        Assert.IsFalse((bool)Get(orders,"IsBraking"));Assert.AreEqual(new Vector2(2,3),Get(orders,"Destination"));
    }
    [Test] public void TacticalLayout_CanBeAppliedTwice_WithVisibleSpeedAndStopLabels()
    {
        var go=new GameObject("panel",typeof(RectTransform));objects.Add(go);
        var panel=go.AddComponent(T("TacticalCommandPanel"));
        Call(panel,"ConfigureLayout");Call(panel,"ConfigureLayout");
        foreach(string path in new[]{"StopOrder/Caption","SpeedLimit1/Caption","SpeedLimit4/Caption","MovementStatus"})
        {
            var child=go.transform.Find(path);Assert.IsNotNull(child,path);Assert.IsTrue(child.gameObject.activeSelf,path);
        }
    }
}
