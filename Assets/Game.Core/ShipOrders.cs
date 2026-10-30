using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class BattleArea
    {
        public AimPoint Center { get; }
        public float Radius { get; }
        public BattleArea(AimPoint center,float radius){Center=center;Radius=Math.Max(1,radius);}
        public bool Contains(AimPoint point,float margin=0)=>ShipOrders.Length(ShipOrders.Subtract(point,Center))<=Math.Max(0,Radius-margin);
        public AimPoint Clamp(AimPoint point,float margin=0)
        {
            var d=ShipOrders.Subtract(point,Center);float length=ShipOrders.Length(d),limit=Math.Max(0,Radius-margin);
            return length<=limit?point:new AimPoint(Center.X+d.X/length*limit,Center.Y+d.Y/length*limit);
        }
    }
    public enum OrderMode { HoldPosition, MoveToPoint, EngageTarget, Orbit, KeepDistance, Flank, FollowCursor }
    public enum OrderOrientation { Course, OrderTarget }
    public enum PilotPersonality { Brawler, Sniper, Flanker, Cautious }

    public struct OrderObservation
    {
        public AimPoint Position, Velocity, TargetPosition, TargetForward;
        public bool TargetAvailable;
        public float WeaponRange;
    }

    /// <summary>One owner of the order. Physics projection and retry clocks belong to the adapter.</summary>
    public sealed class ShipOrders
    {
        private readonly List<AimPoint> points = new List<AimPoint>();
        private OrderOrientation? orientationOverride;
        private bool initialized, flankAttacking;
        private AimPoint hold;
        public OrderMode Mode { get; private set; }
        public string TargetId { get; private set; }
        public string OrientationTargetId { get; private set; }
        public bool SetOrientationTarget(string id)
        {
            if(string.IsNullOrEmpty(id))return false;
            OrientationTargetId=id;orientationOverride=OrderOrientation.OrderTarget;return true;
        }
        public void ValidateOrientationTarget(bool available)
        {
            if(OrientationTargetId!=null&&!available){OrientationTargetId=null;orientationOverride=OrderOrientation.Course;}
        }
        public float SpeedLimit { get; private set; } = 1f;
        public float PreferredDistance { get; set; }
        public AimPoint Destination { get; private set; }
        public bool IsBraking { get; private set; }
        public string Error { get; private set; }
        public int NavigationRevision { get; private set; }
        public IReadOnlyList<AimPoint> Waypoints => Array.AsReadOnly(points.ToArray());
        public OrderOrientation Orientation => orientationOverride ??
            (Mode == OrderMode.EngageTarget || Mode == OrderMode.KeepDistance || Mode == OrderMode.Flank && flankAttacking
                ? OrderOrientation.OrderTarget : OrderOrientation.Course);
        public bool SetOrientation(OrderOrientation value, bool targetAvailable)
        {
            if (value == OrderOrientation.OrderTarget && !targetAvailable) return false;
            OrientationTargetId=null;orientationOverride = value; return true;
        }
        public void SetSpeedLimit(float value) { SpeedLimit = MountGeometry.Clamp(value, .25f, 1f); }
        public void MoveTo(AimPoint accessiblePoint, bool append = false)
        {
            if (!append || Mode != OrderMode.MoveToPoint) points.Clear();
            OrientationTargetId=null;orientationOverride = null; flankAttacking = false; TargetId = null; Error = null;
            Mode = OrderMode.MoveToPoint; IsBraking = false; points.Add(accessiblePoint);
            Destination = points[0]; initialized = true; NavigationRevision++;
        }
        public void BeginFollowCursor(AimPoint point)
        {
            MoveTo(point); Mode = OrderMode.FollowCursor;
        }
        public void UpdateFollowCursor(AimPoint point)
        {
            if (Mode != OrderMode.FollowCursor) return;
            points[0] = point; Destination = point;
        }
        public void EndFollowCursor()
        {
            if (Mode == OrderMode.FollowCursor) Mode = OrderMode.MoveToPoint;
        }
        public void Tactical(OrderMode mode, string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            if (Mode != mode || TargetId != targetId) { if(OrientationTargetId==null)orientationOverride = null; flankAttacking = false; }
            points.Clear(); TargetId = targetId; Mode = mode; Error = null;
            initialized = true; IsBraking = false; NavigationRevision++;
        }
        public void Stop(AimPoint position)
        {
            OrientationTargetId=null;orientationOverride = null; flankAttacking = false; points.Clear(); TargetId = null;
            Mode = OrderMode.HoldPosition; hold = position; Destination = hold; initialized = true;
            IsBraking = true; Error = null; NavigationRevision++;
        }
        public void CorrectFirstWaypoint(AimPoint point) { if (points.Count > 0) points[0] = point; }
        public void AdvanceRouteLeg(AimPoint position)
        {
            if (Mode == OrderMode.MoveToPoint && points.Count > 1 && Distance(position, points[0]) < .65f)
            { points.RemoveAt(0); NavigationRevision++; }
        }
        public void NavigationFailed(AimPoint position)
        {
            TargetId = null; Mode = OrderMode.HoldPosition; hold = position; IsBraking = true;
            Error = "Нет прогресса после 3 попыток локального обхода";
        }
        public static float Length(AimPoint p) => (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
        public static AimPoint Subtract(AimPoint a, AimPoint b) => new AimPoint(a.X - b.X, a.Y - b.Y);
        private static float Distance(AimPoint a, AimPoint b) => Length(Subtract(a, b));
        public AimPoint Evaluate(OrderObservation o, out bool final, bool advanceRoute = true)
        {
            if (!initialized) { hold = o.Position; initialized = true; }
            final = true;
            if (Mode == OrderMode.MoveToPoint || Mode == OrderMode.FollowCursor)
            {
                if (points.Count == 0) { Stop(o.Position); return hold; }
                if (advanceRoute) AdvanceRouteLeg(o.Position);
                Destination = points[0]; final = points.Count == 1;
                if (Mode == OrderMode.MoveToPoint && final && Distance(o.Position, Destination) < .18f && Length(o.Velocity) < .12f) { Stop(o.Position); return hold; }
                return Destination;
            }
            if (Mode == OrderMode.HoldPosition)
            {
                if (IsBraking) { hold = o.Position; Destination = hold; if (Length(o.Velocity) < .08f) IsBraking = false; }
                return hold;
            }
            if (!o.TargetAvailable) { Stop(o.Position); return hold; }
            float distance = PreferredDistance > 0f ? PreferredDistance : o.WeaponRange * .7f;
            var delta = Subtract(o.Position, o.TargetPosition); float length = Length(delta);
            var away = length > .00001f ? new AimPoint(delta.X / length, delta.Y / length) : new AimPoint(0, 1);
            if (Mode == OrderMode.Flank)
            {
                Destination = new AimPoint(o.TargetPosition.X - o.TargetForward.X * distance, o.TargetPosition.Y - o.TargetForward.Y * distance);
                float error = Distance(o.Position, Destination);
                if (error < .65f) flankAttacking = true; else if (error > 1.4f) flankAttacking = false;
            }
            else if (Mode == OrderMode.Orbit)
            {
                var rotated = MountGeometry.Rotate(away, 20);
                Destination = new AimPoint(o.TargetPosition.X + rotated.X * distance, o.TargetPosition.Y + rotated.Y * distance);
            }
            else Destination = Math.Abs(length - distance) < .35f ? o.Position :
                new AimPoint(o.TargetPosition.X + away.X * distance, o.TargetPosition.Y + away.Y * distance);
            return Destination;
        }
        public AimPoint Facing(OrderObservation o, AimPoint fallback) =>
            Orientation == OrderOrientation.OrderTarget && o.TargetAvailable ? Subtract(o.TargetPosition, o.Position) : fallback;
    }

    public struct PilotDecision
    {
        public OrderMode Mode;
        public float PreferredDistance;
        public void Apply(ShipOrders orders, string targetId, AimPoint position)
        {
            if (Mode == OrderMode.HoldPosition) { if (orders.Mode != Mode) orders.Stop(position); return; }
            orders.PreferredDistance = PreferredDistance;
            if (orders.Mode != Mode || orders.TargetId != targetId) orders.Tactical(Mode, targetId);
        }
    }
    public static class PilotDecisions
    {
        public static PilotDecision Decide(PilotPersonality personality, bool targetAvailable, bool hasHealth,
            float hull, float maximumHull, float weaponRange, float optimalDistance)
        {
            if (!targetAvailable) return new PilotDecision { Mode = OrderMode.HoldPosition };
            bool retreat = personality == PilotPersonality.Cautious && hasHealth && hull < maximumHull * .35f;
            return new PilotDecision {
                PreferredDistance = retreat ? weaponRange * 1.4f : personality == PilotPersonality.Sniper ? weaponRange * .85f : personality == PilotPersonality.Flanker ? weaponRange * .5f : optimalDistance,
                Mode = personality == PilotPersonality.Flanker ? OrderMode.Flank : personality == PilotPersonality.Sniper || retreat ? OrderMode.KeepDistance : OrderMode.Orbit
            };
        }
    }
}