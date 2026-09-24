using UnityEngine;

/// <summary>
/// World-space mini healthbar & module status display drawn directly above each ship.
/// </summary>
[RequireComponent(typeof(ShipHealth))]
public class ShipWorldHealthBar : MonoBehaviour
{
    [Header("Position & Dimensions")]
    [Tooltip("Vertical offset above the ship in world units.")]
    public float verticalOffset = 0.8f;
    public float barWidth = 60f;
    public float barHeight = 6f;

    private ShipHealth shipHealth;
    private Camera mainCamera;

    // Встроенные цветные текстуры GUI
    private Texture2D bgTexture;
    private Texture2D shieldTexture;
    private Texture2D hullTexture;
    private Texture2D moduleOkTexture;
    private Texture2D moduleDestroyedTexture;
    private GUIStyle moduleTextStyle;

    private void Awake()
    {
        shipHealth = GetComponent<ShipHealth>();
        mainCamera = Camera.main;

        InitTextures();
    }

    private void InitTextures()
    {
        bgTexture = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f, 0.75f));
        shieldTexture = MakeTex(1, 1, new Color(0.2f, 0.6f, 1f, 0.9f));
        hullTexture = MakeTex(1, 1, new Color(0.2f, 0.8f, 0.2f, 0.9f));
        moduleOkTexture = MakeTex(1, 1, new Color(0.2f, 0.8f, 0.2f, 0.9f));
        moduleDestroyedTexture = MakeTex(1, 1, new Color(0.9f, 0.2f, 0.2f, 0.9f));

        moduleTextStyle = new GUIStyle
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }

    private void OnGUI()
    {
        if (shipHealth == null || shipHealth.Data == null || !gameObject.activeInHierarchy)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // Перевод позиции корабля из мировых координат в экранные координаты OnGUI
        Vector3 worldPos = transform.position + new Vector3(0f, verticalOffset, 0f);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // В OnGUI ось Y перевернута
        if (screenPos.z < 0) return; // Объект за камерой
        float guiX = screenPos.x - (barWidth * 0.5f);
        float guiY = Screen.height - screenPos.y;

        // 1. Полоска Щита
        if (shipHealth.Data.maxShieldHP > 0)
        {
            float shieldPct = Mathf.Clamp01(shipHealth.Data.currentShieldHP / shipHealth.Data.maxShieldHP);
            GUI.DrawTexture(new Rect(guiX, guiY, barWidth, barHeight), bgTexture);
            GUI.DrawTexture(new Rect(guiX, guiY, barWidth * shieldPct, barHeight), shieldTexture);
            guiY += barHeight + 2;
        }

        // 2. Полоска Корпуса (Hull)
        float hullPct = Mathf.Clamp01(shipHealth.Data.currentHullHP / shipHealth.Data.maxHullHP);
        GUI.DrawTexture(new Rect(guiX, guiY, barWidth, barHeight), bgTexture);
        GUI.DrawTexture(new Rect(guiX, guiY, barWidth * hullPct, barHeight), hullTexture);
        guiY += barHeight + 3;

        // 3. Плашки статуса 3 модулей (W = Weapon, S = Shield, E = Engine)
        DrawModuleBadge(guiX, guiY, "W", ModuleType.Weapon);
        DrawModuleBadge(guiX + 18, guiY, "S", ModuleType.Shield);
        DrawModuleBadge(guiX + 36, guiY, "E", ModuleType.Engine);
    }

    private void DrawModuleBadge(float x, float y, string label, ModuleType type)
    {
        var module = shipHealth.Data.GetModule(type);
        bool isOk = module != null && !module.isDestroyed;

        Texture2D badgeTex = isOk ? moduleOkTexture : moduleDestroyedTexture;
        Rect badgeRect = new Rect(x, y, 14, 12);

        GUI.DrawTexture(badgeRect, badgeTex);
        GUI.Label(badgeRect, label, moduleTextStyle);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}