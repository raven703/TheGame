using UnityEngine;

/// <summary>
/// Minimal combat AI for the enemy ship: hunt the player, hold the optimal firing
/// distance and keep circling around it so the fight stays dynamic.
/// </summary>
/// <remarks>
/// The AI only decides *where* to fly - movement is delegated to <see cref="ShipMovement"/>
/// and target tracking to <see cref="ShipWeapon"/>.
/// </remarks>
public enum AIPersonality { Brawler, Sniper, Flanker, Cautious }

public class EnemyAI : MonoBehaviour
{
    [Tooltip("Movement component of this ship.")]
    public ShipMovement shipMovement;

    [Tooltip("Weapon component of this ship.")]
    public ShipWeapon shipWeapon;

    [Tooltip("Player ship to hunt. Resolved from the 'Player' tag when left empty.")]
    public ShipHealth playerTarget;

    [Tooltip("Combat distance the ship tries to hold (kept below the 6 unit weapon range).")]
    public float optimalDistance = 4.5f;

    public AIPersonality personality = AIPersonality.Brawler;
    public bool useCombatRole;
    public Game.Core.CombatRole combatRole;
    private readonly Game.Core.CombatRolePilot rolePilot=new Game.Core.CombatRolePilot();
    public Game.Core.CombatManeuver CurrentManeuver=>rolePilot.Maneuver;
    public void SetOpponent(ShipHealth opponent) { playerTarget=opponent; }

    private void Awake()
    {
        if (shipMovement == null)
            shipMovement = GetComponent<ShipMovement>();
        if (shipWeapon == null)
            shipWeapon = GetComponent<ShipWeapon>();

        if (playerTarget == null && !useCombatRole)
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                playerTarget = playerObject.GetComponent<ShipHealth>();
        }

    }

    private void Start()
    {
        if (!useCombatRole && CombatSession.Instance != null) personality = CombatSession.Instance.EnemyPersonality;
        if (shipWeapon != null)
        {
            shipWeapon.SetTarget(playerTarget);
            shipWeapon.useFallbackWeapon = !useCombatRole;
        }
    }

    public void UpdateOrders()
    {
        if (Time.timeScale<=0f || CombatSession.IsPreparing || shipMovement == null || shipWeapon == null) return;
        var orders = shipMovement.Orders;
        if(useCombatRole){UpdateCombatRole(orders);return;}
        if (!ShipOrderController.Available(playerTarget))
        {
            orders.ApplyDecision(Game.Core.PilotDecisions.Decide((Game.Core.PilotPersonality)personality,
                false,false,0,0,shipWeapon.WorkingRange(),optimalDistance),null);
            return;
        }
        if (shipWeapon.Target != playerTarget && shipWeapon.PendingTarget != playerTarget) shipWeapon.SetTarget(playerTarget);
        var health = GetComponent<ShipHealth>();
        var decision = Game.Core.PilotDecisions.Decide((Game.Core.PilotPersonality)personality,
            ShipOrderController.Available(playerTarget), health != null && health.Data != null,
            health != null && health.Data != null ? health.Data.currentHullHP : 0f,
            health != null && health.Data != null ? health.Data.maxHullHP : 0f, shipWeapon.WorkingRange(), shipWeapon.WorkingRange()*.75f);
        orders.ApplyDecision(decision, playerTarget);
    }

    private void UpdateCombatRole(ShipOrderController orders)
    {
        bool available=ShipOrderController.Available(playerTarget);
        if(available && shipWeapon.Target!=playerTarget && shipWeapon.PendingTarget!=playerTarget)shipWeapon.SetTarget(playerTarget);
        var health=GetComponent<ShipHealth>();var data=health!=null?health.Data:null;
        var reactor=GetComponent<ShipReactor>();var abilities=GetComponent<ShipAbilities>();
        var targetBody=available?playerTarget.GetComponent<Rigidbody2D>():null;
        var targetMovement=available?playerTarget.GetComponent<ShipMovement>():null;
        var targetWeapon=available?playerTarget.GetComponent<ShipWeapon>():null;
        Vector2 position=transform.position,targetPosition=available?(Vector2)playerTarget.transform.position:position;
        Vector2 forward=shipMovement.GetForwardVector(),targetForward=targetMovement!=null?targetMovement.GetForwardVector():Vector2.up;
        Vector2 velocity=targetBody!=null?targetBody.linearVelocity:Vector2.zero;
        bool blocked=available&&Asteroid.FindCover(position,targetPosition,out _ )!=null;
        var observation=new Game.Core.CombatRoleObservation {
            Position=new Game.Core.AimPoint(position.x,position.y),Forward=new Game.Core.AimPoint(forward.x,forward.y),
            Velocity=new Game.Core.AimPoint(shipMovement.Rb.linearVelocity.x,shipMovement.Rb.linearVelocity.y),
            Radius=GetComponent<Collider2D>()!=null?GetComponent<Collider2D>().bounds.extents.magnitude:.5f,
            TargetRadius=available&&playerTarget.GetComponent<Collider2D>()!=null?playerTarget.GetComponent<Collider2D>().bounds.extents.magnitude:.5f,
            TargetPosition=new Game.Core.AimPoint(targetPosition.x,targetPosition.y),TargetVelocity=new Game.Core.AimPoint(velocity.x,velocity.y),
            TargetForward=new Game.Core.AimPoint(targetForward.x,targetForward.y),TargetAvailable=available,
            EngineAvailable=shipMovement.EngineAvailable,WeaponAvailable=data!=null&&!data.GetModule(ModuleType.Weapon).isDestroyed,
            LineBlocked=blocked,Range=shipWeapon.WorkingRange(combatRole),Energy=reactor!=null?reactor.Energy:0,MaxEnergy=reactor!=null?reactor.maxEnergy:0,
            EnergyFraction=reactor!=null?reactor.Energy/Mathf.Max(1,reactor.maxEnergy):0,HeatFraction=reactor!=null?reactor.Heat/100f:0,
            Hull=data!=null?data.currentHullHP:0,HullFraction=data!=null?data.currentHullHP/Mathf.Max(1,data.maxHullHP):0,
            ShieldFraction=data!=null?data.currentShieldHP/Mathf.Max(1,data.maxShieldHP):1,
            BoostReady=abilities!=null&&abilities.afterburnerCooldown<=0,ShieldReady=abilities!=null&&abilities.shieldBoostCooldown<=0,
            EmpReady=abilities!=null&&abilities.emPulseCooldown<=0,TargetSuppressed=targetWeapon!=null&&targetWeapon.IsTargetingDisrupted };
        var decision=rolePilot.Decide(combatRole,ShipOrderController.RuntimeTargetId(playerTarget),observation,Time.fixedDeltaTime);
        if(decision.NewCommand)
        {
            if(decision.UseWaypoint)orders.MoveTo(new Vector2(decision.Waypoint.X,decision.Waypoint.Y));
            else {orders.ApplyDecision(decision.Order,playerTarget);if(decision.Order.Mode!=Game.Core.OrderMode.HoldPosition)orders.SetOrientation((HullOrientation)decision.Orientation);}
        }
        shipWeapon.AIFireAllowed=decision.AllowFire;
        if(abilities!=null)
        {
            if(decision.Shield)abilities.UseShieldBoost();
            if(decision.Boost)abilities.UseAfterburner();
            if(decision.Emp)abilities.UseEMPulse();
        }
    }

}
