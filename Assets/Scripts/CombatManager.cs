using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    [Tooltip("ShipHealth of the player ship.")]
    public ShipHealth playerHealth;

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    private bool isCombatOver = false;
    private bool allWavesCleared = false;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // Очищаем реестр врагов при старте сцены (на случай перезапуска)
        // EnemyRegistry.Clear();
    }

    private void Update()
    {
        if (CombatSession.IsPreparing || isCombatOver)
            return;

        // Поражение — если игрок уничтожен
        if (playerHealth == null || !playerHealth.gameObject.activeSelf || (playerHealth.Data != null && playerHealth.Data.IsDestroyed))
        {
            EndCombat(false);
            return;
        }

        // Победа — если волны кончились и живых врагов нет
        if (allWavesCleared && EnemyRegistry.GetAliveEnemies().Count == 0)
        {
            EndCombat(true);
        }
    }

    /// <summary>
    /// Вызывается ShipSpawner'ом, когда все волны заспавнены и уничтожены.
    /// </summary>
    public void NotifyAllWavesCleared()
    {
        allWavesCleared = true;
        Debug.Log("[CombatManager] Все волны уничтожены — победа!");
    }

    private void EndCombat(bool playerWon)
    {
        isCombatOver = true;

        if (playerWon)
        {
            if (winPanel != null) winPanel.SetActive(true);
            Debug.Log("=== БОЙ ЗАВЕРШЕН: ПОБЕДА! ===");
        }
        else
        {
            if (losePanel != null) losePanel.SetActive(true);
            Debug.Log("=== БОЙ ЗАВЕРШЕН: ПОРАЖЕНИЕ! ===");
        }
    }

    public void RestartCombat()
    {
        if (GameState.Instance != null)
            GameState.Instance.EndCombat();
        Time.timeScale = 1f;
        EnemyRegistry.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}