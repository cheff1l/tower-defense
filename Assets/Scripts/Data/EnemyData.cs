using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Enemy Data", fileName = "EnemyData")]
public sealed class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Enemy";
    public Sprite sprite;

    [Header("Stats")]
    [Min(1f)] public float maxHealth = 10f;
    [Min(0.1f)] public float moveSpeed = 2f;
    [Min(1)] public int attackCost = 10;
    [Min(0)] public int rewardGold = 5;
    [Min(1)] public int baseDamage = 1;
    public bool ignoresSlow;
}
