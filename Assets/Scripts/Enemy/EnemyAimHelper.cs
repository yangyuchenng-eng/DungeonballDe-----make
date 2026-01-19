using UnityEngine;
public static class EnemyAimHelper
{
    public static Vector3 GetDirectionToPlayer(Vector3 firePoint, Transform player)
    {
        if (player == null) return Vector3.forward;
        return (player.position - firePoint).normalized;
    }

    public static void RotateTowardsPlayer(Transform enemy, Transform player, float rotationSpeed)
    {
        if (enemy == null || player == null) return;

        Vector3 toPlayer = player.position - enemy.position;
        toPlayer.y = 0;

        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
            enemy.rotation = Quaternion.Slerp(enemy.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public static GameObject FireProjectile(GameObject projectilePrefab, Vector3 firePoint, Vector3 direction, float speed)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[EnemyAimHelper] projectilePrefab 为空，无法发射！");
            return null;
        }

        GameObject projectile = Object.Instantiate(projectilePrefab, firePoint, Quaternion.identity);

        
        BallDamage bd = projectile.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = true;
            bd.damagesPlayer = true;
            bd.damagesEnemies = false;
        }

        
        ThrowableBall throwable = projectile.GetComponent<ThrowableBall>();
        if (throwable != null)
        {
            throwable.BeginKinematicThrow(direction, speed);
        }
        else
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = direction * speed;
        }

        return projectile;
    }
}
