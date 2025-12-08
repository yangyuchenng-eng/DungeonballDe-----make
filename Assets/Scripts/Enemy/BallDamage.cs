using UnityEngine;

// 挂在球上，用来标记这个球会对谁造成伤害。
[RequireComponent(typeof(PickupItem))]
public class BallDamage : MonoBehaviour
{
    [Header("Damage")]
    public float damageToEnemies = 999f; // 对敌人：基础球一击必杀
    public float damageToPlayer = 20f;   // 对玩家的伤害（以后红色敌人球启用）

    [Header("Who is this ball hostile to?")]
    public bool damagesEnemies = true;
    public bool damagesPlayer = false;   // 现在先关掉，以后敌人球再开

    public bool isEnemyProjectile = false; // 以后敌人发射球时设 true
}

