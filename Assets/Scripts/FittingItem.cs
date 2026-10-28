using UnityEngine;

/// <summary>
/// Тип визуализации выстрела, зависит от установленного оружия.
/// </summary>
public enum WeaponMountType { Fixed, Turret }

public enum WeaponKind { Laser, Plasma, Railgun }

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
    public WeaponMountType mountType = WeaponMountType.Fixed;
    public float fixedFireSector = 60f;
    public float FixedFireSector => fixedFireSector > 0f ? Mathf.Clamp(fixedFireSector, 0f, 360f) : 60f;
    public float trackingSpeed = 90f;
    public float aimTolerance = 3f;
    public WeaponKind weaponKind = WeaponKind.Laser;
    public WeaponKind EffectiveWeaponKind => (WeaponKind)(int)Game.Core.WeaponStatRules.EffectiveKind(id,
        (Game.Core.WeaponKind)(int)weaponKind,visualType==WeaponVisualType.Plasma);
    public string id;
    // Definition identity remains id; each owned physical copy has its own identity.
    public string instanceId;
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
    public float heatResistanceBonus;
    // Legacy saves contain item snapshots. e2 retains its identity and quantity.
    public float EffectiveHeatResistanceBonus => Game.Core.StatMath.EffectiveResistanceBonus(id,heatResistanceBonus);
    public string EffectiveDescription => id == "e2"
        ? "Тяга +10, скорость +1, масса +0.5; устойчивость к перегреву +10 п.п."
        : description;

    [Tooltip("Бонус к времени захвата цели. Отрицательный = быстрее захват.")]
    public float lockTimeBonus;

    // Визуал выстрела (актуально только для оружия)
    public WeaponVisualType visualType = WeaponVisualType.Laser;

    public FittingItem(string id, string name, SlotGroup allowedGroup, Color color, string desc,
                       float damage = 0, float cooldown = 0, float shield = 0, float speed = 0, float accel = 0, float mass = 0,
                       float lockTime = 0,
                       WeaponVisualType visual = WeaponVisualType.Laser)
    {
        weaponKind = id == "w2" ? WeaponKind.Plasma : id == "w3" ? WeaponKind.Railgun : WeaponKind.Laser;
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
        this.lockTimeBonus = lockTime;
        this.visualType = visual;
    }
}