using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor-инструмент: одной кнопкой заполняет defaultInventory у ShipDefinition.
/// Меню: Tools → Ship → Generate Default Loadouts.
/// </summary>
public static class DefaultLoadoutGenerator
{
    [MenuItem("Tools/Ship/Generate Default Loadouts")]
    public static void GenerateDefaultLoadouts()
    {
        // 1. Загружаем все ShipDefinition в проекте
        var guids = AssetDatabase.FindAssets("t:ShipDefinition");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Ошибка", "В проекте нет ассетов ShipDefinition.", "OK");
            return;
        }

        var allDefs = new List<ShipDefinition>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<ShipDefinition>(path);
            if (def != null) allDefs.Add(def);
        }

        int updated = 0;
        foreach (var def in allDefs)
        {
            var inventory = BuildInventoryFor(def.shipId);
            if (inventory == null) continue;

            def.defaultInventory = inventory;
            EditorUtility.SetDirty(def);
            updated++;
            Debug.Log($"[DefaultLoadoutGenerator] {def.shipId}: {inventory.Count} модулей.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Готово",
            $"Обработано кораблей: {updated}.\n\n" +
            "- cobra_mk3 → универсальный набор\n" +
            "- sidewinder → лёгкий, скоростной\n\n" +
            "Для других shipId — только модули для оружия (базовый набор).",
            "OK");
    }

    private static List<FittingItem> BuildInventoryFor(string shipId)
    {
        switch (shipId)
        {
            case "cobra_mk3": return BuildCobraLoadout();
            case "sidewinder": return BuildSidewinderLoadout();
            default: return BuildFallbackLoadout();
        }
    }

    // ========== COBRA MK3: тяжёлый универсал ==========
    private static List<FittingItem> BuildCobraLoadout()
    {
        return new List<FittingItem>
        {
            // Оружие — 4 варианта на выбор
            new FittingItem("w1", "Лазер T1", SlotGroup.Top_Weapons,
                new Color(1f, 0.3f, 0.3f), "Оружие: Урон +15",
                damage: 15f, visual: WeaponVisualType.Laser),
            new FittingItem("w2", "Плазма T2", SlotGroup.Top_Weapons,
                new Color(1f, 0.1f, 0.5f), "Оружие: Урон +30",
                damage: 30f, visual: WeaponVisualType.Plasma),
            new FittingItem("w3", "Рейлган T1", SlotGroup.Top_Weapons,
                new Color(1f, 0.5f, 0.2f), "Оружие: Урон +20, быстрый",
                damage: 20f, cooldown: -0.1f, visual: WeaponVisualType.Laser),
            new FittingItem("w4", "Импульсный лазер", SlotGroup.Top_Weapons,
                new Color(0.9f, 0.9f, 0.3f), "Оружие: Урон +12, очень быстрый",
                damage: 12f, cooldown: -0.2f, visual: WeaponVisualType.Laser),

            // Щиты — 3 варианта
            new FittingItem("s1", "Легкий Щит", SlotGroup.Center_Shields,
                new Color(0.2f, 0.6f, 1f), "Защита: Щит +30 HP",
                shield: 30f, mass: 0.2f),
            new FittingItem("s2", "Тяжелый Щит", SlotGroup.Center_Shields,
                new Color(0.3f, 0.9f, 0.4f), "Защита: Щит +80 HP, тяжёлый",
                shield: 80f, mass: 1.0f),
            new FittingItem("s3", "Система Наведения", SlotGroup.Center_Shields,
                new Color(0.8f, 0.9f, 1f), "Захват цели: -0.2 сек",
                lockTime: -0.2f, mass: 0.1f),

            // Двигатели — 3 варианта
            new FittingItem("e1", "Форсаж M1", SlotGroup.Bottom_Engines,
                new Color(0.9f, 0.8f, 0.2f), "Двиг: Скорость +3, Ускорение +6",
                speed: 3f, accel: 6f),
            new FittingItem("e2", "Тяжелый Двиг", SlotGroup.Bottom_Engines,
                new Color(0.8f, 0.4f, 1f), "Двиг: Ускорение +10",
                speed: 1f, accel: 10f, mass: 0.5f),
            new FittingItem("e3", "Маневровые", SlotGroup.Bottom_Engines,
                new Color(0.4f, 0.9f, 0.6f), "Двиг: Скорость +2, лёгкий",
                speed: 2f, accel: 4f, mass: -0.1f),
        };
    }

    // ========== SIDEWINDER: лёгкий перехватчик ==========
    private static List<FittingItem> BuildSidewinderLoadout()
    {
        return new List<FittingItem>
        {
            // Оружие — только лёгкое (2 слота)
            new FittingItem("w1", "Лазер T1", SlotGroup.Top_Weapons,
                new Color(1f, 0.3f, 0.3f), "Оружие: Урон +15",
                damage: 15f, visual: WeaponVisualType.Laser),
            new FittingItem("w4", "Импульсный лазер", SlotGroup.Top_Weapons,
                new Color(0.9f, 0.9f, 0.3f), "Оружие: Урон +12, очень быстрый",
                damage: 12f, cooldown: -0.2f, visual: WeaponVisualType.Laser),
            new FittingItem("w5", "Скорострельный лазер", SlotGroup.Top_Weapons,
                new Color(1f, 0.6f, 0.4f), "Оружие: Урон +8, очень быстрый",
                damage: 8f, cooldown: -0.3f, visual: WeaponVisualType.Laser),

            // Щиты — только лёгкие (2 слота)
            new FittingItem("s1", "Легкий Щит", SlotGroup.Center_Shields,
                new Color(0.2f, 0.6f, 1f), "Защита: Щит +30 HP",
                shield: 30f, mass: 0.2f),
            new FittingItem("s4", "Компактный Щит", SlotGroup.Center_Shields,
                new Color(0.5f, 0.8f, 1f), "Защита: Щит +20 HP, лёгкий",
                shield: 20f, mass: 0.05f),
            new FittingItem("s3", "Система Наведения", SlotGroup.Center_Shields,
                new Color(0.8f, 0.9f, 1f), "Захват цели: -0.2 сек",
                lockTime: -0.2f, mass: 0.1f),

            // Двигатели — акцент на скорость (3 слота)
            new FittingItem("e1", "Форсаж M1", SlotGroup.Bottom_Engines,
                new Color(0.9f, 0.8f, 0.2f), "Двиг: Скорость +3, Ускорение +6",
                speed: 3f, accel: 6f),
            new FittingItem("e3", "Маневровые", SlotGroup.Bottom_Engines,
                new Color(0.4f, 0.9f, 0.6f), "Двиг: Скорость +2, лёгкий",
                speed: 2f, accel: 4f, mass: -0.1f),
            new FittingItem("e4", "Скоростной двигатель", SlotGroup.Bottom_Engines,
                new Color(1f, 0.7f, 0.3f), "Двиг: Скорость +5, лёгкий",
                speed: 5f, accel: 3f, mass: -0.15f),
        };
    }

    // ========== Fallback для любых других кораблей ==========
    private static List<FittingItem> BuildFallbackLoadout()
    {
        return new List<FittingItem>
        {
            new FittingItem("w1", "Лазер T1", SlotGroup.Top_Weapons,
                new Color(1f, 0.3f, 0.3f), "Оружие: Урон +15",
                damage: 15f, visual: WeaponVisualType.Laser),
            new FittingItem("s1", "Легкий Щит", SlotGroup.Center_Shields,
                new Color(0.2f, 0.6f, 1f), "Защита: Щит +30 HP",
                shield: 30f, mass: 0.2f),
            new FittingItem("e1", "Форсаж M1", SlotGroup.Bottom_Engines,
                new Color(0.9f, 0.8f, 0.2f), "Двиг: Скорость +3, Ускорение +6",
                speed: 3f, accel: 6f),
        };
    }
}