using System;

namespace Game.Core
{
    // Separate from legacy serialized personalities: their numeric meaning never changes.
    public enum CombatRole { Interceptor=0, Artillery=1, Escort=2 }
    public enum CombatManeuver { Approach, Attack, Exit, Recover, Orbit, Reposition, Disabled }
    public struct CombatRoleObservation
    {
        public AimPoint Position, Forward, TargetPosition, TargetVelocity, TargetForward;
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
            bool exit=role==CombatRole.Interceptor&&current.Maneuver==CombatManeuver.Attack&&distance<o.Range*.25f;
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
                return Tactical(distance<o.Range*.45f?CombatManeuver.Exit:CombatManeuver.Attack,OrderMode.KeepDistance,o.Range*.8f);
            }
            if(role==CombatRole.Escort)
            {
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
                return Tactical(CombatManeuver.Attack,OrderMode.EngageTarget,o.Range*.1f);
            }
            if(current.Maneuver==CombatManeuver.Attack)
            {
                if(distance<o.Range*.25f||age>=1.5f)return Waypoint(CombatManeuver.Exit,o,1.2f,false);
                return current;
            }
            return Waypoint(CombatManeuver.Approach,o,.85f,true);
        }
        private static CombatRoleDecision Tactical(CombatManeuver maneuver,OrderMode mode,float distance)=>
            new CombatRoleDecision {Maneuver=maneuver,Order=new PilotDecision{Mode=mode,PreferredDistance=distance},Orientation=mode==OrderMode.Orbit?OrderOrientation.Course:OrderOrientation.OrderTarget};
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