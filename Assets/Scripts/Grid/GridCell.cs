using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class GridCell : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color freeColor = new Color(0.25f, 0.55f, 0.35f);
    [SerializeField] private Color pathColor = new Color(0.55f, 0.45f, 0.25f);
    [SerializeField] private Color occupiedColor = new Color(0.35f, 0.35f, 0.35f);

    public Vector2Int Coordinates { get; private set; }
    public bool IsPath { get; private set; }
    public bool IsOccupied { get; private set; }

    public bool CanBuild => !IsPath && !IsOccupied;

    private TowerBuildController buildController;

    public void Initialize(Vector2Int coordinates, bool isPath, TowerBuildController controller)
    {
        Coordinates = coordinates;
        IsPath = isPath;
        buildController = controller;
        RefreshColor();
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
        RefreshColor();
    }

    private void OnMouseDown()
    {
        if (buildController != null)
        {
            buildController.TryBuildOn(this);
        }
    }

    private void RefreshColor()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = IsPath ? pathColor : IsOccupied ? occupiedColor : freeColor;
    }
}
