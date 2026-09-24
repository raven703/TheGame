using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простая полоска HP и статуса модулей прямо над кораблём в 2D.
/// </summary>
public class ShipWorldHealthBar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 1.2f, 0);

    private ShipHealth shipHealth;
    private Slider hullSlider;
    private Image shieldImg;
    private Image weaponImg;
    private Image engineImg;

    private void Awake()
    {
        shipHealth = GetComponent<ShipHealth>();
        CreateWorldUI();
    }

    private void OnEnable()
    {
        if (shipHealth != null)
            shipHealth.OnDamageTaken += UpdateUI;
    }

    private void OnDisable()
    {
        if (shipHealth != null)
            shipHealth.OnDamageTaken -= UpdateUI;
    }

    private void Start() => UpdateUI();

    private void UpdateUI()
    {
        if (shipHealth == null || shipHealth.Data == null) return;

        if (hullSlider != null)
        {
            hullSlider.maxValue = shipHealth.Data.maxHullHP;
            hullSlider.value = shipHealth.Data.currentHullHP;
        }

        UpdateModuleIcon(shieldImg, ModuleType.Shield);
        UpdateModuleIcon(weaponImg, ModuleType.Weapon);
        UpdateModuleIcon(engineImg, ModuleType.Engine);
    }

    private void UpdateModuleIcon(Image img, ModuleType type)
    {
        if (img == null) return;
        var mod = shipHealth.Data.GetModule(type);
        if (mod == null) return;

        // Если модуль уничтожен — красный, если повреждён — жёлтый, цел — зелёный
        if (mod.isDestroyed) img.color = Color.red;
        else if (mod.currentHP < mod.maxHP) img.color = Color.yellow;
        else img.color = Color.green;
    }

    private void CreateWorldUI()
    {
        var canvasGO = new GameObject("WorldCanvas", typeof(Canvas));
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = offset;

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        // Используем стандартный пиксельный размер Canvas и уменьшаем его масштабом (Scale)
        var rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150f, 40f);
        rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Корпус Slider
        var sliderGO = new GameObject("HullBar", typeof(Slider));
        sliderGO.transform.SetParent(canvasGO.transform, false);
        hullSlider = sliderGO.GetComponent<Slider>();
        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0.45f);
        sliderRect.anchorMax = new Vector2(1, 1f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Фон слайдера
        var bg = new GameObject("BG", typeof(Image)).GetComponent<Image>();
        bg.transform.SetParent(sliderGO.transform, false);
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = Vector2.zero;
        bg.rectTransform.offsetMax = Vector2.zero;

        // Заполнение слайдера
        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(fillArea.transform, false);
        fill.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Зеленый цвет здоровья вместо Cyan
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        hullSlider.fillRect = fill.rectTransform;
        hullSlider.targetGraphic = fill;
        hullSlider.minValue = 0;
        hullSlider.maxValue = 100;
        hullSlider.value = 100;

        // Индикаторы модулей (3 точки снизу: Shield, Weapon, Engine)
        var modulesPanel = new GameObject("Modules", typeof(RectTransform));
        modulesPanel.transform.SetParent(canvasGO.transform, false);
        var modRect = modulesPanel.GetComponent<RectTransform>();
        modRect.anchorMin = new Vector2(0, 0);
        modRect.anchorMax = new Vector2(1, 0.35f);
        modRect.offsetMin = Vector2.zero;
        modRect.offsetMax = Vector2.zero;

        shieldImg = CreateDot(modulesPanel.transform, "S", new Vector2(0.2f, 0.5f));
        weaponImg = CreateDot(modulesPanel.transform, "W", new Vector2(0.5f, 0.5f));
        engineImg = CreateDot(modulesPanel.transform, "E", new Vector2(0.8f, 0.5f));
    }

    private Image CreateDot(Transform parent, string label, Vector2 pos)
    {
        var dot = new GameObject($"Mod_{label}", typeof(Image)).GetComponent<Image>();
        dot.transform.SetParent(parent, false);
        dot.rectTransform.anchorMin = pos;
        dot.rectTransform.anchorMax = pos;
        dot.rectTransform.sizeDelta = new Vector2(16f, 12f);
        dot.color = Color.green;
        return dot;
    }
}