using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

public class ShipOrdersTests
{
    [Test] public void ExplicitFacingTargetPreservesRouteSpeedAndNavigationRevision()
    {
        var order=new ShipOrders();order.MoveTo(P(8,4));order.MoveTo(P(12,6),true);order.SetSpeedLimit(.5f);
        int revision=order.NavigationRevision;
        Assert.True(order.SetOrientationTarget("pending-a"));
        Assert.AreEqual(OrderMode.MoveToPoint,order.Mode);Assert.IsNull(order.TargetId);
        Assert.AreEqual(2,order.Waypoints.Count);Assert.AreEqual(8,order.Waypoints[0].X);
        Assert.AreEqual(.5f,order.SpeedLimit);Assert.AreEqual(revision,order.NavigationRevision);
        order.ValidateOrientationTarget(true);Assert.AreEqual("pending-a",order.OrientationTargetId);
        order.SetOrientationTarget("b");Assert.AreEqual("b",order.OrientationTargetId);
        order.ValidateOrientationTarget(false);Assert.AreEqual(OrderOrientation.Course,order.Orientation);
        Assert.AreEqual(2,order.Waypoints.Count);Assert.AreEqual(revision,order.NavigationRevision);
        order.SetOrientationTarget("c");order.MoveTo(P(16));Assert.IsNull(order.OrientationTargetId);
        Assert.AreEqual(OrderOrientation.Course,order.Orientation);
        order.SetOrientationTarget("d");order.Stop(P());Assert.IsNull(order.OrientationTargetId);Assert.True(order.IsBraking);
    }
    private static AimPoint P(float x=0,float y=0) => new AimPoint(x,y);
    private static OrderObservation O(float x=0,float y=0,float speed=0,bool available=true) =>
        new OrderObservation {Position=P(x,y),Velocity=P(speed),TargetAvailable=available,TargetForward=P(0,1),WeaponRange=10};
    [Test] public void FollowCursor_OnePoint_NoRevisionReset_ReleaseAndStopAreFinal()
    {
        var order=new ShipOrders();order.Tactical(OrderMode.Orbit,"enemy");order.BeginFollowCursor(P(5));
        int revision=order.NavigationRevision;
        for(int i=0;i<100;i++)order.UpdateFollowCursor(P(i));
        Assert.AreEqual(revision,order.NavigationRevision);Assert.AreEqual(1,order.Waypoints.Count);
        Assert.IsNull(order.TargetId);Assert.AreEqual(OrderOrientation.Course,order.Orientation);
        order.Evaluate(O(99,0),out _);Assert.AreEqual(OrderMode.FollowCursor,order.Mode);
        order.EndFollowCursor();Assert.AreEqual(OrderMode.MoveToPoint,order.Mode);
        order.UpdateFollowCursor(P(500));Assert.AreEqual(99,order.Destination.X);
        order.Stop(P());order.UpdateFollowCursor(P(500));Assert.AreEqual(OrderMode.HoldPosition,order.Mode);
    }
    [Test] public void InitialManualQueueStopAndNoAutomaticOrbit()
    {
        var order=new ShipOrders();Assert.AreEqual(OrderMode.HoldPosition,order.Mode);Assert.False(order.IsBraking);
        order.Evaluate(O(3,4),out var final);Assert.True(final);
        order.Tactical(OrderMode.Orbit,"enemy");order.MoveTo(P(2,3),true);order.MoveTo(P(4,5),true);
        Assert.IsNull(order.TargetId);Assert.AreEqual(2,order.Waypoints.Count);
        Assert.AreEqual(2,order.Waypoints[0].X);Assert.AreEqual(4,order.Waypoints[1].X);
        order.Evaluate(O(2,3),out final);Assert.True(final);Assert.AreEqual(1,order.Waypoints.Count);
        order.Stop(P(3,4));Assert.AreEqual(0,order.Waypoints.Count);Assert.True(order.IsBraking);
        order.Evaluate(O(4,4,1),out final);Assert.AreEqual(4,order.Destination.X);Assert.True(order.IsBraking);
        order.Evaluate(O(5,4,.079f),out final);Assert.False(order.IsBraking);
        var result=order.Evaluate(O(6,4),out final);Assert.AreEqual(5,result.X);Assert.AreEqual(OrderMode.HoldPosition,order.Mode);
    }
    [Test] public void LimitsOrientationAndTargetLossAreExplicit()
    {
        var order=new ShipOrders();order.SetSpeedLimit(-1);Assert.AreEqual(.25f,order.SpeedLimit);order.SetSpeedLimit(2);Assert.AreEqual(1,order.SpeedLimit);
        Assert.False(order.SetOrientation(OrderOrientation.OrderTarget,false));
        order.Tactical(OrderMode.EngageTarget,"a");Assert.AreEqual(OrderOrientation.OrderTarget,order.Orientation);
        Assert.True(order.SetOrientation(OrderOrientation.Course,true));Assert.AreEqual(OrderOrientation.Course,order.Orientation);
        Assert.AreEqual("a",order.TargetId,"weapon selection is not an input to this model");
        order.Evaluate(O(2,3,1,false),out _);Assert.IsNull(order.TargetId);Assert.True(order.IsBraking);Assert.AreEqual(OrderMode.HoldPosition,order.Mode);
    }
    [Test] public void FlankStrictThresholdsAndHysteresis()
    {
        var order=new ShipOrders {PreferredDistance=4};order.Tactical(OrderMode.Flank,"target");
        foreach(var sample in new[]{(0.7f,OrderOrientation.Course),(.64f,OrderOrientation.OrderTarget),(1f,OrderOrientation.OrderTarget),(1.41f,OrderOrientation.Course),(1f,OrderOrientation.Course)})
        {
            order.Evaluate(O(sample.Item1,-4),out _);Assert.AreEqual(sample.Item2,order.Orientation);
        }
    }
    [Test] public void FailurePreservesQueueUntilExplicitCommandAndSnapshotsCannotMutate()
    {
        var order=new ShipOrders();order.MoveTo(P(8));order.MoveTo(P(12),true);var snapshot=order.Waypoints;
        Assert.Throws<NotSupportedException>(()=>((IList<AimPoint>)snapshot)[0]=P(999));
        order.NavigationFailed(P(1));Assert.AreEqual(2,order.Waypoints.Count);Assert.NotNull(order.Error);Assert.True(order.IsBraking);
        order.Evaluate(O(2,0,1),out _);Assert.AreEqual(2,order.Waypoints.Count);Assert.NotNull(order.Error);
        order.MoveTo(P(5),true);Assert.AreEqual(1,order.Waypoints.Count);Assert.IsNull(order.Error);Assert.AreEqual(2,snapshot.Count);
    }
    [TestCase(PilotPersonality.Brawler,OrderMode.Orbit,4.5f)]
    [TestCase(PilotPersonality.Sniper,OrderMode.KeepDistance,8.5f)]
    [TestCase(PilotPersonality.Flanker,OrderMode.Flank,5f)]
    [TestCase(PilotPersonality.Cautious,OrderMode.Orbit,4.5f)]
    public void PersonalitiesAndRepeatedDecisionsDoNotResetNavigation(PilotPersonality personality,OrderMode mode,float distance)
    {
        var order=new ShipOrders();var decision=PilotDecisions.Decide(personality,true,true,100,100,10,4.5f);
        decision.Apply(order,"a",P());Assert.AreEqual(mode,order.Mode);Assert.AreEqual(distance,order.PreferredDistance);
        int revision=order.NavigationRevision;order.SetOrientation(OrderOrientation.Course,true);
        decision.Apply(order,"a",P());Assert.AreEqual(revision,order.NavigationRevision);Assert.AreEqual(OrderOrientation.Course,order.Orientation);
        decision.Apply(order,"b",P());Assert.AreEqual(revision+1,order.NavigationRevision);Assert.AreEqual("b",order.TargetId);
        PilotDecisions.Decide(personality,false,true,100,100,10,4.5f).Apply(order,null,P());Assert.AreEqual(OrderMode.HoldPosition,order.Mode);
    }
    [TestCase(35,OrderMode.Orbit,4.5f)] [TestCase(34.99f,OrderMode.KeepDistance,14f)]
    public void CautiousRetreatPreservesStrictThreshold(float hull,OrderMode mode,float distance)
    {
        var decision=PilotDecisions.Decide(PilotPersonality.Cautious,true,true,hull,100,10,4.5f);
        Assert.AreEqual(mode,decision.Mode);Assert.AreEqual(distance,decision.PreferredDistance,.0001f);
    }
    [Test] public void ArrivalIsStrictAndIntermediateLegsAdvanceOnlyOnce()
    {
        var order=new ShipOrders();order.MoveTo(P());order.MoveTo(P(.1f),true);order.MoveTo(P(10),true);
        order.Evaluate(O(),out var final);Assert.False(final);Assert.AreEqual(2,order.Waypoints.Count);
        order.MoveTo(P());order.Evaluate(O(.18f),out _);Assert.AreEqual(OrderMode.MoveToPoint,order.Mode);
        order.Evaluate(O(.17f,0,.12f),out _);Assert.AreEqual(OrderMode.MoveToPoint,order.Mode);
        order.Evaluate(O(.17f,0,.119f),out _);Assert.AreEqual(OrderMode.HoldPosition,order.Mode);
    }
}