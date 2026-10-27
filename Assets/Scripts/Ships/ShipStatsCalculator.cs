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
        var s = new ShipStats {
            hull = definition != null ? definition.maxHullHP : 100f,
            shield = definition != null ? definition.baseShieldHP : 50f,
            mass = definition != null ? definition.baseMass : 2f,
            speed = definition != null ? definition.baseMaxSpeed : 5f,
            thrust = definition != null ? definition.baseAcceleration : 12f,
            turn = definition != null ? definition.baseRotationSpeed : 300f,
            lockTime = definition != null ? definition.baseLockTime : .5f,
            energy = definition != null ? definition.baseMaxEnergy : 100f,
            regeneration = definition != null ? definition.baseEnergyRegeneration : 12f,
            cooling = definition != null ? definition.baseCooling : 15f,
            resistance = definition != null ? definition.baseHeatResistance : 0f
        };
        if (loadout != null)
        {
            Add(s, loadout.weaponSlots); Add(s, loadout.shieldSlots); Add(s, loadout.engineSlots);
        }
        s.mass = Mathf.Max(.5f, s.mass);
        s.lockTime = Mathf.Max(.05f, s.lockTime);
        s.shield = Mathf.Max(0f, s.shield);
        s.resistance = Mathf.Clamp(s.resistance, 0f, .75f);
        s.energy = Mathf.Max(0f, s.energy);
        s.regeneration = Mathf.Max(0f, s.regeneration);
        s.cooling = Mathf.Max(0f, s.cooling);
        return s;
    }

    private static void Add(ShipStats s, List<FittingItem> items)
    {
        if (items == null) return;
        foreach (var item in items)
        {
            if (item == null || string.IsNullOrEmpty(item.id)) continue;
            s.shield += item.shieldBonus; s.mass += item.massBonus;
            s.speed += item.speedBonus; s.thrust += item.accelBonus;
            s.lockTime += item.lockTimeBonus; s.resistance += item.EffectiveHeatResistanceBonus;
        }
    }

    public static void Apply(ShipHealth health, ShipDefinition definition, ShipLoadout loadout)
    {
        if (health == null || health.Data == null || !CombatSession.IsPreparing) return;
        var s = Calculate(definition, loadout);
        var data = health.Data;
        data.currentHullHP = Mathf.Clamp(data.currentHullHP + s.hull - data.maxHullHP, 0f, s.hull);
        data.currentShieldHP = Mathf.Clamp(data.currentShieldHP + s.shield - data.maxShieldHP, 0f, s.shield);
        data.maxHullHP = s.hull; data.maxShieldHP = s.shield; data.mass = s.mass; data.lockTime = s.lockTime;
        var movement = health.GetComponent<ShipMovement>();
        if (movement != null)
        {
            movement.maxSpeed = s.speed; movement.acceleration = s.thrust; movement.rotationSpeed = s.turn;
            movement.ApplyMassFromData();
        }
        var reactor = health.GetComponent<ShipReactor>();
        if (reactor != null) reactor.Configure(s.energy, s.regeneration, s.cooling, s.resistance);
        var weapon = health.GetComponent<ShipWeapon>();
        if (weapon != null) weapon.SetEquippedWeapons(loadout != null && loadout.weaponSlots != null
            ? loadout.weaponSlots.FindAll(item => item != null && !string.IsNullOrEmpty(item.id)) : new List<FittingItem>());
    }
}