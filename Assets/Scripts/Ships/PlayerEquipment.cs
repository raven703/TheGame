using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Detached ledger: validate, persist, then publish. Failed writes leave ownership unchanged.</summary>
public sealed class PlayerEquipment
{
    private ShipLoadoutSaveFile state;
    private readonly Action<string> persist;
    public string Json => JsonUtility.ToJson(state, true);
    public ShipLoadoutSaveFile Snapshot => Parse(Json, false);
    public PlayerEquipment(string json, Action<string> persist)
    {
        state = Parse(json, true);
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
    }
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
        var seen = new HashSet<string>();
        foreach (var item in Items(data))
        {
            if (legacy && string.IsNullOrEmpty(item.instanceId)) item.instanceId = Guid.NewGuid().ToString("N");
            if (string.IsNullOrEmpty(item.instanceId) || !seen.Add(item.instanceId)) throw new InvalidDataException("Пустой или повторный instanceId: " + item.instanceId);
        }
        data.formatVersion = 4;
        if (legacy) data.starterInventoryIssued = true;
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
    private static bool Editing(bool editable) => editable && CombatSession.IsPreparing && (GameState.Instance == null || !GameState.Instance.isCombatStarted);
    private static bool Fits(ShipDefinition def, SlotGroup group, int index, FittingItem item)
    {
        if (def == null || item == null || item.allowedGroup != group || index < 0) return false;
        int capacity = group == SlotGroup.Top_Weapons ? def.weaponSlots : group == SlotGroup.Center_Shields ? def.shieldSlots : group == SlotGroup.Bottom_Engines ? def.engineSlots : 0;
        return index < capacity && (group != SlotGroup.Top_Weapons || WeaponMountRules.Fits(def, index, item));
    }
    private static ShipLoadout Ship(ShipLoadoutSaveFile data, string id) => data.loadouts.Find(x => x.shipId == id);
    private static bool EqualOwnership(ShipLoadoutSaveFile before, ShipLoadoutSaveFile after)
    {
        var owned = new Dictionary<string, string>();
        foreach (var item in Items(before)) owned.Add(item.instanceId, JsonUtility.ToJson(item));
        int count = 0;
        foreach (var item in Items(after))
        {
            if (!owned.TryGetValue(item.instanceId, out var original) || original != JsonUtility.ToJson(item)) return false;
            count++;
        }
        return count == owned.Count;
    }
    private bool Commit(ShipLoadoutSaveFile next, out string error, bool grant = false)
    {
        try
        {
            string json = JsonUtility.ToJson(next, true);
            var validated = Parse(json, false);
            if (!grant && !EqualOwnership(state, validated)) throw new InvalidDataException("Изменён состав принадлежащих игроку экземпляров.");
            persist(json);
            state = validated; error = null; return true;
        }
        catch (Exception e) { error = "Оснащение не изменено: " + e.Message; return false; }
    }
    public bool EnsureShip(ShipDefinition def, List<FittingItem> starter, out string error)
    {
        error = null;
        if (def == null) { error = "Корабль не найден."; return false; }
        var next = Snapshot;
        var ship = Ship(next, def.shipId);
        bool changed = false, grant = !next.starterInventoryIssued;
        if (ship == null) { ship = new ShipLoadout(def.shipId); next.loadouts.Add(ship); changed = true; }
        if (grant)
        {
            if (starter != null) foreach (var template in starter)
            {
                if (template == null || string.IsNullOrEmpty(template.id)) continue;
                var copy = JsonUtility.FromJson<FittingItem>(JsonUtility.ToJson(template));
                copy.instanceId = Guid.NewGuid().ToString("N"); next.inventory.Add(copy);
            }
            next.starterInventoryIssued = true; changed = true;
        }
        string old = JsonUtility.ToJson(ship);
        ship.inventory = next.inventory; WeaponMountRules.Migrate(def, ship); ship.inventory = new List<FittingItem>();
        changed |= old != JsonUtility.ToJson(ship);
        return !changed || Commit(next, out error, grant);
    }
    public bool CanMove(string shipId, ShipDefinition def, string instanceId, SlotGroup group, int target, int source, bool editable, out string error)
    {
        error = null;
        if (!Editing(editable)) { error = "Оснащение заблокировано в бою."; return false; }
        var ship = Ship(state, shipId); var slots = ship == null ? null : Slots(ship, group);
        if (def == null || def.shipId != shipId || slots == null || target < 0 || target >= slots.Count) { error = "Слот недоступен."; return false; }
        if (source == target) { error = "Перетаскивание отменено."; return false; }
        if (slots[target] != null && !string.IsNullOrEmpty(slots[target].id)) { error = "Слот занят. Сначала снимите модуль."; return false; }
        var item = source < 0 ? state.inventory.Find(x => x.instanceId == instanceId) : source < slots.Count ? slots[source] : null;
        if (string.IsNullOrEmpty(instanceId) || item == null || item.instanceId != instanceId) { error = "Предмет недоступен в исходном месте."; return false; }
        if (!Fits(def, group, target, item)) { error = "Несовместимый слот."; return false; }
        return true;
    }
    public bool Move(string shipId, ShipDefinition def, string instanceId, SlotGroup group, int target, int source, bool editable, out string error)
    {
        if (!CanMove(shipId, def, instanceId, group, target, source, editable, out error)) return false;
        var next = Snapshot; var slots = Slots(Ship(next, shipId), group);
        var item = source < 0 ? next.inventory.Find(x => x.instanceId == instanceId) : slots[source];
        if (source < 0) next.inventory.Remove(item); else slots[source] = null;
        slots[target] = item; return Commit(next, out error);
    }
    public bool Unequip(string shipId, SlotGroup group, int index, string instanceId, bool editable, out string error)
    {
        error = null;
        if (!Editing(editable)) { error = "Оснащение заблокировано в бою."; return false; }
        var next = Snapshot; var ship = Ship(next, shipId); var slots = ship == null ? null : Slots(ship, group);
        if (slots == null || index < 0 || index >= slots.Count || slots[index] == null || slots[index].instanceId != instanceId) { error = "Предмет недоступен в исходном месте."; return false; }
        next.inventory.Add(slots[index]); slots[index] = null; return Commit(next, out error);
    }
    public bool ApplyPlan(ShipDefinition def, ShipLoadout plan, bool editable, out string error)
    {
        error = null;
        if (!Editing(editable)) { error = "Оснащение заблокировано в бою."; return false; }
        if (!WeaponMountRules.Validate(def, plan)) { error = "Комплект несовместим с кораблём."; return false; }
        var next = Snapshot; var current = Ship(next, plan.shipId);
        if (current == null) { error = "Корабль недоступен."; return false; }
        var available = new List<FittingItem>(next.inventory);
        foreach (var slots in new[] { current.weaponSlots, current.shieldSlots, current.engineSlots }) foreach (var item in slots) if (item != null) available.Add(item);
        var resolved = new ShipLoadout(plan.shipId);
        foreach (var group in new[] { SlotGroup.Top_Weapons, SlotGroup.Center_Shields, SlotGroup.Bottom_Engines }) foreach (var requirement in Slots(plan, group))
        {
            if (requirement == null || string.IsNullOrEmpty(requirement.id)) { Slots(resolved, group).Add(null); continue; }
            var item = available.Find(x => x.id == requirement.id && x.allowedGroup == group && x.mountType == requirement.mountType);
            if (item == null) { error = "Не хватает свободных предметов для комплекта."; return false; }
            available.Remove(item); Slots(resolved, group).Add(item);
        }
        if (!WeaponMountRules.Validate(def, resolved)) { error = "Комплект несовместим с креплениями."; return false; }
        Pad(resolved.weaponSlots,def.weaponSlots);Pad(resolved.shieldSlots,def.shieldSlots);Pad(resolved.engineSlots,def.engineSlots);
        next.loadouts[next.loadouts.IndexOf(current)] = resolved; next.inventory = available;
        return Commit(next, out error);
    }
    private static void Pad(List<FittingItem> slots,int count){while(slots.Count<count)slots.Add(null);}
}