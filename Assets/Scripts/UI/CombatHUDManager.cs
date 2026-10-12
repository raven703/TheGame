using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-панель боевого HUD: HP игрока слева, HP цели справа.
/// Полосы корпуса/щита, индикаторы модулей W/S/E.
/// </summary>
public class CombatHUDManager : MonoBehaviour
{
    [Header("Ship References")]
    public ShipHealth playerHealth;
    public ShipWeapon playerWeapon;

    [Header("Fallback Enemy")]
    [Tooltip("Используется, если цель не захвачена playerWeapon.Target == null.")]
    public ShipHealth defaultEnemyHealth;

    [Header("Player HUD — Labels")]
    public TextMeshProUGUI playerTitleLabel;
    public TextMeshProUGUI playerHullText;
    public TextMeshProUGUI playerShieldText;

    [Header("Player HUD — Bars")]
    public Image playerHullBarFill;
    public Image playerShieldBarFill;

    [Header("Player HUD — Modules")]
    public Image playerWeaponBoxImage;
    public Image playerShieldBoxImage;
    public Image playerEngineBoxImage;

    [Header("Target HUD — Labels")]
    public TextMeshProUGUI targetTitleLabel;
    public TextMeshProUGUI targetHullText;
    public TextMeshProUGUI targetShieldText;

    [Header("Target HUD — Bars")]
    public Image targetHullBarFill;
    public Image targetShieldBarFill;

    [Header("Target HUD — Modules")]
    public Image targetWeaponBoxImage;
    public Image targetShieldBoxImage;
    public Image targetEngineBoxImage;

    [Header("Colors")]
    public Color hullColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color shieldColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color moduleOkColor = new Color(0.15f, 0.75f, 0.15f, 1f);
    public Color moduleDamagedColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    public Color moduleDestroyedColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private void Update()
    {
        UpdatePlayerHUD();
        UpdateTargetHUD();
    }

    private void UpdatePlayerHUD()
    {
        if (playerHealth == null || playerHealth.Data == null) return;

        // Заголовок
        if (playerTitleLabel != null)
            playerTitleLabel.text = "ИГРОК";

        // Корпус
        float hullPct = Mathf.Clamp01(playerHealth.Data.currentHullHP / Mathf.Max(1f, playerHealth.Data.maxHullHP));
        if (playerHullBarFill != null)
        {
            playerHullBarFill.fillAmount = hullPct;
            playerHullBarFill.color = hullColor;
        }
        if (playerHullText != null)
            playerHullText.text = $"Корпус: {playerHealth.Data.currentHullHP:F0}/{playerHealth.Data.maxHullHP:F0}";

        // Щит
        float shieldPct = playerHealth.Data.maxShieldHP > 0f
            ? Mathf.Clamp01(playerHealth.Data.currentShieldHP / playerHealth.Data.maxShieldHP)
            : 0f;
        if (playerShieldBarFill != null)
        {
            playerShieldBarFill.fillAmount = shieldPct;
            playerShieldBarFill.color = shieldColor;
        }
        if (playerShieldText != null)
            playerShieldText.text = $"Щит: {playerHealth.Data.currentShieldHP:F0}/{playerHealth.Data.maxShieldHP:F0}";

        // Модули
        UpdateModuleBox(playerWeaponBoxImage, playerHealth.Data.GetModule(ModuleType.Weapon));
        UpdateModuleBox(playerShieldBoxImage, playerHealth.Data.GetModule(ModuleType.Shield));
        UpdateModuleBox(playerEngineBoxImage, playerHealth.Data.GetModule(ModuleType.Engine));
    }

    private void UpdateTargetHUD()
    {
        ShipHealth target = (playerWeapon != null && playerWeapon.Target != null)
            ? playerWeapon.Target
            : defaultEnemyHealth;

        if (target == null || target.Data == null)
            return;

        if (targetTitleLabel != null)
            targetTitleLabel.text = "ЦЕЛЬ";

        float hullPct = Mathf.Clamp01(target.Data.currentHullHP / Mathf.Max(1f, target.Data.maxHullHP));
        if (targetHullBarFill != null)
        {
            targetHullBarFill.fillAmount = hullPct;
            targetHullBarFill.color = hullColor;
        }
        if (targetHullText != null)
            targetHullText.text = $"Корпус: {target.Data.currentHullHP:F0}/{target.Data.maxHullHP:F0}";

        float shieldPct = target.Data.maxShieldHP > 0f
            ? Mathf.Clamp01(target.Data.currentShieldHP / target.Data.maxShieldHP)
            : 0f;
        if (targetShieldBarFill != null)
        {
            targetShieldBarFill.fillAmount = shieldPct;
            targetShieldBarFill.color = shieldColor;
        }
        if (targetShieldText != null)
            targetShieldText.text = $"Щит: {target.Data.currentShieldHP:F0}/{target.Data.maxShieldHP:F0}";

        UpdateModuleBox(targetWeaponBoxImage, target.Data.GetModule(ModuleType.Weapon));
        UpdateModuleBox(targetShieldBoxImage, target.Data.GetModule(ModuleType.Shield));
        UpdateModuleBox(targetEngineBoxImage, target.Data.GetModule(ModuleType.Engine));
    }

    private void UpdateModuleBox(Image boxImage, ShipModule module)
    {
        if (boxImage == null) return;

        if (module == null)
        {
            boxImage.color = moduleDestroyedColor;
            return;
        }

        if (module.isDestroyed)
        {
            boxImage.color = moduleDestroyedColor;
            return;
        }

        float pct = module.maxHP > 0f ? module.currentHP / module.maxHP : 0f;

        if (pct >= 0.99f)
            boxImage.color = moduleOkColor;
        else
            boxImage.color = moduleDamagedColor;
    }
}