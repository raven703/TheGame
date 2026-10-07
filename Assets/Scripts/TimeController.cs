using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Global time control: pause toggle plus 1x / 2x game speed presets.
/// </summary>
/// <remarks>
/// Uses <see cref="Time.timeScale"/>; the Input System keeps delivering events while
/// timeScale is 0, so the game can always be resumed.
/// </remarks>
public class TimeController : MonoBehaviour
{
    private CombatControls controls;
    private bool isPaused = false;

    private void Awake()
    {
        controls = new CombatControls();
        controls.Combat.Pause.performed += _ => TogglePause();
        controls.Combat.Speed1.performed += _ => SetSpeed(1f);
        controls.Combat.Speed2.performed += _ => SetSpeed(2f);
    }

    private void OnEnable()
    {
        controls.Combat.Enable();
    }

    private void OnDisable()
    {
        controls.Combat.Disable();
    }

    private void OnDestroy()
    {
        controls?.Dispose();
    }

    /// <summary>Pauses the game, or resumes it when it was already paused.</summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log(isPaused ? "ПАУЗА" : "ИГРА ПРОДОЛЖЕНА");
    }

    /// <summary>Sets the game speed multiplier and leaves the paused state.</summary>
    public void SetSpeed(float speed)
    {
        isPaused = false;
        Time.timeScale = speed;
        Debug.Log($"Скорость игры: {speed}x");
    }
}
