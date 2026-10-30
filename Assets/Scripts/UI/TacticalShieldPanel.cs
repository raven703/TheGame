using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Nonmodal UGUI projection of existing pause, order and damage models.</summary>
public class TacticalShieldPanel : MonoBehaviour
{
    private TextMeshProUGUI status,plan;
    private GameObject pauseBar;
    private UnityEngine.UI.Image movementIcon;
    private readonly UnityEngine.UI.Button[] plannedAbilities=new UnityEngine.UI.Button[3];
    private readonly TextMeshProUGUI[] activationNumbers=new TextMeshProUGUI[3];
    private void Awake()
    {
        var pause=CombatUIStyle.Button("PauseToggle",transform,"Пауза [Space]",()=>TimeController.Instance?.TogglePause());
        CombatUIStyle.Rect(pause.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-80,-42),new Vector2(80,-12));
        var strip=CombatUIStyle.Panel("TacticalPauseBar",transform,CombatUIStyle.Surface);pauseBar=strip.gameObject;
        CombatUIStyle.Rect(strip.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-260,-128),new Vector2(260,-48));
        plan=CombatUIStyle.Text("Plan",strip.transform,"",14);
        CombatUIStyle.Box(plan.transform,44,6,326,38);
        movementIcon=CombatUIStyle.Panel("MovementIcon",strip.transform,Color.white);movementIcon.raycastTarget=false;
        movementIcon.preserveAspect=true;CombatUIStyle.Box(movementIcon.transform,12,10,26,26);
        string[] icons={"boost","shieldboost","emp"};
        for(int i=0;i<3;i++)
        {
            int index=i;
            var button=CombatUIStyle.Button("PlannedAbility"+i,strip.transform,"",()=>TimeController.Instance?.RequestAbility((Game.Core.AbilityCommandKind)index));
            plannedAbilities[i]=button;CombatUIStyle.Box(button.transform,44+i*108,46,100,28);
            var icon=CombatUIStyle.Panel("Icon",button.transform,Color.white);icon.raycastTarget=false;
            icon.sprite=Resources.Load<Sprite>("TacticalIcons/"+icons[i]);icon.preserveAspect=true;CombatUIStyle.Box(icon.transform,4,2,24,24);
            activationNumbers[i]=button.GetComponentInChildren<TextMeshProUGUI>();
        }
        var resume=CombatUIStyle.Button("Resume",strip.transform,"Продолжить",()=>TimeController.Instance?.TogglePause());
        CombatUIStyle.Box(resume.transform,380,22,128,30);
        var panel=CombatUIStyle.Panel("ShieldDistribution",transform,CombatUIStyle.Surface);
        CombatUIStyle.Rect(panel.transform,Vector2.one,Vector2.one,new Vector2(-570,-714),new Vector2(-298,-652));
        status=CombatUIStyle.Text("ShieldStatus",panel.transform,"",12);CombatUIStyle.Box(status.transform,8,4,256,22);
        var forward=CombatUIStyle.Button("ShieldForward",panel.transform,"Вперёд [W]",()=>Shift(1));
        var even=CombatUIStyle.Button("ShieldEven",panel.transform,"Поровну",()=>Shield()?.SetDesired(.5f));
        var rear=CombatUIStyle.Button("ShieldRear",panel.transform,"Назад [S]",()=>Shift(-1));
        CombatUIStyle.Box(forward.transform,8,28,86,28);CombatUIStyle.Box(even.transform,98,28,70,28);CombatUIStyle.Box(rear.transform,172,28,92,28);
        foreach(var button in new[]{forward,even,rear})
        {
            var label=button.GetComponentInChildren<TextMeshProUGUI>();label.fontSize=12;label.textWrappingMode=TextWrappingModes.NoWrap;
        }
    }
    private static Game.Core.DirectionalShield Shield()
    {
        var player=GameObject.FindGameObjectWithTag("Player");
        var health=player!=null?player.GetComponent<ShipHealth>():null;
        return health!=null&&health.Data!=null?health.Data.Damage.Shields:null;
    }
    private static void Shift(int step) => Shield()?.StepDesired(step);
    private void LateUpdate()
    {
        if(!TimeController.TextInputFocused && Keyboard.current!=null && FittingWindow.OpenWindow==null)
        {
            if(Keyboard.current.wKey.wasPressedThisFrame)Shift(1);
            if(Keyboard.current.sKey.wasPressedThisFrame)Shift(-1);
        }
        var s=Shield();
        if(s!=null)status.text=!TimeController.IsPaused&&s.Transferring?"Перенос":"Распределение щита";
        pauseBar.SetActive(TimeController.IsPaused);
        if(!TimeController.IsPaused)return;
        var player=GameObject.FindGameObjectWithTag("Player");var orders=player!=null?player.GetComponent<ShipOrderController>():null;
        plan.text="ТАКТИЧЕСКАЯ ПАУЗА\n"+(orders!=null?$"{orders.Status}; точек {orders.Waypoints.Count}":"Нет корабля");
        string iconName=orders==null?"engine":orders.Mode==ShipOrderMode.Orbit?"orbit":orders.Mode==ShipOrderMode.KeepDistance?"distance":orders.Mode==ShipOrderMode.Flank?"flank":orders.Mode==ShipOrderMode.EngageTarget?"weapon":"engine";
        movementIcon.sprite=Resources.Load<Sprite>("TacticalIcons/"+iconName);
        var commands=FindFirstObjectByType<TacticalCommandPanel>();
        var sources=commands!=null?new[]{commands.afterburnerButton,commands.shieldBoostButton,commands.empButton}:null;
        for(int i=0;i<3;i++)
        {
            int number=0;var queue=TimeController.Instance.Plan.Commands;
            for(int j=0;j<queue.Count;j++)if((int)queue[j].Kind==i)number=j+1;
            activationNumbers[i].text=number>0?"#"+number:"—";
            var tip=CombatUIStyle.Ensure<TacticalTooltip>(plannedAbilities[i].gameObject);tip.panel=commands;
            var source=sources!=null&&sources[i]!=null?sources[i].GetComponent<TacticalTooltip>():null;
            tip.description=source!=null?source.description:"Нет корабля";
            plannedAbilities[i].interactable=sources!=null&&sources[i]!=null&&sources[i].interactable;
        }
    }
}