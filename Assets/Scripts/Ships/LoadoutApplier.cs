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
        if (health.Data == null) return;

        health.Data.maxHullHP = def.maxHullHP;
        health.Data.currentHullHP = def.maxHullHP;
        health.Data.maxShieldHP = def.baseShieldHP;
        health.Data.currentShieldHP = def.baseShieldHP;
        health.Data.mass = def.baseMass;
        health.Data.lockTime = def.baseLockTime;

        var movement = health.GetComponent<ShipMovement>();
        if (movement != null)
        {
            movement.maxSpeed = def.baseMaxSpeed;
            movement.acceleration = def.baseAcceleration;
            movement.rotationSpeed = def.baseRotationSpeed;
            movement.ApplyMassFromData();
        }
    }

    private void ApplyLoadout(ShipHealth health, ShipDefinition def, ShipLoadout loadout)
    {
        var shipWeapon = health.GetComponent<ShipWeapon>();
        var shipMovement = health.GetComponent<ShipMovement>();

        // Оружие
        if (shipWeapon != null)
        {
            var installedWeapons = new List<FittingItem>();
            if (loadout.weaponSlots != null)
                foreach (var w in loadout.weaponSlots)
                    if (w != null) installedWeapons.Add(w);

            shipWeapon.SetEquippedWeapons(installedWeapons);
        }

        // Бонусы от установленных модулей
        float shieldBonus = 0f, speedBonus = 0f, accelBonus = 0f, massBonus = 0f, lockTimeBonus = 0f;
        ApplyBonuses(loadout.weaponSlots, ref shieldBonus, ref speedBonus, ref accelBonus, ref massBonus, ref lockTimeBonus);
        ApplyBonuses(loadout.shieldSlots, ref shieldBonus, ref speedBonus, ref accelBonus, ref massBonus, ref lockTimeBonus);
        ApplyBonuses(loadout.engineSlots, ref shieldBonus, ref speedBonus, ref accelBonus, ref massBonus, ref lockTimeBonus);

        if (health.Data != null)
        {
            // Щит с учётом дельты
            float oldMaxShield = health.Data.maxShieldHP;
            float newMaxShield = def.baseShieldHP + shieldBonus;
            float delta = newMaxShield - oldMaxShield;

            health.Data.maxShieldHP = newMaxShield;
            health.Data.currentShieldHP = Mathf.Clamp(
                health.Data.currentShieldHP + delta,
                0f,
                newMaxShield);

            health.Data.mass = Mathf.Max(0.5f, def.baseMass + massBonus);

            // Захват цели: база + бонусы, минимум 0.05 сек
            health.Data.lockTime = Mathf.Max(0.05f, def.baseLockTime + lockTimeBonus);
        }

        if (shipMovement != null)
        {
            shipMovement.maxSpeed = def.baseMaxSpeed + speedBonus;
            shipMovement.acceleration = def.baseAcceleration + accelBonus;
            shipMovement.ApplyMassFromData();
        }
    }

    private void ApplyBonuses(List<FittingItem> list,
        ref float shield, ref float speed, ref float accel, ref float mass, ref float lockTime)
    {
        if (list == null) return;
        foreach (var item in list)
        {
            if (item == null) continue;
            shield += item.shieldBonus;
            speed += item.speedBonus;
            accel += item.accelBonus;
            mass += item.massBonus;
            lockTime += item.lockTimeBonus;
        }
    }
}