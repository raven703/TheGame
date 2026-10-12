using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-окно фитинга. Редактирует loadout ТЕКУЩЕГО корабля игрока (PlayerProfile.currentShipId).
/// Селектор корабля в окне МЕНЯЕТ корабль игрока (ShipHealth.shipDefinition).
/// </summary>
public class FittingWindow : MonoBehaviour
{
    [Header("Ship Library (fallback)")]
    [Tooltip("Справочник всех кораблей. Используется для селектора и fallback, если ShipRegistry не задан.")]
    public ShipLibrary shipLibrary;

    [Header("Prefabs")]
    public FittingSlot slotPrefab;
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
    public Button shipButtonPrefab;

    [Header("UI References")]
    public Button closeButton;
    public TextMeshProUGUI titleLabel;
    public Transform dragLayer;

    [Header("Player Ship Target")]
    [Tooltip("Корабль игрока. Если null — ищется по тегу 'Player'.")]
    public ShipHealth targetShipHealth;

    // ============ RUNTIME ============

    private ShipDefinition shipDefinition;   // текущий редактируемый корабль (кэш)
    private ShipLoadout currentLoadout;

    private List<FittingSlot> weaponSlots = new List<FittingSlot>();
    private List<FittingSlot> shieldSlots = new List<FittingSlot>();
    private List<FittingSlot> engineSlots = new List<FittingSlot>();

    private List<FittingItemView> itemViews = new List<FittingItemView>();

    private ShipMovement targetShipMovement;
    private ShipWeapon targetShipWeapon;

    private bool isBuilding = false;
    private bool isBuilt = false;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
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

    public void Open()
    {
        if (targetShipHealth == null)
            ResolveTargetShip();

        ResolveCurrentShipDefinition();

        if (!isBuilt)
        {
            BuildAll();
            isBuilt = true;
        }

        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    public void Toggle()
    {
        if (gameObject.activeSelf) Close();
        else Open();
    }

    /// <summary>
    /// Определяет текущий корабль игрока.
    /// </summary>
    private void ResolveCurrentShipDefinition()
    {
        // 1. Из ShipHealth игрока
        if (targetShipHealth != null && targetShipHealth.shipDefinition != null)
        {
            shipDefinition = targetShipHealth.shipDefinition;
            return;
        }

        // 2. Из PlayerProfile + ShipRegistry
        if (PlayerProfile.Instance != null && ShipRegistry.Instance != null)
        {
            var def = ShipRegistry.Instance.Get(PlayerProfile.Instance.currentShipId);
            if (def != null)
            {
                shipDefinition = def;
                return;
            }
        }

        // 3. Fallback — первый из ShipLibrary
        if (shipLibrary != null && shipLibrary.ships != null && shipLibrary.ships.Count > 0)
        {
            shipDefinition = shipLibrary.ships[0];
        }
    }

    // ==================== BUILD ALL ====================

    private void BuildAll()
    {
        if (isBuilding) return;
        isBuilding = true;

        if (shipDefinition != null)
            LoadLoadoutFor(shipDefinition);

        BuildShipSelector();
        BuildSlots();
        BuildInventory();
        ApplyLoadoutToShip();

        isBuilding = false;
    }

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
    }

    private void PopulateDefaultInventory(ShipLoadout loadout)
    {
        if (loadout.inventory == null)
            loadout.inventory = new List<FittingItem>();

        loadout.inventory.Add(new FittingItem("w1", "Лазер T1", SlotGroup.Top_Weapons, new Color(1f, 0.3f, 0.3f), "Оружие: Урон +15", damage: 15f, visual: WeaponVisualType.Laser));
        loadout.inventory.Add(new FittingItem("w2", "Плазма T2", SlotGroup.Top_Weapons, new Color(1f, 0.1f, 0.5f), "Оружие: Урон +30", damage: 30f, visual: WeaponVisualType.Plasma));
        loadout.inventory.Add(new FittingItem("w3", "Рейлган T1", SlotGroup.Top_Weapons, new Color(1f, 0.5f, 0.2f), "Оружие: Урон +20", damage: 20f, visual: WeaponVisualType.Laser));
        loadout.inventory.Add(new FittingItem("s1", "Легкий Щит", SlotGroup.Center_Shields, new Color(0.2f, 0.6f, 1f), "Защита: Щит +30 HP", shield: 30f, mass: 0.2f));
        loadout.inventory.Add(new FittingItem("s2", "Тяжелый Щит", SlotGroup.Center_Shields, new Color(0.3f, 0.9f, 0.4f), "Защита: Щит +80 HP", shield: 80f, mass: 1.0f));
        loadout.inventory.Add(new FittingItem("e1", "Форсаж M1", SlotGroup.Bottom_Engines, new Color(0.9f, 0.8f, 0.2f), "Двиг: Скорость +3", speed: 3f, accel: 6f));
        loadout.inventory.Add(new FittingItem("e2", "Тяжелый Двиг", SlotGroup.Bottom_Engines, new Color(0.8f, 0.4f, 1f), "Двиг: Ускорение +10", speed: 1f, accel: 10f, mass: 0.5f));
    }

