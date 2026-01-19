using UnityEngine;


[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class SmartBounceBall : MonoBehaviour
{
    [Header("Layer-based classification")]
    [Tooltip("被视为“地板”的 Layer（例如 Floor、Ground），优先级最高")]
    public LayerMask floorLayers;

    [Tooltip("被视为“墙/障碍物”的 Layer（例如 Wall），优先级第二")]
    public LayerMask wallLayers;

    [Header("Wall bounce settings")]
    [Tooltip("撞墙后的速度倍率（1 = 完全弹性，<1 = 逐渐减速）")]
    public float wallBounceMultiplier = 0.9f;

    [Tooltip("撞墙时竖直方向速度的保留比例（0 = 只保留水平，1 = 完全保留原来的竖直分量）")]
    public float wallVerticalKeep = 0.3f;

    [Header("Floor behaviour")]
    [Tooltip("撞地板时，水平速度乘以这个系数（<1 = 减速，0 = 直接停住）")]
    public float floorHorizontalDamping = 0.6f;

    [Tooltip("如果撞地板后速度低于这个值，就直接停住")]
    public float minSpeedToStop = 1.0f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (rb == null) return;

        Vector3 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.0001f) return; 
        float volMul = Mathf.Clamp01(v.magnitude / 20f);
        if (volMul > 0.05f)
            AudioManager.I?.PlayBounce(volMul);

        GameObject other = collision.gameObject;
        int otherLayer = other.layer;

       
        if (IsInLayerMask(otherLayer, floorLayers))
        {
            HandleFloorHit(v);
            return;
        }

        
        if (IsInLayerMask(otherLayer, wallLayers))
        {
            Vector3 normal = GetAverageNormal(collision);
            HandleWallHit(v, normal);
            return;
        }

       
        Vector3 avgNormal = GetAverageNormal(collision);
        if (avgNormal == Vector3.zero) return;

        float upDot = Vector3.Dot(avgNormal, Vector3.up);

        
        if (upDot > 0.7f)
        {
            HandleFloorHit(v);
        }
        else
        {
            HandleWallHit(v, avgNormal);
        }
    }

    bool IsInLayerMask(int layer, LayerMask mask)
    {
        if (mask.value == 0) return false;
        return (mask.value & (1 << layer)) != 0;
    }

    Vector3 GetAverageNormal(Collision collision)
    {
        Vector3 avgNormal = Vector3.zero;
        int count = collision.contactCount;

        for (int i = 0; i < count; i++)
        {
            avgNormal += collision.GetContact(i).normal;
        }

        if (count > 0)
        {
            avgNormal /= count;
            avgNormal.Normalize();
        }

        return avgNormal;
    }

    void HandleFloorHit(Vector3 v)
    {
       
        Vector3 horizontal = new Vector3(v.x, 0f, v.z) * floorHorizontalDamping;
        float speed = horizontal.magnitude;

        if (speed < minSpeedToStop)
        {
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            rb.linearVelocity = horizontal;
        }
    }

    void HandleWallHit(Vector3 v, Vector3 normal)
    {
        if (normal == Vector3.zero)
        {
           
            rb.linearVelocity = v * wallBounceMultiplier;
            return;
        }

        
        Vector3 reflected = Vector3.Reflect(v, normal);

        
        reflected *= wallBounceMultiplier;

       
        reflected.y = reflected.y * wallVerticalKeep;

        rb.linearVelocity = reflected;
    }
}
