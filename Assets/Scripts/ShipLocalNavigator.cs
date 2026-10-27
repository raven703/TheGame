using UnityEngine;

/// <summary>Bounded local avoidance, not a global path finder. Side is latched per obstruction.</summary>
public class ShipLocalNavigator
{
    private Collider2D obstacle;
    private int side = 1;
    private float bestDistance = float.PositiveInfinity;
    private float stalled;
    private int retries;
    private Vector2 lastDestination;
    private float legTime;
    private Vector2 progressPosition;
    public bool Failed { get; private set; }
    public int Retries => retries;
    public void Reset()
    {
        obstacle = null; bestDistance = float.PositiveInfinity; stalled = 0f; retries = 0; Failed = false;legTime=0f;
    }
    private static bool Rock(Collider2D collider) => collider != null && !collider.isTrigger && collider.GetComponentInParent<Asteroid>() != null;
    public static Vector2 AccessiblePoint(Vector2 requested, Vector2 from, float radius)
    {
        // Conservative collider bounds avoid projecting into a polygon's concave interior.
        Vector2 point = requested;
        for (int pass = 0; pass < 8; pass++)
        {
            bool changed = false;
            foreach (var collider in Physics2D.OverlapCircleAll(point, radius + .15f))
            {
                if (!Rock(collider)) continue;
                Vector2 center = collider.bounds.center;
                Vector2 direction = point - center;
                if (direction.sqrMagnitude < .001f) direction = from - center;
                if (direction.sqrMagnitude < .001f) direction = Vector2.up;
                point = center + direction.normalized * (collider.bounds.extents.magnitude + radius + .25f);
                changed = true;
            }
            if (!changed) break;
        }
        return point;
    }
    public Vector2 Steer(Vector2 position, Vector2 destination, Vector2 velocity, float radius, float brakingAcceleration, float dt, bool fixedDestination, out bool detouring)
    {
        if ((lastDestination - destination).sqrMagnitude > .25f) { bestDistance = float.PositiveInfinity; stalled = 0f; }
        lastDestination = destination;
        float distance = Vector2.Distance(position, destination);
        if(obstacle!=null)legTime+=dt;
        if(legTime>32f && distance>.5f){retries=3;Failed=true;}
        bool progress = fixedDestination ? distance < bestDistance - .15f : Vector2.Distance(position,progressPosition)>.5f;
        if (progress) { bestDistance = distance; stalled = 0f;progressPosition=position; }
        else if (distance > .35f) stalled += dt;
        // Includes time needed for a slow/heavy hull to turn and get moving.
        if (stalled > 8f)
        {
            stalled = 0f; bestDistance = float.PositiveInfinity; obstacle = null; side = -side;
            retries++;
            if (retries >= 3) Failed = true;
        }
        Vector2 direction = (destination - position).normalized;
        float look = Mathf.Min(distance, Mathf.Max(3f, velocity.sqrMagnitude / (2f * Mathf.Max(.1f, brakingAcceleration)) + radius * 2f + 1f));
        Collider2D hitObstacle = null;
        foreach (var hit in Physics2D.CircleCastAll(position, radius + .2f, direction, look))
        {
            if (Rock(hit.collider)) { hitObstacle = hit.collider; break; }
        }
        if (hitObstacle == null)
        {
            // Release only after the full straight path is clear, retaining a stable side around the rock.
            bool clear = true;
            foreach (var hit in Physics2D.CircleCastAll(position, radius + .2f, direction, distance))
                if (Rock(hit.collider)) { clear = false; break; }
            if (clear) obstacle = null;
        }
        else if (obstacle == null)
        {
            obstacle = hitObstacle;
            Vector2 offset = (Vector2)obstacle.bounds.center - position;
            side = Vector3.Cross(direction, offset).z >= 0f ? -1 : 1;
        }
        detouring = obstacle != null;
        if (!detouring) return destination;
        Vector2 center = obstacle.bounds.center;
        float clearance = obstacle.bounds.extents.magnitude + radius + .65f;
        Vector2 outward = (position - center).normalized;
        if (outward.sqrMagnitude < .01f) outward = -direction;
        Vector2 tangent = new Vector2(-outward.y, outward.x) * side;
        // This moving waypoint follows drifting rocks rather than a stale sampled center.
        return center + (outward + tangent * .9f).normalized * clearance;
    }
}