# Сводка архитектуры кораблей Unity-проекта (перед добавлением Sidewinder)

Проект: `c:\Personal\Unity\TheGame\My project`
Сцена: `Assets/Scenes/CombatSandbox.unity`
Дата анализа: 2026-10-20
Код проекта **не изменялся** — это только описание текущего состояния.

---

## 1. Архитектура кораблей

### 1.1 `ShipDefinition` — ScriptableObject с данными типа корабля

**Путь:** `Assets/Scripts/Ships/ShipDefinition.cs`
**GUID скрипта:** `0ae1760575bf1904e833f28114de4e88`
**Существующий ассет:** `Assets/Ships/CobraMK3.asset` (GUID `c346ed5929ae76343a04e9f5c080ea0c`)

```csharp
using UnityEngine;

/// <summary>
/// ScriptableObject: паспорт типа корабля. Базовые ТТХ и количество слотов.
/// Один ассет на тип (CobraMK3, Sidewinder, Python...).
/// </summary>
[CreateAssetMenu(fileName = "ShipDefinition", menuName = "Space/Ship Definition")]
public class ShipDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Уникальный ключ для сохранения. Не менять после релиза!")]
    public string shipId = "cobra_mk3";

    [Tooltip("Отображаемое имя в UI.")]
    public string displayName = "Cobra MK3";

    [Tooltip("Иконка корабля (для UI).")]
    public Sprite icon;

    [Header("Base Stats")]
    public float maxHullHP = 120f;
    public float baseShieldHP = 60f;
    public float baseMass = 3f;

    public float baseMaxSpeed = 6f;
    public float baseAcceleration = 14f;
    public float baseRotationSpeed = 300f;

    [Tooltip("Базовое время захвата цели (сек). Меньше — быстрее.")]
    public float baseLockTime = 0.5f;

    [Header("Slots")]
    public int weaponSlots = 4;
    public int shieldSlots = 4;
    public int engineSlots = 4;
}
```

Фактические значения `CobraMK3.asset`: `shipId: cobra_mk3`, `displayName: Cobra MK3`, `icon: {fileID: 0}`, `maxHullHP: 120`, `baseShieldHP: 60`, `baseMass: 3`, `baseMaxSpeed: 6`, `baseAcceleration: 14`, `baseRotationSpeed: 300`, слоты `4/4/4`.
Поля `baseLockTime` и `icon` в ассете не сериализованы — берутся дефолты класса (`0.5` / `null`).

### 1.2 `ShipLibrary` — ScriptableObject-справочник кораблей

**Путь:** `Assets/Scripts/Ships/ShipLibrary.cs`
**GUID скрипта:** `af0128e53b037b2469c37ef34a104d8f`
**Ассет:** `Assets/Ships/ShipLibrary.asset` (GUID `2315397736df8414aac7f72fe4828679`)

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject-справочник всех доступных кораблей в игре.
/// Нужен для UI-селектора: получить список ShipDefinition по имени.
/// </summary>
[CreateAssetMenu(fileName = "ShipLibrary", menuName = "Space/Ship Library")]
public class ShipLibrary : ScriptableObject
{
    public List<ShipDefinition> ships = new List<ShipDefinition>();

    public ShipDefinition GetById(string shipId)
    {
        foreach (var s in ships)
        {
            if (s != null && s.shipId == shipId)
                return s;
        }
        return null;
    }
}
```

**Содержимое `ShipLibrary.asset` сейчас — ровно один корабль:**

```yaml
ships:
- {fileID: 11400000, guid: c346ed5929ae76343a04e9f5c080ea0c, type: 2}   # CobraMK3
```

### 1.3 `ShipRegistry` — реестр `shipId -> ShipDefinition`

**Путь:** `Assets/Scripts/ShipRegistry.cs`
**GUID скрипта:** `991a96ca3d8f9474d805f9ab92901104`
**Расположение в сцене:** объект `[Managers]`, поле `shipLibrary` -> `ShipLibrary.asset`

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Реестр всех ShipDefinition по shipId.
/// Заполняется из ShipLibrary или вручную.
/// </summary>
public class ShipRegistry : MonoBehaviour
{
    public static ShipRegistry Instance { get; private set; }

    [Tooltip("Справочник кораблей (ShipLibrary). Если задан — реестр заполнится из него.")]
    public ShipLibrary shipLibrary;

    private Dictionary<string, ShipDefinition> registry = new Dictionary<string, ShipDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildRegistry();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildRegistry()
    {
        registry.Clear();

        if (shipLibrary == null || shipLibrary.ships == null)
            return;

        foreach (var ship in shipLibrary.ships)
        {
            if (ship == null || string.IsNullOrEmpty(ship.shipId))
                continue;

            if (registry.ContainsKey(ship.shipId))
            {
                Debug.LogWarning($"[ShipRegistry] Дубликат shipId: {ship.shipId}. Пропускаем.");
                continue;
            }

            registry[ship.shipId] = ship;
        }

        Debug.Log($"[ShipRegistry] Зарегистрировано кораблей: {registry.Count}");
    }

    /// <summary>
    /// Получить ShipDefinition по shipId. Возвращает null, если не найден.
    /// </summary>
    public ShipDefinition Get(string shipId)
    {
        if (string.IsNullOrEmpty(shipId))
            return null;

        return registry.TryGetValue(shipId, out var def) ? def : null;
    }

    /// <summary>
    /// Первый доступный корабль из реестра (fallback).
    /// </summary>
    public ShipDefinition GetFirst()
    {
        foreach (var kv in registry)
            return kv.Value;
        return null;
    }
}
```

### 1.4 `PlayerProfile` — выбранный корабль игрока + сохранение

**Путь:** `Assets/Scripts/PlayerProfile.cs`
**GUID скрипта:** `ab00f2a670dab324487dd199aa865342`
**Расположение в сцене:** объект `[Managers]`, в сцене `currentShipId: cobra_mk3`, `credits: 1000`, `reputation: 0`

```csharp
using UnityEngine;

/// <summary>
/// Профиль игрока: текущий корабль, деньги, репутация.
/// Синглтон — один на сцену, живёт на [Managers].
/// </summary>
public class PlayerProfile : MonoBehaviour
{
    public static PlayerProfile Instance { get; private set; }

    [Header("Current Ship")]
    [Tooltip("shipId текущего корабля игрока. Должен совпадать с ShipDefinition.shipId.")]
    public string currentShipId = "cobra_mk3";

    [Header("Future")]
    public int credits = 1000;
    public int reputation = 0;

    private void Awake()
    {
        // Синглтон
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Сменить текущий корабль игрока. Загружает loadout нового корабля.
    /// </summary>
    public void SetCurrentShip(string shipId)
    {
        if (string.IsNullOrEmpty(shipId))
            return;

        currentShipId = shipId;
        PlayerPrefs.SetString("player_current_ship_id", shipId);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerProfile] Текущий корабль: {shipId}");
    }

    private void Start()
    {
        // Загружаем сохранённый корабль
        if (PlayerPrefs.HasKey("player_current_ship_id"))
        {
            currentShipId = PlayerPrefs.GetString("player_current_ship_id");
        }
    }
}
```

**Ключ сохранения:** `PlayerPrefs` -> строка `player_current_ship_id` (единственный ключ проекта, относящийся к кораблям).
Значение в инспекторе (`cobra_mk3`) — только дефолт; в `Start()` перезаписывается из PlayerPrefs, если ключ есть.

---


## 2. Фитинг: данные, сохранение, применение

### 2.1 `FittingItem` — описание одного модуля (оружие/щит/двигатель)

**Путь:** `Assets/Scripts/FittingItem.cs`

```csharp
using UnityEngine;

/// <summary>
/// Тип визуализации выстрела, зависит от установленного оружия.
/// </summary>
public enum WeaponVisualType
{
    /// <summary>Тонкий луч лазера.</summary>
    Laser,

    /// <summary>Широкий луч-капля (плазма).</summary>
    Plasma
}

/// <summary>
/// Один устанавливаемый модуль (оружие, щит, двигатель).
/// Хранит модификаторы характеристик корабля и визуальный тип.
/// </summary>
[System.Serializable]
public class FittingItem
{
    public string id;
    public string name;
    public SlotGroup allowedGroup;
    public Color iconColor = Color.cyan;
    public string description;

    // Модификаторы характеристик
    public float damageBonus;
    public float cooldownBonus;
    public float shieldBonus;
    public float speedBonus;
    public float accelBonus;
    public float massBonus;

    [Tooltip("Бонус к времени захвата цели. Отрицательный = быстрее захват.")]
    public float lockTimeBonus;

    // Визуал выстрела (актуально только для оружия)
    public WeaponVisualType visualType = WeaponVisualType.Laser;

    public FittingItem(string id, string name, SlotGroup allowedGroup, Color color, string desc,
                       float damage = 0, float cooldown = 0, float shield = 0, float speed = 0, float accel = 0, float mass = 0,
                       float lockTime = 0,
                       WeaponVisualType visual = WeaponVisualType.Laser)
    {
        this.id = id;
        this.name = name;
        this.allowedGroup = allowedGroup;
        this.iconColor = color;
        this.description = desc;
        this.damageBonus = damage;
        this.cooldownBonus = cooldown;
        this.shieldBonus = shield;
        this.speedBonus = speed;
        this.accelBonus = accel;
        this.massBonus = mass;
        this.lockTimeBonus = lockTime;
        this.visualType = visual;
    }
}
```

### 2.2 `SlotGroup` — логические группы слотов

**Путь:** `Assets/Scripts/Ships/SlotGroup.cs`

```csharp
/// <summary>
/// Логические группы слотов корабля.
/// Используется в FittingItem, FittingSlot, FittingWindow, ShipLoadout.
/// </summary>
public enum SlotGroup
{
    /// <summary>1 Группа: Верхние слоты (Оружие).</summary>
    Top_Weapons,

    /// <summary>3 Группа: Центральные слоты (Защита / Щиты).</summary>
    Center_Shields,

    /// <summary>2 Группа: Нижние слоты (Двигатели / Активки).</summary>
    Bottom_Engines,

    /// <summary>Свободный инвентарь справа.</summary>
    Inventory
}
```

### 2.3 `ShipLoadout` — состояние фитинга корабля (сериализуется в JSON)

**Путь:** `Assets/Scripts/Ships/ShipLoadout.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Состояние фитинга корабля: что установлено в слоты и что лежит в инвентаре.
/// Сериализуется через JsonUtility.
/// </summary>
[System.Serializable]
public class ShipLoadout
{
    public string shipId;

    public List<FittingItem> weaponSlots = new List<FittingItem>();
    public List<FittingItem> shieldSlots = new List<FittingItem>();
    public List<FittingItem> engineSlots = new List<FittingItem>();
    public List<FittingItem> inventory = new List<FittingItem>();

    public ShipLoadout() { }

    public ShipLoadout(string shipId)
    {
        this.shipId = shipId;
    }

    public bool IsEmpty()
    {
        return (weaponSlots == null || weaponSlots.Count == 0)
            && (shieldSlots == null || shieldSlots.Count == 0)
            && (engineSlots == null || engineSlots.Count == 0)
            && (inventory == null || inventory.Count == 0);
    }
}

/// <summary>
/// Обёртка для JsonUtility (он не умеет сериализовать словари, только списки).
/// </summary>
[System.Serializable]
public class ShipLoadoutSaveFile
{
    public List<ShipLoadout> loadouts = new List<ShipLoadout>();
}
```

### 2.4 `ShipLoadoutManager` — сохранение/загрузка фитингов по `shipId`

**Путь:** `Assets/Scripts/Ships/ShipLoadoutManager.cs`

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Статический менеджер сохранения/загрузки фитингов.
/// Хранит loadout'ы по shipId, пишет/читает JSON-файл в persistentDataPath.
/// </summary>
public static class ShipLoadoutManager
{
    private const string FileName = "ship_loadouts.json";