    // ==================== BUILD UI ====================

    private void BuildShipSelector()
    {
        if (shipSelectorContainer == null || shipButtonPrefab == null)
            return;

        var ships = (shipLibrary != null && shipLibrary.ships != null) ? shipLibrary.ships : null;
        if (ships == null || ships.Count == 0) return;

        ClearContainer(shipSelectorContainer);

        foreach (var ship in ships)
        {
            if (ship == null) continue;

            var btn = Instantiate(shipButtonPrefab, shipSelectorContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = ship.displayName;

            var capturedShip = ship;
            btn.onClick.AddListener(() => OnShipSelected(capturedShip));

            if (ship == shipDefinition)
            {
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 0.85f, 0.2f, 1f);
            }
        }
    }

    private void BuildSlots()
    {
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

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child == null) continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
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
            slots[i].SetItem(items[i]);
    }

    private void BuildInventory()
    {
        ClearContainer(inventoryContainer);
        itemViews.Clear();

        if (currentLoadout == null || currentLoadout.inventory == null) return;

        foreach (var item in currentLoadout.inventory)
            CreateItemView(item);
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

    public void HandleDropFromInventory(FittingItemView source, FittingSlot targetSlot)
    {
        if (source == null || targetSlot == null) return;
        if (source.item == null) return;
        if (source.item.allowedGroup != targetSlot.group) return;

        if (targetSlot.Item != null)
            AddToInventory(targetSlot.Item);

        if (currentLoadout != null && currentLoadout.inventory != null)
            currentLoadout.inventory.Remove(source.item);

        targetSlot.SetItem(source.item);

        itemViews.Remove(source);
        Destroy(source.gameObject);

        ApplyLoadoutToShip();
        SaveCurrentLoadout();
    }

    public void HandleDropFromSlot(FittingSlot sourceSlot, FittingSlot targetSlot)
    {
        if (sourceSlot == null || targetSlot == null) return;
        if (sourceSlot == targetSlot) return;
        if (sourceSlot.Item == null) return;
        if (sourceSlot.Item.allowedGroup != targetSlot.group) return;

        var sourceItem = sourceSlot.Item;
        var targetItem = targetSlot.Item;

        targetSlot.SetItem(sourceItem);
        sourceSlot.SetItem(targetItem);

        ApplyLoadoutToShip();
        SaveCurrentLoadout();
    }

    public void HandleUnequip(FittingSlot slot)
    {
        if (slot == null || slot.Item == null) return;

        AddToInventory(slot.Item);
        slot.SetItem(null);

        ApplyLoadoutToShip();
        SaveCurrentLoadout();
    }

    public void HandleAutoEquip(FittingItemView view)
    {
        if (view == null || view.item == null) return;

        var item = view.item;
        FittingSlot target = FindFirstEmptySlot(item.allowedGroup);
        if (target == null) return;

        if (currentLoadout != null && currentLoadout.inventory != null)
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
            if (s.Item == null) return s;
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

        if (targetShipWeapon != null)
            targetShipWeapon.SetEquippedWeapons(installedWeapons);

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
            // Щит: изменяем maxShieldHP и корректируем currentShieldHP на дельту
            float oldMaxShield = targetShipHealth.Data.maxShieldHP;
            float newMaxShield = baseShield + totalShieldHP;
            float delta = newMaxShield - oldMaxShield;

            targetShipHealth.Data.maxShieldHP = newMaxShield;
            targetShipHealth.Data.currentShieldHP = Mathf.Clamp(
                targetShipHealth.Data.currentShieldHP + delta,
                0f,
                newMaxShield);

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

        if (PlayerProfile.Instance != null)
            PlayerProfile.Instance.SetCurrentShip(newShip.shipId);

        if (targetShipHealth != null)
        {
            targetShipHealth.shipDefinition = newShip;

            if (targetShipHealth.Data != null)
            {
                targetShipHealth.Data.maxHullHP = newShip.maxHullHP;
                targetShipHealth.Data.currentHullHP = newShip.maxHullHP;
                targetShipHealth.Data.maxShieldHP = newShip.baseShieldHP;
                targetShipHealth.Data.currentShieldHP = newShip.baseShieldHP;
                targetShipHealth.Data.mass = newShip.baseMass;
            }

            if (targetShipMovement != null)
            {
                targetShipMovement.maxSpeed = newShip.baseMaxSpeed;
                targetShipMovement.acceleration = newShip.baseAcceleration;
                targetShipMovement.rotationSpeed = newShip.baseRotationSpeed;
                targetShipMovement.ApplyMassFromData();
            }
        }

        shipDefinition = newShip;
        BuildAll();
    }
}