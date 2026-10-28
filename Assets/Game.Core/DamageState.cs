using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game.Core
{
    public enum CombatModule { Weapon, Shield, Engine }
    public interface ICombatRandom
    {
        float Value();
        int Index(int exclusiveMaximum);
    }
    public readonly struct ModuleSnapshot
    {
        public readonly CombatModule Type;
        public readonly float Maximum, Current;
        public bool Destroyed => Current <= 0f;
        public ModuleSnapshot(CombatModule type, float maximum, float current) { Type=type;Maximum=maximum;Current=current; }
    }
    public readonly struct DamageResult
    {
        public readonly bool Applied, ShieldOnly, ModuleDestroyed, HullDestroyed;
        public readonly CombatModule? Module;
        public readonly float Remaining, HullDamage, Absorbed;
        public DamageResult(bool applied, bool shieldOnly, CombatModule? module, bool moduleDestroyed,
            float remaining, float hullDamage, bool hullDestroyed, float absorbed=0f)
        { Applied=applied;ShieldOnly=shieldOnly;Module=module;ModuleDestroyed=moduleDestroyed;Remaining=remaining;HullDamage=hullDamage;HullDestroyed=hullDestroyed;Absorbed=absorbed; }
    }
    /// <summary>Authoritative HP and module owner. Snapshots never expose mutable internals.</summary>
    public sealed class DamageState
    {
        public float MaxHull { get; private set; }
        public float Hull { get; private set; }
        public float MaxShield { get; private set; } = 50f;
        public float Shield { get; private set; } = 50f;
        public bool Destroyed => Hull<=0f;
        public bool DestructionReported { get; private set; }
        private readonly float[] maxima={30f,40f,30f};
        private readonly float[] current={30f,40f,30f};
        public DamageState(float hull) { MaxHull=Math.Max(0f,hull);Hull=MaxHull; }
        public ModuleSnapshot Module(CombatModule type) => new ModuleSnapshot(type,maxima[(int)type],current[(int)type]);
        public IReadOnlyList<ModuleSnapshot> Modules => new ReadOnlyCollection<ModuleSnapshot>(new[]{Module(CombatModule.Weapon),Module(CombatModule.Shield),Module(CombatModule.Engine)});
        // Compatibility setters for old setup/fixture APIs; storage remains exclusively here.
        public void SetHull(float value) { Hull=value; }
        public void SetShield(float value) { Shield=value; }
        public void SetMaxHull(float value) { MaxHull=value; }
        public void SetMaxShield(float value) { MaxShield=value; }
        public void ConfigureMaximums(float hull,float shield)
        {
            Hull=StatMath.Clamp(Hull+hull-MaxHull,0f,hull);
            Shield=StatMath.Clamp(Shield+shield-MaxShield,0f,shield);
            MaxHull=hull;MaxShield=shield;
        }
        public void InitializeMaximums(float hull,float shield) { MaxHull=hull;Hull=hull;MaxShield=shield;Shield=shield; }
        public void HullDamage(float amount) { if(amount>0f)Hull=Math.Max(0f,Hull-amount); }
        public void ModuleDamage(CombatModule type,float amount) { if(amount>0f)current[(int)type]=Math.Max(0f,current[(int)type]-amount); }
        public void InitializeModule(CombatModule type,float maximum) { maxima[(int)type]=Math.Max(0f,maximum);current[(int)type]=maxima[(int)type]; }
        public void RepairModule(CombatModule type,float amount) { if(amount>0f)current[(int)type]=Math.Min(maxima[(int)type],current[(int)type]+amount); }
        public void BoostShield(float amount,float hullCost) { Shield=Math.Min(MaxShield,Shield+amount);HullDamage(hullCost); }
        public float Evasion(float speed,float basis,float multiplier) => Module(CombatModule.Engine).Destroyed?0f:basis+speed*multiplier;
        public float Absorb(float damage)
        {
            if(Module(CombatModule.Shield).Destroyed||Shield<=0f)return damage;
            if(Shield>=damage){Shield-=damage;return 0f;}
            float excess=damage-Shield;Shield=0f;return excess;
        }
        public DamageResult Damage(float amount,CombatModule? selected,ICombatRandom random,bool allowed=true)
        {
            if(!allowed||Destroyed||amount<=0f)return default;
            float remaining=Absorb(amount);
            if(remaining<=0f)return new DamageResult(true,true,null,false,remaining,0f,Destroyed,amount);
            CombatModule? module=selected;
            if(!module.HasValue&&random.Value()<.4f)module=(CombatModule)random.Index(3);
            float before=Hull;bool newlyDestroyed=false;
            if(module.HasValue)
            {
                int index=(int)module.Value;
                if(index<0||index>=current.Length)HullDamage(remaining);
                else
                {
                    bool wasDestroyed=current[index]<=0f;
                    ModuleDamage(module.Value,remaining);
                    if(wasDestroyed)HullDamage(remaining*.5f);
                    newlyDestroyed=!wasDestroyed&&current[index]<=0f;
                }
            }
            else HullDamage(remaining);
            return new DamageResult(true,false,module,newlyDestroyed,remaining,before-Hull,Destroyed);
        }
        // Preserve the historical observable two-phase railgun notification order.
        public DamageResult WeaponDamage(float amount,WeaponKind kind,CombatModule? selected,ICombatRandom random,
            bool allowed,Action<DamageResult> primaryNotification=null,Action<float> bypassNotification=null)
        {
            if(!allowed||Destroyed||amount<=0f)return default;
            if(kind==WeaponKind.Plasma&&!Module(CombatModule.Shield).Destroyed)Shield-=Math.Min(Shield,amount*.5f);
            float bypass=kind==WeaponKind.Railgun?amount*.4f:0f;
            var result=Damage(amount-bypass,selected,random,allowed);
            if(result.Applied)primaryNotification?.Invoke(result);
            if(bypass>0f){HullDamage(bypass);bypassNotification?.Invoke(bypass);}
            return new DamageResult(result.Applied,result.ShieldOnly,result.Module,result.ModuleDestroyed,result.Remaining,result.HullDamage+bypass,Destroyed);
        }
        public bool TryReportDestruction()
        {
            if(!Destroyed||DestructionReported)return false;
            DestructionReported=true;return true;
        }
    }
}