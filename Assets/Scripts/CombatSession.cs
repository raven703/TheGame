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
    private TMPro.TextMeshProUGUI journal;
    private UnityEngine.UI.Button personalityButton;
    private void Update()
    {
        if (journal == null)
        {
            var canvas = FindFirstObjectByType<CombatInterfaceLayout>();
            if (canvas == null) return;
            var panel = CombatUIStyle.Panel("CombatJournal", canvas.transform, CombatUIStyle.Surface);
            CombatUIStyle.Rect(panel.transform, Vector2.zero,Vector2.zero,new Vector2(16,228),new Vector2(366,366));
            journal = CombatUIStyle.Text("Journal",panel.transform,"",14);
            CombatUIStyle.Box(journal.transform,12,10,326,86);
            personalityButton = CombatUIStyle.Button("Personality",panel.transform,"",()=>EnemyPersonality=(AIPersonality)(((int)EnemyPersonality+1)%4));
            CombatUIStyle.Box(personalityButton.transform,12,102,326,28);
        }
        journal.text = Started ? string.Join("\n", entries) : "ПОДГОТОВКА\nОснастите корабль перед началом боя.";
        personalityButton.gameObject.SetActive(!Started);
        personalityButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Противники: " + EnemyPersonality;
    }

    private void CombatLogStart() { Log("Бой начат. Оснащение заблокировано."); }
}
