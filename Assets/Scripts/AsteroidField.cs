using UnityEngine;

/// <summary>Deterministic scene-local cover, without modifying the scene asset.</summary>
public class AsteroidField : MonoBehaviour
{
    private Texture2D texture;
    private Sprite sprite;
    private void Start()
    {
        texture = new Texture2D(48, 48);
        for (int y = 0; y < 48; y++)
            for (int x = 0; x < 48; x++)
            {
                float radius = new Vector2(x - 23.5f, y - 23.5f).magnitude;
                float edge = 21f + 2f * Mathf.Sin(Mathf.Atan2(y - 23.5f, x - 23.5f) * 7f);
                texture.SetPixel(x, y, radius < edge ? new Color(.40f, .36f, .30f) : Color.clear);
            }
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, 48, 48), new Vector2(.5f, .5f), 24f);
        Vector2[] positions = { new Vector2(-3, 2), new Vector2(3, -2), new Vector2(0, 4), new Vector2(0, -4) };
        var ships = FindObjectsByType<ShipHealth>(FindObjectsSortMode.None);
        foreach (Vector2 point in positions)
        {
            bool occupied = false;
            foreach (var ship in ships)
                if (Vector2.Distance(point, ship.transform.position) < 2.5f) occupied = true;
            if (occupied) continue;
            var rock = new GameObject("Asteroid");
            rock.transform.SetParent(transform);
            rock.transform.position = point;
            rock.AddComponent<SpriteRenderer>().sprite = sprite;
            rock.AddComponent<CircleCollider2D>().radius = .9f;
            rock.AddComponent<Asteroid>();
        }
    }
    private void OnDestroy()
    {
        if (sprite != null)
        {
            if (Application.isPlaying) Destroy(sprite);
            else DestroyImmediate(sprite);
        }
        if (texture != null)
        {
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }
    }
}
