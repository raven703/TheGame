using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Полное динамическое состояние корабля: прочность корпуса, щиты, точность, масса и состояние модулей.
/// </summary>
public class ShipData
{
    public const float DefaultWeaponMaxHP = 30f;
    public const float DefaultShieldMaxHP = 40f;
    public const float DefaultEngineMaxHP = 30f;

    // Прочность корпуса
    public float maxHullHP { get => Damage.MaxHull; set => Damage.SetMaxHull(value); }
    public float currentHullHP { get => Damage.Hull; set => Damage.SetHull(value); }

    // Параметры физики и инерции
    public float mass = 2f;             // Масса корабля (влияет на инерцию)
    public float enginePower = 15f;     // Тяга маршевых двигателей

    // Параметры для математики боя
    public float accuracy = 0.85f;      // Базовая точность (85%)
    public float baseEvasion = 0.10f;   // Базовое уклонение неподвижного корабля (10%)
    public float speedEvasionMultiplier = 0.05f; // Бонус уклонения за каждый unit/sec скорости

    // Динамический щит
    public float maxShieldHP { get => Damage.MaxShield; set => Damage.SetMaxShield(value); }
    public float currentShieldHP { get => Damage.Shield; set => Damage.SetShield(value); }

    // Захват цели: время в секундах (база + бонусы модулей)
    public float lockTime = 0.5f;

    // Состояния повреждений отдельных узлов
    public Game.Core.DamageState Damage { get; }
    private readonly Dictionary<ModuleType, ShipModule> moduleViews = new Dictionary<ModuleType, ShipModule>();
    public IReadOnlyDictionary<ModuleType, ShipModule> modules => new System.Collections.ObjectModel.ReadOnlyDictionary<ModuleType, ShipModule>(moduleViews);

    public ShipData(float maxHull)
    {
        Damage = new Game.Core.DamageState(maxHull);
        foreach (ModuleType type in System.Enum.GetValues(typeof(ModuleType)))
            moduleViews[type] = new ShipModule(type, Damage);
    }
    public bool IsDestroyed => Damage.Destroyed;
    public float GetTotalEvasion(float currentSpeed) => Damage.Evasion(currentSpeed, baseEvasion, speedEvasionMultiplier);
    public ShipModule GetModule(ModuleType type) => moduleViews.TryGetValue(type, out var module) ? module : null;
    public float AbsorbDamageWithShield(float damage) => Damage.Absorb(damage);
    public void TakeHullDamage(float amount) => Damage.HullDamage(amount);
    public void TakeModuleDamage(ModuleType type, float amount) { if (GetModule(type)!=null) Damage.ModuleDamage((Game.Core.CombatModule)(int)type, amount); }
    public void ApplyMaximums(float hull, float shield) => Damage.ConfigureMaximums(hull, shield);
}
