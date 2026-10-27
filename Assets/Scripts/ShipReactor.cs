using UnityEngine;

/// <summary>Shared energy pool and heat budget for weapons and active abilities.</summary>
public class ShipReactor : MonoBehaviour
{
    public float maxEnergy = 100f;
    public float regeneration = 12f;
    public float cooling = 15f;
    public float Energy { get; private set; } = 100f;
    public float Heat { get; private set; }
    public bool Overheated { get; private set; }
    private void Awake() { Energy = maxEnergy; }
    private void Update()
    {
        if (CombatSession.IsPreparing) return;
        Energy = Mathf.Min(maxEnergy, Energy + regeneration * Time.deltaTime);
        Heat = Mathf.Max(0f, Heat - cooling * Time.deltaTime);
        if (Overheated && Heat <= 35f)
        {
            Overheated = false;
            CombatSession.Log($"{name}: реактор остыл");
        }
    }
    public bool TrySpend(float energy, float heat)
    {
        if (CombatSession.IsPreparing || Overheated || Energy < energy) return false;
        Energy -= energy;
        Heat = Mathf.Min(100f, Heat + heat);
        if (Heat >= 100f)
        {
            Overheated = true;
            CombatSession.Log($"{name}: ПЕРЕГРЕВ");
        }
        return true;
    }
}
