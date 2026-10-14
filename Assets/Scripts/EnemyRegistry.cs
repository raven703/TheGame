using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Реестр всех живых врагов на сцене. Используется радаром и переключателем цели.
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<ShipHealth> enemies = new List<ShipHealth>();

    public static IReadOnlyList<ShipHealth> Enemies => enemies;

    public static void Register(ShipHealth enemy)
    {
        if (enemy == null)
        {
            Debug.LogWarning("[Registry] Попытка зарегистрировать null.");
            return;
        }

        if (enemies.Contains(enemy))
        {
            Debug.LogWarning($"[Registry] {enemy.name} уже в реестре.");
            return;
        }

        enemies.Add(enemy);
        Debug.Log($"[Registry] Зарегистрирован: {enemy.name}. Всего: {enemies.Count}");
    }

    public static void Unregister(ShipHealth enemy)
    {
        if (enemy == null) return;

        if (enemies.Remove(enemy))
            Debug.Log($"[Registry] Удалён: {enemy.name}. Осталось: {enemies.Count}");
    }

    public static void Clear()
    {
        enemies.Clear();
        Debug.Log("[Registry] Реестр очищен.");
    }

    /// <summary>
    /// Живые (не уничтоженные) враги.
    /// </summary>
    public static List<ShipHealth> GetAliveEnemies()
    {
        var result = new List<ShipHealth>();

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];

            if (e == null) continue;
            if (e.Data == null) continue;
            if (e.Data.IsDestroyed) continue;
            if (!e.gameObject.activeInHierarchy) continue;

            result.Add(e);
        }

        return result;
    }
}