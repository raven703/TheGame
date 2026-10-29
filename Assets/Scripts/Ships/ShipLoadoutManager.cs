using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Persistent player ledger. Reads are detached; failed writes never publish changes.</summary>
public static class ShipLoadoutManager
{
    private static PlayerEquipment ledger;
    internal static Action BeforeProductionAccess;
    private static string FilePath => Path.Combine(Application.persistentDataPath, "ship_loadouts.json");
    public static string GetSavePath() => FilePath;
    private static PlayerEquipment Ledger
    {
        get
        {
            if (ledger != null) return ledger;
            BeforeProductionAccess?.Invoke();
            if (!File.Exists(FilePath))
            {
                var fresh=new ShipLoadoutSaveFile { formatVersion = 4 };
                CombatStandardDefinitions.EnsureStarterSet(fresh);
                var created=new PlayerEquipment(JsonUtility.ToJson(fresh), WriteAtomic);
                WriteAtomic(created.Json);
                ledger = created;
                return ledger;
            }
            try
            {
                string original = File.ReadAllText(FilePath);
                var migrated=PlayerEquipment.Parse(original,true);
                bool starterGrant=CombatStandardDefinitions.EnsureStarterSet(migrated);
                var prepared = new PlayerEquipment(JsonUtility.ToJson(migrated), WriteAtomic);
                var originalData=JsonUtility.FromJson<ShipLoadoutSaveFile>(original);
                bool balanceMigration=false;
                if(originalData.formatVersion==4)
                    foreach(var item in PlayerEquipment.Items(originalData))
                        if(CombatStandardDefinitions.Upgrade(item))balanceMigration=true;
                if(balanceMigration || starterGrant || originalData.formatVersion<4)
                {
                    string backup=FilePath+".pre-combat-v1-"+Guid.NewGuid().ToString("N")+".bak";
                    File.Copy(FilePath,backup);
                    if(File.ReadAllText(backup)!=original)throw new IOException("Combat balance backup verification failed.");
                    WriteAtomic(prepared.Json);
                }
                ledger = prepared;
                return ledger;
            }
            catch (Exception e)
            {
                throw new InvalidDataException("Не удалось прочитать оснащение. Прогресс не сброшен: " + e.Message, e);
            }
        }
    }
    private static void WriteAtomic(string json)
    {
        new EquipmentFileStore(FilePath).Write(json);
    }
    public static ShipLoadout GetLoadout(string shipId)
    {
        var data = Ledger.Snapshot;
        var ship = data.loadouts.Find(x => x.shipId == shipId);
        if (ship != null) ship.inventory = data.inventory;
        return ship;
    }
    public static List<FittingItem> SharedInventory => Ledger.Snapshot.inventory;
    public static bool StarterInventoryIssued => Ledger.Snapshot.starterInventoryIssued;
    public static bool EnsureShip(ShipDefinition definition, List<FittingItem> starter, out string error) => Ledger.EnsureShip(definition, starter, out error);
    public static bool CanMove(string shipId, ShipDefinition def, string id, SlotGroup group, int target, int source, bool editable, out string error) => Ledger.CanMove(shipId, def, id, group, target, source, editable, out error);
    public static bool Move(string shipId, ShipDefinition def, string id, SlotGroup group, int target, int source, bool editable, out string error) => Ledger.Move(shipId, def, id, group, target, source, editable, out error);
    public static bool AutoEquip(ShipDefinition def, string id, bool editable, out string error) => Ledger.AutoEquip(def, id, editable, out error);
    public static bool Unequip(string ship, SlotGroup group, int index, string id, bool editable, out string error) => Ledger.Unequip(ship, group, index, id, editable, out error);
    public static bool ApplyPlan(ShipDefinition def, ShipLoadout plan, bool editable, out string error) => Ledger.ApplyPlan(def, plan, editable, out error);
    public static void SaveLoadout(ShipLoadout loadout)
    {
        var definition = ShipRegistry.Instance != null ? ShipRegistry.Instance.Get(loadout.shipId) : null;
        if (!ApplyPlan(definition, loadout, true, out var error)) throw new InvalidOperationException(error);
    }
    private static void Sanitize(ShipLoadout loadout) => PlayerEquipment.Normalize(loadout);
}
