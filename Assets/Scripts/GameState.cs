using UnityEngine;
using System;

/// <summary>
/// Глобальное состояние игры. Сейчас — флаг «бой начался».
/// Синглтон, живёт на [Managers].
/// </summary>
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    [Tooltip("Как только true — смена корабля и фитинга запрещены до конца боя.")]
    public bool isCombatStarted = false;

    /// <summary>Событие: бой начался.</summary>
    public event Action OnCombatStarted;

    /// <summary>Событие: бой завершён.</summary>
    public event Action OnCombatEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartCombat()
    {
        if (isCombatStarted) return;
        isCombatStarted = true;
        Debug.Log("[GameState] Бой начался — смена корабля/фитинга заблокирована.");
        OnCombatStarted?.Invoke();
    }

    public void EndCombat()
    {
        if (!isCombatStarted) return;
        isCombatStarted = false;
        Debug.Log("[GameState] Бой завершён — можно снова менять корабль/фитинг.");
        OnCombatEnded?.Invoke();
    }
}