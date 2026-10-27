using UnityEngine;

public struct WeaponStats
{
    public WeaponKind kind;
    public float damage, cooldown, energy, heat;
    public float NominalDps => damage / cooldown;
}

public static class WeaponStatsCalculator
{
    public static WeaponStats Calculate(FittingItem item, float laserCooldown, float fallbackDamage = .5f)
    {
        var kind = item != null ? item.EffectiveWeaponKind : WeaponKind.Laser;
        return new WeaponStats {
            kind = kind, damage = item != null ? item.damageBonus : fallbackDamage,
            cooldown = Mathf.Max(.1f, (kind == WeaponKind.Plasma ? 1.2f : kind == WeaponKind.Railgun ? 1.8f : laserCooldown) + (item != null ? item.cooldownBonus : 0f)),
            energy = kind == WeaponKind.Plasma ? 16f : kind == WeaponKind.Railgun ? 22f : 7f,
            heat = kind == WeaponKind.Plasma ? 22f : kind == WeaponKind.Railgun ? 30f : 9f
        };
    }
}