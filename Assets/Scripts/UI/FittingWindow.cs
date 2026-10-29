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
    public ShipHealth targetShipHealth;

    // ============ RUNTIME ============

    private ShipDefinition shipDefinition;
    private ShipLoadout currentLoadout;

    private List<FittingSlot> weaponSlots = new List<FittingSlot>();
    private List<FittingSlot> shieldSlots = new List<FittingSlot>();
    private List<FittingSlot> engineSlots = new List<FittingSlot>();

    private List<FittingItemView> itemViews = new List<FittingItemView>();

    private ShipMovement targetShipMovement;
    private ShipWeapon targetShipWeapon;

    public static Rect PresetsPanel => new Rect((Screen.width - 480f) * .5f, Screen.height - 45f, 480f, 40f);
    public bool CanEdit => CombatSession.IsPreparing && (GameState.Instance == null || !GameState.Instance.isCombatStarted);
    private string presetMessage = "";
    private TMPro.TextMeshProUGUI detailsLabel;
    private TextMeshProUGUI statsLabel;
    private void RefreshStats()
    {
        if (statsLabel == null) return;
        var b = ShipStatsCalculator.Calculate(shipDefinition, null);
        var s = ShipStatsCalculator.Calculate(shipDefinition, currentLoadout);
        statsLabel.text = "ХАРАКТЕРИСТИКИ КОРАБЛЯ\nБаза → оснащение (без боевых эффектов)\n" +
            $"Корпус: {b.hull:0.#} → {s.hull:0.#}\nЩит: {b.shield:0.#} → {s.shield:0.#}\n" +
            $"Масса: {b.mass:0.##} → {s.mass:0.##}\nСкорость: {b.speed:0.#} → {s.speed:0.#}\n" +
            $"Маршевая сила: {b.thrust:0.#} → {s.thrust:0.#}\nНазад: {s.reverseThrust:0.#}; вбок: {s.lateralThrust:0.#}\nПоворот: {b.turn:0.#} → {s.turn:0.#} °/с\n" +
            $"Захват: {b.lockTime:0.##} → {s.lockTime:0.##} с\nЭнергия макс.: {b.energy:0.#} → {s.energy:0.#}\n" +
            $"Восстановление: {b.regeneration:0.#} → {s.regeneration:0.#}/с\nУстойчивость: {b.resistance:P0} → {s.resistance:P0}\n" +
            $"Охлаждение: {b.cooling:0.#} → {s.cooling:0.#}/с\n\nОРУЖИЕ (каждый слот отдельно)\n";
        if (currentLoadout?.weaponSlots != null)
            foreach (var item in currentLoadout.weaponSlots)
            {
                if (item == null || string.IsNullOrEmpty(item.id)) continue;
                var w = WeaponStatsCalculator.Calculate(item, targetShipWeapon != null ? targetShipWeapon.baseCooldown : .6f);
                int index = currentLoadout.weaponSlots.IndexOf(item);
                float sector = targetShipWeapon != null ? targetShipWeapon.MountSector(index) : 60f;
                statsLabel.text += $"{item.name} / {w.kind} / {(item.mountType == WeaponMountType.Turret ? "Турель" : "Курсовая")}\nКрепление {index + 1}: сектор {sector:0}°" +
                    (item.mountType == WeaponMountType.Turret ? $"; {item.trackingSpeed:0} °/с; вращение ограничено креплением" : $"; собственный сектор {item.FixedFireSector:0}°; итог ±{Game.Core.MountGeometry.EffectiveSector(sector,(Game.Core.WeaponMountType)(int)item.mountType,item.FixedFireSector)*.5f:0}°") +
                    $"\nУрон {w.damage:0.#}; DPS* {w.NominalDps:0.#}; КД {w.cooldown:0.##} с\nЭнергия {w.energy:0.#}; нагрев {ShipReactor.CalculateHeat(w.heat,s.resistance):0.#} (база {w.heat:0.#})\n";
            }
        statsLabel.text += "\n* Номинальный DPS без ограничений энергии,\nперегрева, точности и защиты цели.\nОбщий DPS не суммируется.";
    }
    private Canvas modalCanvas;
    public static FittingWindow OpenWindow { get; private set; }
    private readonly Dictionary<CanvasGroup, bool[]> backgroundStates = new Dictionary<CanvasGroup, bool[]>();
    public void ShowDescription(FittingItem item) { if (detailsLabel != null) detailsLabel.text = item == null ? "Перетащите модуль в слот своей категории. ПКМ — снять." : item.name + "\n" + item.EffectiveDescription; }
    public void ConfigureLayout()
    {
        var root = (RectTransform)transform;
        CombatUIStyle.Rect(root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-590,-310),new Vector2(590,310));
        var image = GetComponent<Image>(); image.sprite=null; image.color=new Color(.055f,.075f,.105f,1f); image.raycastTarget=true;
        modalCanvas = CombatUIStyle.Ensure<Canvas>(gameObject);
        modalCanvas.overrideSorting=true; modalCanvas.sortingOrder=100;
        if(GetComponent<GraphicRaycaster>()==null)gameObject.AddComponent<GraphicRaycaster>();
        CombatUIStyle.Box(titleLabel.transform,24,16,1060,34); titleLabel.fontSize=22; titleLabel.color=Color.white;
        CombatUIStyle.Box(closeButton.transform,1120,16,36,34);
        CombatUIStyle.Box(shipSelectorContainer,24,62,1132,38);
        var selectorImage=shipSelectorContainer.GetComponent<Image>(); if(selectorImage!=null){selectorImage.color=Color.clear;selectorImage.raycastTarget=false;}
        ConfigureGroup(weaponSlotsContainer,"ОРУЖИЕ",24,122);
        ConfigureGroup(shieldSlotsContainer,"ЩИТЫ",24,254);
        ConfigureGroup(engineSlotsContainer,"ДВИГАТЕЛИ",24,386);
        var scroll=inventoryContainer.GetComponentInParent<ScrollRect>(true);
        if(scroll!=null)
        {
            CombatUIStyle.Box(scroll.transform,508,150,280,350);
            var bg=scroll.GetComponent<Image>();if(bg!=null){bg.sprite=null;bg.color=CombatUIStyle.Card;}
            CombatUIStyle.Rect(scroll.viewport,Vector2.zero,Vector2.one,new Vector2(8,8),new Vector2(-8,-8));
            var viewportImage=scroll.viewport.GetComponent<Image>();if(viewportImage!=null){viewportImage.sprite=null;viewportImage.color=new Color(.08f,.11f,.15f);}
            var vertical=inventoryContainer.GetComponent<VerticalLayoutGroup>();if(vertical!=null){vertical.spacing=10;vertical.padding=new RectOffset(6,6,6,6);vertical.childControlHeight=true;vertical.childControlWidth=true;vertical.childForceExpandHeight=false;vertical.childForceExpandWidth=true;}
            var grid=inventoryContainer.GetComponent<GridLayoutGroup>();
            if(grid!=null){grid.cellSize=new Vector2(244,64);grid.spacing=new Vector2(10,10);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=1;grid.padding=new RectOffset(4,4,4,4);}
            var drop=CombatUIStyle.Ensure<InventoryDropZone>(scroll.viewport.gameObject);drop.window=this;
        }
        var inventoryTitle=CombatUIStyle.Text("InventoryHeading",transform,"ИНВЕНТАРЬ",17);CombatUIStyle.Box(inventoryTitle.transform,508,122,280,25);
        var statsPanel=CombatUIStyle.Node("StatisticsScroll",transform);
        CombatUIStyle.Box(statsPanel.transform,808,112,350,490);
        var statsScroll=CombatUIStyle.Ensure<ScrollRect>(statsPanel);
        statsScroll.horizontal=false;statsScroll.vertical=true;
        var statsViewport=CombatUIStyle.Node("Viewport",statsPanel.transform);
        CombatUIStyle.Rect(statsViewport.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        CombatUIStyle.Ensure<Image>(statsViewport).color=new Color(.055f,.075f,.105f);
        CombatUIStyle.Ensure<RectMask2D>(statsViewport);
        statsLabel=CombatUIStyle.Text("ShipStatistics",statsViewport.transform,"",14);
        var statsRect=(RectTransform)statsLabel.transform;
        statsRect.anchorMin=new Vector2(0,1);statsRect.anchorMax=Vector2.one;
        statsRect.pivot=new Vector2(.5f,1);statsRect.anchoredPosition=Vector2.zero;statsRect.sizeDelta=Vector2.zero;
        var statsFit=CombatUIStyle.Ensure<ContentSizeFitter>(statsLabel.gameObject);
        statsFit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        statsScroll.viewport=(RectTransform)statsViewport.transform;statsScroll.content=statsRect;
        statsLabel.enableAutoSizing=false;statsLabel.fontSize=14;
        RefreshStats();
        foreach(var text in GetComponentsInChildren<TextMeshProUGUI>(true))
            if(text!=titleLabel && (text.text.Contains("ГРУППА")||text.text.Contains("ГРУППЫ")||text.text.Contains("ИНВЕНТАРЬ МОДУЛЕЙ")))text.gameObject.SetActive(false);
        detailsLabel=CombatUIStyle.Text("ModuleDescription",transform,"",14);CombatUIStyle.Box(detailsLabel.transform,508,510,280,84);ShowDescription(null);
        var presets=CombatUIStyle.Node("PresetButtons",transform);CombatUIStyle.Box(presets.transform,24,548,466,42);
        for (int slot = 1; slot <= 3; slot++)
        {
            int captured=slot;
            var save=CombatUIStyle.Button("Save"+slot,presets.transform,"Сохр. "+slot,()=>{if(!CanEdit||currentLoadout==null)return;SyncSlotsToLoadout();LoadoutPresets.Save(currentLoadout,captured);ShowDescription(null);});
            var load=CombatUIStyle.Button("Load"+slot,presets.transform,"Загр. "+slot,()=>{LoadPreset(captured);if(detailsLabel!=null)detailsLabel.text=presetMessage;});
            CombatUIStyle.Box(save.transform,(slot-1)*156,0,72,36);CombatUIStyle.Box(load.transform,(slot-1)*156+76,0,72,36);
        }
        var layer=CombatUIStyle.Node("ModalDragLayer",transform);
        CombatUIStyle.Rect(layer.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var layerCanvas=CombatUIStyle.Ensure<Canvas>(layer);layerCanvas.overrideSorting=true;layerCanvas.sortingOrder=110;
        dragLayer=layer.transform;dragLayer.SetAsLastSibling();
    }
    private void ConfigureGroup(Transform container,string caption,float x,float y)
    {
        var heading=CombatUIStyle.Text(container.name+"Heading",transform,caption,17);CombatUIStyle.Box(heading.transform,x,y,466,25);
        CombatUIStyle.Box(container,x,y+30,466,74);
        var image=container.GetComponent<Image>();if(image!=null){image.sprite=null;image.color=new Color(.08f,.11f,.15f);image.raycastTarget=false;}
        var layout=container.GetComponent<HorizontalLayoutGroup>();if(layout!=null){layout.spacing=12;layout.padding=new RectOffset(4,4,4,4);layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=true;}
    }
    private void OnDisable()
    {
        CancelDrags();
        foreach(var entry in backgroundStates)if(entry.Key!=null){entry.Key.interactable=entry.Value[0];entry.Key.blocksRaycasts=entry.Value[1];}
        backgroundStates.Clear();if(OpenWindow==this)OpenWindow=null;
        var blocker=transform.parent==null?null:transform.parent.Find("LoadoutBlocker");if(blocker!=null)blocker.gameObject.SetActive(false);
    }
    private void CancelDrags()
    {
        HideSlotTooltip();
        HighlightDestinations(null,null);
        foreach(var view in GetComponentsInChildren<FittingItemView>(true))view.CancelDrag();
        foreach(var slot in GetComponentsInChildren<FittingSlot>(true))slot.CancelDrag();
    }
    private void MakeModal()
    {
        OpenWindow=this;
        var blocker=CombatUIStyle.Panel("LoadoutBlocker",transform.parent,new Color(0,0,0,.72f));
        CombatUIStyle.Rect(blocker.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var canvas=CombatUIStyle.Ensure<Canvas>(blocker.gameObject);canvas.overrideSorting=true;canvas.sortingOrder=99;
        if(blocker.GetComponent<GraphicRaycaster>()==null)blocker.gameObject.AddComponent<GraphicRaycaster>();blocker.gameObject.SetActive(true);
        foreach(Transform child in transform.parent)
        {
            if(child==transform||child==blocker.transform)continue;
            var group=CombatUIStyle.Ensure<CanvasGroup>(child.gameObject);
            if(!backgroundStates.ContainsKey(group))backgroundStates.Add(group,new[]{group.interactable,group.blocksRaycasts});
            group.interactable=false;group.blocksRaycasts=false;
        }
    }
    private void LoadPreset(int slot)
    {
        if (!CanEdit || shipDefinition == null) return;
        SyncSlotsToLoadout();
        var restored = LoadoutPresets.Load(currentLoadout, slot);
        if (restored == null || !WeaponMountRules.Validate(shipDefinition,restored))
        { presetMessage = "Комплект недоступен: предметы, вместимость или совместимость креплений. Ничего не изменено."; return; }
        if(!ShipLoadoutManager.ApplyPlan(shipDefinition,restored,CanEdit,out var error)){presetMessage=error;return;}
        ReloadEquipment();
        presetMessage = $"Комплект {slot} загружен";
    }

    private bool isBuilding = false;
    private bool isBuilt = false;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

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

    public void Open()
    {
        if (!CanEdit) return;
        ConfigureLayout();
        if (targetShipHealth == null)
            ResolveTargetShip();

        ResolveCurrentShipDefinition();

        if (!isBuilt)
        {
            try { BuildAll(); }
            catch(System.Exception e)
            {
                isBuilding=false;presetMessage=e.Message;CombatSession.Log(e.Message);Debug.LogError(e.Message);return;
            }
            isBuilt = true;
        }

        gameObject.SetActive(true);
        MakeModal();
        modalCanvas.overrideSorting=true;
        modalCanvas.sortingOrder=100;
        dragLayer.GetComponent<Canvas>().overrideSorting=true;
        dragLayer.GetComponent<Canvas>().sortingOrder=110;
    }

    public void Close() { CancelDrags(); gameObject.SetActive(false); }

    public void Toggle()
    {
        if (gameObject.activeSelf) Close();
        else Open();
    }

    private void ResolveCurrentShipDefinition()
    {
        if (targetShipHealth != null && targetShipHealth.shipDefinition != null)
        {
            shipDefinition = targetShipHealth.shipDefinition;
            return;
        }

        if (PlayerProfile.Instance != null && ShipRegistry.Instance != null)
        {
            var def = ShipRegistry.Instance.Get(PlayerProfile.Instance.currentShipId);
            if (def != null)
            {
                shipDefinition = def;
                return;
            }
        }

        if (shipLibrary != null && shipLibrary.ships != null && shipLibrary.ships.Count > 0)
        {
            shipDefinition = shipLibrary.ships[0];
        }
    }

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

    private void LoadLoadoutFor(ShipDefinition def)
    {
        var starter = new ShipLoadout(def.shipId);
        if(!ShipLoadoutManager.StarterInventoryIssued)PopulateDefaultInventory(starter);
        if(!ShipLoadoutManager.EnsureShip(def,starter.inventory,out var error))throw new System.InvalidOperationException(error);
        currentLoadout=ShipLoadoutManager.GetLoadout(def.shipId);
        if (titleLabel != null)
            titleLabel.text = $"ПОСТРОЙКА И КОНФИГУРАЦИЯ: {def.displayName}";
    }

    private void SaveCurrentLoadout()
    {
        if (currentLoadout == null) return;
        SyncSlotsToLoadout();
        // Operations already persisted by the ledger; never import mutable UI snapshots.
    }

    private void SyncSlotsToLoadout()
    {
        if (currentLoadout == null) return;

        currentLoadout.weaponSlots = new List<FittingItem>();
        currentLoadout.shieldSlots = new List<FittingItem>();
        currentLoadout.engineSlots = new List<FittingItem>();

        foreach (var s in weaponSlots) currentLoadout.weaponSlots.Add(s.Item);
        foreach (var s in shieldSlots) currentLoadout.shieldSlots.Add(s.Item);
        foreach (var s in engineSlots) currentLoadout.engineSlots.Add(s.Item);
    }

    private void PopulateDefaultInventory(ShipLoadout loadout)
    {
        if (loadout.inventory == null)
            loadout.inventory = new List<FittingItem>();

        // 1. Если у корабля задан свой дефолтный набор — используем его
        if (shipDefinition != null && shipDefinition.defaultInventory != null
            && shipDefinition.defaultInventory.Count > 0)
        {
            foreach (var item in shipDefinition.defaultInventory)
            {
                if (item != null)
                    loadout.inventory.Add(item);
            }
            Debug.Log($"[FittingWindow] Загружен дефолтный инвентарь для {shipDefinition.displayName}: {shipDefinition.defaultInventory.Count} модулей.");
            return;
        }

        // 2. Fallback: старый универсальный набор
        Debug.Log("[FittingWindow] У корабля нет defaultInventory, используется fallback.");

        loadout.inventory.Add(new FittingItem("w1", "Лазер T1", SlotGroup.Top_Weapons, new Color(1f, 0.3f, 0.3f), "Оружие: Урон +15", damage: 15f, visual: WeaponVisualType.Laser));
        loadout.inventory.Add(new FittingItem("w2", "Плазма T2", SlotGroup.Top_Weapons, new Color(1f, 0.1f, 0.5f), "Оружие: Урон +30", damage: 30f, visual: WeaponVisualType.Plasma));
        loadout.inventory.Add(new FittingItem("w3", "Рейлган T1", SlotGroup.Top_Weapons, new Color(1f, 0.5f, 0.2f), "Оружие: Урон +20", damage: 20f, visual: WeaponVisualType.Laser));
        loadout.inventory.Add(new FittingItem("s1", "Легкий Щит", SlotGroup.Center_Shields, new Color(0.2f, 0.6f, 1f), "Защита: Щит +30 HP", shield: 30f, mass: 0.2f));
        loadout.inventory.Add(new FittingItem("s2", "Тяжелый Щит", SlotGroup.Center_Shields, new Color(0.3f, 0.9f, 0.4f), "Защита: Щит +80 HP", shield: 80f, mass: 1.0f));
        loadout.inventory.Add(new FittingItem("e1", "Форсаж M1", SlotGroup.Bottom_Engines, new Color(0.9f, 0.8f, 0.2f), "Двиг: Скорость +3", speed: 3f, accel: 6f));
        loadout.inventory.Add(new FittingItem("e2", "Тяжелый Двиг", SlotGroup.Bottom_Engines, new Color(0.8f, 0.4f, 1f), "Двиг: Ускорение +10", speed: 1f, accel: 10f, mass: 0.5f));
        loadout.inventory.Add(new FittingItem("t1", "Система Наведения", SlotGroup.Center_Shields, new Color(0.8f, 0.9f, 1f), "Захват: -0.2 сек", lockTime: -0.2f, mass: 0.1f));
    }
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
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
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

    private void ReloadEquipment()
    {
        CancelDrags();
        currentLoadout = ShipLoadoutManager.GetLoadout(shipDefinition.shipId);
        BuildSlots(); BuildInventory(); ApplyLoadoutToShip();
    }
    public void HandleDropFromInventory(FittingItemView source, FittingSlot target)
    {
        if(source == null || !CanDrop(target,source.item,null)) { CancelDrags(); return; }
        if(!ShipLoadoutManager.Move(shipDefinition.shipId,shipDefinition,source.item.instanceId,target.group,target.index,-1,CanEdit,out var error))
        { presetMessage=error; CancelDrags(); return; }
        ReloadEquipment();
    }
    public void HandleDropFromSlot(FittingSlot source, FittingSlot target)
    {
        if(source==null || !CanDrop(target,source.Item,source)) { CancelDrags(); return; }
        if(!ShipLoadoutManager.Move(shipDefinition.shipId,shipDefinition,source.Item.instanceId,target.group,target.index,source.index,CanEdit,out var error))
        { presetMessage=error; CancelDrags(); return; }
        ReloadEquipment();
    }
    public void HandleUnequip(FittingSlot slot)
    {
        if(slot==null || slot.Item==null || !CanEdit)return;
        if(!ShipLoadoutManager.Unequip(shipDefinition.shipId,slot.group,slot.index,slot.Item.instanceId,CanEdit,out var error))
        { presetMessage=error; return; }
        ReloadEquipment();
    }
    public void HandleAutoEquip(FittingItemView view)
    {
        if(view==null || view.item==null || !CanEdit)return;
        if(!ShipLoadoutManager.AutoEquip(shipDefinition,view.item.instanceId,CanEdit,out var error))
        {presetMessage=error;if(detailsLabel!=null)detailsLabel.text=presetMessage;return;}
        ReloadEquipment();
    }
    public bool Accepts(FittingSlot slot,FittingItem item)
    {
        return Game.Core.EquipmentRules.Fits(EquipmentDataAdapter.Definition(shipDefinition),
            (Game.Core.EquipmentCategory)(int)slot.group,slot.index,new EquipmentDataAdapter().ToCore(item));
    }
    public bool CanDrop(FittingSlot target, FittingItem item, FittingSlot source)
    {
        return target != null && target.window == this && item != null && target.Item == null && target != source &&
            (source == null || (source.window == this && source.Item == item && source.group == target.group)) &&
            ShipLoadoutManager.CanMove(shipDefinition.shipId,shipDefinition,item.instanceId,target.group,target.index,
                source==null?-1:source.index,CanEdit,out _);
    }

    public void HighlightDestinations(FittingItem item, FittingSlot source)
    {
        if(item!=null && draggedItem==null)HideSlotTooltip();
        draggedItem=item;draggedSource=source;
        foreach (var slot in weaponSlots) slot.SetHighlight(CanDrop(slot,item,source));
        foreach (var slot in shieldSlots) slot.SetHighlight(CanDrop(slot,item,source));
        foreach (var slot in engineSlots) slot.SetHighlight(CanDrop(slot,item,source));
    }
    public string SlotDescription(int index)
    {
        var point=WeaponMountRules.Get(shipDefinition,index);
        return $"{index+1}: {point.compatibility} / {point.sector:0}°";
    }
    public string SlotIcon(FittingSlot slot)
    {
        if(slot.group==SlotGroup.Center_Shields)return "Shield";
        if(slot.group==SlotGroup.Bottom_Engines)return "Engine";
        var mount=WeaponMountRules.Get(shipDefinition,slot.index).compatibility;
        return mount==MountCompatibility.FixedOnly?"Fixed":mount==MountCompatibility.TurretOnly?"Turret":"Universal";
    }
    private TextMeshProUGUI slotTooltip;
    [System.NonSerialized] private FittingItem draggedItem;
    private FittingSlot draggedSource;
    private void Update(){if(!CanEdit && draggedItem!=null)CancelDrags();}
    public void HideSlotTooltip(){if(slotTooltip!=null)slotTooltip.transform.parent.gameObject.SetActive(false);}
    public void DescribeSlot(FittingSlot slot)
    {
        string text;
        if(slot.group==SlotGroup.Top_Weapons)
        {
            var mount=WeaponMountRules.Get(shipDefinition,slot.index);
            text=(mount.compatibility==MountCompatibility.FixedOnly?"Курсовое крепление":
                mount.compatibility==MountCompatibility.TurretOnly?"Турельное крепление":"Универсальное крепление")+
                ". Сектор "+mount.sector.ToString("0")+"°";
            targetShipWeapon?.SelectMount(slot.index);
        }
        else text=slot.group==SlotGroup.Center_Shields?"Слот щита":"Слот двигателя";
        if(draggedItem!=null && !string.IsNullOrEmpty(draggedItem.id) && !CanDrop(slot,draggedItem,draggedSource))
            text+="\n"+(!CanEdit?"Оснащение заблокировано в бою.":slot==draggedSource?"Исходный слот.":slot.Item!=null?"Слот занят. Сначала снимите модуль.":"Несовместимый слот.");
        ShowTooltip(text,(RectTransform)slot.transform);
    }
    public void DescribeItem(FittingItem item, RectTransform target)
    {
        if(item==null || draggedItem!=null)return;
        string text=item.allowedGroup!=SlotGroup.Top_Weapons?item.name:
            item.mountType==WeaponMountType.Fixed?"Курсовое орудие. Сектор "+item.FixedFireSector.ToString("0")+"°":
            "Турель. Наведение "+item.trackingSpeed.ToString("0")+"°/с";
        ShowTooltip(text,target);
    }
    private void ShowTooltip(string text, RectTransform target)
    {
        var canvas=GetComponentInParent<Canvas>().rootCanvas;
        var bounds=(RectTransform)canvas.transform;
        if(slotTooltip==null)
        {
            var panel=CombatUIStyle.Panel("SlotTooltip",transform,new Color(.04f,.06f,.09f,.98f));panel.raycastTarget=false;
            slotTooltip=CombatUIStyle.Text("Text",panel.transform,"",16);slotTooltip.raycastTarget=false;
            slotTooltip.textWrappingMode=TextWrappingModes.Normal;
            slotTooltip.enableAutoSizing=false;
            CombatUIStyle.Rect(slotTooltip.transform,Vector2.zero,Vector2.one,new Vector2(12,8),new Vector2(-12,-8));
        }
        var rect=(RectTransform)slotTooltip.transform.parent;
        rect.gameObject.SetActive(true);rect.SetAsLastSibling();
        slotTooltip.text=text;
        // Work in root Canvas units, not screen pixels: honors CanvasScaler and Game View zoom.
        float width=Mathf.Min(300,bounds.rect.width-24);
        float height=slotTooltip.GetPreferredValues(text,width-24,0).y+16;
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
        rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;
        var corners=new Vector3[4];target.GetWorldCorners(corners);
        Vector2 low=bounds.InverseTransformPoint(corners[0]), high=bounds.InverseTransformPoint(corners[2]);
        var visible=bounds.rect;
        float y=high.y+10+height*.5f;
        if(y+height*.5f>visible.yMax-8)y=low.y-10-height*.5f;
        float x=Mathf.Clamp((low.x+high.x)*.5f,visible.xMin+width*.5f+8,visible.xMax-width*.5f-8);
        y=Mathf.Clamp(y,visible.yMin+height*.5f+8,visible.yMax-height*.5f-8);
        rect.position=bounds.TransformPoint(new Vector3(x,y,0));
    }
    private void Reject(FittingItem item)
    {
        presetMessage=item.mountType==WeaponMountType.Fixed?"Требуется курсовое крепление":"Требуется турельное/универсальное крепление";
        if(detailsLabel!=null)detailsLabel.text=presetMessage;
    }

    // ==================== APPLY TO SHIP ====================

    private void ApplyLoadoutToShip()
    {
        if (!CanEdit) return;
        if (targetShipHealth == null) ResolveTargetShip();
        SyncSlotsToLoadout();
        ShipStatsCalculator.Apply(targetShipHealth, shipDefinition, currentLoadout);
        RefreshStats();
    }
    private void OnShipSelected(ShipDefinition newShip)
    {
        if (!CanEdit) return;
        if (newShip == null || newShip == shipDefinition) return;

        CancelDrags();

        SaveCurrentLoadout();

        if(!ShipLoadoutManager.EnsureShip(newShip,null,out var selectionError))
        {presetMessage=selectionError;return;}

        if (PlayerProfile.Instance != null)
            PlayerProfile.Instance.SetCurrentShip(newShip.shipId);

        if (targetShipHealth != null)
        {
            targetShipHealth.shipDefinition = newShip;


            // 👇 НОВОЕ: смена спрайта
            var visuals = targetShipHealth.GetComponentInChildren<ShipVisuals>();
            if (visuals != null)
                visuals.Apply(newShip);

           


                if (targetShipHealth.Data != null)
            {
                // Ship selection historically resets to full before applying equipment deltas.
                targetShipHealth.Data.Damage.InitializeMaximums(newShip.maxHullHP, newShip.baseShieldHP);
                targetShipHealth.Data.mass = newShip.baseMass;
                targetShipHealth.Data.lockTime = newShip.baseLockTime;
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
