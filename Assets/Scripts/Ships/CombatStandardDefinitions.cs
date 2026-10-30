using System;
using Game.Core;
using UnityEngine;

/// <summary>Serialization adapter for the closed, versioned Core standard catalog. Never grants stock.</summary>
public static class CombatStandardDefinitions
{
    // Hull assets have no saved equipment snapshot. Version the narrow runtime
    // standard update separately so custom hulls and weapon/module balance stay intact.
    public const int FlightVersion=2;
    public static bool UpgradeFlight(ShipDefinition definition)
    {
        if(definition==null||definition.flightVersion>=FlightVersion)return false;
        if(definition.shipId=="sidewinder"){definition.baseMaxSpeed=10f;definition.baseAcceleration=19f;}
        else if(definition.shipId=="cobra_mk3"){definition.baseMaxSpeed=8f;definition.baseAcceleration=28.5f;}
        else return false;
        definition.flightVersion=FlightVersion;return true;
    }
    public static readonly string[] StarterIds = {
        CombatArchetypeCatalog.LightweightEngine, CombatArchetypeCatalog.ManeuverBlock,
        CombatArchetypeCatalog.HeavyShield, CombatArchetypeCatalog.Radiator, CombatArchetypeCatalog.Capacitor,
        CombatArchetypeCatalog.PulseLaser, CombatArchetypeCatalog.Plasma, CombatArchetypeCatalog.Railgun, CombatArchetypeCatalog.LaserTurret
    };
    public static bool EnsureStarterSet(ShipLoadoutSaveFile data)
    {
        if(data.combatStarterVersion>=1)return false;
        var owned=new System.Collections.Generic.HashSet<string>();
        foreach(var item in PlayerEquipment.Items(data))
        {
            var expected=CombatArchetypeCatalog.TryWeapon(item.id,out _)?SlotGroup.Top_Weapons:
                IsShield(item.id)?SlotGroup.Center_Shields:SlotGroup.Bottom_Engines;
            if(item.allowedGroup==expected)owned.Add(item.id);
        }
        foreach(string id in StarterIds)if(!owned.Contains(id))data.inventory.Add(Create(id,Guid.NewGuid().ToString("N")));
        data.combatStarterVersion=1;
        return true;
    }
    public static bool Upgrade(FittingItem item)
    {
        if(item==null || item.balanceVersion>=CombatArchetypeCatalog.Version)return false;
        bool weapon=CombatArchetypeCatalog.TryWeapon(item.id,out var w);
        bool module=CombatArchetypeCatalog.TryModule(item.id,out var m);
        if(!weapon&&!module)return false;
        if(weapon && item.allowedGroup!=SlotGroup.Top_Weapons || module &&
            item.allowedGroup!=(IsShield(item.id)?SlotGroup.Center_Shields:SlotGroup.Bottom_Engines))return false;
        // Version 2 changes only shield capacity/regeneration. Do not reapply old balance to v1 items.
        if(item.balanceVersion>=1)
        {
            if(IsShield(item.id)){item.shieldBonus=m.Shield;item.shieldRegenPerSecond=m.ShieldRegenPerSecond;}
            item.balanceVersion=CombatArchetypeCatalog.Version;return true;
        }
        if(weapon)
        {
            item.explicitWeaponStats=true;item.weaponKind=(global::WeaponKind)(int)w.Kind;
            item.damageBonus=w.Damage;item.cooldownBonus=0f;
            item.weaponInterval=w.Cooldown;item.weaponEnergy=w.Energy;item.weaponHeat=w.Heat;
            item.fixedFireSector=60f;
            item.mountType=item.id==CombatArchetypeCatalog.LaserTurret?global::WeaponMountType.Turret:global::WeaponMountType.Fixed;
            if(item.mountType==global::WeaponMountType.Turret)item.trackingSpeed=90f;
        }
        else
        {
            // Only the balance-owned fields of the five listed standards are refreshed.
            item.shieldBonus=m.Shield;item.shieldRegenPerSecond=m.ShieldRegenPerSecond;
            if(item.id==CombatArchetypeCatalog.LightShield){item.balanceVersion=CombatArchetypeCatalog.Version;return true;}
            item.massBonus=m.Mass;item.speedBonus=m.Speed;item.accelBonus=m.Thrust;
            item.energyBonus=m.Energy;item.regenerationBonus=m.Regeneration;item.coolingBonus=m.Cooling;
            item.speedPercent=m.SpeedPercent;item.thrustPercent=m.ThrustPercent;
            item.reversePercent=m.ReversePercent;item.lateralPercent=m.LateralPercent;
            item.afterburnerHeatPercent=m.AfterburnerHeatPercent;
        }
        item.balanceVersion=CombatArchetypeCatalog.Version;
        return true;
    }
    public static FittingItem Create(string id,string instanceId)
    {
        bool weapon=CombatArchetypeCatalog.TryWeapon(id,out _);
        if(!weapon&&!CombatArchetypeCatalog.TryModule(id,out _))throw new ArgumentException("Unknown standard definition",nameof(id));
        var group=weapon?SlotGroup.Top_Weapons:IsShield(id)?SlotGroup.Center_Shields:SlotGroup.Bottom_Engines;
        var item=new FittingItem(id,Name(id),group,weapon?Color.red:Color.cyan,Description(id)){instanceId=instanceId};
        Upgrade(item);return item;
    }
    private static string Name(string id)
    {
        switch(id)
        {
            case CombatArchetypeCatalog.PulseLaser:return "Импульсный лазер";
            case CombatArchetypeCatalog.Plasma:return "Плазменная пушка";
            case CombatArchetypeCatalog.Railgun:return "Рейлган";
            case CombatArchetypeCatalog.LaserTurret:return "Лазерная турель";
            case CombatArchetypeCatalog.LightweightEngine:return "Облегчённый двигатель";
            case CombatArchetypeCatalog.ManeuverBlock:return "Маневровый блок";
            case CombatArchetypeCatalog.LightShield:return "Лёгкий щит";
            case CombatArchetypeCatalog.HeavyShield:return "Тяжёлый щит";
            case CombatArchetypeCatalog.Radiator:return "Радиатор";
            case CombatArchetypeCatalog.Capacitor:return "Конденсатор";
            default:throw new ArgumentException(nameof(id));
        }
    }
    public static string Description(string id)
    {
        if(CombatArchetypeCatalog.TryWeapon(id,out var w))return $"Урон {w.Damage}; интервал {w.Cooldown:0.0} с; энергия {w.Energy}; нагрев {w.Heat}.";
        CombatArchetypeCatalog.TryModule(id,out var m);
        if(id==CombatArchetypeCatalog.LightweightEngine)return $"Скорость +{m.SpeedPercent:P0}; маршевая тяга +{m.ThrustPercent:P0}; нагрев форсажа +{m.AfterburnerHeatPercent:P0}.";
        if(id==CombatArchetypeCatalog.ManeuverBlock)return $"Обратная/боковая тяга +{m.ReversePercent:P0}/+{m.LateralPercent:P0}; масса +{m.Mass:0.0}.";
        if(IsShield(id))return $"Щит +{m.Shield}; реген +{m.ShieldRegenPerSecond}/с; масса +{m.Mass:0.0}.";
        if(id==CombatArchetypeCatalog.Radiator)return $"Охлаждение +{m.Cooling}/с; масса +{m.Mass:0.0}.";
        return $"Энергия +{m.Energy}; масса +{m.Mass:0.0}.";
    }
    private static bool IsShield(string id) => id==CombatArchetypeCatalog.LightShield || id==CombatArchetypeCatalog.HeavyShield;
}