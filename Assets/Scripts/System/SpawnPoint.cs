using System.Collections;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public enum SpawnMode { Ground, Air }

    [Header("Spawn Config")]
    public GameObject enemyPrefab;
    public int count = 3;
    public float interval = 1f;
    public float startDelay = 0f;

    [Header("Mode")]
    public SpawnMode mode = SpawnMode.Ground;

    [Header("Floor Reference (NO layers needed)")]
    [Tooltip("Drag the ROOM FLOOR collider here (BoxCollider/MeshCollider ok).")]
    public Collider floorCollider;

    [Tooltip("Ray origin height above floorCollider.bounds.max.y. (NOT above spawnpoint)")]
    public float rayStartHeight = 2f;

    [Tooltip("Max ray distance downward.")]
    public float rayDistance = 30f;

    [Tooltip("Extra lift above ground after snapping (small).")]
    public float groundYOffset = 0.02f;

    [Header("Air")]
    [Tooltip("Flying enemies height above ground.")]
    public float airHeight = 2.0f;

    [Header("Rotation")]
    public bool faceForward = true;

  
    private RoomEncounter owner;

    public void Init(RoomEncounter encounter) => owner = encounter;

    public void StartSpawning(MonoBehaviour runner)
    {
        if (runner == null) return;

        if (enemyPrefab == null || count <= 0)
        {
            owner?.NotifyAllSpawnsFinished();
            return;
        }

        runner.StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < count; i++)
        {
            bool hasGround;
            float groundY;
            Vector3 spawnBase = GetSpawnBasePositionXZ(out groundY, out hasGround);
            Quaternion spawnRot = GetSpawnRotation();

            
            GameObject go = Instantiate(enemyPrefab, spawnBase, spawnRot);

            
            if (hasGround)
            {
                if (mode == SpawnMode.Ground)
                {
                    
                    LiftObjectToGround(go, groundY + groundYOffset);
                }
                else
                {
                    
                    Vector3 p = go.transform.position;
                    go.transform.position = new Vector3(p.x, groundY + airHeight, p.z);
                }
            }
            else
            {
               
                if (mode == SpawnMode.Air)
                {
                    Vector3 p = go.transform.position;
                    go.transform.position = new Vector3(p.x, p.y + airHeight, p.z);
                }
            }

           
            BatEnemy bat = go.GetComponent<BatEnemy>();
            if (bat != null)
            {
                bat.SetSpawnHeight(go.transform.position.y);
            }

           
            if (owner != null)
            {
                var link = go.GetComponent<SpawnedEnemyLink>();
                if (link == null) link = go.AddComponent<SpawnedEnemyLink>();
                link.Bind(owner);
                owner.NotifyEnemySpawned(link);
            }

            if (interval > 0f)
                yield return new WaitForSeconds(interval);
        }

        owner?.NotifyAllSpawnsFinished();
    }

   
    private Vector3 GetSpawnBasePositionXZ(out float groundY, out bool hasGroundY)
    {
        Vector3 basePos = transform.position;

        groundY = basePos.y;
        hasGroundY = false;

        if (floorCollider == null)
        {
           
            return basePos;
        }

        
        float originY = floorCollider.bounds.max.y + Mathf.Max(0.1f, rayStartHeight);
        Vector3 origin = new Vector3(basePos.x, originY, basePos.z);
        Ray ray = new Ray(origin, Vector3.down);

        if (floorCollider.Raycast(ray, out RaycastHit hit, Mathf.Max(0.1f, rayDistance)))
        {
            groundY = hit.point.y;
            hasGroundY = true;
            return new Vector3(basePos.x, groundY, basePos.z);
        }

      
        groundY = floorCollider.bounds.max.y;
        hasGroundY = false;
        return new Vector3(basePos.x, groundY, basePos.z);
    }

    public Quaternion GetSpawnRotation()
    {
        return faceForward ? transform.rotation : Quaternion.identity;
    }

  
    private void LiftObjectToGround(GameObject go, float targetGroundY)
    {
        if (go == null) return;

        var cols = go.GetComponentsInChildren<Collider>();
        bool found = false;
        float minY = float.PositiveInfinity;

        foreach (var c in cols)
        {
            if (c == null || c.isTrigger) continue;
            float y = c.bounds.min.y;
            if (y < minY)
            {
                minY = y;
                found = true;
            }
        }

       
        if (!found)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                if (r == null) continue;
                float y = r.bounds.min.y;
                if (y < minY)
                {
                    minY = y;
                    found = true;
                }
            }
        }

        if (!found) return;

        float delta = targetGroundY - minY;

     
        if (delta > 0f)
        {
            go.transform.position += Vector3.up * delta;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = (mode == SpawnMode.Air) ? Color.cyan : Color.green;

        float gy; bool has;
        Vector3 p = GetSpawnBasePositionXZ(out gy, out has);

        if (has)
        {
            if (mode == SpawnMode.Air)
                p = new Vector3(p.x, gy + airHeight, p.z);
            else
                p = new Vector3(p.x, gy + groundYOffset, p.z);
        }

        Gizmos.DrawWireSphere(p, 0.15f);
    }
#endif
}
