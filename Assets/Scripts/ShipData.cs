using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Full runtime state of a ship: hull integrity plus the health of every module.
/// </summary>
/// <remarks>
/// Plain C# class on purpose: MonoBehaviours (views, selection, movement) hold a
/// reference to <see cref="ShipData"/> instead of carrying combat state themselves.
/// </remarks>
public class ShipData
{
    /// <summary>Default hit points of the weapon module.</summary>
    public const float DefaultWeaponMaxHP = 30f;

    /// <summary>Default hit points of the shield module.</summary>
    public const float DefaultShieldMaxHP = 40f;

    /// <summary>Default hit points of the engine module.</summary>
    public const float DefaultEngineMaxHP = 30f;

    /// <summary>Maximum hull hit points of the ship.</summary>
    public float maxHullHP;

    /// <summary>Current hull hit points of the ship.</summary>
    public float currentHullHP;

    /// <summary>Module states keyed by <see cref="ModuleType"/>. Always contains all module types.</summary>
    public Dictionary<ModuleType, ShipModule> modules = new Dictionary<ModuleType, ShipModule>();

    /// <summary>Creates a ship with a full hull and all three modules at full health.</summary>
    /// <param name="maxHull">Maximum (and starting) hull hit points. Negative values are clamped to 0.</param>
    public ShipData(float maxHull)
    {
        maxHullHP = Mathf.Max(0f, maxHull);
        currentHullHP = maxHullHP;

        modules.Add(ModuleType.Weapon, new ShipModule(ModuleType.Weapon, DefaultWeaponMaxHP));
        modules.Add(ModuleType.Shield, new ShipModule(ModuleType.Shield, DefaultShieldMaxHP));
        modules.Add(ModuleType.Engine, new ShipModule(ModuleType.Engine, DefaultEngineMaxHP));
    }

    /// <summary>True when the hull has no hit points left.</summary>
    public bool IsDestroyed => currentHullHP <= 0f;

    /// <summary>Returns the module of the requested type, or null when it is not tracked.</summary>
    public ShipModule GetModule(ModuleType type)
    {
        ShipModule module;
        return modules.TryGetValue(type, out module) ? module : null;
    }

    /// <summary>Applies damage to the hull. Hull never drops below 0.</summary>
    /// <param name="amount">Damage amount. Non-positive values are ignored.</param>
    public void TakeHullDamage(float amount)
    {
        if (amount <= 0f)
            return;

        currentHullHP = Mathf.Max(0f, currentHullHP - amount);
    }

    /// <summary>Applies damage to a single module through <see cref="GetModule"/>.</summary>
    /// <param name="type">Module to damage.</param>
    /// <param name="amount">Damage amount. Non-positive values are ignored.</param>
    public void TakeModuleDamage(ModuleType type, float amount)
    {
        var module = GetModule(type);
        if (module != null)
            module.TakeDamage(amount);
    }
}
