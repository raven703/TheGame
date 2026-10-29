using Game.Core;
using NUnit.Framework;

public class CombatRolePilotTests
{
    private static CombatRoleObservation Observation()=>new CombatRoleObservation {TargetAvailable=true,EngineAvailable=true,WeaponAvailable=true,
        Position=new AimPoint(0,8),TargetPosition=new AimPoint(0,0),Forward=new AimPoint(0,-1),Range=8,
        EnergyFraction=1,Energy=120,MaxEnergy=120,Hull=120,HullFraction=1,ShieldFraction=1};
    [Test] public void InterceptorCyclesRatherThanOrbitingWithFixedGuns()
    {
        var pilot=new CombatRolePilot();var o=Observation();
        Assert.AreEqual(CombatManeuver.Approach,pilot.Decide(CombatRole.Interceptor,"target",o,0).Maneuver);
        Assert.AreEqual(CombatManeuver.Attack,pilot.Decide(CombatRole.Interceptor,"target",o,1.6f).Maneuver);
        o.Position=new AimPoint(0,1);
        Assert.AreEqual(CombatManeuver.Exit,pilot.Decide(CombatRole.Interceptor,"target",o,.02f).Maneuver);
        o.Position=new AimPoint(0,10);
        Assert.AreEqual(CombatManeuver.Approach,pilot.Decide(CombatRole.Interceptor,"target",o,1.6f).Maneuver);
    }
    [Test] public void ArtilleryRecoveryHasSeparateEnterAndExitThresholds()
    {
        var pilot=new CombatRolePilot();var o=Observation();
        Assert.AreEqual(.8f*o.Range,pilot.Decide(CombatRole.Artillery,"t",o,0).Order.PreferredDistance);
        o.EnergyFraction=.19f;Assert.AreEqual(CombatManeuver.Recover,pilot.Decide(CombatRole.Artillery,"t",o,.02f).Maneuver);
        o.EnergyFraction=.4f;o.HeatFraction=.2f;Assert.AreEqual(CombatManeuver.Recover,pilot.Decide(CombatRole.Artillery,"t",o,2).Maneuver);
        o.EnergyFraction=.6f;o.HeatFraction=.5f;Assert.AreEqual(CombatManeuver.Recover,pilot.Decide(CombatRole.Artillery,"t",o,2).Maneuver);
        o.HeatFraction=.44f;Assert.AreEqual(CombatManeuver.Attack,pilot.Decide(CombatRole.Artillery,"t",o,2).Maneuver);
    }
    [Test] public void EscortCreatesPeriodicFixedWeaponWindow()
    {
        var pilot=new CombatRolePilot();var o=Observation();
        Assert.AreEqual(OrderMode.Orbit,pilot.Decide(CombatRole.Escort,"t",o,0).Order.Mode);
        Assert.AreEqual(OrderMode.KeepDistance,pilot.Decide(CombatRole.Escort,"t",o,1.6f).Order.Mode);
        Assert.AreEqual(OrderMode.Orbit,pilot.Decide(CombatRole.Escort,"t",o,1.6f).Order.Mode);
    }
    [Test] public void RepeatedDecisionDoesNotResetOrdersAndBlockedFireRepositionsOnce()
    {
        var pilot=new CombatRolePilot();var o=Observation();var orders=new ShipOrders();
        var decision=pilot.Decide(CombatRole.Artillery,"t",o,0);decision.Order.Apply(orders,"t",o.Position);int revision=orders.NavigationRevision;
        for(int i=0;i<10;i++){decision=pilot.Decide(CombatRole.Artillery,"t",o,.02f);Assert.False(decision.NewCommand);decision.Order.Apply(orders,"t",o.Position);}
        Assert.AreEqual(revision,orders.NavigationRevision);
        o.LineBlocked=true;decision=pilot.Decide(CombatRole.Artillery,"t",o,1.6f);
        Assert.AreEqual(CombatManeuver.Reposition,decision.Maneuver);Assert.True(decision.NewCommand);Assert.False(decision.AllowFire);
        Assert.False(pilot.Decide(CombatRole.Artillery,"t",o,.02f).NewCommand);
    }
    [Test] public void AbilitiesRequirePaidResourcesPublicSuppressionAndSurvivableShieldCost()
    {
        var o=Observation();o.BoostReady=o.ShieldReady=o.EmpReady=true;o.ShieldFraction=.2f;
        var pilot=new CombatRolePilot();var d=pilot.Decide(CombatRole.Interceptor,"t",o,0);Assert.True(d.Boost&&d.Shield&&d.Emp);
        o.Energy=40;o.Hull=10;o.TargetSuppressed=true;d=pilot.Decide(CombatRole.Interceptor,"t",o,.02f);Assert.False(d.Boost||d.Shield||d.Emp);
        o.EngineAvailable=false;o.WeaponAvailable=false;d=pilot.Decide(CombatRole.Interceptor,"t",o,.02f);
        Assert.AreEqual(CombatManeuver.Disabled,d.Maneuver);Assert.False(d.Boost||d.AllowFire);
    }
}