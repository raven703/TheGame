using UnityEngine;

public class CombatHUDManager : MonoBehaviour
{
    [Header("Ship References")]
    public ShipHealth playerHealth;
    public ShipWeapon playerWeapon;

    [Header("Fallback Enemy")]
    public ShipHealth defaultEnemyHealth;

    [Header("UI Styling")]
    public Color hullColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color shieldColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color moduleOkColor = new Color(0.15f, 0.75f, 0.15f, 1f);
    public Color moduleDamagedColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    public Color moduleDestroyedColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle moduleTextStyle;
    private Texture2D whiteTexture;

    private void Awake()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft
        };

        moduleTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        moduleTextStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        InitStyles();

        // Фиксируем масштаб OnGUI независимо от Scale окна Game
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 1f));

        // Игрок слева
        if (playerHealth != null)
        {
            Rect playerRect = new Rect(15, 15, 200, 140);
            DrawPanel(playerRect, playerHealth, "ИГРОК", true);
        }

        // Враг справа
        ShipHealth enemyHealth = (playerWeapon != null && playerWeapon.Target != null)
            ? playerWeapon.Target
            : defaultEnemyHealth;

        if (enemyHealth != null)
        {
            Rect enemyRect = new Rect(Screen.width - 215, 15, 200, 140);
            DrawPanel(enemyRect, enemyHealth, "ЦЕЛЬ", false);
        }
    }

    private void DrawPanel(Rect rect, ShipHealth health, string header, bool isPlayer)
    {
        // Чёрный полупрозрачный фон панели
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = oldColor;

        float padding = 8f;
        float currentY = rect.y + padding;
        float width = rect.width - (padding * 2f);

        // Заголовок
        headerStyle.normal.textColor = isPlayer ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.4f, 0.4f);
        GUI.Label(new Rect(rect.x + padding, currentY, width, 18), header, headerStyle);
        currentY += 20f;

        if (health == null || health.Data == null) return;

        // Корпус HP
        float hullPct = Mathf.Clamp01(health.Data.currentHullHP / health.Data.maxHullHP);
        DrawBar(rect.x + padding, ref currentY, width, $"Корпус: {health.Data.currentHullHP:F0}/{health.Data.maxHullHP:F0}", hullPct, hullColor);

        // Щит HP
        var shieldMod = health.Data.GetModule(ModuleType.Shield);
        if (shieldMod != null)
        {
            float shieldPct = Mathf.Clamp01(shieldMod.currentHP / shieldMod.maxHP);
            DrawBar(rect.x + padding, ref currentY, width, $"Щит: {shieldMod.currentHP:F0}/{shieldMod.maxHP:F0}", shieldPct, shieldColor);
        }

        currentY += 4f;
        GUI.Label(new Rect(rect.x + padding, currentY, width, 16), "Модули:", labelStyle);
        currentY += 18f;

        // Модули
        float boxWidth = 32f;
        float boxHeight = 24f;
        float boxGap = 6f;

        DrawModuleBox(rect.x + padding, currentY, boxWidth, boxHeight, "W", health.Data.GetModule(ModuleType.Weapon));
        DrawModuleBox(rect.x + padding + boxWidth + boxGap, currentY, boxWidth, boxHeight, "S", health.Data.GetModule(ModuleType.Shield));
        DrawModuleBox(rect.x + padding + (boxWidth + boxGap) * 2, currentY, boxWidth, boxHeight, "E", health.Data.GetModule(ModuleType.Engine));
    }

    private void DrawBar(float x, ref float y, float width, string text, float fillPct, Color barColor)
    {
        labelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, width, 14), text, labelStyle);
        y += 14f;

        // Задний фон полосы
        Color oldColor = GUI.color;
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, width, 10), whiteTexture);

        // Заполнение полосы
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(x, y, width * fillPct, 10), whiteTexture);
        GUI.color = oldColor;

        y += 14f;
    }

    private void DrawModuleBox(float x, float y, float w, float h, string label, ShipModule module)
    {
        Color boxColor = moduleDestroyedColor;
        if (module != null && !module.isDestroyed)
        {
            float pct = module.currentHP / module.maxHP;
            boxColor = pct >= 0.99f ? moduleOkColor : moduleDamagedColor;
        }

        Color oldColor = GUI.color;

        // Отрисовка квадрата модуля
        GUI.color = boxColor;
        Rect boxRect = new Rect(x, y, w, h);
        GUI.DrawTexture(boxRect, whiteTexture);

        // Буква (W, S, E)
        GUI.color = Color.white;
        GUI.Label(boxRect, label, moduleTextStyle);

        GUI.color = oldColor;
    }
}