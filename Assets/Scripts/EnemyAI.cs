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

    private void Awake()
    {
        if (shipMovement == null)
            shipMovement = GetComponent<ShipMovement>();
        if (shipWeapon == null)
            shipWeapon = GetComponent<ShipWeapon>();

        if (playerTarget == null)
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                playerTarget = playerObject.GetComponent<ShipHealth>();
        }

    }

    private void Start()
    {
        if (CombatSession.Instance != null) personality = CombatSession.Instance.EnemyPersonality;
        if (shipWeapon != null)
        {
            shipWeapon.SetTarget(playerTarget);
            shipWeapon.useFallbackWeapon = true;
        }
    }

    public void UpdateOrders()
    {
        if (CombatSession.IsPreparing || shipMovement == null || shipWeapon == null) return;
        var orders = shipMovement.Orders;
        if (!ShipOrderController.Available(playerTarget))
        {
            orders.ApplyDecision(Game.Core.PilotDecisions.Decide((Game.Core.PilotPersonality)personality,
                false,false,0,0,shipWeapon.range,optimalDistance),null);
            return;
        }
        if (shipWeapon.Target != playerTarget && shipWeapon.PendingTarget != playerTarget) shipWeapon.SetTarget(playerTarget);
        var health = GetComponent<ShipHealth>();
        var decision = Game.Core.PilotDecisions.Decide((Game.Core.PilotPersonality)personality,
            ShipOrderController.Available(playerTarget), health != null && health.Data != null,
            health != null && health.Data != null ? health.Data.currentHullHP : 0f,
            health != null && health.Data != null ? health.Data.maxHullHP : 0f, shipWeapon.range, optimalDistance);
        orders.ApplyDecision(decision, playerTarget);
    }

}
