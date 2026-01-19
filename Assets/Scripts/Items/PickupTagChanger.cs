using UnityEngine;


public class PickupTagChanger : MonoBehaviour
{
    [Header("Tag Settings")]
    [Tooltip("物体初始的 Tag")]
    public string initialTag = "rock";

    [Tooltip("物体被捡起后的 Tag")]
    public string pickedUpTag = "fireball";

    private PickupItem pickupItem;
    private bool wasPickedUp = false;

    void Awake()
    {
        pickupItem = GetComponent<PickupItem>();
    }

    void Update()
    {
        if (pickupItem == null) return;

        
        if (pickupItem.isHeld && !wasPickedUp)
        {
          
            gameObject.tag = pickedUpTag;
            wasPickedUp = true;
        }
        
    }
}
