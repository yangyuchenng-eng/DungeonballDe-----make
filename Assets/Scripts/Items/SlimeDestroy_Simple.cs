using UnityEngine;


public class SlimeDestroy : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("玩家的 Tag")]
    public string playerTag = "Player";

    private PickupItem pickupItem;

    void Awake()
    {
        pickupItem = GetComponent<PickupItem>();
    }

    void OnCollisionEnter(Collision collision)
    {
        TryDestroy(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        TryDestroy(other.gameObject);
    }

    void TryDestroy(GameObject hitObject)
    {
        
        if (!gameObject.CompareTag("fireball"))
        {
            return;
        }

        
        if (pickupItem != null && pickupItem.isHeld)
        {
            return;
        }

       
        if (hitObject.CompareTag(playerTag))
        {
            return;
        }

       
        Destroy(gameObject);
    }
}
