using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class StatsReactorTests
    {
        [Test] public void BaseAndInstalledTotalsAreDetachedAndDoNotAccumulate()
        {
            var basis=ShipStatValues.Fallback;
            var bonuses=new List<EquipmentStatBonus>{new EquipmentStatBonus{Shield=30,Speed=3,Thrust=6,Mass=.2f},
                new EquipmentStatBonus{Shield=80,Mass=1,LockTime=-.2f,Resistance=.25f}};
            var empty=StatMath.Calculate(basis,null);Assert.That(empty.Shield,Is.EqualTo(50));Assert.That(empty.Speed,Is.EqualTo(5));
            var result=StatMath.Calculate(basis,bonuses);
            for(int i=0;i<3;i++){var s=StatMath.Calculate(basis,bonuses);Assert.That(s.Shield,Is.EqualTo(160));Assert.That(s.Speed,Is.EqualTo(8));
                Assert.That(s.Thrust,Is.EqualTo(18));Assert.That(s.Mass,Is.EqualTo(3.2f).Within(.0001));Assert.That(s.LockTime,Is.EqualTo(.3f).Within(.0001));}
            bonuses.Clear();basis.Speed=999;Assert.That(result.Speed,Is.EqualTo(8));Assert.That(result.Resistance,Is.EqualTo(.25f));
        }
        [Test] public void ClampAfterSumAndLegacyResistanceRemainUnchanged()
        {
            var basis=ShipStatValues.Fallback;basis.Energy=-1;basis.Regeneration=-1;basis.Cooling=-1;
            var s=StatMath.Calculate(basis,new[]{new EquipmentStatBonus{Mass=-100,Shield=-100,LockTime=-10,Resistance=2,Speed=-20,Thrust=-30}});
            Assert.That(s.Mass,Is.EqualTo(.5f));Assert.That(s.Shield,Is.Zero);Assert.That(s.LockTime,Is.EqualTo(.05f));
            Assert.That(s.Resistance,Is.EqualTo(.75f));Assert.That(s.Energy,Is.Zero);Assert.That(s.Regeneration,Is.Zero);Assert.That(s.Cooling,Is.Zero);
            Assert.That(s.Speed,Is.EqualTo(-15));Assert.That(s.Thrust,Is.EqualTo(-18));
            Assert.That(StatMath.EffectiveResistanceBonus("e2",.7f),Is.EqualTo(.1f));Assert.That(StatMath.EffectiveResistanceBonus("custom",.7f),Is.EqualTo(.7f));
            basis.Resistance=-1;Assert.That(StatMath.Calculate(basis,null).Resistance,Is.Zero);
        }
        [TestCase(WeaponKind.Laser,.4f,7,9)] [TestCase(WeaponKind.Plasma,1f,16,22)] [TestCase(WeaponKind.Railgun,1.6f,22,30)]
        public void AllWeaponNumbers(WeaponKind kind,float cooldown,float energy,float heat)
        {
            var s=WeaponStatRules.Calculate(kind,17,-.2f,.6f);Assert.That(s.Damage,Is.EqualTo(17));Assert.That(s.Cooldown,Is.EqualTo(cooldown).Within(.0001));
            Assert.That(s.Energy,Is.EqualTo(energy));Assert.That(s.Heat,Is.EqualTo(heat));Assert.That(s.NominalDps,Is.EqualTo(17/cooldown).Within(.0001));
            Assert.That(WeaponStatRules.Calculate(kind,0,-100,.6f).Cooldown,Is.EqualTo(.1f));
        }
        [Test] public void LegacyKindPriorityAndFallback()
        {
            Assert.That(WeaponStatRules.EffectiveKind("w3",WeaponKind.Laser,true),Is.EqualTo(WeaponKind.Railgun));
            Assert.That(WeaponStatRules.EffectiveKind("w2",WeaponKind.Laser,false),Is.EqualTo(WeaponKind.Plasma));
            Assert.That(WeaponStatRules.EffectiveKind("custom",WeaponKind.Laser,true),Is.EqualTo(WeaponKind.Plasma));
            Assert.That(WeaponStatRules.EffectiveKind("w3",WeaponKind.Plasma,false),Is.EqualTo(WeaponKind.Plasma));
            var s=WeaponStatRules.Calculate(WeaponKind.Laser,.5f,0,.6f);Assert.That(s.Damage,Is.EqualTo(.5f));Assert.That(s.Cooldown,Is.EqualTo(.6f));
        }
        [Test] public void InitialSpendAndAtomicFailures()
        {
            var r=new Reactor();Assert.That(r.Energy,Is.EqualTo(100));Assert.That(r.Heat,Is.Zero);Assert.False(r.Overheated);
            Assert.False(r.TrySpend(10,90,false,out var transition));Assert.That(transition,Is.EqualTo(ReactorTransition.None));
            Assert.False(r.TrySpend(101,90,true,out _));Assert.That(r.Energy,Is.EqualTo(100));Assert.That(r.Heat,Is.Zero);
            Assert.True(r.TrySpend(100,9,true,out _));Assert.That(r.Energy,Is.Zero);Assert.That(r.Heat,Is.EqualTo(9));
            Assert.False(r.TrySpend(1,90,true,out _));Assert.That(r.Heat,Is.EqualTo(9));
            r.Tick(.5f,true);Assert.That(r.Energy,Is.EqualTo(6));Assert.That(r.Heat,Is.EqualTo(1.5f));
        }
        [Test] public void ExactThresholdsAndTransitionsOnlyOnce()
        {
            var r=new Reactor();Assert.True(r.TrySpend(10,100,true,out var transition));Assert.That(transition,Is.EqualTo(ReactorTransition.Overheated));
            Assert.That(r.Heat,Is.EqualTo(100));Assert.True(r.Overheated);Assert.False(r.TrySpend(0,1,true,out transition));Assert.That(transition,Is.EqualTo(ReactorTransition.None));
            Assert.That(r.Tick(4,true),Is.EqualTo(ReactorTransition.None));Assert.That(r.Heat,Is.EqualTo(40));Assert.True(r.Overheated);
            Assert.That(r.Tick(1f/3,true),Is.EqualTo(ReactorTransition.Recovered));Assert.That(r.Heat,Is.EqualTo(35).Within(.0001));Assert.False(r.Overheated);
            Assert.That(r.Tick(0,true),Is.EqualTo(ReactorTransition.None));Assert.True(r.TrySpend(0,65,true,out transition));Assert.That(transition,Is.EqualTo(ReactorTransition.Overheated));
        }
        [TestCase(0,80)] [TestCase(.25f,60)] [TestCase(.75f,20)]
        public void ResistanceChangesOnlyAddedHeat(float resistance,float expectedHeat)
        {
            var r=new Reactor(resistance:resistance);Assert.True(r.TrySpend(40,80,true,out _));Assert.That(r.Heat,Is.EqualTo(expectedHeat));
            r.Tick(.5f,true);Assert.That(r.Energy,Is.EqualTo(66));Assert.That(r.Heat,Is.EqualTo(expectedHeat-7.5f));
            Assert.That(Reactor.CalculateHeat(-1,resistance),Is.Zero);
        }
        [Test] public void ZeroNegativeAndDisallowedTimeDoNotAdvanceResources()
        {
            var r=new Reactor();r.TrySpend(40,80,true,out _);
            r.Tick(100,false);r.Tick(0,true);r.Tick(-1,true);Assert.That(r.Energy,Is.EqualTo(60));Assert.That(r.Heat,Is.EqualTo(80));
            r.Tick(100,true);Assert.That(r.Energy,Is.EqualTo(100));Assert.That(r.Heat,Is.Zero);
        }
        [Test] public void ConfigureKeepsDeficitHeatAndLatch_AndClampsTuning()
        {
            var r=new Reactor();r.TrySpend(40,100,true,out _);r.Configure(150,20,10,2);
            Assert.That(r.Energy,Is.EqualTo(110));Assert.That(r.Heat,Is.EqualTo(100));Assert.True(r.Overheated);Assert.That(r.HeatResistance,Is.EqualTo(.75f));
            r.Configure(50,20,10,.25f);Assert.That(r.Energy,Is.EqualTo(10));r.Tick(6.5f,true);Assert.That(r.Heat,Is.EqualTo(35));Assert.False(r.Overheated);
            r.Configure(-10,-1,-1,-1);Assert.That(r.Energy,Is.Zero);Assert.That(r.MaxEnergy,Is.Zero);Assert.That(r.Regeneration,Is.Zero);Assert.That(r.Cooling,Is.Zero);Assert.That(r.HeatResistance,Is.Zero);
        }
        [Test] public void OrderedTickAndSpendUseOnePoolWithoutPartialOverheatFailure()
        {
            var r=new Reactor(40,12,15,.25f);
            Assert.True(r.TrySpend(22,30,true,out _));Assert.True(r.TrySpend(16,22,true,out _));Assert.False(r.TrySpend(7,9,true,out _));
            Assert.That(r.Energy,Is.EqualTo(2));Assert.That(r.Heat,Is.EqualTo(39));
            r.Tick(1,true);Assert.True(r.TrySpend(7,9,true,out _));Assert.That(r.Energy,Is.EqualTo(7));Assert.That(r.Heat,Is.EqualTo(30.75f));
        }
    }
}