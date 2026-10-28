using System.Collections.Generic;
using UnityEngine;

/// <summary>Equipment-only snapshot. Never reads temporary combat multipliers.</summary>
public sealed class ShipStats
{
    public float hull, shield, mass, speed, thrust, turn, lockTime;
    public float energy, regeneration, cooling, resistance;
}

public static class ShipStatsCalculator
{
    public static ShipStats Calculate(ShipDefinition definition, ShipLoadout loadout)
    {
        var s = Game.Core.StatMath.Calculate(StatsDataAdapter.Basis(definition), StatsDataAdapter.Installed(loadout));
        return new ShipStats { hull=s.Hull,shield=s.Shield,mass=s.Mass,speed=s.Speed,thrust=s.Thrust,turn=s.Turn,
            lockTime=s.LockTime,energy=s.Energy,regeneration=s.Regeneration,cooling=s.Cooling,resistance=s.Resistance };
    }
    public static void Apply(ShipHealth health, ShipDefinition definition, ShipLoadout loadout)
    {
        if (health == null || health.Data == null || !CombatSession.IsPreparing) return;
        if(loadout != null && definition != null)
        {
            int moved=WeaponMountRules.Migrate(definition,loadout);
            if(moved>0){CombatSession.Log($"Несовместимые модули перенесены в инвентарь: {moved}");ShipLoadoutManager.SaveLoadout(loadout);}
        }
        var s = Calculate(definition, loadout);
        var data = health.Data;
        data.ApplyMaximums(s.hull, s.shield);
        data.mass = s.mass; data.lockTime = s.lockTime;
        var movement = health.GetComponent<ShipMovement>();
        if (movement != null)
        {
            movement.maxSpeed = s.speed; movement.acceleration = s.thrust; movement.rotationSpeed = s.turn;
            movement.reverseThrustRatio = definition != null && definition.baseReverseThrust > 0f ? definition.baseReverseThrust / Mathf.Max(.01f, definition.baseAcceleration) : .5f;
            movement.lateralThrustRatio = definition != null && definition.baseLateralThrust > 0f ? definition.baseLateralThrust / Mathf.Max(.01f, definition.baseAcceleration) : .35f;
            movement.ApplyMassFromData();
        }
        var reactor = health.GetComponent<ShipReactor>();
        if (reactor != null) reactor.Configure(s.energy, s.regeneration, s.cooling, s.resistance);
        var weapon = health.GetComponent<ShipWeapon>();
        if (weapon != null) weapon.SetEquippedWeapons(loadout != null && loadout.weaponSlots != null
            ? new List<FittingItem>(loadout.weaponSlots) : new List<FittingItem>());
    }
}