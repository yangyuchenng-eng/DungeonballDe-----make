using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    public enum DoorType { Normal, Locked }

    [Header("Door")]
    public DoorType doorType = DoorType.Normal;

    [Tooltip("真正的门对象（墙体缩放那块）。触发器是子物体时一定要拖这个")]
    public GameObject doorRoot;

    [Tooltip("门的实体Collider（不填就自动从 doorRoot 找）")]
    public Collider solidCollider;

    [Tooltip("门的可视对象（不填就用 doorRoot）")]
    public GameObject visualRoot;

    [Header("Normal Door (Ball)")]
    public bool requirePlayerThrown = true;   
    public bool allowKeyAsBall = false;       

    [Header("Locked Door (Key)")]
    public string requiredKeyId = "MainDoor"; 

    [Header("Open Behavior")]
    public bool destroyDoorRoot = true;        
    public GameObject openVfxPrefab;

    private void Awake()
    {
        
        var triggerCol = GetComponent<Collider>();
        if (triggerCol != null) triggerCol.isTrigger = true;

        if (doorRoot == null) doorRoot = transform.root.gameObject;

        if (visualRoot == null) visualRoot = doorRoot;

        if (solidCollider == null && doorRoot != null)
            solidCollider = doorRoot.GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (doorType == DoorType.Locked)
        {
            TryOpenLocked(other);
        }
        else
        {
            TryOpenNormal(other);
        }
    }

    void TryOpenNormal(Collider other)
    {
        
        var item = other.GetComponentInParent<PickupItem>();
        if (item == null) return;

        
        bool isKey = item.GetComponent<KeyItem>() != null || item.GetComponentInChildren<KeyItem>() != null;
        if (isKey && !allowKeyAsBall) return;

        if (requirePlayerThrown && !item.wasThrownByPlayer) return;

        OpenDoor();
        
        item.wasThrownByPlayer = false;
    }

    void TryOpenLocked(Collider other)
    {
        
        KeyItem key = other.GetComponent<KeyItem>() ?? other.GetComponentInParent<KeyItem>();
        if (key == null) return;

        if (key.keyId != requiredKeyId) return;

        OpenDoor();

       
        Destroy(key.gameObject);
    }

    void OpenDoor()
    {
        if (openVfxPrefab != null)
            Instantiate(openVfxPrefab, transform.position, Quaternion.identity);

        
        if (solidCollider != null) solidCollider.enabled = false;

       
        if (visualRoot != null) visualRoot.SetActive(false);

        if (destroyDoorRoot && doorRoot != null)
            Destroy(doorRoot);
    }
}
