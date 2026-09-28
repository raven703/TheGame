using UnityEngine;

public enum SlotGroup
{
    Group1_Weapons,     // Верхние слоты: Оружие
    Group2_Engines,     // Нижние слоты: Двигатели и активные способности
    Group3_Shields      // Центральные слоты: Защитные и вспомогательные модули
}

public class ShipFitting : MonoBehaviour
{
    [Header("Equipped Config")]
    public string weaponPreset = "Лазерный Резак";
    public string enginePreset = "Форсаж + Маневровые";
    public string shieldPreset = "Стандартный Щит";

    private bool showFittingWindow = false;

    private ShipWeapon shipWeapon;
    private ShipMovement shipMovement;
    private ShipHealth shipHealth;

    private void Awake()
    {
        shipWeapon = GetComponent<ShipWeapon>();
        shipMovement = GetComponent<ShipMovement>();
        shipHealth = GetComponent<ShipHealth>();
    }

    private void OnGUI()
    {
        // Кнопка открытия Фитинга вверху
        if (GUI.Button(new Rect(Screen.width / 2f - 50f, 10, 100, 25), "ФИТИНГ"))
        {
            showFittingWindow = !showFittingWindow;
        }

        if (!showFittingWindow) return;

        Rect winRect = new Rect(Screen.width / 2f - 200f, Screen.height / 2f - 160f, 400, 320);
        GUI.Box(winRect, "ПОСТРОЙКА И КОНФИГУРАЦИЯ КОРАБЛЯ");

        GUILayout.BeginArea(new Rect(winRect.x + 15, winRect.y + 35, winRect.width - 30, winRect.height - 40));

        // 1 ГРУППА - ВЕРХНИЕ СЛОТЫ (ОРУЖИЕ)
        GUILayout.Label("1 ГРУППА: Верхние слоты (Оружие)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Лазер (Урон 15 / Скорострельность 1.5s)")) ApplyWeapon(15f, 1.5f, "Лазер");
        if (GUILayout.Button("Плазма (Урон 30 / Скорострельность 3.0s)")) ApplyWeapon(30f, 3.0f, "Плазма");
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 2 ГРУППА - НИЖНИЕ СЛОТЫ (ДВИГАТЕЛИ И АКТИВКИ)
        GUILayout.Label("2 ГРУППА: Нижние слоты (Двигатели / Способности)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Скоростной (Макс. скор. 8 / Ускор. 18)")) ApplyEngine(8f, 18f, "Скоростной");
        if (GUILayout.Button("Тяжелый (Макс. скор. 4 / Ускор. 8)")) ApplyEngine(4f, 8f, "Тяжелый");
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 3 ГРУППА - ЦЕНТРАЛЬНЫЕ СЛОТЫ (ЗАЩИТА)
        GUILayout.Label("3 ГРУППА: Центральные слоты (Защита)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Легкий Щит (30 HP)")) ApplyShield(30f, "Легкий");
        if (GUILayout.Button("Тяжелый Щит (80 HP)")) ApplyShield(80f, "Тяжелый");
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        if (GUILayout.Button("ЗАКРЫТЬ", GUILayout.Height(30)))
        {
            showFittingWindow = false;
        }

        GUILayout.EndArea();
    }

    private void ApplyWeapon(float damage, float cd, string name)
    {
        if (shipWeapon != null)
        {
            shipWeapon.damage = damage;
            shipWeapon.cooldown = cd;
            weaponPreset = name;
            Debug.Log($"[ФИТИНГ] Установлено оружие: {name}");
        }
    }

    private void ApplyEngine(float speed, float accel, string name)
    {
        if (shipMovement != null)
        {
            shipMovement.maxSpeed = speed;
            shipMovement.acceleration = accel;
            enginePreset = name;
            Debug.Log($"[ФИТИНГ] Установлен двигатель: {name}");
        }
    }

    private void ApplyShield(float shieldHP, string name)
    {
        if (shipHealth != null && shipHealth.Data != null)
        {
            shipHealth.Data.maxShieldHP = shieldHP;
            shipHealth.Data.currentShieldHP = shieldHP;
            shieldPreset = name;
            Debug.Log($"[ФИТИНГ] Установлен щит: {name}");
        }
    }
}