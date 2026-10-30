using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum AbilityCommandKind { Afterburner, ShieldBoost, Emp }
    public readonly struct AbilityCommand
    {
        public readonly AbilityCommandKind Kind;
        public readonly string TargetId;
        public AbilityCommand(AbilityCommandKind kind,string targetId=null) { Kind=kind;TargetId=targetId; }
    }
    /// <summary>Planning only: never executes effects or owns Unity objects.</summary>
    public sealed class TacticalPause
    {
        private readonly List<AbilityCommand> commands=new List<AbilityCommand>();
        public bool Paused { get; private set; }
        public float ResumeScale { get; private set; }=1f;
        public IReadOnlyList<AbilityCommand> Commands => commands.AsReadOnly();
        public float Toggle(float currentScale)
        {
            if(Paused){Paused=false;return ResumeScale;}
            if(currentScale<=0f)return currentScale;
            ResumeScale=currentScale;Paused=true;return 0f;
        }
        public void SetRunningScale(float scale) { Paused=false;ResumeScale=scale; }
        public void ToggleAbility(AbilityCommandKind kind,string targetId=null)
        {
            if(!Paused)return;
            int index=commands.FindIndex(c=>c.Kind==kind);
            if(index>=0)commands.RemoveAt(index);else commands.Add(new AbilityCommand(kind,targetId));
        }
        // Remove before dispatch: rejection, exception or next frame cannot retry an activation.
        public AbilityCommand[] Drain()
        {
            if(Paused)return Array.Empty<AbilityCommand>();
            var result=commands.ToArray();commands.Clear();return result;
        }
        public void Clear() => commands.Clear();
    }
}