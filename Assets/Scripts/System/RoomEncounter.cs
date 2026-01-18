using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomEncounter : MonoBehaviour
{
    [Header("Room")]
    public bool isFinalRoom = false;

    [Tooltip("把本房间所有刷怪点(SpawnPoint)拖进来，或放在子物体上自动找")]
    public SpawnPoint[] spawnPoints;

    [Header("Floor Injection (推荐)")]
    [Tooltip("把本房间地板的 Collider 拖进来。会自动分发给所有 SpawnPoint.floorCollider（地面/飞行都用）")]
    public Collider roomFloorCollider;

    [Header("Activation")]
    public string playerTag = "Player";
    public bool startOnce = true;

    private bool started = false;

    // 统计
    private int aliveSpawnedEnemies = 0;
    private int pendingSpawnPoints = 0;
    private bool allSpawnsFinished = false;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (spawnPoints == null || spawnPoints.Length == 0)
            spawnPoints = GetComponentsInChildren<SpawnPoint>(true);

        foreach (var sp in spawnPoints)
        {
            if (sp == null) continue;

            sp.Init(this);

            // ✅ 关键：把房间地板 collider 自动给 SpawnPoint
            // 这样 SpawnPoint 的 Y 就完全由 floorCollider 决定，SpawnPoint 自己的 y 不再影响
            if (sp.floorCollider == null && roomFloorCollider != null)
                sp.floorCollider = roomFloorCollider;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (startOnce && started) return;
        if (!other.CompareTag(playerTag)) return;

        started = true;
        StartRoom();
    }

    public void StartRoom()
    {
        if (spawnPoints == null) return;

        allSpawnsFinished = false;
        pendingSpawnPoints = spawnPoints.Length;

        // 如果一个房间没有任何 spawnPoints，也应该立刻算“刷完”
        if (pendingSpawnPoints == 0)
        {
            allSpawnsFinished = true;
            return;
        }

        foreach (var sp in spawnPoints)
        {
            if (sp == null)
            {
                NotifyAllSpawnsFinished();
                continue;
            }

            sp.StartSpawning(this);
        }
    }

    // --- 给 SpawnPoint / SpawnedEnemyLink 调用 ---
    public void NotifyEnemySpawned(SpawnedEnemyLink link)
    {
        aliveSpawnedEnemies++;
    }

    public void NotifyEnemyDied(SpawnedEnemyLink link)
    {
        aliveSpawnedEnemies = Mathf.Max(0, aliveSpawnedEnemies - 1);
    }

    public void NotifyAllSpawnsFinished()
    {
        pendingSpawnPoints = Mathf.Max(0, pendingSpawnPoints - 1);
        if (pendingSpawnPoints == 0)
            allSpawnsFinished = true;
    }

    public bool IsCleared()
    {
        // 全部刷完 + 存活为 0 才算房间清完
        return allSpawnsFinished && aliveSpawnedEnemies == 0;
    }

    public bool HasStarted() => started;
}
