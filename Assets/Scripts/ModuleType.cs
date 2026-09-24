/// <summary>
/// Logical ship module categories.
/// </summary>
/// <remarks>
/// Ship modules are addressed by these enum values instead of magic strings,
/// which keeps module lookups compile-time safe and refactor friendly.
/// </remarks>
public enum ModuleType
{
    /// <summary>Offensive module.</summary>
    Weapon,

    /// <summary>Defensive module.</summary>
    Shield,

    /// <summary>Propulsion module.</summary>
    Engine
}
