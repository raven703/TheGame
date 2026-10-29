using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CombatDemoLoadoutTests
{
    [TestCase(0,"Assets/Ships/Sidewinder.asset",2,1)]
    [TestCase(1,"Assets/Ships/CobraMK3.asset",2,1)]
    [TestCase(2,"Assets/Ships/CobraMK3.asset",2,2)]
    public void BuildsUseExistingCapacitiesAndDistinctOwnedIdentities(int role,string path,int weapons,int engines)
    {
        var type=Type.GetType("CombatDemoLoadouts, Assembly-CSharp",true);
        var roleType=type.GetNestedType("Role");var definition=AssetDatabase.LoadMainAssetAtPath(path);
        var loadout=type.GetMethod("Create").Invoke(null,new object[]{Enum.ToObject(roleType,role),definition,"fixture"});
        var weaponSlots=(IList)loadout.GetType().GetField("weaponSlots").GetValue(loadout);
        var engineSlots=(IList)loadout.GetType().GetField("engineSlots").GetValue(loadout);
        int count=0;var ids=new System.Collections.Generic.HashSet<string>();
        foreach(var item in weaponSlots)if(item!=null){count++;Assert.True(ids.Add((string)item.GetType().GetField("instanceId").GetValue(item)));}
        Assert.AreEqual(weapons,count);count=0;foreach(var item in engineSlots)if(item!=null)count++;Assert.AreEqual(engines,count);
        if(role==2)Assert.AreEqual(1,(int)weaponSlots[2].GetType().GetField("mountType").GetValue(weaponSlots[2]));
    }
}