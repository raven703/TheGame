using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class AimCharacterizationTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    private Component Weapon()
    {
        var go = new GameObject("isolated-aim-characterization"); objects.Add(go);
        foreach (var name in new[] { "ShipHealth", "ShipMovement", "ShipWeapon" })
        {
            var component = go.AddComponent(T(name));
            T(name).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, null);
        }
        return go.GetComponent(T("ShipWeapon"));
    }
    private static object Item(bool turret) => JsonUtility.FromJson(
        "{\"id\":\"isolated\",\"mountType\":" + (turret ? "1" : "0") + ",\"trackingSpeed\":90,\"aimTolerance\":3}", T("FittingItem"));
    [TearDown] public void Cleanup() { foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go); objects.Clear(); }

    [TestCase(true, 0f)] [TestCase(false, 90f)]
    public void Axis_UsesSpriteNoseAndHull_NotFirePointRotation(bool noseUp, float rotation)
    {
        var weapon = Weapon(); var movement = weapon.GetComponent(T("ShipMovement"));
        Set(movement, "spriteNoseIsUp", noseUp); weapon.transform.rotation = Quaternion.Euler(0, 0, rotation);
        var point = new GameObject("firePoint"); point.transform.SetParent(weapon.transform, false);
        point.transform.localPosition = new Vector3(2, 1); point.transform.localRotation = Quaternion.Euler(0, 0, 73);
        Set(weapon, "firePoints", new[] { point.transform });
        var axis = (Vector2)Call(weapon, "MountAxis", 0);
        Assert.That(Vector2.Distance(axis, Vector2.up), Is.LessThan(.0001f));
        Assert.IsTrue((bool)Call(weapon, "CanAim", 0, Item(false), (Vector2)point.transform.position + axis * 3, .1f));
        Assert.IsFalse((bool)Call(weapon, "CanAim", 0, Item(false), (Vector2)point.transform.position + axis * 9, .1f));
    }

    [Test] public void RefreshRemovalReplacementAndTargetChanges_RetainMountAngle()
    {
        var weapon = Weapon(); Set(weapon, "firingArcAngle", 360f); var item = Item(true);
        Call(weapon, "CanAim", 0, item, Vector2.right * 3, .1f);
        Assert.That((float)Call(weapon, "MountAngle", 0), Is.EqualTo(-9).Within(.001));
        var listType = typeof(List<>).MakeGenericType(T("FittingItem"));
        var list = (System.Collections.IList)Activator.CreateInstance(listType); list.Add(item);
        Call(weapon, "SetEquippedWeapons", list); Call(weapon, "SetEquippedWeapons", list);
        Call(weapon, "ClearTarget"); Call(weapon, "DisruptTargeting", 1f);
        Call(weapon, "SetEquippedWeapons", Activator.CreateInstance(listType));
        list[0] = Item(true); Call(weapon, "SetEquippedWeapons", list);
        Assert.That((float)Call(weapon, "MountAngle", 0), Is.EqualTo(-9).Within(.001));
        Assert.That((float)Call(weapon, "MountAngle", 1), Is.Zero);
    }

    [Test] public void OutsideSector_StillTracksClampedBoundary_AndRangeDoesNotStopTracking()
    {
        var weapon = Weapon(); Set(weapon, "firingArcAngle", 60f);
        Assert.IsFalse((bool)Call(weapon, "CanAim", 0, Item(true), Vector2.right * 100, 1f));
        Assert.That((float)Call(weapon, "MountAngle", 0), Is.EqualTo(-30).Within(.001));
    }
}