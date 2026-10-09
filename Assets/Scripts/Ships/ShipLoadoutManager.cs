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