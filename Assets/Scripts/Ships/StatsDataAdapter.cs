using System.Collections.Generic;
using Game.Core;

/// <summary>Saved equipment parameters only, never free stock or catalog replacements.</summary>
public static class StatsDataAdapter
{
    public static ShipStatValues Basis(ShipDefinition def) => def==null ? ShipStatValues.Fallback : new ShipStatValues {
        Hull=def.maxHullHP,Shield=def.baseShieldHP,Mass=def.baseMass,Speed=def.baseMaxSpeed,Thrust=def.baseAcceleration,
        Turn=def.baseRotationSpeed,LockTime=def.baseLockTime,Energy=def.baseMaxEnergy,Regeneration=def.baseEnergyRegeneration,
        Cooling=def.baseCooling,Resistance=def.baseHeatResistance,ReverseThrust=def.baseReverseThrust,LateralThrust=def.baseLateralThrust };
    public static List<EquipmentStatBonus> Installed(ShipLoadout loadout)
    {
        var result=new List<EquipmentStatBonus>();
        if(loadout!=null)foreach(var slots in new[]{loadout.weaponSlots,loadout.shieldSlots,loadout.engineSlots})
            if(slots!=null)foreach(var item in slots)if(item!=null&&!string.IsNullOrEmpty(item.id))
                result.Add(new EquipmentStatBonus { Shield=item.shieldBonus,Mass=item.massBonus,Speed=item.speedBonus,
                    Thrust=item.accelBonus,LockTime=item.lockTimeBonus,Resistance=item.EffectiveHeatResistanceBonus,
                    Energy=item.energyBonus,Regeneration=item.regenerationBonus,Cooling=item.coolingBonus,
                    SpeedPercent=item.speedPercent,ThrustPercent=item.thrustPercent,ReversePercent=item.reversePercent,
                    LateralPercent=item.lateralPercent,AfterburnerHeatPercent=item.afterburnerHeatPercent });
        return result;
    }
}