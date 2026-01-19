using UnityEngine;


[RequireComponent(typeof(Collider))]
public class EnemyTouchDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damagePerHit = 10;          
    public float damageInterval = 0.5f;    

    [Header("Target")]
    public string playerTag = "Player";     

    [Header("Tag Check")]
    [Tooltip("只有当这个敌人的 Tag 是以下列表中的某一个时，才会造成伤害。留空表示不检查 Tag。")]
    public string[] allowedTags = new string[] { "slime" };  

    private PlayerHealth playerHealthInRange;
    private float nextDamageTime = 0f;

    void Awake()
    {
       
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
       
        if (!IsAllowedToDealtDamage())
        {
            return;
        }

        if (!other.CompareTag(playerTag)) return;

       
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null)
        {
            ph = other.GetComponentInParent<PlayerHealth>();
        }

        if (ph != null)
        {
            playerHealthInRange = ph;
            
            nextDamageTime = Time.time;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>()
                          ?? other.GetComponentInParent<PlayerHealth>();

        if (ph == playerHealthInRange)
        {
            playerHealthInRange = null;
        }
    }

    void Update()
    {
        
        if (!IsAllowedToDealtDamage())
        {
            
            playerHealthInRange = null;
            return;
        }

        if (playerHealthInRange == null) return;

        if (Time.time >= nextDamageTime)
        {
            playerHealthInRange.TakeDamage(damagePerHit);
            nextDamageTime = Time.time + damageInterval;
        }
    }

   
    
    bool IsAllowedToDealtDamage()
    {
        if (allowedTags == null || allowedTags.Length == 0) return true;

        
        Transform root = transform.root;

        foreach (string allowedTag in allowedTags)
        {
            if (root != null && root.CompareTag(allowedTag)) return true;
        }
        return false;
    }

}
