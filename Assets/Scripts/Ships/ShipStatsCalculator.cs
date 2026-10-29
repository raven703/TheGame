using System.Collections.Generic;
using UnityEngine;

/// <summary>Equipment-only snapshot. Never reads temporary combat multipliers.</summary>
public sealed class ShipStats
{
    public float hull, shield, mass, speed, thrust, turn, lockTime;
    public float energy, regeneration, cooling, resistance;
    public float reverseThrust, lateralThrust, afterburnerHeatMultiplier;
}

public static class ShipStatsCalculator
{
    public static ShipStats Calculate(ShipDefinition definition, ShipLoadout loadout)
    {
        var s = Game.Core.StatMath.Calculate(StatsDataAdapter.Basis(definition), StatsDataAdapter.Installed(loadout));
        return new ShipStats { hull=s.Hull,shield=s.Shield,mass=s.Mass,speed=s.Speed,thrust=s.Thrust,turn=s.Turn,
            lockTime=s.LockTime,energy=s.Energy,regeneration=s.Regeneration,cooling=s.Cooling,resistance=s.Resistance,
            reverseThrust=s.ReverseThrust,lateralThrust=s.LateralThrust,afterburnerHeatMultiplier=s.AfterburnerHeatMultiplier };
    }
    public static void Apply(ShipHealth health, ShipDefinition definition, ShipLoadout loadout)
    {
        if (health == null || health.Data == null || !CombatSession.IsPreparing) return;
        ApplyPrepared(health, definition, loadout);
    }
    // Runtime NPC installation: validates detached ownership and never touches player storage.
    public static void ApplyNpc(ShipHealth health, ShipDefinition definition, ShipLoadout loadout)
    {
        if(health==null||health.Data==null||!WeaponMountRules.Validate(definition,loadout))
            throw new System.InvalidOperationException("Invalid NPC installation");
        health.shipDefinition=definition;
        ApplyPrepared(health,definition,loadout,false);
    }
#if UNITY_EDITOR
    internal static void ApplyExperiment(ShipHealth health, ShipDefinition definition, ShipLoadout loadout)
    {
        if (health == null || health.Data == null) throw new System.InvalidOperationException("Experiment ship is not initialized");
        ApplyPrepared(health, definition, loadout, false);
    }
#endif
    private static void ApplyPrepared(ShipHealth health, ShipDefinition definition, ShipLoadout loadout, bool migrate = true)
    {
        if(migrate && loadout != null && definition != null)
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
            movement.reverseThrustRatio = s.reverseThrust / Mathf.Max(.01f,s.thrust);
            movement.lateralThrustRatio = s.lateralThrust / Mathf.Max(.01f,s.thrust);
            movement.ApplyMassFromData();
        }
        var reactor = health.GetComponent<ShipReactor>();
        var abilities=health.GetComponent<ShipAbilities>();
        if(abilities!=null)abilities.AfterburnerHeatMultiplier=s.afterburnerHeatMultiplier;
        if (reactor != null) reactor.Configure(s.energy, s.regeneration, s.cooling, s.resistance);
        var weapon = health.GetComponent<ShipWeapon>();
        if (weapon != null) weapon.SetEquippedWeapons(loadout != null && loadout.weaponSlots != null
            ? new List<FittingItem>(loadout.weaponSlots) : new List<FittingItem>());
    }
}