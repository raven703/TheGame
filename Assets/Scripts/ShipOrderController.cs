using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;

public enum HullOrientation { Course, OrderTarget }
public enum ShipOrderMode { HoldPosition, MoveToPoint, EngageTarget, Orbit, KeepDistance, Flank, FollowCursor }

/// <summary>Unity observation/physics adapter. ShipOrders is the only order-state owner.</summary>
[RequireComponent(typeof(ShipMovement))]
public class ShipOrderController : MonoBehaviour
{
    private readonly ShipOrders model = new ShipOrders();
    // IDs live for one ShipHealth instance, not one Unity instance-ID value or ship definition.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ShipHealth, TargetIdentity> identities = new System.Runtime.CompilerServices.ConditionalWeakTable<ShipHealth, TargetIdentity>();
    private sealed class TargetIdentity { public readonly string Id = Guid.NewGuid().ToString("N"); }
    private readonly Dictionary<string, WeakReference<ShipHealth>> targets = new Dictionary<string, WeakReference<ShipHealth>>();
    private readonly ShipLocalNavigator navigator = new ShipLocalNavigator();
    private int navigationRevision;
    public HullOrientation Orientation => (HullOrientation)model.Orientation;
    public ShipOrderMode Mode => (ShipOrderMode)model.Mode;
    public ShipHealth OrderTarget
    {
        get { ShipHealth target; return model.TargetId != null && targets.TryGetValue(model.TargetId, out var reference) && reference.TryGetTarget(out target) && target != null ? target : null; }
    }
    public float SpeedLimit => model.SpeedLimit;
    public IReadOnlyList<Vector2> Waypoints
    {
        get { var source=model.Waypoints;var result=new Vector2[source.Count];for(int i=0;i<source.Count;i++)result[i]=V(source[i]);return Array.AsReadOnly(result); }
    }
    public string Error => model.Error;
    public Vector2 Destination => V(model.Destination);
    public bool IsBraking => model.IsBraking;
    public float PreferredDistance { get => model.PreferredDistance; set => model.PreferredDistance=value; }
    public bool Detouring { get; private set; }
    public int AvoidanceRetries => navigator.Retries;
    private float Radius => GetComponent<ShipMovement>().ShipRadius;
    private static AimPoint P(Vector2 p) => new AimPoint(p.x,p.y);
    private static Vector2 V(AimPoint p) => new Vector2(p.X,p.Y);
    private void SynchronizeNavigator()
    {
        if(navigationRevision==model.NavigationRevision)return;
        navigationRevision=model.NavigationRevision;navigator.Reset();
    }
    private string Register(ShipHealth target)
    {
        if(!Available(target))return null;
        string id=identities.GetValue(target,_=>new TargetIdentity()).Id;
        targets[id]=new WeakReference<ShipHealth>(target);return id;
    }
    public static string RuntimeTargetId(ShipHealth target)=>Available(target)?identities.GetValue(target,_=>new TargetIdentity()).Id:null;
    public bool SetOrientation(HullOrientation value) => model.SetOrientation((OrderOrientation)value,Available(OrderTarget));
    public string OrientationTargetId=>model.OrientationTargetId;
    private ShipHealth ResolveTarget(string id)
    {
        ShipHealth target;return id!=null&&targets.TryGetValue(id,out var reference)&&reference.TryGetTarget(out target)&&target!=null?target:null;
    }
    public bool FaceSelectedTarget(ShipHealth target)=>Available(target)&&model.SetOrientationTarget(Register(target));
    public void ValidateFacingTarget()=>model.ValidateOrientationTarget(Available(ResolveTarget(model.OrientationTargetId)));
    public Vector2 FacingDirection(Vector2 position, Vector2 fallback)
    {
        ValidateFacingTarget();
        var target=ResolveTarget(model.OrientationTargetId);
        if(model.Orientation==OrderOrientation.OrderTarget&&Available(target))return (Vector2)target.transform.position-position;
        return V(model.Facing(Observe(position,Vector2.zero),P(fallback)));
    }
    public void SetSpeedLimit(float value) => model.SetSpeedLimit(value);
    public void MoveTo(Vector2 point, bool append = false)
    {
        Vector2 actual=ShipLocalNavigator.AccessiblePoint(point,transform.position,Radius);
        if((actual-point).sqrMagnitude>.01f)CombatSession.Log("Точка внутри препятствия — назначена доступная точка рядом");
        model.MoveTo(P(actual),append);targets.Clear();SynchronizeNavigator();
    }
    public void BeginFollowCursor(Vector2 point)
    {
        model.BeginFollowCursor(P(ShipLocalNavigator.AccessiblePoint(point,transform.position,Radius)));
        targets.Clear();SynchronizeNavigator();
    }
    public void UpdateFollowCursor(Vector2 point) => model.UpdateFollowCursor(P(ShipLocalNavigator.AccessiblePoint(point,transform.position,Radius)));
    public void EndFollowCursor() => model.EndFollowCursor();
    public void Tactical(ShipOrderMode mode, ShipHealth target)
    {
        if(!Available(target))return;
        model.Tactical((OrderMode)mode,Register(target));SynchronizeNavigator();
    }
    public void ApplyDecision(PilotDecision decision, ShipHealth target)
    {
        decision.Apply(model,Available(target)?Register(target):null,P(transform.position));
        SynchronizeNavigator();
    }
    public void Stop()
    {
        model.Stop(P(transform.position));targets.Clear();Detouring=false;SynchronizeNavigator();
    }
    public static bool Available(ShipHealth target) => target!=null && target.isActiveAndEnabled && target.Data!=null && !target.Data.IsDestroyed;
    private OrderObservation Observe(Vector2 position, Vector2 velocity)
    {
        var target=OrderTarget;var weapon=GetComponent<ShipWeapon>();
        var movement=target!=null?target.GetComponent<ShipMovement>():null;
        return new OrderObservation { Position=P(position),Velocity=P(velocity),TargetAvailable=Available(target),
            TargetPosition=target!=null?P(target.transform.position):default(AimPoint),
            TargetForward=target!=null?P(movement!=null?movement.GetForwardVector():(Vector2)target.transform.up):new AimPoint(0,1),
            WeaponRange=weapon!=null?weapon.range:4f/.7f };
    }
    public Vector2 Evaluate(Vector2 position, Vector2 velocity, out bool final)
    {
        var previousMode=Mode;
        if(Mode==ShipOrderMode.MoveToPoint || Mode==ShipOrderMode.FollowCursor)
        {
            model.AdvanceRouteLeg(P(position));
            var points=model.Waypoints;
            if(points.Count>0)model.CorrectFirstWaypoint(P(ShipLocalNavigator.AccessiblePoint(V(points[0]),position,Radius)));
        }
        Vector2 destination=V(model.Evaluate(Observe(position,velocity),out final,false));
        SynchronizeNavigator();
        if(Mode==ShipOrderMode.HoldPosition)
        {
            if(previousMode!=ShipOrderMode.HoldPosition){Detouring=false;if(previousMode!=ShipOrderMode.MoveToPoint)CombatSession.Log("Цель приказа недоступна — торможение");}
            return destination;
        }
        var movement=GetComponent<ShipMovement>();
        if(!movement.EngineAvailable)return destination;
        bool detour;
        Vector2 steering=navigator.Steer(position,destination,velocity,Radius,movement.ForceAlong(-velocity)/Mathf.Max(.1f,movement.Rb.mass),Time.fixedDeltaTime,Mode==ShipOrderMode.MoveToPoint,out detour);
        Detouring=detour;if(detour)final=true;
        if(GetComponent<EnemyAI>()!=null)
        {
            var nearby=Physics2D.OverlapCircleAll(position,Radius+velocity.magnitude*1.2f+3f);
            foreach(var collider in nearby)
            {
                var other=collider.GetComponentInParent<ShipHealth>();
                if(other==null||other.gameObject==gameObject||other.Data==null||other.Data.Damage.Destroyed)continue;
                var body=other.GetComponent<Rigidbody2D>();
                if(body==null)continue;
                var adjusted=CombatRolePilot.AvoidShip(P(position),P(velocity),P(body.position),P(body.linearVelocity),Radius,
                    collider.bounds.extents.magnitude,P(steering));
                var point=V(adjusted);
                if((point-steering).sqrMagnitude>.001f){steering=point;final=false;break;}
            }
        }
        if(navigator.Failed)
        {
            model.NavigationFailed(P(position));Detouring=false;
            CombatSession.Log("Маршрут недоступен: "+Error+". Выполнено торможение.");return position;
        }
        return steering;
    }
    public string Status
    {
        get {
            if(!string.IsNullOrEmpty(Error))return "Маршрут недоступен";
            switch(Mode){case ShipOrderMode.FollowCursor:return "За курсором";case ShipOrderMode.MoveToPoint:return "Движение";case ShipOrderMode.EngageTarget:return "Вступить в бой";
            case ShipOrderMode.Orbit:return "Орбита";case ShipOrderMode.KeepDistance:return "Дистанция";case ShipOrderMode.Flank:return "Заход с кормы";default:
            var m=GetComponent<ShipMovement>();return m!=null&&m.Rb!=null&&m.Rb.linearVelocity.magnitude>.12f?"Торможение":"Удержание позиции";}
        }
    }
}
