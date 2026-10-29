using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum EquipmentCategory { Weapons = 0, Shields = 1, Engines = 2 }

    // Immutable rule data. DataKey refers to an adapter-owned immutable record,
    // not to a second inventory, serialized payload or current definition catalog.
    public sealed class EquipmentItem
    {
        public string DefinitionId { get; }
        public string InstanceId { get; }
        public EquipmentCategory Category { get; }
        public WeaponMountType MountType { get; }
        public string DataKey { get; }
        public EquipmentItem(string definitionId, string instanceId, EquipmentCategory category, WeaponMountType mountType, string dataKey)
        { DefinitionId = definitionId; InstanceId = instanceId; Category = category; MountType = mountType; DataKey = dataKey; }
        public EquipmentItem WithInstance(string id) => new EquipmentItem(DefinitionId, id, Category, MountType, DataKey);
        public bool SameData(EquipmentItem other) => other != null && DefinitionId == other.DefinitionId &&
            InstanceId == other.InstanceId && Category == other.Category && MountType == other.MountType && DataKey == other.DataKey;
    }

    public sealed class EquipmentHardpoint
    {
        public string Id;
        public int SlotIndex;
        public MountCompatibility Compatibility;
    }

    public sealed class EquipmentShipDefinition
    {
        public string Id;
        public int WeaponSlots, ShieldSlots, EngineSlots;
        public List<EquipmentHardpoint> Hardpoints = new List<EquipmentHardpoint>();
        public int Capacity(EquipmentCategory category) => category == EquipmentCategory.Weapons ? WeaponSlots :
            category == EquipmentCategory.Shields ? ShieldSlots : category == EquipmentCategory.Engines ? EngineSlots : 0;
        public EquipmentHardpoint Hardpoint(int index)
        {
            foreach (var point in Hardpoints) if (point != null && point.SlotIndex == index) return point;
            return new EquipmentHardpoint { Id = Id + ":weapon:" + index, SlotIndex = index, Compatibility = MountCompatibility.FixedOnly };
        }
    }

    // Mutable transport snapshots are always detached from the authoritative ledger.
    public sealed class ShipEquipment
    {
        public string ShipId;
        public List<EquipmentItem> Weapons = new List<EquipmentItem>();
        public List<EquipmentItem> Shields = new List<EquipmentItem>();
        public List<EquipmentItem> Engines = new List<EquipmentItem>();
        public List<EquipmentItem> Slots(EquipmentCategory category) => category == EquipmentCategory.Weapons ? Weapons :
            category == EquipmentCategory.Shields ? Shields : category == EquipmentCategory.Engines ? Engines : null;
        public ShipEquipment Copy() => new ShipEquipment { ShipId = ShipId, Weapons = new List<EquipmentItem>(Weapons),
            Shields = new List<EquipmentItem>(Shields), Engines = new List<EquipmentItem>(Engines) };
    }

    public sealed class EquipmentSnapshot
    {
        public bool StarterInventoryIssued;
        public int CombatStarterVersion;
        public List<EquipmentItem> Inventory = new List<EquipmentItem>();
        public List<ShipEquipment> Ships = new List<ShipEquipment>();
        public EquipmentSnapshot Copy()
        {
            var result = new EquipmentSnapshot { StarterInventoryIssued = StarterInventoryIssued, CombatStarterVersion = CombatStarterVersion, Inventory = new List<EquipmentItem>(Inventory) };
            foreach (var ship in Ships) result.Ships.Add(ship.Copy());
            return result;
        }
    }

    public static class EquipmentRules
    {
        public static IEnumerable<EquipmentCategory> Categories
        { get { yield return EquipmentCategory.Weapons; yield return EquipmentCategory.Shields; yield return EquipmentCategory.Engines; } }
        public static bool Fits(EquipmentShipDefinition definition, EquipmentCategory category, int index, EquipmentItem item)
        {
            return item == null || (definition != null && index >= 0 && index < definition.Capacity(category) && item.Category == category &&
                (category != EquipmentCategory.Weapons || MountCompatibilityRules.Accepts(definition.Hardpoint(index).Compatibility, item.MountType)));
        }
        public static bool Validate(EquipmentShipDefinition definition, ShipEquipment ship)
        {
            if (definition == null || ship == null || definition.Id != ship.ShipId) return false;
            foreach (var category in Categories)
            {
                var slots = ship.Slots(category);
                if (slots == null || slots.Count > definition.Capacity(category)) return false;
                for (int i = 0; i < slots.Count; i++) if (!Fits(definition, category, i, slots[i])) return false;
            }
            return true;
        }
        // Legacy repair semantics retained: preserve holes, return incompatible items to free stock.
        public static int Repair(EquipmentShipDefinition definition, ShipEquipment ship, List<EquipmentItem> inventory)
        {
            int moved = 0;
            foreach (var category in Categories)
            {
                var slots = ship.Slots(category); int capacity = definition.Capacity(category);
                for (int i = 0; i < slots.Count; i++)
                    if (slots[i] != null && !Fits(definition, category, i, slots[i])) { inventory.Add(slots[i]); slots[i] = null; moved++; }
                if (slots.Count > capacity) slots.RemoveRange(capacity, slots.Count - capacity);
                while (slots.Count < capacity) slots.Add(null);
            }
            return moved;
        }
        public static IEnumerable<EquipmentItem> Items(EquipmentSnapshot snapshot)
        {
            foreach (var item in snapshot.Inventory) yield return item;
            foreach (var ship in snapshot.Ships) foreach (var category in Categories)
                foreach (var item in ship.Slots(category)) if (item != null) yield return item;
        }
        public static void ValidateOwnership(EquipmentSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Inventory == null || snapshot.Ships == null) throw new ArgumentException("Повреждённое состояние оснащения.");
            var ships = new HashSet<string>();
            foreach (var ship in snapshot.Ships)
                if (ship == null || string.IsNullOrEmpty(ship.ShipId) || !ships.Add(ship.ShipId) || ship.Weapons == null || ship.Shields == null || ship.Engines == null)
                    throw new ArgumentException("Повтор или повреждённый корабль.");
            var ids = new HashSet<string>();
            foreach (var item in Items(snapshot))
                if (item == null || string.IsNullOrEmpty(item.DefinitionId) || string.IsNullOrEmpty(item.InstanceId) || !ids.Add(item.InstanceId))
                    throw new ArgumentException("Пустой или повторный instanceId.");
        }
        // Preset preview uses the historical installed-first order. Application re-resolves
        // atomically against current free stock + this ship, never another ship.
        public static ShipEquipment ResolvePreset(ShipEquipment owned, List<EquipmentItem> inventory, List<string>[] requirements, EquipmentShipDefinition definition)
        {
            if (owned == null || requirements == null || requirements.Length != 3) return null;
            var available = new List<EquipmentItem>();
            foreach (var category in Categories) foreach (var item in owned.Slots(category)) if (item != null) available.Add(item);
            available.AddRange(inventory);
            var result = new ShipEquipment { ShipId = owned.ShipId };
            for (int c = 0; c < 3; c++)
            {
                if (requirements[c] == null) return null;
                foreach (var id in requirements[c])
                {
                    var category = (EquipmentCategory)c;
                    if (string.IsNullOrEmpty(id)) { result.Slots(category).Add(null); continue; }
                    var item = available.Find(x => x.DefinitionId == id && x.Category == category);
                    if (item == null) return null;
                    result.Slots(category).Add(item); available.Remove(item);
                }
            }
            return definition == null || Validate(definition, result) ? result : null;
        }
    }

    /// <summary>The sole inventory owner. Commands prepare, validate, persist, then publish synchronously.</summary>
    public sealed class PlayerEquipment
    {
        private EquipmentSnapshot state;
        private readonly Action<EquipmentSnapshot> persist;
        private readonly object gate = new object();
        private bool writing;
        private readonly Func<string> newInstanceId;
        public PlayerEquipment(EquipmentSnapshot initial, Action<EquipmentSnapshot> persist, Func<string> newInstanceId = null)
        {
            EquipmentRules.ValidateOwnership(initial);
            state = initial.Copy(); this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
            this.newInstanceId = newInstanceId ?? (() => throw new InvalidOperationException("An identity source is required for starter issuance."));
        }
        public EquipmentSnapshot Snapshot { get { lock (gate) return state.Copy(); } }
        private static ShipEquipment Ship(EquipmentSnapshot data, string id) => data.Ships.Find(x => x.ShipId == id);
        private bool Editing(bool editable, out string error)
        {
            error = writing ? "Запись оснащения уже выполняется." : !editable ? "Оснащение заблокировано в бою." : null;
            return error == null;
        }
        private bool Commit(EquipmentSnapshot next, out string error, bool grant = false)
        {
            try
            {
                EquipmentRules.ValidateOwnership(next);
                var owned = new Dictionary<string, EquipmentItem>();
                foreach (var item in EquipmentRules.Items(state)) owned.Add(item.InstanceId, item);
                int count = 0;
                foreach (var item in EquipmentRules.Items(next))
                {
                    if (owned.TryGetValue(item.InstanceId, out var original))
                    { if (!item.SameData(original)) throw new InvalidOperationException("Изменены данные предмета."); }
                    else if (!grant) throw new InvalidOperationException("Изменён состав предметов.");
                    count++;
                }
                if (!grant && count != owned.Count) throw new InvalidOperationException("Изменён состав предметов.");
                // Callback cannot mutate the candidate or re-enter another command.
                writing = true;
                try { persist(next.Copy()); } finally { writing = false; }
                state = next; error = null; return true;
            }
            catch (Exception e) { error = "Оснащение не изменено: " + e.Message; return false; }
        }
        public bool CanMove(string shipId, EquipmentShipDefinition definition, string id, EquipmentCategory category, int target, int source, bool editable, out string error)
        {
            lock (gate)
            {
                if (!Editing(editable, out error)) return false;
                var ship = Ship(state, shipId); var slots = ship?.Slots(category);
                if (definition == null || definition.Id != shipId || slots == null || target < 0 || target >= slots.Count)
                { error = "Слот недоступен."; return false; }
                if (source == target) { error = "Перетаскивание отменено."; return false; }
                if (slots[target] != null) { error = "Слот занят. Сначала снимите модуль."; return false; }
                var item = source < 0 ? state.Inventory.Find(x => x.InstanceId == id) : source < slots.Count ? slots[source] : null;
                if (string.IsNullOrEmpty(id) || item == null || item.InstanceId != id) { error = "Предмет недоступен в исходном месте."; return false; }
                if (!EquipmentRules.Fits(definition, category, target, item)) { error = "Несовместимый слот."; return false; }
                return true;
            }
        }
        public bool Move(string shipId, EquipmentShipDefinition definition, string id, EquipmentCategory category, int target, int source, bool editable, out string error)
        {
            lock (gate)
            {
                if (!CanMove(shipId, definition, id, category, target, source, editable, out error)) return false;
                var next = state.Copy(); var slots = Ship(next, shipId).Slots(category);
                var item = source < 0 ? next.Inventory.Find(x => x.InstanceId == id) : slots[source];
                if (source < 0) next.Inventory.Remove(item); else slots[source] = null;
                slots[target] = item; return Commit(next, out error);
            }
        }
        public bool AutoEquip(EquipmentShipDefinition definition, string id, bool editable, out string error)
        {
            lock (gate)
            {
                if (!Editing(editable, out error)) return false;
                var ship = definition == null ? null : Ship(state, definition.Id);
                if (ship != null) foreach (var category in EquipmentRules.Categories)
                    for (int i = 0; i < ship.Slots(category).Count; i++)
                        if (CanMove(definition.Id, definition, id, category, i, -1, editable, out _))
                            return Move(definition.Id, definition, id, category, i, -1, editable, out error);
                error = "Нет свободного совместимого слота"; return false;
            }
        }
        public bool Unequip(string shipId, EquipmentCategory category, int index, string id, bool editable, out string error)
        {
            lock (gate)
            {
                if (!Editing(editable, out error)) return false;
                var next = state.Copy(); var slots = Ship(next, shipId)?.Slots(category);
                if (slots == null || index < 0 || index >= slots.Count || slots[index] == null || slots[index].InstanceId != id)
                { error = "Предмет недоступен в исходном месте."; return false; }
                next.Inventory.Add(slots[index]); slots[index] = null; return Commit(next, out error);
            }
        }
        public bool ApplyPlan(EquipmentShipDefinition definition, ShipEquipment plan, bool editable, out string error)
        {
            lock (gate)
            {
                if (!Editing(editable, out error)) return false;
                if (!EquipmentRules.Validate(definition, plan)) { error = "Комплект несовместим с кораблём."; return false; }
                var next = state.Copy(); var current = Ship(next, plan.ShipId);
                if (current == null) { error = "Корабль недоступен."; return false; }
                var available = new List<EquipmentItem>(next.Inventory);
                foreach (var category in EquipmentRules.Categories) foreach (var item in current.Slots(category)) if (item != null) available.Add(item);
                var resolved = new ShipEquipment { ShipId = plan.ShipId };
                foreach (var category in EquipmentRules.Categories) foreach (var requirement in plan.Slots(category))
                {
                    if (requirement == null) { resolved.Slots(category).Add(null); continue; }
                    var item = available.Find(x => x.DefinitionId == requirement.DefinitionId && x.Category == category && x.MountType == requirement.MountType);
                    if (item == null) { error = "Не хватает свободных предметов для комплекта."; return false; }
                    available.Remove(item); resolved.Slots(category).Add(item);
                }
                if (!EquipmentRules.Validate(definition, resolved)) { error = "Комплект несовместим с креплениями."; return false; }
                EquipmentRules.Repair(definition, resolved, available);
                next.Ships[next.Ships.IndexOf(current)] = resolved; next.Inventory = available;
                return Commit(next, out error);
            }
        }
        public bool EnsureShip(EquipmentShipDefinition definition, List<EquipmentItem> starter, bool editable, out string error)
        {
            lock (gate)
            {
                if (!Editing(editable, out error)) return false;
                if (definition == null) { error = "Корабль не найден."; return false; }
                var next = state.Copy(); var ship = Ship(next, definition.Id);
                bool changed = false, grant = !next.StarterInventoryIssued;
                if (ship == null) { ship = new ShipEquipment { ShipId = definition.Id }; next.Ships.Add(ship); changed = true; }
                if (grant)
                {
                    if (starter != null) foreach (var template in starter)
                        if (template != null && !string.IsNullOrEmpty(template.DefinitionId)) next.Inventory.Add(template.WithInstance(newInstanceId()));
                    next.StarterInventoryIssued = true; changed = true;
                }
                foreach (var category in EquipmentRules.Categories) changed |= ship.Slots(category).Count != definition.Capacity(category);
                changed |= EquipmentRules.Repair(definition, ship, next.Inventory) > 0;
                return !changed || Commit(next, out error, grant);
            }
        }
    }
}