using System;
using System.Collections.Generic;

namespace Game.Core
{
    public struct DistanceWeapon
    {
        public string DefinitionId, MountId;
        public bool Turret;
        public WeaponStatValues Stats;
    }
    /// <summary>One deterministic equipment policy shared by player orders and role observations.</summary>
    public static class LoadoutCombatDistance
    {
        private static bool Better(DistanceWeapon a,DistanceWeapon b)
        {
            int dps=a.Stats.NominalDps.CompareTo(b.Stats.NominalDps);
            if(dps!=0)return dps>0;
            int id=string.CompareOrdinal(a.DefinitionId,b.DefinitionId);
            return id<0 || id==0&&string.CompareOrdinal(a.MountId,b.MountId)<0;
        }
        public static float Range(IEnumerable<DistanceWeapon> weapons,float fallback,CombatRole? role=null)
        {
            DistanceWeapon best=default;bool found=false;int priority=-1;
            if(weapons!=null)foreach(var weapon in weapons)
            {
                if(weapon.Stats.Range<=0||string.IsNullOrEmpty(weapon.DefinitionId))continue;
                int rank=!weapon.Turret?1:0;
                if(role==CombatRole.Artillery)rank=weapon.Stats.Kind==WeaponKind.Railgun?3:rank;
                else if(role==CombatRole.Escort)rank=weapon.Turret?3:0;
                else if(role==CombatRole.Interceptor)rank=weapon.DefinitionId==CombatArchetypeCatalog.PulseLaser?3:rank;
                if(!found||rank>priority||rank==priority&&Better(weapon,best)){best=weapon;found=true;priority=rank;}
            }
            return found?best.Stats.Range:Math.Max(.01f,fallback);
        }
    }
    public static class AbilityRanges
    {
        public const float Emp=8f;
        public static bool EmpInRange(float distance)=>distance>=0&&distance<=Emp;
    }
}