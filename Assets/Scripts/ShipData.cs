using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Full runtime state of a ship: hull integrity, shield, hit chance stats, mass, plus module states.
/// </summary>
public class ShipData
{
    public const float DefaultWeaponMaxHP = 30f;
    public const float DefaultShieldMaxHP = 40f;
    public const float DefaultEngineMaxHP = 30f;

    public float maxHullHP;
    public float currentHullHP;

    // Параметры физики и инерции
    public float mass = 2f;             // Масса корабля (влияет на инерцию)
    public float enginePower = 15f;     // Тяга маршевых двигателей

    // Параметры для математики боя
    public float accuracy = 0.85f;      // Базовая точность (85%)
    public float baseEvasion = 0.10f;   // Базовое уклонение неподвижного корабля (10%)
    public float speedEvasionMultiplier = 0.05f; // Бонус уклонения за каждый unit/sec скорости

    // Динамический щит
    public float maxShieldHP = 50f;
    public float currentShieldHP = 50f;

    public Dictionary<ModuleType, ShipModule> modules = new Dictionary<ModuleType, ShipModule>();

    public ShipData(float maxHull)
    {
        maxHullHP = Mathf.Max(0f, maxHull);
        currentHullHP = maxHullHP;

        modules.Add(ModuleType.Weapon, new ShipModule(ModuleType.Weapon, DefaultWeaponMaxHP));
        modules.Add(ModuleType.Shield, new ShipModule(ModuleType.Shield, DefaultShieldMaxHP));
        modules.Add(ModuleType.Engine, new ShipModule(ModuleType.Engine, DefaultEngineMaxHP));
    }

    public bool IsDestroyed => currentHullHP <= 0f;

    /// <summary>
    /// Вычисляет итоговое уклонение с учётом текущей физической скорости.
    /// </summary>
    public float GetTotalEvasion(float currentSpeed)
    {
        var engineMod = GetModule(ModuleType.Engine);
        if (engineMod != null && engineMod.isDestroyed)
            return 0f; // С выбитым двигателем уклонение падаёт до нуля

        return baseEvasion + (currentSpeed * speedEvasionMultiplier);
    }

    public ShipModule GetModule(ModuleType type)
    {
        ShipModule module;
        return modules.TryGetValue(type, out module) ? module : null;
    }

    public float AbsorbDamageWithShield(float damage)
    {
        var shieldMod = GetModule(ModuleType.Shield);

        if (shieldMod != null && shieldMod.isDestroyed)
            return damage;

        if (currentShieldHP <= 0f)
            return damage;

        if (currentShieldHP >= damage)
        {
            currentShieldHP -= damage;
            return 0f;
        }

        float excessDamage = damage - currentShieldHP;
        currentShieldHP = 0f;
        return excessDamage;
    }

    public void TakeHullDamage(float amount)
    {
        if (amount <= 0f)
            return;

        currentHullHP = Mathf.Max(0f, currentHullHP - amount);
    }

    public void TakeModuleDamage(ModuleType type, float amount)
    {
        var module = GetModule(type);
        if (module != null)
            module.TakeDamage(amount);
    }
}