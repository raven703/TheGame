using System;
using UnityEngine;

/// <summary>
/// Runtime state of a single ship module (weapon / shield / engine).
/// </summary>
/// <remarks>
/// Plain C# class on purpose: it is owned by <see cref="ShipData"/> and kept out of the
/// MonoBehaviour view layer, so damage logic can be tested without a scene.
/// </remarks>
[Serializable]
public class ShipModule
{
    /// <summary>Module category. Also used as the key inside <see cref="ShipData.modules"/>.</summary>
    public ModuleType type;

    /// <summary>Maximum hit points of the module.</summary>
    public float maxHP;

    /// <summary>Current hit points of the module.</summary>
    public float currentHP;

    /// <summary>True when the module has no hit points left.</summary>
    public bool isDestroyed => currentHP <= 0f;

    /// <summary>Creates a module at full health.</summary>
    /// <param name="type">Module category.</param>
    /// <param name="maxHP">Maximum (and starting) hit points. Negative values are clamped to 0.</param>
    public ShipModule(ModuleType type, float maxHP)
    {
        this.type = type;
        this.maxHP = Mathf.Max(0f, maxHP);
        currentHP = this.maxHP;
    }

    /// <summary>Applies damage. Health never drops below 0.</summary>
    /// <param name="amount">Damage amount. Non-positive values are ignored.</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
            return;

        currentHP = Mathf.Max(0f, currentHP - amount);
    }

    /// <summary>Restores health, clamped to <see cref="maxHP"/>.</summary>
    /// <param name="amount">Repair amount. Non-positive values are ignored.</param>
    public void Repair(float amount)
    {
        if (amount <= 0f)
            return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }
}
