using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Применяет сохранённый loadout текущего корабля игрока при старте игры.
/// Корабль определяется через PlayerProfile + ShipRegistry.
/// </summary>
public class LoadoutApplier : MonoBehaviour
{
    [Tooltip("Корабль игрока. Если null — ищется по тегу 'Player'.")]
    public ShipHealth targetShipHealth;

    private void Start()
    {
        // Дадим один кадр, чтобы PlayerProfile и ShipRegistry успели инициализироваться.
        Invoke(nameof(ApplySavedLoadout), 0.05f);
    }

    private void ApplySavedLoadout()
    {
        if (!CombatSession.IsPreparing) return;
        // 1. Находим корабль игрока
        if (targetShipHealth == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                targetShipHealth = playerObj.GetComponent<ShipHealth>();
        }

        if (targetShipHealth == null)
        {
            Debug.LogWarning("[LoadoutApplier] Корабль игрока не найден.");
            return;
        }

        // 2. Определяем ShipDefinition
        ShipDefinition def = targetShipHealth.shipDefinition;

        if (def == null && PlayerProfile.Instance != null && ShipRegistry.Instance != null)
        {
            def = ShipRegistry.Instance.Get(PlayerProfile.Instance.currentShipId);
        }

        if (def == null && ShipRegistry.Instance != null)
        {
            def = ShipRegistry.Instance.GetFirst();
        }

        if (def == null)
        {
            Debug.LogWarning("[LoadoutApplier] Не удалось определить ShipDefinition.");
            return;
        }

        // 3. Применяем ShipDefinition к кораблю (базовые статы)
        targetShipHealth.shipDefinition = def;
        ApplyBaseStats(targetShipHealth, def);

        var visuals = targetShipHealth.GetComponentInChildren<ShipVisuals>();
        if (visuals != null)
            visuals.Apply(def);

        // 4. Загружаем loadout
        var loadout = ShipLoadoutManager.GetLoadout(def.shipId);
        if (loadout == null)
        {
            Debug.Log($"[LoadoutApplier] Нет сохранённого loadout для {def.shipId}.");
            return;
        }

        ApplyLoadout(targetShipHealth, def, loadout);
        Debug.Log($"[LoadoutApplier] Loadout применён для {def.displayName} (lockTime={targetShipHealth.Data.lockTime:F2}s)");
    }

    private void ApplyBaseStats(ShipHealth health, ShipDefinition def)
    {
        ShipStatsCalculator.Apply(health, def, null);
    }
    private void ApplyLoadout(ShipHealth health, ShipDefinition def, ShipLoadout loadout)
    {
        ShipStatsCalculator.Apply(health, def, loadout);
    }
}