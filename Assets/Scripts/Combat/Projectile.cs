using UnityEngine;

public sealed class Projectile : MonoBehaviour, IPoolable
{
    [SerializeField] private float maxLifetime = 4f;

    private Enemy target;
    private float speed;
    private float damage;
    private float splashRadius;
    private float slowMultiplier;
    private float slowDuration;
    private PoolMember poolMember;
    private float lifetime;

    private void Awake()
    {
        poolMember = GetComponent<PoolMember>();
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f || target == null || !target.gameObject.activeInHierarchy)
        {
            ReleaseToPool();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target.transform.position) <= 0.08f)
        {
            Hit();
        }
    }

    public void Initialize(Enemy newTarget, float projectileSpeed, float projectileDamage, float areaRadius, float slow, float slowTime)
    {
        target = newTarget;
        speed = projectileSpeed;
        damage = projectileDamage;
        splashRadius = areaRadius;
        slowMultiplier = slow;
        slowDuration = slowTime;
        lifetime = maxLifetime;
    }

    public void OnTakenFromPool()
    {
        lifetime = maxLifetime;
    }

    public void OnReturnedToPool()
    {
        target = null;
    }

    private void Hit()
    {
        if (splashRadius > 0.01f)
        {
            DamageArea();
        }
        else
        {
            ApplyEffects(target);
        }

        ReleaseToPool();
    }

    private void DamageArea()
    {
        for (int i = EnemyRegistry.ActiveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = EnemyRegistry.ActiveEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (Vector3.Distance(transform.position, enemy.transform.position) <= splashRadius)
            {
                ApplyEffects(enemy);
            }
        }
    }

    private void ApplyEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);
        if (slowDuration > 0f)
        {
            enemy.ApplySlow(slowMultiplier, slowDuration);
        }
    }

    private void ReleaseToPool()
    {
        if (poolMember == null)
        {
            poolMember = GetComponent<PoolMember>();
        }

        poolMember.Release();
    }
}
