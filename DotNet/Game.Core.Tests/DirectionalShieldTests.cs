using Game.Core;
using NUnit.Framework;
public class DirectionalShieldTests
{
    [TestCase(.5f)] [TestCase(.75f)] [TestCase(1f)] [TestCase(0f)]
    public void DesiredPreviewDoesNotChangeHpOrCapacity_TransferConservesTotal(float desired)
    {
        var s=new DirectionalShield();s.Initialize(125);s.SetTotal(63);
        float front=s.Front,rear=s.Rear,capacity=s.FrontCapacity;
        s.SetDesired(desired);s.Tick(0,true);
        Assert.AreEqual(front,s.Front);Assert.AreEqual(rear,s.Rear);
        Assert.AreEqual(capacity,s.FrontCapacity);Assert.AreEqual(.5f,s.Fraction);
        for(int i=0;i<100;i++){s.Tick(.01f,true);Assert.That(s.Total,Is.EqualTo(63).Within(.0001f));}
        Assert.AreEqual(desired,s.Fraction);Assert.AreEqual(125,s.FrontCapacity+s.RearCapacity);
    }
    private sealed class Random : ICombatRandom { public float Value()=>1f;public int Index(int n)=>0; }
    [Test] public void SideBoundary_EmptySideDoesNotBorrow_RailgunBypasses()
    {
        Assert.AreEqual(ShieldSide.Front,DirectionalShield.Side(new AimPoint(0,1),new AimPoint(1,0)));
        Assert.AreEqual(ShieldSide.Rear,DirectionalShield.Side(new AimPoint(0,1),new AimPoint(0,-1)));
        var d=new DamageState(100);d.Shields.SetDesired(0);d.TickShields(1);
        d.WeaponDamage(20,WeaponKind.Railgun,null,new Random(),true,side:ShieldSide.Front);
        Assert.AreEqual(50,d.Shield);Assert.AreEqual(80,d.Hull);Assert.AreEqual(3,d.Shields.FrontDelay);
    }
    [Test] public void TransferRetargetWithDamageConservesHp_AndNeverExceedsCapacity()
    {
        var d=new DamageState(100);d.SetShield(31);var s=d.Shields;s.SetDesired(0);d.TickShields(1);
        Assert.AreEqual(31,s.Total);s.SetDesired(1);
        for(int i=0;i<100;i++)
        {
            if(i==20)s.SetDesired(.25f);if(i==40){s.Absorb(3,ShieldSide.Rear,true);s.SetDesired(1);}
            float total=s.Total;d.TickShields(.01f);
            Assert.That(s.Total,Is.EqualTo(total).Within(.0001));
            Assert.That(s.Front,Is.InRange(0,s.FrontCapacity+.0001f));Assert.That(s.Rear,Is.InRange(0,s.RearCapacity+.0001f));
        }
        d.TickShields(1);Assert.AreEqual(1,s.Fraction);
    }
    [Test] public void IndependentDelays_LargeStep_NoSpill_DisabledModule_AndBoost()
    {
        var d=new DamageState(100);d.SetShield(0);var s=d.Shields;s.RegenPerSecond=10;s.Hit(ShieldSide.Front);
        d.TickShields(4);Assert.AreEqual(5,s.Front);Assert.AreEqual(20,s.Rear);
        s.Hit(ShieldSide.Front);d.BoostShield(10,10);Assert.AreEqual(3,s.FrontDelay);Assert.AreEqual(90,d.Hull);
        float total=s.Total;d.ModuleDamage(CombatModule.Shield,40);s.SetDesired(1);d.TickShields(10);
        Assert.AreEqual(total,s.Total);Assert.AreEqual(.5f,s.Fraction);
        d.RepairModule(CombatModule.Shield,40);d.TickShields(1);Assert.AreEqual(1,s.Fraction);
    }
}