using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-панель перед боем: кнопки "В бой" и "Фитинг".
/// Показывается при старте сцены, скрывается при нажатии "В бой".
/// </summary>
public class PreCombatPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelRoot;         // корневой объект панели (для скрытия)
    public Button startCombatButton;
    public Button openFittingButton;
    public TextMeshProUGUI statusLabel;  // опционально: "Выбранный корабль: Cobra MK3"

    [Header("Scene References")]
    public ShipSpawner shipSpawner;
    public FittingButton fittingButton;  // существующая кнопка фитинга (опционально)

    private void Start()
    {
        // Кнопка "В бой"
        if (startCombatButton != null)
            startCombatButton.onClick.AddListener(OnStartCombat);

        // Кнопка "Фитинг"
        if (openFittingButton != null)
            openFittingButton.onClick.AddListener(OnOpenFitting);

        // Подписка на конец боя (если нужно показать панель снова)
        if (GameState.Instance != null)
            GameState.Instance.OnCombatEnded += ShowPanel;

        // Показать панель при старте
        ShowPanel();
        UpdateStatus();
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null)
            GameState.Instance.OnCombatEnded -= ShowPanel;
    }

    private void ShowPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        UpdateStatus();
    }

    private void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void UpdateStatus()
    {
        if (statusLabel == null) return;
        if (PlayerProfile.Instance == null || ShipRegistry.Instance == null)
        {
            statusLabel.text = "";
            return;
        }

        var def = ShipRegistry.Instance.Get(PlayerProfile.Instance.currentShipId);
        statusLabel.text = def != null ? $"Корабль: {def.displayName}" : "Корабль не выбран";
    }

    private void OnStartCombat()
    {
        if (shipSpawner == null)
        {
            Debug.LogError("[PreCombatPanel] shipSpawner не назначен.");
            return;
        }

        HidePanel();
        shipSpawner.StartCombat();
        Debug.Log("[PreCombatPanel] Бой запущен.");
    }

    private void OnOpenFitting()
    {
        if (fittingButton != null && fittingButton.fittingWindow != null)
        {
            fittingButton.fittingWindow.Toggle();
        }
        else
        {
            // fallback: ищем FittingWindow в сцене
            var fw = FindObjectOfType<FittingWindow>();
            if (fw != null) fw.Toggle();
        }
    }
}