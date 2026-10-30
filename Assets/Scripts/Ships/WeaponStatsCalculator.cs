using UnityEngine;

public struct WeaponStats
{
    public WeaponKind kind;
    public float damage, cooldown, energy, heat, range;
    public float NominalDps => damage / cooldown;
}

public static class WeaponStatsCalculator
{
    public static WeaponStats Calculate(FittingItem item, float laserCooldown, float fallbackDamage = .5f,float fallbackRange=6f)
    {
        var kind = item != null ? item.EffectiveWeaponKind : WeaponKind.Laser;
        if(item!=null && item.explicitWeaponStats)
            return new WeaponStats {kind=kind,damage=item.damageBonus,cooldown=Mathf.Max(.1f,item.weaponInterval),
                energy=Mathf.Max(0f,item.weaponEnergy),heat=Mathf.Max(0f,item.weaponHeat),range=Game.Core.WeaponStatRules.EffectiveRange(item.weaponRange,fallbackRange)};
        var s=Game.Core.WeaponStatRules.Calculate((Game.Core.WeaponKind)(int)kind,item!=null?item.damageBonus:fallbackDamage,
            item!=null?item.cooldownBonus:0f,laserCooldown);
        s.Range=Game.Core.WeaponStatRules.EffectiveRange(item!=null?item.weaponRange:0,fallbackRange);
        return new WeaponStats { kind=(WeaponKind)(int)s.Kind,damage=s.Damage,cooldown=s.Cooldown,energy=s.Energy,heat=s.Heat,range=s.Range };
    }
}