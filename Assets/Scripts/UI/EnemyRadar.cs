using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UGUI-радар: игрок в центре, враги — точками вокруг.
/// Позиции врагов пропорциональны их реальному положению относительно игрока.
/// </summary>
public class EnemyRadar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Игрок. Если null — ищется по тегу 'Player'.")]
    public Transform playerTransform;

    [Tooltip("Контейнер для точек врагов (RectTransform).")]
    public RectTransform dotsContainer;

    [Tooltip("Префаб точки врага (Image).")]
    public Image enemyDotPrefab;

    [Tooltip("Точка игрока в центре радара.")]
    public Image playerDot;

    [Header("Settings")]
    [Tooltip("Радиус радара в пикселях (визуальный).")]
    public float radarRadiusPixels = 120f;

    [Tooltip("Радиус радара в игровых единицах (что попадает на радар).")]
    public float radarRangeWorld = 25f;

    [Tooltip("Цвет врагов на радаре.")]
    public Color enemyDotColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Цвет точки игрока.")]
    public Color playerDotColor = new Color(0.3f, 0.6f, 1f, 1f);

    private readonly List<Image> dotPool = new List<Image>();
    private readonly List<ShipHealth> aliveEnemies = new List<ShipHealth>();
    private Sprite triangle;
    private Texture2D triangleTexture;
    private Sprite pointSprite;
    public static Vector2 ProjectOffset(Vector2 delta,float range,float radius,float padding=10f)
    {
        float edge=Mathf.Max(0,radius-padding);
        return delta.normalized*Mathf.Min(delta.magnitude/Mathf.Max(.01f,range)*edge,edge);
    }
    private void OnDestroy(){if(triangle!=null)Destroy(triangle);if(triangleTexture!=null)Destroy(triangleTexture);}

    private void Awake()
    {
        triangleTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);
        var pixels=new Color[1024];
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)pixels[y*32+x]=Mathf.Abs(x-15.5f)<(31-y)*.5f?Color.white:Color.clear;
        triangleTexture.SetPixels(pixels);triangleTexture.Apply();
        triangle=Sprite.Create(triangleTexture,new Rect(0,0,32,32),new Vector2(.5f,.5f));
        pointSprite=enemyDotPrefab!=null?enemyDotPrefab.sprite:null;
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (playerDot != null)
            playerDot.color = playerDotColor;
    }

    private void Update()
    {
        if(playerTransform==null){var player=GameObject.FindGameObjectWithTag("Player");if(player!=null)playerTransform=player.transform;}
        if (playerTransform == null || dotsContainer == null || enemyDotPrefab == null)
        {
            foreach(var dot in dotPool)if(dot!=null)dot.gameObject.SetActive(false);
            return;
        }

        // Живые враги
        aliveEnemies.Clear();
        aliveEnemies.AddRange(EnemyRegistry.GetAliveEnemies());

        // Пул точек
        EnsureDotCount(aliveEnemies.Count);

        Vector3 playerPos = playerTransform.position;

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            var enemy = aliveEnemies[i];
            var dot = dotPool[i];

            Vector3 delta = enemy.transform.position - playerPos;
            Vector2 delta2D = new Vector2(delta.x, delta.y);
            float dist = delta2D.magnitude;

            bool distant=dist>radarRangeWorld;
            Vector2 pixelOffset=ProjectOffset(delta2D,radarRangeWorld,radarRadiusPixels);

            dot.rectTransform.anchoredPosition = pixelOffset;
            dot.sprite=distant?triangle:pointSprite;
            dot.type=Image.Type.Simple;
            dot.rectTransform.sizeDelta=distant?new Vector2(12,14):enemyDotPrefab.rectTransform.sizeDelta;
            dot.rectTransform.localRotation=Quaternion.Euler(0,0,distant?Mathf.Atan2(delta2D.y,delta2D.x)*Mathf.Rad2Deg-90:0);
            var weapon=playerTransform.GetComponent<ShipWeapon>();
            bool selected=weapon!=null&&(weapon.PendingTarget!=null?weapon.PendingTarget:weapon.Target)==enemy;
            dot.color = selected?new Color(1,.9f,.45f):enemyDotColor;
            var outline=dot.GetComponent<Outline>();outline.enabled=selected;outline.effectColor=Color.white;
            if(selected)dot.transform.SetAsLastSibling();
            dot.gameObject.SetActive(true);
        }

        // Скрываем лишние
        for (int i = aliveEnemies.Count; i < dotPool.Count; i++)
        {
            dotPool[i].gameObject.SetActive(false);
        }
    }

    private void EnsureDotCount(int count)
    {
        while (dotPool.Count < count)
        {
            var dot = Instantiate(enemyDotPrefab, dotsContainer);
            dot.raycastTarget=false;CombatUIStyle.Ensure<Outline>(dot.gameObject).enabled=false;
            dot.gameObject.SetActive(false);
            dotPool.Add(dot);
        }
    }
}