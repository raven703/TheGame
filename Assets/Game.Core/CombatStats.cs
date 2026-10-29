using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum WeaponKind { Laser = 0, Plasma = 1, Railgun = 2 }
    public struct ShipStatValues
    {
        public float Hull, Shield, Mass, Speed, Thrust, Turn, LockTime;
        public float Energy, Regeneration, Cooling, Resistance;
        public float ReverseThrust, LateralThrust, AfterburnerHeatMultiplier;
        public static ShipStatValues Fallback => new ShipStatValues { Hull=100f, Shield=50f, Mass=2f, Speed=5f,
            Thrust=12f, Turn=300f, LockTime=.5f, Energy=100f, Regeneration=12f, Cooling=15f };
    }
    // Calculation-only values, not owned items or an inventory.
    public struct EquipmentStatBonus
    {
        public float Shield, Mass, Speed, Thrust, LockTime, Resistance;
        public float Energy, Regeneration, Cooling, ReverseThrust, LateralThrust;
        public float SpeedPercent, ThrustPercent, ReversePercent, LateralPercent, AfterburnerHeatPercent;
    }
    public static class StatMath
    {
        public static float Clamp(float value,float min,float max) => value < min ? min : value > max ? max : value;
        public static float EffectiveResistanceBonus(string definitionId,float savedBonus) => definitionId == "e2" ? .1f : savedBonus;
        public static ShipStatValues Calculate(ShipStatValues basis,IEnumerable<EquipmentStatBonus> installed)
        {
            var result=basis;
            float speedPercent=0f, thrustPercent=0f, reversePercent=0f, lateralPercent=0f, boostHeat=0f;
            result.ReverseThrust=basis.ReverseThrust>0f?basis.ReverseThrust:basis.Thrust*.5f;
            result.LateralThrust=basis.LateralThrust>0f?basis.LateralThrust:basis.Thrust*.35f;
            if(installed!=null)foreach(var bonus in installed)
            {
                result.Shield+=bonus.Shield;result.Mass+=bonus.Mass;result.Speed+=bonus.Speed;
                result.Thrust+=bonus.Thrust;result.LockTime+=bonus.LockTime;result.Resistance+=bonus.Resistance;
                result.Energy+=bonus.Energy;result.Regeneration+=bonus.Regeneration;result.Cooling+=bonus.Cooling;
                // Preserve legacy additive engine effects on all axes; new forward percentages do not leak.
                result.ReverseThrust+=bonus.ReverseThrust+bonus.Thrust*.5f;
                result.LateralThrust+=bonus.LateralThrust+bonus.Thrust*.35f;
                speedPercent+=bonus.SpeedPercent;thrustPercent+=bonus.ThrustPercent;
                reversePercent+=bonus.ReversePercent;lateralPercent+=bonus.LateralPercent;
                boostHeat+=bonus.AfterburnerHeatPercent;
            }
            result.Speed*=1f+Math.Min(.30f,speedPercent);
            result.Thrust*=1f+Math.Min(.20f,thrustPercent);
            result.ReverseThrust*=1f+Math.Min(.60f,reversePercent);
            result.LateralThrust*=1f+Math.Min(.60f,lateralPercent);
            result.AfterburnerHeatMultiplier=1f+boostHeat;
            result.Mass=Math.Max(.5f,result.Mass);result.LockTime=Math.Max(.05f,result.LockTime);
            result.Shield=Math.Max(0f,result.Shield);result.Resistance=Clamp(result.Resistance,0f,.75f);
            result.Energy=Math.Max(0f,result.Energy);result.Regeneration=Math.Max(0f,result.Regeneration);result.Cooling=Math.Max(0f,result.Cooling);
            return result;
        }
    }
    public struct WeaponStatValues
    {
        public WeaponKind Kind;
        public float Damage, Cooldown, Energy, Heat;
        public float NominalDps => Damage/Cooldown;
    }
    public static class WeaponStatRules
    {
        public static WeaponKind EffectiveKind(string id,WeaponKind savedKind,bool plasmaVisual) => savedKind!=WeaponKind.Laser ? savedKind :
            id=="w3" ? WeaponKind.Railgun : id=="w2" || plasmaVisual ? WeaponKind.Plasma : WeaponKind.Laser;
        public static WeaponStatValues Calculate(WeaponKind kind,float damage,float cooldownBonus,float laserCooldown)
        {
            return new WeaponStatValues { Kind=kind,Damage=damage,
                Cooldown=Math.Max(.1f,(kind==WeaponKind.Plasma?1.2f:kind==WeaponKind.Railgun?1.8f:laserCooldown)+cooldownBonus),
                Energy=kind==WeaponKind.Plasma?16f:kind==WeaponKind.Railgun?22f:7f,
                Heat=kind==WeaponKind.Plasma?22f:kind==WeaponKind.Railgun?30f:9f };
        }
    }
}