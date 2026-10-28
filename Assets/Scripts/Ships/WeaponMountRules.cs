using System;
using System.Collections.Generic;
using UnityEngine;

public enum MountCompatibility { FixedOnly, TurretOnly, Universal }

[Serializable]
public class WeaponHardpoint
{
    public string id;
    public int slotIndex;
    public float localAxis;
    public MountCompatibility compatibility;
    public float sector = 60f;
}

/// <summary>Shared fitting, save migration and combat rules. Never infers mount type from damage.</summary>
public static class WeaponMountRules
{
    public static bool Accepts(MountCompatibility mount, WeaponMountType type)
    {
        // Keep serialized Unity types in place; the rule itself has one owner in Core.
        return Game.Core.MountCompatibilityRules.Accepts(
            (Game.Core.MountCompatibility)(int)mount, (Game.Core.WeaponMountType)(int)type);
    }
    public static WeaponHardpoint Get(ShipDefinition definition, int slot)
    {
        if (definition != null && definition.hardpoints != null)
            foreach (var point in definition.hardpoints)
                if (point != null && point.slotIndex == slot) return point;
        return new WeaponHardpoint { id = (definition != null ? definition.shipId : "legacy") + ":weapon:" + slot,
            slotIndex = slot, compatibility = MountCompatibility.FixedOnly, sector = 60f };
    }
    public static bool Fits(ShipDefinition definition, int slot, FittingItem item)
    {
        var description = EquipmentDataAdapter.Definition(definition) ?? new Game.Core.EquipmentShipDefinition {
            Id = "legacy", WeaponSlots = int.MaxValue };
        return Game.Core.EquipmentRules.Fits(description,Game.Core.EquipmentCategory.Weapons,slot,new EquipmentDataAdapter().ToCore(item));
    }
    public static bool CanSwap(ShipDefinition definition, int a, FittingItem first, int b, FittingItem second)
    {
        return Fits(definition,b,first) && Fits(definition,a,second);
    }
    public static bool Validate(ShipDefinition definition, ShipLoadout loadout)
    {
        return Game.Core.EquipmentRules.Validate(EquipmentDataAdapter.Definition(definition),new EquipmentDataAdapter().ToCore(loadout));
    }
    // Active saves migrate in place. Presets must use Validate, never this repair path.
    public static int Migrate(ShipDefinition definition, ShipLoadout loadout)
    {
        if (definition == null || loadout == null) return 0;
        var adapter=new EquipmentDataAdapter(true);
        var ship=adapter.ToCore(loadout);var inventory=adapter.ToCore(loadout.inventory);
        int moved=Game.Core.EquipmentRules.Repair(EquipmentDataAdapter.Definition(definition),ship,inventory);
        if(loadout.inventory==null)loadout.inventory=new List<FittingItem>();
        adapter.RestoreReferences(loadout.weaponSlots,ship.Weapons);adapter.RestoreReferences(loadout.shieldSlots,ship.Shields);
        adapter.RestoreReferences(loadout.engineSlots,ship.Engines);adapter.RestoreReferences(loadout.inventory,inventory);
        loadout.formatVersion=3;
        return moved;
    }
}