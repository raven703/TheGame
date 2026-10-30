using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Nonmodal UGUI projection of existing pause, order and damage models.</summary>
public class TacticalShieldPanel : MonoBehaviour
{
    private TextMeshProUGUI status,plan;
    private GameObject pauseBar;
    private void Awake()
    {
        var pause=CombatUIStyle.Button("PauseToggle",transform,"Пауза [Space]",()=>TimeController.Instance?.TogglePause());
        CombatUIStyle.Rect(pause.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-80,-42),new Vector2(80,-12));
        var strip=CombatUIStyle.Panel("TacticalPauseBar",transform,CombatUIStyle.Surface);pauseBar=strip.gameObject;
        CombatUIStyle.Rect(strip.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-260,-128),new Vector2(260,-48));
        plan=CombatUIStyle.Text("Plan",strip.transform,"",14);
        CombatUIStyle.Box(plan.transform,12,6,360,68);
        var resume=CombatUIStyle.Button("Resume",strip.transform,"Продолжить",()=>TimeController.Instance?.TogglePause());
        CombatUIStyle.Box(resume.transform,380,22,128,30);
        var panel=CombatUIStyle.Panel("ShieldDistribution",transform,CombatUIStyle.Surface);
        CombatUIStyle.Rect(panel.transform,Vector2.one,Vector2.one,new Vector2(-570,-714),new Vector2(-298,-592));
        status=CombatUIStyle.Text("ShieldStatus",panel.transform,"",12);CombatUIStyle.Box(status.transform,8,4,256,74);
        var forward=CombatUIStyle.Button("ShieldForward",panel.transform,"Вперёд [W]",()=>Shift(1));
        var even=CombatUIStyle.Button("ShieldEven",panel.transform,"Поровну",()=>Shield()?.SetDesired(.5f));
        var rear=CombatUIStyle.Button("ShieldRear",panel.transform,"Назад [S]",()=>Shift(-1));
        CombatUIStyle.Box(forward.transform,8,84,86,28);CombatUIStyle.Box(even.transform,98,84,70,28);CombatUIStyle.Box(rear.transform,172,84,92,28);
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
    private void Update()
    {
        if(!TimeController.TextInputFocused && Keyboard.current!=null && FittingWindow.OpenWindow==null)
        {
            if(Keyboard.current.wKey.wasPressedThisFrame)Shift(1);
            if(Keyboard.current.sKey.wasPressedThisFrame)Shift(-1);
        }
        var s=Shield();
        if(s!=null)status.text=$"Перед {s.Front:F0} / {s.FrontCapacity:F0}   Корма {s.Rear:F0} / {s.RearCapacity:F0}\nЁмкость: Перед {s.Fraction:P0} / Корма {1-s.Fraction:P0}\n"+
            (s.Transferring?$"Перенос → {s.Desired:P0} / {1-s.Desired:P0}":$"Реген +{s.RegenPerSecond:F1}/с")+
            $"\nЗадержка П {s.FrontDelay:F1} / К {s.RearDelay:F1} с";
        pauseBar.SetActive(TimeController.IsPaused);
        if(!TimeController.IsPaused)return;
        var player=GameObject.FindGameObjectWithTag("Player");var orders=player!=null?player.GetComponent<ShipOrderController>():null;
        plan.text="ТАКТИЧЕСКАЯ ПАУЗА\n"+(orders!=null?$"{orders.Status}; точек {orders.Waypoints.Count}; {orders.SpeedLimit:P0}":"Нет корабля")+
            $"\nСпособностей в очереди: {TimeController.Instance.Plan.Commands.Count}";
    }
}