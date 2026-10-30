using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CombatCatalogMigrationTests
{
    [Test] public void RadarUsesParentMaskRatherThanOversizedContainer()
    {
        var root=new GameObject("radar mask",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Mask));
        var prefab=new GameObject("point",typeof(RectTransform),typeof(UnityEngine.UI.Image));
        try
        {
            root.GetComponent<RectTransform>().sizeDelta=new Vector2(208,208);
            var child=new GameObject("oversized",typeof(RectTransform));child.transform.SetParent(root.transform,false);
            var rect=child.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(260,260);
            var radar=root.AddComponent(T("EnemyRadar"));
            radar.GetType().GetField("dotsContainer").SetValue(radar,rect);
            radar.GetType().GetField("enemyDotPrefab").SetValue(radar,prefab.GetComponent<UnityEngine.UI.Image>());
            var radius=(float)radar.GetType().GetMethod("VisibleRadius",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(radar,null);
            Assert.AreEqual(104,radius,.001f);
            var method=T("EnemyRadar").GetMethod("ProjectOffset");
            foreach(float distance in new[]{.9f,1.1f,2f,5f,10f})for(int i=0;i<8;i++)
            {
                var direction=new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4));
                float padding=new Vector2(12,14).magnitude*.5f+4;
                var point=(Vector2)method.Invoke(null,new object[]{direction*distance*25,25f,radius,padding});
                Assert.LessOrEqual(point.magnitude+padding,104.001f);
                if(distance>1)Assert.AreEqual(radius-padding,point.magnitude,.001f);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(prefab);}
    }
    [Test] public void SupportTimerHasHysteresisPauseResetAndPreparationGuard()
    {
        var root=new GameObject("isolated support timer");
        try
        {
            var type=T("BattleArena");var arena=root.AddComponent(type);
            type.GetMethod("Initialize",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(arena,null);
            var tick=type.GetMethod("TickSupport");
            Action<Vector2,float,bool> step=(p,dt,active)=>tick.Invoke(arena,new object[]{p,dt,active,null});
            step(new Vector2(91,0),4,false);Assert.AreEqual(0,type.GetProperty("OutsideSeconds").GetValue(arena));
            step(new Vector2(91,0),4,true);Assert.AreEqual(4,type.GetProperty("OutsideSeconds").GetValue(arena));
            step(new Vector2(89,0),0,true);Assert.AreEqual(true,type.GetProperty("OutsideSupport").GetValue(arena));
            Assert.AreEqual(4,type.GetProperty("OutsideSeconds").GetValue(arena));
            step(new Vector2(87,0),0,true);Assert.AreEqual(0,type.GetProperty("OutsideSeconds").GetValue(arena));
            step(new Vector2(91,0),3,true);step(Vector2.zero,0,false);
            Assert.AreEqual(false,type.GetProperty("OutsideSupport").GetValue(arena));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void SeededFieldRepeatsPlacementAndRespectsPairClearance()
    {
        Vector3[] first=null;
        for(int run=0;run<2;run++)
        {
            var root=new GameObject("isolated seeded field");
            try
            {
                var field=root.AddComponent(T("AsteroidField"));
                field.GetType().GetField("randomizeSeed").SetValue(field,false);
                field.GetType().GetField("seed").SetValue(field,732);
                field.GetType().GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(field,null);
                Assert.LessOrEqual(root.transform.childCount,15);Assert.Greater(root.transform.childCount,0);
                var snapshot=new Vector3[root.transform.childCount];
                for(int i=0;i<snapshot.Length;i++)
                {
                    var rock=root.transform.GetChild(i);snapshot[i]=new Vector3(rock.position.x,rock.position.y,rock.localScale.x);
                    Assert.That(rock.localScale.x,Is.InRange(.6f,1.6f));
                    for(int j=0;j<i;j++)Assert.GreaterOrEqual(Vector2.Distance(snapshot[i],snapshot[j]),.96f*(snapshot[i].z+snapshot[j].z)+1.2f-.0001f);
                }
                if(first==null)first=snapshot;else CollectionAssert.AreEqual(first,snapshot);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
    [Test] public void RadarProjectionPreservesEightDirectionsAndContainsDistantIcons()
    {
        var method=T("EnemyRadar").GetMethod("ProjectOffset");
        foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right,new Vector2(1,1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(-1,-1)})
        {
            var distant=(Vector2)method.Invoke(null,new object[]{direction.normalized*100,25f,120f,10f});
            Assert.AreEqual(110,distant.magnitude,.001f);Assert.Greater(Vector2.Dot(direction.normalized,distant.normalized),.999f);
            var near=(Vector2)method.Invoke(null,new object[]{direction.normalized*12.5f,25f,120f,10f});Assert.AreEqual(55,near.magnitude,.001f);
        }
    }
    private static Type T(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    private static object Field(object item,string name)=>item.GetType().GetField(name).GetValue(item);
    [Test] public void ShieldV2Migration_PreservesPlacementIdentitiesOtherStatsAndCustomItems()
    {
        const string json="{\"formatVersion\":4,\"combatStarterVersion\":1,\"starterInventoryIssued\":true,\"inventory\":[{\"id\":\"s1\",\"instanceId\":\"light\",\"allowedGroup\":1,\"shieldBonus\":99,\"massBonus\":0.37},{\"id\":\"custom-shield\",\"instanceId\":\"custom\",\"allowedGroup\":1,\"shieldBonus\":123}],\"loadouts\":[{\"shipId\":\"one\",\"shieldSlots\":[null,{\"id\":\"s2\",\"instanceId\":\"heavy\",\"balanceVersion\":1,\"allowedGroup\":1,\"shieldBonus\":80,\"massBonus\":0.6}]}]}";
        var ledger=Activator.CreateInstance(T("PlayerEquipment"),json,(Action<string>)(_=>Assert.Fail("Detached migration must not write")));
        var data=T("PlayerEquipment").GetProperty("Snapshot").GetValue(ledger);
        var inventory=(IList)Field(data,"inventory");Assert.AreEqual(2,inventory.Count);
        Assert.AreEqual("light",Field(inventory[0],"instanceId"));Assert.AreEqual(25f,Field(inventory[0],"shieldBonus"));
        Assert.AreEqual(5f,Field(inventory[0],"shieldRegenPerSecond"));Assert.AreEqual(.37f,Field(inventory[0],"massBonus"));
        Assert.AreEqual(123f,Field(inventory[1],"shieldBonus"));Assert.AreEqual(0f,Field(inventory[1],"shieldRegenPerSecond"));
        var slots=(IList)Field(((IList)Field(data,"loadouts"))[0],"shieldSlots");Assert.AreEqual(2,slots.Count);Assert.IsNull(slots[0]);
        Assert.AreEqual("heavy",Field(slots[1],"instanceId"));Assert.AreEqual(40f,Field(slots[1],"shieldBonus"));Assert.AreEqual(2f,Field(slots[1],"shieldRegenPerSecond"));
        string once=(string)T("PlayerEquipment").GetProperty("Json").GetValue(ledger);
        var again=Activator.CreateInstance(T("PlayerEquipment"),once,(Action<string>)(_=>Assert.Fail("Read must not write")));
        Assert.AreEqual(once,T("PlayerEquipment").GetProperty("Json").GetValue(again));
    }
    [Test] public void SpeedZoomIsMonotonicBoundedAndHasLowSpeedDeadZone()
    {
        var method=T("CameraFollow").GetMethod("SpeedZoomMultiplier");float previous=1;
        for(int i=0;i<=300;i++)
        {
            float value=(float)method.Invoke(null,new object[]{i*.1f,10f,.2f,1.35f,1.6f});
            Assert.That(value,Is.InRange(1f,1.6f));Assert.GreaterOrEqual(value,previous);previous=value;
            if(i<=20)Assert.AreEqual(1f,value);
            if(i==100)Assert.AreEqual(1.35f,value,.0001f);
        }
    }
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