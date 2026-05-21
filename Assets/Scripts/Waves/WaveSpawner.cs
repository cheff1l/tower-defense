using System.Collections;
using UnityEngine;

public sealed class WaveSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPool enemyPool;
    [SerializeField] private WaypointPath path;
    [SerializeField] private AiWavePlanner aiWavePlanner;
    [SerializeField] private GameBalance balance;

    private int aliveEnemies;
    private bool spawning;

    public bool IsBusy => spawning || aliveEnemies > 0;

    public void StartAiWave(int roundNumber, int budget)
    {
        AttackWave wave = aiWavePlanner.CreateWave(roundNumber, budget);
        StartCoroutine(SpawnWave(wave));
    }

    private IEnumerator SpawnWave(AttackWave wave)
    {
        spawning = true;
        foreach (EnemyData enemyData in wave.Enemies)
        {
            GameObject enemyObject = enemyPool.Get(path.StartPosition, Quaternion.identity);
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogError("Enemy prefab must include Enemy component.");
                enemyObject.GetComponent<PoolMember>().Release();
                continue;
            }

            aliveEnemies++;
            enemy.Initialize(enemyData, path, HandleEnemyReachedBase, HandleEnemyKilled);
            yield return new WaitForSeconds(balance.spawnInterval);
        }

        spawning = false;
    }

    private void HandleEnemyReachedBase(Enemy enemy)
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        GameManager.Instance.ApplyBaseDamage(enemy.BaseDamage);
    }

    private void HandleEnemyKilled(Enemy enemy, int rewardGold)
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        GameManager.Instance.AddGold(rewardGold);
    }
}
