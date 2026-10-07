using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum SlotGroup
{
    Top_Weapons,      // 1 Группа: Верхние слоты (Оружие)
    Center_Shields,   // 3 Группа: Центральные слоты (Защита / Щиты)
    Bottom_Engines,   // 2 Группа: Нижние слоты (Двигатели / Активки)
    Inventory         // Свободный инвентарь справа
}

public class ShipFitting : MonoBehaviour
{
    [Header("Ship Definition")]
    [Tooltip("Корабль, который редактируется по умолчанию (для UI-объекта).")]
    public ShipDefinition shipDefinition;

    [Tooltip("Справочник всех кораблей в игре (для селектора в UI).")]
    public ShipLibrary shipLibrary;

    [Header("Visuals")]
    [Tooltip("Текстура силуэта корабля в центре")]
    public Texture2D shipSilhouetteTexture;

    [Header("Hotkeys")]
    public KeyCode toggleKey = KeyCode.I;

    // Слоты (списки, чтобы совпадать с ShipLoadout)
    private List<FittingItem> topSlots = new List<FittingItem>();
    private List<FittingItem> centerSlots = new List<FittingItem>();
    private List<FittingItem> bottomSlots = new List<FittingItem>();
    private List<FittingItem> inventory = new List<FittingItem>();

    // Текущий редактируемый loadout
    private ShipLoadout currentLoadout;

    // Drag & Drop
    private FittingItem draggedItem = null;
    private SlotGroup dragSourceGroup;
    private int dragSourceIndex = -1;

    private bool showFittingWindow = false;
    private Texture2D whiteTexture;
    private GUIStyle headerStyle;
    private GUIStyle slotStyle;
    private GUIStyle itemStyle;

    // Корабль игрока, к которому применяются статы
    private ShipHealth targetShipHealth;
    private ShipMovement targetShipMovement;
    private ShipWeapon targetShipWeapon;

    private void Awake()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    private void Start()
    {
        ResolveTargetShip();

        // Загружаем loadout для стартового shipDefinition
        if (shipDefinition != null)
            LoadLoadoutFor(shipDefinition);

        ApplyLoadoutToShip();
    }

