using UnityEngine;

/// <summary>Unity lifecycle/time/log adapter. Core alone owns resources and overheat.</summary>
public class ShipReactor : MonoBehaviour
{
    public float maxEnergy = 100f;
    public float regeneration = 12f;
    public float cooling = 15f;
    [SerializeField, Range(0f, .75f)] private float heatResistance;
    private Game.Core.Reactor model;
    private Game.Core.Reactor Model
    {
        get
        {
            if(model==null)model=new Game.Core.Reactor(maxEnergy,regeneration,cooling,heatResistance);
            return model;
        }
    }
    public float HeatResistance
    {
        get => Model.HeatResistance;
        set { Model.HeatResistance=value;heatResistance=Model.HeatResistance; }
    }
    public const float OverheatThreshold = Game.Core.Reactor.OverheatThreshold;
    public const float RecoveryThreshold = Game.Core.Reactor.RecoveryThreshold;
    public static float CalculateHeat(float heat,float resistance) => Game.Core.Reactor.CalculateHeat(heat,resistance);
    public float Energy => Model.Energy;
    public float Heat => Model.Heat;
    public bool Overheated => Model.Overheated;
    private void Awake() { var initialized=Model; }
    private void SyncTuning() => Model.SetTuning(maxEnergy,regeneration,cooling,heatResistance);
    internal Game.Core.Reactor CombatModel { get { SyncTuning(); return Model; } }
    internal void ReportCombatTransition(Game.Core.ReactorTransition transition) => Report(transition);
    public void Configure(float capacity,float recovery,float coolingRate,float resistance)
    {
        SyncTuning();Model.Configure(capacity,recovery,coolingRate,resistance);
        maxEnergy=Model.MaxEnergy;regeneration=Model.Regeneration;cooling=Model.Cooling;heatResistance=Model.HeatResistance;
    }
    private void Update() { Tick(Time.deltaTime); }
    public void Tick(float deltaTime)
    {
        SyncTuning();Report(Model.Tick(deltaTime,!CombatSession.IsPreparing));
    }
    public bool TrySpend(float energy,float heat)
    {
        SyncTuning();bool success=Model.TrySpend(energy,heat,!CombatSession.IsPreparing,out var transition);
        Report(transition);return success;
    }
    private void Report(Game.Core.ReactorTransition transition)
    {
        if(transition==Game.Core.ReactorTransition.Overheated)CombatSession.Log($"{name}: ПЕРЕГРЕВ");
        else if(transition==Game.Core.ReactorTransition.Recovered)CombatSession.Log($"{name}: реактор остыл");
    }
}
