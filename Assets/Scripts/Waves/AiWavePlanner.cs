using System.Collections.Generic;
using UnityEngine;

public sealed class AiWavePlanner : MonoBehaviour
{
    [SerializeField] private GameBalance balance;

    public AttackWave CreateWave(int roundNumber, int budget)
    {
        AttackWave wave = new AttackWave();
        if (balance == null || balance.enemies == null || balance.enemies.Length == 0)
        {
            return wave;
        }

        int safety = balance.maxEnemiesPerWave;
        while (safety > 0)
        {
            EnemyData enemy = PickEnemy(roundNumber, budget - wave.TotalCost);
            if (enemy == null || !wave.TryAdd(enemy, budget))
            {
                break;
            }

            safety--;
        }

        return wave;
    }

    private EnemyData PickEnemy(int roundNumber, int remainingBudget)
    {
        List<EnemyData> options = new List<EnemyData>();
        foreach (EnemyData enemy in balance.enemies)
        {
            if (enemy != null && enemy.attackCost <= remainingBudget)
            {
                options.Add(enemy);
            }
        }

        if (options.Count == 0)
        {
            return null;
        }

        int highTierChance = Mathf.Clamp(roundNumber * 8, 0, 70);
        if (Random.Range(0, 100) < highTierChance)
        {
            options.Sort((a, b) => b.attackCost.CompareTo(a.attackCost));
            return options[0];
        }

        return options[Random.Range(0, options.Count)];
    }
}
