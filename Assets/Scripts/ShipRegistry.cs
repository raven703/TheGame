using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Реестр всех ShipDefinition по shipId.
/// Заполняется из ShipLibrary или вручную.
/// </summary>
public class ShipRegistry : MonoBehaviour
{
    public static ShipRegistry Instance { get; private set; }

    [Tooltip("Справочник кораблей (ShipLibrary). Если задан — реестр заполнится из него.")]
    public ShipLibrary shipLibrary;

    private Dictionary<string, ShipDefinition> registry = new Dictionary<string, ShipDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildRegistry();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildRegistry()
    {
        registry.Clear();

        if (shipLibrary == null || shipLibrary.ships == null)
            return;

        foreach (var ship in shipLibrary.ships)
        {
            if (ship == null || string.IsNullOrEmpty(ship.shipId))
                continue;

            if (registry.ContainsKey(ship.shipId))
            {
                Debug.LogWarning($"[ShipRegistry] Дубликат shipId: {ship.shipId}. Пропускаем.");
                continue;
            }

            registry[ship.shipId] = ship;
        }

        Debug.Log($"[ShipRegistry] Зарегистрировано кораблей: {registry.Count}");
    }

    /// <summary>
    /// Получить ShipDefinition по shipId. Возвращает null, если не найден.
    /// </summary>
    public ShipDefinition Get(string shipId)
    {
        if (string.IsNullOrEmpty(shipId))
            return null;

        return registry.TryGetValue(shipId, out var def) ? def : null;
    }

    /// <summary>
    /// Первый доступный корабль из реестра (fallback).
    /// </summary>
    public ShipDefinition GetFirst()
    {
        foreach (var kv in registry)
            return kv.Value;
        return null;
    }
}