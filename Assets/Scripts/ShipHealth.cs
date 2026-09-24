using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    private const float ModuleHitChance = 0.5f;

    private static readonly ModuleType[] AllModuleTypes =
    {
        ModuleType.Weapon,
        ModuleType.Shield,
        ModuleType.Engine
    };

    [Tooltip("Maximum hull hit points. ShipData is created from this value in Awake.")]
    public float maxHullHP = 100f;

    public ShipData Data { get; private set; }

    // C# Events для UI
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

    public void TakeDamage(float amount)
    {
        if (Data == null || Data.IsDestroyed || amount <= 0f)
            return;

        if (UnityEngine.Random.value < ModuleHitChance)
            ApplyModuleDamage(amount);
        else
            ApplyHullDamage(amount);

        OnDamageTaken?.Invoke();
    }

    private void ApplyModuleDamage(float amount)
    {
        var moduleType = AllModuleTypes[UnityEngine.Random.Range(0, AllModuleTypes.Length)];
        var module = Data.GetModule(moduleType);
        if (module == null)
        {
            Debug.Log($"{gameObject.name}: попадание в {moduleType}, но этот модуль отсутствует в ShipData");
            return;
        }

        var wasAlreadyDestroyed = module.isDestroyed;
        Data.TakeModuleDamage(moduleType, amount);

        if (wasAlreadyDestroyed)
            Debug.Log($"{gameObject.name}: попадание в выбитый модуль {moduleType} (урон {amount} ушёл в никуда), HP модуля {module.currentHP}/{module.maxHP}");
        else
            Debug.Log($"{gameObject.name}: попадание в модуль {moduleType} - урон {amount}, HP модуля {module.currentHP}/{module.maxHP}{(module.isDestroyed ? " (МОДУЛЬ ВЫБИТ)" : string.Empty)}");
    }

    private void ApplyHullDamage(float amount)
    {
        Data.TakeHullDamage(amount);
        Debug.Log($"{gameObject.name}: попадание в корпус - урон {amount}, HP корпуса {Data.currentHullHP}/{Data.maxHullHP}");
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