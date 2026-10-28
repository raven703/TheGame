using NUnit.Framework;

namespace Game.Core.Tests
{
    public class WeaponCombatTests
    {
        private sealed class Random : ICombatRandom
        {
            public float Roll;
            public int Calls;
            public float Value() { Calls++;return Roll; }
            public int Index(int maximum) { Calls++;return 0; }
        }
        private WeaponCombat combat;
        private Reactor reactor;
        private DamageState target;
        private Random random;
        private WeaponStatValues stats;
        private int queries,notifications;
        private CoverState cover;
        [SetUp] public void Setup()
        {
            combat=new WeaponCombat();reactor=new Reactor();target=new DamageState(100);random=new Random();
            stats=WeaponStatRules.Calculate(WeaponKind.Laser,20,0,.6f);queries=notifications=0;cover=null;
        }
        private ShotResult Fire(string id="a",float time=0,bool allowed=true,bool occupied=true,bool operational=true,float accuracy=.85f,float evasion=.1f)
            => combat.TryFire(id,time,stats,allowed,occupied,operational,reactor,accuracy,evasion,random,
                ()=>{queries++;return cover;},target,CombatModule.Weapon,result=>notifications++);
        [Test] public void HitMissAndReloadEachSpendExactlyOnce()
        {
            random.Roll=.849f;var hit=Fire();Assert.That(hit.Outcome,Is.EqualTo(ShotOutcome.Ship));
            Assert.That(target.Shield,Is.EqualTo(30));Assert.That(reactor.Energy,Is.EqualTo(93));Assert.That(reactor.Heat,Is.EqualTo(9));
            Assert.That(combat.ReadyAt("a"),Is.EqualTo(.6f));Assert.That(notifications,Is.EqualTo(1));Assert.That(random.Calls,Is.EqualTo(1));
            var refused=Fire(time:.599f);Assert.That(refused.Refusal,Is.EqualTo(ShotRefusal.Reloading));
            Assert.That(queries,Is.EqualTo(1));Assert.That(random.Calls,Is.EqualTo(1));Assert.That(reactor.Energy,Is.EqualTo(93));
            random.Roll=.85f;var miss=Fire(time:.6f);Assert.That(miss.Outcome,Is.EqualTo(ShotOutcome.Miss));
            Assert.That(target.Shield,Is.EqualTo(30));Assert.That(reactor.Energy,Is.EqualTo(86));Assert.That(reactor.Heat,Is.EqualTo(18));Assert.That(combat.ReadyAt("a"),Is.EqualTo(1.2f).Within(.0001));
        }
        [Test] public void IndependentStableMountsKeepDeadlinesAcrossStatRefresh()
        {
            Fire("stable-left",10);Fire("stable-right",10.1f);
            stats=WeaponStatRules.Calculate(WeaponKind.Plasma,40,0,.6f);
            Assert.That(Fire("stable-left",10.5f).Refusal,Is.EqualTo(ShotRefusal.Reloading));
            Assert.True(Fire("stable-left",10.6f).Fired);
            Assert.That(Fire("stable-right",10.6f).Refusal,Is.EqualTo(ShotRefusal.Reloading));
            Assert.That(combat.ReadyAt("stable-right"),Is.EqualTo(10.7f).Within(.0001));
        }
        [TestCase(false,true,true,ShotRefusal.ExternalConditions)]
        [TestCase(true,false,true,ShotRefusal.EmptySlot)]
        [TestCase(true,true,false,ShotRefusal.WeaponDestroyed)]
        public void RefusalDoesNotQueryPhysicsConsumeRandomOrChangeState(bool allowed,bool occupied,bool operational,ShotRefusal reason)
        {
            var result=Fire(allowed:allowed,occupied:occupied,operational:operational);
            Assert.False(result.Fired);Assert.That(result.Refusal,Is.EqualTo(reason));Assert.That(reactor.Energy,Is.EqualTo(100));
            Assert.That(reactor.Heat,Is.Zero);Assert.That(combat.ReadyAt("a"),Is.Zero);Assert.That(queries,Is.Zero);Assert.That(random.Calls,Is.Zero);Assert.That(target.Shield,Is.EqualTo(50));
        }
        [Test] public void InsufficientEnergyAndOverheatLeaveReloadUntouched()
        {
            reactor.TrySpend(100,0,true,out _);Assert.That(Fire().Refusal,Is.EqualTo(ShotRefusal.InsufficientEnergy));
            reactor.Configure(200,12,15,0);reactor.TrySpend(0,100,true,out _);
            Assert.That(Fire().Refusal,Is.EqualTo(ShotRefusal.Overheated));Assert.That(combat.ReadyAt("a"),Is.Zero);
            Assert.That(queries,Is.Zero);Assert.That(random.Calls,Is.Zero);Assert.That(reactor.Energy,Is.EqualTo(100));
        }
        [Test] public void CoverConsumesResourcesWithoutHitRollAndReportsDestructionOnce()
        {
            cover=new CoverState(10);bool destroyed=false;
            var result=combat.TryFire("a",0,stats,true,true,true,reactor,.85f,.1f,random,()=>cover,target,null,
                coverNotification:(state,death)=>destroyed=death);
            Assert.That(result.Outcome,Is.EqualTo(ShotOutcome.Cover));Assert.That(result.CoverDamage,Is.EqualTo(20));
            Assert.True(destroyed);Assert.That(cover.HitPoints,Is.EqualTo(-10));Assert.That(target.Shield,Is.EqualTo(50));Assert.That(random.Calls,Is.Zero);
            Assert.True(result.CoverDestroyed);
            Assert.That(reactor.Energy,Is.EqualTo(93));Assert.That(reactor.Heat,Is.EqualTo(9));Assert.False(cover.Damage(20));
        }
        [TestCase(-1f,0f,0f,false)]
        [TestCase(2f,0f,1f,false)]
        [TestCase(2f,0f,.999f,true)]
        [TestCase(.5f,.6f,0f,false)]
        public void AccuracyClampsButUsesStrictLessThan(float accuracy,float evasion,float roll,bool expectedHit)
        {
            random.Roll=roll;var result=Fire(accuracy:accuracy,evasion:evasion);
            Assert.That(result.Outcome,Is.EqualTo(expectedHit?ShotOutcome.Ship:ShotOutcome.Miss));Assert.That(random.Calls,Is.EqualTo(1));
        }
    }
}