using UnityEngine;


[RequireComponent(typeof(PickupItem))]
public class BallDamage : MonoBehaviour
{
    [Header("Damage")]
    public float damageToEnemies = 999f; 
    public float damageToPlayer = 20f;  

    [Header("Who is this ball hostile to?")]
    public bool damagesEnemies = true;
    public bool damagesPlayer = false;   

    public bool isEnemyProjectile = false; 
}

