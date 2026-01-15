using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理所有场景中的可拾取物品。
/// 避免了 PlayerHands 中频繁使用 FindObjectsByType 带来的性能问题。
/// 
/// 使用方式：
/// 1. 在场景中创建一个空 GameObject，挂上这个脚本
/// 2. PickupItem 会自动在 OnEnable 时注册，OnDisable 时注销
/// 3. PlayerHands 通过 PickupItemManager.Instance.GetAllPickupItems() 获取物品列表
/// </summary>
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

    /// <summary>
    /// 注册一个可拾取物品（由 PickupItem 的 OnEnable 调用）
    /// </summary>
    public void RegisterPickupItem(PickupItem item)
    {
        if (item != null && !pickupItems.Contains(item))
        {
            pickupItems.Add(item);
        }
    }

    /// <summary>
    /// 注销一个可拾取物品（由 PickupItem 的 OnDisable 调用）
    /// </summary>
    public void UnregisterPickupItem(PickupItem item)
    {
        if (item != null)
        {
            pickupItems.Remove(item);
        }
    }

    /// <summary>
    /// 获取所有当前存活的可拾取物品数组
    /// </summary>
    public PickupItem[] GetAllPickupItems()
    {
        return pickupItems.ToArray();
    }

    /// <summary>
    /// 获取当前可拾取物品的数量
    /// </summary>
    public int GetPickupItemCount()
    {
        return pickupItems.Count;
    }
}
