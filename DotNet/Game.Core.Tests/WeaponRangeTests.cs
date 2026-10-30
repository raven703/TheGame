using System;
using Game.Core;
using NUnit.Framework;

public class WeaponRangeTests
{
    private static DistanceWeapon W(string id,bool turret=false)
    {
        Assert.IsTrue(CombatArchetypeCatalog.TryWeapon(id,out var stats));
        return new DistanceWeapon {DefinitionId=id,MountId=id,Turret=turret,Stats=stats};
    }
    [TestCase("w2",10)] [TestCase("w3",18)] [TestCase("w4",12)] [TestCase("combat-v1:laser-turret",10)]
    public void CatalogRange(string id,float expected)
    {Assert.IsTrue(CombatArchetypeCatalog.TryWeapon(id,out var stats));Assert.AreEqual(expected,stats.Range);}
    [Test] public void MissingRangeUsesHullFallback()
    {Assert.AreEqual(6,WeaponStatRules.EffectiveRange(0,6));Assert.AreEqual(18,WeaponStatRules.EffectiveRange(18,6));}
    [Test] public void RoleAndPlayerSelectionAreStableAndDifferent()
    {
        var guns=new[]{W("w2"),W("w3"),W("combat-v1:laser-turret",true),W("w4")};
        Assert.AreEqual(10,LoadoutCombatDistance.Range(guns,6)); // Plasma has greatest fixed nominal DPS.
        Assert.AreEqual(18,LoadoutCombatDistance.Range(guns,6,CombatRole.Artillery));
        Assert.AreEqual(10,LoadoutCombatDistance.Range(guns,6,CombatRole.Escort));
        Assert.AreEqual(12,LoadoutCombatDistance.Range(guns,6,CombatRole.Interceptor));
        Array.Reverse(guns);Assert.AreEqual(18,LoadoutCombatDistance.Range(guns,6,CombatRole.Artillery));
    }
    [Test] public void RailgunDoesNotExtendEmp()
    {Assert.IsTrue(AbilityRanges.EmpInRange(8));Assert.IsFalse(AbilityRanges.EmpInRange(8.01f));Assert.IsFalse(AbilityRanges.EmpInRange(18));}
    [Test] public void MixedMountsCheckRangeIndependently()
    {
        var aim=new MountGeometry();var origin=new AimPoint(0,0);var forward=new AimPoint(0,1);var target=new AimPoint(0,14);
        Assert.IsFalse(aim.Advance("plasma",origin,forward,target,10,60,WeaponMountType.Fixed,60,0,0,0,true).Ready);
        Assert.IsTrue(aim.Advance("rail",origin,forward,target,18,60,WeaponMountType.Fixed,60,0,0,0,true).Ready);
    }
}