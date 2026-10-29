using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;

/// <summary>Immutable full saved records indexed by opaque keys. No inventory or placement state here.</summary>
public sealed class EquipmentDataAdapter
{
    private readonly Dictionary<string, string> records = new Dictionary<string, string>();
    private readonly Dictionary<string, string> keys = new Dictionary<string, string>();
    private readonly Dictionary<string, int> shipFormats = new Dictionary<string, int>();
    private readonly Dictionary<EquipmentItem, FittingItem> sourceReferences = new Dictionary<EquipmentItem, FittingItem>();
    private readonly bool preserveReferences;
    public EquipmentDataAdapter(bool preserveReferences = false) { this.preserveReferences = preserveReferences; }
    public EquipmentItem ToCore(FittingItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return null;
        // Instance identity is Core-owned; do not bake it into the retained definition record.
        var copy = JsonUtility.FromJson<FittingItem>(JsonUtility.ToJson(item));
        copy.instanceId = null;
        string record = JsonUtility.ToJson(copy);
        if (!keys.TryGetValue(record, out var key))
        { key = Guid.NewGuid().ToString("N"); keys.Add(record, key); records.Add(key, record); }
        var result = new EquipmentItem(item.id, item.instanceId, (EquipmentCategory)(int)item.allowedGroup,
            (Game.Core.WeaponMountType)(int)item.mountType, key);
        if(preserveReferences)sourceReferences.Add(result,item);
        return result;
    }
    // Only legacy migration of a caller-owned DTO uses original references.
    // Ledger snapshots always use FromCore's detached reconstruction instead.
    public void RestoreReferences(List<FittingItem> target, List<EquipmentItem> source)
    { target.Clear(); foreach(var item in source) target.Add(item==null?null:sourceReferences[item]); }
    public FittingItem FromCore(EquipmentItem item)
    {
        if (item == null) return null;
        if (!records.TryGetValue(item.DataKey, out var record)) throw new InvalidOperationException("Утрачены данные предмета.");
        var result = JsonUtility.FromJson<FittingItem>(record);
        result.instanceId = item.InstanceId;
        return result;
    }
    public List<EquipmentItem> ToCore(List<FittingItem> items)
    {
        var result = new List<EquipmentItem>();
        if (items != null) foreach (var item in items) result.Add(ToCore(item));
        return result;
    }
    public List<FittingItem> FromCore(List<EquipmentItem> items)
    { var result = new List<FittingItem>(); foreach (var item in items) result.Add(FromCore(item)); return result; }
    public ShipEquipment ToCore(ShipLoadout ship)
    {
        if(ship==null)return null;
        if(!shipFormats.ContainsKey(ship.shipId))shipFormats.Add(ship.shipId,ship.formatVersion);
        return new ShipEquipment { ShipId = ship.shipId, Weapons = ToCore(ship.weaponSlots), Shields = ToCore(ship.shieldSlots), Engines = ToCore(ship.engineSlots) };
    }
    public ShipLoadout FromCore(ShipEquipment ship) => new ShipLoadout(ship.ShipId) {
        formatVersion = shipFormats.TryGetValue(ship.ShipId,out var version)?version:3,
        weaponSlots = FromCore(ship.Weapons), shieldSlots = FromCore(ship.Shields), engineSlots = FromCore(ship.Engines) };
    public EquipmentSnapshot ToCore(ShipLoadoutSaveFile data)
    {
        var result = new EquipmentSnapshot { StarterInventoryIssued = data.starterInventoryIssued, CombatStarterVersion = data.combatStarterVersion, Inventory = ToCore(data.inventory) };
        foreach (var ship in data.loadouts) result.Ships.Add(ToCore(ship));
        return result;
    }
    public ShipLoadoutSaveFile FromCore(EquipmentSnapshot data)
    {
        var result = new ShipLoadoutSaveFile { formatVersion = 4, starterInventoryIssued = data.StarterInventoryIssued, combatStarterVersion = data.CombatStarterVersion, inventory = FromCore(data.Inventory) };
        foreach (var ship in data.Ships) result.loadouts.Add(FromCore(ship));
        return result;
    }
    public static EquipmentShipDefinition Definition(ShipDefinition definition)
    {
        if (definition == null) return null;
        var result = new EquipmentShipDefinition { Id = definition.shipId, WeaponSlots = definition.weaponSlots,
            ShieldSlots = definition.shieldSlots, EngineSlots = definition.engineSlots };
        if (definition.hardpoints != null) foreach (var point in definition.hardpoints)
            if (point != null) result.Hardpoints.Add(new EquipmentHardpoint { Id = point.id, SlotIndex = point.slotIndex,
                Compatibility = (Game.Core.MountCompatibility)(int)point.compatibility });
        return result;
    }
    public static ShipLoadout ResolvePreset(ShipLoadout owned, List<string>[] requirements, ShipDefinition definition)
    {
        if (owned == null) return null;
        var adapter = new EquipmentDataAdapter();
        var result = EquipmentRules.ResolvePreset(adapter.ToCore(owned), adapter.ToCore(owned.inventory), requirements, Definition(definition));
        return result == null ? null : adapter.FromCore(result);
    }
}