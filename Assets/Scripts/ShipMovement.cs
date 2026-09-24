using UnityEngine;

/// <summary>
/// Drives a ship towards a target position: turns the nose (transform.right) towards the
/// movement direction and moves the Kinematic <see cref="Rigidbody2D"/> without physics forces.
/// </summary>
/// <remarks>
/// The nose points along transform.right because the WeaponPoint marker sits at local X = 0.5.
/// Attach to the ship root.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
public class ShipMovement : MonoBehaviour
{
    /// <summary>Distance at which the ship is considered to have arrived.</summary>
    private const float ArriveDistance = 0.05f;

    [Tooltip("Movement speed in units per second.")]
    public float speed = 3f;

    [Tooltip("Turn speed in degrees per second.")]
    public float rotationSpeed = 360f;

    /// <summary>World position the ship is currently flying to.</summary>
    public Vector3 TargetPosition { get; private set; }

    /// <summary>True while the ship has an active move order.</summary>
    public bool HasTarget { get; private set; } = false;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        TargetPosition = transform.position;
    }

    /// <summary>Issues a move order. The Z component is always forced to 0 (2D plane).</summary>
    public void SetTargetPosition(Vector3 pos)
    {
        TargetPosition = new Vector3(pos.x, pos.y, 0f);
        HasTarget = true;
    }

    /// <summary>Cancels the current move order.</summary>
    public void Stop()
    {
        HasTarget = false;
    }

    private void FixedUpdate()
    {
        if (!HasTarget || rb == null)
            return;

        var toTarget = (Vector2)TargetPosition - rb.position;
        var distance = toTarget.magnitude;

        if (distance < ArriveDistance)
        {
            Stop();
            rb.MovePosition((Vector2)TargetPosition);
            return;
        }

        var direction = toTarget.normalized;

        // Turn the nose (transform.right) towards the travel direction.
        var targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        rb.MoveRotation(Quaternion.RotateTowards(rb.transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

        // Advance along the straight line towards the target.
        rb.MovePosition(Vector2.MoveTowards(rb.position, TargetPosition, speed * Time.fixedDeltaTime));
    }
}
