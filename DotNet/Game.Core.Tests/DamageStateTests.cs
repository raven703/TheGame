using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class DamageStateTests
    {
        private sealed class Sequence : ICombatRandom
        {
            private readonly float value;
            private readonly int index;
            public int Values, Indices;
            public Sequence(float value=1f,int index=0) { this.value=value;this.index=index; }
            public float Value() { Values++;return value; }
            public int Index(int maximum) { Indices++;Assert.That(index,Is.LessThan(maximum));return index; }
        }
        [Test] public void ShieldThenModule_ExcessDiscardedThenHalfSpill()
        {
            var state=new DamageState(100);var random=new Sequence();
            var shield=state.Damage(20,CombatModule.Weapon,random);
            Assert.True(shield.ShieldOnly);Assert.That(state.Shield,Is.EqualTo(30));
            var hit=state.Damage(100,CombatModule.Weapon,random);
            Assert.True(hit.ModuleDestroyed);Assert.That(state.Module(CombatModule.Weapon).Current,Is.Zero);
            Assert.That(state.Hull,Is.EqualTo(100));Assert.That(state.Shield,Is.Zero);
            hit=state.Damage(20,CombatModule.Weapon,random);
            Assert.False(hit.ModuleDestroyed);Assert.That(hit.HullDamage,Is.EqualTo(10));Assert.That(state.Hull,Is.EqualTo(90));
            Assert.That(random.Values,Is.Zero);Assert.That(random.Indices,Is.Zero);
        }
        [Test] public void BrokenShieldDoesNotAbsorb_EngineRemovesEvasion_RepairRestoresIt()
        {
            var state=new DamageState(100);state.ModuleDamage(CombatModule.Shield,40);
            var hit=state.Damage(10,CombatModule.Engine,new Sequence());
            Assert.False(hit.ShieldOnly);Assert.That(state.Shield,Is.EqualTo(50));
            state.ModuleDamage(CombatModule.Engine,100);
            Assert.That(state.Evasion(20,.1f,.05f),Is.Zero);
            state.RepairModule(CombatModule.Engine,100);
            Assert.That(state.Evasion(20,.1f,.05f),Is.EqualTo(1.1f).Within(.0001));
        }
        [TestCase(.3999f,1,30f,100f)]
        [TestCase(.4f,0,40f,90f)]
        public void RandomComparisonIsStrict(float value,int expectedIndices,float shieldModule,float hull)
        {
            var state=new DamageState(100);state.SetShield(0);
            var random=new Sequence(value,1);state.Damage(10,null,random);
            Assert.That(random.Values,Is.EqualTo(1));Assert.That(random.Indices,Is.EqualTo(expectedIndices));
            Assert.That(state.Module(CombatModule.Shield).Current,Is.EqualTo(shieldModule));Assert.That(state.Hull,Is.EqualTo(hull));
        }
        [TestCase(WeaponKind.Laser,30f,100f,1)]
        [TestCase(WeaponKind.Plasma,20f,100f,1)]
        [TestCase(WeaponKind.Railgun,38f,92f,2)]
        public void SpecialWeaponsPreserveTwoPhaseNotifications(WeaponKind kind,float shield,float hull,int count)
        {
            var state=new DamageState(100);int notifications=0;
            var hit=state.WeaponDamage(20,kind,CombatModule.Weapon,new Sequence(),true,
                primary=>{notifications++;Assert.That(state.Hull,Is.EqualTo(100));Assert.True(primary.Applied);},
                bypass=>{notifications++;Assert.That(bypass,Is.EqualTo(8));Assert.That(state.Hull,Is.EqualTo(92));});
            Assert.That(state.Shield,Is.EqualTo(shield));Assert.That(state.Hull,Is.EqualTo(hull));Assert.That(notifications,Is.EqualTo(count));Assert.True(hit.Applied);
        }
        [Test] public void DestructionReportedOnlyOnce_RefusalsConsumeNoRandom()
        {
            var state=new DamageState(10);var random=new Sequence();
            Assert.False(state.Damage(10,null,random,false).Applied);
            Assert.False(state.Damage(0,null,random).Applied);
            state.HullDamage(100);Assert.That(state.Hull,Is.Zero);Assert.False(state.DestructionReported);
            Assert.False(state.Damage(10,null,random).Applied);Assert.That(random.Values,Is.Zero);
            Assert.True(state.TryReportDestruction());Assert.False(state.TryReportDestruction());
        }
        [Test] public void MaximumDifferenceBoostAndSnapshotsRemainIndependent()
        {
            var state=new DamageState(100);state.HullDamage(20);state.SetShield(10);
            var snapshot=state.Modules;state.ModuleDamage(CombatModule.Weapon,10);
            Assert.That(snapshot[0].Current,Is.EqualTo(30));Assert.That(state.Module(CombatModule.Weapon).Current,Is.EqualTo(20));
            state.ConfigureMaximums(120,60);Assert.That(state.Hull,Is.EqualTo(100));Assert.That(state.Shield,Is.EqualTo(20));
            state.BoostShield(30,10);Assert.That(state.Hull,Is.EqualTo(90));Assert.That(state.Shield,Is.EqualTo(50));
        }
    }
}