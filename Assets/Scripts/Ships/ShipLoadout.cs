using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Состояние фитинга корабля: что установлено в слоты и что лежит в инвентаре.
/// Сериализуется через JsonUtility.
/// </summary>
[System.Serializable]
public class ShipLoadout
{
    public string shipId;

    public List<FittingItem> weaponSlots = new List<FittingItem>();
    public List<FittingItem> shieldSlots = new List<FittingItem>();
    public List<FittingItem> engineSlots = new List<FittingItem>();
    public List<FittingItem> inventory = new List<FittingItem>();

    public ShipLoadout() { }

    public ShipLoadout(string shipId)
    {
        this.shipId = shipId;
    }

    public bool IsEmpty()
    {
        return (weaponSlots == null || weaponSlots.Count == 0)
            && (shieldSlots == null || shieldSlots.Count == 0)
            && (engineSlots == null || engineSlots.Count == 0)
            && (inventory == null || inventory.Count == 0);
    }
}

/// <summary>
/// Обёртка для JsonUtility (он не умеет сериализовать словари, только списки).
/// </summary>
[System.Serializable]
public class ShipLoadoutSaveFile
{
    public List<ShipLoadout> loadouts = new List<ShipLoadout>();
}