    /// <summary>
    /// Ищет корабль игрока (тег "Player") и кэширует ссылки на его компоненты.
    /// </summary>
    private void ResolveTargetShip()
    {
        // Если ShipFitting случайно остался на корабле — используем его.
        var selfHealth = GetComponent<ShipHealth>();
        if (selfHealth != null)
        {
            targetShipHealth = selfHealth;
            targetShipMovement = GetComponent<ShipMovement>();
            targetShipWeapon = GetComponent<ShipWeapon>();
            return;
        }

        // Иначе — ищем по тегу "Player".
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            targetShipHealth = playerObj.GetComponent<ShipHealth>();
            targetShipMovement = playerObj.GetComponent<ShipMovement>();
            targetShipWeapon = playerObj.GetComponent<ShipWeapon>();
        }
        else
        {
            Debug.LogWarning("[ShipFitting] Не найден объект с тегом 'Player'. Применение loadout не сработает.");
        }
    }

    // ==================== LOADOUT ====================

    private void LoadLoadoutFor(ShipDefinition def)
    {
        currentLoadout = ShipLoadoutManager.GetLoadout(def.shipId);

        if (currentLoadout == null)
        {
            // Сохранённого нет — пустой loadout + дефолтный инвентарь (чтобы было что ставить).
            currentLoadout = new ShipLoadout(def.shipId);
            PopulateDefaultInventory(currentLoadout);
        }

        SyncFromLoadout();
    }

    private void SyncFromLoadout()
    {
        topSlots.Clear();
        centerSlots.Clear();
        bottomSlots.Clear();
        inventory.Clear();

        if (currentLoadout == null) return;

        if (currentLoadout.weaponSlots != null) topSlots.AddRange(currentLoadout.weaponSlots);
        if (currentLoadout.shieldSlots != null) centerSlots.AddRange(currentLoadout.shieldSlots);
        if (currentLoadout.engineSlots != null) bottomSlots.AddRange(currentLoadout.engineSlots);
        if (currentLoadout.inventory != null) inventory.AddRange(currentLoadout.inventory);
    }

    private void SyncToLoadout()
    {
        if (currentLoadout == null) return;

        currentLoadout.weaponSlots = new List<FittingItem>(topSlots);
        currentLoadout.shieldSlots = new List<FittingItem>(centerSlots);
        currentLoadout.engineSlots = new List<FittingItem>(bottomSlots);
        currentLoadout.inventory = new List<FittingItem>(inventory);
    }

    private void PopulateDefaultInventory(ShipLoadout loadout)
    {
        loadout.inventory.Add(new FittingItem("w1", "Лазер T1", SlotGroup.Top_Weapons, new Color(1f, 0.3f, 0.3f), "Оружие: Урон +15", damage: 15f, visual: WeaponVisualType.Laser));
        loadout.inventory.Add(new FittingItem("w2", "Плазма T2", SlotGroup.Top_Weapons, new Color(1f, 0.1f, 0.5f), "Оружие: Урон +30", damage: 30f, visual: WeaponVisualType.Plasma));
        loadout.inventory.Add(new FittingItem("w3", "Рейлган T1", SlotGroup.Top_Weapons, new Color(1f, 0.5f, 0.2f), "Оружие: Урон +20", damage: 20f, visual: WeaponVisualType.Laser));

        loadout.inventory.Add(new FittingItem("s1", "Легкий Щит", SlotGroup.Center_Shields, new Color(0.2f, 0.6f, 1f), "Защита: Щит +30 HP", shield: 30f, mass: 0.2f));
        loadout.inventory.Add(new FittingItem("s2", "Тяжелый Щит", SlotGroup.Center_Shields, new Color(0.3f, 0.9f, 0.4f), "Защита: Щит +80 HP", shield: 80f, mass: 1.0f));

        loadout.inventory.Add(new FittingItem("e1", "Форсаж M1", SlotGroup.Bottom_Engines, new Color(0.9f, 0.8f, 0.2f), "Двиг: Скорость +3", speed: 3f, accel: 6f));
        loadout.inventory.Add(new FittingItem("e2", "Тяжелый Двиг", SlotGroup.Bottom_Engines, new Color(0.8f, 0.4f, 1f), "Двиг: Ускорение +10", speed: 1f, accel: 10f, mass: 0.5f));
    }

    private void SaveCurrentLoadout()
    {
        if (currentLoadout == null) return;
        SyncToLoadout();
        ShipLoadoutManager.SaveLoadout(currentLoadout);
    }

    // ==================== APPLY TO SHIP ====================

    private void ApplyLoadoutToShip()
    {
        if (currentLoadout == null) return;
        RecalculateShipStats();
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

        foreach (var weapon in topSlots)
        {
            if (weapon != null)
                installedWeapons.Add(weapon);
        }

        ProcessSlotStats(topSlots, ref totalDamage, ref totalCooldownAdd, ref totalShieldHP, ref totalSpeedAdd, ref totalAccelAdd, ref totalMassAdd);
        ProcessSlotStats(centerSlots, ref totalDamage, ref totalCooldownAdd, ref totalShieldHP, ref totalSpeedAdd, ref totalAccelAdd, ref totalMassAdd);
        ProcessSlotStats(bottomSlots, ref totalDamage, ref totalCooldownAdd, ref totalShieldHP, ref totalSpeedAdd, ref totalAccelAdd, ref totalMassAdd);

        // Оружие → ShipWeapon
        if (targetShipWeapon != null)
            targetShipWeapon.SetEquippedWeapons(installedWeapons);

        // Базовые ТТХ — из ShipDefinition, иначе fallback
        float baseSpeed = shipDefinition != null ? shipDefinition.baseMaxSpeed : 5f;
        float baseAccel = shipDefinition != null ? shipDefinition.baseAcceleration : 12f;
        float baseShield = shipDefinition != null ? shipDefinition.baseShieldHP : 50f;
        float baseMass = shipDefinition != null ? shipDefinition.baseMass : 2f;

        if (targetShipMovement != null)
        {
            targetShipMovement.maxSpeed = baseSpeed + totalSpeedAdd;
            targetShipMovement.acceleration = baseAccel + totalAccelAdd;
        }

        if (targetShipHealth != null && targetShipHealth.Data != null)
        {
            targetShipHealth.Data.maxShieldHP = baseShield + totalShieldHP;
            targetShipHealth.Data.currentShieldHP = Mathf.Min(targetShipHealth.Data.currentShieldHP, targetShipHealth.Data.maxShieldHP);
            targetShipHealth.Data.mass = Mathf.Max(0.5f, baseMass + totalMassAdd);

            if (targetShipMovement != null)
                targetShipMovement.ApplyMassFromData();
        }
    }

    private void ProcessSlotStats(List<FittingItem> slots, ref float dmg, ref float cd, ref float shield, ref float speed, ref float accel, ref float mass)
    {
        foreach (var item in slots)
        {
            if (item == null) continue;
            dmg += item.damageBonus;
            cd += item.cooldownBonus;
            shield += item.shieldBonus;
            speed += item.speedBonus;
            accel += item.accelBonus;
            mass += item.massBonus;
        }
    }

    // ==================== INPUT ====================

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            showFittingWindow = !showFittingWindow;
#else
        if (Input.GetKeyDown(toggleKey))
            showFittingWindow = !showFittingWindow;
