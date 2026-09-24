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
        var canvasGO = new GameObject("WorldCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = offset;

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1.5f, 0.4f);
        rect.localScale = Vector3.one;

        // Корпус Slider
        var sliderGO = new GameObject("HullBar", typeof(Slider));
        sliderGO.transform.SetParent(canvasGO.transform, false);
        hullSlider = sliderGO.GetComponent<Slider>();
        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0.5f);
        sliderRect.anchorMax = new Vector2(1, 1f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Фон слайдера
        var bg = new GameObject("BG", typeof(Image)).GetComponent<Image>();
        bg.transform.SetParent(sliderGO.transform, false);
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;

        // Заполнение слайдера
        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;

        var fill = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(fillArea.transform, false);
        fill.color = Color.cyan;
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        hullSlider.fillRect = fill.rectTransform;

        // Индикаторы модулей (3 точки снизу: Shield, Weapon, Engine)
        var modulesPanel = new GameObject("Modules", typeof(RectTransform));
        modulesPanel.transform.SetParent(canvasGO.transform, false);
        var modRect = modulesPanel.GetComponent<RectTransform>();
        modRect.anchorMin = new Vector2(0, 0);
        modRect.anchorMax = new Vector2(1, 0.4f);
        modRect.offsetMin = Vector2.zero;
        modRect.offsetMax = Vector2.zero;

        shieldImg = CreateDot(modulesPanel.transform, "S", new Vector2(0.15f, 0.5f));
        weaponImg = CreateDot(modulesPanel.transform, "W", new Vector2(0.50f, 0.5f));
        engineImg = CreateDot(modulesPanel.transform, "E", new Vector2(0.85f, 0.5f));
    }

    private Image CreateDot(Transform parent, string label, Vector2 pos)
    {
        var dot = new GameObject($"Mod_{label}", typeof(Image)).GetComponent<Image>();
        dot.transform.SetParent(parent, false);
        dot.rectTransform.anchorMin = pos;
        dot.rectTransform.anchorMax = pos;
        dot.rectTransform.sizeDelta = new Vector2(0.2f, 0.15f);
        dot.color = Color.green;
        return dot;
    }
}