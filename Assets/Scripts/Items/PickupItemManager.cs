using System.Collections.Generic;
using UnityEngine;


public class PickupItemManager : MonoBehaviour
{
    public static PickupItemManager Instance { get; private set; }

    private List<PickupItem> pickupItems = new List<PickupItem>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    
    public void RegisterPickupItem(PickupItem item)
    {
        if (item != null && !pickupItems.Contains(item))
        {
            pickupItems.Add(item);
        }
    }

    public void UnregisterPickupItem(PickupItem item)
    {
        if (item != null)
        {
            pickupItems.Remove(item);
        }
    }

    
    public PickupItem[] GetAllPickupItems()
    {
        return pickupItems.ToArray();
    }

   
    public int GetPickupItemCount()
    {
        return pickupItems.Count;
    }
}
