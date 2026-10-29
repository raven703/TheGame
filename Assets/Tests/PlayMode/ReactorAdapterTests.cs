using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ReactorAdapterTests
{
    private static Type T(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    private static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
    private static object Get(object o,string name)=>o.GetType().GetProperty(name).GetValue(o);
    private static float P(object o,string name)=>(float)Get(o,name);
    private static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    private Scene previous,scene;
    private object oldSession,game;
    private bool oldStarted;
    private float scale;
    private UnityEngine.Random.State random;
    private readonly List<GameObject> disabled=new List<GameObject>();
    [UnitySetUp] public IEnumerator Setup()
    {
        scale=Time.timeScale;random=UnityEngine.Random.state;previous=SceneManager.GetActiveScene();
        oldSession=T("CombatSession").GetProperty("Instance").GetValue(null);game=T("GameState").GetProperty("Instance").GetValue(null);
        if(game!=null){oldStarted=(bool)game.GetType().GetField("isCombatStarted").GetValue(game);Set(game,"isCombatStarted",true);}
        foreach(var root in previous.GetRootGameObjects())if(root.activeSelf&&root.name!="Code-based tests runner"){disabled.Add(root);root.SetActive(false);}
        scene=SceneManager.CreateScene("reactor adapter isolated");SceneManager.SetActiveScene(scene);
        T("CombatSession").GetProperty("Instance").SetValue(null,null);Time.timeScale=1;yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        SceneManager.SetActiveScene(previous);yield return SceneManager.UnloadSceneAsync(scene);
        T("CombatSession").GetProperty("Instance").SetValue(null,oldSession);if(game!=null)Set(game,"isCombatStarted",oldStarted);
        foreach(var root in disabled)if(root!=null)root.SetActive(true);disabled.Clear();Time.timeScale=scale;UnityEngine.Random.state=random;
    }
    [UnityTest] public IEnumerator UpdateUsesScaledDeltaExactlyOnce_PauseAndPreparationFreeze()
    {
        var go=new GameObject("timed reactor");var reactor=go.AddComponent(T("ShipReactor"));
        Call(reactor,"Configure",100f,12f,15f,.25f);Call(reactor,"TrySpend",80f,80f);
        Time.timeScale=0;yield return null;float energy=P(reactor,"Energy"),heat=P(reactor,"Heat");
        yield return null;yield return null;Assert.AreEqual(energy,P(reactor,"Energy"),.0001f);Assert.AreEqual(heat,P(reactor,"Heat"),.0001f);
        Time.timeScale=2;yield return null;
        energy=P(reactor,"Energy");heat=P(reactor,"Heat");float start=Time.time;
        yield return new WaitForSeconds(.4f);float elapsed=Time.time-start;
        Assert.AreEqual(energy+12*elapsed,P(reactor,"Energy"),.2f);Assert.AreEqual(heat-15*elapsed,P(reactor,"Heat"),.2f);
        var session=new GameObject("preparation").AddComponent(T("CombatSession"));
        var field=session.GetComponent(T("AsteroidField"));if(field!=null)UnityEngine.Object.DestroyImmediate(field);
        if(game!=null)Set(game,"isCombatStarted",false);
        energy=P(reactor,"Energy");heat=P(reactor,"Heat");yield return null;yield return null;
        Assert.AreEqual(energy,P(reactor,"Energy"),.0001f);Assert.AreEqual(heat,P(reactor,"Heat"),.0001f);Assert.False((bool)Call(reactor,"TrySpend",1f,1f));
    }
    [UnityTest] public IEnumerator WeaponsAndAbilitiesUseSamePool_ResistanceAndFailureDoNotStartCooldown()
    {
        var go=new GameObject("resource owner");go.SetActive(false);go.AddComponent(T("ShipMovement"));var health=go.AddComponent(T("ShipHealth"));
        var weapon=go.AddComponent(T("ShipWeapon"));var abilities=go.AddComponent(T("ShipAbilities"));go.SetActive(true);
        var reactor=go.GetComponent(T("ShipReactor"));((Behaviour)reactor).enabled=false;
        Call(reactor,"Configure",100f,0f,15f,.25f);((Behaviour)weapon).enabled=false;
        var enemy=new GameObject("target");enemy.transform.position=new Vector3(0,3);var target=enemy.AddComponent(T("ShipHealth"));
        var items=JsonUtility.FromJson("{\"weaponSlots\":[{\"id\":\"w1\",\"damageBonus\":15},{\"id\":\"w2\",\"damageBonus\":30}]}",T("ShipLoadout"));
        Call(weapon,"SetEquippedWeapons",items.GetType().GetField("weaponSlots").GetValue(items));Call(weapon,"SetTargetImmediate",target);
        Call(weapon,"Shoot");Assert.AreEqual(77,P(reactor,"Energy"),.001f);Assert.AreEqual(23.25f,P(reactor,"Heat"),.001f);
        Assert.True((bool)Call(abilities,"UseAfterburner"));Assert.AreEqual(52,P(reactor,"Energy"));Assert.AreEqual(38.25f,P(reactor,"Heat"),.001f);
        Assert.AreEqual(2f,P(go.GetComponent(T("ShipMovement")),"SpeedMultiplier"));
        Call(reactor,"TrySpend",52f,0f);Assert.False((bool)Call(abilities,"UseShieldBoost"));Assert.AreEqual(0f,P(abilities,"shieldBoostCooldown"));
        Call(reactor,"Configure",200f,0f,15f,.25f);Assert.True((bool)Call(reactor,"TrySpend",0f,1000f));
        Assert.False((bool)Call(abilities,"UseShieldBoost"));Assert.AreEqual(0f,P(abilities,"shieldBoostCooldown"));
        Call(reactor,"Tick",65f/15f);Assert.False((bool)Get(reactor,"Overheated"));
        Assert.True((bool)Call(abilities,"UseShieldBoost"));Assert.AreEqual(8f,P(abilities,"shieldBoostCooldown"));Assert.AreEqual(46.25f,P(reactor,"Heat"),.001f);
        yield return null;
    }
    [UnityTest] public IEnumerator PhysicalNearestCoverTakesDamage_RefreshKeepsReload_EmptySlotCannotFallback()
    {
        var go=new GameObject("cover shooter");go.SetActive(false);
        go.AddComponent(T("ShipMovement"));var owner=go.AddComponent(T("ShipHealth"));var weapon=go.AddComponent(T("ShipWeapon"));go.SetActive(true);
        ((Behaviour)weapon).enabled=false;
        var reactor=go.GetComponent(T("ShipReactor"));((Behaviour)reactor).enabled=false;Call(reactor,"Configure",100f,0f,0f,0f);
        var enemy=new GameObject("covered target");enemy.transform.position=new Vector3(0,4);
        var target=enemy.AddComponent(T("ShipHealth"));
        var near=new GameObject("near cover");near.transform.position=new Vector3(0,1);near.AddComponent<CircleCollider2D>().radius=.3f;var rock=near.AddComponent(T("Asteroid"));
        var far=new GameObject("far cover");far.transform.position=new Vector3(0,2);far.AddComponent<CircleCollider2D>().radius=.3f;var farther=far.AddComponent(T("Asteroid"));
        Physics2D.SyncTransforms();
        var items=JsonUtility.FromJson("{\"weaponSlots\":[{\"id\":\"w1\",\"damageBonus\":15},null]}",T("ShipLoadout"));
        var slots=items.GetType().GetField("weaponSlots").GetValue(items);
        Call(weapon,"SetEquippedWeapons",slots);Call(weapon,"SetTargetImmediate",target);
        var before=UnityEngine.Random.state;
        Call(weapon,"Shoot");
        Assert.AreEqual(105f,(float)rock.GetType().GetProperty("hitPoints").GetValue(rock));
        Assert.AreEqual(120f,(float)farther.GetType().GetProperty("hitPoints").GetValue(farther));
        Assert.AreEqual(93f,P(reactor,"Energy"));Assert.AreEqual(9f,P(reactor,"Heat"));
        float afterRoll=UnityEngine.Random.value;UnityEngine.Random.state=before;Assert.AreEqual(UnityEngine.Random.value,afterRoll,"cover must not consume hit/module random rolls");
        Call(weapon,"SetEquippedWeapons",slots);Call(weapon,"Shoot");Assert.AreEqual(93f,P(reactor,"Energy"),"refresh cannot reset cooldown");
        Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<LineRenderer>().Length,"one actual shot, not empty slot or cooldown refusal");
        Set(weapon,"useFallbackWeapon",true);
        var empty=JsonUtility.FromJson("{\"weaponSlots\":[null,null]}",T("ShipLoadout"));Call(weapon,"SetEquippedWeapons",empty.GetType().GetField("weaponSlots").GetValue(empty));
        Call(weapon,"Shoot");Assert.AreEqual(93f,P(reactor,"Energy"),"empty physical slots cannot enable fallback");
        yield return null;
    }
    [UnityTest] public IEnumerator MountLifecycle_ShipSwapRefreshRemovalHolesAndRecreation_AreIsolated()
    {
        var definitions=new List<UnityEngine.Object>();
        Func<string,int,UnityEngine.Object> definition=(id,count)=>{
            var def=ScriptableObject.CreateInstance(T("ShipDefinition"));definitions.Add(def);
            Set(def,"shipId",id);Set(def,"weaponSlots",count);
            var points=Array.CreateInstance(T("WeaponHardpoint"),count);
            for(int i=0;i<count;i++){
                var point=Activator.CreateInstance(T("WeaponHardpoint"));Set(point,"id",id+":weapon:"+i);
                Set(point,"slotIndex",i);Set(point,"sector",360f);Set(point,"compatibility",Enum.ToObject(T("MountCompatibility"),2));points.SetValue(point,i);
            }
            Set(def,"hardpoints",points);return def;
        };
        var a=definition("isolated-A",3);var b=definition("isolated-B",1);
        Func<GameObject> ship=()=>{
            var node=new GameObject("lifecycle ship");node.SetActive(false);
            node.AddComponent(T("ShipHealth"));node.AddComponent(T("ShipWeapon"));node.SetActive(true);
            ((Behaviour)node.GetComponent(T("ShipWeapon"))).enabled=false;
            ((Behaviour)node.GetComponent(T("ShipReactor"))).enabled=false;return node;
        };
        var go=ship();var health=go.GetComponent(T("ShipHealth"));var weapon=go.GetComponent(T("ShipWeapon"));
        Set(health,"shipDefinition",a);
        var reactor=go.GetComponent(T("ShipReactor"));Call(reactor,"Configure",100f,0f,0f,0f);
        var enemy=new GameObject("lifecycle target");enemy.transform.position=new Vector3(3,0);
        var target=enemy.AddComponent(T("ShipHealth"));Call(weapon,"SetTargetImmediate",target);
        var loadout=JsonUtility.FromJson("{\"weaponSlots\":[null,null,{\"id\":\"isolated-turret\",\"mountType\":1,\"trackingSpeed\":90,\"aimTolerance\":1,\"damageBonus\":1}]}",T("ShipLoadout"));
        var slots=(System.Collections.IList)loadout.GetType().GetField("weaponSlots").GetValue(loadout);
        Call(weapon,"SetEquippedWeapons",slots);Call(weapon,"CanAim",2,slots[2],new Vector2(3,0),2f);
        Call(weapon,"Shoot");
        var combat=weapon.GetType().GetField("combat",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(weapon);
        var deadlines=(System.Collections.IDictionary)combat.GetType().GetField("readyAt",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(combat);
        Assert.AreEqual(1,deadlines.Count);Assert.True(deadlines.Contains("isolated-A:weapon:2"));
        var deadline=deadlines["isolated-A:weapon:2"];float energy=P(reactor,"Energy");
        float angle=(float)Call(weapon,"MountAngle",2);
        Call(weapon,"SetEquippedWeapons",slots);Call(weapon,"Shoot");
        Assert.AreEqual(angle,Call(weapon,"MountAngle",2));Assert.AreEqual(deadline,deadlines["isolated-A:weapon:2"]);Assert.AreEqual(energy,P(reactor,"Energy"));
        var removed=slots[2];slots[2]=null;Call(weapon,"SetEquippedWeapons",slots);Set(weapon,"useFallbackWeapon",true);Call(weapon,"Shoot");
        Assert.AreEqual(energy,P(reactor,"Energy"));Assert.AreEqual(3,Get(weapon,"EquippedSlotCount"));
        Assert.IsNull(Call(weapon,"EquippedWeapon",0));Assert.IsNull(Call(weapon,"EquippedWeapon",1));
        slots[2]=JsonUtility.FromJson(JsonUtility.ToJson(removed),T("FittingItem"));Call(weapon,"SetEquippedWeapons",slots);Call(weapon,"Shoot");
        Assert.AreEqual(angle,Call(weapon,"MountAngle",2));Assert.AreEqual(deadline,deadlines["isolated-A:weapon:2"]);Assert.AreEqual(energy,P(reactor,"Energy"));
        // Same runtime hull, different definition and hardpoint set, as fitting selection uses.
        Set(health,"shipDefinition",b);slots.Clear();slots.Add(removed);Call(weapon,"SetEquippedWeapons",slots);
        Assert.AreEqual(0f,Call(weapon,"MountAngle",0));Assert.False(deadlines.Contains("isolated-B:weapon:0"));
        Assert.AreEqual("isolated-B:weapon:0",Call(weapon,"MountId",0));
        Call(weapon,"CanAim",0,removed,new Vector2(3,0),2f);Call(weapon,"Shoot");
        Assert.True(deadlines.Contains("isolated-B:weapon:0"));Assert.AreEqual(deadline,deadlines["isolated-A:weapon:2"]);
        UnityEngine.Object.Destroy(go);yield return null;
        var recreated=ship();var fresh=recreated.GetComponent(T("ShipWeapon"));Set(recreated.GetComponent(T("ShipHealth")),"shipDefinition",a);
        Assert.AreEqual(0f,Call(fresh,"MountAngle",2));
        var freshCombat=fresh.GetType().GetField("combat",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(fresh);
        Assert.AreNotSame(combat,freshCombat);
        Assert.AreEqual(0,((System.Collections.IDictionary)freshCombat.GetType().GetField("readyAt",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(freshCombat)).Count);
        foreach(var def in definitions)UnityEngine.Object.Destroy(def);
    }

    [UnityTest] public IEnumerator ShieldBoostHullPaymentAndModuleEffectsUseCore_DeathNotificationOnce()
    {
        var go=new GameObject("boost owner");go.SetActive(false);var movement=go.AddComponent(T("ShipMovement"));
        var health=go.AddComponent(T("ShipHealth"));go.AddComponent(T("ShipWeapon"));var abilities=go.AddComponent(T("ShipAbilities"));go.SetActive(true);
        var data=Get(health,"Data");var dataType=data.GetType();
        dataType.GetProperty("currentHullHP").SetValue(data,10f);dataType.GetProperty("currentShieldHP").SetValue(data,0f);
        int destroyed=0;
        T("EnemyRegistry").GetMethod("Register").Invoke(null,new object[]{health});
        health.GetType().GetEvent("OnDestroyed").AddEventHandler(health,(Action)(()=>{
            destroyed++;
            Assert.True(go.activeSelf,"subscribers run before deactivation");
            T("EnemyRegistry").GetMethod("Unregister").Invoke(null,new object[]{health});
        }));
        Assert.True((bool)Call(abilities,"UseShieldBoost"));
        Assert.AreEqual(0f,(float)dataType.GetProperty("currentHullHP").GetValue(data));Assert.AreEqual(30f,(float)dataType.GetProperty("currentShieldHP").GetValue(data));
        Assert.AreEqual(0,destroyed,"legacy notification waits for Update");
        yield return null;Assert.AreEqual(1,destroyed);Assert.False(go.activeSelf);
        Assert.AreEqual(0,((System.Collections.IList)T("EnemyRegistry").GetMethod("GetAliveEnemies").Invoke(null,null)).Count);
        Call(health,"Update");Assert.AreEqual(1,destroyed);
    }
}