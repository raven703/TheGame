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

    private void Awake()
    {
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
        if (playerTransform == null || dotsContainer == null || enemyDotPrefab == null)
        {
            Debug.LogWarning($"[EnemyRadar] Ссылки не назначены: player={playerTransform != null}, dots={dotsContainer != null}, prefab={enemyDotPrefab != null}");
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

            if (dist > radarRangeWorld)
                delta2D = delta2D.normalized * radarRangeWorld;

            Vector2 pixelOffset = (delta2D / radarRangeWorld) * radarRadiusPixels;

            dot.rectTransform.anchoredPosition = pixelOffset;
            dot.color = enemyDotColor;
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
            dot.gameObject.SetActive(false);
            dotPool.Add(dot);
        }
    }
}