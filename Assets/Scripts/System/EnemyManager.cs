using System.Collections;
using UnityEngine;

// AI-assisted: structure & parts generated with ChatGPT, then adapted by the student.
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject prefab;   // 敌人预制体（例如 Enemy_Jumper）
        public float weight = 1f;   // 生成权重（以后有多种怪时用）
    }

    [Header("Spawn Settings")]
    public EnemyEntry[] enemyTypes;
    public Transform[] spawnPoints;

    [Tooltip("开局刷几只怪")]
    public int initialSpawnCount = 3;

    [Tooltip("场上同时存在的最大敌人数")]
    public int maxAliveEnemies = 5;

    [Tooltip("多久检查一次是否要刷新敌人（秒）")]
    public float spawnInterval = 2f;

    [Header("Total Spawn Limit")]
    [Tooltip("是否无限刷怪")]
    public bool endlessSpawn = true;

    [Tooltip("非无限模式下，总共最多刷多少只敌人")]
    public int totalSpawnLimit = 20;

    [Header("Key Spawn")]
    [Tooltip("击杀多少只敌人后生成钥匙")]
    public int killsForKey = 5;

    [Tooltip("要生成的钥匙预制体")]
    public GameObject keyPrefab;

    [Tooltip("钥匙出现的位置（可以是场景中的一个空物体）")]
    public Transform keySpawnPoint;

    private int totalSpawnedSoFar = 0;
    private int totalKills = 0;
    private bool keySpawned = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 开局先刷一批怪
        for (int i = 0; i < initialSpawnCount; i++)
        {
            TrySpawnOne();
        }

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            // 非无限模式，并且总共已经刷满，就停止协程
            if (!endlessSpawn && totalSpawnLimit > 0 && totalSpawnedSoFar >= totalSpawnLimit)
            {
                yield break;
            }

            // 统计当前存活敌人数量
            int alive = CountAliveEnemies();

            // 如果当前存活数 < 最大存活数，就尝试补一只
            if (alive < maxAliveEnemies)
            {
                TrySpawnOne();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    int CountAliveEnemies()
    {
        var enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        return enemies.Length;
    }

    void TrySpawnOne()
    {
        if (!endlessSpawn && totalSpawnLimit > 0 && totalSpawnedSoFar >= totalSpawnLimit)
            return;

        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            Debug.LogWarning("EnemyManager: enemyTypes 为空！");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemyManager: spawnPoints 为空！");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject prefabToSpawn = PickRandomEnemyType();
        if (prefabToSpawn == null) return;

        Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        totalSpawnedSoFar++;
    }

    GameObject PickRandomEnemyType()
    {
        if (enemyTypes.Length == 1)
            return enemyTypes[0].prefab;

        float totalWeight = 0f;
        foreach (var e in enemyTypes)
        {
            if (e.prefab == null) continue;
            totalWeight += Mathf.Max(0f, e.weight);
        }
        if (totalWeight <= 0f) return null;

        float r = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var e in enemyTypes)
        {
            if (e.prefab == null) continue;
            float w = Mathf.Max(0f, e.weight);
            cumulative += w;
            if (r <= cumulative)
                return e.prefab;
        }

        return enemyTypes[enemyTypes.Length - 1].prefab;
    }

    /// <summary>
    /// 敌人死亡时调用：增加击杀数，必要时生成钥匙。
    /// </summary>
    public void RegisterEnemyDeath(EnemyHealth enemy)
    {
        totalKills++;

        // 达到阈值、还没刷过钥匙，就刷钥匙
        if (!keySpawned && totalKills >= killsForKey)
        {
            SpawnKey();
        }
    }

    void SpawnKey()
    {
        if (keyPrefab == null || keySpawnPoint == null)
        {
            Debug.LogWarning("EnemyManager: keyPrefab 或 keySpawnPoint 未设置，无法生成钥匙。");
            return;
        }

        Instantiate(keyPrefab, keySpawnPoint.position, keySpawnPoint.rotation);
        keySpawned = true;
        Debug.Log("[EnemyManager] 击杀数达到要求，生成钥匙。");
    }
}
