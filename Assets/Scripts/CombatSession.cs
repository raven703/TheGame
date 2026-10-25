using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-local preparation phase and bounded combat history.</summary>
public class CombatSession : MonoBehaviour
{
    public static CombatSession Instance { get; private set; }
    public static bool IsPreparing => Instance != null && !Instance.Started;
    public bool Started { get; private set; }
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
            bool overFitting = false;
            Vector2 mouse = Event.current.mousePosition;
            foreach (var fitting in FindObjectsByType<ShipFitting>(FindObjectsSortMode.None))
                if (fitting.ContainsPointer(mouse)) overFitting = true;
            GUI.enabled = !overFitting;
            foreach (var ai in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                if (GUILayout.Button($"{ai.name}: {ai.personality} (сменить)"))
                    ai.personality = (AIPersonality)(((int)ai.personality + 1) % 4);
            }
            if (GUILayout.Button("Начать бой"))
            {
                foreach (var fitting in FindObjectsByType<ShipFitting>(FindObjectsSortMode.None))
                    fitting.CancelDrag();
                Started = true;
                CombatLogStart();
            }
            GUI.enabled = true;
        }
        else GUILayout.Label("Журнал боя");
        foreach (string entry in entries) GUILayout.Label(entry);
        GUILayout.EndArea();
    }

    private void CombatLogStart() { Log("Бой начат. Оснащение заблокировано."); }
}
