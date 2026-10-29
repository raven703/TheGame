using UnityEngine;

/// <summary>Only propulsion writer. Orders are evaluated in this same physics step.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ShipMovement : MonoBehaviour
{
    public float maxSpeed = 5f;
    public float acceleration = 12f;
    public float reverseThrustRatio = .5f;
    public float lateralThrustRatio = .35f;
    public float rotationSpeed = 300f;
    public bool spriteNoseIsUp = true;
    public Vector3 TargetPosition { get; private set; }
    public bool HasTarget => Orders != null && Orders.Mode != ShipOrderMode.HoldPosition;
    public Rigidbody2D Rb { get; private set; }
    public ShipOrderController Orders { get; private set; }
    public float SpeedMultiplier { get; set; } = 1f;
    private ShipHealth shipHealth;
    public bool EngineAvailable
    {
        get
        {
            var engine = shipHealth != null && shipHealth.Data != null ? shipHealth.Data.GetModule(ModuleType.Engine) : null;
            return (shipHealth == null || shipHealth.Data == null || !shipHealth.Data.IsDestroyed) && (engine == null || !engine.isDestroyed);
        }
    }
    public float AvailableForce => EngineAvailable ? Mathf.Max(0f, acceleration * SpeedMultiplier) : 0f;
    // Elliptic engine budget: simultaneous axes share one normalized effort.
    public Vector2 LimitThrust(Vector2 requested)
    {
        Vector2 forward = GetForwardVector();
        Vector2 side = new Vector2(forward.y, -forward.x);
        float longitudinal = Vector2.Dot(requested, forward);
        float lateral = Vector2.Dot(requested, side);
        float main = AvailableForce;
        float a = main * (longitudinal >= 0f ? 1f : reverseThrustRatio);
        float b = main * lateralThrustRatio;
        var limited=Game.Core.DirectedThrust.Limit(longitudinal,lateral,main,main*reverseThrustRatio,b);
        return forward * limited.X + side * limited.Y;
    }
    public float ForceAlong(Vector2 direction)
    {
        if (direction.sqrMagnitude < .0001f) return AvailableForce * Mathf.Min(reverseThrustRatio, lateralThrustRatio);
        Vector2 f = GetForwardVector();
        float x = Vector2.Dot(direction.normalized, f);
        float y = Vector2.Dot(direction.normalized, new Vector2(f.y, -f.x));
        float a = AvailableForce * (x >= 0f ? 1f : reverseThrustRatio);
        float b = AvailableForce * lateralThrustRatio;
        if (a <= 0f || b <= 0f) return 0f;
        return 1f / Mathf.Sqrt(x*x/(a*a) + y*y/(b*b));
    }
    public float ShipRadius { get; private set; } = .4f;
    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        shipHealth = GetComponent<ShipHealth>();
        Rb.gravityScale = 0f;
        Rb.linearDamping = 1.2f;
        Rb.angularDamping = 3f;
        Orders = GetComponent<ShipOrderController>();
        if (Orders == null) Orders = gameObject.AddComponent<ShipOrderController>();
        if (Application.isPlaying && CompareTag("Player") && GetComponent<ShipOrderVisuals>() == null) gameObject.AddComponent<ShipOrderVisuals>();
        foreach (var collider in GetComponentsInChildren<Collider2D>())
            if (!collider.isTrigger) ShipRadius = Mathf.Max(ShipRadius, collider.bounds.extents.magnitude);
    }
    private void Start() { ApplyMassFromData(); }
    public void ApplyMassFromData()
    {
        if (shipHealth != null && shipHealth.Data != null) Rb.mass = Mathf.Max(.1f, shipHealth.Data.mass);
    }
    public void SetTargetPosition(Vector3 position) { Orders.MoveTo(position); }
    public void Stop() { Orders.Stop(); }
    private void FixedUpdate()
    {
        if (CombatSession.IsPreparing || Rb == null || Orders == null) return;
        var ai = GetComponent<EnemyAI>();
        if (ai != null && ai.isActiveAndEnabled) ai.UpdateOrders();
        bool final;
        Vector2 destination = Orders.Evaluate(Rb.position, Rb.linearVelocity, out final);
        TargetPosition = Orders.Destination;
        if (!EngineAvailable) return;
        Vector2 delta = destination - Rb.position;
        float force = AvailableForce;
        float deceleration = ForceAlong(Rb.linearVelocity.sqrMagnitude > .01f ? -Rb.linearVelocity : -delta) / Mathf.Max(.01f, Rb.mass);
        float desiredSpeed = Mathf.Max(0f, maxSpeed * SpeedMultiplier * Orders.SpeedLimit);
        // A queued corner can be crossed, but its turn radius must fit the remaining leg.
        if(!final && Orders.Waypoints.Count>1)
        {
            Vector2 next=(Orders.Waypoints[1]-Orders.Waypoints[0]).normalized;
            float corner=Vector2.Angle(delta,next)*Mathf.Deg2Rad;
            float cornerSpeed=Mathf.Sqrt(Mathf.Max(.05f,deceleration*.65f/Mathf.Max(.15f,corner)));
            desiredSpeed=Mathf.Min(desiredSpeed,Mathf.Sqrt(cornerSpeed*cornerSpeed+2f*deceleration*Mathf.Max(0f,delta.magnitude-.5f))*.8f);
        }
        if (final) desiredSpeed = Mathf.Min(desiredSpeed, Mathf.Sqrt(2f * deceleration * Mathf.Max(0f, delta.magnitude - .06f)) * .7f);
        Vector2 direction = delta.normalized;
        // Lateral velocity also consumes the same finite force budget. Never overwrite velocity.
        float alignment = Mathf.Clamp01(Vector2.Dot(GetForwardVector(), direction));
        Vector2 desiredVelocity = delta.magnitude < .08f ? Vector2.zero : direction * desiredSpeed * Mathf.Lerp(.15f, 1f, alignment);
        if (Orders.IsBraking) desiredVelocity = Vector2.zero;
        Rb.AddForce(LimitThrust((desiredVelocity - Rb.linearVelocity) * Rb.mass * 4f));
        Vector2 facing = Orders.FacingDirection(Rb.position, direction);
        float desiredAngular = 0f;
        if (facing.sqrMagnitude > .001f && !Orders.IsBraking)
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - (spriteNoseIsUp ? 90f : 0f);
            desiredAngular = Mathf.Clamp(Mathf.DeltaAngle(Rb.rotation, angle) * 5f, -rotationSpeed, rotationSpeed);
        }
        Rb.AddTorque(Mathf.Clamp((desiredAngular - Rb.angularVelocity) * Mathf.Deg2Rad * Rb.inertia * 6f, -force, force));
    }
    public Vector2 GetForwardVector() { return spriteNoseIsUp ? (Vector2)transform.up : (Vector2)transform.right; }
}
