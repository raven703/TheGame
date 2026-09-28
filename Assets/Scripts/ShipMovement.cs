using UnityEngine;

/// <summary>
/// Drives a ship towards a target position using physics forces and inertia.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ShipMovement : MonoBehaviour
{
    private const float ArriveDistance = 0.4f;

    [Tooltip("Maximum velocity limit.")]
    public float maxSpeed = 5f;

    [Tooltip("Acceleration power force.")]
    public float acceleration = 12f;

    [Tooltip("Turn speed in degrees per second.")]
    public float rotationSpeed = 300f;

    [Tooltip("Check TRUE if your sprite image points UP (along Y axis). Check FALSE if it points RIGHT (along X axis).")]
    public bool spriteNoseIsUp = true;

    /// <summary>World position the ship is currently flying to.</summary>
    public Vector3 TargetPosition { get; private set; }

    /// <summary>True while the ship has an active move order.</summary>
    public bool HasTarget { get; private set; } = false;

    public Rigidbody2D Rb { get; private set; }
    private ShipHealth shipHealth;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        shipHealth = GetComponent<ShipHealth>();

        Rb.gravityScale = 0f;
        Rb.linearDamping = 1.2f;
        Rb.angularDamping = 3f;
    }

    private void Start()
    {
        TargetPosition = transform.position;
        ApplyMassFromData();
    }

    public void ApplyMassFromData()
    {
        if (shipHealth != null && shipHealth.Data != null)
        {
            Rb.mass = shipHealth.Data.mass;
        }
    }

    public void SetTargetPosition(Vector3 pos)
    {
        TargetPosition = new Vector3(pos.x, pos.y, 0f);
        HasTarget = true;
    }

    public void Stop()
    {
        HasTarget = false;
    }

    private void FixedUpdate()
    {
        if (shipHealth != null && shipHealth.Data != null)
        {
            var engineMod = shipHealth.Data.GetModule(ModuleType.Engine);
            if (engineMod != null && engineMod.isDestroyed)
                return;
        }

        if (!HasTarget || Rb == null)
            return;

        Vector2 currentPos = Rb.position;
        Vector2 toTarget = (Vector2)TargetPosition - currentPos;
        float distance = toTarget.magnitude;

        if (distance < ArriveDistance)
        {
            Stop();
            return;
        }

        Vector2 desiredDirection = toTarget.normalized;

        // 1. Поворот корабля в сторону цели
        RotateTowardsDirection(desiredDirection);

        Vector2 forwardVector = GetForwardVector();

        // 2. Гашение бокового заноса (дрейфа)
        Vector2 forwardVelocity = forwardVector * Vector2.Dot(Rb.linearVelocity, forwardVector);
        Vector2 rightVector = new Vector2(-forwardVector.y, forwardVector.x);
        Vector2 rightVelocity = rightVector * Vector2.Dot(Rb.linearVelocity, rightVector);

        // Активно гасим боковую скорость (0.15f вместо 0.9f) для точного вхождения в повороты
        Rb.linearVelocity = forwardVelocity + rightVelocity * 0.15f;

        // 3. Вычисление alignment (насколько нос сориентирован на цель)
        float alignment = Vector2.Dot(forwardVector, desiredDirection);

        // Импульс даем ТОЛЬКО если сориентированы в сторону цели (alignment > 0)
        if (alignment > 0f)
        {
            // Чем точнее повернут нос к цели, тем больше тяги применяем
            float thrustFactor = Mathf.Pow(alignment, 2f);
            Rb.AddForce(forwardVector * (acceleration * thrustFactor), ForceMode2D.Force);
        }

        // Ограничение максимальной скорости
        if (Rb.linearVelocity.magnitude > maxSpeed)
        {
            Rb.linearVelocity = Rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void RotateTowardsDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (spriteNoseIsUp)
        {
            targetAngle -= 90f;
        }

        float currentAngle = Rb.rotation;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.fixedDeltaTime);
        Rb.MoveRotation(newAngle);
    }

    public Vector2 GetForwardVector()
    {
        return spriteNoseIsUp ? (Vector2)transform.up : (Vector2)transform.right;
    }
}