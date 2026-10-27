using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Reflection avoids a forbidden asmdef dependency on the predefined Assembly-CSharp.
public class CombatFeatureTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private float oldScale;
    private const string PresetKey = "TheGame.Loadout.v2.test-ship.987";
    private string oldPreset;
    private bool hadPreset;
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private Component Add(string type)
    {
        var go = new GameObject("test-" + type);
        objects.Add(go);
        var component = go.AddComponent(RuntimeType(type));
        // EditMode does not run ordinary MonoBehaviour lifecycle callbacks.
        bool initialized = type == "CombatSession"
            ? RuntimeType(type).GetProperty("Instance").GetValue(null) == component
            : type == "ShipHealth" ? Read<object>(component, "Data") != null
            : false;
        if (!initialized)
            RuntimeType(type).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, null);
        return component;
    }
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);
    private static T Read<T>(object target, string name) =>
        (T)target.GetType().GetProperty(name).GetValue(target);
    private static void BeginCombat(Component session) =>
        session.GetType().GetProperty("Started").GetSetMethod(true).Invoke(session, new object[] { true });

    [SetUp] public void SetUp()
    {
        oldScale = Time.timeScale;
        hadPreset = PlayerPrefs.HasKey(PresetKey);
        oldPreset = PlayerPrefs.GetString(PresetKey);
    }
    [TearDown] public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Time.timeScale = oldScale;
        if (hadPreset) PlayerPrefs.SetString(PresetKey, oldPreset); else PlayerPrefs.DeleteKey(PresetKey);
        PlayerPrefs.Save();
    }

    [Test] public void Reactor_RejectsPreparationAndInsufficientEnergy_WithoutSpending()
    {
        var session = Add("CombatSession");
        var reactor = Add("ShipReactor");
        Assert.IsFalse((bool)Call(reactor, "TrySpend", 10f, 20f));
        Assert.AreEqual(100f, Read<float>(reactor, "Energy"));
        BeginCombat(session);
        Assert.IsFalse((bool)Call(reactor, "TrySpend", 101f, 20f));
        Assert.AreEqual(0f, Read<float>(reactor, "Heat"));
        Assert.IsTrue((bool)Call(reactor, "TrySpend", 10f, 20f));
        Assert.AreEqual(90f, Read<float>(reactor, "Energy"));
    }

    [Test] public void Reactor_OverheatBlocksFurtherSpending()
    {
        var reactor = Add("ShipReactor");
        Assert.IsTrue((bool)Call(reactor, "TrySpend", 10f, 100f));
        Assert.IsTrue(Read<bool>(reactor, "Overheated"));
        Assert.IsFalse((bool)Call(reactor, "TrySpend", 1f, 1f));
        Assert.AreEqual(90f, Read<float>(reactor, "Energy"));
    }

    private static object OwnedLoadout() => JsonUtility.FromJson(
        "{\"shipId\":\"test-ship\",\"weaponSlots\":[{\"id\":\"w1\",\"allowedGroup\":0}],\"shieldSlots\":[],\"engineSlots\":[],\"inventory\":[]}",
        RuntimeType("ShipLoadout"));
    private static object PresetCall(string method, params object[] args) =>
        RuntimeType("LoadoutPresets").GetMethod(method).Invoke(null, args);

    [Test] public void Fitting_RoundTripAndCombatLock_AreAtomic()
    {
        var session = Add("CombatSession");
        object owned = OwnedLoadout();
        Assert.IsTrue((bool)PresetCall("Save", owned, 987));
        Assert.IsNotNull(PresetCall("Load", owned, 987));
        string saved = PlayerPrefs.GetString(PresetKey);
        BeginCombat(session);
        Assert.IsNull(PresetCall("Load", owned, 987));
        Assert.AreEqual(saved, PlayerPrefs.GetString(PresetKey));
    }

    [Test] public void Fitting_RejectsUnavailableDuplicateItemsWithoutChangingEquipment()
    {
        Add("CombatSession");
        object owned = OwnedLoadout();
        PlayerPrefs.SetString(PresetKey,
            "{\"shipId\":\"test-ship\",\"weapons\":[\"w1\",\"w1\"],\"shields\":[],\"engines\":[]}");
        Assert.IsNull(PresetCall("Load", owned, 987));
        Assert.IsTrue((bool)PresetCall("Save", owned, 987));
        var loaded = PresetCall("Load", owned, 987);
        Assert.IsNotNull(loaded);
        var weapons = (System.Collections.IList)RuntimeType("ShipLoadout").GetField("weaponSlots").GetValue(loaded);
        Assert.AreEqual(1, weapons.Count);
    }

    [Test] public void WeaponDamage_PlasmaStripsShields_RailgunBypassesThem()
    {
        var plasmaTarget = Add("ShipHealth");
        var railTarget = Add("ShipHealth");
        var kind = RuntimeType("WeaponKind");
        Call(plasmaTarget, "TakeWeaponDamage", 20f, Enum.Parse(kind, "Plasma"), null);
        Call(railTarget, "TakeWeaponDamage", 20f, Enum.Parse(kind, "Railgun"), null);
        object plasma = Read<object>(plasmaTarget, "Data");
        object rail = Read<object>(railTarget, "Data");
        Assert.AreEqual(20f, (float)plasma.GetType().GetField("currentShieldHP").GetValue(plasma));
        Assert.AreEqual(38f, (float)rail.GetType().GetField("currentShieldHP").GetValue(rail));
        Assert.AreEqual(92f, (float)rail.GetType().GetField("currentHullHP").GetValue(rail));
    }

    [Test] public void TargetDisruption_DoesNotLoseTargetOrShortenExistingEffect()
    {
        var weapon = Add("ShipWeapon");
        var target = Add("ShipHealth");
        Call(weapon, "SetTargetImmediate", target);
        Call(weapon, "DisruptTargeting", 3f);
        float first = Read<float>(weapon, "TargetingDisruptionRemaining");
        Call(weapon, "DisruptTargeting", 1f);
        Assert.AreSame(target, Read<object>(weapon, "Target"));
        Assert.IsTrue(Read<bool>(weapon, "IsTargetingDisrupted"));
        Assert.That(Read<float>(weapon, "TargetingDisruptionRemaining"), Is.GreaterThanOrEqualTo(first - .01f));
    }

    [Test] public void Cover_ReturnsNearestAsteroid_AndRemovalClearsIt()
    {
        var near = Add("Asteroid"); near.transform.position = new Vector3(2, 1000);
        near.gameObject.AddComponent<CircleCollider2D>().radius = .5f;
        var far = Add("Asteroid"); far.transform.position = new Vector3(4, 1000);
        far.gameObject.AddComponent<CircleCollider2D>().radius = .5f;
        Physics2D.SyncTransforms();
        object[] args = { new Vector2(0, 1000), new Vector2(6, 1000), Vector2.zero };
        var method = RuntimeType("Asteroid").GetMethod("FindCover");
        Assert.AreSame(near, method.Invoke(null, args));
        UnityEngine.Object.DestroyImmediate(near.gameObject);
        Physics2D.SyncTransforms();
        Assert.AreSame(far, method.Invoke(null, args));
    }
}
