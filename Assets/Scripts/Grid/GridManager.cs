using System.Collections.Generic;
using UnityEngine;

public sealed class GridManager : MonoBehaviour
{
    [SerializeField] private GridCell cellPrefab;
    [SerializeField] private TowerBuildController buildController;
    [SerializeField] private int width = 12;
    [SerializeField] private int height = 8;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 origin;
    [SerializeField] private Vector2Int[] pathCells;

    private readonly Dictionary<Vector2Int, GridCell> cells = new Dictionary<Vector2Int, GridCell>();
    private HashSet<Vector2Int> pathLookup;

    public IReadOnlyDictionary<Vector2Int, GridCell> Cells => cells;

    private void Awake()
    {
        Generate();
    }

    public Vector3 CellToWorld(Vector2Int coordinates)
    {
        return new Vector3(
            origin.x + coordinates.x * cellSize,
            origin.y + coordinates.y * cellSize,
            0f);
    }

    private void Generate()
    {
        pathLookup = new HashSet<Vector2Int>(pathCells);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                GridCell cell = Instantiate(cellPrefab, CellToWorld(coordinates), Quaternion.identity, transform);
                cell.name = $"Cell_{x}_{y}";
                cell.Initialize(coordinates, pathLookup.Contains(coordinates), buildController);
                cells.Add(coordinates, cell);
            }
        }
    }
}
