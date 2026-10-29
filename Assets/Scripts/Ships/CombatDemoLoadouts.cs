using System;
using System.Collections.Generic;
using Game.Core;

/// <summary>Detached demonstration data. No player ledger, persistence or implicit grants.</summary>
public static class CombatDemoLoadouts
{
    public enum Role { Interceptor=0, Artillery=1, Escort=2 }
    public static ShipLoadout Create(Role role,ShipDefinition definition,string instancePrefix)
    {
        string expected=role==Role.Interceptor?"sidewinder":"cobra_mk3";
        if(definition==null||definition.shipId!=expected)throw new ArgumentException("Wrong hull for demonstration role");
        var loadout=new ShipLoadout(expected){weaponSlots=Empty(definition.weaponSlots),shieldSlots=Empty(definition.shieldSlots),engineSlots=Empty(definition.engineSlots)};
        if(role==Role.Interceptor)
        {
            Put(loadout.weaponSlots,0,CombatArchetypeCatalog.PulseLaser,instancePrefix);
            Put(loadout.weaponSlots,1,CombatArchetypeCatalog.PulseLaser,instancePrefix);
            Put(loadout.engineSlots,0,CombatArchetypeCatalog.LightweightEngine,instancePrefix);
        }
        else if(role==Role.Artillery)
        {
            Put(loadout.weaponSlots,0,CombatArchetypeCatalog.Railgun,instancePrefix);
            Put(loadout.weaponSlots,1,CombatArchetypeCatalog.Plasma,instancePrefix);
            Put(loadout.shieldSlots,0,CombatArchetypeCatalog.HeavyShield,instancePrefix);
            Put(loadout.engineSlots,0,CombatArchetypeCatalog.Capacitor,instancePrefix);
        }
        else
        {
            Put(loadout.weaponSlots,0,CombatArchetypeCatalog.PulseLaser,instancePrefix);
            Put(loadout.weaponSlots,2,CombatArchetypeCatalog.LaserTurret,instancePrefix);
            Put(loadout.engineSlots,0,CombatArchetypeCatalog.ManeuverBlock,instancePrefix);
            Put(loadout.engineSlots,1,CombatArchetypeCatalog.Radiator,instancePrefix);
        }
        if(!WeaponMountRules.Validate(definition,loadout))throw new InvalidOperationException("Demonstration loadout does not fit existing hardpoints/capacities");
        return loadout;
    }
    private static List<FittingItem> Empty(int count)
    {var result=new List<FittingItem>();for(int i=0;i<count;i++)result.Add(null);return result;}
    private static void Put(List<FittingItem> slots,int index,string id,string prefix)
    {
        if(index>=slots.Count)throw new InvalidOperationException("Insufficient demonstration slot capacity");
        slots[index]=CombatStandardDefinitions.Create(id,prefix+":"+id+":"+index);
    }
}