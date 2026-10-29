using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CombatThrustMeasurementTests
{
    [UnityTest] public IEnumerator SweepActualControllerInIsolatedPhysicsScene()
    {
        var movementType=Type.GetType("ShipMovement, Assembly-CSharp",true);
        var sessionType=Type.GetType("CombatSession, Assembly-CSharp",true);
        var singleton=sessionType.GetProperty("Instance");var oldSession=singleton.GetValue(null);
        var roots=SceneManager.GetActiveScene().GetRootGameObjects();var active=new List<GameObject>();
        foreach(var root in roots)if(root.activeSelf && (root.GetComponentInChildren(movementType,true)!=null ||
            root.GetComponentInChildren<Canvas>(true)!=null || root.GetComponentInChildren(sessionType,true)!=null))
        {active.Add(root);root.SetActive(false);}
        singleton.SetValue(null,null);
        var scene=SceneManager.CreateScene("isolated propulsion measurement",new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        var physics=scene.GetPhysicsScene2D();var report=new List<string>();
        try
        {
            foreach(float mass in new[]{1.8f,3f})foreach(float force in new[]{14f,18f,24f,32f,48f,64f,96f})
            {
                var go=new GameObject("measured ship");SceneManager.MoveGameObjectToScene(go,scene);
                var rb=go.AddComponent<Rigidbody2D>();var move=go.AddComponent(movementType);
                ((Behaviour)move).enabled=false;
                movementType.GetField("acceleration").SetValue(move,force);float speed=mass<2?8:6;
                movementType.GetField("maxSpeed").SetValue(move,speed);rb.mass=mass;
                var orders=movementType.GetProperty("Orders").GetValue(move);
                orders.GetType().GetMethod("MoveTo").Invoke(orders,new object[]{new Vector2(0,1000),false});
                var tick=movementType.GetMethod("FixedUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
                float reach=-1;var samples=new List<float>();
                for(int n=0;n<400;n++){tick.Invoke(move,null);physics.Simulate(.02f);samples.Add(rb.linearVelocity.magnitude);if(reach<0&&rb.linearVelocity.magnitude>=speed*.9f)reach=(n+1)*.02f;}
                float terminal=rb.linearVelocity.magnitude;int actual90=samples.FindIndex(v=>v>=terminal*.9f);
                var brakes=new List<string>();
                foreach(float orientation in new[]{0f,90f,180f})
                {
                    rb.rotation=orientation;rb.linearVelocity=Vector2.up*terminal;rb.angularVelocity=0;
                    orders.GetType().GetMethod("Stop").Invoke(orders,null);float stop=-1;
                    for(int n=0;n<400;n++){tick.Invoke(move,null);physics.Simulate(.02f);if(rb.linearVelocity.magnitude<.1f){stop=(n+1)*.02f;break;}}
                    brakes.Add(orientation+":"+stop);
                }
                report.Add($"mass={mass};force={force};t90Specified={reach};t90Actual={(actual90+1)*.02f};speed8={terminal};brakes="+string.Join(",",brakes));
                UnityEngine.Object.DestroyImmediate(go);
            }
            Directory.CreateDirectory("Temp/CombatArchetypesV1");File.WriteAllLines("Temp/CombatArchetypesV1/thrust-sweep.txt",report);
            Assert.AreEqual(14,report.Count);
        }
        finally
        {
            foreach(var root in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
            singleton.SetValue(null,oldSession);foreach(var root in active)if(root!=null)root.SetActive(true);
        }
        yield return SceneManager.UnloadSceneAsync(scene);
    }
}