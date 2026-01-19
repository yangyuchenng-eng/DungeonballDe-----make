using UnityEngine;


[RequireComponent(typeof(Collider))]
public class DoorExit : MonoBehaviour
{
    [Header("Key")]
    [Tooltip("需要的钥匙 ID，要和 KeyItem.keyId 一致")]
    public string requiredKeyId = "MainDoor";

    [Header("Door Parts")]
    [Tooltip("门的可见模型，开门后会隐藏")]
    public GameObject doorVisual;

    [Tooltip("真正挡住玩家/球的 Collider，开门后会禁用")]
    public Collider doorCollider;

    [Header("Debug")]
    public bool debugLogs = true;

    private bool isOpened = false;

    void Awake()
    {
        
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isOpened) return;

        if (debugLogs)
        {
            Debug.Log($"[DoorExit] Trigger with {other.name}", this);
        }

      
        KeyItem key = other.GetComponent<KeyItem>()
                      ?? other.GetComponentInParent<KeyItem>();

       
        if (key == null && other.CompareTag("Player"))
        {
            key = other.GetComponentInChildren<KeyItem>();
            if (debugLogs && key != null)
            {
                Debug.Log($"[DoorExit] 在玩家子物体中找到了钥匙：{key.name}", this);
            }
        }

        if (key == null)
        {
            if (debugLogs)
            {
                Debug.Log("[DoorExit] 未找到 KeyItem，忽略这次触发。", this);
            }
            return;
        }

       
        if (key.keyId != requiredKeyId)
        {
            if (debugLogs)
            {
                Debug.Log($"[DoorExit] 找到了钥匙 {key.name}，但 keyId = {key.keyId} != {requiredKeyId}，忽略。", this);
            }
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"[DoorExit] 正确钥匙 {key.name} 碰到门，开门并触发胜利。", this);
        }

        OpenDoor();

      
        Destroy(key.gameObject);

        
        var ui = FindFirstObjectByType<GameStateUI>();
        if (ui != null)
        {
            ui.ShowWin();
        }
    }

    void OpenDoor()
    {
        if (isOpened) return;
        isOpened = true;

        if (doorVisual != null)
            doorVisual.SetActive(false);

        if (doorCollider != null)
            doorCollider.enabled = false;

        Debug.Log("[DoorExit] Door opened.");
    }
}
