using UnityEngine;

/// <summary>
/// 敌人瞄准攻击的工具类，提供共用的瞄准和发射逻辑
/// </summary>
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

        // ★新增：如果有 BallDamage，默认标记为敌人投射物
        BallDamage bd = projectile.GetComponent<BallDamage>();
        if (bd != null)
        {
            bd.isEnemyProjectile = true;
            bd.damagesPlayer = true;
            bd.damagesEnemies = false;
        }

        // 如果投射物有 ThrowableBall，优先使用它（会禁用 collider，用 cast 命中）
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
