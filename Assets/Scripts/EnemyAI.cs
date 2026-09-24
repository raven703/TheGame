using UnityEngine;

/// <summary>
/// Minimal combat AI for the enemy ship: hunt the player, hold the optimal firing
/// distance and keep circling around it so the fight stays dynamic.
/// </summary>
/// <remarks>
/// The AI only decides *where* to fly - movement is delegated to <see cref="ShipMovement"/>
/// and target tracking to <see cref="ShipWeapon"/>.
/// </remarks>
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
        if (shipWeapon != null)
            shipWeapon.SetTarget(playerTarget);
    }

    private void Update()
    {
        if (shipMovement == null || shipWeapon == null)
            return;

        if (playerTarget == null || !playerTarget.gameObject.activeSelf || (playerTarget.Data != null && playerTarget.Data.IsDestroyed))
        {
            shipMovement.Stop();
            return;
        }

        var targetPosition = playerTarget.transform.position;
        var distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > optimalDistance)
        {
            // Hunt: fly straight at the player until the comfort zone is reached.
            orbitAngle = BearingFromTarget();
            shipMovement.SetTargetPosition(targetPosition);
            return;
        }

        // Comfort zone: keep circling the player at the optimal distance instead of ramming it.
        orbitAngle += OrbitSpeedDegrees * Time.deltaTime;
        var radians = orbitAngle * Mathf.Deg2Rad;
        var offset = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * optimalDistance;
        shipMovement.SetTargetPosition(targetPosition + offset);
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