    private static Dictionary<string, ShipLoadout> cache = new Dictionary<string, ShipLoadout>();
    private static bool isLoaded = false;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static ShipLoadout GetLoadout(string shipId)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(shipId))
            return null;

        return cache.TryGetValue(shipId, out var loadout) ? loadout : null;
    }

    public static void SaveLoadout(ShipLoadout loadout)
    {
        if (loadout == null || string.IsNullOrEmpty(loadout.shipId))
            return;

        EnsureLoaded();

        // Санитайзим перед сохранением — вдруг кто-то добавил пустышку.
        Sanitize(loadout);

        cache[loadout.shipId] = loadout;
        SaveToDisk();
    }

    public static void DeleteLoadout(string shipId)
    {
        EnsureLoaded();
        if (cache.Remove(shipId))
            SaveToDisk();
    }

    public static void ClearAll()
    {
        cache.Clear();
        isLoaded = true;
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }

    public static string GetSavePath() => FilePath;

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        if (!File.Exists(FilePath))
            return;

        try
        {
            string json = File.ReadAllText(FilePath);
            var saveFile = JsonUtility.FromJson<ShipLoadoutSaveFile>(json);

            if (saveFile != null && saveFile.loadouts != null)
            {
                foreach (var loadout in saveFile.loadouts)
                {
                    if (loadout != null && !string.IsNullOrEmpty(loadout.shipId))
                    {
                        Sanitize(loadout);
                        cache[loadout.shipId] = loadout;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShipLoadoutManager] Не удалось загрузить {FilePath}: {e.Message}");
        }
    }

    private static void SaveToDisk()
    {
        try
        {
            var saveFile = new ShipLoadoutSaveFile();
            saveFile.loadouts.AddRange(cache.Values);

            string json = JsonUtility.ToJson(saveFile, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShipLoadoutManager] Не удалось сохранить {FilePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Удаляет пустые элементы (null или без id) из списков loadout.
    /// JsonUtility при загрузке превращает null в пустые объекты — их нужно чистить.
    /// </summary>
    private static void Sanitize(ShipLoadout loadout)
    {
        if (loadout == null) return;

        SanitizeList(loadout.weaponSlots);
        SanitizeList(loadout.shieldSlots);
        SanitizeList(loadout.engineSlots);
        SanitizeList(loadout.inventory);

        if (loadout.weaponSlots == null) loadout.weaponSlots = new List<FittingItem>();
        if (loadout.shieldSlots == null) loadout.shieldSlots = new List<FittingItem>();
        if (loadout.engineSlots == null) loadout.engineSlots = new List<FittingItem>();
        if (loadout.inventory == null) loadout.inventory = new List<FittingItem>();
    }

    private static void SanitizeList(List<FittingItem> list)
    {
        if (list == null) return;
        list.RemoveAll(item => item == null || string.IsNullOrEmpty(item.id));
    }
}
```

**Ключевые факты по сохранению:**
- Файл: `Application.persistentDataPath` + `ship_loadouts.json`
  (на Windows: `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\ship_loadouts.json`).
- Структура JSON: `{"loadouts": [ { "shipId": "...", "weaponSlots": [...], "shieldSlots": [...], "engineSlots": [...], "inventory": [...] } ]}`.
- Ключом записи является `ShipLoadout.shipId`, т.е. **у каждого корабля свой независимый фитинг** — это и есть точка расширения под Sidewinder.
- `Sanitize()` вычищает из списков `null`-элементы и элементы с пустым `id` (лечит баг `JsonUtility`, превращающего `null` в списках в пустые объекты).
- `GetLoadout(shipId)` возвращает `null`, если фитинга для корабля ещё нет (у Sidewinder при первом запуске будет `null`).

### 2.5 `LoadoutApplier` — применение сохранённого фитинга при старте сцены

**Путь:** `Assets/Scripts/Ships/LoadoutApplier.cs`
**Расположение в сцене:** объект `[Managers]`, `targetShipHealth: {fileID: 1898430270}` (корабль игрока)

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Применяет сохранённый loadout текущего корабля игрока при старте игры.
/// Корабль определяется через PlayerProfile + ShipRegistry.
/// </summary>
public class LoadoutApplier : MonoBehaviour
{
    [Tooltip("Корабль игрока. Если null — ищется по тегу 'Player'.")]
    public ShipHealth targetShipHealth;

    private void Start()
    {
        // Дадим один кадр, чтобы PlayerProfile и ShipRegistry успели инициализироваться.
        Invoke(nameof(ApplySavedLoadout), 0.05f);
    }

    private void ApplySavedLoadout()
    {
        // 1. Находим корабль игрока
        if (targetShipHealth == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                targetShipHealth = playerObj.GetComponent<ShipHealth>();
        }

        if (targetShipHealth == null)
        {
            Debug.LogWarning("[LoadoutApplier] Корабль игрока не найден.");
            return;
        }

        // 2. Определяем ShipDefinition
        ShipDefinition def = targetShipHealth.shipDefinition;

        if (def == null && PlayerProfile.Instance != null && ShipRegistry.Instance != null)
        {
            def = ShipRegistry.Instance.Get(PlayerProfile.Instance.currentShipId);
        }

        if (def == null && ShipRegistry.Instance != null)
        {
            def = ShipRegistry.Instance.GetFirst();
        }

        if (def == null)
        {
            Debug.LogWarning("[LoadoutApplier] Не удалось определить ShipDefinition.");
            return;
        }

        // 3. Применяем ShipDefinition к кораблю (базовые статы)
        targetShipHealth.shipDefinition = def;
        ApplyBaseStats(targetShipHealth, def);

        // 4. Загружаем loadout
        var loadout = ShipLoadoutManager.GetLoadout(def.shipId);
        if (loadout == null)
        {
            Debug.Log($"[LoadoutApplier] Нет сохранённого loadout для {def.shipId}.");
            return;
        }

        ApplyLoadout(targetShipHealth, def, loadout);
        Debug.Log($"[LoadoutApplier] Loadout применён для {def.displayName} (lockTime={targetShipHealth.Data.lockTime:F2}s)");
    }

    private void ApplyBaseStats(ShipHealth health, ShipDefinition def)
    {
        if (health.Data == null) return;

        health.Data.maxHullHP = def.maxHullHP;
        health.Data.currentHullHP = def.maxHullHP;
        health.Data.maxShieldHP = def.baseShieldHP;
        health.Data.currentShieldHP = def.baseShieldHP;
        health.Data.mass = def.baseMass;
        health.Data.lockTime = def.baseLockTime;

        var movement = health.GetComponent<ShipMovement>();
        if (movement != null)
        {
            movement.maxSpeed = def.baseMaxSpeed;
            movement.acceleration = def.baseAcceleration;
            movement.rotationSpeed = def.baseRotationSpeed;
            movement.ApplyMassFromData();
        }
    }

    private void ApplyLoadout(ShipHealth health, ShipDefinition def, ShipLoadout loadout)
    {
        var shipWeapon = health.GetComponent<ShipWeapon>();
        var shipMovement = health.GetComponent<ShipMovement>();

        // Оружие
        if (shipWeapon != null)
        {
            var installedWeapons = new List<FittingItem>();
            if (loadout.weaponSlots != null)
                foreach (var w in loadout.weaponSlots)
                    if (w != null) installedWeapons.Add(w);

            shipWeapon.SetEquippedWeapons(installedWeapons);
        }

        // Бонусы от установленных модулей
        float shieldBonus = 0f, speedBonus = 0f, accelBonus = 0f, massBonus = 0f, lockTimeBonus = 0f;
        ApplyBonuses(loadout.weaponSlots, ref shieldBonus, ref speedBonus, ref accelBonus, ref massBonus, ref lockTimeBonus);
        ApplyBonuses(loadout.shieldSlots, ref shieldBonus, ref speedBonus, ref accelBonus, ref massBonus, ref lockTimeBonus);
        ApplyBonuses(loadout.engineSlots, ref shieldBonus, ref speedBonus, ref accelBonus, ref massBonus, ref lockTimeBonus);

        if (health.Data != null)
        {
            // Щит с учётом дельты
            float oldMaxShield = health.Data.maxShieldHP;
            float newMaxShield = def.baseShieldHP + shieldBonus;
            float delta = newMaxShield - oldMaxShield;

            health.Data.maxShieldHP = newMaxShield;
            health.Data.currentShieldHP = Mathf.Clamp(
                health.Data.currentShieldHP + delta,
                0f,
                newMaxShield);

            health.Data.mass = Mathf.Max(0.5f, def.baseMass + massBonus);

            // Захват цели: база + бонусы, минимум 0.05 сек
            health.Data.lockTime = Mathf.Max(0.05f, def.baseLockTime + lockTimeBonus);
        }

        if (shipMovement != null)
        {
            shipMovement.maxSpeed = def.baseMaxSpeed + speedBonus;
            shipMovement.acceleration = def.baseAcceleration + accelBonus;
            shipMovement.ApplyMassFromData();
        }
    }

    private void ApplyBonuses(List<FittingItem> list,
        ref float shield, ref float speed, ref float accel, ref float mass, ref float lockTime)
    {
        if (list == null) return;
        foreach (var item in list)
        {
            if (item == null) continue;
            shield += item.shieldBonus;
            speed += item.speedBonus;
            accel += item.accelBonus;
            mass += item.massBonus;
            lockTime += item.lockTimeBonus;
        }
    }
}
```

**Логика определения корабля (важно для Sidewinder):**
1. `targetShipHealth` (или поиск по тегу `Player`).
2. `targetShipHealth.shipDefinition` — если назначен, он главный приоритет. **В `PlayerShip.prefab` это поле пустое (`shipDefinition: {fileID: 0}`)**, поэтому фактически определение идёт из профиля.
3. Иначе `ShipRegistry.Instance.Get(PlayerProfile.Instance.currentShipId)`.
4. Иначе `ShipRegistry.Instance.GetFirst()` — fallback (сейчас это CobraMK3 как первый в списке).
5. Затем `ApplyBaseStats` (корпус/щит/масса/lockTime + скорость/ускорение/поворот) и `ApplyLoadout` (оружие + бонусы модулей).

**Особенность:** `ApplyBaseStats` **сбрасывает HP/щит на максимум** (`currentHullHP = maxHullHP`, `currentShieldHP = baseShieldHP`). Для смены корабля в бою это «чит» (отмечено в `readme.md`).

---


## 3. Здоровье, модули и урон

### 3.1 `ShipData` — динамическое состояние корабля

**Путь:** `Assets/Scripts/Ships/ShipData.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Полное динамическое состояние корабля: прочность корпуса, щиты, точность, масса и состояние модулей.
/// </summary>
public class ShipData
{
    public const float DefaultWeaponMaxHP = 30f;
    public const float DefaultShieldMaxHP = 40f;
    public const float DefaultEngineMaxHP = 30f;

    // Прочность корпуса
    public float maxHullHP;
    public float currentHullHP;

    // Параметры физики и инерции
    public float mass = 2f;             // Масса корабля (влияет на инерцию)
    public float enginePower = 15f;     // Тяга маршевых двигателей

    // Параметры для математики боя
    public float accuracy = 0.85f;      // Базовая точность (85%)
    public float baseEvasion = 0.10f;   // Базовое уклонение неподвижного корабля (10%)
    public float speedEvasionMultiplier = 0.05f; // Бонус уклонения за каждый unit/sec скорости

    // Динамический щит
    public float maxShieldHP = 50f;
    public float currentShieldHP = 50f;

    // Захват цели: время в секундах (база + бонусы модулей)
    public float lockTime = 0.5f;

    // Состояния повреждений отдельных узлов
    public Dictionary<ModuleType, ShipModule> modules = new Dictionary<ModuleType, ShipModule>();

    public ShipData(float maxHull)
    {
        maxHullHP = Mathf.Max(0f, maxHull);
        currentHullHP = maxHullHP;

        modules[ModuleType.Weapon] = new ShipModule(ModuleType.Weapon, DefaultWeaponMaxHP);
        modules[ModuleType.Shield] = new ShipModule(ModuleType.Shield, DefaultShieldMaxHP);
        modules[ModuleType.Engine] = new ShipModule(ModuleType.Engine, DefaultEngineMaxHP);
    }

    public bool IsDestroyed => currentHullHP <= 0f;

    /// <summary>
    /// Вычисляет итоговое уклонение с учётом текущей физической скорости.
    /// </summary>
    public float GetTotalEvasion(float currentSpeed)
    {
        var engineMod = GetModule(ModuleType.Engine);
        if (engineMod != null && engineMod.isDestroyed)
            return 0f; // С выбитым двигателем уклонение падает до нуля

        return baseEvasion + (currentSpeed * speedEvasionMultiplier);
    }

    public ShipModule GetModule(ModuleType type)
    {
        return modules.TryGetValue(type, out var module) ? module : null;
    }

    public float AbsorbDamageWithShield(float damage)
    {
        var shieldMod = GetModule(ModuleType.Shield);

        if (shieldMod != null && shieldMod.isDestroyed)
            return damage;

        if (currentShieldHP <= 0f)
            return damage;

        if (currentShieldHP >= damage)
        {
            currentShieldHP -= damage;
            return 0f;
        }

        float excessDamage = damage - currentShieldHP;
        currentShieldHP = 0f;
        return excessDamage;
    }

    public void TakeHullDamage(float amount)
    {
        if (amount <= 0f)
            return;

        currentHullHP = Mathf.Max(0f, currentHullHP - amount);
    }

    public void TakeModuleDamage(ModuleType type, float amount)
    {
        var module = GetModule(type);
        module?.TakeDamage(amount);
    }
}
```

### 3.2 `ShipModule` — состояние отдельного модуля

**Путь:** `Assets/Scripts/ShipModule.cs`

```csharp
using UnityEngine;

/// <summary>
/// Класс физического состояния отдельного модуля (оружие, щит, двигатель)
/// </summary>
public class ShipModule
{
    public ModuleType type;
    public float maxHP;
    public float currentHP;

    public bool isDestroyed => currentHP <= 0f;

    public ShipModule(ModuleType type, float maxHP)
    {
        this.type = type;
        this.maxHP = Mathf.Max(0f, maxHP);
        this.currentHP = this.maxHP;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Max(0f, currentHP - amount);
    }

    public void Repair(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }
}
```

### 3.3 `ShipHealth` — компонент корабля (связывает `ShipDefinition` и `ShipData`)

**Путь:** `Assets/Scripts/Ships/ShipHealth.cs`

```csharp
using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    private const float ModuleHitChance = 0.4f;

    private static readonly ModuleType[] AllModuleTypes =
    {
        ModuleType.Weapon,
        ModuleType.Shield,
        ModuleType.Engine
    };

    [Header("Ship Definition")]
    [Tooltip("ScriptableObject с базовыми ТТХ. Если назначен — maxHullHP/shield/mass берутся оттуда.")]
    public ShipDefinition shipDefinition;

    [Header("Fallback")]
    [Tooltip("Fallback max hull HP, если ShipDefinition не назначен.")]
    public float maxHullHP = 100f;

    public ShipData Data { get; private set; }

    public event Action OnDamageTaken;
    public event Action OnDestroyed;

    private bool isDead;

    private void Awake()
    {
        float hull = shipDefinition != null ? shipDefinition.maxHullHP : maxHullHP;
        Data = new ShipData(hull);

        if (shipDefinition != null)
        {
            Data.maxShieldHP = shipDefinition.baseShieldHP;
            Data.currentShieldHP = shipDefinition.baseShieldHP;
            Data.mass = shipDefinition.baseMass;
            Data.lockTime = shipDefinition.baseLockTime;
        }
    }

    private void Update()
    {
        if (Data == null || !Data.IsDestroyed)
            return;

        Die();
    }

    public void TakeDamage(float amount, ModuleType? targetedModule = null)
    {
        if (Data == null || Data.IsDestroyed || amount <= 0f)
            return;

        float remainingDamage = Data.AbsorbDamageWithShield(amount);

        if (remainingDamage <= 0f)
        {
            OnDamageTaken?.Invoke();
            return;
        }

        if (targetedModule.HasValue)
        {
            ApplyModuleDamage(remainingDamage, targetedModule.Value);
        }
        else if (UnityEngine.Random.value < ModuleHitChance)
        {
            ApplyModuleDamage(remainingDamage, AllModuleTypes[UnityEngine.Random.Range(0, AllModuleTypes.Length)]);
        }
        else
        {
            ApplyHullDamage(remainingDamage);
        }

        OnDamageTaken?.Invoke();
    }

    private void ApplyModuleDamage(float amount, ModuleType moduleType)
    {
        var module = Data.GetModule(moduleType);
        if (module == null)
        {
            ApplyHullDamage(amount);
            return;
        }

        var wasAlreadyDestroyed = module.isDestroyed;
        Data.TakeModuleDamage(moduleType, amount);

        if (wasAlreadyDestroyed)
        {
            float spilloverDamage = amount * 0.5f;
            Data.TakeHullDamage(spilloverDamage);
            Debug.Log($"{gameObject.name}: Попадание в выбитый модуль {moduleType}! {spilloverDamage} урона прошло в корпус.");
        }
        else
        {
            Debug.Log($"{gameObject.name}: Попадание в модуль {moduleType} - урон {amount}, HP модуля {module.currentHP}/{module.maxHP}{(module.isDestroyed ? " (МОДУЛЬ ВЫБИТ)" : string.Empty)}");
        }
    }

    private void ApplyHullDamage(float amount)
    {
        Data.TakeHullDamage(amount);
        Debug.Log($"{gameObject.name}: Попадание в корпус - урон {amount}, HP корпуса {Data.currentHullHP}/{Data.maxHullHP}");
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        OnDestroyed?.Invoke();
        Debug.Log($"{gameObject.name} уничтожен!");
        gameObject.SetActive(false);
    }
}
```

**Что важно для второго корабля:**
- `ShipHealth.shipDefinition` — единственное поле, через которое корабль получает ТТХ. В `PlayerShip.prefab` оно **не назначено** (`{fileID: 0}`), поэтому используется fallback `maxHullHP = 100` в `Awake()`, а фактические статы подставляет `LoadoutApplier` по `PlayerProfile.currentShipId`.
- `Data.modules` — это **всегда ровно 3 модуля** (`Weapon`, `Shield`, `Engine`) с фиксированными HP (`30/40/30`). Количество слотов фитинга (`weaponSlots` и т.д. в `ShipDefinition`) сюда **не влияет**: у Sidewinder с 2/2/3 слотами модулей всё равно будет 3 (по одному на группу).
- `Data.lockTime` берётся из `ShipDefinition.baseLockTime` + бонусы модулей, минимум `0.05`.

### 3.4 `ModuleType` — типы модулей

**Путь:** `Assets/Scripts/ModuleType.cs`

```csharp
/// <summary>
/// Logical ship module categories.
/// </summary>
/// <remarks>
/// Ship modules are addressed by these enum values instead of magic strings,
/// which keeps module lookups compile-time safe and refactor friendly.
/// </remarks>
public enum ModuleType
{
    /// <summary>Offensive module.</summary>
    Weapon,

    /// <summary>Defensive module.</summary>
    Shield,

    /// <summary>Propulsion module.</summary>
    Engine
}
```

---


## 4. Движение, оружие, способности

### 4.1 `ShipMovement` — физика и маневрирование

**Путь:** `Assets/Scripts/ShipMovement.cs`

```csharp
using UnityEngine;

/// <summary>
/// Drives a ship towards a target position using physics forces and inertia.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ShipMovement : MonoBehaviour
{
    private const float ArriveDistance = 0.4f;

    [Tooltip("Maximum velocity limit.")]
    public float maxSpeed = 5f;

    [Tooltip("Acceleration power force.")]
    public float acceleration = 12f;

    [Tooltip("Turn speed in degrees per second.")]
    public float rotationSpeed = 300f;

    [Tooltip("Check TRUE if your sprite image points UP (along Y axis). Check FALSE if it points RIGHT (along X axis).")]
    public bool spriteNoseIsUp = true;

    /// <summary>World position the ship is currently flying to.</summary>
    public Vector3 TargetPosition { get; private set; }

    /// <summary>True while the ship has an active move order.</summary>
    public bool HasTarget { get; private set; } = false;

    public Rigidbody2D Rb { get; private set; }
    private ShipHealth shipHealth;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        shipHealth = GetComponent<ShipHealth>();

        Rb.gravityScale = 0f;
        Rb.linearDamping = 1.2f;
        Rb.angularDamping = 3f;
    }

    private void Start()
    {
        TargetPosition = transform.position;
        ApplyMassFromData();
    }

    public void ApplyMassFromData()
    {
        if (shipHealth != null && shipHealth.Data != null)
        {
            Rb.mass = shipHealth.Data.mass;
        }
    }

    public void SetTargetPosition(Vector3 pos)
    {
        TargetPosition = new Vector3(pos.x, pos.y, 0f);
        HasTarget = true;
    }

    public void Stop()
    {
        HasTarget = false;
    }

    private void FixedUpdate()
    {
        if (shipHealth != null && shipHealth.Data != null)
        {
            var engineMod = shipHealth.Data.GetModule(ModuleType.Engine);
            if (engineMod != null && engineMod.isDestroyed)
                return;
        }

        if (!HasTarget || Rb == null)
            return;

        Vector2 currentPos = Rb.position;
        Vector2 toTarget = (Vector2)TargetPosition - currentPos;
        float distance = toTarget.magnitude;

        if (distance < ArriveDistance)
        {
            Stop();
            return;
        }

        Vector2 desiredDirection = toTarget.normalized;

        // 1. Поворот корабля в сторону цели
        RotateTowardsDirection(desiredDirection);

        Vector2 forwardVector = GetForwardVector();

        // 2. Гашение бокового заноса (дрейфа)
        Vector2 forwardVelocity = forwardVector * Vector2.Dot(Rb.linearVelocity, forwardVector);
        Vector2 rightVector = new Vector2(-forwardVector.y, forwardVector.x);
        Vector2 rightVelocity = rightVector * Vector2.Dot(Rb.linearVelocity, rightVector);

        // Активно гасим боковую скорость (0.15f вместо 0.9f) для точного вхождения в повороты
        Rb.linearVelocity = forwardVelocity + rightVelocity * 0.15f;

        // 3. Вычисление alignment (насколько нос сориентирован на цель)
        float alignment = Vector2.Dot(forwardVector, desiredDirection);

        // Импульс даем ТОЛЬКО если сориентированы в сторону цели (alignment > 0)
        if (alignment > 0f)
        {
            // Чем точнее повернут нос к цели, тем больше тяги применяем
            float thrustFactor = Mathf.Pow(alignment, 2f);
            Rb.AddForce(forwardVector * (acceleration * thrustFactor), ForceMode2D.Force);
        }

        // Ограничение максимальной скорости
        if (Rb.linearVelocity.magnitude > maxSpeed)
        {
            Rb.linearVelocity = Rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void RotateTowardsDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (spriteNoseIsUp)
        {
            targetAngle -= 90f;
        }

        float currentAngle = Rb.rotation;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.fixedDeltaTime);
        Rb.MoveRotation(newAngle);
    }

    public Vector2 GetForwardVector()
    {
        return spriteNoseIsUp ? (Vector2)transform.up : (Vector2)transform.right;
    }
}
```

**Связь с кораблем:** `maxSpeed`, `acceleration`, `rotationSpeed` перезаписываются из `ShipDefinition` (+ бонусы модулей). Масса берётся из `ShipData.mass` через `ApplyMassFromData()`.
**Замечание для Sidewinder:** в `PlayerShip.prefab` `spriteNoseIsUp: 1`, тогда как в `readme.md` указано, что в сцене это переопределено в `false` (нос вправо). Для отдельного префаба Sidewinder этот флаг нужно задать осознанно.

### 4.2 `ShipWeapon` — оружие, захват цели, тактика

**Путь:** `Assets/Scripts/ShipWeapon.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Тактические режимы поведения корабля в бою
/// </summary>
public enum CombatBehavior
{
    Orbit,
    KeepDistance,
    Flank
}

public class ShipWeapon : MonoBehaviour
{
    [Header("Weapon & Projectile Setup")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    public Transform weaponPoint
    {
        get => firePoint != null ? firePoint : transform;
        set => firePoint = value;
    }

    public float bulletSpeed = 15f;
    public float range = 8f;
    public float baseCooldown = 0.6f;
    public float firingArcAngle = 60f;

    [Header("Fallback Weapon (если фитинг не задан)")]
    public bool useFallbackWeapon = false;
    public float fallbackDamage = 0.5f;
    public Color fallbackColor = new Color(1f, 0.3f, 0.3f, 1f);
    public WeaponVisualType fallbackVisual = WeaponVisualType.Laser;

    [Header("Shot Visuals")]
    public float laserVisualDuration = 0.1f;
    public float plasmaVisualDuration = 0.15f;
    public float laserWidth = 0.05f;
    public float plasmaWidth = 0.25f;

    [Header("Tactics & AI")]
    public CombatBehavior behavior = CombatBehavior.Orbit;
    public bool orbitClockwise = true;

    [Tooltip("Приоритетный модуль для выбивания (null = авто-распределение урона)")]
    [System.NonSerialized]
    public ModuleType? targetedModule = null;

    [HideInInspector] public bool manualMoveOrder = false;

    [Header("Target Lock (fallback)")]
    [Tooltip("Fallback время захвата, если ShipData недоступен. Обычно берётся из ShipData.lockTime.")]
    public float fallbackLockTime = 0.5f;

    // ============ TARGET STATE ============

    private ShipHealth target;
    public ShipHealth Target
    {
        get => target;
        private set
        {
            if (target != value)
            {
                target = value;
                OnTargetChanged?.Invoke(target);
            }
        }
    }

    private ShipHealth pendingTarget;
    public ShipHealth PendingTarget => pendingTarget;

    private float lockProgress = 0f;
    public float LockProgress => lockProgress;

    public bool IsLocking => pendingTarget != null;

    // ============ EVENTS ============

    public event Action<ShipHealth> OnTargetChanged;
    public event Action<ShipHealth> OnLockStarted;
    public event Action<ShipHealth> OnLockCancelled;
    public event Action<ShipHealth> OnLockCompleted;

    // ============ RUNTIME ============

    private List<FittingItem> activeWeapons = new List<FittingItem>();
    private float nextFireTime = 0f;

    private ShipHealth ownerHealth;
    public ShipHealth OwnerHealth => ownerHealth;
    private ShipMovement shipMovement;

    private float orbitChangeTimer = 0f;
    private float randomRadiusOffset = 0f;

    private Material lineMaterial;

    private void Awake()
    {
        ownerHealth = GetComponentInParent<ShipHealth>();
        shipMovement = GetComponentInParent<ShipMovement>();
        if (shipMovement == null)
            shipMovement = GetComponent<ShipMovement>();

        if (firePoint == null)
        {
            var found = transform.Find("WeaponPoint");
            if (found != null)
                firePoint = found;
        }

        var shader = Shader.Find("Sprites/Default")
                  ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                  ?? Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Legacy Shaders/Diffuse");

        if (shader != null)
            lineMaterial = new Material(shader);
    }

    // ============ LOCK TIME ============

    /// <summary>
    /// Итоговое время захвата: из ShipData, иначе fallback.
    /// </summary>
    private float GetLockTime()
    {
        if (ownerHealth != null && ownerHealth.Data != null)
            return Mathf.Max(0.05f, ownerHealth.Data.lockTime);

        return Mathf.Max(0.05f, fallbackLockTime);
    }

    // ============ PUBLIC API ============

    public void SetBehavior(CombatBehavior newBehavior)
    {
        behavior = newBehavior;

        if (Target != null)
            manualMoveOrder = false;
    }

    public void SetTargetModule(ModuleType? module) => targetedModule = module;

    public void SetTarget(ShipHealth newTarget)
    {
        if (newTarget == null)
        {
            CancelLock();
            Target = null;
            manualMoveOrder = false;
            return;
        }

        if (newTarget == Target && pendingTarget == null)
            return;

        pendingTarget = newTarget;
        lockProgress = 0f;

        OnLockStarted?.Invoke(newTarget);

        Debug.Log($"[ShipWeapon] Начат захват: {newTarget.name} ({GetLockTime():F2} сек)");
    }

    public void SetTargetImmediate(ShipHealth newTarget)
    {
        pendingTarget = null;
        lockProgress = 0f;
        Target = newTarget;
        manualMoveOrder = false;
    }

    public void ClearTarget()
    {
        CancelLock();
        Target = null;
    }

    public void SetEquippedWeapons(List<FittingItem> weapons)
    {
        activeWeapons = weapons;
    }

    private void CancelLock()
    {
        if (pendingTarget != null)
        {
            OnLockCancelled?.Invoke(pendingTarget);
            pendingTarget = null;
            lockProgress = 0f;
        }
    }

    // ============ UPDATE ============

    private void Update()
    {
        if (ownerHealth == null || ownerHealth.Data == null)
            return;

        var weaponModule = ownerHealth.Data.GetModule(ModuleType.Weapon);
        if (ownerHealth.Data.IsDestroyed || weaponModule == null || weaponModule.isDestroyed)
            return;

        if (Time.timeScale <= 0f)
            return;

        UpdateLock();

        if (Target != null && (Target.Data == null || Target.Data.IsDestroyed))
        {
            Target = null;
        }

        if (Target != null && pendingTarget == null && !manualMoveOrder)
        {
            UpdateCombatTactics();
        }

        if (pendingTarget != null)
            return;

        bool isManualFiring = false;
#if ENABLE_INPUT_SYSTEM
        isManualFiring = Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        isManualFiring = Input.GetMouseButton(0);
#endif

        if ((isManualFiring || Target != null) && Time.time >= nextFireTime)
        {
            if (Target != null)
            {
                float dist = Vector2.Distance(transform.position, Target.transform.position);
                bool inRange = dist <= range;
                bool inArc = IsTargetInFiringArc(Target.transform.position);

                if (inRange && inArc)
                {
                    Shoot();
                    nextFireTime = Time.time + baseCooldown;
                }
            }
            else if (isManualFiring)
            {
                Shoot();
                nextFireTime = Time.time + baseCooldown;
            }
        }
    }

    private void UpdateLock()
    {
        if (pendingTarget == null)
            return;

        if (pendingTarget.Data == null || pendingTarget.Data.IsDestroyed
            || !pendingTarget.gameObject.activeInHierarchy)
        {
            Debug.Log($"[ShipWeapon] Захват отменён: цель недоступна.");
            CancelLock();
            return;
        }

        float lockTime = GetLockTime();
        lockProgress += Time.deltaTime / lockTime;

        if (lockProgress >= 1f)
        {
            Target = pendingTarget;
            pendingTarget = null;
            lockProgress = 0f;
            manualMoveOrder = false;

            Debug.Log($"[ShipWeapon] Захват завершён: {Target.name}");
            OnLockCompleted?.Invoke(Target);
        }
    }

    // ============ COMBAT TACTICS ============

    private void UpdateCombatTactics()
    {
        if (shipMovement == null || Target == null) return;

        Vector3 enemyPos = Target.transform.position;
        Vector3 myPos = transform.position;
        Vector3 dirToEnemy = (enemyPos - myPos).normalized;
        float currentDistance = Vector2.Distance(myPos, enemyPos);

        orbitChangeTimer -= Time.deltaTime;
        if (orbitChangeTimer <= 0f)
        {
            orbitChangeTimer = UnityEngine.Random.Range(3f, 6f);
            randomRadiusOffset = UnityEngine.Random.Range(-1.2f, 1.2f);

            if (UnityEngine.Random.value < 0.3f)
                orbitClockwise = !orbitClockwise;
        }

        float optimalDistance = Mathf.Clamp(range * 0.65f + randomRadiusOffset, 2f, range * 0.9f);

        switch (behavior)
        {
            case CombatBehavior.Orbit:
                Vector3 tangent = orbitClockwise
                    ? new Vector3(-dirToEnemy.y, dirToEnemy.x, 0f)
                    : new Vector3(dirToEnemy.y, -dirToEnemy.x, 0f);

                float radialCorrection = (currentDistance - optimalDistance);
                Vector3 orbitDestination = myPos + tangent * 4f + dirToEnemy * radialCorrection;

                shipMovement.SetTargetPosition(orbitDestination);
                break;

            case CombatBehavior.KeepDistance:
                if (currentDistance < optimalDistance)
                {
                    Vector3 retreatPoint = myPos - dirToEnemy * 4f;
                    shipMovement.SetTargetPosition(retreatPoint);
                }
                else
                {
                    shipMovement.SetTargetPosition(enemyPos);
                }
                break;

            case CombatBehavior.Flank:
                Vector3 enemyForward = Target.transform.right;
                var targetMovement = Target.GetComponent<ShipMovement>();
                if (targetMovement != null)
                    enemyForward = targetMovement.GetForwardVector();

                Vector3 rearPosition = enemyPos - enemyForward * optimalDistance;
                shipMovement.SetTargetPosition(rearPosition);
                break;
        }
    }

    private bool IsTargetInFiringArc(Vector3 targetPos)
    {
        Vector2 dirToTarget = (targetPos - transform.position).normalized;
        Vector2 forward = shipMovement != null ? shipMovement.GetForwardVector() : (Vector2)transform.up;

        float angle = Vector2.Angle(forward, dirToTarget);
        return angle <= (firingArcAngle * 0.5f);
    }

    // ============ SHOOT ============

    private void Shoot()
    {
        if (Target != null)
        {
            float dist = Vector2.Distance(transform.position, Target.transform.position);
            if (dist > range || !IsTargetInFiringArc(Target.transform.position))
                return;
        }

        Transform spawnPoint = firePoint != null ? firePoint : transform;

        bool useFitting = activeWeapons != null && activeWeapons.Count > 0;

        if (!useFitting && !useFallbackWeapon)
            return;

        int weaponCount = useFitting ? activeWeapons.Count : 1;

        float spacing = 0.35f;
        float startOffset = -(weaponCount - 1) * spacing * 0.5f;

        Vector2 forward = shipMovement != null
            ? shipMovement.GetForwardVector()
            : (Vector2)spawnPoint.up;

        for (int i = 0; i < weaponCount; i++)
        {
            FittingItem weaponModule = useFitting ? activeWeapons[i] : null;

            float currentDamage = weaponModule != null ? weaponModule.damageBonus : fallbackDamage;
            Color shotColor = weaponModule != null ? weaponModule.iconColor : fallbackColor;
            WeaponVisualType visual = weaponModule != null ? weaponModule.visualType : fallbackVisual;

            Vector3 offset = spawnPoint.right * (startOffset + i * spacing);
            Vector3 spawnPos = spawnPoint.position + offset;

            Vector3 endPos;
            if (Target != null)
                endPos = Target.transform.position;
            else
                endPos = spawnPos + (Vector3)(forward * range);

            if (bulletPrefab != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnPoint.rotation);

                var rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = forward * bulletSpeed;

                var renderer = bullet.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.color = shotColor;
            }
            else
            {
                if (Target != null)
                    Target.TakeDamage(currentDamage, targetedModule);

                DrawShot(spawnPos, endPos, shotColor, visual);
            }
        }
    }

    private void DrawShot(Vector3 from, Vector3 to, Color color, WeaponVisualType visual)
    {
        GameObject go = new GameObject("ShotVisual");
        var lr = go.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);

        if (lineMaterial != null)
            lr.material = lineMaterial;

        lr.sortingOrder = 5;

        float width;
        float duration;
        int capVertices;

        if (visual == WeaponVisualType.Plasma)
        {
            width = plasmaWidth;
            duration = plasmaVisualDuration;
            capVertices = 4;
        }
        else
        {
            width = laserWidth;
            duration = laserVisualDuration;
            capVertices = 0;
        }

        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = capVertices;
        lr.startColor = color;
        lr.endColor = color;

        Destroy(go, duration);
    }
}
```

**Связь с фитингом:** `SetEquippedWeapons(List<FittingItem>)` — единственная точка, куда попадают установленные оружейные модули. `Shoot()` создаёт по одному выстрелу на каждый установленный модуль.
**Fallback-оружие:** в `PlayerShip.prefab` `useFallbackWeapon: 0` — если фитинг пуст, корабль стрелять не будет.

### 4.3 `ShipAbilities` — форсаж / щит+ / РЭБ

**Путь:** `Assets/Scripts/ShipAbilities.cs`

```csharp
using System.Collections;
using UnityEngine;

public class ShipAbilities : MonoBehaviour
{
    private ShipMovement shipMovement;
    private ShipHealth shipHealth;
    private ShipWeapon shipWeapon;

    // Таймеры перезарядки
    public float afterburnerCooldown { get; private set; }
    public float shieldBoostCooldown { get; private set; }
    public float emPulseCooldown { get; private set; }

    public bool isAfterburnerActive { get; private set; }

    private void Awake()
    {
        shipMovement = GetComponent<ShipMovement>();
        shipHealth = GetComponent<ShipHealth>();
        shipWeapon = GetComponent<ShipWeapon>();
    }

    private void Update()
    {
        if (afterburnerCooldown > 0f) afterburnerCooldown -= Time.deltaTime;
        if (shieldBoostCooldown > 0f) shieldBoostCooldown -= Time.deltaTime;
        if (emPulseCooldown > 0f) emPulseCooldown -= Time.deltaTime;
    }

    /// <summary> Форсаж (2 группа): +100% к скорости на 2 секунды, КД 10 сек </summary>
    public bool UseAfterburner()
    {
        if (afterburnerCooldown > 0f || isAfterburnerActive || shipMovement == null)
            return false;

        StartCoroutine(AfterburnerRoutine());
        afterburnerCooldown = 10f;
        return true;
    }

    private IEnumerator AfterburnerRoutine()
    {
        isAfterburnerActive = true;
        float originalSpeed = shipMovement.maxSpeed;
        float originalAccel = shipMovement.acceleration;

        shipMovement.maxSpeed *= 2f;
        shipMovement.acceleration *= 2f;

        Debug.Log($"[АКТИВАЦИЯ] {gameObject.name} включил Форсаж!");

        yield return new WaitForSeconds(2f);

        shipMovement.maxSpeed = originalSpeed;
        shipMovement.acceleration = originalAccel;
        isAfterburnerActive = false;
    }

    /// <summary> Перегрузка щита (2 группа): +30 HP щита, -10 HP корпуса, КД 8 сек </summary>
    public bool UseShieldBoost()
    {
        if (shieldBoostCooldown > 0f || shipHealth == null || shipHealth.Data == null)
            return false;

        var shieldMod = shipHealth.Data.GetModule(ModuleType.Shield);
        if (shieldMod != null && shieldMod.isDestroyed)
            return false;

        shipHealth.Data.currentShieldHP = Mathf.Min(shipHealth.Data.maxShieldHP, shipHealth.Data.currentShieldHP + 30f);
        shipHealth.Data.TakeHullDamage(10f);

        shieldBoostCooldown = 8f;
        Debug.Log($"[АКТИВАЦИЯ] {gameObject.name} перегрузил щиты! (+30 Щит, -10 Корпус)");
        return true;
    }

    /// <summary> Импульс помех (2 группа): Сбивает захват цели у врага на 3 сек, КД 12 сек </summary>
    public bool UseEMPulse()
    {
        if (emPulseCooldown > 0f || shipWeapon == null || shipWeapon.Target == null)
            return false;

        var enemyWeapon = shipWeapon.Target.GetComponent<ShipWeapon>();
        if (enemyWeapon != null)
        {
            enemyWeapon.ClearTarget();
            enemyWeapon.manualMoveOrder = true; // Сбиваем автопилот
            Debug.Log($"[АКТИВАЦИЯ] {gameObject.name} запустил Импульс Помех! Захват цели у {shipWeapon.Target.name} сбит!");
        }

        emPulseCooldown = 12f;
        return true;
    }
}
```

**Связь с кораблем:** способности не зависят от `ShipDefinition` — только от `ShipMovement`/`ShipHealth`/`ShipWeapon` на том же объекте. Т.е. на Sidewinder они будут работать без изменений, но их эффекты (x2 скорость, +30 щит) не масштабируются под ТТХ корабля.

### 4.4 `TargetSwitcher` — Tab-переключение цели и автозахват

**Путь:** `Assets/Scripts/TargetSwitcher.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Переключение цели:
/// - Tab — циклически по живым врагам.
/// - Автозахват ближайшего, если цели нет.
/// </summary>
public class TargetSwitcher : MonoBehaviour
{
    [Tooltip("Weapon игрока.")]
    public ShipWeapon playerWeapon;

    [Tooltip("Клавиша переключения.")]
    public KeyCode switchKey = KeyCode.Tab;

    private void Update()
    {
        if (playerWeapon == null) return;

#if ENABLE_INPUT_SYSTEM
        bool switchPressed = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
        bool switchPressed = Input.GetKeyDown(switchKey);
#endif

        var alive = EnemyRegistry.GetAliveEnemies();

        // === Автозахват: если цели нет (ни захваченной, ни в процессе) — берём ближайшую ===
        bool noTarget = playerWeapon.Target == null
                        && playerWeapon.PendingTarget == null;

        bool targetDestroyed = playerWeapon.Target != null
                               && (playerWeapon.Target.Data == null
                                   || playerWeapon.Target.Data.IsDestroyed);

        if ((noTarget || targetDestroyed) && alive.Count > 0)
        {
            playerWeapon.SetTarget(GetNearestEnemy(alive));
        }

        // === Tab — циклическое переключение ===
        if (switchPressed && alive.Count > 0)
        {
            playerWeapon.SetTarget(GetNextEnemy(alive));
        }
    }

    private ShipHealth GetNearestEnemy(List<ShipHealth> alive)
    {
        ShipHealth best = null;
        float bestDist = float.MaxValue;
        Vector3 myPos = playerWeapon.transform.position;

        foreach (var e in alive)
        {
            if (e == null) continue;
            float d = Vector3.Distance(myPos, e.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }

    private ShipHealth GetNextEnemy(List<ShipHealth> alive)
    {
        if (alive.Count == 0) return null;

        // Если сейчас идёт захват — от него и считаем
        var current = playerWeapon.PendingTarget != null
            ? playerWeapon.PendingTarget
            : playerWeapon.Target;

        if (current == null) return alive[0];

        int currentIndex = alive.IndexOf(current);
        if (currentIndex < 0)
            return alive[0];

        int nextIndex = (currentIndex + 1) % alive.Count;
        return alive[nextIndex];
    }
}
```

### 4.5 `PlayerInputHandler` — клики по врагу/полю

**Путь:** `Assets/Scripts/PlayerInputHandler.cs`

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Translates player input into ship orders: left click on an enemy captures it as a target
/// and enables automatic approach/orbiting, left click on empty space keeps the active target
/// but forces a manual movement order (allowing strafe runs and retreat while firing).
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private const string EnemyTag = "Enemy";

    [Tooltip("Movement component of the player ship.")]
    public ShipMovement playerMovement;

    [Tooltip("Weapon component of the player ship.")]
    public ShipWeapon playerWeapon;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    // LateUpdate — чтобы EventSystem уже обновил состояние.
    // Если обрабатывать клик в Update (или через InputAction callback) — IsPointerOverGameObject()
    // вернёт состояние прошлого кадра, и клик по UI «провалится» в игровое поле.
    private void LateUpdate()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // Клик по UI (UGUI) — игнорируем.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        HandleClick();
    }

    private void HandleClick()
    {
        if (playerMovement == null || playerWeapon == null)
        {
            Debug.LogWarning("[PlayerInputHandler] playerMovement / playerWeapon links are not assigned.");
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("[PlayerInputHandler] No Main Camera found (tag the camera as MainCamera).");
            return;
        }

        var mousePosition = Mouse.current.position.ReadValue();
        var worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;

        var hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag(EnemyTag))
        {
            var enemyHealth = hit.collider.GetComponentInParent<ShipHealth>();
            if (enemyHealth != null)
            {
                playerWeapon.SetTarget(enemyHealth);
                playerWeapon.manualMoveOrder = false;
                Debug.Log($"[PlayerInputHandler] Захват цели и авто-маневрирование: {enemyHealth.name}");
            }
        }
        else
        {
            playerWeapon.manualMoveOrder = true;
            playerMovement.SetTargetPosition(worldPosition);

            if (playerWeapon.Target != null)
            {
                Debug.Log($"[PlayerInputHandler] Ручной приказ в {worldPosition}. Ведётся прикрывающий огонь по: {playerWeapon.Target.name}");
            }
            else
            {
                Debug.Log($"[PlayerInputHandler] Приказ двигаться в {worldPosition} (цель не захвачена)");
            }
        }
    }
}
```

---


## 5. UI: окно фитинга, выбор корабля, HUD

### 5.1 `FittingWindow` — главное окно фитинга (и переключатель корабля)

**Путь:** `Assets/Scripts/UI/FittingWindow.cs`
**Префаб:** `Assets/Prefabs/UI/FittingWindow.prefab` (GUID `6523d71a8f52a1442a994e456c969292`)

Привязки в префабе:
```
shipLibrary:        ShipLibrary.asset (2315397736df8414aac7f72fe4828679)
slotPrefab:         Assets/Prefabs/UI/FittingSlot.prefab  (24182a2ef0c711f49b1498038c959df6)
itemViewPrefab:     Assets/Prefabs/UI/FittingItemView.prefab (8e48f4c3d8c406341a269bb51a4dc02f)
shipButtonPrefab:   Assets/Prefabs/UI/ShipButton.prefab (d989f9ae46f66a3459cd9f16574b5d41)
weaponSlotsContainer / shieldSlotsContainer / engineSlotsContainer
inventoryContainer / shipSelectorContainer
dragLayer:          {fileID: 0}   <- не назначен
targetShipHealth:   {fileID: 0}   <- не назначен (используется поиск по тегу "Player")
```

```csharp
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

        // === Модуль наведения: ускоряет захват цели ===
        loadout.inventory.Add(new FittingItem("t1", "Система Наведения", SlotGroup.Center_Shields,
            new Color(0.8f, 0.9f, 1f), "Захват: -0.2 сек",
            lockTime: -0.2f, mass: 0.1f));
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
        float totalLockTimeAdd = 0f;

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
                totalLockTimeAdd += s.Item.lockTimeBonus;
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
                totalLockTimeAdd += s.Item.lockTimeBonus;
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
                totalLockTimeAdd += s.Item.lockTimeBonus;
            }
        }

        if (targetShipWeapon != null)
            targetShipWeapon.SetEquippedWeapons(installedWeapons);

        float baseSpeed = shipDefinition != null ? shipDefinition.baseMaxSpeed : 5f;
        float baseAccel = shipDefinition != null ? shipDefinition.baseAcceleration : 12f;
        float baseShield = shipDefinition != null ? shipDefinition.baseShieldHP : 50f;
        float baseMass = shipDefinition != null ? shipDefinition.baseMass : 2f;
        float baseLockTime = shipDefinition != null ? shipDefinition.baseLockTime : 0.5f;

        if (targetShipMovement != null)
        {
            targetShipMovement.maxSpeed = baseSpeed + totalSpeedAdd;
            targetShipMovement.acceleration = baseAccel + totalAccelAdd;
        }

        if (targetShipHealth.Data != null)
        {
            float oldMaxShield = targetShipHealth.Data.maxShieldHP;
            float newMaxShield = baseShield + totalShieldHP;
            float delta = newMaxShield - oldMaxShield;

            targetShipHealth.Data.maxShieldHP = newMaxShield;
            targetShipHealth.Data.currentShieldHP = Mathf.Clamp(
                targetShipHealth.Data.currentShieldHP + delta,
                0f,
                newMaxShield);

            targetShipHealth.Data.mass = Mathf.Max(0.5f, baseMass + totalMassAdd);

            // Время захвата: база + бонусы, минимум 0.05 сек
            targetShipHealth.Data.lockTime = Mathf.Max(0.05f, baseLockTime + totalLockTimeAdd);

            if (targetShipMovement != null)
                targetShipMovement.ApplyMassFromData();
        }
    }

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
```

**Как это работает (важно для Sidewinder):**
- `Open()` -> `ResolveTargetShip()` (по тегу `Player`) -> `ResolveCurrentShipDefinition()` -> `BuildAll()` (один раз, дальше переиспользуется).
- Слоты строятся **по количеству из `ShipDefinition`** (`weaponSlots`/`shieldSlots`/`engineSlots`), т.е. Sidewinder с другим числом слотов отобразится корректно автоматически.
- `OnShipSelected(newShip)` — единственное место смены корабля:
  1. `SaveCurrentLoadout()` — сохраняет фитинг **старого** корабля по его `shipId`;
  2. `PlayerProfile.Instance.SetCurrentShip(newShip.shipId)` — пишет `player_current_ship_id` в PlayerPrefs;
  3. `targetShipHealth.shipDefinition = newShip` + сброс HP/щита/массы/lockTime/скоростей на базовые значения корабля;
  4. `BuildAll()` — перестроение UI под новый корабль.
- **Ключевой нюанс:** смена корабля в окне фитинга меняет **статы и фитинг**, но **не визуал корабля** (спрайт/префаб). Спрайт задаётся в `PlayerShip.prefab` (`m_Sprite` c GUID `6cb9aa7c9f1212a4b85139a11d93c1af`) и **не меняется при выборе Sidewinder** — потребуется отдельный механизм подмены спрайта/префаба.
- **Критично:** при смене корабля `currentHullHP`/`currentShieldHP` ставятся в максимум — корабль «бесплатно» чинится (в `readme.md` это отмечено как известный баг).

### 5.2 `FittingSlot` — один слот (перетаскивание модуля)

**Путь:** `Assets/Scripts/UI/FittingSlot.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-слот фитинга. Принимает drop от FittingItemView и от других FittingSlot.
/// Сам может быть источником drag (для тачскрина, где ПКМ недоступен).
/// </summary>
[RequireComponent(typeof(Image))]
public class FittingSlot : MonoBehaviour, IDropHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image background;
    public TextMeshProUGUI label;

    [Header("Colors")]
    public Color emptyColor = new Color(0.15f, 0.18f, 0.25f, 0.9f);
    public Color filledColor = new Color(0.2f, 0.22f, 0.3f, 1f);
    public Color validDropColor = new Color(0.2f, 0.5f, 0.2f, 1f);

    public SlotGroup group;
    public int index;
    public FittingWindow window;

    private FittingItem item;
    public FittingItem Item => item;

    private CanvasGroup canvasGroup;
    private GameObject dragVisual;
    private bool isDragging = false;
    private bool isHighlighted = false;
    private FittingSlot lastHighlightedSlot;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetItem(FittingItem newItem)
    {
        item = newItem;
        isHighlighted = false;   // сброс подсветки при любом изменении
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();

        if (item != null)
        {
            background.color = item.iconColor;
            if (label != null) label.text = item.name;
        }
        else
        {
            background.color = isHighlighted ? validDropColor : emptyColor;
            if (label != null) label.text = $"[Слот {index + 1}]";
        }
    }

    // ==================== DROP ====================

    public void OnDrop(PointerEventData eventData)
    {
        if (window == null) return;

        var itemView = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FittingItemView>()
            : null;

        if (itemView != null && itemView.item != null && itemView.item.allowedGroup == group)
        {
            window.HandleDropFromInventory(itemView, this);
            return;
        }

        var sourceSlot = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FittingSlot>()
            : null;

        if (sourceSlot != null && sourceSlot != this && sourceSlot.Item != null)
        {
            window.HandleDropFromSlot(sourceSlot, this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (window != null && item != null)
                window.HandleUnequip(this);
        }
    }

    // ==================== DRAG ====================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;
        if (window == null) return;

        if (window.itemViewPrefab != null && window.dragLayer != null)
        {
            var view = Instantiate(window.itemViewPrefab, window.dragLayer);
            view.window = window;
            view.SetItem(item);
            view.enabled = false;

            var cg = view.GetComponent<CanvasGroup>();
            if (cg == null) cg = view.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            dragVisual = view.gameObject;
            dragVisual.transform.position = eventData.position;
        }

        if (background != null) background.enabled = false;
        if (label != null) label.enabled = false;

        canvasGroup.blocksRaycasts = false;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
            dragVisual.transform.position = eventData.position;

        HighlightSlotUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        canvasGroup.blocksRaycasts = true;
        isDragging = false;

        if (background != null) background.enabled = true;
        if (label != null) label.enabled = true;

        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
        }

        if (lastHighlightedSlot != null)
        {
            lastHighlightedSlot.SetHighlight(false);
            lastHighlightedSlot = null;
        }
    }

    // ==================== HIGHLIGHT ====================

    public void SetHighlight(bool on)
    {
        isHighlighted = on;

        if (background == null) background = GetComponent<Image>();

        if (item == null)
        {
            background.color = on ? validDropColor : emptyColor;
        }
    }

    private void HighlightSlotUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        FittingSlot slotUnderPointer = null;
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponentInParent<FittingSlot>();
            if (slot != null)
            {
                slotUnderPointer = slot;
                break;
            }
        }

        if (lastHighlightedSlot != null && lastHighlightedSlot != slotUnderPointer)
        {
            lastHighlightedSlot.SetHighlight(false);
        }

        if (slotUnderPointer != null && slotUnderPointer != this
            && item != null && slotUnderPointer.group == item.allowedGroup)
        {
            slotUnderPointer.SetHighlight(true);
            lastHighlightedSlot = slotUnderPointer;
        }
        else
        {
            lastHighlightedSlot = null;
        }
    }
}
```

### 5.3 `FittingItemView` — визуал модуля (иконка, бонусы)

**Путь:** `Assets/Scripts/UI/FittingItemView.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-элемент инвентаря. Можно перетащить в FittingSlot.
/// При перетаскивании подсвечивает слоты нужной группы.
/// </summary>
[RequireComponent(typeof(Image))]
public class FittingItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image background;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI descriptionLabel;

    /// <summary>Данные модуля.</summary>
    public FittingItem item;

    /// <summary>Ссылка на окно фитинга.</summary>
    public FittingWindow window;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;

    // Текущий подсвеченный слот (чтобы снять подсветку при перемещении)
    private FittingSlot lastHighlightedSlot;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Устанавливает данные предмета и обновляет UI.
    /// </summary>
    public void SetItem(FittingItem newItem)
    {
        item = newItem;
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (item == null) return;

        if (background != null) background.color = item.iconColor;
        if (nameLabel != null) nameLabel.text = item.name;
        if (descriptionLabel != null) descriptionLabel.text = item.description;
    }

    // ==================== DRAG ====================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        originalParent = transform.parent;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        // Переносим в DragLayer, чтобы был поверх всего
        transform.SetParent(window != null ? window.dragLayer : originalParent, false);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false; // чтобы drop прошёл под элементом
        canvasGroup.alpha = 0.85f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (item == null) return;

        rectTransform.position = eventData.position;

        // Подсветка слота под курсором
        HighlightSlotUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (item == null) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Снимаем подсветку с последнего слота
        if (lastHighlightedSlot != null)
        {
            lastHighlightedSlot.SetHighlight(false);
            lastHighlightedSlot = null;
        }

        // Если drop не был обработан слотом — возвращаем на место
        if (transform.parent == (window != null ? window.dragLayer : null))
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Двойной клик — автоустановка в первый свободный слот нужной группы
        if (eventData.clickCount == 2 && window != null)
        {
            window.HandleAutoEquip(this);
        }
    }

    // ==================== HIGHLIGHT ====================

    /// <summary>
    /// Подсвечивает слот под курсором, если группа слота совпадает с группой модуля.
    /// </summary>
    private void HighlightSlotUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return;

        // Raycast по всем UI-элементам под курсором
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        FittingSlot slotUnderPointer = null;
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponentInParent<FittingSlot>();
            if (slot != null)
            {
                slotUnderPointer = slot;
                break;
            }
        }

        // Снимаем подсветку с прошлого
        if (lastHighlightedSlot != null && lastHighlightedSlot != slotUnderPointer)
        {
            lastHighlightedSlot.SetHighlight(false);
        }

        // Подсвечиваем новый, если группа совпадает
        if (slotUnderPointer != null && item != null && slotUnderPointer.group == item.allowedGroup)
        {
            slotUnderPointer.SetHighlight(true);
            lastHighlightedSlot = slotUnderPointer;
        }
        else
        {
            lastHighlightedSlot = null;
        }
    }
}
```

### 5.4 `FittingButton` — кнопка открытия окна фитинга

**Путь:** `Assets/Scripts/UI/FittingButton.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Кнопка "ФИТИНГ (I)". Открывает/закрывает FittingWindow по клику и по клавише I.
/// </summary>
public class FittingButton : MonoBehaviour
{
    [Header("References")]
    public FittingWindow fittingWindow;
    public Button button;

