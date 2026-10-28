using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class MountCompatibilityTests
    {
        [TestCase(MountCompatibility.FixedOnly, WeaponMountType.Fixed, true)]
        [TestCase(MountCompatibility.FixedOnly, WeaponMountType.Turret, false)]
        [TestCase(MountCompatibility.TurretOnly, WeaponMountType.Fixed, false)]
        [TestCase(MountCompatibility.TurretOnly, WeaponMountType.Turret, true)]
        [TestCase(MountCompatibility.Universal, WeaponMountType.Fixed, true)]
        [TestCase(MountCompatibility.Universal, WeaponMountType.Turret, true)]
        // Preserve the old expression even for unknown serialized enum values.
        [TestCase(MountCompatibility.Universal, (WeaponMountType)99, true)]
        [TestCase(MountCompatibility.FixedOnly, (WeaponMountType)99, false)]
        [TestCase(MountCompatibility.TurretOnly, (WeaponMountType)99, false)]
        [TestCase((MountCompatibility)99, WeaponMountType.Fixed, false)]
        [TestCase((MountCompatibility)99, WeaponMountType.Turret, false)]
        public void Accepts_PreservesExistingTable(MountCompatibility mount, WeaponMountType type, bool expected)
        {
            Assert.That(MountCompatibilityRules.Accepts(mount, type), Is.EqualTo(expected));
        }

        [Test]
        public void Core_HasNoUnityAssemblyReferences()
        {
            foreach (var reference in typeof(MountCompatibilityRules).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name, Does.Not.StartWith("Unity"));
        }
    }
}