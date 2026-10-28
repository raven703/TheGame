using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Unity serialization facade. Core is the sole owner of inventory and placement.</summary>
public sealed class PlayerEquipment
{
    private readonly Game.Core.PlayerEquipment core;
    private readonly EquipmentDataAdapter adapter = new EquipmentDataAdapter();
    public static bool EditingAllowed(bool requested) => requested && CombatSession.IsPreparing && (GameState.Instance == null || !GameState.Instance.isCombatStarted);
    public string Json => JsonUtility.ToJson(Snapshot, true);
    public ShipLoadoutSaveFile Snapshot => adapter.FromCore(core.Snapshot);
    public PlayerEquipment(string json, Action<string> persist)
    {
        if (persist == null) throw new ArgumentNullException(nameof(persist));
        var data = Parse(json, true);
        core = new Game.Core.PlayerEquipment(adapter.ToCore(data), snapshot => persist(JsonUtility.ToJson(adapter.FromCore(snapshot), true)));
    }
    public bool EnsureShip(ShipDefinition def, List<FittingItem> starter, out string error) =>
        core.EnsureShip(EquipmentDataAdapter.Definition(def), adapter.ToCore(starter), EditingAllowed(true), out error);
    public bool CanMove(string shipId, ShipDefinition def, string id, SlotGroup group, int target, int source, bool editable, out string error) =>
        core.CanMove(shipId, EquipmentDataAdapter.Definition(def), id, (Game.Core.EquipmentCategory)(int)group, target, source, EditingAllowed(editable), out error);
    public bool Move(string shipId, ShipDefinition def, string id, SlotGroup group, int target, int source, bool editable, out string error) =>
        core.Move(shipId, EquipmentDataAdapter.Definition(def), id, (Game.Core.EquipmentCategory)(int)group, target, source, EditingAllowed(editable), out error);
    public bool AutoEquip(ShipDefinition def, string id, bool editable, out string error) =>
        core.AutoEquip(EquipmentDataAdapter.Definition(def), id, EditingAllowed(editable), out error);
    public bool Unequip(string ship, SlotGroup group, int index, string id, bool editable, out string error) =>
        core.Unequip(ship, (Game.Core.EquipmentCategory)(int)group, index, id, EditingAllowed(editable), out error);
    public bool ApplyPlan(ShipDefinition def, ShipLoadout plan, bool editable, out string error) =>
        core.ApplyPlan(EquipmentDataAdapter.Definition(def), adapter.ToCore(plan), EditingAllowed(editable), out error);
    public static ShipLoadoutSaveFile Parse(string json, bool migrate)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"loadouts\"")) throw new InvalidDataException("Отсутствуют корабли.");
        var data = JsonUtility.FromJson<ShipLoadoutSaveFile>(json);
        if (data == null || data.loadouts == null || data.formatVersion > 4 || data.formatVersion < 0) throw new InvalidDataException("Повреждённый формат оснащения.");
        bool legacy = data.formatVersion < 4;
        if (legacy && !migrate) throw new InvalidDataException("Не выполнена миграция.");
        if (data.inventory == null) throw new InvalidDataException("Отсутствует общий инвентарь.");
        var ships = new HashSet<string>();
        foreach (var ship in data.loadouts)
        {
            if (ship == null || string.IsNullOrEmpty(ship.shipId) || !ships.Add(ship.shipId)) throw new InvalidDataException("Повтор или повреждённый корабль.");
            Normalize(ship);
            if (legacy) data.inventory.AddRange(ship.inventory);
            else if (ship.inventory.Count != 0) throw new InvalidDataException("Инвентарь записан дважды.");
            ship.inventory.Clear();
        }
        data.inventory.RemoveAll(x => x == null || string.IsNullOrEmpty(x.id));
        foreach (var item in Items(data))
        {
            if (legacy && string.IsNullOrEmpty(item.instanceId)) item.instanceId = Guid.NewGuid().ToString("N");
        }
        data.formatVersion = 4;
        if (legacy) data.starterInventoryIssued = true;
        Game.Core.EquipmentRules.ValidateOwnership(new EquipmentDataAdapter().ToCore(data));
        return data;
    }
    public static void Normalize(ShipLoadout ship)
    {
        ship.weaponSlots = NormalizeSlots(ship.weaponSlots); ship.shieldSlots = NormalizeSlots(ship.shieldSlots); ship.engineSlots = NormalizeSlots(ship.engineSlots);
        if (ship.inventory == null) ship.inventory = new List<FittingItem>();
        ship.inventory.RemoveAll(x => x == null || string.IsNullOrEmpty(x.id));
    }
    private static List<FittingItem> NormalizeSlots(List<FittingItem> slots)
    {
        if (slots == null) return new List<FittingItem>();
        for (int i = 0; i < slots.Count; i++) if (slots[i] == null || string.IsNullOrEmpty(slots[i].id)) slots[i] = null;
        return slots;
    }
    public static List<FittingItem> Slots(ShipLoadout ship, SlotGroup group) => group == SlotGroup.Top_Weapons ? ship.weaponSlots :
        group == SlotGroup.Center_Shields ? ship.shieldSlots : group == SlotGroup.Bottom_Engines ? ship.engineSlots : null;
    public static IEnumerable<FittingItem> Items(ShipLoadoutSaveFile data)
    {
        foreach (var item in data.inventory) if (item != null && !string.IsNullOrEmpty(item.id)) yield return item;
        foreach (var ship in data.loadouts) foreach (var slots in new[] { ship.weaponSlots, ship.shieldSlots, ship.engineSlots })
            foreach (var item in slots) if (item != null && !string.IsNullOrEmpty(item.id)) yield return item;
    }
}
