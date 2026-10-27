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
        var points=(IList)T("ShipOrderController").GetField("Waypoints").GetValue(orders);
        Assert.AreEqual(2,points.Count);Assert.AreEqual(new Vector2(2,3),points[0]);Assert.AreEqual(new Vector2(4,5),points[1]);
        Call(input,"IssueClick",false,Vector2.zero,null,false,true);Assert.AreEqual(2,points.Count);
        Call(orders,"Stop");Assert.AreEqual(0,points.Count);Assert.IsTrue((bool)Get(orders,"IsBraking"));
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