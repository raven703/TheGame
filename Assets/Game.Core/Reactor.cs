using System;

namespace Game.Core
{
    public enum ReactorTransition { None, Overheated, Recovered }

    /// <summary>Sole mutable owner of reactor resources. Call order and game seconds come from the adapter.</summary>
    public sealed class Reactor
    {
        public const float OverheatThreshold=100f, RecoveryThreshold=35f;
        public float Energy { get; private set; }
        public float Heat { get; private set; }
        public bool Overheated { get; private set; }
        public float MaxEnergy { get; private set; }
        public float Regeneration { get; private set; }
        public float Cooling { get; private set; }
        private float resistance;
        public float HeatResistance { get => StatMath.Clamp(resistance,0f,.75f); set => resistance=StatMath.Clamp(value,0f,.75f); }
        public Reactor(float capacity=100f,float regeneration=12f,float cooling=15f,float resistance=0f)
        { MaxEnergy=capacity;Energy=capacity;Regeneration=regeneration;Cooling=cooling;HeatResistance=resistance; }
        public static float CalculateHeat(float heat,float resistance) => Math.Max(0f,heat)*(1f-StatMath.Clamp(resistance,0f,.75f));
        public void Configure(float capacity,float recovery,float coolingRate,float resistance)
        {
            float delta=capacity-MaxEnergy;
            MaxEnergy=Math.Max(0f,capacity);Energy=StatMath.Clamp(Energy+delta,0f,MaxEnergy);
            Regeneration=Math.Max(0f,recovery);Cooling=Math.Max(0f,coolingRate);HeatResistance=resistance;
        }
        // Serialized facade tuning can change without capacity-delta refill (legacy field semantics).
        public void SetTuning(float capacity,float recovery,float coolingRate,float resistance)
        { MaxEnergy=capacity;Regeneration=recovery;Cooling=coolingRate;HeatResistance=resistance; }
        public ReactorTransition Tick(float deltaSeconds,bool allowed)
        {
            if(!allowed)return ReactorTransition.None;
            deltaSeconds=Math.Max(0f,deltaSeconds);
            Energy=Math.Min(MaxEnergy,Energy+Regeneration*deltaSeconds);
            Heat=Math.Max(0f,Heat-Cooling*deltaSeconds);
            if(Overheated&&Heat<=RecoveryThreshold){Overheated=false;return ReactorTransition.Recovered;}
            return ReactorTransition.None;
        }
        public bool TrySpend(float energy,float heat,bool allowed,out ReactorTransition transition)
        {
            transition=ReactorTransition.None;
            if(!allowed||Overheated||Energy<energy)return false;
            Energy-=energy;Heat=Math.Min(OverheatThreshold,Heat+CalculateHeat(heat,HeatResistance));
            if(Heat>=OverheatThreshold){Overheated=true;transition=ReactorTransition.Overheated;}
            return true;
        }
    }
}