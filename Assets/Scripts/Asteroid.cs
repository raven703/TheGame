using UnityEngine;

/// <summary>Destructible static cover; ships collide with it and direct fire stops at it.</summary>
public class Asteroid : MonoBehaviour
{
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("hitPoints")] private float initialHitPoints = 120f;
    private Game.Core.CoverState model;
    internal Game.Core.CoverState Model => model ?? (model = new Game.Core.CoverState(initialHitPoints));
    public float hitPoints { get => Model.HitPoints; set => Model.SetHitPoints(value); }
    public void TakeDamage(float amount)
    {
        ReportDamage(Model.Damage(amount));
    }
    internal void ReportDamage(bool destroyed)
    {
        if (destroyed)
        {
            CombatSession.Log("Астероид разрушен");
            Destroy(gameObject);
        }
    }

    public static Asteroid FindCover(Vector2 from, Vector2 to, out Vector2 impact)
    {
        impact = to;
        float nearest = float.PositiveInfinity;
        Asteroid result = null;
        foreach (var hit in Physics2D.LinecastAll(from, to))
        {
            var asteroid = hit.collider.GetComponent<Asteroid>();
            if (asteroid != null && hit.distance < nearest)
            {
                nearest = hit.distance; result = asteroid; impact = hit.point;
            }
        }
        return result;
    }
}
