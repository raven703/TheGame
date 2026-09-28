using UnityEngine;

public class TacticalCommandPanel : MonoBehaviour
{
    [Header("Ship References")]
    public ShipWeapon playerWeapon;
    public ShipAbilities playerAbilities;

    private GUIStyle btnNormalStyle;
    private GUIStyle btnActiveStyle;
    private GUIStyle headerStyle;
    private Texture2D whiteTexture;

    private void Awake()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    private void InitStyles()
    {
        if (btnNormalStyle != null) return;

        btnNormalStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        btnActiveStyle = new GUIStyle(btnNormalStyle);
        btnActiveStyle.normal.textColor = Color.yellow;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
    }

    private void OnGUI()
    {
        if (playerWeapon == null) return;
        InitStyles();

        // Центрируем панель снизу экрана
        float panelWidth = 640f;
        float panelHeight = 65f;
        float startX = (Screen.width - panelWidth) * 0.5f;
        float startY = Screen.height - panelHeight - 10f;

        Rect panelRect = new Rect(startX, startY, panelWidth, panelHeight);

        // Полупрозрачная плашка
        Color oldColor = GUI.color;
        GUI.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        GUI.DrawTexture(panelRect, whiteTexture);
        GUI.color = oldColor;

        GUILayout.BeginArea(panelRect);
        GUILayout.BeginHorizontal();

        // --- БЛОК 1: ПОВЕДЕНИЕ (3 КНОПКИ) ---
        GUILayout.BeginVertical(GUILayout.Width(180));
        GUILayout.Label("ПОВЕДЕНИЕ", headerStyle);
        GUILayout.BeginHorizontal();
        DrawBehaviorBtn("Орбита", CombatBehavior.Orbit);
        DrawBehaviorBtn("Кайт", CombatBehavior.KeepDistance);
        DrawBehaviorBtn("Фланг", CombatBehavior.Flank);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // --- БЛОК 2: ФОКУС ЦЕЛИ (4 КНОПКИ) ---
        GUILayout.BeginVertical(GUILayout.Width(220));
        GUILayout.Label("ПРИОРИТЕТ ЦЕЛИ", headerStyle);
        GUILayout.BeginHorizontal();
        DrawTargetBtn("Авто", null);
        DrawTargetBtn("Пушка", ModuleType.Weapon);
        DrawTargetBtn("Щит", ModuleType.Shield);
        DrawTargetBtn("Двиг", ModuleType.Engine);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // --- БЛОК 3: АКТИВНЫЕ СПОСОБНОСТИ (2 ГРУППА) ---
        GUILayout.BeginVertical(GUILayout.Width(200));
        GUILayout.Label("СПОСОБНОСТИ (ГР. 2)", headerStyle);
        GUILayout.BeginHorizontal();
        DrawAbilityBtn("Форсаж", playerAbilities ? playerAbilities.afterburnerCooldown : 0f, () => playerAbilities?.UseAfterburner());
        DrawAbilityBtn("Щит+", playerAbilities ? playerAbilities.shieldBoostCooldown : 0f, () => playerAbilities?.UseShieldBoost());
        DrawAbilityBtn("РЭБ", playerAbilities ? playerAbilities.emPulseCooldown : 0f, () => playerAbilities?.UseEMPulse());
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawBehaviorBtn(string label, CombatBehavior behavior)
    {
        bool isActive = playerWeapon.behavior == behavior;
        GUIStyle style = isActive ? btnActiveStyle : btnNormalStyle;
        if (GUILayout.Button(label, style, GUILayout.Height(32)))
        {
            playerWeapon.SetBehavior(behavior);
        }
    }

    private void DrawTargetBtn(string label, ModuleType? module)
    {
        bool isActive = playerWeapon.targetedModule == module;
        GUIStyle style = isActive ? btnActiveStyle : btnNormalStyle;
        if (GUILayout.Button(label, style, GUILayout.Height(32)))
        {
            playerWeapon.SetTargetModule(module);
        }
    }

    private void DrawAbilityBtn(string label, float cd, System.Action onClick)
    {
        string display = cd > 0f ? $"{label}\n({cd:F0}s)" : label;
        GUI.enabled = cd <= 0f;
        if (GUILayout.Button(display, btnNormalStyle, GUILayout.Height(32)))
        {
            onClick?.Invoke();
        }
        GUI.enabled = true;
    }
}