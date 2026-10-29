using Game.Core;
using NUnit.Framework;

public class CombatArchetypeCatalogTests
{
    [TestCase("w4",10f,.7f,6f,7f)]
    [TestCase("w2",22f,1.4f,14f,20f)]
    [TestCase("w3",26f,2f,20f,25f)]
    [TestCase("combat-v1:laser-turret",7f,.8f,7f,8f)]
    public void WeaponsHaveIndependentDefinitions(string id,float damage,float interval,float energy,float heat)
    {
        Assert.True(CombatArchetypeCatalog.TryWeapon(id,out var value));
        Assert.That(value.Damage,Is.EqualTo(damage));Assert.That(value.Cooldown,Is.EqualTo(interval));
        Assert.That(value.Energy,Is.EqualTo(energy));Assert.That(value.Heat,Is.EqualTo(heat));
    }
    [Test] public void PercentagesSumAfterAdditivesAndAreCapped()
    {
        var basis=ShipStatValues.Fallback;
        var bonus=new EquipmentStatBonus {Speed=1,Thrust=2,SpeedPercent=.15f,ThrustPercent=.1f,ReversePercent=.3f,LateralPercent=.3f,AfterburnerHeatPercent=.25f};
        var s=StatMath.Calculate(basis,new[]{bonus,bonus,bonus});
        Assert.That(s.Speed,Is.EqualTo(8f*1.3f).Within(.0001));
        Assert.That(s.Thrust,Is.EqualTo(18f*1.2f).Within(.0001));
        Assert.That(s.ReverseThrust,Is.EqualTo(9f*1.6f).Within(.0001));
        Assert.That(s.LateralThrust,Is.EqualTo(6.3f*1.6f).Within(.0001));
        Assert.That(s.AfterburnerHeatMultiplier,Is.EqualTo(1.75f));
        Assert.That(Reactor.CalculateHeat(20f*s.AfterburnerHeatMultiplier,.25f),Is.EqualTo(26.25f));
    }
    [Test] public void ModulesHaveOnlyTheirDeclaredEffects()
    {
        CombatArchetypeCatalog.TryModule(CombatArchetypeCatalog.LightweightEngine,out var engine);
        var basis=ShipStatValues.Fallback;var s=StatMath.Calculate(basis,new[]{engine});
        Assert.That(s.Thrust,Is.EqualTo(13.2f).Within(.0001));Assert.That(s.ReverseThrust,Is.EqualTo(6));
        Assert.That(s.LateralThrust,Is.EqualTo(4.2f).Within(.0001));Assert.That(s.Cooling,Is.EqualTo(15));
        CombatArchetypeCatalog.TryModule(CombatArchetypeCatalog.ManeuverBlock,out var maneuver);
        s=StatMath.Calculate(basis,new[]{maneuver});Assert.That(s.Thrust,Is.EqualTo(12));
        Assert.That(s.Speed,Is.EqualTo(5));Assert.That(s.Mass,Is.EqualTo(2.3f).Within(.0001));
        CombatArchetypeCatalog.TryModule(CombatArchetypeCatalog.HeavyShield,out var shield);
        CombatArchetypeCatalog.TryModule(CombatArchetypeCatalog.Radiator,out var radiator);
        CombatArchetypeCatalog.TryModule(CombatArchetypeCatalog.Capacitor,out var capacitor);
        s=StatMath.Calculate(basis,new[]{shield,radiator,capacitor});
        Assert.That(s.Shield,Is.EqualTo(90));Assert.That(s.Mass,Is.EqualTo(3.4f).Within(.0001));
        Assert.That(s.Energy,Is.EqualTo(135));Assert.That(s.Regeneration,Is.EqualTo(12));Assert.That(s.Cooling,Is.EqualTo(21));
    }
    [Test] public void DiagonalEffortIsNotFreeAndReverseUsesOwnBudget()
    {
        var f=DirectedThrust.Limit(12,4.2f,12,6,4.2f);
        Assert.That(f.X*f.X/144f+f.Y*f.Y/(4.2f*4.2f),Is.EqualTo(1).Within(.0001));
        Assert.That(DirectedThrust.Limit(-100,0,12,6,4.2f).X,Is.EqualTo(-6));
        Assert.That(DirectedThrust.Limit(100,100,0,0,0).X,Is.Zero);
    }
    [Test] public void UnknownDefinitionsAreNotImplicitlyStandard()
    {
        Assert.False(CombatArchetypeCatalog.TryWeapon("w1",out _));
        Assert.False(CombatArchetypeCatalog.TryModule("e3",out _));
        Assert.False(CombatArchetypeCatalog.TryWeapon(null,out _));
    }
}