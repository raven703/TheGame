using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class PlayerEquipmentTests
    {
        private PlayerEquipment ledger;
        private EquipmentShipDefinition definition;
        private bool fail;
        private int writes;
        private static EquipmentItem Item(string id, string definition = "laser", EquipmentCategory category = EquipmentCategory.Weapons,
            WeaponMountType mount = WeaponMountType.Fixed) => new EquipmentItem(definition, id, category, mount, "saved-" + id);
        private static ShipEquipment Ship(string id) => new ShipEquipment { ShipId = id, Weapons = new List<EquipmentItem> { null, null, null } };
        private static string Map(EquipmentSnapshot data) => data.StarterInventoryIssued + ":" +
            string.Join(",", data.Inventory.Select(x => x.InstanceId + "/" + x.DataKey)) + ":" +
            string.Join(";", data.Ships.Select(x => x.ShipId + ":" + string.Join("|", EquipmentRules.Categories.Select(c =>
                string.Join(",", x.Slots(c).Select(i => i == null ? "_" : i.InstanceId + "/" + i.DefinitionId + "/" + i.DataKey))))));
        private void Conserved(params string[] ids) => Assert.That(EquipmentRules.Items(ledger.Snapshot).Select(x => x.InstanceId), Is.EquivalentTo(ids));
        [SetUp] public void Setup()
        {
            fail = false; writes = 0;
            definition = new EquipmentShipDefinition { Id = "one", WeaponSlots = 3 };
            var data = new EquipmentSnapshot { StarterInventoryIssued = true, Inventory = new List<EquipmentItem> { Item("a"), Item("b") } };
            data.Ships.Add(Ship("one")); var other = Ship("two"); other.Weapons[0] = Item("c", "other"); data.Ships.Add(other);
            ledger = new PlayerEquipment(data, _ => { if (fail) throw new InvalidOperationException("disk failure"); writes++; });
        }
        private bool Move(string id, int target, int source = -1, bool edit = true) =>
            ledger.Move("one", definition, id, EquipmentCategory.Weapons, target, source, edit, out _);
        [Test] public void OwnedCopiesRemainDistinct_MovePreservesIdsDataAndHoles()
        {
            Assert.True(Move("a", 2)); Assert.AreEqual("a", ledger.Snapshot.Ships[0].Weapons[2].InstanceId);
            Assert.IsNull(ledger.Snapshot.Ships[0].Weapons[0]); Assert.IsNull(ledger.Snapshot.Ships[0].Weapons[1]);
            Assert.AreEqual("b", ledger.Snapshot.Inventory.Single().InstanceId); Conserved("a", "b", "c");
            Assert.AreEqual("saved-a", ledger.Snapshot.Ships[0].Weapons[2].DataKey);
        }
        [Test] public void UnknownOrOtherShipInstanceCannotBeMoved()
        {
            string before = Map(ledger.Snapshot);
            Assert.False(Move("missing", 0)); Assert.False(Move("c", 0));
            Assert.AreEqual(before, Map(ledger.Snapshot)); Assert.Zero(writes);
        }
        [Test] public void DuplicateAndMissingIdentityAreRejectedAtConstruction()
        {
            var data = ledger.Snapshot; data.Inventory.Add(Item("a"));
            Assert.Throws<ArgumentException>(() => new PlayerEquipment(data, _ => { }));
            data = ledger.Snapshot; data.Inventory[0] = Item("");
            Assert.Throws<ArgumentException>(() => new PlayerEquipment(data, _ => { }));
        }
        [Test] public void OccupiedAndIncompatibleRefuseWithoutChanges()
        {
            Assert.True(Move("a", 0)); string before = Map(ledger.Snapshot); int oldWrites = writes;
            Assert.False(Move("b", 0)); definition.Hardpoints.Add(new EquipmentHardpoint { SlotIndex = 1, Compatibility = MountCompatibility.TurretOnly });
            Assert.False(Move("b", 1)); Assert.AreEqual(before, Map(ledger.Snapshot)); Assert.AreEqual(oldWrites, writes);
            Assert.False(EquipmentRules.Fits(definition, EquipmentCategory.Shields, 0, Item("x")));
        }
        [Test] public void UnequipExactlyOne_TransfersThroughSharedStock_NotOtherShip()
        {
            Assert.True(Move("a", 2));
            Assert.True(ledger.Unequip("one", EquipmentCategory.Weapons, 2, "a", true, out _));
            Assert.False(ledger.Unequip("one", EquipmentCategory.Weapons, 2, "a", true, out _));
            definition.Id = "two"; Assert.True(ledger.Move("two", definition, "a", EquipmentCategory.Weapons, 1, -1, true, out _));
            Assert.IsNull(ledger.Snapshot.Ships[0].Weapons[2]); Assert.AreEqual("c", ledger.Snapshot.Ships[1].Weapons[0].InstanceId);
            Assert.AreEqual("a", ledger.Snapshot.Ships[1].Weapons[1].InstanceId); Conserved("a", "b", "c");
        }
        [Test] public void CombatRejectsEveryMutationIncludingEnsureAndAutoEquip()
        {
            string before = Map(ledger.Snapshot);
            Assert.False(Move("a", 0, -1, false));
            Assert.False(ledger.AutoEquip(definition, "a", false, out _));
            Assert.False(ledger.Unequip("two", EquipmentCategory.Weapons, 0, "c", false, out _));
            Assert.False(ledger.ApplyPlan(definition, Ship("one"), false, out _));
            Assert.False(ledger.EnsureShip(definition, null, false, out _));
            Assert.AreEqual(before, Map(ledger.Snapshot)); Assert.Zero(writes);
        }
        [Test] public void PlanUsesTwoOwnedCopiesAndPreservesEmptyIndex()
        {
            var plan = Ship("one"); plan.Weapons[0] = Item("irrelevant-a"); plan.Weapons[2] = Item("irrelevant-b");
            Assert.True(ledger.ApplyPlan(definition, plan, true, out _));
            var slots = ledger.Snapshot.Ships[0].Weapons;
            Assert.AreEqual("a", slots[0].InstanceId); Assert.IsNull(slots[1]); Assert.AreEqual("b", slots[2].InstanceId);
            Assert.IsEmpty(ledger.Snapshot.Inventory); Conserved("a", "b", "c");
        }
        [Test] public void MissingPlanRejectsWholeAndNeverStealsOtherShip()
        {
            foreach (string missing in new[] { "laser", "other" })
            {
                var plan = Ship("one"); plan.Weapons[0] = Item("x"); plan.Weapons[1] = Item("y"); plan.Weapons[2] = Item("z", missing);
                string before = Map(ledger.Snapshot);
                Assert.False(ledger.ApplyPlan(definition, plan, true, out _)); Assert.AreEqual(before, Map(ledger.Snapshot));
            }
            Assert.Zero(writes); Conserved("a", "b", "c");
        }
        [Test] public void FailedWritesDoNotPublishMoveUnequipPlanOrGrant()
        {
            fail = true; string before = Map(ledger.Snapshot); Assert.False(Move("a", 0)); Assert.AreEqual(before, Map(ledger.Snapshot));
            fail = false; Assert.True(Move("a", 0)); before = Map(ledger.Snapshot); fail = true;
            Assert.False(ledger.Unequip("one", EquipmentCategory.Weapons, 0, "a", true, out _));
            Assert.False(ledger.ApplyPlan(definition, Ship("one"), true, out _)); Assert.AreEqual(before, Map(ledger.Snapshot));
            var fresh = new PlayerEquipment(new EquipmentSnapshot(), _ => throw new Exception("disk failure"));
            Assert.False(fresh.EnsureShip(definition, new List<EquipmentItem> { Item("template") }, true, out _));
            Assert.IsEmpty(fresh.Snapshot.Ships); Assert.False(fresh.Snapshot.StarterInventoryIssued);
        }
        [Test] public void SnapshotAndWriterMutationCannotChangeLedger()
        {
            var data = ledger.Snapshot; var isolated = new PlayerEquipment(data, s => { s.Inventory.Clear(); s.Ships.Clear(); });
            data.Inventory.Clear(); data.Ships[0].Weapons.Clear();
            var read = isolated.Snapshot; read.Inventory.Clear(); read.Ships.Clear();
            Assert.True(isolated.Move("one", definition, "a", EquipmentCategory.Weapons, 0, -1, true, out _));
            Assert.AreEqual(3, EquipmentRules.Items(isolated.Snapshot).Count()); Assert.AreEqual("a", isolated.Snapshot.Ships[0].Weapons[0].InstanceId);
        }
        [Test] public void CanMoveAndCancelledDragNeverReserveByRemovingSource()
        {
            Assert.True(Move("a", 0)); string before = Map(ledger.Snapshot); int oldWrites = writes;
            Assert.True(ledger.CanMove("one", definition, "a", EquipmentCategory.Weapons, 2, 0, true, out _));
            Assert.False(Move("a", 0, 0)); Assert.False(Move("b", 2, 0));
            Assert.AreEqual(before, Map(ledger.Snapshot)); Assert.AreEqual(oldWrites, writes);
            Assert.True(Move("a", 2, 0)); Assert.IsNull(ledger.Snapshot.Ships[0].Weapons[0]); Conserved("a", "b", "c");
        }
        [Test] public void AutoEquipUsesFirstCompatibleEmptySlot()
        {
            definition.Hardpoints.Add(new EquipmentHardpoint { SlotIndex = 0, Compatibility = MountCompatibility.TurretOnly });
            Assert.True(ledger.AutoEquip(definition, "a", true, out _)); Assert.True(ledger.AutoEquip(definition, "b", true, out _));
            Assert.IsNull(ledger.Snapshot.Ships[0].Weapons[0]); Assert.AreEqual("a", ledger.Snapshot.Ships[0].Weapons[1].InstanceId);
            Assert.AreEqual("b", ledger.Snapshot.Ships[0].Weapons[2].InstanceId); Conserved("a", "b", "c");
        }
        [Test] public void StarterIssuedOnce_RepeatedReadsAndNewShipDoNotGrant()
        {
            var fresh = new PlayerEquipment(new EquipmentSnapshot(), _ => { });
            var templates = new List<EquipmentItem> { Item("template"), Item("template") };
            Assert.True(fresh.EnsureShip(definition, templates, true, out _));
            string before = Map(fresh.Snapshot);
            for (int i = 0; i < 3; i++) { Assert.True(fresh.EnsureShip(definition, templates, true, out _)); Assert.AreEqual(before, Map(fresh.Snapshot)); }
            definition.Id = "new"; Assert.True(fresh.EnsureShip(definition, templates, true, out _));
            Assert.AreEqual(2, fresh.Snapshot.Inventory.Count); Assert.AreEqual(2, fresh.Snapshot.Inventory.Select(x => x.InstanceId).Distinct().Count());
        }
        [Test] public void ReentrantWriterCannotOverwriteNewerState()
        {
            PlayerEquipment nested = null; bool nestedResult = true;
            nested = new PlayerEquipment(ledger.Snapshot, snapshot => nestedResult = nested.Move("one", definition, "b", EquipmentCategory.Weapons, 1, -1, true, out _));
            Assert.True(nested.Move("one", definition, "a", EquipmentCategory.Weapons, 0, -1, true, out _));
            Assert.False(nestedResult); Assert.AreEqual("a", nested.Snapshot.Ships[0].Weapons[0].InstanceId);
            Assert.IsNull(nested.Snapshot.Ships[0].Weapons[1]); Assert.AreEqual("b", nested.Snapshot.Inventory.Single().InstanceId);
        }
    }
}