    [Header("Hotkey")]
    public bool useHotkey = true;
    public KeyCode toggleKey = KeyCode.I;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        if (!useHotkey) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            OnClick();
#else
        if (Input.GetKeyDown(toggleKey))
            OnClick();
#endif
    }

    private void OnClick()
    {
        if (fittingWindow == null) return;
        fittingWindow.Toggle();
    }
}
```

### 5.5 `InventoryDropZone` — приём перетаскиваемого модуля в инвентарь

**Путь:** `Assets/Scripts/UI/InventoryDropZone.cs`

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop-зона инвентаря. Принимает drop из FittingSlot — снимает предмет со слота
/// и возвращает его в инвентарь (создаёт FittingItemView в контейнере).
/// </summary>
public class InventoryDropZone : MonoBehaviour, IDropHandler
{
    [Tooltip("Ссылка на окно фитинга.")]
    public FittingWindow window;

    public void OnDrop(PointerEventData eventData)
    {
        if (window == null) return;

        // Источник — FittingSlot
        var sourceSlot = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FittingSlot>()
            : null;

        if (sourceSlot != null && sourceSlot.Item != null)
        {
            window.HandleUnequip(sourceSlot);
        }
    }
}
```

### 5.6 `CombatHUDManager` — HUD игрока и цели

