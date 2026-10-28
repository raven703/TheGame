using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WeaponRangeTests
{
    private static Type T(string n)=>Type.GetType(n+", Assembly-CSharp",true);
    private static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n).Invoke(o,a);
    private static void Set(object o,string n,object v)=>o.GetType().GetField(n).SetValue(o,v);
    private static object Get(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
    [UnityTest] public IEnumerator GeometryMatchesAim_Deduplicates_ToggleDoesNotLeak()
    {
        var go=new GameObject("range-test");go.tag="Player";
        var health=go.AddComponent(T("ShipHealth"));var def=ScriptableObject.CreateInstance(T("ShipDefinition"));
        Set(def,"shipId","range-test");Set(def,"weaponSlots",4);Set(health,"shipDefinition",def);
        var points=Array.CreateInstance(T("WeaponHardpoint"),4);
        for(int i=0;i<4;i++)
        {
            var p=Activator.CreateInstance(T("WeaponHardpoint"));Set(p,"slotIndex",i);Set(p,"id","range:"+i);
            Set(p,"sector",i==2?360f:180f);Set(p,"compatibility",Enum.ToObject(T("MountCompatibility"),2));points.SetValue(p,i);
        }
        Set(def,"hardpoints",points);
        var weapon=go.AddComponent(T("ShipWeapon"));Set(weapon,"range",8f);
        var mount=new GameObject("firepoint");mount.transform.SetParent(go.transform,false);mount.transform.localPosition=new Vector3(2,1,0);
        Set(weapon,"firePoints",new[]{mount.transform,mount.transform,mount.transform,mount.transform});
        var list=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(T("FittingItem")));
        var fixedItem=JsonUtility.FromJson("{\"id\":\"fixed\",\"mountType\":0,\"fixedFireSector\":60}",T("FittingItem"));
        var turret=JsonUtility.FromJson("{\"id\":\"turret\",\"mountType\":1,\"trackingSpeed\":90}",T("FittingItem"));
        list.Add(fixedItem);list.Add(fixedItem);list.Add(turret);list.Add(turret);Call(weapon,"SetEquippedWeapons",list);
        var visual=go.AddComponent(T("FiringArcVisualizer"));
        Assert.False((bool)Get(visual,"Visible"));Call(visual,"SetVisible",true);
        yield return null;
        Assert.AreEqual(60f,Call(weapon,"EffectiveSector",0,fixedItem));
        Assert.AreEqual(360f,Call(weapon,"EffectiveSector",2,turret));Assert.AreEqual(180f,Call(weapon,"EffectiveSector",3,turret));
        Assert.AreEqual(1,Get(visual,"RingCount"));Assert.AreEqual(3,Get(visual,"SectorCount"));
        Assert.AreEqual(mount.transform.position,Call(weapon,"MountOrigin",0));
        Call(weapon,"CanAim",2,turret,new Vector2(6,1),.25f);
        var angle=(float)Call(weapon,"MountAngle",2);Assert.That(Mathf.Abs(angle),Is.EqualTo(22.5f).Within(.01f));
        Vector2 direction=(Vector2)Call(weapon,"BarrelDirection",2),axis=(Vector2)Call(weapon,"MountAxis",2);
        Assert.That(Vector2.SignedAngle(axis,direction),Is.EqualTo(angle).Within(.01f));
        int objects=UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        for(int i=0;i<30;i++){Call(visual,"Toggle");Call(visual,"Toggle");}
        yield return null;
        Assert.AreEqual(objects,UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length);
        Call(weapon,"SelectMount",0);Assert.True((bool)Get(visual,"Visible"));
        Assert.IsNull(go.transform.Find("Selected mount sector"));
        list.Clear();list.Add(null);Call(weapon,"SetEquippedWeapons",list);yield return null;
        Assert.AreEqual(0,Get(visual,"RingCount"));Assert.AreEqual(0,Get(visual,"SectorCount"));
        UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(def);yield return null;
    }
}