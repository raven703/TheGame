using Game.Core;
using NUnit.Framework;

public class MountGeometryTests
{
    private static AimPoint Direction(float degrees) => MountGeometry.Rotate(new AimPoint(0, 1), degrees);
    private static AimResult Aim(MountGeometry state, float target, float sector = 360, float dt = 1,
        WeaponMountType type = WeaponMountType.Turret, string id = "ship:weapon:0", float tolerance = 3) =>
        state.Advance(id, new AimPoint(0, 0), new AimPoint(0, 1), Direction(target), 8, sector, type, 60, 90, tolerance, dt);

    [TestCase(0, true)] [TestCase(29.99f, true)] [TestCase(30, true)]
    [TestCase(30.01f, false)] [TestCase(90, false)] [TestCase(180, false)]
    public void FixedOwnSectorOnUniversal360(float target, bool ready) =>
        Assert.AreEqual(ready, Aim(new MountGeometry(), target, type: WeaponMountType.Fixed).Ready);

    [TestCase(180)] [TestCase(360)]
    public void UniversalDoesNotExpandFixed(float sector) =>
        Assert.IsFalse(Aim(new MountGeometry(), 70, sector, type: WeaponMountType.Fixed).Ready);

    [Test] public void NarrowMountIntersectsFixed()
    {
        Assert.IsTrue(Aim(new MountGeometry(), 10, 40, type: WeaponMountType.Fixed).Ready);
        Assert.IsFalse(Aim(new MountGeometry(), 25, 40, type: WeaponMountType.Fixed).Ready);
    }

    [Test] public void ActualOriginAndRotatedAxisDetermineRangeAndSector()
    {
        var state = new MountGeometry(); var axis = MountGeometry.FixedAxis(new AimPoint(1, 0), 90);
        var result = state.Advance("offset", new AimPoint(100, 100), axis, new AimPoint(100, 108), 8, 60,
            WeaponMountType.Fixed, 60, 90, 3, .1f);
        Assert.IsTrue(result.Ready);
        Assert.AreEqual(AimRefusal.Range, state.Advance("offset", new AimPoint(100, 100), axis,
            new AimPoint(100, 108.01f), 8, 60, WeaponMountType.Fixed, 60, 90, 3, .1f).Refusal);
    }

    [Test] public void SequenceUsesActualAngleAndTolerance()
    {
        var state = new MountGeometry();
        Assert.AreEqual(AimRefusal.Tracking, Aim(state, -90, dt: .1f).Refusal);
        Assert.That(state.Angle("ship:weapon:0"), Is.EqualTo(-9).Within(.001));
        for (int i = 0; i < 8; i++) Aim(state, -90, dt: .1f);
        Assert.IsFalse(Aim(state, -90, dt: 0).Ready);
        Assert.IsTrue(Aim(state, -90, dt: .1f).Ready);
        Assert.IsTrue(Aim(state, -87, dt: 0).Ready);
        Assert.IsFalse(Aim(state, -86.9f, dt: 0).Ready);
    }

    [Test] public void FullCircleWrapsAcross180()
    {
        var state = new MountGeometry(); Aim(state, 179, dt: 3);
        Assert.IsTrue(Aim(state, -179, dt: .1f).Ready);
        Assert.That(state.Angle("ship:weapon:0"), Is.EqualTo(-179).Within(.001));
    }

    [Test] public void LimitedSectorNeverCrossesForbiddenRear()
    {
        var state = new MountGeometry(); Aim(state, 179, 300, 3);
        Assert.IsFalse(Aim(state, -179, 300, .1f).Ready);
        Assert.That(state.Angle("ship:weapon:0"), Is.EqualTo(141).Within(.001));
    }

    [Test] public void IndependentStableIdsAndReadOnlyRefresh()
    {
        var state = new MountGeometry(); Aim(state, 90, dt: .1f);
        Assert.That(state.Angle("ship:weapon:0"), Is.EqualTo(9).Within(.001));
        Assert.AreEqual(0, state.Angle("ship:weapon:2")); // holes never create an installation
        Aim(state, -90, dt: .2f, id: "ship:weapon:2");
        Assert.That(state.Angle("ship:weapon:0"), Is.EqualTo(9).Within(.001));
        Assert.That(state.Angle("ship:weapon:2"), Is.EqualTo(-18).Within(.001));
        Assert.AreEqual(0, state.Angle("other-ship:weapon:0"));
    }

    [Test] public void IncompatibleDoesNotAdvance()
    {
        var state = new MountGeometry();
        Assert.AreEqual(AimRefusal.Incompatible, state.Advance("x", new AimPoint(), new AimPoint(0, 1),
            new AimPoint(1, 0), 8, 360, WeaponMountType.Turret, 60, 90, 3, 1, false).Refusal);
        Assert.AreEqual(0, state.Angle("x"));
    }
}