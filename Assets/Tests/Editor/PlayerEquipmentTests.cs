using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PlayerEquipmentTests
{
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    private static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value,args);
    private static object Snapshot(object ledger) => ledger.GetType().GetProperty("Snapshot").GetValue(ledger);
    private static string Json(object ledger) => (string)ledger.GetType().GetProperty("Json").GetValue(ledger);
    private static object Group => Enum.ToObject(T("SlotGroup"),0);
    private static object Ship(object ledger,int index=0) => ((IList)Field(Snapshot(ledger),"loadouts"))[index];
    private static IList Free(object ledger) => (IList)Field(Snapshot(ledger),"inventory");
    private const string Initial = "{\"formatVersion\":4,\"starterInventoryIssued\":true,\"inventory\":[{\"id\":\"laser\",\"name\":\"Laser\",\"instanceId\":\"a\",\"allowedGroup\":0},{\"id\":\"laser\",\"name\":\"Laser\",\"instanceId\":\"b\",\"allowedGroup\":0}],\"loadouts\":[{\"shipId\":\"test-a\",\"weaponSlots\":[null,null,null],\"inventory\":[]},{\"shipId\":\"test-b\",\"weaponSlots\":[{\"id\":\"other\",\"instanceId\":\"c\",\"allowedGroup\":0},null,null],\"inventory\":[]}]}";
    private object ledger;
    private ScriptableObject definition;
    private GameObject session;
    private bool failWrite;
    private int writes;
    [SetUp] public void Setup()
    {
        failWrite=false;writes=0;
        session=new GameObject("equipment-test-session");var component=session.AddComponent(T("CombatSession"));
        T("CombatSession").GetProperty("Instance").SetValue(null,component);
        definition=ScriptableObject.CreateInstance(T("ShipDefinition"));
        definition.GetType().GetField("shipId").SetValue(definition,"test-a");
        definition.GetType().GetField("weaponSlots").SetValue(definition,3);
        Action<string> writer=json=>{if(failWrite)throw new System.IO.IOException("injected disk failure");writes++;};
        ledger=Activator.CreateInstance(T("PlayerEquipment"),Initial,writer);
    }
    [TearDown] public void Cleanup(){T("CombatSession").GetProperty("Instance").SetValue(null,null);UnityEngine.Object.DestroyImmediate(definition);UnityEngine.Object.DestroyImmediate(session);}
    private bool Move(string id,int target,int source=-1,bool edit=true)
    {
        object[] args={"test-a",definition,id,Group,target,source,edit,null};
        bool result=(bool)ledger.GetType().GetMethod("Move").Invoke(ledger,args);
        if(!result)TestContext.WriteLine(args[7]);return result;
    }
    private bool Can(string id,int target,int source=-1,bool edit=true)
    {
        object[] args={"test-a",definition,id,Group,target,source,edit,null};
        bool result=(bool)ledger.GetType().GetMethod("CanMove").Invoke(ledger,args);
        if(!result)TestContext.WriteLine(args[7]);return result;
    }
    [Test] public void EmptyCompatible_AgreesWithMove_OccupiedAndOriginRejectWithoutChanges()
    {
        Assert.True(Can("a",0));Assert.True(Move("a",0));Assert.AreEqual(1,Free(ledger).Count);
        string before=Json(ledger);
        Assert.False(Can("b",0));Assert.False(Move("b",0));Assert.False(Can("a",0,0));Assert.False(Move("a",0,0));
        Assert.AreEqual(before,Json(ledger));
        Assert.True(Can("a",2,0));Assert.True(Move("a",2,0));
        Assert.IsNull(((IList)Field(Ship(ledger),"weaponSlots"))[0]);Assert.AreEqual(1,Free(ledger).Count);
    }
    [Test] public void OwnershipCombatAndIncompatibleMountRejectWithoutWrites()
    {
        Assert.False(Move("forged",0));Assert.False(Move("c",0));Assert.False(Move("a",0,-1,false));
        var point=Activator.CreateInstance(T("WeaponHardpoint"));point.GetType().GetField("slotIndex").SetValue(point,1);
        point.GetType().GetField("compatibility").SetValue(point,Enum.ToObject(T("MountCompatibility"),1));
        var array=Array.CreateInstance(T("WeaponHardpoint"),1);array.SetValue(point,0);definition.GetType().GetField("hardpoints").SetValue(definition,array);
        Assert.False(Can("a",1));Assert.False(Move("a",1));Assert.AreEqual(0,writes);Assert.AreEqual(2,Free(ledger).Count);
    }
    [Test] public void FailureRollsBackMoveUnequipAndPreset()
    {
        string before=Json(ledger);failWrite=true;Assert.False(Move("a",0));Assert.AreEqual(before,Json(ledger));
        failWrite=false;Assert.True(Move("a",0));before=Json(ledger);failWrite=true;
        Assert.False((bool)Call(ledger,"Unequip","test-a",Group,0,"a",true,null));Assert.AreEqual(before,Json(ledger));
        var plan=JsonUtility.FromJson("{\"shipId\":\"test-a\",\"weaponSlots\":[null,{\"id\":\"laser\",\"allowedGroup\":0}]}",T("ShipLoadout"));
        Assert.False((bool)Call(ledger,"ApplyPlan",definition,plan,true,null));Assert.AreEqual(before,Json(ledger));
    }
    [Test] public void UnequipReturnsExactlyOne_TransferBetweenShips_SelectionDoesNotGrant()
    {
        Assert.True(Move("a",0));Assert.True((bool)Call(ledger,"Unequip","test-a",Group,0,"a",true,null));
        Assert.False((bool)Call(ledger,"Unequip","test-a",Group,0,"a",true,null));Assert.AreEqual(2,Free(ledger).Count);
        definition.GetType().GetField("shipId").SetValue(definition,"test-b");
        Assert.True((bool)Call(ledger,"EnsureShip",definition,null,null));
        Assert.True((bool)Call(ledger,"Move","test-b",definition,"a",Group,1,-1,true,null));
        Assert.AreEqual("c",Field(((IList)Field(Ship(ledger,1),"weaponSlots"))[0],"instanceId"));
        Assert.AreEqual("a",Field(((IList)Field(Ship(ledger,1),"weaponSlots"))[1],"instanceId"));
        Assert.AreEqual(1,Free(ledger).Count);
        for(int i=0;i<3;i++)Assert.True((bool)Call(ledger,"EnsureShip",definition,null,null));Assert.AreEqual(1,Free(ledger).Count);
    }
    [Test] public void PresetUsesDefinitionCopies_MissingRejectsWhole_LeavesOtherShipAlone()
    {
        var plan=JsonUtility.FromJson("{\"shipId\":\"test-a\",\"weaponSlots\":[{\"id\":\"laser\"},{\"id\":\"laser\"},null]}",T("ShipLoadout"));
        Assert.True((bool)Call(ledger,"ApplyPlan",definition,plan,true,null));Assert.AreEqual(0,Free(ledger).Count);
        var slots=(IList)Field(Ship(ledger),"weaponSlots");Assert.AreNotEqual(Field(slots[0],"instanceId"),Field(slots[1],"instanceId"));
        string before=Json(ledger);
        plan=JsonUtility.FromJson("{\"shipId\":\"test-a\",\"weaponSlots\":[{\"id\":\"laser\"},{\"id\":\"other\"},null]}",T("ShipLoadout"));
        Assert.False((bool)Call(ledger,"ApplyPlan",definition,plan,true,null));Assert.AreEqual(before,Json(ledger));
        var restored=Activator.CreateInstance(T("PlayerEquipment"),Json(ledger),(Action<string>)(_=>{}));Assert.AreEqual(Json(ledger),Json(restored));
    }
    [Test] public void MigrationPreservesEveryLegacyRecordAndHoles_RepeatedV4DoesNotGrant()
    {
        string legacy="{\"loadouts\":[{\"shipId\":\"one\",\"weaponSlots\":[null,{\"id\":\"same\"}],\"inventory\":[{\"id\":\"same\"}]},{\"shipId\":\"two\",\"inventory\":[{\"id\":\"same\"}]}]}";
        var migrated=Activator.CreateInstance(T("PlayerEquipment"),legacy,(Action<string>)(_=>{}));
        Assert.AreEqual(2,Free(migrated).Count);Assert.IsNull(((IList)Field(Ship(migrated),"weaponSlots"))[0]);
        string a=(string)Field(Free(migrated)[0],"instanceId"),b=(string)Field(Free(migrated)[1],"instanceId");Assert.AreNotEqual(a,b);
        for(int i=0;i<3;i++){string json=Json(migrated);migrated=Activator.CreateInstance(T("PlayerEquipment"),json,(Action<string>)(_=>{}));Assert.AreEqual(json,Json(migrated));}
    }
    [Test] public void DuplicateOrMissingV4IdentityAndCorruptionAreNotReset()
    {
        foreach(string bad in new[]{"{}",Initial.Replace("\"instanceId\":\"b\"","\"instanceId\":\"a\""),Initial.Replace("\"instanceId\":\"b\"","\"instanceId\":\"\"")})
            Assert.Throws<TargetInvocationException>(()=>Activator.CreateInstance(T("PlayerEquipment"),bad,(Action<string>)(_=>{})));
    }
    [Test] public void V4RoundTripRetainsFullSavedItemDataAndDetachedSnapshots()
    {
        var data=Snapshot(ledger);var item=((IList)Field(data,"inventory"))[0];
        foreach(var field in new[]{"damageBonus","cooldownBonus","shieldBonus","speedBonus","accelBonus","massBonus","heatResistanceBonus","lockTimeBonus","fixedFireSector","trackingSpeed","aimTolerance"})
            item.GetType().GetField(field).SetValue(item,12.345f);
        item.GetType().GetField("iconColor").SetValue(item,new Color(.13f,.27f,.39f,.51f));
        item.GetType().GetField("description").SetValue(item,"saved custom data, not current catalog");
        string input=JsonUtility.ToJson(data,true);
        var restored=Activator.CreateInstance(T("PlayerEquipment"),input,(Action<string>)(_=>{}));
        Assert.AreEqual(input,Json(restored));
        var read=Snapshot(restored);((IList)Field(read,"inventory")).Clear();((IList)Field(((IList)Field(read,"loadouts"))[0],"weaponSlots")).Clear();
        Assert.AreEqual(input,Json(restored));
        for(int i=0;i<3;i++){restored=Activator.CreateInstance(T("PlayerEquipment"),Json(restored),(Action<string>)(_=>{}));Assert.AreEqual(input,Json(restored));}
    }
    [Test] public void IsolatedFileWriterFailurePreservesExistingBytesAndPublishesNothing()
    {
        string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"TheGame-equipment-"+Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);string path=System.IO.Path.Combine(directory,"ship_loadouts.json");
        try
        {
            var store=Activator.CreateInstance(T("EquipmentFileStore"),path,null);
            Call(store,"Write",Initial);byte[] original=System.IO.File.ReadAllBytes(path);
            Action<string> fail=_=>{throw new System.IO.IOException("injected before atomic replace");};
            var failing=Activator.CreateInstance(T("EquipmentFileStore"),path,fail);
            var isolated=Activator.CreateInstance(T("PlayerEquipment"),Initial,(Action<string>)(json=>Call(failing,"Write",json)));
            string before=Json(isolated);
            Assert.False((bool)Call(isolated,"Move","test-a",definition,"a",Group,2,-1,true,null));
            Assert.AreEqual(before,Json(isolated));CollectionAssert.AreEqual(original,System.IO.File.ReadAllBytes(path));
            Assert.False(System.IO.File.Exists(path+".pending"));
            isolated=Activator.CreateInstance(T("PlayerEquipment"),Initial,(Action<string>)(json=>Call(store,"Write",json)));
            Assert.True((bool)Call(isolated,"Move","test-a",definition,"a",Group,2,-1,true,null));
            var reloaded=Activator.CreateInstance(T("PlayerEquipment"),System.IO.File.ReadAllText(path),(Action<string>)(_=>{}));
            Assert.AreEqual(Json(isolated),Json(reloaded));var slots=(IList)Field(Ship(reloaded),"weaponSlots");
            Assert.IsNull(slots[0]);Assert.IsNull(slots[1]);Assert.AreEqual("a",Field(slots[2],"instanceId"));
            CollectionAssert.AreEqual(original,System.IO.File.ReadAllBytes(path+".previous"));
        }
        finally{System.IO.Directory.Delete(directory,true);}
    }
    [Test] public void CategoryEnumMappingAndHardpointIdsStayStable()
    {
        var core=Assembly.Load("Game.Core").GetType("Game.Core.EquipmentCategory",true);
        foreach(var pair in new[]{new[]{"Top_Weapons","Weapons"},new[]{"Center_Shields","Shields"},new[]{"Bottom_Engines","Engines"}})
            Assert.AreEqual(Convert.ToInt32(Enum.Parse(T("SlotGroup"),pair[0])),Convert.ToInt32(Enum.Parse(core,pair[1])));
        var converted=T("EquipmentDataAdapter").GetMethod("Definition").Invoke(null,new object[]{definition});
        var point=converted.GetType().GetMethod("Hardpoint").Invoke(converted,new object[]{2});
        Assert.AreEqual("test-a:weapon:2",Field(point,"Id"));
    }
}