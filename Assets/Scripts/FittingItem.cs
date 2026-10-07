using UnityEngine;

/// <summary>
/// Тип визуализации выстрела, зависит от установленного оружия.
/// </summary>
public enum WeaponVisualType
{
    /// <summary>Тонкий луч лазера.</summary>
    Laser,

    /// <summary>Широкий луч-капля (плазма).</summary>
    Plasma
}

/// <summary>
/// Один устанавливаемый модуль (оружие, щит, двигатель).
/// Хранит модификаторы характеристик корабля и визуальный тип.
/// </summary>
[System.Serializable]
public class FittingItem
{
    public string id;
    public string name;
    public SlotGroup allowedGroup;
    public Color iconColor = Color.cyan;
    public string description;

    // Модификаторы характеристик
    public float damageBonus;
    public float cooldownBonus;
    public float shieldBonus;
    public float speedBonus;
    public float accelBonus;
    public float massBonus;

    // Визуал выстрела (актуально только для оружия)
    public WeaponVisualType visualType = WeaponVisualType.Laser;

    public FittingItem(string id, string name, SlotGroup allowedGroup, Color color, string desc,
                       float damage = 0, float cooldown = 0, float shield = 0, float speed = 0, float accel = 0, float mass = 0,
                       WeaponVisualType visual = WeaponVisualType.Laser)
    {
        this.id = id;
        this.name = name;
        this.allowedGroup = allowedGroup;
        this.iconColor = color;
        this.description = desc;
        this.damageBonus = damage;
        this.cooldownBonus = cooldown;
        this.shieldBonus = shield;
        this.speedBonus = speed;
        this.accelBonus = accel;
        this.massBonus = mass;
        this.visualType = visual;
    }
}