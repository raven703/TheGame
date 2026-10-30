using System;

namespace Game.Core
{
    // Separate from legacy serialized personalities: their numeric meaning never changes.
    public enum CombatRole { Interceptor=0, Artillery=1, Escort=2 }
    public enum CombatManeuver { Approach, Attack, Exit, Recover, Orbit, Reposition, Disabled }
    public struct CombatRoleObservation
    {
        public AimPoint Position, Forward, Velocity, TargetPosition, TargetVelocity, TargetForward;
        public float Radius, TargetRadius;
        public bool TargetAvailable, EngineAvailable, WeaponAvailable, LineBlocked;
        public float Range, EnergyFraction, HeatFraction, HullFraction, ShieldFraction;
        public float Hull, Energy, MaxEnergy;
        public bool BoostReady, ShieldReady, EmpReady, TargetSuppressed;
    }
    public struct CombatRoleDecision
    {
        public CombatManeuver Maneuver;
        public PilotDecision Order;
        public AimPoint Waypoint;
        public bool UseWaypoint, AllowFire, Boost, Shield, Emp, NewCommand;
        public OrderOrientation Orientation;
    }
    /// <summary>Persistent role decisions from public observations; owns no physical navigation or resources.</summary>
    public sealed class CombatRolePilot
    {
        // Bounded steering only: never changes an order or weapon target. Both ships
        // choose their own right, producing opposite world-space passing offsets.
        public static AimPoint AvoidShip(AimPoint position,AimPoint velocity,AimPoint other,AimPoint otherVelocity,float radius,float otherRadius,AimPoint destination)
        {
            var delta=ShipOrders.Subtract(other,position);
            var relative=ShipOrders.Subtract(otherVelocity,velocity);
            float speed2=relative.X*relative.X+relative.Y*relative.Y;
            float t=speed2>.001f?Math.Max(0,Math.Min(1.2f,-(delta.X*relative.X+delta.Y*relative.Y)/speed2)):0;
            var closest=new AimPoint(delta.X+relative.X*t,delta.Y+relative.Y*t);
            float safe=(radius+otherRadius)*1.35f;
            if(ShipOrders.Length(closest)>=safe||ShipOrders.Length(delta)>safe+ShipOrders.Length(relative)*1.2f)return destination;
            // Already separating: allow the original maneuver to finish.
            if(delta.X*relative.X+delta.Y*relative.Y>0&&ShipOrders.Length(delta)>safe)return destination;
            float length=ShipOrders.Length(delta);
            if(length<.001f)return destination;
            var right=new AimPoint(delta.Y/length,-delta.X/length);
            return new AimPoint(position.X+right.X*safe-delta.X/length*safe*.35f,
                position.Y+right.Y*safe-delta.Y/length*safe*.35f);
        }
        private bool initialized,recovering;
        private float age,blocked;
        private CombatRoleDecision current;
        private string targetId;
        public CombatManeuver Maneuver=>current.Maneuver;
        public CombatRoleDecision Decide(CombatRole role,string opponent,CombatRoleObservation o,float deltaSeconds)
        {
            float dt=Math.Max(0,deltaSeconds);age+=dt;blocked=o.LineBlocked?blocked+dt:0f;
            bool emergency=!o.TargetAvailable||!o.EngineAvailable;
            if(role==CombatRole.Artillery)
            {
                if(o.EnergyFraction<.2f||o.HeatFraction>.8f)recovering=true;
                else if(o.EnergyFraction>.45f&&o.HeatFraction<.45f)recovering=false;
            }
            float distance=ShipOrders.Length(ShipOrders.Subtract(o.Position,o.TargetPosition));
            bool changedTarget=targetId!=opponent;
            bool recoverTransition=role==CombatRole.Artillery&&recovering&&current.Maneuver!=CombatManeuver.Recover;
            bool recovered=role==CombatRole.Artillery&&!recovering&&current.Maneuver==CombatManeuver.Recover;
            float safe=SafeDistance(o);
            bool exit=role==CombatRole.Interceptor&&current.Maneuver==CombatManeuver.Attack&&distance<Math.Max(o.Range*.25f,safe);
            bool transition=!initialized||changedTarget||emergency||recoverTransition||recovered||exit||age>=1.5f;
            current.NewCommand=false;
            if(transition)
            {
                var next=Choose(role,o,distance);
                bool differs=!initialized||changedTarget||next.Maneuver!=current.Maneuver;
                // A bounded reposition/waypoint is retained for its whole action; do not chase it per-frame.
                if(differs){current=next;current.NewCommand=true;age=0;}
                initialized=true;targetId=opponent;
            }
            current.AllowFire=o.TargetAvailable&&o.WeaponAvailable&&!o.LineBlocked&&current.Maneuver!=CombatManeuver.Recover;
            bool retreat=current.Maneuver==CombatManeuver.Exit||current.Maneuver==CombatManeuver.Approach||current.Maneuver==CombatManeuver.Recover;
            current.Boost=o.EngineAvailable&&retreat&&o.BoostReady&&o.Energy-25f>=o.MaxEnergy*.2f;
            current.Shield=o.ShieldReady&&o.ShieldFraction<.35f&&o.HullFraction>.4f&&o.Hull>10f&&o.Energy>=30f&&o.HeatFraction<1f;
            current.Emp=o.EmpReady&&o.TargetAvailable&&!o.TargetSuppressed&&distance<=o.Range&&o.Energy>=35f&&o.HeatFraction<1f;
            return current;
        }
        private CombatRoleDecision Choose(CombatRole role,CombatRoleObservation o,float distance)
        {
            if(!o.TargetAvailable||!o.EngineAvailable)return Tactical(CombatManeuver.Disabled,OrderMode.HoldPosition,0);
            if(current.Maneuver==CombatManeuver.Reposition&&age<2f)return current;
            if(blocked>1.5f&&current.Maneuver!=CombatManeuver.Reposition)
            {blocked=0;return Waypoint(CombatManeuver.Reposition,o,.6f,true);}
            if(role==CombatRole.Artillery)
            {
                if(recovering)return Tactical(CombatManeuver.Recover,OrderMode.KeepDistance,o.Range*1.1f);
                float threshold=current.Maneuver==CombatManeuver.Exit?.55f:.45f;
                return Tactical(distance<Math.Max(o.Range*threshold,SafeDistance(o))?CombatManeuver.Exit:CombatManeuver.Attack,OrderMode.KeepDistance,Math.Max(o.Range*.8f,SafeDistance(o)));
            }
            if(role==CombatRole.Escort)
            {
                if(distance<SafeDistance(o))return Tactical(CombatManeuver.Exit,OrderMode.KeepDistance,SafeDistance(o)*1.25f);
                if(current.Maneuver==CombatManeuver.Exit&&distance<SafeDistance(o)*1.3f)return current;
                if(current.Maneuver==CombatManeuver.Attack&&age<1.5f)return current;
                if(current.Maneuver==CombatManeuver.Orbit&&age>=1.5f)return Tactical(CombatManeuver.Attack,OrderMode.KeepDistance,o.Range*.65f);
                return Tactical(CombatManeuver.Orbit,OrderMode.Orbit,o.Range*.65f);
            }
            if(current.Maneuver==CombatManeuver.Exit)
            {
                if(distance<o.Range*.9f&&age<4.5f)return current;
                return Waypoint(CombatManeuver.Approach,o,.85f,true);
            }
            if(current.Maneuver==CombatManeuver.Approach)
            {
                if(age<1.5f)return current;
                return Pass(o);
            }
            if(current.Maneuver==CombatManeuver.Attack)
            {
                if(distance<Math.Max(o.Range*.25f,SafeDistance(o))||age>=1.5f)
                {
                    var next=current;next.Maneuver=CombatManeuver.Exit;return next;
                }
                return current;
            }
            return Waypoint(CombatManeuver.Approach,o,.85f,true);
        }
        private static CombatRoleDecision Tactical(CombatManeuver maneuver,OrderMode mode,float distance)=>
            new CombatRoleDecision {Maneuver=maneuver,Order=new PilotDecision{Mode=mode,PreferredDistance=distance},Orientation=mode==OrderMode.Orbit?OrderOrientation.Course:OrderOrientation.OrderTarget};
        private static CombatRoleDecision Pass(CombatRoleObservation o)
        {
            var direction=ShipOrders.Subtract(o.TargetPosition,o.Position);
            float length=ShipOrders.Length(direction);
            direction=length>.001f?new AimPoint(direction.X/length,direction.Y/length):o.Forward;
            // Keep the entire straight pass outside the target's physical envelope,
            // not merely its endpoint. The side stays fixed for this maneuver.
            float clearance=(o.Radius+o.TargetRadius)*1.6f;
            float lateral=length>clearance?clearance*(length+o.Range)/length:clearance*2;
            return new CombatRoleDecision {Maneuver=CombatManeuver.Attack,UseWaypoint=true,Orientation=OrderOrientation.Course,
                Waypoint=new AimPoint(o.TargetPosition.X+direction.X*o.Range+direction.Y*lateral,o.TargetPosition.Y+direction.Y*o.Range-direction.X*lateral)};
        }
        private static float SafeDistance(CombatRoleObservation o)
        {
            var delta=ShipOrders.Subtract(o.TargetPosition,o.Position);
            float distance=ShipOrders.Length(delta);
            var relative=ShipOrders.Subtract(o.Velocity,o.TargetVelocity);
            float closing=distance>.001f?Math.Max(0,(relative.X*delta.X+relative.Y*delta.Y)/distance):0;
            return Math.Max(.5f,(o.Radius+o.TargetRadius)*1.3f)+Math.Min(o.Range*.4f,closing*.4f);
        }
        private static CombatRoleDecision Waypoint(CombatManeuver maneuver,CombatRoleObservation o,float multiplier,bool lateral)
        {
            var away=ShipOrders.Subtract(o.Position,o.TargetPosition);float length=ShipOrders.Length(away);
            away=length>.001f?new AimPoint(away.X/length,away.Y/length):new AimPoint(0,1);
            if(lateral)away=MountGeometry.Rotate(away,70f);
            return new CombatRoleDecision {Maneuver=maneuver,UseWaypoint=true,Orientation=OrderOrientation.Course,
                Waypoint=new AimPoint(o.TargetPosition.X+away.X*o.Range*multiplier,o.TargetPosition.Y+away.Y*o.Range*multiplier)};
        }
    }
}