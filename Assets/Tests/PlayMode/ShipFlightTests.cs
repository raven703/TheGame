using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>Actual fixed-step physics in an isolated scene, bounded in simulated time.</summary>
public class ShipFlightTests
{
    private readonly List<GameObject> objects=new List<GameObject>();
    private Scene testScene;
    private Scene previousScene;
    private float oldScale;
    private static Type T(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    private static object Get(object o,string name)=>o.GetType().GetProperty(name).GetValue(o);
    private static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name).Invoke(o,args);
    private static void Field(object o,string name,object value)=>o.GetType().GetField(name).SetValue(o,value);
    private GameObject Node(string name)
    {
        var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,testScene);objects.Add(go);return go;
    }
    [UnitySetUp] public IEnumerator Setup()
    {
        oldScale=Time.timeScale;Time.timeScale=1;
        previousScene=SceneManager.GetActiveScene();
        // Disable scene actors without destroying user scene objects or changing saved data.
        if(previousScene.IsValid())foreach(var root in previousScene.GetRootGameObjects())
            if(root.activeSelf && (root.GetComponentInChildren(T("ShipMovement"),true)!=null || root.GetComponentInChildren(T("CombatManager"),true)!=null || root.GetComponentInChildren<Canvas>(true)!=null))
            {disabled.Add(root);root.SetActive(false);}
        testScene=SceneManager.CreateScene("Flight test "+Guid.NewGuid());SceneManager.SetActiveScene(testScene);
        var session=UnityEngine.Object.FindAnyObjectByType(T("CombatSession"),FindObjectsInactive.Include);
        if(session!=null)
        {
            oldSession=session;
            var state=T("GameState").GetProperty("Instance").GetValue(null);
            if(state!=null){oldGameState=state;oldStarted=(bool)state.GetType().GetField("isCombatStarted").GetValue(state);state.GetType().GetField("isCombatStarted").SetValue(state,true);}
            else T("CombatSession").GetProperty("Started").GetSetMethod(true).Invoke(session,new object[]{true});
        }
        yield return null;
    }
    private readonly List<GameObject> disabled=new List<GameObject>();
    private UnityEngine.Object oldSession;
    private object oldGameState;
    private bool oldStarted;
    [UnityTearDown] public IEnumerator Cleanup()
    {
        foreach(var go in objects)if(go!=null)UnityEngine.Object.Destroy(go);objects.Clear();
        yield return null;
        if(previousScene.IsValid())SceneManager.SetActiveScene(previousScene);
        yield return SceneManager.UnloadSceneAsync(testScene);
        if(oldGameState!=null)oldGameState.GetType().GetField("isCombatStarted").SetValue(oldGameState,oldStarted);
        foreach(var go in disabled)if(go!=null)go.SetActive(true);disabled.Clear();
        oldSession=null;oldGameState=null;Time.timeScale=oldScale;
    }
    private Component Ship(out Rigidbody2D rb,out object orders)
    {
        var go=Node("flight ship");rb=go.AddComponent<Rigidbody2D>();go.AddComponent<CircleCollider2D>().radius=.3f;
        var movement=go.AddComponent(T("ShipMovement"));orders=Get(movement,"Orders");return movement;
    }
    private Component Rock(Vector2 position,float radius,Vector2 velocity)
    {
        var go=Node("flight asteroid");go.transform.position=position;go.AddComponent<CircleCollider2D>().radius=radius;
        var body=go.AddComponent<Rigidbody2D>();body.gravityScale=0;body.mass=100;body.linearVelocity=velocity;
        return go.AddComponent(T("Asteroid"));
    }
    private IEnumerator Arrival(float mass,float initialSpeed,float boost)
    {
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);
        rb.mass=mass;rb.linearVelocity=new Vector2(-initialSpeed*.3f,initialSpeed);Field(movement,"acceleration",24f);
        movement.GetType().GetProperty("SpeedMultiplier").SetValue(movement,boost);
        Call(orders,"MoveTo",new Vector2(8,0),false);
        float elapsed=0;
        while(elapsed<25f && Get(orders,"Mode").ToString()!="HoldPosition"){yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;}
        Assert.Less(Vector2.Distance(rb.position,new Vector2(8,0)),.3f,"arrival position mass="+mass+" boost="+boost);
        Assert.Less(rb.linearVelocity.magnitude,.15f,"arrival speed");
        Assert.AreEqual("HoldPosition",Get(orders,"Mode").ToString());
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual("HoldPosition",Get(orders,"Mode").ToString(),"manual route must never resume orbit");
    }
    [UnityTest] public IEnumerator TacticalPause_PlansWithoutSimulation_QueuesOnce_AndRestoresExactTempo()
    {
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);movement.gameObject.tag="Player";
        var health=movement.gameObject.AddComponent(T("ShipHealth"));var data=Get(health,"Data");
        var weapon=movement.gameObject.AddComponent(T("ShipWeapon"));var abilities=movement.gameObject.AddComponent(T("ShipAbilities"));
        var reactor=movement.GetComponent(T("ShipReactor"));
        var input=Node("pause input").AddComponent(T("PlayerInputHandler"));Field(input,"playerMovement",movement);Field(input,"playerWeapon",weapon);
        var existing=T("TimeController").GetProperty("Instance").GetValue(null);
        var time=existing??Node("pause controller").AddComponent(T("TimeController"));
        Time.timeScale=1.75f;Call(time,"TogglePause");Assert.AreEqual(0,Time.timeScale);
        Call(input,"ProcessRightPointer",true,true,false,false,false,new Vector2(10,2),0f);
        Call(input,"ProcessRightPointer",false,true,false,false,false,new Vector2(20,2),1f);
        Assert.AreEqual("MoveToPoint",Get(orders,"Mode").ToString());
        var kind=Enum.Parse(Type.GetType("Game.Core.AbilityCommandKind, Game.Core",true),"ShieldBoost");
        Call(time,"RequestAbility",kind);
        float energy=(float)Get(reactor,"Energy");Vector2 position=rb.position;
        var damage=Get(data,"Damage");var shields=Get(damage,"Shields");Call(shields,"SetDesired",1f);
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(position,rb.position);Assert.AreEqual(energy,Get(reactor,"Energy"));Assert.AreEqual(.5f,Get(shields,"Fraction"));
        Assert.AreEqual(0f,Get(abilities,"shieldBoostCooldown"));
        Call(time,"TogglePause");Assert.AreEqual(1.75f,Time.timeScale);Assert.AreEqual(8f,Get(abilities,"shieldBoostCooldown"));
        Assert.AreEqual(energy-30f,(float)Get(reactor,"Energy"),.001f);
        Assert.AreEqual(0,((ICollection)Get(time.GetType().GetField("Plan").GetValue(time),"Commands")).Count);
        yield return null;
        Assert.Greater((float)Get(abilities,"shieldBoostCooldown"),0f);
    }
    [UnityTest] public IEnumerator Arrival_LightHull_WithLateralVelocity(){yield return Arrival(1,3,1);}
    [UnityTest] public IEnumerator StandardHull_LongStraightReachesSpeed_AndBrakesWithoutDrift()
    {
        foreach(string id in new[]{"sidewinder","cobra_mk3"})
        {
            Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);
            var definition=ScriptableObject.CreateInstance(T("ShipDefinition"));Field(definition,"shipId",id);Field(definition,"flightVersion",0);
            T("CombatStandardDefinitions").GetMethod("UpgradeFlight").Invoke(null,new object[]{definition});
            Field(definition,"baseMass",id=="sidewinder"?1.5f:3f);
            movement.gameObject.SetActive(false);var health=movement.gameObject.AddComponent(T("ShipHealth"));Field(health,"shipDefinition",definition);movement.gameObject.SetActive(true);
            movement.GetType().GetField("shipHealth",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(movement,health);
            var loadout=Activator.CreateInstance(T("ShipLoadout"),id);
            T("ShipStatsCalculator").GetMethod("ApplyNpc").Invoke(null,new object[]{health,definition,loadout});
            Assert.AreEqual(id=="sidewinder"?10f:8f,movement.GetType().GetField("maxSpeed").GetValue(movement));
            Call(orders,"MoveTo",new Vector2(0,500),false);float elapsed=0,t90=-1;
            for(int i=0;i<400;i++){yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;if(t90<0&&rb.linearVelocity.magnitude>=(id=="sidewinder"?9f:7.2f))t90=elapsed;}
            Assert.That(t90,Is.InRange(id=="sidewinder"?1.4f:1.9f,id=="sidewinder"?2.1f:2.6f));
            Assert.AreEqual(id=="sidewinder"?10f:8f,rb.linearVelocity.magnitude,.15f);
            Debug.Log(id+" speed="+rb.linearVelocity.magnitude+" t90="+t90);
            Call(orders,"Stop");yield return new WaitForSeconds(3);Assert.Less(rb.linearVelocity.magnitude,.1f);
            var hold=rb.position;yield return new WaitForSeconds(1);Assert.Less(Vector2.Distance(hold,rb.position),.05f);
            UnityEngine.Object.Destroy(movement.gameObject);UnityEngine.Object.Destroy(definition);yield return null;
        }
    }
    [UnityTest] public IEnumerator Arrival_HeavyHull_WithBoost(){yield return Arrival(6,5,2);}
    [UnityTest] public IEnumerator Stop_ActivelyBrakesLinearAndAngular_ThenHolds()
    {
        Rigidbody2D rb;object orders;Ship(out rb,out orders);rb.linearVelocity=new Vector2(5,2);rb.angularVelocity=100;
        Call(orders,"MoveTo",new Vector2(30,0),false);Call(orders,"Stop");
        float elapsed=0;while(elapsed<6 && (rb.linearVelocity.magnitude>.1f||Mathf.Abs(rb.angularVelocity)>1)){yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;}
        Assert.Less(rb.linearVelocity.magnitude,.1f);Assert.Less(Mathf.Abs(rb.angularVelocity),1);
        var hold=rb.position;yield return new WaitForSeconds(1);Assert.Less(Vector2.Distance(hold,rb.position),.2f);
        Assert.AreEqual(0,((IList)orders.GetType().GetProperty("Waypoints").GetValue(orders)).Count);
    }
    [UnityTest] public IEnumerator LowerSpeedLimit_BrakesGradually()
    {
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);Field(movement,"acceleration",30f);rb.rotation=-90;
        Call(orders,"MoveTo",new Vector2(100,0),false);
        yield return new WaitForSeconds(3);float before=rb.linearVelocity.magnitude;Assert.Greater(before,2);
        Call(orders,"SetSpeedLimit",.25f);Assert.AreEqual(before,rb.linearVelocity.magnitude,.001f,"must not directly clamp physics");
        yield return new WaitForSeconds(3);Assert.Less(rb.linearVelocity.magnitude,1.4f);Assert.AreEqual(5f,movement.GetType().GetField("maxSpeed").GetValue(movement));
    }
    [UnityTest] public IEnumerator EngineDestroyed_OnlyPassiveForcesRemain()
    {
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);
        var health=movement.gameObject.AddComponent(T("ShipHealth"));
        // Movement supports health added after initialization as well as normal prefabs.
        movement.GetType().GetField("shipHealth",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(movement,health);
        var data=Get(health,"Data");Call(data,"TakeModuleDamage",Enum.Parse(T("ModuleType"),"Engine"),1000f);
        rb.linearDamping=0;rb.angularDamping=0;rb.linearVelocity=Vector2.right*3;rb.angularVelocity=20;
        Call(orders,"Stop");yield return new WaitForSeconds(1);
        Assert.AreEqual(3f,rb.linearVelocity.x,.02f);Assert.AreEqual(20f,rb.angularVelocity,.02f);
        Assert.AreEqual(0f,Get(movement,"AvailableForce"));
    }
    private IEnumerator Avoid(Vector2 rockVelocity)
    {
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);Field(movement,"acceleration",25f);
        var rock=Rock(new Vector2(4,0),.9f,rockVelocity);
        Call(orders,"MoveTo",new Vector2(9,0),false);
        float elapsed=0,minClearance=float.PositiveInfinity;
        while(elapsed<30 && Get(orders,"Mode").ToString()!="HoldPosition")
        {yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;minClearance=Mathf.Min(minClearance,Vector2.Distance(rb.position,rock.transform.position));}
        Assert.IsNull(Get(orders,"Error"));Assert.Less(Vector2.Distance(rb.position,new Vector2(9,0)),.35f);
        Assert.Greater(minClearance,1.15f,"ship must not pass through collider");
    }
    [UnityTest] public IEnumerator Avoid_SingleAsteroid(){yield return Avoid(Vector2.zero);}
    [UnityTest] public IEnumerator Avoid_MovingAsteroid(){yield return Avoid(new Vector2(0,.12f));}
    [UnityTest] public IEnumerator DestinationInsideRock_IsProjectedOutside()
    {
        Rigidbody2D rb;object orders;Ship(out rb,out orders);var rock=Rock(new Vector2(4,0),1,Vector2.zero);
        Call(orders,"MoveTo",new Vector2(4,0),false);Vector2 destination=(Vector2)Get(orders,"Destination");
        Assert.Greater(Vector2.Distance(destination,rock.transform.position),1.4f);
        float elapsed=0;while(elapsed<25&&Get(orders,"Mode").ToString()!="HoldPosition"){yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;}
        Assert.Less(Vector2.Distance(rb.position,destination),.35f);
    }
    [UnityTest] public IEnumerator BlockedQueue_StopsWithError_AndRetainsFailedLeg()
    {
        Rigidbody2D rb;object orders;Ship(out rb,out orders);
        // A physical cage cannot be solved by local avoidance.
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var rock=Rock(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*2, .85f,Vector2.zero);rock.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;}
        Call(orders,"MoveTo",new Vector2(10,0),false);Call(orders,"MoveTo",new Vector2(14,0),true);
        float elapsed=0;while(elapsed<45&&Get(orders,"Error")==null){yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;}
        Assert.IsNotNull(Get(orders,"Error"));Assert.AreEqual("HoldPosition",Get(orders,"Mode").ToString());
        Assert.AreEqual(2,((IList)orders.GetType().GetProperty("Waypoints").GetValue(orders)).Count);
        yield return new WaitForSeconds(2);Assert.Less(rb.linearVelocity.magnitude,.15f);
    }
    [UnityTest] public IEnumerator QueuedCorner_IsTraversedInOrder_AndFinalPointStops()
    {
        Rigidbody2D rb;object orders;Ship(out rb,out orders);
        Call(orders,"MoveTo",new Vector2(4,0),false);Call(orders,"MoveTo",new Vector2(4,4),true);
        var points=(IList)orders.GetType().GetProperty("Waypoints").GetValue(orders);
        float elapsed=0;bool passed=false;
        while(elapsed<30 && Get(orders,"Mode").ToString()!="HoldPosition")
        {
            yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;
            points=(IList)Get(orders,"Waypoints");
            if(!passed&&points.Count==1){passed=true;Assert.Less(Vector2.Distance(rb.position,new Vector2(4,0)),1f);}
        }
        Assert.IsTrue(passed);Assert.Less(Vector2.Distance(rb.position,new Vector2(4,4)),.35f);Assert.Less(rb.linearVelocity.magnitude,.15f);
    }
    [UnityTest] public IEnumerator AutoFire_RequiresLock_RespectsEmp_AndDoesNotOverrideRoute()
    {
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);
        var owner=movement.gameObject.AddComponent(T("ShipHealth"));
        var weapon=movement.gameObject.AddComponent(T("ShipWeapon"));Field(weapon,"useFallbackWeapon",true);Field(weapon,"firingArcAngle",360f);
        var enemy=Node("weapon target");enemy.transform.position=new Vector3(0,3);var target=enemy.AddComponent(T("ShipHealth"));
        var data=Get(target,"Data");data.GetType().GetProperty("maxShieldHP").SetValue(data,1000f);data.GetType().GetProperty("currentShieldHP").SetValue(data,1000f);
        var ownData=Get(owner,"Data");ownData.GetType().GetField("lockTime").SetValue(ownData,.4f);
        // Lock/EMP contract must not fail because every permitted shot randomly misses.
        Field(ownData,"accuracy",1f);Field(data,"baseEvasion",0f);Field(data,"speedEvasionMultiplier",0f);
        var input=Node("single-click input").AddComponent(T("PlayerInputHandler"));
        Field(input,"playerMovement",movement);Field(input,"playerWeapon",weapon);
        Call(orders,"BeginFollowCursor",new Vector2(0,1));Call(input,"IssueClick",true,(Vector2)enemy.transform.position,target,false,false);Call(weapon,"DisruptTargeting",.6f);
        yield return new WaitForSeconds(.4f);Assert.AreEqual(1000f,data.GetType().GetProperty("currentShieldHP").GetValue(data));
        yield return new WaitForSeconds(.25f);Assert.AreEqual(1000f,data.GetType().GetProperty("currentShieldHP").GetValue(data));
        yield return new WaitForSeconds(1.2f);Assert.AreSame(target,Get(weapon,"Target"));
        Assert.Less((float)data.GetType().GetProperty("currentShieldHP").GetValue(data),1000f);
        Assert.AreNotEqual("Orbit",Get(orders,"Mode").ToString());
        Assert.IsNull(Get(orders,"OrderTarget"));
    }
    [UnityTest] public IEnumerator EnemyAI_AllFourPersonalitiesUseOneOrderSource()
    {
        var targetGo=Node("AI target");targetGo.transform.position=new Vector3(0,8);var target=targetGo.AddComponent(T("ShipHealth"));
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);var health=movement.gameObject.AddComponent(T("ShipHealth"));
        var weapon=movement.gameObject.AddComponent(T("ShipWeapon"));var ai=movement.gameObject.AddComponent(T("EnemyAI"));Field(ai,"playerTarget",target);
        foreach(string personality in new[]{"Brawler","Sniper","Flanker","Cautious"})
        {
            Field(ai,"personality",Enum.Parse(T("AIPersonality"),personality));
            if(personality=="Cautious"){var d=Get(health,"Data");d.GetType().GetProperty("currentHullHP").SetValue(d,10f);}
            Call(ai,"UpdateOrders");yield return new WaitForFixedUpdate();
            Assert.AreSame(target,Get(orders,"OrderTarget"));Assert.AreNotEqual("MoveToPoint",Get(orders,"Mode").ToString());
            Assert.AreEqual(personality=="Flanker"?"Flank":personality=="Brawler"?"Orbit":"KeepDistance",Get(orders,"Mode").ToString());
        }
        yield return new WaitForSeconds(.5f);Assert.Greater(rb.position.magnitude,.02f);
        targetGo.SetActive(false);yield return new WaitForFixedUpdate();Assert.AreEqual("HoldPosition",Get(orders,"Mode").ToString());
    }
    [UnityTest] public IEnumerator Hardpoints_ActualShots_RespectGeometryResourcesAndStableSlots()
    {
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);
        movement.GetType().GetProperty("enabled").SetValue(movement,false);
        var owner=movement.gameObject.AddComponent(T("ShipHealth"));
        var weapon=movement.gameObject.AddComponent(T("ShipWeapon"));
        var targetGo=Node("mount target");targetGo.transform.position=new Vector3(3,0);
        var target=targetGo.AddComponent(T("ShipHealth"));
        var definition=ScriptableObject.CreateInstance(T("ShipDefinition"));
        Field(definition,"shipId","mount_integration");Field(definition,"weaponSlots",4);
        var points=Array.CreateInstance(T("WeaponHardpoint"),4);
        for(int i=0;i<4;i++)
        {
            var point=Activator.CreateInstance(T("WeaponHardpoint"));Field(point,"id","mount-test-"+i);Field(point,"slotIndex",i);
            Field(point,"compatibility",Enum.ToObject(T("MountCompatibility"),2));Field(point,"sector",i==3?180f:360f);points.SetValue(point,i);
        }
        Field(definition,"hardpoints",points);Field(owner,"shipDefinition",definition);
        var fixedGun=JsonUtility.FromJson("{\"id\":\"fixed\",\"damageBonus\":1,\"fixedFireSector\":60}",T("FittingItem"));
        var turret=JsonUtility.FromJson("{\"id\":\"turret\",\"damageBonus\":1,\"mountType\":1,\"trackingSpeed\":90,\"aimTolerance\":1}",T("FittingItem"));
        var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(T("FittingItem")));
        list.Add(fixedGun);list.Add(null);list.Add(turret);list.Add(null);
        Call(weapon,"SetEquippedWeapons",list);Field(weapon,"fallbackLockTime",.01f);
        var ownData=Get(owner,"Data");ownData.GetType().GetField("lockTime").SetValue(ownData,.01f);
        var reactor=owner.GetComponent(T("ShipReactor"));Call(reactor,"Configure",100f,0f,0f,0f);
        Call(weapon,"SetTarget",target);
        var combat=weapon.GetType().GetField("combat",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(weapon);
        var shots=(IDictionary)combat.GetType().GetField("readyAt",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(combat);
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(100f,Get(reactor,"Energy"),"no spend before turret aims; fixed cannot fire sideways");Assert.AreEqual(0,shots.Count);
        yield return new WaitForSeconds(1f);
        Assert.IsFalse(shots.Contains("mount-test-0"));Assert.IsTrue(shots.Contains("mount-test-2"));
        Assert.Less((float)Get(reactor,"Energy"),100f);Assert.AreEqual(0f,Call(weapon,"MountAngle",1));
        // Fixed keeps its own 60-degree cone even on 360-degree hardpoint.
        Assert.False((bool)Call(weapon,"CanAim",0,fixedGun,Vector2.down*3f,0f));
        Assert.True((bool)Call(weapon,"CanAim",0,fixedGun,Vector2.up*3f,0f));
        Assert.True((bool)Call(weapon,"CanAim",0,fixedGun,(Vector2)(Quaternion.Euler(0,0,30)*Vector2.up)*3f,0f));
        Assert.False((bool)Call(weapon,"CanAim",0,fixedGun,(Vector2)(Quaternion.Euler(0,0,31)*Vector2.up)*3f,0f));
        Field(points.GetValue(0),"sector",20f);
        Assert.False((bool)Call(weapon,"CanAim",0,fixedGun,(Vector2)(Quaternion.Euler(0,0,15)*Vector2.up)*3f,0f),"hardpoint can narrow, never widen, the weapon cone");
        Field(points.GetValue(0),"sector",360f);
        movement.transform.rotation=Quaternion.Euler(0,0,-90);
        Assert.True((bool)Call(weapon,"CanAim",0,fixedGun,Vector2.right*3f,0f));
        Assert.False((bool)Call(weapon,"CanAim",0,fixedGun,Vector2.up*3f,0f));
        movement.transform.rotation=Quaternion.identity;
        Assert.False((bool)Call(weapon,"CanAim",3,turret,Vector2.down*3f,5f));
        Assert.True((bool)Call(weapon,"CanAim",2,turret,Vector2.down*3f,5f));
        float oldAngle=(float)Call(weapon,"MountAngle",2);
        Call(weapon,"CanAim",2,turret,new Vector2(.01f,-3f),.01f);
        Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(oldAngle,(float)Call(weapon,"MountAngle",2))),.901f);
        // UI/equipment refresh preserves identity and readiness, with an empty slot in the middle.
        object ready=shots["mount-test-2"];Call(weapon,"SetEquippedWeapons",list);Assert.AreEqual(ready,shots["mount-test-2"]);
        UnityEngine.Object.Destroy(definition);
    }
    [UnityTest] public IEnumerator RealUGUIRaycast_BlocksWorldOrders()
    {
        var eventGo=Node("test event system");eventGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var canvasGo=Node("test canvas");var canvas=canvasGo.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var panel=new GameObject("blocking panel",typeof(RectTransform),typeof(UnityEngine.UI.Image));panel.transform.SetParent(canvasGo.transform,false);
        var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        yield return null;Canvas.ForceUpdateCanvases();
        bool blocked=(bool)T("PlayerInputHandler").GetMethod("IsScreenPointOverUI").Invoke(null,new object[]{new Vector2(Screen.width*.5f,Screen.height*.5f)});
        Assert.IsTrue(blocked,"real GraphicRaycaster result, not a manually assigned UI flag");
        Rigidbody2D rb;object orders;var movement=Ship(out rb,out orders);var weapon=movement.gameObject.AddComponent(T("ShipWeapon"));
        var input=Node("input").AddComponent(T("PlayerInputHandler"));Field(input,"playerMovement",movement);Field(input,"playerWeapon",weapon);
        Call(input,"IssueClick",false,new Vector2(10,0),null,false,blocked);
        Assert.AreEqual("HoldPosition",Get(orders,"Mode").ToString());
    }
}
