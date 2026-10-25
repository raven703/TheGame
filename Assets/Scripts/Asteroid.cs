using UnityEngine;

/// <summary>Destructible static cover; ships collide with it and direct fire stops at it.</summary>
public class Asteroid : MonoBehaviour
{
    public float hitPoints = 120f;
    public void TakeDamage(float amount)
    {
        hitPoints -= Mathf.Max(0f, amount);
        if (hitPoints <= 0f)
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
