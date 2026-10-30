using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    [Header("Ship Definition")]
    [Tooltip("ScriptableObject с базовыми ТТХ. Если назначен — maxHullHP/shield/mass берутся оттуда.")]
    public ShipDefinition shipDefinition;

    [Header("Fallback")]
    [Tooltip("Fallback max hull HP, если ShipDefinition не назначен.")]
    public float maxHullHP = 100f;

    public ShipData Data { get; private set; }

    public event Action OnDamageTaken;
    public event Action OnDestroyed;



    private void Awake()
    {
        float hull = shipDefinition != null ? shipDefinition.maxHullHP : maxHullHP;
        Data = new ShipData(hull);

        if (shipDefinition != null)
        {
            Data.Damage.InitializeMaximums(Data.maxHullHP, shipDefinition.baseShieldHP);
            Data.mass = shipDefinition.baseMass;
            Data.lockTime = shipDefinition.baseLockTime;
        }
        if (GetComponent<ShipReactor>() == null) gameObject.AddComponent<ShipReactor>();
        if(Application.isPlaying && GetComponent<ShieldArcs>()==null)gameObject.AddComponent<ShieldArcs>();
        if (shipDefinition != null)
            GetComponent<ShipReactor>().Configure(shipDefinition.baseMaxEnergy, shipDefinition.baseEnergyRegeneration,
                shipDefinition.baseCooling, shipDefinition.baseHeatResistance);
    }

    private void Update()
    {
        if(Data!=null && !CombatSession.IsPreparing)Data.Damage.TickShields(Time.deltaTime);
        if (Data == null || !Data.IsDestroyed)
            return;

        Die();
    }

    public void TakeWeaponDamage(float amount, WeaponKind kind, ModuleType? targetedModule = null)
    {
        if(Data==null)return;
        Data.Damage.WeaponDamage(amount,(Game.Core.WeaponKind)(int)kind,ToCore(targetedModule),UnityCombatRandom.Instance,
            !CombatSession.IsPreparing && Time.timeScale>0f,ReportDamage,ReportBypass);
    }
    public Game.Core.ShieldSide ShieldSideFromSource(Vector2 source)
    {
        var movement=GetComponent<ShipMovement>();
        Vector2 forward=movement!=null?movement.GetForwardVector():(Vector2)transform.up;
        Vector2 direction=source-(Vector2)transform.position;
        return Game.Core.DirectionalShield.Side(new Game.Core.AimPoint(forward.x,forward.y),new Game.Core.AimPoint(direction.x,direction.y));
    }
    public void TakeWeaponDamageFrom(float amount,WeaponKind kind,Vector2 source,ModuleType? module=null)
    {
        if(Data==null)return;
        Data.Damage.WeaponDamage(amount,(Game.Core.WeaponKind)(int)kind,ToCore(module),UnityCombatRandom.Instance,
            !CombatSession.IsPreparing && Time.timeScale>0f,ReportDamage,ReportBypass,ShieldSideFromSource(source));
    }
    private static Game.Core.CombatModule? ToCore(ModuleType? module) => module.HasValue ? (Game.Core.CombatModule?)(int)module.Value : null;
    public void TakeDamage(float amount, ModuleType? targetedModule = null)
    {
        if(Data==null)return;
        ReportDamage(Data.Damage.Damage(amount,ToCore(targetedModule),UnityCombatRandom.Instance,!CombatSession.IsPreparing && Time.timeScale>0f));
    }
    internal void ReportDamage(Game.Core.DamageResult result)
    {
        if(!result.Applied)return;
        if(result.ShieldOnly)CombatSession.Log($"{name}: щит поглотил {result.Absorbed:F0}");
        else
        {
            if(result.ModuleDestroyed)CombatSession.Log($"{name}: модуль {(ModuleType)(int)result.Module.Value} выбит");
            CombatSession.Log($"{name}: урон {result.Remaining:F0}, корпус {Data.currentHullHP:F0}");
        }
        OnDamageTaken?.Invoke();
    }
    internal void ReportBypass(float bypass)
    {
        CombatSession.Log($"{name}: пробитие корпуса {bypass:F0}");
        OnDamageTaken?.Invoke();
    }
    private void Die()
    {
        if(Data==null||!Data.Damage.TryReportDestruction())return;
        CombatSession.Log($"{name}: уничтожен");
        OnDestroyed?.Invoke();
        Debug.Log($"{gameObject.name} уничтожен!");
        gameObject.SetActive(false);
    }
}
