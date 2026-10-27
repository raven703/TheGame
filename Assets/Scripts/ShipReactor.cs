using UnityEngine;

/// <summary>Shared energy pool and heat budget for weapons and active abilities.</summary>
public class ShipReactor : MonoBehaviour
{
    public float maxEnergy = 100f;
    public float regeneration = 12f;
    public float cooling = 15f;
    [SerializeField, Range(0f, .75f)] private float heatResistance;
    public float HeatResistance { get => Mathf.Clamp(heatResistance, 0f, .75f); set => heatResistance = Mathf.Clamp(value, 0f, .75f); }
    public const float OverheatThreshold = 100f;
    public const float RecoveryThreshold = 35f;
    public static float CalculateHeat(float heat, float resistance) => Mathf.Max(0f, heat) * (1f - Mathf.Clamp(resistance, 0f, .75f));
    public void Configure(float capacity, float recovery, float coolingRate, float resistance)
    {
        float delta = capacity - maxEnergy;
        maxEnergy = Mathf.Max(0f, capacity);
        Energy = Mathf.Clamp(Energy + delta, 0f, maxEnergy);
        regeneration = Mathf.Max(0f, recovery);
        cooling = Mathf.Max(0f, coolingRate);
        HeatResistance = resistance;
    }
    public float Energy { get; private set; } = 100f;
    public float Heat { get; private set; }
    public bool Overheated { get; private set; }
    private void Awake() { Energy = maxEnergy; }
    private void Update()
    {
        Tick(Time.deltaTime);
    }
    public void Tick(float deltaTime)
    {
        if (CombatSession.IsPreparing) return;
        deltaTime = Mathf.Max(0f, deltaTime);
        Energy = Mathf.Min(maxEnergy, Energy + regeneration * deltaTime);
        Heat = Mathf.Max(0f, Heat - cooling * deltaTime);
        if (Overheated && Heat <= RecoveryThreshold)
        {
            Overheated = false;
            CombatSession.Log($"{name}: реактор остыл");
        }
    }
    public bool TrySpend(float energy, float heat)
    {
        if (CombatSession.IsPreparing || Overheated || Energy < energy) return false;
        Energy -= energy;
        Heat = Mathf.Min(OverheatThreshold, Heat + CalculateHeat(heat, HeatResistance));
        if (Heat >= OverheatThreshold)
        {
            Overheated = true;
            CombatSession.Log($"{name}: ПЕРЕГРЕВ");
        }
        return true;
    }
}
