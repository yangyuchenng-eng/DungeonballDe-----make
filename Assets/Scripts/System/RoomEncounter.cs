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
      
        return allSpawnsFinished && aliveSpawnedEnemies == 0;
    }

    public bool HasStarted() => started;
}
