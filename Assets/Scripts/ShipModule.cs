/// <summary>Compatibility view; HP storage belongs exclusively to the Core model.</summary>
public class ShipModule
{
    private readonly Game.Core.DamageState owner;
    public ModuleType type { get; }
    private Game.Core.ModuleSnapshot Snapshot => owner.Module((Game.Core.CombatModule)(int)type);
    public float maxHP => Snapshot.Maximum;
    public float currentHP => Snapshot.Current;
    public bool isDestroyed => Snapshot.Destroyed;
    internal ShipModule(ModuleType type, Game.Core.DamageState owner) { this.type=type;this.owner=owner; }
    public ShipModule(ModuleType type,float maxHP)
    {
        this.type=type;owner=new Game.Core.DamageState(0);
        owner.InitializeModule((Game.Core.CombatModule)(int)type,maxHP);
    }
    public void TakeDamage(float amount) => owner.ModuleDamage((Game.Core.CombatModule)(int)type,amount);
    public void Repair(float amount) => owner.RepairModule((Game.Core.CombatModule)(int)type,amount);
}
