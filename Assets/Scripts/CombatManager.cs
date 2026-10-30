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
    public bool IsCombatOver => isCombatOver;
    public string DefeatReason { get; private set; }
    public void FailOutsideSupport()
    {
        if(CombatSession.IsPreparing || isCombatOver)return;
        DefeatReason="Операция провалена: покинута зона поддержки";
        CombatSession.Log(DefeatReason);
        EndCombat(false);
        if(losePanel!=null)
        {
            var text=losePanel.GetComponent<TMPro.TextMeshProUGUI>();
            if(text==null)text=System.Array.Find(losePanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true),t=>t.GetComponentInParent<UnityEngine.UI.Button>()==null);
            if(text!=null)text.text=DefeatReason;
            var legacy=System.Array.Find(losePanel.GetComponentsInChildren<UnityEngine.UI.Text>(true),t=>t.GetComponentInParent<UnityEngine.UI.Button>()==null);
            if(legacy!=null)legacy.text=DefeatReason;
        }
    }

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
        if(isCombatOver)return;
        allWavesCleared = true;
        Debug.Log("[CombatManager] Все волны уничтожены — победа!");
    }

    private void EndCombat(bool playerWon)
    {
        if(isCombatOver)return;
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