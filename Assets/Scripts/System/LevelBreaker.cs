using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelBreaker : MonoBehaviour
{
    [Header("Break")]
    public int hp = 1;
    public bool requirePlayerThrown = true;

    [Header("Layer Filter")]
    public string allowedLayerA = "Pickup";
    public string allowedLayerB = "Key"; // 玩家也可能用钥匙砸碎（你想禁止就删掉 Key）

    [Header("VFX Optional")]
    public GameObject breakVfxPrefab;

    private int layerA;
    private int layerB;
    private LevelManager levelManager;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        layerA = LayerMask.NameToLayer(allowedLayerA);
        layerB = LayerMask.NameToLayer(allowedLayerB);

        levelManager = FindFirstObjectByType<LevelManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponentInParent<PickupItem>();
        if (item == null) return;

        // layer 过滤
        int l = item.gameObject.layer;
        bool okLayer = (layerA == -1 || l == layerA) || (layerB != -1 && l == layerB);
        if (!okLayer) return;

        if (requirePlayerThrown && !item.wasThrownByPlayer) return;

        hp--;

        // ✅ 目标物/关卡触发器受击音效（有效命中时播）
        AudioManager.I?.PlayObjectiveHit();

        if (hp <= 0)
        {
            if (breakVfxPrefab != null)
                Instantiate(breakVfxPrefab, transform.position, Quaternion.identity);

            if (levelManager != null)
                levelManager.NotifyBreakerDestroyed(this);

            Destroy(gameObject);
        }

        // 防止一个投掷物连续触发多个碎片（可选）
        item.wasThrownByPlayer = false;
    }
}
