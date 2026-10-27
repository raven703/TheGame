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
    /// <summary>Orbit speed in degrees per second while holding the optimal distance.</summary>
    private const float OrbitSpeedDegrees = 25f;

    [Tooltip("Movement component of this ship.")]
    public ShipMovement shipMovement;

    [Tooltip("Weapon component of this ship.")]
    public ShipWeapon shipWeapon;

    [Tooltip("Player ship to hunt. Resolved from the 'Player' tag when left empty.")]
    public ShipHealth playerTarget;

    [Tooltip("Combat distance the ship tries to hold (kept below the 6 unit weapon range).")]
    public float optimalDistance = 4.5f;

    public AIPersonality personality = AIPersonality.Brawler;
    private float orbitAngle;

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

        if (playerTarget != null)
            orbitAngle = BearingFromTarget();
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
            if (orders.Mode != ShipOrderMode.HoldPosition) orders.Stop();
            return;
        }
        if (shipWeapon.Target != playerTarget && shipWeapon.PendingTarget != playerTarget) shipWeapon.SetTarget(playerTarget);
        var health = GetComponent<ShipHealth>();
        bool retreat = personality == AIPersonality.Cautious && health != null && health.Data != null && health.Data.currentHullHP < health.Data.maxHullHP * .35f;
        orders.PreferredDistance = retreat ? shipWeapon.range * 1.4f : personality == AIPersonality.Sniper ? shipWeapon.range * .85f : personality == AIPersonality.Flanker ? shipWeapon.range * .5f : optimalDistance;
        var mode=personality == AIPersonality.Flanker ? ShipOrderMode.Flank : personality == AIPersonality.Sniper || retreat ? ShipOrderMode.KeepDistance : ShipOrderMode.Orbit;
        if(orders.Mode!=mode || orders.OrderTarget!=playerTarget)orders.Tactical(mode,playerTarget);
    }

    /// <summary>Bearing in degrees from the player towards this ship.</summary>
    private float BearingFromTarget()
    {
        if (playerTarget == null)
            return 0f;

        var delta = transform.position - playerTarget.transform.position;
        return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
    }
}
