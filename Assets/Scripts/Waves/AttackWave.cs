using System.Collections.Generic;

public sealed class AttackWave
{
    private readonly List<EnemyData> enemies = new List<EnemyData>();

    public IReadOnlyList<EnemyData> Enemies => enemies;
    public int TotalCost { get; private set; }

    public bool TryAdd(EnemyData enemy, int budget)
    {
        if (enemy == null || TotalCost + enemy.attackCost > budget)
        {
            return false;
        }

        enemies.Add(enemy);
        TotalCost += enemy.attackCost;
        return true;
    }
}
