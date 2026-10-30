using UnityEngine;

/// <summary>Deterministic scene-local cover, without modifying the scene asset.</summary>
public class AsteroidField : MonoBehaviour
{
    public int count=15,seed=12345,attemptsPerRock=80;
    public bool randomizeSeed=true;
    public Vector2 sizeRange=new Vector2(.6f,1.6f);
    public Rect area=new Rect(-16,-11,32,22);
    public float minimumGap=1.2f,shipClearance=4f;
    public int ActualSeed {get;private set;}
    public int ActualCount {get;private set;}
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
        ActualSeed=randomizeSeed?System.Guid.NewGuid().GetHashCode():seed;
        var random=new System.Random(ActualSeed);
        var placed=new System.Collections.Generic.List<Vector3>();
        var ships = FindObjectsByType<ShipHealth>(FindObjectsSortMode.None);
        var arena=BattleArena.Ensure();
        Vector2 arenaCenter=new Vector2(arena.Area.Center.X,arena.Area.Center.Y);
        float largest=1;
        foreach(var ship in ships){var movement=ship.GetComponent<ShipMovement>();if(movement!=null)largest=Mathf.Max(largest,movement.ShipRadius);}
        float passage=Mathf.Max(minimumGap,largest*2+2);
        int index=0;
        for(int attempt=0;attempt<Mathf.Clamp(count,0,200)*Mathf.Clamp(attemptsPerRock,1,500)&&index<count;attempt++)
        {
            float size=Mathf.Lerp(Mathf.Max(.1f,sizeRange.x),Mathf.Max(sizeRange.x,sizeRange.y),(float)random.NextDouble());
            float radius=.96f*size;
            float angle=(float)random.NextDouble()*Mathf.PI*2;
            float distance=Mathf.Sqrt((float)random.NextDouble())*(arena.Area.Radius-radius);
            Vector2 point=arenaCenter+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;
            if(distance<arena.InitialRange*1.25f+radius)continue;
            bool occupied = false;
            foreach (var ship in ships)
            {
                var collider=ship.GetComponentInChildren<Collider2D>();
                float shipRadius=collider!=null?collider.bounds.extents.magnitude:1;
                if(Vector2.Distance(point,ship.transform.position)<radius+shipRadius+shipClearance)occupied=true;
            }
            foreach(var previous in placed)if(Vector2.Distance(point,previous)<radius+previous.z+passage)occupied=true;
            if (occupied) continue;
            placed.Add(new Vector3(point.x,point.y,radius));
            var rock = new GameObject("Asteroid");
            rock.transform.SetParent(transform);
            rock.transform.position = point;
            rock.transform.localScale = Vector3.one * size;
            rock.AddComponent<SpriteRenderer>().sprite = sprites[index++%sprites.Count];
            rock.AddComponent<CircleCollider2D>().radius = .9f;
            rock.AddComponent<Asteroid>().hitPoints = 120f * size * size;
            var body = rock.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f; body.mass = 8f * size * size;
            body.linearDamping = 0f; body.angularDamping = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            int driftVariant=1+(index-1)%4;
            rock.AddComponent<AsteroidDrift>().Initialize(new Vector2(Mathf.Cos(index*2.4f),Mathf.Sin(index*2.4f)) * (.15f+.07f*driftVariant), (index%2==0?1:-1)*(3f+driftVariant));
        }
        ActualCount=index;
        Debug.Log($"[AsteroidField] seed={ActualSeed}; created={ActualCount}/{count}; sizes={sizeRange.x:0.##}–{sizeRange.y:0.##}");
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
