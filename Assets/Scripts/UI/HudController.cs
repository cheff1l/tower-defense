using UnityEngine;
using UnityEngine.UI;

public sealed class HudController : MonoBehaviour
{
    [SerializeField] private Text goldText;
    [SerializeField] private Text baseHpText;
    [SerializeField] private Text roundText;
    [SerializeField] private Text stateText;
    [SerializeField] private Button startBattleButton;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverText;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.ResourcesChanged += Refresh;
        GameManager.Instance.StateChanged += Refresh;

        if (startBattleButton != null)
        {
            startBattleButton.onClick.AddListener(GameManager.Instance.StartBattle);
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.ResourcesChanged -= Refresh;
        GameManager.Instance.StateChanged -= Refresh;
    }

    private void Refresh()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            return;
        }

        if (goldText != null) goldText.text = $"Gold: {game.Gold}";
        if (baseHpText != null) baseHpText.text = $"Base HP: {game.BaseHp}";
        if (roundText != null) roundText.text = $"Round: {game.CurrentRound}";
        if (stateText != null) stateText.text = game.State.ToString();

        if (startBattleButton != null)
        {
            startBattleButton.interactable = game.State == GameState.Preparation;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(game.State == GameState.GameOver);
        }

        if (gameOverText != null && game.State == GameState.GameOver)
        {
            gameOverText.text = game.DefenderWon ? "Defender wins" : "Attacker wins";
        }
    }
}
