using System;
using UnityEngine;

public sealed class Enemy : MonoBehaviour, IPoolable
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform healthBarFill;

    private EnemyData data;
    private WaypointPath path;
    private Action<Enemy> reachedBase;
    private Action<Enemy, int> killed;
    private PoolMember poolMember;
    private int waypointIndex;
    private float currentHealth;
    private float slowTimer;
    private float slowMultiplier = 1f;
    private bool isAlive;

    public float Health => currentHealth;
    public float MaxHealth => data != null ? data.maxHealth : 1f;
    public int RewardGold => data != null ? data.rewardGold : 0;
    public int BaseDamage => data != null ? data.baseDamage : 1;
    public float PathProgress => waypointIndex + SegmentProgress();

    private void Awake()
    {
        poolMember = GetComponent<PoolMember>();
    }

    private void Update()
    {
        if (!isAlive || path == null || path.Count == 0)
        {
            return;
        }

        UpdateSlow();
        MoveAlongPath();
    }

    public void Initialize(EnemyData enemyData, WaypointPath waypointPath, Action<Enemy> onReachedBase, Action<Enemy, int> onKilled)
    {
        data = enemyData;
        path = waypointPath;
        reachedBase = onReachedBase;
        killed = onKilled;
        waypointIndex = 0;
        currentHealth = data.maxHealth;
        slowTimer = 0f;
        slowMultiplier = 1f;
        isAlive = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = data.sprite;
        }

        transform.position = path.StartPosition;
        RefreshHealthBar();
        EnemyRegistry.Register(this);
    }

    public void TakeDamage(float damage)
    {
        if (!isAlive)
        {
            return;
        }

        currentHealth -= damage;
        RefreshHealthBar();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        if (data != null && data.ignoresSlow)
        {
            return;
        }

        slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    public void OnTakenFromPool()
    {
        isAlive = false;
    }

    public void OnReturnedToPool()
    {
        EnemyRegistry.Unregister(this);
        isAlive = false;
    }

    private void MoveAlongPath()
    {
        if (waypointIndex >= path.Count)
        {
            ReachBase();
            return;
        }

        Vector3 target = path[waypointIndex];
        float speed = data.moveSpeed * slowMultiplier;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) <= 0.01f)
        {
            waypointIndex++;
            if (waypointIndex >= path.Count)
            {
                ReachBase();
            }
        }
    }

    private void UpdateSlow()
    {
        if (slowTimer <= 0f)
        {
            slowMultiplier = 1f;
            return;
        }

        slowTimer -= Time.deltaTime;
        if (slowTimer <= 0f)
        {
            slowMultiplier = 1f;
        }
    }

    private void Die()
    {
        isAlive = false;
        EnemyRegistry.Unregister(this);
        killed?.Invoke(this, RewardGold);
        ReleaseToPool();
    }

    private void ReachBase()
    {
        isAlive = false;
        EnemyRegistry.Unregister(this);
        reachedBase?.Invoke(this);
        ReleaseToPool();
    }

    private float SegmentProgress()
    {
        if (path == null || waypointIndex <= 0 || waypointIndex >= path.Count)
        {
            return 0f;
        }

        float segmentLength = Vector3.Distance(path[waypointIndex - 1], path[waypointIndex]);
        if (segmentLength <= 0.001f)
        {
            return 0f;
        }

        float remaining = Vector3.Distance(transform.position, path[waypointIndex]);
        return Mathf.Clamp01(1f - remaining / segmentLength);
    }

    private void RefreshHealthBar()
    {
        if (healthBarFill == null)
        {
            return;
        }

        float normalized = Mathf.Clamp01(currentHealth / MaxHealth);
        healthBarFill.localScale = new Vector3(normalized, 1f, 1f);
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
