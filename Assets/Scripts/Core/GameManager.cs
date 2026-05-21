using System;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private GameBalance balance;
    [SerializeField] private WaveSpawner waveSpawner;

    public static GameManager Instance { get; private set; }

    public event Action StateChanged;
    public event Action ResourcesChanged;

    public GameState State { get; private set; } = GameState.Menu;
    public int CurrentRound { get; private set; }
    public int Gold { get; private set; }
    public int BaseHp { get; private set; }
    public int AttackBudget { get; private set; }
    public bool DefenderWon { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (State == GameState.Battle && waveSpawner != null && !waveSpawner.IsBusy)
        {
            EndRound();
        }
    }

    public void StartGame()
    {
        CurrentRound = 1;
        Gold = balance.startingGold;
        BaseHp = balance.startingBaseHp;
        AttackBudget = balance.startingAttackBudget;
        DefenderWon = false;
        SetState(GameState.Preparation);
        ResourcesChanged?.Invoke();
    }

    public void StartBattle()
    {
        if (State != GameState.Preparation)
        {
            return;
        }

        SetState(GameState.Battle);
        waveSpawner.StartAiWave(CurrentRound, AttackBudget);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || Gold < amount)
        {
            return false;
        }

        Gold -= amount;
        ResourcesChanged?.Invoke();
        return true;
    }

    public void AddGold(int amount)
    {
        Gold += Mathf.Max(0, amount);
        ResourcesChanged?.Invoke();
    }

    public void ApplyBaseDamage(int damage)
    {
        BaseHp = Mathf.Max(0, BaseHp - Mathf.Max(0, damage));
        ResourcesChanged?.Invoke();

        if (BaseHp <= 0)
        {
            DefenderWon = false;
            SetState(GameState.GameOver);
        }
    }

    private void EndRound()
    {
        if (BaseHp <= 0)
        {
            DefenderWon = false;
            SetState(GameState.GameOver);
            return;
        }

        SetState(GameState.RoundEnd);

        if (CurrentRound >= balance.totalRounds)
        {
            DefenderWon = true;
            SetState(GameState.GameOver);
            return;
        }

        CurrentRound++;
        Gold += balance.roundGoldBonus;
        AttackBudget += balance.attackBudgetIncreasePerRound;
        ResourcesChanged?.Invoke();
        SetState(GameState.Preparation);
    }

    private void SetState(GameState state)
    {
        State = state;
        StateChanged?.Invoke();
    }
}