#endif
    }

    // ==================== UI ====================

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

        if (GUI.Button(new Rect(Screen.width / 2f - 50f, 10, 100, 25), "ФИТИНГ (I)"))
            showFittingWindow = !showFittingWindow;

        if (!showFittingWindow) return;

        float winWidth = 720f;
        float winHeight = 520f;
        Rect mainRect = new Rect((Screen.width - winWidth) * 0.5f, (Screen.height - winHeight) * 0.5f, winWidth, winHeight);

        DrawRect(mainRect, new Color(0.06f, 0.08f, 0.12f, 0.95f));
        GUI.Box(mainRect, "ПОСТРОЙКА И КОНФИГУРАЦИЯ КОРАБЛЯ");

        if (GUI.Button(new Rect(mainRect.x + mainRect.width - 28, mainRect.y + 4, 24, 20), "X"))
            showFittingWindow = false;

        // Селектор корабля
        DrawShipSelector(new Rect(mainRect.x + 15, mainRect.y + 30, mainRect.width - 30, 30));

        float contentY = mainRect.y + 70;

        // Левая часть: круг со слот-рядами
        Rect shipAreaRect = new Rect(mainRect.x + 15, contentY, 440, 430);
        DrawRect(shipAreaRect, new Color(0.12f, 0.15f, 0.2f, 0.6f));

        Vector2 center = new Vector2(shipAreaRect.x + shipAreaRect.width * 0.5f, shipAreaRect.y + shipAreaRect.height * 0.5f);
        DrawCircleFrame(center, 135f, new Color(0.2f, 0.5f, 0.8f, 0.35f));

        Rect shipImgRect = new Rect(center.x - 50, center.y - 60, 100, 120);
        if (shipSilhouetteTexture != null)
            GUI.DrawTexture(shipImgRect, shipSilhouetteTexture, ScaleMode.ScaleToFit);
        else
        {
            DrawRect(shipImgRect, new Color(0.2f, 0.3f, 0.4f, 0.4f));
            GUI.Label(shipImgRect, "КОРАБЛЬ", headerStyle);
        }

        GUI.Label(new Rect(shipAreaRect.x, center.y - 180, shipAreaRect.width, 20), "1 ГРУППА: ОРУЖИЕ (ВЕРХ)", headerStyle);
        DrawSlotRow(center.x, center.y - 155, topSlots, SlotGroup.Top_Weapons);

        GUI.Label(new Rect(shipAreaRect.x, center.y - 85, shipAreaRect.width, 20), "3 ГРУППА: ЗАЩИТА (ЦЕНТР)", headerStyle);
        DrawSlotRow(center.x, center.y - 60, centerSlots, SlotGroup.Center_Shields);

        GUI.Label(new Rect(shipAreaRect.x, center.y + 75, shipAreaRect.width, 20), "2 ГРУППА: ДВИГАТЕЛИ (НИЗ)", headerStyle);
        DrawSlotRow(center.x, center.y + 100, bottomSlots, SlotGroup.Bottom_Engines);

        // Правая часть: инвентарь
        Rect invRect = new Rect(mainRect.x + 465, contentY, 240, 430);
        DrawInventoryPanel(invRect);

        HandleDragAndDrop();
    }

    private void DrawShipSelector(Rect rect)
    {
        if (shipLibrary == null || shipLibrary.ships == null || shipLibrary.ships.Count == 0)
        {
            GUI.Label(rect, "Ship Library не назначен", headerStyle);
            return;
        }

        DrawRect(rect, new Color(0.1f, 0.12f, 0.18f, 0.9f));

        int count = shipLibrary.ships.Count;
        float btnWidth = Mathf.Min(140f, (rect.width - 8f) / count - 4f);
        float x = rect.x + 4f;

        foreach (var ship in shipLibrary.ships)
        {
            if (ship == null) continue;

            bool isActive = ship == shipDefinition;
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
            if (isActive)
                style.normal.textColor = Color.yellow;

            if (GUI.Button(new Rect(x, rect.y + 4f, btnWidth, rect.height - 8f), ship.displayName, style))
            {
                if (ship != shipDefinition)
                {
                    SaveCurrentLoadout();
                    shipDefinition = ship;
                    LoadLoadoutFor(shipDefinition);
                    ApplyLoadoutToShip();
                }
            }

            x += btnWidth + 4f;
        }
    }

    private void DrawSlotRow(float centerX, float startY, List<FittingItem> slotList, SlotGroup group)
    {
        float slotSize = 54f;
        float spacing = 10f;

        int maxSlots =
            group == SlotGroup.Top_Weapons ? (shipDefinition != null ? shipDefinition.weaponSlots : 4) :
            group == SlotGroup.Center_Shields ? (shipDefinition != null ? shipDefinition.shieldSlots : 4) :
                                                (shipDefinition != null ? shipDefinition.engineSlots : 4);

        float totalWidth = (slotSize * maxSlots) + (spacing * (maxSlots - 1));
        float startX = centerX - (totalWidth * 0.5f);

        // Дополняем список пустыми слотами до maxSlots (для UI)
        while (slotList.Count < maxSlots)
            slotList.Add(null);

        for (int i = 0; i < maxSlots; i++)
        {
            Rect slotRect = new Rect(startX + i * (slotSize + spacing), startY, slotSize, slotSize);
            DrawSlot(slotRect, slotList, i, group);
        }
    }

    private void DrawSlot(Rect rect, List<FittingItem> slotList, int index, SlotGroup group)
    {
        if (index >= slotList.Count) return;

        FittingItem item = slotList[index];
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
                slotList[index] = null;
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
                FittingItem temp = slotList[index];
                slotList[index] = draggedItem;

                if (temp != null)
                    ReturnItemToSource(temp);

                draggedItem = null;
                SaveCurrentLoadout();
                ApplyLoadoutToShip();
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
            SaveCurrentLoadout();
            ApplyLoadoutToShip();
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
                SaveCurrentLoadout();
                ApplyLoadoutToShip();
                evt.Use();
            }
        }
    }

    private void ReturnItemToSource(FittingItem item)
    {
        if (dragSourceGroup == SlotGroup.Inventory)
            inventory.Add(item);
        else if (dragSourceGroup == SlotGroup.Top_Weapons && dragSourceIndex < topSlots.Count)
            topSlots[dragSourceIndex] = item;
        else if (dragSourceGroup == SlotGroup.Center_Shields && dragSourceIndex < centerSlots.Count)
            centerSlots[dragSourceIndex] = item;
        else if (dragSourceGroup == SlotGroup.Bottom_Engines && dragSourceIndex < bottomSlots.Count)
            bottomSlots[dragSourceIndex] = item;
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