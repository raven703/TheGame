using System.Collections.Generic;
using UnityEngine;

public enum ShipOrderMode { HoldPosition, MoveToPoint, EngageTarget, Orbit, KeepDistance, Flank }

/// <summary>Single authoritative route/tactical order. Evaluated by ShipMovement's physics step.</summary>
[RequireComponent(typeof(ShipMovement))]
public class ShipOrderController : MonoBehaviour
{
    public ShipOrderMode Mode { get; private set; }
    public ShipHealth OrderTarget { get; private set; }
    public float SpeedLimit { get; private set; } = 1f;
    public readonly List<Vector2> Waypoints = new List<Vector2>();
    public string Error { get; private set; }
    public Vector2 Destination { get; private set; }
    private Vector2 hold;
    private bool initialized;
    public bool IsBraking { get; private set; }
    public float PreferredDistance { get; set; }
    private readonly ShipLocalNavigator navigator = new ShipLocalNavigator();
    public bool Detouring { get; private set; }
    public int AvoidanceRetries => navigator.Retries;
    private float Radius => GetComponent<ShipMovement>().ShipRadius;
    public Vector2 FacingDirection(Vector2 position, Vector2 fallback)
    {
        return Available(OrderTarget) ? (Vector2)OrderTarget.transform.position - position : fallback;
    }
    public void SetSpeedLimit(float value) { SpeedLimit = Mathf.Clamp(value,.25f,1f); }
    public void MoveTo(Vector2 point, bool append = false)
    {
        if(!append || Mode!=ShipOrderMode.MoveToPoint)Waypoints.Clear();
        OrderTarget=null;Error=null;Mode=ShipOrderMode.MoveToPoint;IsBraking=false;
        Vector2 actual = ShipLocalNavigator.AccessiblePoint(point, transform.position, Radius);
        if ((actual-point).sqrMagnitude>.01f) CombatSession.Log("Точка внутри препятствия — назначена доступная точка рядом");
        Waypoints.Add(actual);Destination=Waypoints[0];initialized=true;
        navigator.Reset();
    }
    public void Tactical(ShipOrderMode mode, ShipHealth target)
    {
        if(!Available(target))return;
        Waypoints.Clear();OrderTarget=target;Mode=mode;Error=null;initialized=true;IsBraking=false;navigator.Reset();
    }
    public void Stop()
    {
        Waypoints.Clear();OrderTarget=null;Mode=ShipOrderMode.HoldPosition;
        hold=transform.position;Destination=hold;initialized=true;IsBraking=true;Error=null;Detouring=false;navigator.Reset();
    }
    public static bool Available(ShipHealth target) => target!=null && target.isActiveAndEnabled && target.Data!=null && !target.Data.IsDestroyed;
    public Vector2 Evaluate(Vector2 position, Vector2 velocity, out bool final)
    {
        if(!initialized){hold=position;initialized=true;}
        final=true;
        if(Mode==ShipOrderMode.MoveToPoint)
        {
            if(Waypoints.Count==0){Stop();return hold;}
            if(Waypoints.Count>1 && Vector2.Distance(position,Waypoints[0])<.65f){Waypoints.RemoveAt(0);navigator.Reset();}
            Waypoints[0]=ShipLocalNavigator.AccessiblePoint(Waypoints[0],position,Radius);
            Destination=Waypoints[0];final=Waypoints.Count==1;
            if(final && Vector2.Distance(position,Destination)<.18f && velocity.magnitude<.12f){Stop();return hold;}
            return Navigate(position, velocity, Destination, ref final);
        }
        if(Mode==ShipOrderMode.HoldPosition)
        {
            if(IsBraking)
            {
                hold=position;Destination=hold;
                if(velocity.magnitude<.08f)IsBraking=false;
            }
            return hold;
        }
        if(!Available(OrderTarget)){Stop();CombatSession.Log("Цель приказа недоступна — торможение");return hold;}
        var weapon=GetComponent<ShipWeapon>();
        float distance=PreferredDistance>0f?PreferredDistance:weapon!=null?weapon.range*.7f:4f;
        Vector2 target=OrderTarget.transform.position;
        Vector2 away=(position-target).normalized;if(away.sqrMagnitude<.01f)away=Vector2.up;
        if(Mode==ShipOrderMode.Flank)
        {
            var movement=OrderTarget.GetComponent<ShipMovement>();
            Destination=target-(movement!=null?movement.GetForwardVector():(Vector2)OrderTarget.transform.up)*distance;
        }
        else if(Mode==ShipOrderMode.Orbit)
        {
            Destination=target+(Vector2)(Quaternion.Euler(0,0,20)*away)*distance;
        }
        else Destination=Mathf.Abs(Vector2.Distance(position,target)-distance)<.35f?position:target+away*distance;
        // Tactical destination moves with the target: only stagnation, not total leg duration, applies.
        return Navigate(position, velocity, Destination, ref final);
    }
    private Vector2 Navigate(Vector2 position, Vector2 velocity, Vector2 destination, ref bool final)
    {
        var movement=GetComponent<ShipMovement>();
        if(!movement.EngineAvailable)return destination;
        bool detour;
        Vector2 steering=navigator.Steer(position,destination,velocity,Radius,movement.AvailableForce/Mathf.Max(.1f,movement.Rb.mass),Time.fixedDeltaTime,Mode==ShipOrderMode.MoveToPoint,out detour);
        Detouring=detour;
        if(detour)final=true;
        if(navigator.Failed)
        {
            // Preserve failed and remaining waypoints for a visible error; never silently skip a leg.
            OrderTarget=null;Mode=ShipOrderMode.HoldPosition;hold=position;IsBraking=true;Detouring=false;
            Error="Нет прогресса после 3 попыток локального обхода";
            CombatSession.Log("Маршрут недоступен: "+Error+". Выполнено торможение.");
            return position;
        }
        return steering;
    }
    public string Status
    {
        get {
            if(!string.IsNullOrEmpty(Error))return "Маршрут недоступен";
            switch(Mode){case ShipOrderMode.MoveToPoint:return "Движение";case ShipOrderMode.EngageTarget:return "Вступить в бой";
            case ShipOrderMode.Orbit:return "Орбита";case ShipOrderMode.KeepDistance:return "Дистанция";case ShipOrderMode.Flank:return "Заход с кормы";default:
            var m=GetComponent<ShipMovement>();return m!=null&&m.Rb!=null&&m.Rb.linearVelocity.magnitude>.12f?"Торможение":"Удержание позиции";}
        }
    }
}