**Путь:** `Assets/Scripts/UI/CombatHUDManager.cs`
**В сцене:** объект `Canvas`, `playerHealth: 1898430270`, `playerWeapon: 1898430268`, `defaultEnemyHealth: {fileID: 0}`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-панель боевого HUD: HP игрока слева, HP цели справа.
/// Полосы корпуса/щита, индикаторы модулей W/S/E.
/// </summary>
public class CombatHUDManager : MonoBehaviour
{
    [Header("Ship References")]
    public ShipHealth playerHealth;
    public ShipWeapon playerWeapon;

    [Header("Fallback Enemy")]
    [Tooltip("Используется, если цель не захвачена playerWeapon.Target == null.")]
    public ShipHealth defaultEnemyHealth;

    [Header("Player HUD — Labels")]
    public TextMeshProUGUI playerTitleLabel;
    public TextMeshProUGUI playerHullText;
    public TextMeshProUGUI playerShieldText;

    [Header("Player HUD — Bars")]
    public Image playerHullBarFill;
    public Image playerShieldBarFill;

    [Header("Player HUD — Modules")]
    public Image playerWeaponBoxImage;
    public Image playerShieldBoxImage;
    public Image playerEngineBoxImage;

    [Header("Target HUD — Labels")]
    public TextMeshProUGUI targetTitleLabel;
    public TextMeshProUGUI targetHullText;
    public TextMeshProUGUI targetShieldText;

