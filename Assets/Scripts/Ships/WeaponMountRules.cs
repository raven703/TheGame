using System;
using System.Collections.Generic;
using UnityEngine;

public enum MountCompatibility { FixedOnly, TurretOnly, Universal }

[Serializable]
public class WeaponHardpoint
{
    public string id;
    public int slotIndex;
    public float localAxis;
    public MountCompatibility compatibility;
    public float sector = 60f;
}

/// <summary>Shared fitting, save migration and combat rules. Never infers mount type from damage.</summary>
public static class WeaponMountRules
{
    public static bool Accepts(MountCompatibility mount, WeaponMountType type)
    {
        return mount == MountCompatibility.Universal ||
            (mount == MountCompatibility.FixedOnly && type == WeaponMountType.Fixed) ||
            (mount == MountCompatibility.TurretOnly && type == WeaponMountType.Turret);
    }
    public static WeaponHardpoint Get(ShipDefinition definition, int slot)
    {
        if (definition != null && definition.hardpoints != null)
            foreach (var point in definition.hardpoints)
                if (point != null && point.slotIndex == slot) return point;
        return new WeaponHardpoint { id = (definition != null ? definition.shipId : "legacy") + ":weapon:" + slot,
            slotIndex = slot, compatibility = MountCompatibility.FixedOnly, sector = 60f };
    }
    public static bool Fits(ShipDefinition definition, int slot, FittingItem item)
    {
        return item == null || (item.allowedGroup == SlotGroup.Top_Weapons && slot >= 0 &&
            (definition == null || slot < definition.weaponSlots) && Accepts(Get(definition,slot).compatibility,item.mountType));
    }
    public static bool CanSwap(ShipDefinition definition, int a, FittingItem first, int b, FittingItem second)
    {
        return Fits(definition,b,first) && Fits(definition,a,second);
    }
    public static bool Validate(ShipDefinition definition, ShipLoadout loadout)
    {
        if (definition == null || loadout == null || loadout.shipId != definition.shipId) return false;
        if (loadout.weaponSlots.Count > definition.weaponSlots || loadout.shieldSlots.Count > definition.shieldSlots || loadout.engineSlots.Count > definition.engineSlots) return false;
        for (int i=0;i<loadout.weaponSlots.Count;i++) if (!Fits(definition,i,loadout.weaponSlots[i])) return false;
        return Groups(loadout.shieldSlots,SlotGroup.Center_Shields) && Groups(loadout.engineSlots,SlotGroup.Bottom_Engines);
    }
    private static bool Groups(List<FittingItem> items, SlotGroup group)
    {
        foreach(var item in items) if(item != null && item.allowedGroup != group) return false;
        return true;
    }
    // Active saves migrate in place. Presets must use Validate, never this repair path.
    public static int Migrate(ShipDefinition definition, ShipLoadout loadout)
    {
        if (definition == null || loadout == null) return 0;
        if(loadout.inventory==null)loadout.inventory=new List<FittingItem>();
        int moved=Repair(loadout.weaponSlots,definition.weaponSlots,loadout.inventory,(i,item)=>Fits(definition,i,item));
        moved+=Repair(loadout.shieldSlots,definition.shieldSlots,loadout.inventory,(i,item)=>item.allowedGroup==SlotGroup.Center_Shields);
        moved+=Repair(loadout.engineSlots,definition.engineSlots,loadout.inventory,(i,item)=>item.allowedGroup==SlotGroup.Bottom_Engines);
        loadout.formatVersion=3;
        return moved;
    }
    private static int Repair(List<FittingItem> slots,int capacity,List<FittingItem> inventory,Func<int,FittingItem,bool> fits)
    {
        int moved=0;
        for(int i=0;i<slots.Count;i++)
        {
            var item=slots[i];
            if(item==null || string.IsNullOrEmpty(item.id)){slots[i]=null;continue;}
            if(i>=capacity || !fits(i,item)){inventory.Add(item);slots[i]=null;moved++;}
        }
        if(slots.Count>capacity)slots.RemoveRange(capacity,slots.Count-capacity);
        while(slots.Count<capacity)slots.Add(null);
        return moved;
    }
}