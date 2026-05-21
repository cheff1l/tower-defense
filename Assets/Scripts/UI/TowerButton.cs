using UnityEngine;
using UnityEngine.UI;

public sealed class TowerButton : MonoBehaviour
{
    [SerializeField] private TowerBuildController buildController;
    [SerializeField] private int towerIndex;
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(() => buildController.SelectTower(towerIndex));
        }
    }
}
