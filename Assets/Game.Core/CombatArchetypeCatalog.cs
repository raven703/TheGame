using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Versioned standard balance data. Does not own or grant equipment instances.</summary>
    public static class CombatArchetypeCatalog
    {
        public const int Version = 3;
        public static bool TryRange(string id,out float range)
        {
            switch(id)
            {
                case "w1":case PulseLaser:range=12;return true;
                case Plasma:case LaserTurret:range=10;return true;
                case Railgun:range=18;return true;
                default:range=0;return false;
            }
        }
        public const string PulseLaser = "w4", Plasma = "w2", Railgun = "w3";
        public const string LaserTurret = "combat-v1:laser-turret";
        public const string LightweightEngine = "combat-v1:lightweight-engine";
        public const string ManeuverBlock = "combat-v1:maneuver-block";
        public const string LightShield="s1";
        public const string HeavyShield = "s2", Radiator = "combat-v1:radiator", Capacitor = "combat-v1:capacitor";

        private static readonly IReadOnlyDictionary<string, WeaponStatValues> weapons =
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, WeaponStatValues>(new Dictionary<string, WeaponStatValues> {
                { PulseLaser, Weapon(WeaponKind.Laser,10f,.7f,6f,7f,12) },
                { Plasma, Weapon(WeaponKind.Plasma,22f,1.4f,14f,20f,10) },
                { Railgun, Weapon(WeaponKind.Railgun,26f,2f,20f,25f,18) },
                { LaserTurret, Weapon(WeaponKind.Laser,7f,.8f,7f,8f,10) }
            });
        private static readonly IReadOnlyDictionary<string, EquipmentStatBonus> modules =
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, EquipmentStatBonus>(new Dictionary<string, EquipmentStatBonus> {
                { LightweightEngine,new EquipmentStatBonus {SpeedPercent=.15f,ThrustPercent=.10f,AfterburnerHeatPercent=.25f} },
                { ManeuverBlock,new EquipmentStatBonus {ReversePercent=.30f,LateralPercent=.30f,Mass=.3f} },
                { LightShield,new EquipmentStatBonus {Shield=25f,ShieldRegenPerSecond=5f} },
                { HeavyShield,new EquipmentStatBonus {Shield=40f,ShieldRegenPerSecond=2f,Mass=.6f} },
                { Radiator,new EquipmentStatBonus {Cooling=6f,Mass=.4f} },
                { Capacitor,new EquipmentStatBonus {Energy=35f,Mass=.4f} }
            });
        private static WeaponStatValues Weapon(WeaponKind kind,float damage,float interval,float energy,float heat,float range) =>
            new WeaponStatValues {Kind=kind,Damage=damage,Cooldown=interval,Energy=energy,Heat=heat,Range=range};
        public static bool TryWeapon(string id,out WeaponStatValues value)
        { value=default;return id!=null&&weapons.TryGetValue(id,out value); }
        public static bool TryModule(string id,out EquipmentStatBonus value)
        { value=default;return id!=null&&modules.TryGetValue(id,out value); }
    }

    public static class DirectedThrust
    {
        /// <summary>Local requested force; longitudinal and lateral axes share one elliptic effort.</summary>
        public static AimPoint Limit(float longitudinal,float lateral,float forward,float reverse,float side)
        {
            float a=Math.Max(0f,longitudinal>=0f?forward:reverse), b=Math.Max(0f,side);
            float x=a>0f?longitudinal/a:0f, y=b>0f?lateral/b:0f;
            float scale=Math.Max(1f,(float)Math.Sqrt(x*x+y*y));
            return new AimPoint(x/scale*a,y/scale*b);
        }
    }
}