using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum ShotOutcome { Refused, Miss, Ship, Cover }
    public enum ShotRefusal { None, ExternalConditions, EmptySlot, WeaponDestroyed, Reloading, MissingReactor, Overheated, InsufficientEnergy }
    public readonly struct ShotResult
    {
        public readonly ShotOutcome Outcome;
        public readonly ShotRefusal Refusal;
        public readonly DamageResult Damage;
        public readonly float CoverDamage, ReadyAt;
        public readonly bool CoverDestroyed;
        public bool Fired => Outcome!=ShotOutcome.Refused;
        public ShotResult(ShotOutcome outcome,ShotRefusal refusal=ShotRefusal.None,DamageResult damage=default,float coverDamage=0f,float readyAt=0f,bool coverDestroyed=false)
        { Outcome=outcome;Refusal=refusal;Damage=damage;CoverDamage=coverDamage;ReadyAt=readyAt;CoverDestroyed=coverDestroyed; }
    }
    public sealed class CoverState
    {
        public float HitPoints { get; private set; }
        public bool DestructionReported { get; private set; }
        public CoverState(float hitPoints) { HitPoints=hitPoints; }
        public void SetHitPoints(float value) { HitPoints=value; }
        public bool Damage(float amount)
        {
            HitPoints-=Math.Max(0f,amount);
            if(HitPoints>0f||DestructionReported)return false;
            DestructionReported=true;return true;
        }
    }
    /// <summary>Per owning ship, stable hardpoint deadlines in explicitly supplied game seconds.</summary>
    public sealed class WeaponCombat
    {
        private readonly Dictionary<string,float> readyAt=new Dictionary<string,float>();
        public float ReadyAt(string mountId) => readyAt.TryGetValue(mountId,out var value)?value:0f;
        public static float HitChance(float accuracy,float evasion,WeaponKind kind) => StatMath.Clamp(
            accuracy+(kind==WeaponKind.Laser?.1f:kind==WeaponKind.Plasma?-.1f:0f)-evasion,0f,1f);
        public ShotResult TryFire(string mountId,float gameSeconds,WeaponStatValues stats,bool externalAllowed,
            bool occupied,bool weaponOperational,Reactor reactor,float accuracy,float evasion,ICombatRandom random,
            Func<CoverState> queryCover,DamageState target,CombatModule? selected,
            Action<DamageResult> primaryNotification=null,Action<float> bypassNotification=null,Action<ReactorTransition> reactorNotification=null,
            Action<CoverState,bool> coverNotification=null)
        {
            ShotRefusal reason= !externalAllowed?ShotRefusal.ExternalConditions:!occupied?ShotRefusal.EmptySlot:
                !weaponOperational?ShotRefusal.WeaponDestroyed:gameSeconds<ReadyAt(mountId)?ShotRefusal.Reloading:
                reactor==null?ShotRefusal.MissingReactor:reactor.Overheated?ShotRefusal.Overheated:
                reactor.Energy<stats.Energy?ShotRefusal.InsufficientEnergy:ShotRefusal.None;
            if(reason!=ShotRefusal.None)return new ShotResult(ShotOutcome.Refused,reason,readyAt:ReadyAt(mountId));
            if(!reactor.TrySpend(stats.Energy,stats.Heat,true,out var transition))return new ShotResult(ShotOutcome.Refused,ShotRefusal.InsufficientEnergy);
            reactorNotification?.Invoke(transition);
            readyAt[mountId]=gameSeconds+stats.Cooldown;
            // Physics remains external and executes only after a committed shot, before random rolls.
            var cover=queryCover?.Invoke();
            if(cover!=null)
            {
                bool destroyed=cover.Damage(stats.Damage);coverNotification?.Invoke(cover,destroyed);
                return new ShotResult(ShotOutcome.Cover,coverDamage:stats.Damage,readyAt:ReadyAt(mountId),coverDestroyed:destroyed);
            }
            if(target!=null&&random.Value()<HitChance(accuracy,evasion,stats.Kind))
            {
                var damage=target.WeaponDamage(stats.Damage,stats.Kind,selected,random,true,primaryNotification,bypassNotification);
                return new ShotResult(ShotOutcome.Ship,damage:damage,readyAt:ReadyAt(mountId));
            }
            return new ShotResult(ShotOutcome.Miss,readyAt:ReadyAt(mountId));
        }
    }
}