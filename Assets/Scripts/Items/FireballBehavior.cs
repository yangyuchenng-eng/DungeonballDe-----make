using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class FireballBehavior : MonoBehaviour
{
    private Rigidbody rb;
    private PickupItem pickupItem;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pickupItem = GetComponent<PickupItem>();
    }

    void OnCollisionEnter(Collision collision)
    {
       
        if (!gameObject.CompareTag("fireball"))
        {
            return;
        }

        
        if (pickupItem != null && pickupItem.isHeld)
        {
            return;
        }

       
        Destroy(gameObject);
    }
}
