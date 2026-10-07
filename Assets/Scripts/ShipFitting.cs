using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Поддержка New Input System
#endif

public enum SlotGroup
{
    Top_Weapons,      // 1 Группа: Верхние слоты (Оружие) - 4 шт.
    Center_Shields,   // 3 Группа: Центральные слоты (Защита / Щиты) - 4 шт.
    Bottom_Engines,   // 2 Группа: Нижние слоты (Двигатели / Активки) - 4 шт.
    Inventory         // Свободный инвентарь справа
}

public class ShipFitting : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Текстура силуэта корабля в центре")]
    public Texture2D shipSilhouetteTexture;

    [Header("Hotkeys")]
    public KeyCode toggleKey = KeyCode.I;

    // Слоты корабля (по 4 слота в каждой группе)
    private FittingItem[] topSlots = new FittingItem[4];
    private FittingItem[] centerSlots = new FittingItem[4];
    private FittingItem[] bottomSlots = new FittingItem[4];

    // Инвентарь неактивного оборудования
    private List<FittingItem> inventory = new List<FittingItem>();

    // Переменные системы Drag & Drop
    private FittingItem draggedItem = null;
    private SlotGroup dragSourceGroup;
    private int dragSourceIndex = -1;

    private bool showFittingWindow = false;
    private Texture2D whiteTexture;
    private GUIStyle headerStyle;
    private GUIStyle slotStyle;
    private GUIStyle itemStyle;

    private ShipWeapon shipWeapon;
    private ShipMovement shipMovement;
    private ShipHealth shipHealth;

    private void Awake()
    {
        shipWeapon = GetComponent<ShipWeapon>();
        shipMovement = GetComponent<ShipMovement>();
        shipHealth = GetComponent<ShipHealth>();

        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();

        InitDefaultInventory();
    }

    private void Start()
    {
        RecalculateShipStats();
    }

    private void InitDefaultInventory()
    {
        inventory.Add(new FittingItem("w1", "Лазер T1", SlotGroup.Top_Weapons, new Color(1f, 0.3f, 0.3f), "Оружие: Урон +15", damage: 15f, visual: WeaponVisualType.Laser));
        inventory.Add(new FittingItem("w2", "Плазма T2", SlotGroup.Top_Weapons, new Color(1f, 0.1f, 0.5f), "Оружие: Урон +30", damage: 30f, visual: WeaponVisualType.Plasma));
        inventory.Add(new FittingItem("w3", "Рейлган T1", SlotGroup.Top_Weapons, new Color(1f, 0.5f, 0.2f), "Оружие: Урон +20", damage: 20f, visual: WeaponVisualType.Laser));

        inventory.Add(new FittingItem("s1", "Легкий Щит", SlotGroup.Center_Shields, new Color(0.2f, 0.6f, 1f), "Защита: Щит +30 HP", shield: 30f, mass: 0.2f));
        inventory.Add(new FittingItem("s2", "Тяжелый Щит", SlotGroup.Center_Shields, new Color(0.3f, 0.9f, 0.4f), "Защита: Щит +80 HP", shield: 80f, mass: 1.0f));

        inventory.Add(new FittingItem("e1", "Форсаж M1", SlotGroup.Bottom_Engines, new Color(0.9f, 0.8f, 0.2f), "Двиг: Скорость +3", speed: 3f, accel: 6f));
        inventory.Add(new FittingItem("e2", "Тяжелый Двиг", SlotGroup.Bottom_Engines, new Color(0.8f, 0.4f, 1f), "Двиг: Ускорение +10", speed: 1f, accel: 10f, mass: 0.5f));
    }

    private void Update()
    {
        // Поддержка как New Input System, так и Old Input Manager
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            showFittingWindow = !showFittingWindow;
        }
#else
        if (Input.GetKeyDown(toggleKey))
        {
            showFittingWindow = !showFittingWindow;
        }
