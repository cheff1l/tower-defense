using UnityEngine;

public sealed class TowerBuildController : MonoBehaviour
{
    [SerializeField] private GameBalance balance;
    [SerializeField] private Tower towerPrefab;
    [SerializeField] private ObjectPool projectilePool;

    private TowerData selectedTower;

    private void Start()
    {
        if (balance != null && balance.towers != null && balance.towers.Length > 0)
        {
            selectedTower = balance.towers[0];
        }
    }

    public void SelectTower(int index)
    {
        if (balance == null || balance.towers == null || index < 0 || index >= balance.towers.Length)
        {
            return;
        }

        selectedTower = balance.towers[index];
    }

    public void TryBuildOn(GridCell cell)
    {
        if (cell == null || selectedTower == null || !cell.CanBuild)
        {
            return;
        }

        GameManager game = GameManager.Instance;
        if (game == null || game.State != GameState.Preparation || !game.TrySpendGold(selectedTower.cost))
        {
            return;
        }

        Tower tower = Instantiate(towerPrefab, cell.transform.position, Quaternion.identity);
        tower.Initialize(selectedTower, projectilePool);
        cell.SetOccupied(true);
    }
}
