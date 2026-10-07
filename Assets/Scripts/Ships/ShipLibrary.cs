using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject-справочник всех доступных кораблей в игре.
/// Нужен для UI-селектора: получить список ShipDefinition по имени.
/// </summary>
[CreateAssetMenu(fileName = "ShipLibrary", menuName = "Space/Ship Library")]
public class ShipLibrary : ScriptableObject
{
    public List<ShipDefinition> ships = new List<ShipDefinition>();

    public ShipDefinition GetById(string shipId)
    {
        foreach (var s in ships)
        {
            if (s != null && s.shipId == shipId)
                return s;
        }
        return null;
    }
}