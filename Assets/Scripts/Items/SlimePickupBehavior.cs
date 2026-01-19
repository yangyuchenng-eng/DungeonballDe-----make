using UnityEngine;


[RequireComponent(typeof(PickupItem))]
public class SlimePickupBehavior : MonoBehaviour
{
    private PickupItem pickupItem;

    void Awake()
    {
        pickupItem = GetComponent<PickupItem>();
    }

    void Update()
    {
        if (pickupItem != null && pickupItem.isHeld)
        {
          
            if (gameObject.CompareTag("slime"))
            {
                gameObject.tag = "fireball";
            }
        }
    }
}
