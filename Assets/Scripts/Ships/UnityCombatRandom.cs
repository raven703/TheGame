/// <summary>Same global Unity source and call order as the original combat implementation.</summary>
public sealed class UnityCombatRandom : Game.Core.ICombatRandom
{
    public static readonly UnityCombatRandom Instance = new UnityCombatRandom();
    private UnityCombatRandom() { }
    public float Value() => UnityEngine.Random.value;
    public int Index(int exclusiveMaximum) => UnityEngine.Random.Range(0, exclusiveMaximum);
}