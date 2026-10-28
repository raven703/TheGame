using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Presets retain item IDs and resolve them against the ship's owned equipment.</summary>
public static class LoadoutPresets
{
    [Serializable] private class Preset
    {
        public int formatVersion = 3;
        public string shipId;
        public List<string> weapons = new List<string>();
        public List<string> shields = new List<string>();
        public List<string> engines = new List<string>();
    }
    private static string Key(string ship, int slot) => "TheGame.Loadout.v2." + ship + "." + slot;
    private static List<string> Ids(List<FittingItem> items)
    {
        var result = new List<string>();
        if (items != null) foreach (var item in items)
            result.Add(item != null && !string.IsNullOrEmpty(item.id) ? item.id : "");
        return result;
    }
    public static bool Save(ShipLoadout loadout, int slot)
    {
        if (loadout == null || string.IsNullOrEmpty(loadout.shipId)) return false;
        var preset = new Preset { shipId = loadout.shipId, weapons = Ids(loadout.weaponSlots),
            shields = Ids(loadout.shieldSlots), engines = Ids(loadout.engineSlots) };
        PlayerPrefs.SetString(Key(loadout.shipId, slot), JsonUtility.ToJson(preset));
        PlayerPrefs.Save(); return true;
    }
    public static ShipLoadout Load(ShipLoadout owned, int slot)
    {
        if (!CombatSession.IsPreparing || owned == null) return null;
        string json = PlayerPrefs.GetString(Key(owned.shipId, slot), "");
        if (string.IsNullOrEmpty(json)) return null;
        Preset preset;
        try { preset = JsonUtility.FromJson<Preset>(json); }
        catch (Exception) { return null; }
        if (preset == null || preset.shipId != owned.shipId) return null;
        var definition=ShipRegistry.Instance!=null?ShipRegistry.Instance.Get(owned.shipId):null;
        return EquipmentDataAdapter.ResolvePreset(owned,new[]{preset.weapons,preset.shields,preset.engines},definition);
    }
}