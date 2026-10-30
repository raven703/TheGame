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
    public readonly Game.Core.TacticalPause Plan=new Game.Core.TacticalPause();
    public static TimeController Instance { get; private set; }
    public static bool IsPaused => Instance!=null && Instance.Plan.Paused;
    public static bool TextInputFocused
    {
        get {
            var selected=UnityEngine.EventSystems.EventSystem.current!=null?UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject:null;
            return selected!=null && (selected.GetComponent<TMPro.TMP_InputField>()!=null || selected.GetComponent<UnityEngine.UI.InputField>()!=null);
        }
    }

    private void Awake()
    {
        if(Instance!=null && Instance!=this){enabled=false;return;}
        Instance=this;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded+=OnSceneLoaded;
        controls = new CombatControls();
        controls.Combat.Pause.performed += _ => {if(!TextInputFocused)TogglePause();};
        controls.Combat.Speed1.performed += _ => {if(!TextInputFocused)SetSpeed(1f);};
        controls.Combat.Speed2.performed += _ => {if(!TextInputFocused)SetSpeed(2f);};
    }

    private void OnEnable()
    {
        if (controls != null) controls.Combat.Enable();
    }

    private void OnDisable()
    {
        if (controls != null) controls.Combat.Disable();
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded-=OnSceneLoaded;
        if(Instance==this){if(Plan.Paused)Time.timeScale=Plan.ResumeScale;Instance=null;}
        controls?.Dispose();
    }
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode) => Plan.Clear();

    /// <summary>Pauses the game, or resumes it when it was already paused.</summary>
    public void TogglePause()
    {
        Time.timeScale=Plan.Toggle(Time.timeScale);
        foreach(var input in FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None))input.EndGesture();
        if(!Plan.Paused)Dispatch();
    }

    /// <summary>Sets the game speed multiplier and leaves the paused state.</summary>
    public void SetSpeed(float speed)
    {
        Plan.SetRunningScale(speed);
        Time.timeScale = speed;
        Dispatch();
        Debug.Log($"Скорость игры: {speed}x");
    }
    public void RequestAbility(Game.Core.AbilityCommandKind kind)
    {
        var player=GameObject.FindGameObjectWithTag("Player");
        if(player==null)return;
        var weapon=player.GetComponent<ShipWeapon>();
        var target=weapon!=null?(weapon.PendingTarget!=null?weapon.PendingTarget:weapon.Target):null;
        if(Plan.Paused)Plan.ToggleAbility(kind,kind==Game.Core.AbilityCommandKind.Emp?ShipOrderController.RuntimeTargetId(target):null);
        else
        {
            var abilities=player.GetComponent<ShipAbilities>();
            if(abilities==null)return;
            if(kind==Game.Core.AbilityCommandKind.Emp)abilities.UseEMPulse();
            else if(kind==Game.Core.AbilityCommandKind.Afterburner)abilities.UseAfterburner();
            else abilities.UseShieldBoost();
        }
    }
    private void Dispatch()
    {
        var player=GameObject.FindGameObjectWithTag("Player");
        var commands=Plan.Drain();
        if(player==null)return;
        foreach(var command in commands)Execute(player,command);
    }
    private void Execute(GameObject player,Game.Core.AbilityCommand command)
    {
        var abilities=player.GetComponent<ShipAbilities>();
        ShipHealth target=null;
        if(command.Kind==Game.Core.AbilityCommandKind.Emp)
            foreach(var enemy in EnemyRegistry.GetAliveEnemies())if(ShipOrderController.RuntimeTargetId(enemy)==command.TargetId){target=enemy;break;}
        bool success=abilities!=null && (command.Kind==Game.Core.AbilityCommandKind.Afterburner?abilities.UseAfterburner():
            command.Kind==Game.Core.AbilityCommandKind.ShieldBoost?abilities.UseShieldBoost():abilities.UseEMPulseOn(target));
        if(!success)CombatSession.Log("Отклонено: "+command.Kind+" — цель, модуль, ресурс или перезарядка");
    }
    private void Update()
    {
        var canvas=FindFirstObjectByType<CombatInterfaceLayout>();
        if(canvas!=null && canvas.GetComponent<TacticalShieldPanel>()==null)canvas.gameObject.AddComponent<TacticalShieldPanel>();
        var player=GameObject.FindGameObjectWithTag("Player");
        if(player==null || !ShipOrderController.Available(player.GetComponent<ShipHealth>()))Plan.Clear();
    }
}
