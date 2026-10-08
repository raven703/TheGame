using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-окно фитинга. Создаёт слоты и элементы инвентаря динамически,
/// хранит loadout, применяет статы к кораблю игрока, сохраняет через ShipLoadoutManager.
/// </summary>
public class FittingWindow : MonoBehaviour
{
    [Header("Ship Definition")]
    [Tooltip("Какой корабль редактируется по умолчанию.")]
    public ShipDefinition shipDefinition;

    [Tooltip("Справочник всех кораблей в игре (для селектора в UI).")]
    public ShipLibrary shipLibrary;

    [Header("Prefabs")]
    [Tooltip("Префаб одного слота.")]
    public FittingSlot slotPrefab;

    [Tooltip("Префаб одного элемента инвентаря.")]
    public FittingItemView itemViewPrefab;

    [Header("Containers — Weapon Slots")]
    public Transform weaponSlotsContainer;

    [Header("Containers — Shield Slots")]
    public Transform shieldSlotsContainer;

    [Header("Containers — Engine Slots")]
    public Transform engineSlotsContainer;

    [Header("Container — Inventory")]
    public Transform inventoryContainer;

    [Header("Ship Selector")]
    public Transform shipSelectorContainer;

    [Tooltip("Префаб кнопки выбора корабля.")]
    public Button shipButtonPrefab;

    [Header("UI References")]
    public Button closeButton;
    public TextMeshProUGUI titleLabel;

    [Tooltip("Слой, куда переносятся drag-объекты (обычно корень Canvas).")]
    public Transform dragLayer;

    [Header("Player Ship Target")]
    [Tooltip("Корабль игрока — к нему применяются статы. Если null — ищется по тегу 'Player'.")]
    public ShipHealth targetShipHealth;

    // Текущий loadout
    private ShipLoadout currentLoadout;

    // Список созданных слотов (для очистки)
    private List<FittingSlot> weaponSlots = new List<FittingSlot>();
    private List<FittingSlot> shieldSlots = new List<FittingSlot>();
    private List<FittingSlot> engineSlots = new List<FittingSlot>();

    // Список созданных элементов инвентаря
    private List<FittingItemView> itemViews = new List<FittingItemView>();

    // Кэш компонентов корабля игрока
    private ShipMovement targetShipMovement;
    private ShipWeapon targetShipWeapon;

    private void Awake()
    {
        ResolveTargetShip();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        if (shipDefinition != null)
            LoadLoadoutFor(shipDefinition);

        BuildShipSelector();
        BuildSlots();
        BuildInventory();
        ApplyLoadoutToShip();
    }

    // ==================== TARGET SHIP ====================

    private void ResolveTargetShip()
    {
        if (targetShipHealth == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                targetShipHealth = playerObj.GetComponent<ShipHealth>();
        }

        if (targetShipHealth != null)
        {
            targetShipMovement = targetShipHealth.GetComponent<ShipMovement>();
            targetShipWeapon = targetShipHealth.GetComponent<ShipWeapon>();
        }
    }

    // ==================== OPEN / CLOSE ====================

