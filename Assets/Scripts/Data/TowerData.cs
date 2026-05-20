using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Tower Data", fileName = "TowerData")]
public sealed class TowerData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Tower";
    public Sprite sprite;
    public TowerAttackKind attackKind = TowerAttackKind.SingleTarget;

    [Header("Economy")]
    [Min(0)] public int cost = 100;

    [Header("Combat")]
    [Min(0.1f)] public float range = 3f;
    [Min(0.1f)] public float fireRate = 1f;
    [Min(0f)] public float damage = 5f;
    [Min(0.1f)] public float projectileSpeed = 8f;
    [Min(0f)] public float splashRadius = 0f;

    [Header("Slow")]
    [Range(0.1f, 1f)] public float slowMultiplier = 0.5f;
    [Min(0f)] public float slowDuration = 1.5f;
}
