using UnityEngine;

public sealed class Tower : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ObjectPool projectilePool;
    [SerializeField] private TargetPriority priority = TargetPriority.MostProgress;

    private TowerData data;
    private float cooldown;

    private void Update()
    {
        if (data == null)
        {
            return;
        }

        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
        {
            return;
        }

        Enemy target = FindTarget();
        if (target == null)
        {
            return;
        }

        Shoot(target);
        cooldown = 1f / data.fireRate;
    }

    public void Initialize(TowerData towerData, ObjectPool sharedProjectilePool)
    {
        data = towerData;
        projectilePool = sharedProjectilePool;
        cooldown = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = data.sprite;
        }
    }

    private Enemy FindTarget()
    {
        Enemy best = null;
        float bestScore = float.MinValue;

        foreach (Enemy enemy in EnemyRegistry.ActiveEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance > data.range)
            {
                continue;
            }

            float score = ScoreTarget(enemy, distance);
            if (score > bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }

    private float ScoreTarget(Enemy enemy, float distance)
    {
        switch (priority)
        {
            case TargetPriority.Nearest:
                return -distance;
            case TargetPriority.Weakest:
                return -enemy.Health;
            case TargetPriority.Strongest:
                return enemy.Health;
            default:
                return enemy.PathProgress;
        }
    }

    private void Shoot(Enemy target)
    {
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        GameObject projectileObject = projectilePool.Get(spawnPosition, Quaternion.identity);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError("Projectile prefab must include Projectile component.");
            projectileObject.GetComponent<PoolMember>().Release();
            return;
        }

        float splash = data.attackKind == TowerAttackKind.Area ? data.splashRadius : 0f;
        float slow = data.attackKind == TowerAttackKind.Slow ? data.slowMultiplier : 1f;
        float slowTime = data.attackKind == TowerAttackKind.Slow ? data.slowDuration : 0f;
        projectile.Initialize(target, data.projectileSpeed, data.damage, splash, slow, slowTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, data.range);
    }
}
