using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CombatCatalogMigrationTests
{
    private static Type T(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    private static object Field(object item,string name)=>item.GetType().GetField(name).GetValue(item);
    [Test] public void StarterGrantChecksInstalledOwnershipAndNeverReplenishes()
    {
        var data=JsonUtility.FromJson("{\"formatVersion\":4,\"starterInventoryIssued\":true,\"loadouts\":[{\"shipId\":\"one\",\"weaponSlots\":[null,{\"id\":\"w4\",\"instanceId\":\"existing\",\"allowedGroup\":0}]}]}",T("ShipLoadoutSaveFile"));
        data=T("PlayerEquipment").GetMethod("Parse").Invoke(null,new object[]{JsonUtility.ToJson(data),true});
        var grant=T("CombatStandardDefinitions").GetMethod("EnsureStarterSet");
        Assert.AreEqual(true,grant.Invoke(null,new[]{data}));
        var inventory=(IList)Field(data,"inventory");Assert.AreEqual(8,inventory.Count);
        foreach(var item in inventory)Assert.AreNotEqual("w4",Field(item,"id"));
        var slots=(IList)Field(((IList)Field(data,"loadouts"))[0],"weaponSlots");
        Assert.IsNull(slots[0]);Assert.AreEqual("existing",Field(slots[1],"instanceId"));
        string saved=JsonUtility.ToJson(data);
        var ledger=Activator.CreateInstance(T("PlayerEquipment"),saved,(Action<string>)(_=>Assert.Fail("Read must not write")));
        var roundtrip=T("PlayerEquipment").GetProperty("Snapshot").GetValue(ledger);
        Assert.AreEqual(1,Field(roundtrip,"combatStarterVersion"));
        ((IList)Field(roundtrip,"inventory")).Clear();
        Assert.AreEqual(false,grant.Invoke(null,new[]{roundtrip}));
        Assert.Zero(((IList)Field(roundtrip,"inventory")).Count);
    }
    [Test] public void OrdinarySpawnerWaveRolesAreExplicit()
    {
        var method=T("ShipSpawner").GetMethod("RoleForWave");
        Assert.AreEqual("Interceptor",method.Invoke(null,new object[]{0,0}).ToString());
        Assert.AreEqual("Artillery",method.Invoke(null,new object[]{1,0}).ToString());
        Assert.AreEqual("Escort",method.Invoke(null,new object[]{2,0}).ToString());
        Assert.AreEqual("Interceptor",method.Invoke(null,new object[]{3,0}).ToString());
        Assert.AreEqual("Escort",method.Invoke(null,new object[]{3,1}).ToString());
    }
    [Test] public void CombinedMigrationAndGrantFailurePreservesOriginalBytes()
    {
        string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);string path=System.IO.Path.Combine(directory,"equipment.json");
        const string original="{\"formatVersion\":4,\"inventory\":[{\"id\":\"w4\",\"instanceId\":\"old\",\"allowedGroup\":0,\"damageBonus\":12}],\"loadouts\":[]}";
        try
        {
            System.IO.File.WriteAllText(path,original);
            var data=T("PlayerEquipment").GetMethod("Parse").Invoke(null,new object[]{original,true});
            T("CombatStandardDefinitions").GetMethod("EnsureStarterSet").Invoke(null,new[]{data});
            Action<string> fail=_=>{throw new System.IO.IOException("injected replace failure");};
            var store=Activator.CreateInstance(T("EquipmentFileStore"),path,fail);
            var error=Assert.Throws<TargetInvocationException>(()=>T("EquipmentFileStore").GetMethod("Write").Invoke(store,new object[]{JsonUtility.ToJson(data)}));
            Assert.IsInstanceOf<System.IO.IOException>(error.InnerException);
            Assert.AreEqual(original,System.IO.File.ReadAllText(path));
            Assert.False(System.IO.File.Exists(path+".pending"));
        }
        finally {System.IO.Directory.Delete(directory,true);}
    }
    [Test] public void NarrowMigrationPreservesIdentityPlacementUnknownRecordsAndIsIdempotent()
    {
        const string json="{\"formatVersion\":4,\"starterInventoryIssued\":true,\"inventory\":[{\"id\":\"w4\",\"instanceId\":\"a\",\"allowedGroup\":0,\"damageBonus\":12},{\"id\":\"custom\",\"instanceId\":\"c\",\"damageBonus\":123}],\"loadouts\":[{\"shipId\":\"one\",\"weaponSlots\":[null,{\"id\":\"w4\",\"instanceId\":\"b\",\"allowedGroup\":0,\"damageBonus\":12}],\"shieldSlots\":[{\"id\":\"s2\",\"instanceId\":\"shield\",\"allowedGroup\":1,\"shieldBonus\":80,\"massBonus\":1}],\"inventory\":[]}]}";
        int writes=0;var ledger=Activator.CreateInstance(T("PlayerEquipment"),json,(Action<string>)(_=>writes++));
        var snapshot=T("PlayerEquipment").GetProperty("Snapshot").GetValue(ledger);
        var free=(IList)Field(snapshot,"inventory");var ship=((IList)Field(snapshot,"loadouts"))[0];
        var installed=(IList)Field(ship,"weaponSlots");
        Assert.AreEqual(2,free.Count);Assert.IsNull(installed[0]);
        Assert.AreEqual("a",Field(free[0],"instanceId"));Assert.AreEqual("b",Field(installed[1],"instanceId"));
        Assert.AreEqual(10f,Field(free[0],"damageBonus"));Assert.AreEqual(10f,Field(installed[1],"damageBonus"));
        Assert.AreEqual(123f,Field(free[1],"damageBonus"));Assert.AreEqual(0,Field(free[1],"balanceVersion"));
        var shield=((IList)Field(ship,"shieldSlots"))[0];Assert.AreEqual(40f,Field(shield,"shieldBonus"));Assert.AreEqual(.6f,Field(shield,"massBonus"));
        string migrated=(string)T("PlayerEquipment").GetProperty("Json").GetValue(ledger);
        var again=Activator.CreateInstance(T("PlayerEquipment"),migrated,(Action<string>)(_=>writes++));
        Assert.AreEqual(migrated,T("PlayerEquipment").GetProperty("Json").GetValue(again));Assert.Zero(writes);
    }
    [TestCase("w4",10f,.7f,6f,7f)] [TestCase("w2",22f,1.4f,14f,20f)]
    [TestCase("w3",26f,2f,20f,25f)] [TestCase("combat-v1:laser-turret",7f,.8f,7f,8f)]
    public void DefinitionSnapshotsDriveWeaponInterpreter(string id,float damage,float interval,float energy,float heat)
    {
        var item=T("CombatStandardDefinitions").GetMethod("Create").Invoke(null,new object[]{id,"test"});
        var stats=T("WeaponStatsCalculator").GetMethod("Calculate").Invoke(null,new object[]{item,.123f,.5f});
        Assert.AreEqual(damage,Field(stats,"damage"));Assert.AreEqual(interval,Field(stats,"cooldown"));
        Assert.AreEqual(energy,Field(stats,"energy"));Assert.AreEqual(heat,Field(stats,"heat"));
        Assert.AreEqual(60f,Field(item,"fixedFireSector"));
        if(id=="combat-v1:laser-turret"){Assert.AreEqual(90f,Field(item,"trackingSpeed"));Assert.AreEqual(3f,Field(item,"aimTolerance"));}
    }
}