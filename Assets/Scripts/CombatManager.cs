using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    [Tooltip("ShipHealth of the player ship.")]
    public ShipHealth playerHealth;

    [Tooltip("ShipHealth of the enemy ship.")]
    public ShipHealth enemyHealth;

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    private bool isCombatOver = false;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (enemyHealth == null)
            return;

        var enemyWeapon = enemyHealth.GetComponent<ShipWeapon>();
        if (enemyWeapon != null)
            enemyWeapon.SetTarget(playerHealth);
        else
            Debug.LogWarning("[CombatManager] EnemyShip has no ShipWeapon component to assign a target to.");
    }

    private void Update()
    {
        if (isCombatOver)
            return;

        if (playerHealth == null || !playerHealth.gameObject.activeSelf || (playerHealth.Data != null && playerHealth.Data.IsDestroyed))
        {
            EndCombat(false);
        }
        else if (enemyHealth == null || !enemyHealth.gameObject.activeSelf || (enemyHealth.Data != null && enemyHealth.Data.IsDestroyed))
        {
            EndCombat(true);
        }
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

    /// <summary>
    /// Вызывается при нажатии на кнопку "Restart" в UI.
    /// </summary>
    public void RestartCombat()
    {
        Time.timeScale = 1f; // сброс паузы, если была включена
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}