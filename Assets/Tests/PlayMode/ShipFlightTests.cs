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
    [UnityTest] public IEnumerator Arrival_LightHull_WithLateralVelocity(){yield return Arrival(1,3,1);}
    [UnityTest] public IEnumerator Arrival_HeavyHull_WithBoost(){yield return Arrival(6,5,2);}
    [UnityTest] public IEnumerator Stop_ActivelyBrakesLinearAndAngular_ThenHolds()
    {
        Rigidbody2D rb;object orders;Ship(out rb,out orders);rb.linearVelocity=new Vector2(5,2);rb.angularVelocity=100;
        Call(orders,"MoveTo",new Vector2(30,0),false);Call(orders,"Stop");
        float elapsed=0;while(elapsed<6 && (rb.linearVelocity.magnitude>.1f||Mathf.Abs(rb.angularVelocity)>1)){yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;}
        Assert.Less(rb.linearVelocity.magnitude,.1f);Assert.Less(Mathf.Abs(rb.angularVelocity),1);
        var hold=rb.position;yield return new WaitForSeconds(1);Assert.Less(Vector2.Distance(hold,rb.position),.2f);
        Assert.AreEqual(0,((IList)orders.GetType().GetField("Waypoints").GetValue(orders)).Count);
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
        Assert.AreEqual(2,((IList)orders.GetType().GetField("Waypoints").GetValue(orders)).Count);
        yield return new WaitForSeconds(2);Assert.Less(rb.linearVelocity.magnitude,.15f);
    }
    [UnityTest] public IEnumerator QueuedCorner_IsTraversedInOrder_AndFinalPointStops()
    {
        Rigidbody2D rb;object orders;Ship(out rb,out orders);
        Call(orders,"MoveTo",new Vector2(4,0),false);Call(orders,"MoveTo",new Vector2(4,4),true);
        var points=(IList)orders.GetType().GetField("Waypoints").GetValue(orders);
        float elapsed=0;bool passed=false;
        while(elapsed<30 && Get(orders,"Mode").ToString()!="HoldPosition")
        {
            yield return new WaitForFixedUpdate();elapsed+=Time.fixedDeltaTime;
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
        var data=Get(target,"Data");data.GetType().GetField("currentShieldHP").SetValue(data,1000f);data.GetType().GetField("maxShieldHP").SetValue(data,1000f);
        var ownData=Get(owner,"Data");ownData.GetType().GetField("lockTime").SetValue(ownData,.4f);
        Call(orders,"MoveTo",new Vector2(0,1),false);Call(weapon,"SetTarget",target);Call(weapon,"DisruptTargeting",.6f);
        yield return new WaitForSeconds(.4f);Assert.AreEqual(1000f,data.GetType().GetField("currentShieldHP").GetValue(data));
        yield return new WaitForSeconds(.25f);Assert.AreEqual(1000f,data.GetType().GetField("currentShieldHP").GetValue(data));
        yield return new WaitForSeconds(1.2f);Assert.AreSame(target,Get(weapon,"Target"));
        Assert.Less((float)data.GetType().GetField("currentShieldHP").GetValue(data),1000f);
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
            if(personality=="Cautious"){var d=Get(health,"Data");d.GetType().GetField("currentHullHP").SetValue(d,10f);}
            Call(ai,"UpdateOrders");yield return new WaitForFixedUpdate();
            Assert.AreSame(target,Get(orders,"OrderTarget"));Assert.AreNotEqual("MoveToPoint",Get(orders,"Mode").ToString());
            Assert.AreEqual(personality=="Flanker"?"Flank":personality=="Brawler"?"Orbit":"KeepDistance",Get(orders,"Mode").ToString());
        }
        yield return new WaitForSeconds(.5f);Assert.Greater(rb.position.magnitude,.02f);
        targetGo.SetActive(false);yield return new WaitForFixedUpdate();Assert.AreEqual("HoldPosition",Get(orders,"Mode").ToString());
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