using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Baseline contract: run against the existing Unity implementation before extraction.
public class DamageCharacterizationTests
{
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(value, args);
    private static object Read(object value, string name) => (value.GetType().GetField(name)!=null?value.GetType().GetField(name).GetValue(value):value.GetType().GetProperty(name).GetValue(value));
    private static float F(object value, string name) => (float)Read(value, name);
    private static void Set(object value, string name, object data) { var field=value.GetType().GetField(name); if(field!=null)field.SetValue(value,data);else value.GetType().GetProperty(name).SetValue(value,data); }
    private static object Module(string name) => Enum.Parse(T("ModuleType"), name);
    private GameObject go;
    private object health, data, oldSession;
    private UnityEngine.Random.State random;
    private int damageEvents, destroyedEvents;

    [SetUp] public void Setup()
    {
        damageEvents = destroyedEvents = 0;
        random = UnityEngine.Random.state;
        oldSession = T("CombatSession").GetProperty("Instance").GetValue(null);
        T("CombatSession").GetProperty("Instance").SetValue(null, null);
        go = new GameObject("damage characterization");
        health = go.AddComponent(T("ShipHealth"));
        Call(health, "Awake");
        data = health.GetType().GetProperty("Data").GetValue(health);
        health.GetType().GetEvent("OnDamageTaken").AddEventHandler(health, (Action)(() => damageEvents++));
        health.GetType().GetEvent("OnDestroyed").AddEventHandler(health, (Action)(() => destroyedEvents++));
    }
    [TearDown] public void Cleanup()
    {
        UnityEngine.Object.DestroyImmediate(go);
        T("CombatSession").GetProperty("Instance").SetValue(null, oldSession);
        UnityEngine.Random.state = random;
    }
    [Test] public void ShieldAbsorbsBeforeSelectedModule_ThenPartialDamageReachesModule()
    {
        Call(health, "TakeDamage", 20f, Module("Weapon"));
        Assert.AreEqual(30f, F(data, "currentShieldHP"));
        var module = Call(data, "GetModule", Module("Weapon"));
        Assert.AreEqual(30f, F(module, "currentHP"));
        Call(health, "TakeDamage", 40f, Module("Weapon"));
        Assert.AreEqual(0f, F(data, "currentShieldHP"));
        Assert.AreEqual(20f, F(module, "currentHP"));
        Assert.AreEqual(100f, F(data, "currentHullHP"));
        Assert.AreEqual(2, damageEvents);
    }
    [Test] public void FirstModuleDestructionDiscardsExcess_AlreadyDestroyedSpillsHalf()
    {
        Set(data, "currentShieldHP", 0f);
        var module = Call(data, "GetModule", Module("Engine"));
        Call(health, "TakeDamage", 100f, Module("Engine"));
        Assert.AreEqual(0f, F(module, "currentHP"));
        Assert.AreEqual(100f, F(data, "currentHullHP"));
        Call(health, "TakeDamage", 20f, Module("Engine"));
        Assert.AreEqual(90f, F(data, "currentHullHP"));
        Assert.AreEqual(2, damageEvents);
        Assert.AreEqual(0f, (float)Call(data, "GetTotalEvasion", 5f));
    }
    [Test] public void DestroyedShieldModuleLeavesShieldValueButCannotAbsorb()
    {
        Call(data, "TakeModuleDamage", Module("Shield"), 40f);
        Call(health, "TakeDamage", 10f, Module("Weapon"));
        Assert.AreEqual(50f, F(data, "currentShieldHP"));
        Assert.AreEqual(20f, F(Call(data, "GetModule", Module("Weapon")), "currentHP"));
    }
    [TestCase("Laser", 30f, 100f, 1)]
    [TestCase("Plasma", 20f, 100f, 1)]
    [TestCase("Railgun", 38f, 92f, 2)]
    public void WeaponSpecialDamageAndEventMultiplicity(string kind, float shield, float hull, int events)
    {
        Call(health, "TakeWeaponDamage", 20f, Enum.Parse(T("WeaponKind"), kind), Module("Weapon"));
        Assert.AreEqual(shield, F(data, "currentShieldHP"), .0001f);
        Assert.AreEqual(hull, F(data, "currentHullHP"), .0001f);
        Assert.AreEqual(30f, F(Call(data, "GetModule", Module("Weapon")), "currentHP"));
        Assert.AreEqual(events, damageEvents);
    }
    [Test] public void HullZeroIsImmediateButDestructionNotificationWaitsForUpdateAndOccursOnce()
    {
        Call(data, "TakeHullDamage", 1000f);
        Assert.AreEqual(0f, F(data, "currentHullHP"));
        Assert.True((bool)data.GetType().GetProperty("IsDestroyed").GetValue(data));
        Assert.AreEqual(0, destroyedEvents);
        Assert.True(go.activeSelf);
        Call(health, "TakeDamage", 10f, Module("Engine"));
        Assert.AreEqual(0, damageEvents);
        Call(health, "Update");
        Assert.AreEqual(1, destroyedEvents);
        Assert.False(go.activeSelf);
        Call(health, "Update");
        Assert.AreEqual(1, destroyedEvents);
    }
    [Test] public void RandomModuleChoiceMatchesUnitySequence_ExplicitTargetConsumesNoRandom()
    {
        Set(data, "currentShieldHP", 0f);
        int seed = 0;
        for (; seed < 1000; seed++)
        {
            UnityEngine.Random.InitState(seed);
            if (UnityEngine.Random.value < .4f) break;
        }
        Assert.Less(seed, 1000);
        UnityEngine.Random.InitState(seed);
        float gate = UnityEngine.Random.value;
        int index = UnityEngine.Random.Range(0, 3);
        float next = UnityEngine.Random.value;
        UnityEngine.Random.InitState(seed);
        Call(health, "TakeDamage", 10f, null);
        Assert.AreEqual(20f + (index == 1 ? 10f : 0f), F(Call(data, "GetModule", Module(new[] { "Weapon", "Shield", "Engine" }[index])), "currentHP"));
        Assert.AreEqual(next, UnityEngine.Random.value);
        UnityEngine.Random.InitState(seed);
        Call(health, "TakeDamage", 1f, Module("Weapon"));
        Assert.AreEqual(gate, UnityEngine.Random.value);
    }
    [Test] public void HullAndModuleRepairIgnoreNonpositiveAmounts_EvasionIsNotClamped()
    {
        Call(data, "TakeHullDamage", -10f);
        Assert.AreEqual(100f, F(data, "currentHullHP"));
        Assert.AreEqual(1.1f, (float)Call(data, "GetTotalEvasion", 20f), .0001f);
        var module = Call(data, "GetModule", Module("Weapon"));
        Call(module, "TakeDamage", 10f); Call(module, "Repair", -1f);
        Assert.AreEqual(20f, F(module, "currentHP"));
        Call(module, "Repair", 100f);
        Assert.AreEqual(30f, F(module, "currentHP"));
    }
}