using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    private const float ModuleHitChance = 0.4f;

    private static readonly ModuleType[] AllModuleTypes =
    {
        ModuleType.Weapon,
        ModuleType.Shield,
        ModuleType.Engine
    };

    [Header("Ship Definition")]
    [Tooltip("ScriptableObject с базовыми ТТХ. Если назначен — maxHullHP/shield/mass берутся оттуда.")]
    public ShipDefinition shipDefinition;

    [Header("Fallback")]
    [Tooltip("Fallback max hull HP, если ShipDefinition не назначен.")]
    public float maxHullHP = 100f;

    public ShipData Data { get; private set; }

    public event Action OnDamageTaken;
    public event Action OnDestroyed;

    private bool isDead;

    private void Awake()
    {
        float hull = shipDefinition != null ? shipDefinition.maxHullHP : maxHullHP;
        Data = new ShipData(hull);

        if (shipDefinition != null)
        {
            Data.maxShieldHP = shipDefinition.baseShieldHP;
            Data.currentShieldHP = shipDefinition.baseShieldHP;
            Data.mass = shipDefinition.baseMass;
            Data.lockTime = shipDefinition.baseLockTime;
        }
        if (GetComponent<ShipReactor>() == null) gameObject.AddComponent<ShipReactor>();
    }

    private void Update()
    {
        if (Data == null || !Data.IsDestroyed)
            return;

        Die();
    }

    public void TakeWeaponDamage(float amount, WeaponKind kind, ModuleType? targetedModule = null)
    {
        if (CombatSession.IsPreparing || Data == null || Data.IsDestroyed || amount <= 0f) return;
        if (kind == WeaponKind.Plasma)
        {
            var shield = Data.GetModule(ModuleType.Shield);
            if (shield != null && !shield.isDestroyed)
                Data.currentShieldHP -= Mathf.Min(Data.currentShieldHP, amount * .5f);
        }
        float bypass = kind == WeaponKind.Railgun ? amount * .4f : 0f;
        TakeDamage(amount - bypass, targetedModule);
        if (bypass > 0f)
        {
            Data.TakeHullDamage(bypass);
            CombatSession.Log($"{name}: пробитие корпуса {bypass:F0}");
            OnDamageTaken?.Invoke();
        }
    }

    public void TakeDamage(float amount, ModuleType? targetedModule = null)
    {
        if (CombatSession.IsPreparing || Data == null || Data.IsDestroyed || amount <= 0f)
            return;

        float remainingDamage = Data.AbsorbDamageWithShield(amount);

        if (remainingDamage <= 0f)
        {
            CombatSession.Log($"{name}: щит поглотил {amount:F0}");
            OnDamageTaken?.Invoke();
            return;
        }

        if (targetedModule.HasValue)
        {
            ApplyModuleDamage(remainingDamage, targetedModule.Value);
        }
        else if (UnityEngine.Random.value < ModuleHitChance)
        {
            ApplyModuleDamage(remainingDamage, AllModuleTypes[UnityEngine.Random.Range(0, AllModuleTypes.Length)]);
        }
        else
        {
            ApplyHullDamage(remainingDamage);
        }

        CombatSession.Log($"{name}: урон {remainingDamage:F0}, корпус {Data.currentHullHP:F0}");
        OnDamageTaken?.Invoke();
    }

    private void ApplyModuleDamage(float amount, ModuleType moduleType)
    {
        var module = Data.GetModule(moduleType);
        if (module == null)
        {
            ApplyHullDamage(amount);
            return;
        }

        var wasAlreadyDestroyed = module.isDestroyed;
        Data.TakeModuleDamage(moduleType, amount);

        if (wasAlreadyDestroyed)
        {
            float spilloverDamage = amount * 0.5f;
            Data.TakeHullDamage(spilloverDamage);
            Debug.Log($"{gameObject.name}: Попадание в выбитый модуль {moduleType}! {spilloverDamage} урона прошло в корпус.");
        }
        else
        {
            if (module.isDestroyed) CombatSession.Log($"{name}: модуль {moduleType} выбит");
            Debug.Log($"{gameObject.name}: Попадание в модуль {moduleType} - урон {amount}, HP модуля {module.currentHP}/{module.maxHP}{(module.isDestroyed ? " (МОДУЛЬ ВЫБИТ)" : string.Empty)}");
        }
    }

    private void ApplyHullDamage(float amount)
    {
        Data.TakeHullDamage(amount);
        Debug.Log($"{gameObject.name}: Попадание в корпус - урон {amount}, HP корпуса {Data.currentHullHP}/{Data.maxHullHP}");
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        CombatSession.Log($"{name}: уничтожен");
        OnDestroyed?.Invoke();
        Debug.Log($"{gameObject.name} уничтожен!");
        gameObject.SetActive(false);
    }
}