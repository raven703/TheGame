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

    /// <summary>Возвращает loadout для корабля или null, если сохранённого нет.</summary>
    public static ShipLoadout GetLoadout(string shipId)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(shipId))
            return null;

        return cache.TryGetValue(shipId, out var loadout) ? loadout : null;
    }

    /// <summary>Сохраняет loadout в кэш и на диск.</summary>
    public static void SaveLoadout(ShipLoadout loadout)
    {
        if (loadout == null || string.IsNullOrEmpty(loadout.shipId))
            return;

        EnsureLoaded();
        cache[loadout.shipId] = loadout;
        SaveToDisk();
    }

    /// <summary>Удаляет сохранённый loadout для корабля.</summary>
    public static void DeleteLoadout(string shipId)
    {
        EnsureLoaded();
        if (cache.Remove(shipId))
            SaveToDisk();
    }

    /// <summary>Полная очистка сохранений (для отладки).</summary>
    public static void ClearAll()
    {
        cache.Clear();
        isLoaded = true;
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }

    /// <summary>Путь к файлу сохранения — на случай, если хочешь посмотреть вручную.</summary>
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
                        cache[loadout.shipId] = loadout;
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
}