using UnityEngine;

[RequireComponent(typeof(PickupItem))]
public class HealthPotionPickup : MonoBehaviour
{
    public int healAmount = 30;

    public void Consume(PlayerHealth playerHealth)
    {
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);

            // ✅ 喝药水音效（只有成功调用 Consume 才播）
            AudioManager.I?.PlayPotionUse();
        }
        Destroy(gameObject);
    }
}
