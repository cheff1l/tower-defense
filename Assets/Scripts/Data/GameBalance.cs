using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Game Balance", fileName = "GameBalance")]
public sealed class GameBalance : ScriptableObject
{
    [Header("Rounds")]
    [Min(1)] public int totalRounds = 10;
    [Min(1)] public int startingBaseHp = 20;

    [Header("Defender Economy")]
    [Min(0)] public int startingGold = 300;
    [Min(0)] public int roundGoldBonus = 50;

    [Header("Attacker Economy")]
    [Min(1)] public int startingAttackBudget = 200;
    [Min(0)] public int attackBudgetIncreasePerRound = 40;
    [Range(0.2f, 3f)] public float spawnInterval = 0.9f;
    [Min(1)] public int maxEnemiesPerWave = 50;

    [Header("Catalogs")]
    public TowerData[] towers;
    public EnemyData[] enemies;
}