    public void Open() => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);
    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);

    // ==================== LOADOUT ====================

    private void LoadLoadoutFor(ShipDefinition def)
    {
        currentLoadout = ShipLoadoutManager.GetLoadout(def.shipId);

        if (currentLoadout == null)
        {
            currentLoadout = new ShipLoadout(def.shipId);
            PopulateDefaultInventory(currentLoadout);
        }

        if (titleLabel != null)
            titleLabel.text = $"ПОСТРОЙКА И КОНФИГУРАЦИЯ: {def.displayName}";
    }

    private void SaveCurrentLoadout()
    {
        if (currentLoadout == null) return;
        SyncSlotsToLoadout();
        ShipLoadoutManager.SaveLoadout(currentLoadout);
    }

    private void SyncSlotsToLoadout()
    {
        if (currentLoadout == null) return;

        currentLoadout.weaponSlots = new List<FittingItem>();
        currentLoadout.shieldSlots = new List<FittingItem>();
        currentLoadout.engineSlots = new List<FittingItem>();

        foreach (var s in weaponSlots) if (s.Item != null) currentLoadout.weaponSlots.Add(s.Item);
        foreach (var s in shieldSlots) if (s.Item != null) currentLoadout.shieldSlots.Add(s.Item);
        foreach (var s in engineSlots) if (s.Item != null) currentLoadout.engineSlots.Add(s.Item);

        // inventory берём из currentLoadout.inventory — он обновляется при drop/auto-equip
    }

    private void PopulateDefaultInventory(ShipLoadout loadout)
    {
        loadout.inventory = new List<FittingItem>
        {
            new FittingItem("w1", "Лазер T1", SlotGroup.Top_Weapons, new Color(1f, 0.3f, 0.3f), "Оружие: Урон +15", damage: 15f, visual: WeaponVisualType.Laser),
            new FittingItem("w2", "Плазма T2", SlotGroup.Top_Weapons, new Color(1f, 0.1f, 0.5f), "Оружие: Урон +30", damage: 30f, visual: WeaponVisualType.Plasma),
            new FittingItem("w3", "Рейлган T1", SlotGroup.Top_Weapons, new Color(1f, 0.5f, 0.2f), "Оружие: Урон +20", damage: 20f, visual: WeaponVisualType.Laser),
            new FittingItem("s1", "Легкий Щит", SlotGroup.Center_Shields, new Color(0.2f, 0.6f, 1f), "Защита: Щит +30 HP", shield: 30f, mass: 0.2f),
            new FittingItem("s2", "Тяжелый Щит", SlotGroup.Center_Shields, new Color(0.3f, 0.9f, 0.4f), "Защита: Щит +80 HP", shield: 80f, mass: 1.0f),
            new FittingItem("e1", "Форсаж M1", SlotGroup.Bottom_Engines, new Color(0.9f, 0.8f, 0.2f), "Двиг: Скорость +3", speed: 3f, accel: 6f),
            new FittingItem("e2", "Тяжелый Двиг", SlotGroup.Bottom_Engines, new Color(0.8f, 0.4f, 1f), "Двиг: Ускорение +10", speed: 1f, accel: 10f, mass: 0.5f)
        };
    }

    // ==================== BUILD UI ====================

    private void BuildShipSelector()
    {
        if (shipSelectorContainer == null || shipButtonPrefab == null || shipLibrary == null)
            return;

        // Очистка
        foreach (Transform child in shipSelectorContainer)
            Destroy(child.gameObject);

        foreach (var ship in shipLibrary.ships)
        {
            if (ship == null) continue;

            var btn = Instantiate(shipButtonPrefab, shipSelectorContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = ship.displayName;

            var capturedShip = ship;
            btn.onClick.AddListener(() => OnShipSelected(capturedShip));

            // Подсветка активного
            if (ship == shipDefinition)
            {
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 0.85f, 0.2f, 1f);
            }
        }
    }

    private void BuildSlots()
    {
        // Очистка
        weaponSlots.Clear();
        shieldSlots.Clear();
        engineSlots.Clear();

        ClearContainer(weaponSlotsContainer);
        ClearContainer(shieldSlotsContainer);
        ClearContainer(engineSlotsContainer);

        int weaponCount = shipDefinition != null ? shipDefinition.weaponSlots : 4;
        int shieldCount = shipDefinition != null ? shipDefinition.shieldSlots : 4;
        int engineCount = shipDefinition != null ? shipDefinition.engineSlots : 4;

        CreateSlots(weaponSlotsContainer, weaponCount, SlotGroup.Top_Weapons, weaponSlots);
        CreateSlots(shieldSlotsContainer, shieldCount, SlotGroup.Center_Shields, shieldSlots);
        CreateSlots(engineSlotsContainer, engineCount, SlotGroup.Bottom_Engines, engineSlots);

        // Заполняем слоты из loadout
        if (currentLoadout != null)
        {
            FillSlots(weaponSlots, currentLoadout.weaponSlots);
            FillSlots(shieldSlots, currentLoadout.shieldSlots);
            FillSlots(engineSlots, currentLoadout.engineSlots);
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    private void CreateSlots(Transform container, int count, SlotGroup group, List<FittingSlot> list)
    {
        if (container == null || slotPrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            var slot = Instantiate(slotPrefab, container);
            slot.group = group;
            slot.index = i;
            slot.window = this;
            slot.SetItem(null);
            list.Add(slot);
        }
    }

    private void FillSlots(List<FittingSlot> slots, List<FittingItem> items)
    {
        if (items == null) return;

        for (int i = 0; i < slots.Count && i < items.Count; i++)
        {
            slots[i].SetItem(items[i]);
        }
    }

    private void BuildInventory()
    {
        ClearContainer(inventoryContainer);
        itemViews.Clear();

        if (currentLoadout == null || currentLoadout.inventory == null)
            return;

        foreach (var item in currentLoadout.inventory)
        {
            CreateItemView(item);
        }
    }

    private void CreateItemView(FittingItem item)
    {
        if (inventoryContainer == null || itemViewPrefab == null) return;

        var view = Instantiate(itemViewPrefab, inventoryContainer);
        view.window = this;
        view.SetItem(item);
        itemViews.Add(view);
    }

    // ==================== DRAG & DROP HANDLERS ====================

    /// <summary>
    /// Обработка drop элемента инвентаря в слот.
    /// </summary>
    public void HandleDropFromInventory(FittingItemView source, FittingSlot targetSlot)
    {
        if (source == null || targetSlot == null) return;
        if (source.item == null) return;
        if (source.item.allowedGroup != targetSlot.group) return;

        // Если в слоте уже что-то есть — вернуть в инвентарь
        if (targetSlot.Item != null)
        {
            AddToInventory(targetSlot.Item);
        }

        // Убрать item из инвентаря (из loadout)
        currentLoadout.inventory.Remove(source.item);

        // Поставить в слот
        targetSlot.SetItem(source.item);

        // Удалить view из инвентаря
        itemViews.Remove(source);
        Destroy(source.gameObject);

        // Применить и сохранить
        ApplyLoadoutToShip();
        SaveCurrentLoadout();
    }

    /// <summary>
    /// Обработка drop слота в слот (обмен или перенос).
    /// </summary>
    public void HandleDropFromSlot(FittingSlot sourceSlot, FittingSlot targetSlot)
    {
        if (sourceSlot == null || targetSlot == null) return;
        if (sourceSlot == targetSlot) return;
        if (sourceSlot.Item == null) return;
        if (sourceSlot.Item.allowedGroup != targetSlot.group) return;

        var sourceItem = sourceSlot.Item;
        var targetItem = targetSlot.Item;

        // Обмен
        targetSlot.SetItem(sourceItem);
        sourceSlot.SetItem(targetItem);

        ApplyLoadoutToShip();
        SaveCurrentLoadout();
    }

    /// <summary>
    /// Снять предмет со слота (ПКМ).
    /// </summary>
    public void HandleUnequip(FittingSlot slot)
    {
        if (slot == null || slot.Item == null) return;

        AddToInventory(slot.Item);
        slot.SetItem(null);

        ApplyLoadoutToShip();
        SaveCurrentLoadout();
    }

    /// <summary>
    /// Автоустановка (двойной клик в инвентаре).
    /// </summary>
    public void HandleAutoEquip(FittingItemView view)
    {
        if (view == null || view.item == null) return;

        var item = view.item;

        // Найти первый пустой слот нужной группы
        FittingSlot target = FindFirstEmptySlot(item.allowedGroup);
        if (target == null) return; // нет свободного

        currentLoadout.inventory.Remove(item);
        target.SetItem(item);

        itemViews.Remove(view);
        Destroy(view.gameObject);

        ApplyLoadoutToShip();
        SaveCurrentLoadout();
    }

    private FittingSlot FindFirstEmptySlot(SlotGroup group)
    {
        List<FittingSlot> list = null;
        if (group == SlotGroup.Top_Weapons) list = weaponSlots;
        else if (group == SlotGroup.Center_Shields) list = shieldSlots;
        else if (group == SlotGroup.Bottom_Engines) list = engineSlots;

        if (list == null) return null;

        foreach (var s in list)
        {
            if (s.Item == null) return s;
        }
        return null;
    }

    private void AddToInventory(FittingItem item)
    {
        if (item == null) return;

        if (currentLoadout.inventory == null)
            currentLoadout.inventory = new List<FittingItem>();

        currentLoadout.inventory.Add(item);
        CreateItemView(item);
    }

    // ==================== APPLY TO SHIP ====================

    private void ApplyLoadoutToShip()
    {
        if (targetShipHealth == null) ResolveTargetShip();
        if (targetShipHealth == null) return;

        if (targetShipMovement == null) targetShipMovement = targetShipHealth.GetComponent<ShipMovement>();
        if (targetShipWeapon == null) targetShipWeapon = targetShipHealth.GetComponent<ShipWeapon>();

        float totalShieldHP = 0f;
        float totalSpeedAdd = 0f;
        float totalAccelAdd = 0f;
        float totalMassAdd = 0f;

        List<FittingItem> installedWeapons = new List<FittingItem>();

        foreach (var s in weaponSlots)
        {
            if (s.Item != null)
            {
                installedWeapons.Add(s.Item);
                totalShieldHP += s.Item.shieldBonus;
                totalSpeedAdd += s.Item.speedBonus;
                totalAccelAdd += s.Item.accelBonus;
                totalMassAdd += s.Item.massBonus;
            }
        }
        foreach (var s in shieldSlots)
        {
            if (s.Item != null)
            {
                totalShieldHP += s.Item.shieldBonus;
                totalSpeedAdd += s.Item.speedBonus;
                totalAccelAdd += s.Item.accelBonus;
                totalMassAdd += s.Item.massBonus;
            }
        }
        foreach (var s in engineSlots)
        {
            if (s.Item != null)
            {
                totalShieldHP += s.Item.shieldBonus;
                totalSpeedAdd += s.Item.speedBonus;
                totalAccelAdd += s.Item.accelBonus;
                totalMassAdd += s.Item.massBonus;
            }
        }

        // Оружие
        if (targetShipWeapon != null)
            targetShipWeapon.SetEquippedWeapons(installedWeapons);

        // Базовые ТТХ
        float baseSpeed = shipDefinition != null ? shipDefinition.baseMaxSpeed : 5f;
        float baseAccel = shipDefinition != null ? shipDefinition.baseAcceleration : 12f;
        float baseShield = shipDefinition != null ? shipDefinition.baseShieldHP : 50f;
        float baseMass = shipDefinition != null ? shipDefinition.baseMass : 2f;

        if (targetShipMovement != null)
        {
            targetShipMovement.maxSpeed = baseSpeed + totalSpeedAdd;
            targetShipMovement.acceleration = baseAccel + totalAccelAdd;
        }

        if (targetShipHealth.Data != null)
        {
            targetShipHealth.Data.maxShieldHP = baseShield + totalShieldHP;
            targetShipHealth.Data.currentShieldHP = Mathf.Min(targetShipHealth.Data.currentShieldHP, targetShipHealth.Data.maxShieldHP);
            targetShipHealth.Data.mass = Mathf.Max(0.5f, baseMass + totalMassAdd);

            if (targetShipMovement != null)
                targetShipMovement.ApplyMassFromData();
        }
    }

    // ==================== SHIP SELECTOR ====================

    private void OnShipSelected(ShipDefinition newShip)
    {
        if (newShip == null || newShip == shipDefinition) return;

        SaveCurrentLoadout();

        shipDefinition = newShip;
        LoadLoadoutFor(shipDefinition);

        BuildShipSelector();
        BuildSlots();
        BuildInventory();
        ApplyLoadoutToShip();
    }
}