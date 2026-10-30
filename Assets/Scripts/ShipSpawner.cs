using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спавнит врагов волнами. Волна 1 — 1 враг, волна 2 — 2 врага, волна 3 — 3 врага.
/// Спавнит за пределами экрана, враги сами прилетают (EnemyAI ведёт их к игроку).
/// </summary>
public class ShipSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Префаб врага (EnemyShip).")]
    public GameObject enemyPrefab;

    [Header("Waves")]
    [Tooltip("Количество врагов в каждой волне. Порядок: 1, 2, 3.")]
    public int[] waveSizes = new int[] { 1, 2, 3 };
    public bool useCombatArchetypes;
    public ShipDefinition interceptorDefinition;
    public ShipDefinition cobraDefinition;
    public static CombatDemoLoadouts.Role RoleForWave(int wave,int index)
    {
        if(wave==0)return CombatDemoLoadouts.Role.Interceptor;
        if(wave==1)return CombatDemoLoadouts.Role.Artillery;
        if(wave==2)return CombatDemoLoadouts.Role.Escort;
        return index==0?CombatDemoLoadouts.Role.Interceptor:CombatDemoLoadouts.Role.Escort;
    }

    [Tooltip("Пауза между волнами (сек).")]
    public float pauseBetweenWaves = 3f;

    [Header("Spawn Position")]
    [Tooltip("Радиус спавна от игрока. Должен быть больше половины видимой области.")]
    public float spawnRadius = 14f;

    [Tooltip("Игрок (если null — ищется по тегу 'Player').")]
    public Transform playerTransform;

    [Header("References")]
    public CombatManager combatManager;

    private bool wavesStarted;
    private int currentWaveIndex = 0;
    private readonly List<ShipHealth> spawnedEnemies = new List<ShipHealth>();

    private void Start()
    {
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[ShipSpawner] enemyPrefab не назначен.");
            return;
        }

        // НЕ запускаем бой автоматически.
        // Бой стартует по вызову StartCombat() — кнопкой "В бой".
        Debug.Log("[ShipSpawner] Готов. Ожидание команды StartCombat().");
    }

    /// <summary>
    /// Запускает бой. Вызывается кнопкой "В бой".
    /// </summary>
    public void StartCombat()
    {
        if (wavesStarted) return;
        wavesStarted = true;
        CombatSession.Log("Бой начат: оснащение заблокировано");
        if (GameState.Instance != null)
            GameState.Instance.StartCombat();

        StartCoroutine(SpawnWavesRoutine());
    }
    private IEnumerator SpawnWavesRoutine()
    {
        for (currentWaveIndex = 0; currentWaveIndex < waveSizes.Length; currentWaveIndex++)
        {
            int count = waveSizes[currentWaveIndex];
            Debug.Log($"[ShipSpawner] Волна {currentWaveIndex + 1} — старт, врагов {count}.");
            SpawnWave(count);
            Debug.Log($"[ShipSpawner] Волна {currentWaveIndex + 1} — заспавнена. Ждём уничтожения.");

            yield return new WaitUntil(() => AreAllWaveEnemiesDead());
            Debug.Log($"[ShipSpawner] Волна {currentWaveIndex + 1} уничтожена.");

            if (currentWaveIndex < waveSizes.Length - 1)
            {
                Debug.Log($"[ShipSpawner] Пауза {pauseBetweenWaves} сек перед волной {currentWaveIndex + 2}.");
                yield return new WaitForSeconds(pauseBetweenWaves);
            }
        }

        Debug.Log("[ShipSpawner] Все волны заспавнены и уничтожены.");

        if (combatManager != null)
            combatManager.NotifyAllWavesCleared();
    }

    private void SpawnWave(int count)
    {
        spawnedEnemies.Clear();

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetSpawnPosition(i, count);
            bool safe=false;
            float clearance=2.5f;
            var prefabCollider=enemyPrefab.GetComponentInChildren<Collider2D>();
            if(prefabCollider!=null)clearance=Mathf.Max(clearance,prefabCollider.bounds.extents.magnitude+.5f);
            for(int attempt=0;attempt<48;attempt++)
            {
                safe=true;
                foreach(var collider in Physics2D.OverlapCircleAll(pos,clearance))
                    if(collider.GetComponentInParent<Asteroid>()!=null||collider.GetComponentInParent<ShipHealth>()!=null){safe=false;break;}
                if(safe)break;
                pos=GetSpawnPosition(i+attempt+1,count);
            }
            if(!safe){Debug.LogWarning("[ShipSpawner] No safe position after 48 attempts; ship skipped.");continue;}
            GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            go.name = $"EnemyShip_W{currentWaveIndex + 1}_{i + 1}";

            var health = go.GetComponent<ShipHealth>();
            if(useCombatArchetypes && health!=null)
            {
                var role=RoleForWave(currentWaveIndex,i);
                var definition=role==CombatDemoLoadouts.Role.Interceptor?interceptorDefinition:cobraDefinition;
                var ai=go.GetComponent<EnemyAI>()??go.AddComponent<EnemyAI>();
                ai.useCombatRole=true;ai.combatRole=(Game.Core.CombatRole)(int)role;
                ai.SetOpponent(playerTransform!=null?playerTransform.GetComponent<ShipHealth>():null);
                if(go.GetComponent<ShipAbilities>()==null)go.AddComponent<ShipAbilities>();
                ShipStatsCalculator.ApplyNpc(health,definition,CombatDemoLoadouts.Create(role,definition,"npc:"+System.Guid.NewGuid().ToString("N")));
                var sprite=go.GetComponentInChildren<SpriteRenderer>();if(sprite!=null)sprite.sprite=definition.hullSprite;
                string label=role==CombatDemoLoadouts.Role.Interceptor?"Перехватчик":role==CombatDemoLoadouts.Role.Artillery?"Артиллерист":"Эскорт";
                go.name=label+" W"+(currentWaveIndex+1);CombatSession.Log(go.name);
            }
            if (health != null)
            {
                spawnedEnemies.Add(health);
                EnemyRegistry.Register(health);
                health.OnDestroyed += () => EnemyRegistry.Unregister(health);
            }
            else
            {
                Debug.LogError($"[ShipSpawner] У {go.name} нет ShipHealth!");
            }
        }
    }

    private Vector3 GetSpawnPosition(int index, int total)
    {
        Vector3 center = playerTransform != null ? playerTransform.position : Vector3.zero;
        float baseAngle = Random.Range(0f, Mathf.PI * 2f);
        float angleStep = (Mathf.PI * 2f) / Mathf.Max(1, total);
        float angle = baseAngle + angleStep * index;

        return center + new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            Mathf.Sin(angle) * spawnRadius,
            0f);
    }

    private bool AreAllWaveEnemiesDead()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            var e = spawnedEnemies[i];

            if (e == null) continue;
            if (e.Data == null) continue;
            if (e.Data.IsDestroyed) continue;
            if (!e.gameObject.activeInHierarchy) continue;

            return false;
        }

        return true;
    }

    public List<ShipHealth> GetAliveWaveEnemies()
    {
        var result = new List<ShipHealth>();
        foreach (var e in spawnedEnemies)
        {
            if (e == null) continue;
            if (e.Data == null) continue;
            if (e.Data.IsDestroyed) continue;
            if (!e.gameObject.activeInHierarchy) continue;
            result.Add(e);
        }
        return result;
    }
}