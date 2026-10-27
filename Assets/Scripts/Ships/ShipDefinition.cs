using UnityEngine;

/// <summary>
/// ScriptableObject: паспорт типа корабля. Базовые ТТХ и количество слотов.
/// Один ассет на тип (CobraMK3, Sidewinder, Python...).
/// </summary>
[CreateAssetMenu(fileName = "ShipDefinition", menuName = "Space/Ship Definition")]
public class ShipDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Уникальный ключ для сохранения. Не менять после релиза!")]
    public string shipId = "cobra_mk3";

    [Header("Default Loadout")]
    [Tooltip("Стартовые модули в инвентаре, если у игрока ещё нет сохранения для этого корабля.")]
    public System.Collections.Generic.List<FittingItem> defaultInventory = new System.Collections.Generic.List<FittingItem>();

    [Header("Visuals")]
    [Tooltip("Спрайт корабля в бою (не иконка).")]
    public Sprite hullSprite;

    [Tooltip("Отображаемое имя в UI.")]
    public string displayName = "Cobra MK3";

    [Tooltip("Иконка корабля (для UI).")]
    public Sprite icon;

    [Header("Base Stats")]
    public float maxHullHP = 120f;
    public float baseShieldHP = 60f;
    public float baseMass = 3f;

    public float baseMaxSpeed = 6f;
    public float baseAcceleration = 14f;
    [Tooltip("Reverse force; zero migrates to 50% of forward force.")]
    public float baseReverseThrust;
    [Tooltip("Lateral force; zero migrates to 35% of forward force.")]
    public float baseLateralThrust;
    public float baseRotationSpeed = 300f;
    public float baseMaxEnergy = 100f;
    public float baseEnergyRegeneration = 12f;
    public float baseCooling = 15f;
    [Range(0f, .75f)] public float baseHeatResistance;

    [Tooltip("Базовое время захвата цели (сек). Меньше — быстрее.")]
    public float baseLockTime = 0.5f;

    [Header("Slots")]
    public int weaponSlots = 4;
    public int shieldSlots = 4;
    public int engineSlots = 4;
}