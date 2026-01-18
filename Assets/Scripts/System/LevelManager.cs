using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Scene Flow")]
    public string nextSceneName; // Level_02 / Level_03 / WinScene

    [Header("Final Room")]
    public RoomEncounter finalRoomEncounter;

    [Tooltip("最终房间的所有通关器（LevelBreaker）手动拖，或留空自动找")]
    public List<LevelBreaker> breakers = new List<LevelBreaker>();

    [Header("Portal")]
    public GameObject portalPrefab;
    public Transform portalSpawnPoint;
    public bool spawnPortalInactiveThenEnable = false;

    private int totalBreakers = 0;
    private int destroyedBreakers = 0;
    private bool portalSpawned = false;

    void Start()
    {
        if (finalRoomEncounter == null)
        {
            // 允许你不拖，自动找一个 isFinalRoom 的
            var rooms = FindObjectsByType<RoomEncounter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var r in rooms)
                if (r != null && r.isFinalRoom) { finalRoomEncounter = r; break; }
        }

        if (breakers == null || breakers.Count == 0)
        {
            var found = FindObjectsByType<LevelBreaker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            breakers = new List<LevelBreaker>(found);
        }

        totalBreakers = 0;
        foreach (var b in breakers)
            if (b != null) totalBreakers++;

        destroyedBreakers = 0;
    }

    void Update()
    {
        if (portalSpawned) return;

        bool breakersDone = (destroyedBreakers >= totalBreakers) && totalBreakers > 0;
        bool finalRoomCleared = (finalRoomEncounter != null && finalRoomEncounter.HasStarted() && finalRoomEncounter.IsCleared());

        if (breakersDone && finalRoomCleared)
        {
            SpawnOrEnablePortal();
        }
    }

    public void NotifyBreakerDestroyed(LevelBreaker breaker)
    {
        destroyedBreakers++;
        // Debug.Log($"[LevelManager] Breakers: {destroyedBreakers}/{totalBreakers}");
    }

    void SpawnOrEnablePortal()
    {
        portalSpawned = true;

        if (portalPrefab == null || portalSpawnPoint == null)
        {
            Debug.LogWarning("[LevelManager] portalPrefab 或 portalSpawnPoint 未设置。");
            return;
        }

        var portal = Instantiate(portalPrefab, portalSpawnPoint.position, portalSpawnPoint.rotation);
        var lp = portal.GetComponent<LevelPortal>();
        if (lp != null) lp.nextSceneName = nextSceneName;

        if (spawnPortalInactiveThenEnable)
            portal.SetActive(true);
    }
}
