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
        var s=Game.Core.WeaponStatRules.Calculate((Game.Core.WeaponKind)(int)kind,item!=null?item.damageBonus:fallbackDamage,
            item!=null?item.cooldownBonus:0f,laserCooldown);
        return new WeaponStats { kind=(WeaponKind)(int)s.Kind,damage=s.Damage,cooldown=s.Cooldown,energy=s.Energy,heat=s.Heat };
    }
}