    [Header("Target HUD — Bars")]
    public Image targetHullBarFill;
    public Image targetShieldBarFill;

    [Header("Target HUD — Modules")]
    public Image targetWeaponBoxImage;
    public Image targetShieldBoxImage;
    public Image targetEngineBoxImage;

    [Header("Colors")]
    public Color hullColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color shieldColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color moduleOkColor = new Color(0.15f, 0.75f, 0.15f, 1f);
    public Color moduleDamagedColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    public Color moduleDestroyedColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private void Update()
    {
        UpdatePlayerHUD();
        UpdateTargetHUD();
    }

    private void UpdatePlayerHUD()
    {
        if (playerHealth == null || playerHealth.Data == null) return;

        // Заголовок
        if (playerTitleLabel != null)
            playerTitleLabel.text = "ИГРОК";

        // Корпус
        float hullPct = Mathf.Clamp01(playerHealth.Data.currentHullHP / Mathf.Max(1f, playerHealth.Data.maxHullHP));
        if (playerHullBarFill != null)
        {
            playerHullBarFill.fillAmount = hullPct;
            playerHullBarFill.color = hullColor;
        }
        if (playerHullText != null)
            playerHullText.text = $"Корпус: {playerHealth.Data.currentHullHP:F0}/{playerHealth.Data.maxHullHP:F0}";

        // Щит
        float shieldPct = playerHealth.Data.maxShieldHP > 0f
            ? Mathf.Clamp01(playerHealth.Data.currentShieldHP / playerHealth.Data.maxShieldHP)
            : 0f;
        if (playerShieldBarFill != null)
        {
            playerShieldBarFill.fillAmount = shieldPct;
            playerShieldBarFill.color = shieldColor;
        }
        if (playerShieldText != null)
            playerShieldText.text = $"Щит: {playerHealth.Data.currentShieldHP:F0}/{playerHealth.Data.maxShieldHP:F0}";

        // Модули
        UpdateModuleBox(playerWeaponBoxImage, playerHealth.Data.GetModule(ModuleType.Weapon));
        UpdateModuleBox(playerShieldBoxImage, playerHealth.Data.GetModule(ModuleType.Shield));
        UpdateModuleBox(playerEngineBoxImage, playerHealth.Data.GetModule(ModuleType.Engine));
    }

