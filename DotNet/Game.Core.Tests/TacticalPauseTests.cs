using Game.Core;
using NUnit.Framework;
public class TacticalPauseTests
{
    [Test] public void ExactTempo_UniqueOrderedCommands_TargetSnapshot_AndDrainOnce()
    {
        var p=new TacticalPause();Assert.AreEqual(0,p.Toggle(1.75f));
        p.ToggleAbility(AbilityCommandKind.Emp,"a");p.ToggleAbility(AbilityCommandKind.Afterburner);
        p.ToggleAbility(AbilityCommandKind.ShieldBoost);p.ToggleAbility(AbilityCommandKind.Afterburner);
        Assert.AreEqual(2,p.Commands.Count);Assert.AreEqual(0,p.Drain().Length);
        Assert.AreEqual(1.75f,p.Toggle(0));var commands=p.Drain();
        Assert.AreEqual("a",commands[0].TargetId);Assert.AreEqual(AbilityCommandKind.ShieldBoost,commands[1].Kind);
        Assert.AreEqual(0,p.Drain().Length);
    }
}