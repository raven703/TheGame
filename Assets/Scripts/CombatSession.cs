using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-local preparation phase and bounded combat history.</summary>
public class CombatSession : MonoBehaviour
{
    public static CombatSession Instance { get; private set; }
    public static bool IsPreparing => Instance != null && !Instance.Started;
    private bool standaloneStarted;
    public bool Started
    {
        get => GameState.Instance != null ? GameState.Instance.isCombatStarted : standaloneStarted;
        private set => standaloneStarted = value;
    }
    public AIPersonality EnemyPersonality { get; private set; }
    private readonly Queue<string> entries = new Queue<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindFirstObjectByType<CombatManager>() != null && Instance == null)
            new GameObject("Combat Session").AddComponent<CombatSession>();
    }

    private void Awake()
    {
        Instance = this; Time.timeScale = 1f;
        gameObject.AddComponent<CombatStatusPanel>();
        if (FindFirstObjectByType<AsteroidField>() == null) gameObject.AddComponent<AsteroidField>();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public static void Log(string message)
    {
        if (Instance == null) return;
        Instance.entries.Enqueue($"{Time.timeSinceLevelLoad:F1}: {message}");
        while (Instance.entries.Count > 7) Instance.entries.Dequeue();
    }

    public static Rect Panel => new Rect(15, Screen.height - 265, 350, 175);
    private void OnGUI()
    {
        GUILayout.BeginArea(Panel, GUI.skin.box);
        if (!Started)
        {
            GUILayout.Label("Подготовка: установите оборудование (I)");
            if (GUILayout.Button($"Противники: {EnemyPersonality} (сменить)"))
                EnemyPersonality = (AIPersonality)(((int)EnemyPersonality + 1) % 4);
            // Existing UGUI button starts the spawner. Only show a fallback without it.
            if (FindFirstObjectByType<PreCombatPanel>() == null && GUILayout.Button("Начать бой"))
            {
                var spawner = FindFirstObjectByType<ShipSpawner>();
                if (spawner != null) spawner.StartCombat();
                else if (GameState.Instance != null) GameState.Instance.StartCombat();
                else Started = true;
                CombatLogStart();
            }
        }
        else GUILayout.Label("Журнал боя");
        foreach (string entry in entries) GUILayout.Label(entry);
        GUILayout.EndArea();
    }

    private void CombatLogStart() { Log("Бой начат. Оснащение заблокировано."); }
}
