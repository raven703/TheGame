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

    [Tooltip("Maximum hull hit points. ShipData is created from this value in Awake.")]
    public float maxHullHP = 100f;

    public ShipData Data { get; private set; }

    public event Action OnDamageTaken;
    public event Action OnDestroyed;

    private bool isDead;

    private void Awake()
    {
        Data = new ShipData(maxHullHP);
    }

    private void Update()
    {
        if (Data == null || !Data.IsDestroyed)
            return;

        Die();
    }

    public void TakeDamage(float amount, ModuleType? targetedModule = null)
    {
        if (Data == null || Data.IsDestroyed || amount <= 0f)
            return;

        // 1. Поглощение урона Щитом
        float remainingDamage = Data.AbsorbDamageWithShield(amount);

        if (remainingDamage <= 0f)
        {
            OnDamageTaken?.Invoke();
            return;
        }

        // 2. Распределение оставшегося урона (Целевой модуль / Случайный модуль / Корпус)
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
        OnDestroyed?.Invoke();
        Debug.Log($"{gameObject.name} уничтожен!");
        gameObject.SetActive(false);
    }
}