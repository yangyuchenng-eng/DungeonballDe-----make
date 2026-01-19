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

            AudioManager.I?.PlayPotionUse();
        }
        Destroy(gameObject);
    }
}
