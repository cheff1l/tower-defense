using System.Collections.Generic;

public static class EnemyRegistry
{
    private static readonly List<Enemy> activeEnemies = new List<Enemy>(128);

    public static IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;

    public static void Register(Enemy enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    public static void Unregister(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }
}