    private void UpdateTargetHUD()
    {
        ShipHealth target = (playerWeapon != null && playerWeapon.Target != null)
            ? playerWeapon.Target
            : defaultEnemyHealth;

        if (target == null || target.Data == null)
            return;

        if (targetTitleLabel != null)
            targetTitleLabel.text = "ЦЕЛЬ";

        float hullPct = Mathf.Clamp01(target.Data.currentHullHP / Mathf.Max(1f, target.Data.maxHullHP));
        if (targetHullBarFill != null)
        {
            targetHullBarFill.fillAmount = hullPct;
            targetHullBarFill.color = hullColor;
        }
        if (targetHullText != null)
            targetHullText.text = $"Корпус: {target.Data.currentHullHP:F0}/{target.Data.maxHullHP:F0}";

        float shieldPct = target.Data.maxShieldHP > 0f
            ? Mathf.Clamp01(target.Data.currentShieldHP / target.Data.maxShieldHP)
            : 0f;
        if (targetShieldBarFill != null)
        {
            targetShieldBarFill.fillAmount = shieldPct;
            targetShieldBarFill.color = shieldColor;
        }
        if (targetShieldText != null)
            targetShieldText.text = $"Щит: {target.Data.currentShieldHP:F0}/{target.Data.maxShieldHP:F0}";

        UpdateModuleBox(targetWeaponBoxImage, target.Data.GetModule(ModuleType.Weapon));
        UpdateModuleBox(targetShieldBoxImage, target.Data.GetModule(ModuleType.Shield));
        UpdateModuleBox(targetEngineBoxImage, target.Data.GetModule(ModuleType.Engine));
    }

