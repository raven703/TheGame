using System;
using System.Collections.Generic;

namespace Game.Core
{
    public struct AimPoint
    {
        public readonly float X, Y;
        public AimPoint(float x, float y) { X = x; Y = y; }
    }

    public enum AimRefusal { None, Incompatible, Range, Sector, Tracking }

    public struct AimResult
    {
        public bool InRange, InSector, Aligned;
        public AimRefusal Refusal;
        public bool Ready => Refusal == AimRefusal.None;
    }

    // Angles are degrees, counterclockwise positive. Stored angles are relative
    // to the immutable mount axis, NOT to the rendered barrel or firePoint rotation.
    public sealed class MountGeometry
    {
        private readonly Dictionary<string, float> angles = new Dictionary<string, float>();
        public float Angle(string mountId) => angles.TryGetValue(mountId, out var value) ? value : 0f;
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float Normalize(float degrees)
        {
            float value = degrees % 360f;
            if (value < 0f) value += 360f;
            return value > 180f ? value - 360f : value;
        }
        public static AimPoint Rotate(AimPoint axis, float degrees)
        {
            double radians = degrees * Math.PI / 180d;
            float c = (float)Math.Cos(radians), s = (float)Math.Sin(radians);
            return new AimPoint(axis.X * c - axis.Y * s, axis.X * s + axis.Y * c);
        }
        public static AimPoint FixedAxis(AimPoint hullForward, float localAxis) => Rotate(hullForward, localAxis);
        public static float EffectiveSector(float mountSector, WeaponMountType type, float fixedSector) =>
            Math.Min(Clamp(mountSector, 0f, 360f), type == WeaponMountType.Fixed ? fixedSector : 360f);
        private static float SignedAngle(AimPoint axis, AimPoint delta)
        {
            double length = Math.Sqrt((double)axis.X * axis.X + (double)axis.Y * axis.Y) *
                Math.Sqrt((double)delta.X * delta.X + (double)delta.Y * delta.Y);
            if (length < 1e-15) return 0f; // Unity Vector2.Angle zero-vector convention.
            double dot = Clamp((float)((axis.X * delta.X + axis.Y * delta.Y) / length), -1f, 1f);
            float angle = (float)(Math.Acos(dot) * 180d / Math.PI);
            return axis.X * delta.Y - axis.Y * delta.X < 0f ? -angle : angle;
        }
        public AimResult Advance(string mountId, AimPoint origin, AimPoint fixedAxis, AimPoint target,
            float range, float mountSector, WeaponMountType type, float fixedSector,
            float trackingSpeed, float tolerance, float deltaSeconds, bool compatible = true)
        {
            if (!compatible) return new AimResult { Refusal = AimRefusal.Incompatible };
            var delta = new AimPoint(target.X - origin.X, target.Y - origin.Y);
            float desired = SignedAngle(fixedAxis, delta);
            float half = Clamp(mountSector, 0f, 360f) * .5f;
            bool inside = Math.Abs(desired) <= half;
            float current = Angle(mountId);
            bool aligned;
            if (type == WeaponMountType.Fixed)
            {
                inside = inside && Math.Abs(desired) <= EffectiveSector(mountSector, type, fixedSector) * .5f + .0001f;
                aligned = true;
            }
            else
            {
                float goal = Clamp(desired, -half, half);
                float difference = half >= 180f ? Normalize(goal - current) : goal - current;
                float step = Math.Max(0f, trackingSpeed) * deltaSeconds;
                current += Math.Abs(difference) <= step ? difference : (difference < 0f ? -step : step);
                current = Normalize(current);
                angles[mountId] = current;
                aligned = Math.Abs(Normalize(desired - current)) <= Clamp(tolerance, 0f, 15f);
            }
            bool inRange = Math.Sqrt((double)delta.X * delta.X + (double)delta.Y * delta.Y) <= range;
            return new AimResult { InRange = inRange, InSector = inside, Aligned = aligned,
                Refusal = !inside ? AimRefusal.Sector : !inRange ? AimRefusal.Range : !aligned ? AimRefusal.Tracking : AimRefusal.None };
        }
    }
}