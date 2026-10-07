using UnityEngine;

/// <summary>
/// Класс физического состояния отдельного модуля (оружие, щит, двигатель)
/// </summary>
public class ShipModule
{
    public ModuleType type;
    public float maxHP;
    public float currentHP;

    public bool isDestroyed => currentHP <= 0f;

    public ShipModule(ModuleType type, float maxHP)
    {
        this.type = type;
        this.maxHP = Mathf.Max(0f, maxHP);
        this.currentHP = this.maxHP;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Max(0f, currentHP - amount);
    }

    public void Repair(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }
}