    private void UpdateModuleBox(Image boxImage, ShipModule module)
    {
        if (boxImage == null) return;

        if (module == null)
        {
            boxImage.color = moduleDestroyedColor;
            return;
        }

        if (module.isDestroyed)
        {
            boxImage.color = moduleDestroyedColor;
            return;
        }

        float pct = module.maxHP > 0f ? module.currentHP / module.maxHP : 0f;

        if (pct >= 0.99f)
            boxImage.color = moduleOkColor;
        else
            boxImage.color = moduleDamagedColor;
    }
}
```

### 5.7 `TacticalCommandPanel` — панель тактических команд (кнопки способностей)

**Путь:** `Assets/Scripts/UI/TacticalCommandPanel.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TacticalCommandPanel : MonoBehaviour
{
    [Header("Ship References")]
    public ShipWeapon playerWeapon;
    public ShipAbilities playerAbilities;

    [Header("Behavior Buttons")]
    public Button orbitButton;
    public Button keepDistanceButton;
    public Button flankButton;

    [Header("Target Priority Buttons")]
    public Button autoTargetButton;
    public Button weaponTargetButton;
    public Button shieldTargetButton;
    public Button engineTargetButton;

    [Header("Ability Buttons")]
    public Button afterburnerButton;
    public Button shieldBoostButton;
    public Button empButton;

    [Header("Ability Labels (TMP on each button)")]
    public TextMeshProUGUI afterburnerLabel;
    public TextMeshProUGUI shieldBoostLabel;
    public TextMeshProUGUI empLabel;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color activeColor = new Color(1f, 0.85f, 0.2f, 1f);

    private void Start()
    {
        if (playerWeapon == null)
        {
            Debug.LogWarning("[TacticalCommandPanel] playerWeapon не назначен.");
            return;
        }

        // Behavior
        orbitButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.Orbit));
        keepDistanceButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.KeepDistance));
        flankButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.Flank));

        // Target priority
        autoTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(null));
        weaponTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Weapon));
        shieldTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Shield));
        engineTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Engine));

        // Abilities
        afterburnerButton.onClick.AddListener(() => playerAbilities?.UseAfterburner());
        shieldBoostButton.onClick.AddListener(() => playerAbilities?.UseShieldBoost());
        empButton.onClick.AddListener(() => playerAbilities?.UseEMPulse());
    }

    private void Update()
    {
        if (playerWeapon == null) return;

        HighlightBehavior(orbitButton, CombatBehavior.Orbit);
        HighlightBehavior(keepDistanceButton, CombatBehavior.KeepDistance);
        HighlightBehavior(flankButton, CombatBehavior.Flank);

        HighlightTarget(autoTargetButton, null);
        HighlightTarget(weaponTargetButton, ModuleType.Weapon);
        HighlightTarget(shieldTargetButton, ModuleType.Shield);
        HighlightTarget(engineTargetButton, ModuleType.Engine);

        UpdateAbilityButton(afterburnerButton, afterburnerLabel, "Форсаж", playerAbilities ? playerAbilities.afterburnerCooldown : 0f);
        UpdateAbilityButton(shieldBoostButton, shieldBoostLabel, "Щит+", playerAbilities ? playerAbilities.shieldBoostCooldown : 0f);
        UpdateAbilityButton(empButton, empLabel, "РЭБ", playerAbilities ? playerAbilities.emPulseCooldown : 0f);
    }

    private void HighlightBehavior(Button btn, CombatBehavior behavior)
    {
        if (btn == null) return;
        bool isActive = playerWeapon.behavior == behavior;
        SetButtonColor(btn, isActive ? activeColor : normalColor);
    }

    private void HighlightTarget(Button btn, ModuleType? module)
    {
        if (btn == null) return;
        bool isActive = playerWeapon.targetedModule == module;
        SetButtonColor(btn, isActive ? activeColor : normalColor);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var cb = btn.colors;
        cb.normalColor = color;
        cb.selectedColor = color;
        cb.highlightedColor = color * 1.1f;
        btn.colors = cb;
    }

    private void UpdateAbilityButton(Button btn, TextMeshProUGUI label, string baseLabel, float cooldown)
    {
        if (btn == null) return;

        bool ready = cooldown <= 0f;
        btn.interactable = ready;

        if (label != null)
            label.text = ready ? baseLabel : $"{baseLabel}\n({Mathf.CeilToInt(cooldown)}s)";
    }
}
```

**Замечание:** код панели не зависит от типа корабля — на Sidewinder работает без изменений. Но на панели нет отображения названия корабля, т.е. игрок не видит, какой корабль выбран (кроме как в селекторе фитинга).

---


## 6. Спавн врагов и волны

### 6.1 `ShipSpawner` — волны врагов

**Путь:** `Assets/Scripts/ShipSpawner.cs`
**В сцене:** объект `Spawner` (отдельный от `[Managers]`), `enemyPrefab` -> `Assets/Prefabs/EnemyShip.prefab`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спавнит врагов волнами. Волна 1 — 1 враг, волна 2 — 2 врага, волна 3 — 3 врага.
/// Спавнит за пределами экрана, враги сами прилетают (EnemyAI ведёт их к игроку).
/// </summary>
public class ShipSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Префаб врага (EnemyShip).")]
    public GameObject enemyPrefab;

    [Header("Waves")]
    [Tooltip("Количество врагов в каждой волне. Порядок: 1, 2, 3.")]
    public int[] waveSizes = new int[] { 1, 2, 3 };

    [Tooltip("Пауза между волнами (сек).")]
    public float pauseBetweenWaves = 3f;

    [Header("Spawn Position")]
    [Tooltip("Радиус спавна от игрока. Должен быть больше половины видимой области.")]
    public float spawnRadius = 14f;

    [Tooltip("Игрок (если null — ищется по тегу 'Player').")]
    public Transform playerTransform;

    [Header("References")]
    public CombatManager combatManager;

    private int currentWaveIndex = 0;
    private readonly List<ShipHealth> spawnedEnemies = new List<ShipHealth>();

    private void Start()
    {
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[ShipSpawner] enemyPrefab не назначен.");
            return;
        }

        StartCoroutine(SpawnWavesRoutine());
    }

    private IEnumerator SpawnWavesRoutine()
    {
        for (currentWaveIndex = 0; currentWaveIndex < waveSizes.Length; currentWaveIndex++)
        {
            int count = waveSizes[currentWaveIndex];
            Debug.Log($"[ShipSpawner] Волна {currentWaveIndex + 1} — старт, врагов {count}.");
            SpawnWave(count);
            Debug.Log($"[ShipSpawner] Волна {currentWaveIndex + 1} — заспавнена. Ждём уничтожения.");

            yield return new WaitUntil(() => AreAllWaveEnemiesDead());
            Debug.Log($"[ShipSpawner] Волна {currentWaveIndex + 1} уничтожена.");

            if (currentWaveIndex < waveSizes.Length - 1)
            {
                Debug.Log($"[ShipSpawner] Пауза {pauseBetweenWaves} сек перед волной {currentWaveIndex + 2}.");
                yield return new WaitForSeconds(pauseBetweenWaves);
            }
        }

        Debug.Log("[ShipSpawner] Все волны заспавнены и уничтожены.");

        if (combatManager != null)
            combatManager.NotifyAllWavesCleared();
    }

    private void SpawnWave(int count)
    {
        spawnedEnemies.Clear();

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetSpawnPosition(i, count);
            GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            go.name = $"EnemyShip_W{currentWaveIndex + 1}_{i + 1}";

            var health = go.GetComponent<ShipHealth>();
            if (health != null)
            {
                spawnedEnemies.Add(health);
                EnemyRegistry.Register(health);
                health.OnDestroyed += () => EnemyRegistry.Unregister(health);
            }
            else
            {
                Debug.LogError($"[ShipSpawner] У {go.name} нет ShipHealth!");
            }
        }
    }

    private Vector3 GetSpawnPosition(int index, int total)
    {
        Vector3 center = playerTransform != null ? playerTransform.position : Vector3.zero;
        float baseAngle = Random.Range(0f, Mathf.PI * 2f);
        float angleStep = (Mathf.PI * 2f) / Mathf.Max(1, total);
        float angle = baseAngle + angleStep * index;

        return center + new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            Mathf.Sin(angle) * spawnRadius,
            0f);
    }

    private bool AreAllWaveEnemiesDead()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            var e = spawnedEnemies[i];

            if (e == null) continue;
            if (e.Data == null) continue;
            if (e.Data.IsDestroyed) continue;
            if (!e.gameObject.activeInHierarchy) continue;

            return false;
        }

        return true;
    }

    public List<ShipHealth> GetAliveWaveEnemies()
    {
        var result = new List<ShipHealth>();
        foreach (var e in spawnedEnemies)
        {
            if (e == null) continue;
            if (e.Data == null) continue;
            if (e.Data.IsDestroyed) continue;
            if (!e.gameObject.activeInHierarchy) continue;
            result.Add(e);
        }
        return result;
    }
}
```