#endif
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);

        slotStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleCenter
        };

        itemStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        itemStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        InitStyles();

        // Переключатель сверху
        if (GUI.Button(new Rect(Screen.width / 2f - 50f, 10, 100, 25), "ФИТИНГ (I)"))
        {
            showFittingWindow = !showFittingWindow;
        }

        if (!showFittingWindow) return;

        float winWidth = 720f;
        float winHeight = 480f;
        Rect mainRect = new Rect((Screen.width - winWidth) * 0.5f, (Screen.height - winHeight) * 0.5f, winWidth, winHeight);

        DrawRect(mainRect, new Color(0.06f, 0.08f, 0.12f, 0.95f));
        GUI.Box(mainRect, "ПОСТРОЙКА И КОНФИГУРАЦИЯ КОРАБЛЯ");

        // --- КНОПКА ЗАКРЫТИЯ (X) ---
        if (GUI.Button(new Rect(mainRect.x + mainRect.width - 28, mainRect.y + 4, 24, 20), "X"))
        {
            showFittingWindow = false;
        }

        // 1. ЛЕВАЯ ЧАСТЬ: КРУГ С КОРАБЛЕМ И СЛОТАМИ
        Rect shipAreaRect = new Rect(mainRect.x + 15, mainRect.y + 35, 440, 430);
        DrawRect(shipAreaRect, new Color(0.12f, 0.15f, 0.2f, 0.6f));

        Vector2 center = new Vector2(shipAreaRect.x + shipAreaRect.width * 0.5f, shipAreaRect.y + shipAreaRect.height * 0.5f);
        DrawCircleFrame(center, 135f, new Color(0.2f, 0.5f, 0.8f, 0.35f));

        Rect shipImgRect = new Rect(center.x - 50, center.y - 60, 100, 120);
        if (shipSilhouetteTexture != null)
        {
            GUI.DrawTexture(shipImgRect, shipSilhouetteTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            DrawRect(shipImgRect, new Color(0.2f, 0.3f, 0.4f, 0.4f));
            GUI.Label(shipImgRect, "КОРАБЛЬ", headerStyle);
        }

        // --- ВЕРХНЯЯ ГРУППА (4 слота - Оружие) ---
        GUI.Label(new Rect(shipAreaRect.x, center.y - 180, shipAreaRect.width, 20), "1 ГРУППА: ОРУЖИЕ (ВЕРХ)", headerStyle);
        DrawSlotRow(center.x, center.y - 155, topSlots, SlotGroup.Top_Weapons);

        // --- ЦЕНТРАЛЬНАЯ ГРУППА (4 слота - Защита / Щиты) ---
        GUI.Label(new Rect(shipAreaRect.x, center.y - 85, shipAreaRect.width, 20), "3 ГРУППА: ЗАЩИТА (ЦЕНТР)", headerStyle);
        DrawSlotRow(center.x, center.y - 60, centerSlots, SlotGroup.Center_Shields);

        // --- НИЖНЯЯ ГРУППА (4 слота - Двигатели) ---
        GUI.Label(new Rect(shipAreaRect.x, center.y + 75, shipAreaRect.width, 20), "2 ГРУППА: ДВИГАТЕЛИ (НИЗ)", headerStyle);
        DrawSlotRow(center.x, center.y + 100, bottomSlots, SlotGroup.Bottom_Engines);

        // 2. ПРАВАЯ ЧАСТЬ: ИНВЕНТАРЬ
        Rect invRect = new Rect(mainRect.x + 465, mainRect.y + 35, 240, 430);
        DrawInventoryPanel(invRect);

        // 3. DRAG & DROP
        HandleDragAndDrop();
    }

    private void DrawSlotRow(float centerX, float startY, FittingItem[] slotArray, SlotGroup group)
    {
        float slotSize = 54f;
        float spacing = 10f;
        float totalWidth = (slotSize * 4) + (spacing * 3);
        float startX = centerX - (totalWidth * 0.5f);

        for (int i = 0; i < 4; i++)
        {
            Rect slotRect = new Rect(startX + i * (slotSize + spacing), startY, slotSize, slotSize);
            DrawSlot(slotRect, slotArray, i, group);
        }
    }

    private void DrawSlot(Rect rect, FittingItem[] slotArray, int index, SlotGroup group)
    {
        FittingItem item = slotArray[index];
        Event evt = Event.current;

        DrawRect(rect, new Color(0.15f, 0.18f, 0.25f, 0.9f));

        if (item != null)
        {
            Color oldColor = GUI.color;
            GUI.color = item.iconColor;
            GUI.Box(rect, item.name, itemStyle);
            GUI.color = oldColor;

            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition) && draggedItem == null)
            {
                draggedItem = item;
                dragSourceGroup = group;
                dragSourceIndex = index;
                slotArray[index] = null;
                evt.Use();
            }
        }
        else
        {
            GUI.Box(rect, $"[Слот {index + 1}]", slotStyle);
        }

        if (evt.type == EventType.MouseUp && rect.Contains(evt.mousePosition) && draggedItem != null)
        {
            if (draggedItem.allowedGroup == group)
            {
                FittingItem temp = slotArray[index];
                slotArray[index] = draggedItem;

                if (temp != null)
                {
                    ReturnItemToSource(temp);
                }

                draggedItem = null;
                RecalculateShipStats();
                evt.Use();
            }
        }
    }

    private void DrawInventoryPanel(Rect invRect)
    {
        DrawRect(invRect, new Color(0.09f, 0.11f, 0.16f, 0.9f));
        GUI.Label(new Rect(invRect.x, invRect.y + 5, invRect.width, 20), "ИНВЕНТАРЬ МОДУЛЕЙ", headerStyle);

        Event evt = Event.current;
        float itemHeight = 42f;
        float startY = invRect.y + 30;

        for (int i = 0; i < inventory.Count; i++)
        {
            Rect itemRect = new Rect(invRect.x + 8, startY + i * (itemHeight + 5), invRect.width - 16, itemHeight);
            FittingItem item = inventory[i];

            Color oldColor = GUI.color;
            GUI.color = item.iconColor;
            GUI.Box(itemRect, $"{item.name}\n<size=8>{item.description}</size>", itemStyle);
            GUI.color = oldColor;

            if (evt.type == EventType.MouseDown && itemRect.Contains(evt.mousePosition) && draggedItem == null)
            {
                draggedItem = item;
                dragSourceGroup = SlotGroup.Inventory;
                dragSourceIndex = i;
                inventory.RemoveAt(i);
                evt.Use();
                break;
            }
        }

        if (evt.type == EventType.MouseUp && invRect.Contains(evt.mousePosition) && draggedItem != null)
        {
            inventory.Add(draggedItem);
            draggedItem = null;
            RecalculateShipStats();
            evt.Use();
        }
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;

        if (draggedItem != null)
        {
            Rect dragRect = new Rect(evt.mousePosition.x - 35, evt.mousePosition.y - 20, 70, 40);
            Color oldColor = GUI.color;
            GUI.color = draggedItem.iconColor;
            GUI.Box(dragRect, draggedItem.name, itemStyle);
            GUI.color = oldColor;

            if (evt.type == EventType.MouseUp)
            {
                ReturnItemToSource(draggedItem);
                draggedItem = null;
                RecalculateShipStats();
                evt.Use();
            }
        }
    }

    private void ReturnItemToSource(FittingItem item)
    {
        if (dragSourceGroup == SlotGroup.Inventory)
            inventory.Add(item);
        else if (dragSourceGroup == SlotGroup.Top_Weapons)
            topSlots[dragSourceIndex] = item;
        else if (dragSourceGroup == SlotGroup.Center_Shields)
            centerSlots[dragSourceIndex] = item;
        else if (dragSourceGroup == SlotGroup.Bottom_Engines)
            bottomSlots[dragSourceIndex] = item;
    }

    private void RecalculateShipStats()
    {
        float totalDamage = 0f;
        float totalCooldownAdd = 0f;
        float totalShieldHP = 0f;
        float totalSpeedAdd = 0f;
        float totalAccelAdd = 0f;
        float totalMassAdd = 0f;

        List<FittingItem> installedWeapons = new List<FittingItem>();

        // Сбор установленного оружия из верхних слотов
        foreach (var weapon in topSlots)
        {
            if (weapon != null)
            {
                installedWeapons.Add(weapon);
            }
        }

        ProcessSlotStats(topSlots, ref totalDamage, ref totalCooldownAdd, ref totalShieldHP, ref totalSpeedAdd, ref totalAccelAdd, ref totalMassAdd);
        ProcessSlotStats(centerSlots, ref totalDamage, ref totalCooldownAdd, ref totalShieldHP, ref totalSpeedAdd, ref totalAccelAdd, ref totalMassAdd);
        ProcessSlotStats(bottomSlots, ref totalDamage, ref totalCooldownAdd, ref totalShieldHP, ref totalSpeedAdd, ref totalAccelAdd, ref totalMassAdd);

        // Передаем установленные пушки в скрипт стрельбы
        if (shipWeapon != null)
        {
            shipWeapon.SetEquippedWeapons(installedWeapons);
        }

        if (shipMovement != null)
        {
            shipMovement.maxSpeed = 5f + totalSpeedAdd;
            shipMovement.acceleration = 12f + totalAccelAdd;
        }

        if (shipHealth != null && shipHealth.Data != null)
        {
            shipHealth.Data.maxShieldHP = 50f + totalShieldHP;
            shipHealth.Data.currentShieldHP = Mathf.Min(shipHealth.Data.currentShieldHP, shipHealth.Data.maxShieldHP);
            shipHealth.Data.mass = Mathf.Max(0.5f, 2.0f + totalMassAdd);

            if (shipMovement != null)
            {
                shipMovement.ApplyMassFromData();
            }
        }
    }

    private void ProcessSlotStats(FittingItem[] slots, ref float dmg, ref float cd, ref float shield, ref float speed, ref float accel, ref float mass)
    {
        foreach (var item in slots)
        {
            if (item != null)
            {
                dmg += item.damageBonus;
                cd += item.cooldownBonus;
                shield += item.shieldBonus;
                speed += item.speedBonus;
                accel += item.accelBonus;
                mass += item.massBonus;
            }
        }
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = old;
    }

    private void DrawCircleFrame(Vector2 center, float radius, Color color)
    {
        int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float a1 = (i / (float)segments) * Mathf.PI * 2f;
            Vector2 p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            DrawRect(new Rect(p1.x, p1.y, 3, 3), color);
        }
    }
}