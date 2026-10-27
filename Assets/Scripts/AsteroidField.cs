using UnityEngine;

/// <summary>Deterministic scene-local cover, without modifying the scene asset.</summary>
public class AsteroidField : MonoBehaviour
{
    private readonly System.Collections.Generic.List<Texture2D> textures = new System.Collections.Generic.List<Texture2D>();
    private readonly System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
    private void Start()
    {
        for(int variant=0;variant<4;variant++)
        {
        var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                var p=new Vector2(x-63.5f,y-63.5f)/60f;float angle=Mathf.Atan2(p.y,p.x);
                float edge=.92f+.045f*Mathf.Sin(angle*5+variant*1.9f)+.03f*Mathf.Sin(angle*9+variant);
                float radius=p.magnitude;float alpha=Mathf.Clamp01((edge-radius)*60);
                float noise=Mathf.PerlinNoise(x*.085f+variant*17,y*.085f+variant*9);
                float fine=Mathf.PerlinNoise(x*.35f+variant*4,y*.35f);
                float light=Mathf.Clamp01(.55f+(-p.x+p.y)*.28f);
                float crater=0;
                for(int c=0;c<5;c++){var center=new Vector2(Mathf.Sin(c*8+variant)*.54f,Mathf.Cos(c*5+variant)*.54f);float d=Vector2.Distance(p,center);float r=.09f+(c%3)*.035f;crater+=Mathf.Exp(-Mathf.Pow((d-r)/.025f,2))*.12f-Mathf.Exp(-d*d/(r*r))*.16f;}
                float value=Mathf.Clamp01(.23f+light*.34f+(noise-.5f)*.22f+(fine-.5f)*.07f+crater);
                texture.SetPixel(x,y,new Color(value*1.04f,value,value*.90f,alpha));
            }
        texture.Apply();
        textures.Add(texture);sprites.Add(Sprite.Create(texture,new Rect(0,0,128,128),new Vector2(.5f,.5f),64f));
        }
        Vector2[] positions = { new Vector2(-3, 2), new Vector2(3, -2), new Vector2(0, 4), new Vector2(0, -4) };
        var ships = FindObjectsByType<ShipHealth>(FindObjectsSortMode.None);
        int index=0;
        foreach (Vector2 point in positions)
        {
            bool occupied = false;
            foreach (var ship in ships)
                if (Vector2.Distance(point, ship.transform.position) < 2.5f) occupied = true;
            if (occupied) continue;
            var rock = new GameObject("Asteroid");
            rock.transform.SetParent(transform);
            rock.transform.position = point;
            rock.AddComponent<SpriteRenderer>().sprite = sprites[index++%sprites.Count];
            rock.AddComponent<CircleCollider2D>().radius = .9f;
            rock.AddComponent<Asteroid>();
        }
    }
    private void OnDestroy()
    {
        foreach(var sprite in sprites)
        {
            if (Application.isPlaying) Destroy(sprite);
            else DestroyImmediate(sprite);
        }
        foreach(var texture in textures)
        {
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }
    }
}
