namespace Game.Core
{
    // Numeric values match the existing serialized Unity enums. Do not reorder.
    public enum MountCompatibility { FixedOnly = 0, TurretOnly = 1, Universal = 2 }
    public enum WeaponMountType { Fixed = 0, Turret = 1 }

    public static class MountCompatibilityRules
    {
        public static bool Accepts(MountCompatibility mount, WeaponMountType type)
        {
            return mount == MountCompatibility.Universal ||
                (mount == MountCompatibility.FixedOnly && type == WeaponMountType.Fixed) ||
                (mount == MountCompatibility.TurretOnly && type == WeaponMountType.Turret);
        }
    }
}