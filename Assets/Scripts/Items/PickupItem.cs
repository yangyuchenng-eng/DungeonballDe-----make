using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Basic")]
    public string itemName = "Item";

    [Header("Pickup Settings")]
    public bool isPickupable = true;   
    public bool holdable = true;      

    [Header("Throw Settings")]
    public bool throwable = true;              
    public bool dealsDamageWhenThrown = true;  
    public int damageAmount = 1;

    [Header("Key Settings")]
    public bool isKey = false;
    public string keyID;   

    
    [HideInInspector] public bool isHeld = false;
    [HideInInspector] public bool wasThrownByPlayer = false;

    void OnEnable()
    {
        
        PickupItemManager manager = PickupItemManager.Instance;
        if (manager != null)
        {
            manager.RegisterPickupItem(this);
        }
    }

    void OnDisable()
    {
        
        PickupItemManager manager = PickupItemManager.Instance;
        if (manager != null)
        {
            manager.UnregisterPickupItem(this);
        }
    }
}