**Замечание:** спавнер работает только с `enemyPrefab` — единственным типом врага. Тип корабля игрока на волны не влияет, поэтому Sidewinder не потребует правок здесь.

### 6.2 Итоговая карта привязок в сцене `CombatSandbox.unity`

| Объект в сцене | Компонент | Ключевые привязки |
|---|---|---|
| `[Managers]` | `ShipRegistry` | `shipLibrary` -> `ShipLibrary.asset` |
| `[Managers]` | `PlayerProfile` | `currentShipId: cobra_mk3`, `credits: 1000`, `reputation: 0` |
| `[Managers]` | `LoadoutApplier` | `targetShipHealth: 1898430270` (корабль игрока) |
| `[Managers]` | `ShipFitting` | legacy-скрипт (не используется) |
| `Canvas` | `CombatHUDManager` | `playerHealth: 1898430270`, `playerWeapon: 1898430268` |
| `Canvas` | `TargetSwitcher` | `playerWeapon: 1898430268`, `switchKey: 9` (Tab) |
| `Canvas` | `TacticalCommandPanel` | кнопки способностей/поведения |
| `Canvas` | `FittingButton` | открывает окно фитинга |
| `Spawner` | `ShipSpawner` | `enemyPrefab` -> `EnemyShip.prefab` |
| `PlayerShip` (инстанс) | `ShipHealth` | `shipDefinition: {fileID: 0}` — **пусто** |
| `PlayerShip` (инстанс) | `ShipMovement` | `spriteNoseIsUp: 1` (в сцене должно быть `false`) |
| `PlayerShip` (инстанс) | `ShipWeapon` | `useFallbackWeapon: 0` |

**Главный вывод по привязкам:** ни `ShipHealth.shipDefinition` в префабе, ни `FittingWindow.targetShipHealth` не назначены. Все сценарии «какой корабль у игрока» реально решаются через `PlayerProfile.currentShipId` + `ShipRegistry`. Значит, для Sidewinder достаточно добавить ассет в `ShipLibrary` — пересборка префабов не обязательна (кроме визуала).

---


## 7. Поддержка второго корабля (Sidewinder): что нужно и что уже готово

### 7.1 Что уже поддерживается «из коробки»

- `ShipLibrary.ships` — список `ShipDefinition`; селектор в `FittingWindow` строится динамически (`BuildShipSelector`), т.е. **любое число кораблей** отображается без правок кода.
- `ShipRegistry` — реестр по `shipId`, умеет несколько кораблей, ловит дубликаты `shipId`.
- `ShipLoadoutManager` — фитинги хранятся **по `shipId`**, т.е. у Sidewinder будет свой независимый фитинг в том же `ship_loadouts.json`.
- `PlayerProfile.currentShipId` + PlayerPrefs `player_current_ship_id` — выбранный корабль переживает перезапуск.
- `LoadoutApplier` — применяет фитинг текущего корабля при старте сцены.
- `FittingWindow.OnShipSelected` — полноценная смена корабля: сохранение старого фитинга, запись `shipId`, пересчёт статов, перестроение слотов под новое количество.

### 7.2 Что нужно сделать (пошагово)

1. **Создать ассет** `Assets/Ships/Sidewinder.asset` (Create -> Space -> Ship Definition):
   ```
   shipId:             sidewinder
   displayName:        Sidewinder
   icon:               (желательно спрайт)
   maxHullHP:          80
   baseShieldHP:       40
   baseMass:           1.5
   baseMaxSpeed:       8
   baseAcceleration:   18
   baseRotationSpeed:  420
   baseLockTime:       0.35
   weaponSlots:        2
   shieldSlots:        2
   engineSlots:        3
   ```
   (Значения ТТХ взяты из `Assets/Scripts/readme.md`, раздел «Что осталось / на будущее».)

2. **Добавить в `ShipLibrary.asset`** второй элемент в список `ships`:
   ```yaml
   ships:
   - {fileID: 11400000, guid: c346ed5929ae76343a04e9f5c080ea0c, type: 2}   # CobraMK3
   - {fileID: 11400000, guid: <guid Sidewinder.asset>, type: 2}            # Sidewinder
   ```
   После этого `ShipRegistry` подхватит его автоматически (`BuildRegistry` в `Awake`).

3. **Убедиться, что `shipId` уникален** — иначе `ShipRegistry` выдаст warning и пропустит дубликат.

4. **Визуал корабля (отдельная задача).** Текущая схема «один `PlayerShip.prefab` + смена статов» не умеет менять спрайт. Варианты:
   - добавить в `ShipDefinition` поле `public Sprite hullSprite;` и подменять `SpriteRenderer.sprite` в `OnShipSelected`/`LoadoutApplier`;
   - либо сделать отдельные префабы `PlayerShip_Sidewinder.prefab` и спавнить/подменять объект целиком;
   - либо (минимально) оставить общий спрайт и различать корабли только иконкой в UI и ТТХ.

5. **Проверить `spriteNoseIsUp`.** В `PlayerShip.prefab` стоит `1`, а по `readme.md` должно быть `0` (нос вправо) — иначе корабль развернётся на 180°.

6. **Опционально:** `useFallbackWeapon` у игрового корабля = `0`, поэтому на Sidewinder без фитинга стрелять будет нечем. Если хотите «ручное» оружие — включить fallback.

### 7.3 Известные ограничения/риски

| Проблема | Где | Влияние на Sidewinder |
|---|---|---|
| Смена корабля полностью чинит HP/щит | `FittingWindow.OnShipSelected`, `LoadoutApplier.ApplyBaseStats` | Можно «танковать» переключением корабля |
| `ShipHealth.shipDefinition` в префабе пуст | `Assets/Prefabs/PlayerShip.prefab` | Определение корабля всегда идёт из `PlayerProfile`; при ручной установке `shipDefinition` в инспекторе приоритет изменится |
| `FittingWindow.dragLayer` = `{fileID: 0}` | `FittingWindow.prefab` | Drag&drop работает «на глаз» (родитель — само окно) |
| `FittingWindow.targetShipHealth` = `{fileID: 0}` | `FittingWindow.prefab` | Поиск по тегу `Player` — работает, но лучше назначить явно |
| `ShipHealth.Data.modules` всегда 3 модуля | `ShipHealth` | Число слотов `ShipDefinition` (2/2/3) влияет только на UI фитинга, не на число модулей |
| Способности не зависят от `ShipDefinition` | `ShipAbilities` | Эффекты (+30 щит, x2 скорость) одинаковы для всех кораблей |
| `LoadoutApplier` и `FittingWindow.ApplyLoadoutToShip` дублируют логику | `readme.md` | Щит накапливается корректно (через дельту), но лучше следить |
| `ShipSpawner` имеет один `enemyPrefab` | `ShipSpawner` | На Sidewinder не влияет |

### 7.4 Итоговый чек-лист «добавить Sidewinder»

- [ ] Создать `Assets/Ships/Sidewinder.asset` с `shipId: sidewinder` и ТТХ (80/40/1.5/8/18/420, слоты 2/2/3).
- [ ] Добавить Sidewinder в `ShipLibrary.asset` (список `ships`).
- [ ] Открыть сцену, нажать Play, дождаться лога `[ShipRegistry] Зарегистрировано кораблей: 2`.
- [ ] Открыть окно фитинга (клавиша `I` или кнопка `FittingButton`) — в селекторе кораблей должен появиться Sidewinder.
- [ ] Выбрать Sidewinder: проверить, что слоты перестроились (2/2/3), а `PlayerProfile.currentShipId` = `sidewinder`.
- [ ] Перезапустить сцену — убедиться, что корабль остался Sidewinder (PlayerPrefs `player_current_ship_id`).
- [ ] Проверить файл `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\ship_loadouts.json`: должны появиться записи для `cobra_mk3` и `sidewinder`.
- [ ] (Опционально) Проверить смену визуала корабля.
- [ ] Убрать/исправить читерское восстановление HP при смене корабля.

---


## Приложение A. Сводка по ключам и путям

**PlayerPrefs:**
- `player_current_ship_id` (string) — `Assets/Scripts/PlayerProfile.cs`, метод `SetCurrentShip`.

**Файл сохранения:**
- `Application.persistentDataPath` + `/ship_loadouts.json` — `Assets/Scripts/Ships/ShipLoadoutManager.cs`.

**Ассеты кораблей:**
- `Assets/Ships/CobraMK3.asset` — GUID `c346ed5929ae76343a04e9f5c080ea0c`, `shipId: cobra_mk3`.
- `Assets/Ships/ShipLibrary.asset` — GUID `2315397736df8414aac7f72fe4828679`.
- Sidewinder-ассета в проекте **нет** — его нужно создать.

**Префабы:**
- `Assets/Prefabs/PlayerShip.prefab` — GUID `86c6c15693c9af74e8d6f223692aaf19`.
- `Assets/Prefabs/UI/FittingWindow.prefab` — GUID `6523d71a8f52a1442a994e456c969292`.
- `Assets/Prefabs/UI/ShipButton.prefab` — GUID `d989f9ae46f66a3459cd9f16574b5d41`.
- `Assets/Prefabs/UI/FittingSlot.prefab` — GUID `24182a2ef0c711f49b1498038c959df6`.
- `Assets/Prefabs/UI/FittingItemView.prefab` — GUID `8e48f4c3d8c406341a269bb51a4dc02f`.

**Основные скрипты:**
| Класс | Путь |
|---|---|
| `ShipDefinition` | `Assets/Scripts/Ships/ShipDefinition.cs` |
| `ShipLibrary` | `Assets/Scripts/Ships/ShipLibrary.cs` |
| `ShipRegistry` | `Assets/Scripts/ShipRegistry.cs` |
| `PlayerProfile` | `Assets/Scripts/PlayerProfile.cs` |
| `ShipLoadout` | `Assets/Scripts/Ships/ShipLoadout.cs` |
| `ShipLoadoutManager` | `Assets/Scripts/Ships/ShipLoadoutManager.cs` |
| `LoadoutApplier` | `Assets/Scripts/Ships/LoadoutApplier.cs` |
| `SlotGroup` | `Assets/Scripts/Ships/SlotGroup.cs` |
| `ShipHealth` | `Assets/Scripts/Ships/ShipHealth.cs` |
| `ShipData` | `Assets/Scripts/Ships/ShipData.cs` |
| `ShipModule` | `Assets/Scripts/ShipModule.cs` |
| `ShipMovement` | `Assets/Scripts/ShipMovement.cs` |
| `ShipWeapon` | `Assets/Scripts/ShipWeapon.cs` |
| `ShipAbilities` | `Assets/Scripts/ShipAbilities.cs` |
| `FittingItem` | `Assets/Scripts/FittingItem.cs` |
| `ModuleType` | `Assets/Scripts/ModuleType.cs` |
| `FittingWindow` | `Assets/Scripts/UI/FittingWindow.cs` |
| `FittingSlot` | `Assets/Scripts/UI/FittingSlot.cs` |
| `FittingItemView` | `Assets/Scripts/UI/FittingItemView.cs` |
| `FittingButton` | `Assets/Scripts/UI/FittingButton.cs` |
| `InventoryDropZone` | `Assets/Scripts/UI/InventoryDropZone.cs` |
| `CombatHUDManager` | `Assets/Scripts/UI/CombatHUDManager.cs` |
| `TacticalCommandPanel` | `Assets/Scripts/UI/TacticalCommandPanel.cs` |
| `TargetSwitcher` | `Assets/Scripts/TargetSwitcher.cs` |
| `PlayerInputHandler` | `Assets/Scripts/PlayerInputHandler.cs` |
| `ShipSpawner` | `Assets/Scripts/ShipSpawner.cs` |

---

*Отчёт сформирован автоматически из исходников проекта; фрагменты кода вставлены дословно из перечисленных файлов.*

