using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CompleteTowerDefenseGame : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite mapSprite;
    public Sprite goblinSprite;
    public Sprite orcSprite;
    public Sprite ghostSprite;
    public Sprite archerTowerSprite;
    public Sprite mageTowerSprite;
    public Sprite freezerTowerSprite;
    public Sprite cannonTowerSprite;
    public Sprite arrowSprite;
    public Sprite magicSprite;
    public Sprite cannonProjectileSprite;
    public Sprite freezerProjectileSprite;
    public Sprite goldSprite;
    public Sprite goldPanelSprite;
    public Sprite coinEffectSprite;
    public Sprite attackPowerSprite;
    public Sprite[] waveSprites = new Sprite[10];
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public Sprite shopPanelSprite;
    public Sprite menuButtonsSprite;
    public Sprite startButtonSprite;
    public Sprite testGameButtonSprite;
    public Sprite exitButtonSprite;
    public Sprite restartButtonSprite;
    public Sprite menuButtonSprite;
    public Sprite shopButtonSprite;
    public Sprite buildSpotSprite;
    public Sprite baseHealthFrameSprite;
    public Sprite upgradeMenuSprite;
    public Sprite upgradePipSprite;
    public Sprite shopArcherIcon;
    public Sprite shopMageIcon;
    public Sprite shopFreezerIcon;
    public Sprite shopCannonIcon;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public Texture2D cursorTexture;

    [Header("Audio")]
    public AudioClip introMusic;
    public AudioClip menuMusic;
    public AudioClip battleMusic;
    public AudioClip winMusic;
    public AudioClip loseMusic;
    public AudioClip buildSound;
    public AudioClip archerAttackSound;
    public AudioClip mageAttackSound;
    public AudioClip freezerAttackSound;
    public AudioClip cannonAttackSound;
    public AudioClip deathSound;
    public AudioClip coinSound;

    [Header("Rules")]
    [SerializeField] private int totalRounds = 10;
    [SerializeField] private int startingGold = 350;
    [SerializeField] private int startingBaseHp = 25;
    [SerializeField] private int startingAttackBudget = 150;
    [SerializeField] private int budgetIncreasePerRound = 45;
    [SerializeField] private int roundGoldBonus = 75;
    [SerializeField] private int maxEnemiesPerWave = 34;
    [SerializeField] private float spawnInterval = 0.78f;

    private const int TestTowerCount = 20;
    private const int TestEnemyCount = 50;
    private const int TestProjectilePoolSize = 180;

    private readonly List<CompleteEnemy> activeEnemies = new List<CompleteEnemy>(96);
    private readonly List<CompleteTower> towers = new List<CompleteTower>(32);
    private readonly Queue<CompleteEnemy> enemyPool = new Queue<CompleteEnemy>(64);
    private readonly Queue<CompleteProjectile> projectilePool = new Queue<CompleteProjectile>(96);

    private Camera mainCamera;
    private Transform worldRoot;
    private Transform enemyRoot;
    private Transform towerRoot;
    private Transform projectileRoot;
    private GameObject buildSpotRoot;
    private Canvas canvas;
    private Text goldText;
    private Text hpText;
    private Text roundText;
    private Text stateText;
    private Text budgetText;
    private Image waveImage;
    private Image goldPanelImage;
    private Image attackPowerImage;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private Coroutine musicSequence;
    private GameObject menuPanel;
    private GameObject gamePanel;
    private GameObject towerShopPanel;
    private GameObject gameOverPanel;
    private Text gameOverText;
    private Button menuStartButton;
    private Button menuTestButton;
    private Button menuSoundButton;
    private Button menuExitButton;
    private Button startBattleButton;
    private Button shopButton;
    private Button gameSoundButton;
    private Button restartButton;
    private Button backToMenuButton;
    private Button[] towerButtons;
    private Transform baseHealthRoot;
    private Transform baseHealthFill;
    private SpriteRenderer baseHealthFillRenderer;
    private GameObject rangeIndicator;
    private SpriteRenderer rangeIndicatorRenderer;
    private GameObject upgradeMenuRoot;
    private SpriteRenderer upgradeMenuRenderer;
    private TextMesh upgradeSpeedCostText;
    private TextMesh upgradeDamageCostText;
    private TextMesh upgradeRangeCostText;
    private SpriteRenderer[] upgradeSpeedPips;
    private SpriteRenderer[] upgradeDamagePips;
    private SpriteRenderer[] upgradeRangePips;
    private CompleteTower selectedTower;

    private GameState state = GameState.Menu;
    private TowerConfig[] towerConfigs;
    private EnemyConfig[] enemyConfigs;
    private Vector3[] pathPoints;
    private float[] pathDistanceAtPoint;
    private float totalPathLength;
    private Vector3[] buildSpots;
    private int currentRound;
    private int gold;
    private int baseHp;
    private int attackBudget;
    private int selectedTowerIndex;
    private bool spawning;
    private bool soundOn = true;
    private bool shopOpen;

    public IReadOnlyList<CompleteEnemy> ActiveEnemies => activeEnemies;

    private void Awake()
    {
        LoadRuntimeSprites();
        BuildConfigs();
        BuildPath();
        BuildBuildSpots();
        BuildScene();
        SetupAudio();
        PrewarmPools();
        ShowMenu();
    }

    private void Update()
    {
        if (WasPrimaryClickThisFrame())
        {
            Vector2 screenPosition = GetPointerScreenPosition();
            if (!HandleManualUiClick(screenPosition) && (state == GameState.Preparation || state == GameState.Battle))
            {
                HandleBuildInput(screenPosition);
            }
        }

        if (state == GameState.Battle && !spawning && activeEnemies.Count == 0)
        {
            FinishRound();
        }

        UpdateRangePreview();
    }

    public void StartNewGame()
    {
        currentRound = 1;
        gold = startingGold;
        baseHp = startingBaseHp;
        attackBudget = startingAttackBudget;
        selectedTowerIndex = 0;
        shopOpen = false;
        CloseUpgradeMenu();
        HideRangeIndicator();
        ClearBattlefield();
        state = GameState.Preparation;
        menuPanel.SetActive(false);
        gamePanel.SetActive(true);
        gameOverPanel.SetActive(false);
        StopMusicSequence();
        PlayMusic(menuMusic, true);
        RefreshHud();
    }

    public void StartBattle()
    {
        if (state != GameState.Preparation)
        {
            return;
        }

        state = GameState.Battle;
        CloseShop();
        CloseUpgradeMenu();
        HideRangeIndicator();
        PlayIntroThenMain();
        RefreshHud();
        StartCoroutine(SpawnAiWave());
    }

    public void StartTestGame()
    {
        currentRound = totalRounds;
        gold = 0;
        baseHp = 999;
        attackBudget = TestEnemyCount;
        selectedTowerIndex = 0;
        shopOpen = false;
        CloseUpgradeMenu();
        HideRangeIndicator();
        ClearBattlefield();
        state = GameState.Battle;
        menuPanel.SetActive(false);
        gamePanel.SetActive(true);
        gameOverPanel.SetActive(false);
        CloseShop();
        StopMusicSequence();
        PlayMusic(battleMusic, true);
        BuildTestTowers();
        SpawnTestEnemies();
        spawning = false;
        RefreshHud();
    }

    public void SelectTower(int index)
    {
        selectedTowerIndex = Mathf.Clamp(index, 0, towerConfigs.Length - 1);
        CloseUpgradeMenu();
        CloseShop();
        RefreshHud();
    }

    public void ToggleShop()
    {
        if (state != GameState.Preparation || towerShopPanel == null)
        {
            return;
        }

        shopOpen = !shopOpen;
        if (shopOpen)
        {
            CloseUpgradeMenu();
        }
        towerShopPanel.SetActive(shopOpen);
        RefreshHud();
    }

    public void ToggleSound()
    {
        soundOn = !soundOn;
        AudioListener.volume = soundOn ? 1f : 0f;
        if (soundOn && musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
        RefreshHud();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void NotifyEnemyKilled(CompleteEnemy enemy, int reward)
    {
        Vector3 rewardPosition = enemy.transform.position;
        activeEnemies.Remove(enemy);
        gold += reward;
        PlaySfx(deathSound, 0.85f);
        SpawnCoinReward(rewardPosition);
        ReturnEnemy(enemy);
        RefreshHud();
    }

    public void NotifyEnemyReachedBase(CompleteEnemy enemy, int damage)
    {
        activeEnemies.Remove(enemy);
        baseHp = Mathf.Max(0, baseHp - damage);
        ReturnEnemy(enemy);
        RefreshHud();

        if (baseHp <= 0)
        {
            EndGame(false);
        }
    }

    public CompleteProjectile GetProjectile()
    {
        CompleteProjectile projectile = projectilePool.Count > 0 ? projectilePool.Dequeue() : CreateProjectile();
        projectile.gameObject.SetActive(true);
        return projectile;
    }

    public void ReturnProjectile(CompleteProjectile projectile)
    {
        projectile.gameObject.SetActive(false);
        projectilePool.Enqueue(projectile);
    }

    public Vector3 GetPathPoint(int index)
    {
        return pathPoints[Mathf.Clamp(index, 0, pathPoints.Length - 1)];
    }

    public int PathPointCount => pathPoints.Length;

    public void PlayTowerAttackSound(string towerName)
    {
        switch (towerName)
        {
            case "Mage":
                PlaySfx(mageAttackSound, 0.72f);
                break;
            case "Freezer":
                PlaySfx(freezerAttackSound, 0.72f);
                break;
            case "Cannon":
                PlaySfx(cannonAttackSound, 0.85f);
                break;
            default:
                PlaySfx(archerAttackSound, 0.58f);
                break;
        }
    }

    public float GetPathProgress(int pointIndex, Vector3 position)
    {
        int previousIndex = Mathf.Max(0, pointIndex - 1);
        int currentIndex = Mathf.Clamp(pointIndex, 0, pathPoints.Length - 1);
        float completed = pathDistanceAtPoint[previousIndex];
        float segment = Vector3.Distance(pathPoints[previousIndex], pathPoints[currentIndex]);

        if (segment <= 0.001f)
        {
            return completed / totalPathLength;
        }

        float travelled = segment - Vector3.Distance(position, pathPoints[currentIndex]);
        return Mathf.Clamp01((completed + Mathf.Clamp(travelled, 0f, segment)) / totalPathLength);
    }

    private void LoadRuntimeSprites()
    {
        arrowSprite = arrowSprite != null ? arrowSprite : LoadSpriteFromResource("Art/Projectile_Arrow_Fixed", 180f);
        cannonProjectileSprite = cannonProjectileSprite != null ? cannonProjectileSprite : LoadSpriteFromResource("Art/Projectile_Bomb_Fixed", 180f);
        freezerProjectileSprite = freezerProjectileSprite != null ? freezerProjectileSprite : LoadSpriteFromResource("Art/Projectile_Ice_Fixed", 180f);
        goldSprite = goldSprite != null ? goldSprite : LoadSpriteFromResource("Art/Shop_Gold", 180f);
        coinEffectSprite = coinEffectSprite != null ? coinEffectSprite : LoadSpriteFromResource("Art/Coin_Effect", 260f);
        goldPanelSprite = goldPanelSprite != null ? goldPanelSprite : LoadSpriteFromResource("Art/Hud_GoldPanel", 180f);
        attackPowerSprite = attackPowerSprite != null ? attackPowerSprite : LoadSpriteFromResource("Art/Hud_AttackPanel", 180f);
        if (waveSprites == null || waveSprites.Length != 10)
        {
            waveSprites = new Sprite[10];
        }

        for (int i = 0; i < waveSprites.Length; i++)
        {
            waveSprites[i] = waveSprites[i] != null ? waveSprites[i] : LoadSpriteFromResource("Art/Wave_" + (i + 1), 180f);
        }

        shopPanelSprite = shopPanelSprite != null ? shopPanelSprite : LoadSpriteFromResource("Art/Shop_Window", 180f);
        menuButtonsSprite = menuButtonsSprite != null ? menuButtonsSprite : LoadSpriteFromResource("Art/Button_MenuStartExit", 180f);
        startButtonSprite = startButtonSprite != null ? startButtonSprite : LoadSpriteFromResource("Art/Button_Start", 180f);
        testGameButtonSprite = testGameButtonSprite != null ? testGameButtonSprite : LoadSpriteFromResource("Art/Test_Game_Button", 180f);
        exitButtonSprite = exitButtonSprite != null ? exitButtonSprite : LoadSpriteFromResource("Art/Button_Exit", 180f);
        restartButtonSprite = restartButtonSprite != null ? restartButtonSprite : LoadSpriteFromResource("Art/Button_Restart", 180f);
        menuButtonSprite = menuButtonSprite != null ? menuButtonSprite : LoadSpriteFromResource("Art/Button_Menu", 180f);
        shopButtonSprite = shopButtonSprite != null ? shopButtonSprite : LoadSpriteFromResource("Art/Button_Shop", 180f);
        buildSpotSprite = buildSpotSprite != null ? buildSpotSprite : LoadSpriteFromResource("Art/BuildSpot_Pad", 220f);
        baseHealthFrameSprite = baseHealthFrameSprite != null ? baseHealthFrameSprite : LoadSpriteFromResource("Art/BaseHealthFrame", 220f);
        upgradeMenuSprite = upgradeMenuSprite != null ? upgradeMenuSprite : LoadSpriteFromResource("Art/Upgrade_Menu", 240f);
        upgradePipSprite = upgradePipSprite != null ? upgradePipSprite : LoadSpriteFromResource("Art/Upgrade_LevelPip", 260f);
        shopArcherIcon = shopArcherIcon != null ? shopArcherIcon : LoadSpriteFromResource("Art/Shop_Archer", 180f);
        shopMageIcon = shopMageIcon != null ? shopMageIcon : LoadSpriteFromResource("Art/Shop_Mage", 180f);
        shopFreezerIcon = shopFreezerIcon != null ? shopFreezerIcon : LoadSpriteFromResource("Art/Shop_Freezer", 180f);
        shopCannonIcon = shopCannonIcon != null ? shopCannonIcon : LoadSpriteFromResource("Art/Shop_Cannon", 180f);
        soundOnSprite = soundOnSprite != null ? soundOnSprite : LoadSpriteFromResource("Art/Button_SoundOn", 180f);
        soundOffSprite = soundOffSprite != null ? soundOffSprite : LoadSpriteFromResource("Art/Button_SoundOff", 180f);
        cursorTexture = cursorTexture != null ? cursorTexture : Resources.Load<Texture2D>("Art/Game_Cursor_96");
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, new Vector2(6f, 6f), CursorMode.Auto);
        }
    }

    private Sprite LoadSpriteFromResource(string path, float pixelsPerUnit)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private void BuildConfigs()
    {
        towerConfigs = new[]
        {
            new TowerConfig("Archer", 90, 3.2f, 1.35f, 13f, 8.5f, 0f, 0f, 1f, archerTowerSprite, arrowSprite, new Color(0.96f, 0.82f, 0.34f)),
            new TowerConfig("Mage", 145, 2.9f, 0.72f, 18f, 6.8f, 1.05f, 0f, 1f, mageTowerSprite, magicSprite, new Color(0.62f, 0.24f, 1f)),
            new TowerConfig("Freezer", 120, 3.1f, 1.05f, 6f, 7.2f, 0f, 2.2f, 0.42f, freezerTowerSprite, freezerProjectileSprite != null ? freezerProjectileSprite : magicSprite, new Color(0.42f, 0.9f, 1f)),
            new TowerConfig("Cannon", 210, 4.15f, 0.36f, 48f, 5.9f, 0f, 0f, 1f, cannonTowerSprite, cannonProjectileSprite != null ? cannonProjectileSprite : magicSprite, new Color(1f, 0.46f, 0.16f))
        };

        enemyConfigs = new[]
        {
            new EnemyConfig("Goblin", 40f, 2.65f, 18, 7, 1, false, goblinSprite, new Color(0.25f, 0.9f, 0.25f)),
            new EnemyConfig("Orc", 110f, 1.15f, 44, 18, 2, false, orcSprite, new Color(0.85f, 0.22f, 0.18f)),
            new EnemyConfig("Ghost", 70f, 1.85f, 34, 14, 1, true, ghostSprite, new Color(0.68f, 0.9f, 1f))
        };
    }

    private void BuildPath()
    {
        pathPoints = new[]
        {
            new Vector3(-6.64f, -0.29f, 0f),
            new Vector3(-5.78f, -0.28f, 0f),
            new Vector3(-5.63f, 0.41f, 0f),
            new Vector3(-4.57f, 0.37f, 0f),
            new Vector3(-4.56f, -1.29f, 0f),
            new Vector3(-2.85f, -1.29f, 0f),
            new Vector3(-2.85f, 2.1f, 0f),
            new Vector3(1.93f, 2.1f, 0f),
            new Vector3(1.93f, 0.59f, 0f),
            new Vector3(1.19f, 0.59f, 0f),
            new Vector3(1.19f, -1.78f, 0f),
            new Vector3(3.845f, -1.78f, 0f),
            new Vector3(3.845f, 0.069f, 0f),
            new Vector3(4.559f, 0.069f, 0f)
        };

        pathDistanceAtPoint = new float[pathPoints.Length];
        totalPathLength = 0f;
        for (int i = 1; i < pathPoints.Length; i++)
        {
            totalPathLength += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            pathDistanceAtPoint[i] = totalPathLength;
        }
    }

    private void BuildBuildSpots()
    {
        buildSpots = new[]
        {
            new Vector3(-4.946f, 1.424f, 0f),
            new Vector3(-3.668f, -2.181f, 0f),
            new Vector3(-1.802f, -0.061f, 0f),
            new Vector3(-0.35f, 1.26f, 0f),
            new Vector3(0.23f, -0.74f, 0f),
            new Vector3(2.54f, -1f, 0f),
            new Vector3(2.97f, 1.76f, 0f)
        };
    }

    private void BuildScene()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 4.5f;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.035f, 0.045f, 0.04f);

        worldRoot = new GameObject("RuntimeWorld").transform;
        enemyRoot = new GameObject("Enemies").transform;
        towerRoot = new GameObject("Towers").transform;
        projectileRoot = new GameObject("Projectiles").transform;
        enemyRoot.SetParent(worldRoot);
        towerRoot.SetParent(worldRoot);
        projectileRoot.SetParent(worldRoot);

        CreateMap();
        CreateCanvas();
        CreateWorldDecorations();
        CreateRangeIndicator();
        CreateUpgradeMenu();
    }

    private void CreateMap()
    {
        GameObject map = new GameObject("MapBackground");
        map.transform.SetParent(worldRoot);
        SpriteRenderer renderer = map.AddComponent<SpriteRenderer>();
        renderer.sprite = mapSprite;
        renderer.sortingOrder = -20;

        if (mapSprite != null)
        {
            Vector2 size = mapSprite.bounds.size;
            map.transform.localScale = new Vector3(16f / size.x, 9f / size.y, 1f);
        }
    }

    private void CreateWorldDecorations()
    {
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Debug.DrawLine(pathPoints[i], pathPoints[i + 1], Color.yellow, 60f);
        }

        buildSpotRoot = new GameObject("BuildSpots");
        buildSpotRoot.transform.SetParent(worldRoot);
        Sprite spotSprite = buildSpotSprite != null ? buildSpotSprite : MakeCircleSprite(64);

        for (int i = 0; i < buildSpots.Length; i++)
        {
            GameObject spot = new GameObject("BuildSpot_" + i);
            spot.transform.SetParent(buildSpotRoot.transform);
            spot.transform.position = buildSpots[i];

            SpriteRenderer renderer = spot.AddComponent<SpriteRenderer>();
            renderer.sprite = spotSprite;
            renderer.color = buildSpotSprite != null ? new Color(1f, 1f, 1f, 0.82f) : new Color(0.08f, 0.95f, 0.35f, 0.58f);
            renderer.sortingOrder = -5;
            spot.transform.localScale = buildSpotSprite != null ? new Vector3(0.1368664f, 0.1368664f, 1f) : new Vector3(0.34f, 0.34f, 1f);
        }

        CreateBaseHealthBar();
    }

    private void CreateRangeIndicator()
    {
        rangeIndicator = new GameObject("TowerRangePreview");
        rangeIndicator.transform.SetParent(worldRoot);
        rangeIndicatorRenderer = rangeIndicator.AddComponent<SpriteRenderer>();
        rangeIndicatorRenderer.sprite = MakeRangeSprite(192);
        rangeIndicatorRenderer.sortingOrder = 25;
        rangeIndicatorRenderer.color = new Color(0.22f, 0.72f, 1f, 0.42f);
        rangeIndicator.SetActive(false);
    }

    private void CreateUpgradeMenu()
    {
        upgradeMenuRoot = new GameObject("TowerUpgradeMenu");
        upgradeMenuRoot.transform.SetParent(worldRoot);
        upgradeMenuRoot.transform.position = new Vector3(0f, 0f, -0.12f);

        upgradeMenuRenderer = upgradeMenuRoot.AddComponent<SpriteRenderer>();
        upgradeMenuRenderer.sprite = upgradeMenuSprite;
        upgradeMenuRenderer.sortingOrder = 45;
        upgradeMenuRenderer.color = Color.white;
        if (upgradeMenuSprite != null)
        {
            FitWorldSprite(upgradeMenuRenderer, 3.25f);
        }

        upgradeSpeedCostText = CreateWorldText("SpeedCost", upgradeMenuRoot.transform, new Vector3(-1.29f, 0.51f, -0.05f));
        upgradeDamageCostText = CreateWorldText("DamageCost", upgradeMenuRoot.transform, new Vector3(1.29f, 0.51f, -0.05f));
        upgradeRangeCostText = CreateWorldText("RangeCost", upgradeMenuRoot.transform, new Vector3(0f, -1.93f, -0.05f));
        upgradeSpeedPips = CreateUpgradePips("SpeedPips", upgradeMenuRoot.transform, new Vector3(-1.29f, 0.12f, -0.055f));
        upgradeDamagePips = CreateUpgradePips("DamagePips", upgradeMenuRoot.transform, new Vector3(1.29f, 0.12f, -0.055f));
        upgradeRangePips = CreateUpgradePips("RangePips", upgradeMenuRoot.transform, new Vector3(0f, -2.31f, -0.055f));
        upgradeMenuRoot.SetActive(false);
    }

    private SpriteRenderer[] CreateUpgradePips(string name, Transform parent, Vector3 localPosition)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.localPosition = localPosition;

        SpriteRenderer[] pips = new SpriteRenderer[3];
        Sprite pipSprite = upgradePipSprite != null ? upgradePipSprite : MakeSprite(Color.white);
        for (int i = 0; i < pips.Length; i++)
        {
            GameObject pip = new GameObject("Level_" + (i + 1));
            pip.transform.SetParent(root.transform);
            pip.transform.localPosition = new Vector3((i - 1) * 0.35f, 0f, 0f);
            pip.transform.localScale = GetWorldSpriteScale(pipSprite, 0.29f, 0.085f);

            SpriteRenderer renderer = pip.AddComponent<SpriteRenderer>();
            renderer.sprite = pipSprite;
            renderer.sortingOrder = 50;
            renderer.color = Color.white;
            renderer.enabled = false;
            pips[i] = renderer;
        }

        return pips;
    }

    private TextMesh CreateWorldText(string name, Transform parent, Vector3 localPosition)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = Vector3.one;

        TextMesh text = obj.AddComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 96;
        text.characterSize = 0.052f;
        text.color = new Color(1f, 0.83f, 0.28f, 1f);
        ApplyTextMeshFont(text);

        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 52;

        GameObject shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(obj.transform);
        shadowObject.transform.localPosition = new Vector3(0.035f, -0.035f, 0.01f);
        shadowObject.transform.localScale = Vector3.one;

        TextMesh shadow = shadowObject.AddComponent<TextMesh>();
        shadow.anchor = TextAnchor.MiddleCenter;
        shadow.alignment = TextAlignment.Center;
        shadow.fontSize = text.fontSize;
        shadow.characterSize = text.characterSize;
        shadow.color = new Color(0.06f, 0.025f, 0f, 0.9f);
        ApplyTextMeshFont(shadow);

        MeshRenderer shadowRenderer = shadowObject.GetComponent<MeshRenderer>();
        shadowRenderer.sortingOrder = 51;
        return text;
    }

    private static void ApplyTextMeshFont(TextMesh text)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            return;
        }

        text.font = font;
        MeshRenderer renderer = text.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = font.material;
        }
    }

    private void CreateBaseHealthBar()
    {
        baseHealthRoot = new GameObject("BaseHealthBar").transform;
        baseHealthRoot.SetParent(worldRoot);
        baseHealthRoot.position = Vector3.zero;

        GameObject bgObject = new GameObject("Background");
        bgObject.transform.SetParent(baseHealthRoot);
        bgObject.transform.localPosition = new Vector3(5.9904f, 3.419f, -0.015f);
        SpriteRenderer bgRenderer = bgObject.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = MakeSprite(new Color(0.11f, 0.025f, 0.02f, 0.9f));
        bgRenderer.sortingOrder = 54;
        bgObject.transform.localScale = new Vector3(1.26f, 0.16f, 1f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(baseHealthRoot);
        fillObject.transform.localPosition = new Vector3(5.9904f, 3.419f, -0.01f);
        baseHealthFillRenderer = fillObject.AddComponent<SpriteRenderer>();
        baseHealthFillRenderer.sprite = MakeSprite(new Color(0.15f, 0.95f, 0.32f, 0.95f));
        baseHealthFillRenderer.sortingOrder = 55;
        baseHealthFill = fillObject.transform;
        baseHealthFill.localScale = new Vector3(1.189482f, 0.145f, 1f);

        if (baseHealthFrameSprite != null)
        {
            GameObject frameObject = new GameObject("Frame");
            frameObject.transform.SetParent(baseHealthRoot);
            frameObject.transform.localPosition = new Vector3(5.99f, 3.41f, -0.02f);
            SpriteRenderer frameRenderer = frameObject.AddComponent<SpriteRenderer>();
            frameRenderer.sprite = baseHealthFrameSprite;
            frameRenderer.color = Color.white;
            frameRenderer.sortingOrder = 56;
            frameObject.transform.localScale = new Vector3(0.2901099f, 0.2901099f, 1f);
        }

        RefreshBaseHealthBar();
    }

    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("RuntimeCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        menuPanel = CreatePanel("Menu", canvas.transform, new Color(0f, 0f, 0f, 0.48f));
        Text title = CreateText("Title", menuPanel.transform, "TOWER DEFENSE", 78, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 285f), new Vector2(760f, 100f));

        Vector2 menuButtonSize = new Vector2(620f, 148f);
        menuStartButton = CreateButton("StartButton", menuPanel.transform, "", new Vector2(0f, 185f), StartNewGame, menuButtonSize);
        ApplyButtonSprite(menuStartButton, startButtonSprite, true);
        menuTestButton = CreateButton("TestGameButton", menuPanel.transform, "", new Vector2(0f, 35f), StartTestGame, menuButtonSize);
        ApplyButtonSprite(menuTestButton, testGameButtonSprite, true);
        menuSoundButton = CreateButton("SoundButton", menuPanel.transform, "", new Vector2(0f, -115f), ToggleSound, menuButtonSize);
        ApplyButtonSprite(menuSoundButton, soundOn ? soundOnSprite : soundOffSprite, true);
        menuExitButton = CreateButton("ExitButton", menuPanel.transform, "", new Vector2(0f, -265f), ExitGame, menuButtonSize);
        ApplyButtonSprite(menuExitButton, exitButtonSprite, true);

        gamePanel = CreatePanel("GameUI", canvas.transform, new Color(0f, 0f, 0f, 0f));
        CreateHud(gamePanel.transform);
        CreateTowerBar(gamePanel.transform);

        gameOverPanel = CreatePanel("GameOver", canvas.transform, new Color(0f, 0f, 0f, 0.58f));
        gameOverText = CreateText("GameOverText", gameOverPanel.transform, "Victory", 72, TextAnchor.MiddleCenter);
        SetRect(gameOverText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(760f, 110f));
        restartButton = CreateButton("RestartButton", gameOverPanel.transform, "", new Vector2(0f, -55f), StartNewGame, new Vector2(620f, 148f));
        ApplyButtonSprite(restartButton, restartButtonSprite, true);
        backToMenuButton = CreateButton("BackToMenuButton", gameOverPanel.transform, "", new Vector2(0f, -205f), ShowMenu, new Vector2(620f, 148f));
        ApplyButtonSprite(backToMenuButton, menuButtonSprite, true);
    }

    private void SetupAudio()
    {
        if (mainCamera != null && mainCamera.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }

        introMusic = introMusic != null ? introMusic : Resources.Load<AudioClip>("Audio/defend_castle");
        menuMusic = menuMusic != null ? menuMusic : Resources.Load<AudioClip>("Audio/main");
        battleMusic = battleMusic != null ? battleMusic : Resources.Load<AudioClip>("Audio/main");
        winMusic = winMusic != null ? winMusic : Resources.Load<AudioClip>("Audio/win_battle");
        loseMusic = loseMusic != null ? loseMusic : Resources.Load<AudioClip>("Audio/lose_battle");
        buildSound = buildSound != null ? buildSound : Resources.Load<AudioClip>("Audio/building_sound");
        archerAttackSound = archerAttackSound != null ? archerAttackSound : Resources.Load<AudioClip>("Audio/fire_attack");
        mageAttackSound = mageAttackSound != null ? mageAttackSound : Resources.Load<AudioClip>("Audio/magic_attack");
        freezerAttackSound = freezerAttackSound != null ? freezerAttackSound : Resources.Load<AudioClip>("Audio/ice_attack");
        cannonAttackSound = cannonAttackSound != null ? cannonAttackSound : Resources.Load<AudioClip>("Audio/cannon_attack");
        deathSound = deathSound != null ? deathSound : Resources.Load<AudioClip>("Audio/death_sound");
        coinSound = coinSound != null ? coinSound : Resources.Load<AudioClip>("Audio/coin_sound");

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0.45f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = 0.85f;
        AudioListener.volume = soundOn ? 1f : 0f;
    }

    private void PlayIntroThenMain()
    {
        StopMusicSequence();

        musicSequence = StartCoroutine(PlayIntroThenMainRoutine());
    }

    private void StopMusicSequence()
    {
        if (musicSequence != null)
        {
            StopCoroutine(musicSequence);
            musicSequence = null;
        }
    }

    private IEnumerator PlayIntroThenMainRoutine()
    {
        PlayMusic(introMusic, false);
        if (introMusic != null)
        {
            yield return new WaitForSeconds(Mathf.Min(introMusic.length, 2.25f));
        }

        PlayMusic(battleMusic != null ? battleMusic : menuMusic, true);
        musicSequence = null;
    }

    private void PlayMusic(AudioClip clip, bool loop)
    {
        if (clip == null || musicSource == null)
        {
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            musicSource.loop = loop;
            return;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null || !soundOn)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, volume);
    }

    private void SpawnCoinReward(Vector3 position)
    {
        PlaySfx(coinSound, 0.52f);

        GameObject coin = new GameObject("CoinReward");
        coin.transform.SetParent(worldRoot);
        coin.transform.position = new Vector3(position.x, position.y + 0.18f, -0.08f);

        SpriteRenderer renderer = coin.AddComponent<SpriteRenderer>();
        renderer.sprite = coinEffectSprite != null ? coinEffectSprite : goldSprite;
        renderer.sortingOrder = 40;
        renderer.color = Color.white;
        FitWorldSprite(renderer, 0.46f);

        StartCoroutine(AnimateCoinReward(coin, renderer));
    }

    public void SpawnDamageText(Vector3 position, float amount)
    {
        GameObject obj = new GameObject("DamageText");
        obj.transform.SetParent(worldRoot);
        obj.transform.position = position + new Vector3(Random.Range(-0.12f, 0.12f), 0.58f, -0.14f);

        TextMesh text = obj.AddComponent<TextMesh>();
        text.text = Mathf.CeilToInt(amount).ToString();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 86;
        text.characterSize = 0.034f;
        text.color = new Color(1f, 0.96f, 0.82f, 0.82f);
        ApplyTextMeshFont(text);

        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 60;

        StartCoroutine(AnimateDamageText(obj, text));
    }

    private IEnumerator AnimateCoinReward(GameObject coin, SpriteRenderer renderer)
    {
        float duration = 0.72f;
        float elapsed = 0f;
        Vector3 start = coin.transform.position;
        Vector3 baseScale = coin.transform.localScale;

        while (elapsed < duration && coin != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float lift = Mathf.Sin(t * Mathf.PI) * 0.34f + t * 0.22f;
            coin.transform.position = start + new Vector3(0f, lift, 0f);
            coin.transform.localScale = baseScale * (1f + 0.18f * Mathf.Sin(t * Mathf.PI));

            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((t - 0.48f) / 0.52f));
                renderer.color = color;
            }

            yield return null;
        }

        if (coin != null)
        {
            Destroy(coin);
        }
    }

    private IEnumerator AnimateDamageText(GameObject obj, TextMesh text)
    {
        float duration = 0.78f;
        float elapsed = 0f;
        Vector3 start = obj.transform.position;
        Vector3 drift = new Vector3(Random.Range(-0.22f, 0.22f), 0.62f, 0f);

        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            obj.transform.position = start + drift * t;
            obj.transform.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(t * Mathf.PI));

            if (text != null)
            {
                Color color = text.color;
                color.a = Mathf.SmoothStep(0.82f, 0f, Mathf.Clamp01((t - 0.28f) / 0.72f));
                text.color = color;
            }

            yield return null;
        }

        if (obj != null)
        {
            Destroy(obj);
        }
    }

    private void CreateHud(Transform parent)
    {
        GameObject hud = CreatePanel("HudBar", parent, new Color(0f, 0f, 0f, 0f));
        RectTransform rect = hud.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 78f);
        rect.anchoredPosition = Vector2.zero;

        roundText = CreateText("RoundText", hud.transform, "", 30, TextAnchor.MiddleLeft);
        goldText = CreateText("GoldText", hud.transform, "", 30, TextAnchor.MiddleLeft);
        hpText = CreateText("HpText", hud.transform, "", 30, TextAnchor.MiddleLeft);
        budgetText = CreateText("BudgetText", hud.transform, "", 30, TextAnchor.MiddleLeft);
        stateText = CreateText("StateText", hud.transform, "", 28, TextAnchor.MiddleRight);
        SetRect(roundText.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(220f, 58f));
        SetRect(goldText.rectTransform, new Vector2(0f, 0.5f), new Vector2(350f, 0f), new Vector2(220f, 58f));
        SetRect(hpText.rectTransform, new Vector2(0f, 0.5f), new Vector2(580f, 0f), new Vector2(250f, 58f));
        SetRect(budgetText.rectTransform, new Vector2(0f, 0.5f), new Vector2(850f, 0f), new Vector2(300f, 58f));
        SetRect(stateText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-520f, 0f), new Vector2(300f, 58f));
        roundText.enabled = false;
        hpText.enabled = false;
        stateText.enabled = false;

        waveImage = CreateImage("WaveBadge", hud.transform, null);
        waveImage.preserveAspect = true;
        waveImage.enabled = false;
        SetRect(waveImage.rectTransform, new Vector2(0f, 1f), new Vector2(92f, -96f), new Vector2(190f, 162f));

        goldPanelImage = CreateImage("GoldPanel", hud.transform, goldPanelSprite);
        goldPanelImage.preserveAspect = true;
        goldPanelImage.enabled = goldPanelSprite != null;
        SetRect(goldPanelImage.rectTransform, new Vector2(0f, 1f), new Vector2(315f, -54f), new Vector2(318f, 92f));
        SetRect(goldText.rectTransform, new Vector2(0f, 1f), new Vector2(382f, -55f), new Vector2(156f, 62f));
        goldText.fontSize = 38;
        goldText.alignment = TextAnchor.MiddleCenter;

        attackPowerImage = CreateImage("AttackPowerPanel", hud.transform, attackPowerSprite);
        attackPowerImage.preserveAspect = true;
        attackPowerImage.enabled = attackPowerSprite != null;
        SetRect(attackPowerImage.rectTransform, new Vector2(0f, 1f), new Vector2(662f, -54f), new Vector2(304f, 92f));
        SetRect(budgetText.rectTransform, new Vector2(0f, 1f), new Vector2(732f, -55f), new Vector2(148f, 62f));
        budgetText.fontSize = 38;
        budgetText.alignment = TextAnchor.MiddleCenter;
        goldText.transform.SetAsLastSibling();
        budgetText.transform.SetAsLastSibling();

        shopButton = CreateButton("ShopButton", hud.transform, "SHOP", new Vector2(-410f, 0f), ToggleShop, new Vector2(150f, 58f), new Vector2(1f, 0.5f));
        gameSoundButton = CreateButton("GameSoundButton", hud.transform, "SOUND ON", new Vector2(-260f, 0f), ToggleSound, new Vector2(150f, 58f), new Vector2(1f, 0.5f));
        startBattleButton = CreateButton("StartBattleButton", hud.transform, "", new Vector2(-92f, 0f), StartBattle, new Vector2(150f, 58f), new Vector2(1f, 0.5f));
        ApplyButtonSprite(shopButton, shopButtonSprite, true);
        ApplyButtonSprite(gameSoundButton, soundOn ? soundOnSprite : soundOffSprite, true);
        ApplyButtonSprite(startBattleButton, startButtonSprite, true);
    }

    private Sprite GetShopIcon(int index)
    {
        switch (index)
        {
            case 0:
                return shopArcherIcon != null ? shopArcherIcon : towerConfigs[index].sprite;
            case 1:
                return shopMageIcon != null ? shopMageIcon : towerConfigs[index].sprite;
            case 2:
                return shopFreezerIcon != null ? shopFreezerIcon : towerConfigs[index].sprite;
            case 3:
                return shopCannonIcon != null ? shopCannonIcon : towerConfigs[index].sprite;
            default:
                return towerConfigs[index].sprite;
        }
    }

    private void CreateTowerBar(Transform parent)
    {
        towerShopPanel = CreatePanel("TowerShop", parent, new Color(0f, 0f, 0f, 0.28f));
        Image windowImage = CreateImage("ShopWindow", towerShopPanel.transform, shopPanelSprite);
        windowImage.preserveAspect = true;
        SetRect(windowImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1120f, 900f));

        towerButtons = new Button[towerConfigs.Length];
        towerButtons[0] = CreateTransparentButton("ArcherChoice", windowImage.transform, new Vector2(-270f, -55f), new Vector2(270f, 245f), () => SelectTower(0));
        towerButtons[1] = CreateTransparentButton("MageChoice", windowImage.transform, new Vector2(0f, -55f), new Vector2(270f, 245f), () => SelectTower(1));
        towerButtons[2] = CreateTransparentButton("FreezerChoice", windowImage.transform, new Vector2(270f, -55f), new Vector2(270f, 245f), () => SelectTower(2));
        towerButtons[3] = CreateTransparentButton("CannonChoice", windowImage.transform, new Vector2(0f, -305f), new Vector2(270f, 245f), () => SelectTower(3));

        towerShopPanel.SetActive(false);
    }

    private IEnumerator SpawnAiWave()
    {
        spawning = true;
        int spent = 0;
        int count = 0;

        while (count < maxEnemiesPerWave)
        {
            EnemyConfig config = PickEnemyForRound(attackBudget - spent);
            if (config == null)
            {
                break;
            }

            spent += config.attackCost;
            count++;
            SpawnEnemy(config);
            yield return new WaitForSeconds(spawnInterval);
        }

        spawning = false;
    }

    private EnemyConfig PickEnemyForRound(int remainingBudget)
    {
        List<EnemyConfig> options = new List<EnemyConfig>();
        foreach (EnemyConfig config in enemyConfigs)
        {
            if ((config.name == "Ghost" && currentRound < 3) || (config.name == "Orc" && currentRound < 2))
            {
                continue;
            }

            if (config.attackCost <= remainingBudget)
            {
                options.Add(config);
            }
        }

        if (options.Count == 0)
        {
            return null;
        }

        int strongChance = Mathf.Clamp(currentRound * 8, 0, 75);
        if (Random.Range(0, 100) < strongChance)
        {
            options.Sort((a, b) => b.attackCost.CompareTo(a.attackCost));
            return options[0];
        }

        return options[Random.Range(0, options.Count)];
    }

    public float GetEnemyHealthMultiplier()
    {
        return 1f + Mathf.Max(0, currentRound - 1) * 0.18f;
    }

    private void SpawnEnemy(EnemyConfig config)
    {
        SpawnEnemy(config, Vector3.zero);
    }

    private void SpawnEnemy(EnemyConfig config, Vector3 startOffset)
    {
        CompleteEnemy enemy = enemyPool.Count > 0 ? enemyPool.Dequeue() : CreateEnemy();
        enemy.transform.SetParent(enemyRoot);
        enemy.transform.position = pathPoints[0] + startOffset;
        enemy.gameObject.SetActive(true);
        enemy.Initialize(this, config);
        activeEnemies.Add(enemy);
    }

    private void SpawnTestEnemies()
    {
        for (int i = 0; i < TestEnemyCount; i++)
        {
            EnemyConfig config = enemyConfigs[Random.Range(0, enemyConfigs.Length)];
            Vector3 offset = new Vector3(Random.Range(-0.55f, 0.12f), Random.Range(-0.32f, 0.32f), 0f);
            SpawnEnemy(config, offset);
        }
    }

    private void FinishRound()
    {
        if (baseHp <= 0)
        {
            EndGame(false);
            return;
        }

        if (currentRound >= totalRounds)
        {
            EndGame(true);
            return;
        }

        currentRound++;
        attackBudget += budgetIncreasePerRound;
        gold += roundGoldBonus;
        state = GameState.Preparation;
        StopMusicSequence();
        PlayMusic(menuMusic, true);
        RefreshHud();
    }

    private void EndGame(bool defenderWon)
    {
        state = GameState.GameOver;
        CloseUpgradeMenu();
        HideRangeIndicator();
        gameOverPanel.SetActive(true);
        gameOverText.text = defenderWon ? "DEFENDER WINS" : "ATTACKER WINS";
        StopMusicSequence();
        PlayMusic(defenderWon ? winMusic : loseMusic, false);
        RefreshHud();
    }

    private void ShowMenu()
    {
        state = GameState.Menu;
        menuPanel.SetActive(true);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        CloseShop();
        if (buildSpotRoot != null)
        {
            buildSpotRoot.SetActive(false);
        }
        if (baseHealthRoot != null)
        {
            baseHealthRoot.gameObject.SetActive(false);
        }
        CloseUpgradeMenu();
        HideRangeIndicator();
        StopMusicSequence();
        PlayMusic(menuMusic, true);
    }

    private void CloseShop()
    {
        shopOpen = false;
        if (towerShopPanel != null)
        {
            towerShopPanel.SetActive(false);
        }
    }

    private void HandleBuildInput(Vector2 screenPosition)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        world.z = 0f;
        if (HandleUpgradeMenuClick(world))
        {
            return;
        }

        if (TryFindTowerAt(world, out CompleteTower tower))
        {
            SelectPlacedTower(tower);
            return;
        }

        CloseUpgradeMenu();
        if (state != GameState.Preparation)
        {
            return;
        }

        if (TryGetNearestBuildSpot(world, out Vector3 buildSpot))
        {
            TryBuildTower(buildSpot);
        }
    }

    private void UpdateRangePreview()
    {
        if ((state != GameState.Preparation && state != GameState.Battle) || mainCamera == null || shopOpen)
        {
            HideRangeIndicator();
            return;
        }

        if (selectedTower != null && selectedTower.gameObject.activeInHierarchy)
        {
            ShowRangeIndicator(selectedTower.transform.position, selectedTower.Range, new Color(1f, 0.86f, 0.28f, 0.45f));
            return;
        }

        Vector3 world = mainCamera.ScreenToWorldPoint(GetPointerScreenPosition());
        world.z = 0f;

        if (TryFindTowerAt(world, out CompleteTower tower))
        {
            ShowRangeIndicator(tower.transform.position, tower.Range, new Color(1f, 0.86f, 0.28f, 0.45f));
            return;
        }

        if (state != GameState.Preparation)
        {
            HideRangeIndicator();
            return;
        }

        if (TryGetNearestBuildSpot(world, out Vector3 buildSpot))
        {
            TowerConfig config = towerConfigs[selectedTowerIndex];
            bool canBuild = gold >= config.cost && CanBuildAt(buildSpot);
            Color color = canBuild ? new Color(0.22f, 0.72f, 1f, 0.42f) : new Color(1f, 0.2f, 0.12f, 0.35f);
            ShowRangeIndicator(buildSpot, config.range, color);
            return;
        }

        HideRangeIndicator();
    }

    private bool TryFindTowerAt(Vector3 point, out CompleteTower tower)
    {
        tower = null;
        float bestDistance = float.MaxValue;
        foreach (CompleteTower candidate in towers)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(point, candidate.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                tower = candidate;
            }
        }

        return tower != null && bestDistance <= 0.55f;
    }

    private void SelectPlacedTower(CompleteTower tower)
    {
        if (selectedTower != null && selectedTower != tower)
        {
            selectedTower.SetSelected(false);
        }

        selectedTower = tower;
        selectedTower.SetSelected(true);
        if (state == GameState.Preparation)
        {
            ShowUpgradeMenu(tower);
        }
        else if (upgradeMenuRoot != null)
        {
            upgradeMenuRoot.SetActive(false);
        }
        ShowRangeIndicator(tower.transform.position, tower.Range, new Color(1f, 0.86f, 0.28f, 0.45f));
    }

    private void ShowUpgradeMenu(CompleteTower tower)
    {
        if (upgradeMenuRoot == null)
        {
            return;
        }

        upgradeMenuRoot.transform.position = tower.transform.position + new Vector3(0f, 0.2f, -0.12f);
        upgradeMenuRoot.SetActive(true);
        RefreshUpgradeMenu();
    }

    private void CloseUpgradeMenu()
    {
        if (selectedTower != null)
        {
            selectedTower.SetSelected(false);
        }

        selectedTower = null;
        if (upgradeMenuRoot != null)
        {
            upgradeMenuRoot.SetActive(false);
        }
    }

    private void RefreshUpgradeMenu()
    {
        if (selectedTower == null)
        {
            return;
        }

        SetUpgradeCostText(upgradeSpeedCostText, selectedTower.GetUpgradeCost(TowerUpgradeType.Speed));
        SetUpgradeCostText(upgradeDamageCostText, selectedTower.GetUpgradeCost(TowerUpgradeType.Damage));
        SetUpgradeCostText(upgradeRangeCostText, selectedTower.GetUpgradeCost(TowerUpgradeType.Range));
        RefreshUpgradePips(upgradeSpeedPips, selectedTower.GetUpgradeLevel(TowerUpgradeType.Speed));
        RefreshUpgradePips(upgradeDamagePips, selectedTower.GetUpgradeLevel(TowerUpgradeType.Damage));
        RefreshUpgradePips(upgradeRangePips, selectedTower.GetUpgradeLevel(TowerUpgradeType.Range));
    }

    private void SetUpgradeCostText(TextMesh text, int cost)
    {
        if (text == null)
        {
            return;
        }

        string value = cost < 0 ? "MAX" : cost.ToString();
        Color faceColor = cost < 0
            ? new Color(1f, 0.94f, 0.68f, 1f)
            : gold < cost ? new Color(1f, 0.45f, 0.2f, 1f) : new Color(1f, 0.83f, 0.28f, 1f);

        TextMesh[] parts = text.GetComponentsInChildren<TextMesh>();
        foreach (TextMesh part in parts)
        {
            part.text = value;
            part.color = part == text ? faceColor : new Color(0.06f, 0.025f, 0f, 0.9f);
        }
    }

    private void RefreshUpgradePips(SpriteRenderer[] pips, int level)
    {
        if (pips == null)
        {
            return;
        }

        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] != null)
            {
                pips[i].enabled = i < level;
            }
        }
    }

    private bool HandleUpgradeMenuClick(Vector3 world)
    {
        if (state != GameState.Preparation)
        {
            CloseUpgradeMenu();
            return false;
        }

        if (selectedTower == null || upgradeMenuRoot == null || !upgradeMenuRoot.activeInHierarchy)
        {
            return false;
        }

        Vector3 local = upgradeMenuRoot.transform.InverseTransformPoint(world);
        if (IsInsideUpgradeButton(local, new Vector2(-1.34f, 1.5f)))
        {
            TryUpgradeSelectedTower(TowerUpgradeType.Speed);
            return true;
        }

        if (IsInsideUpgradeButton(local, new Vector2(1.34f, 1.5f)))
        {
            TryUpgradeSelectedTower(TowerUpgradeType.Damage);
            return true;
        }

        if (IsInsideUpgradeButton(local, new Vector2(0f, -0.95f)))
        {
            TryUpgradeSelectedTower(TowerUpgradeType.Range);
            return true;
        }

        if (Mathf.Abs(local.x) <= 2.55f && Mathf.Abs(local.y) <= 2.55f)
        {
            return true;
        }

        CloseUpgradeMenu();
        return false;
    }

    private bool IsInsideUpgradeButton(Vector3 local, Vector2 center)
    {
        return Mathf.Abs(local.x - center.x) <= 0.72f && Mathf.Abs(local.y - center.y) <= 0.72f;
    }

    private void TryUpgradeSelectedTower(TowerUpgradeType type)
    {
        if (state != GameState.Preparation || selectedTower == null)
        {
            return;
        }

        int cost = selectedTower.GetUpgradeCost(type);
        if (cost < 0 || gold < cost)
        {
            return;
        }

        gold -= cost;
        selectedTower.Upgrade(type);
        PlaySfx(coinSound, 0.42f);
        RefreshUpgradeMenu();
        RefreshHud();
    }

    private void ShowRangeIndicator(Vector3 position, float range, Color color)
    {
        if (rangeIndicator == null || rangeIndicatorRenderer == null)
        {
            return;
        }

        rangeIndicator.transform.position = new Vector3(position.x, position.y, 0.05f);
        rangeIndicator.transform.localScale = new Vector3(range * 2f, range * 2f, 1f);
        rangeIndicatorRenderer.color = color;
        rangeIndicator.SetActive(true);
    }

    private void HideRangeIndicator()
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
        }
    }

    private bool WasPrimaryClickThisFrame()
    {
        return Input.GetMouseButtonDown(0);
    }

    private Vector2 GetPointerScreenPosition()
    {
        return Input.mousePosition;
    }

    private bool HandleManualUiClick(Vector2 screenPosition)
    {
        if (state == GameState.Menu)
        {
            if (TryClickButton(menuStartButton, screenPosition, StartNewGame)) return true;
            if (TryClickButton(menuTestButton, screenPosition, StartTestGame)) return true;
            if (TryClickButton(menuSoundButton, screenPosition, ToggleSound)) return true;
            if (TryClickButton(menuExitButton, screenPosition, ExitGame)) return true;
            return true;
        }

        if (state == GameState.GameOver)
        {
            if (TryClickButton(restartButton, screenPosition, StartNewGame)) return true;
            if (TryClickButton(backToMenuButton, screenPosition, ShowMenu)) return true;
            return true;
        }

        if (TryClickButton(startBattleButton, screenPosition, StartBattle))
        {
            return true;
        }

        if (TryClickButton(shopButton, screenPosition, ToggleShop))
        {
            return true;
        }

        if (TryClickButton(gameSoundButton, screenPosition, ToggleSound))
        {
            return true;
        }

        for (int i = 0; i < towerButtons.Length; i++)
        {
            int captured = i;
            if (TryClickButton(towerButtons[i], screenPosition, () => SelectTower(captured)))
            {
                return true;
            }
        }

        if (shopOpen && towerShopPanel != null && towerShopPanel.activeInHierarchy)
        {
            return true;
        }

        return false;
    }

    private bool TryClickButton(Button button, Vector2 screenPosition, UnityEngine.Events.UnityAction action)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
        {
            return false;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null))
        {
            return false;
        }

        action.Invoke();
        return true;
    }

    private Vector3 SnapToGrid(Vector3 point)
    {
        const float cell = 0.62f;
        return new Vector3(Mathf.Round(point.x / cell) * cell, Mathf.Round(point.y / cell) * cell, 0f);
    }

    private bool TryGetNearestBuildSpot(Vector3 point, out Vector3 buildSpot)
    {
        const float clickRadius = 0.31f;
        buildSpot = Vector3.zero;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < buildSpots.Length; i++)
        {
            float distance = Vector3.Distance(point, buildSpots[i]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                buildSpot = buildSpots[i];
            }
        }

        return bestDistance <= clickRadius;
    }

    private void TryBuildTower(Vector3 position)
    {
        TowerConfig config = towerConfigs[selectedTowerIndex];
        if (gold < config.cost || !CanBuildAt(position))
        {
            return;
        }

        gold -= config.cost;
        CreateTower(config, position);
        PlaySfx(buildSound, 0.9f);
        RefreshHud();
    }

    private CompleteTower CreateTower(TowerConfig config, Vector3 position)
    {
        GameObject towerObject = new GameObject(config.name);
        towerObject.transform.SetParent(towerRoot);
        towerObject.transform.position = position;
        CompleteTower tower = towerObject.AddComponent<CompleteTower>();
        tower.Initialize(this, config);
        towers.Add(tower);
        return tower;
    }

    private void BuildTestTowers()
    {
        Vector3[] positions =
        {
            new Vector3(-6.2f, 1.45f, 0f),
            new Vector3(-5.35f, -1.15f, 0f),
            new Vector3(-4.85f, 2.35f, 0f),
            new Vector3(-4.05f, -2.25f, 0f),
            new Vector3(-3.1f, 0.95f, 0f),
            new Vector3(-2.35f, -2.65f, 0f),
            new Vector3(-1.35f, 2.85f, 0f),
            new Vector3(-0.75f, -0.65f, 0f),
            new Vector3(0.05f, 1.25f, 0f),
            new Vector3(0.65f, -2.65f, 0f),
            new Vector3(1.35f, 2.9f, 0f),
            new Vector3(2.15f, 0.2f, 0f),
            new Vector3(2.65f, -2.6f, 0f),
            new Vector3(3.25f, 1.55f, 0f),
            new Vector3(4.05f, -0.9f, 0f),
            new Vector3(4.75f, 2.55f, 0f),
            new Vector3(5.2f, 0.85f, 0f),
            new Vector3(5.55f, -2.05f, 0f),
            new Vector3(-6.55f, -2.65f, 0f),
            new Vector3(6.25f, 2.0f, 0f)
        };

        for (int i = 0; i < TestTowerCount; i++)
        {
            TowerConfig config = towerConfigs[i % towerConfigs.Length];
            CreateTower(config, positions[i]);
        }
    }

    private bool CanBuildAt(Vector3 position)
    {
        if (!IsBuildSpot(position))
        {
            return false;
        }

        if (DistanceToPath(position) < 0.72f)
        {
            return false;
        }

        foreach (CompleteTower tower in towers)
        {
            if (Vector3.Distance(tower.transform.position, position) < 0.78f)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBuildSpot(Vector3 position)
    {
        for (int i = 0; i < buildSpots.Length; i++)
        {
            if (Vector3.Distance(position, buildSpots[i]) <= 0.02f)
            {
                return true;
            }
        }

        return false;
    }

    private float DistanceToPath(Vector3 point)
    {
        float best = float.MaxValue;
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            best = Mathf.Min(best, DistanceToSegment(point, pathPoints[i], pathPoints[i + 1]));
        }

        return best;
    }

    private float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / Mathf.Max(0.001f, Vector3.Dot(ab, ab));
        t = Mathf.Clamp01(t);
        return Vector3.Distance(point, a + ab * t);
    }

    private void RefreshHud()
    {
        if (goldText == null)
        {
            return;
        }

        roundText.text = "Round: " + currentRound + "/" + totalRounds;
        goldText.text = gold.ToString();
        hpText.text = "Base HP: " + baseHp;
        budgetText.text = attackBudget.ToString();
        stateText.text = state.ToString() + (soundOn ? "" : " | Muted");
        RefreshWaveBadge();
        RefreshBaseHealthBar();
        startBattleButton.interactable = state == GameState.Preparation;
        ApplyButtonSprite(startBattleButton, startButtonSprite, true);
        shopButton.interactable = state == GameState.Preparation;
        ApplyButtonSprite(shopButton, shopButtonSprite, true);
        Text shopText = shopButton.GetComponentInChildren<Text>();
        if (shopText != null)
        {
            shopText.text = shopButtonSprite == null ? (shopOpen ? "CLOSE" : "SHOP") : "";
        }

        ApplyButtonSprite(menuSoundButton, soundOn ? soundOnSprite : soundOffSprite, true);
        ApplyButtonSprite(gameSoundButton, soundOn ? soundOnSprite : soundOffSprite, true);
        Text gameSoundText = gameSoundButton.GetComponentInChildren<Text>();
        if (gameSoundText != null)
        {
            gameSoundText.text = (soundOn ? soundOnSprite : soundOffSprite) == null ? (soundOn ? "SOUND ON" : "SOUND OFF") : "";
        }

        Image gameSoundImage = gameSoundButton.GetComponent<Image>();
        if (gameSoundImage != null)
        {
            gameSoundImage.sprite = soundOn ? soundOnSprite : soundOffSprite;
            gameSoundImage.color = (soundOn ? soundOnSprite : soundOffSprite) == null ? new Color(0.42f, 0.28f, 0.13f, 0.95f) : Color.white;
        }

        ApplyButtonSprite(restartButton, restartButtonSprite, true);
        ApplyButtonSprite(backToMenuButton, menuButtonSprite, true);

        if (buildSpotRoot != null)
        {
            buildSpotRoot.SetActive(state == GameState.Preparation);
        }

        if (baseHealthRoot != null)
        {
            baseHealthRoot.gameObject.SetActive(state != GameState.Menu);
        }

        for (int i = 0; i < towerButtons.Length; i++)
        {
            Image image = towerButtons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.clear;
            }
        }
    }

    private void RefreshWaveBadge()
    {
        if (waveImage == null || waveSprites == null || waveSprites.Length == 0)
        {
            return;
        }

        int index = Mathf.Clamp(currentRound - 1, 0, waveSprites.Length - 1);
        waveImage.sprite = waveSprites[index];
        waveImage.enabled = waveImage.sprite != null && state != GameState.Menu;
    }

    private void RefreshBaseHealthBar()
    {
        if (baseHealthFill == null)
        {
            return;
        }

        float percent = startingBaseHp <= 0 ? 0f : Mathf.Clamp01((float)baseHp / startingBaseHp);
        const float fullWidth = 1.189482f;
        baseHealthFill.localScale = new Vector3(fullWidth * percent, 0.145f, 1f);
        baseHealthFill.localPosition = new Vector3(5.9904f - fullWidth * 0.5f * (1f - percent), 3.419f, -0.01f);
        if (baseHealthFillRenderer != null)
        {
            baseHealthFillRenderer.color = Color.Lerp(new Color(0.9f, 0.18f, 0.08f, 0.95f), new Color(0.15f, 0.95f, 0.32f, 0.95f), percent);
        }
    }

    private void ClearBattlefield()
    {
        foreach (CompleteEnemy enemy in activeEnemies.ToArray())
        {
            ReturnEnemy(enemy);
        }
        activeEnemies.Clear();

        foreach (CompleteTower tower in towers)
        {
            if (tower != null)
            {
                Destroy(tower.gameObject);
            }
        }
        towers.Clear();

        foreach (CompleteProjectile projectile in projectileRoot.GetComponentsInChildren<CompleteProjectile>(true))
        {
            if (projectile.gameObject.activeInHierarchy)
            {
                ReturnProjectile(projectile);
            }
        }
    }

    private void PrewarmPools()
    {
        int enemyPrewarmCount = Mathf.Max(maxEnemiesPerWave, TestEnemyCount);
        for (int i = 0; i < enemyPrewarmCount; i++)
        {
            ReturnEnemy(CreateEnemy());
        }

        for (int i = 0; i < TestProjectilePoolSize; i++)
        {
            ReturnProjectile(CreateProjectile());
        }
    }

    private CompleteEnemy CreateEnemy()
    {
        GameObject obj = new GameObject("Enemy");
        obj.transform.SetParent(enemyRoot);
        CompleteEnemy enemy = obj.AddComponent<CompleteEnemy>();
        return enemy;
    }

    private CompleteProjectile CreateProjectile()
    {
        GameObject obj = new GameObject("Projectile");
        obj.transform.SetParent(projectileRoot);
        CompleteProjectile projectile = obj.AddComponent<CompleteProjectile>();
        return projectile;
    }

    private void ReturnEnemy(CompleteEnemy enemy)
    {
        enemy.gameObject.SetActive(false);
        enemyPool.Enqueue(enemy);
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = color.a > 0.01f;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action, Vector2? size = null, Vector2? anchor = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.42f, 0.28f, 0.13f, 0.95f);
        Button button = obj.AddComponent<Button>();
        button.onClick.AddListener(action);
        SetRect(obj.GetComponent<RectTransform>(), anchor ?? new Vector2(0.5f, 0.5f), anchoredPosition, size ?? new Vector2(360f, 96f));

        Text text = CreateText("Text", obj.transform, label, 34, TextAnchor.MiddleCenter);
        SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size ?? new Vector2(350f, 88f));
        return button;
    }

    private Button CreateTransparentButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;
        Button button = obj.AddComponent<Button>();
        button.onClick.AddListener(action);
        SetRect(obj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), anchoredPosition, size);
        return button;
    }

    private void ApplyButtonSprite(Button button, Sprite sprite, bool hideText)
    {
        if (button == null || sprite == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
        }

        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = hideText ? "" : text.text;
        }
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = new Color(1f, 0.9f, 0.68f);
        text.raycastTarget = false;
        return text;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        return image;
    }

    private void SetRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static Sprite MakeSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void FitWorldSprite(SpriteRenderer renderer, float targetHeight)
    {
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        float height = renderer.sprite.bounds.size.y;
        if (height <= 0.001f)
        {
            return;
        }

        float scale = targetHeight / height;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static Vector3 GetWorldSpriteScale(Sprite sprite, float targetWidth, float targetHeight)
    {
        if (sprite == null || sprite.bounds.size.x <= 0.001f || sprite.bounds.size.y <= 0.001f)
        {
            return new Vector3(targetWidth, targetHeight, 1f);
        }

        return new Vector3(targetWidth / sprite.bounds.size.x, targetHeight / sprite.bounds.size.y, 1f);
    }

    private static Sprite MakeCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = (size - 1) * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite MakeRangeSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = (size - 1) * 0.5f;
        float edgeWidth = 3.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance > radius)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float edge = Mathf.Clamp01((edgeWidth - Mathf.Abs(radius - distance)) / edgeWidth);
                float fill = Mathf.Clamp01((radius - distance) / radius);
                float alpha = 0.1f + edge * 0.45f + fill * 0.05f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

public sealed class CompleteEnemy : MonoBehaviour
{
    private CompleteTowerDefenseGame game;
    private EnemyConfig config;
    private SpriteRenderer spriteRenderer;
    private Transform hpFill;
    private int pathIndex;
    private float health;
    private float slowTimer;
    private float slowMultiplier = 1f;

    public float Progress { get; private set; }
    public float Health => health;

    private void Awake()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 10;
        GameObject bar = new GameObject("HealthBar");
        bar.transform.SetParent(transform);
        bar.transform.localPosition = new Vector3(0f, 0.84f, 0f);
        SpriteRenderer bg = bar.AddComponent<SpriteRenderer>();
        bg.sprite = MakeSprite(Color.black);
        bg.sortingOrder = 30;
        bar.transform.localScale = new Vector3(0.72f, 0.07f, 1f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(bar.transform);
        fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        SpriteRenderer fg = fill.AddComponent<SpriteRenderer>();
        fg.sprite = MakeSprite(Color.green);
        fg.sortingOrder = 31;
        hpFill = fill.transform;
    }

    public void Initialize(CompleteTowerDefenseGame owner, EnemyConfig data)
    {
        game = owner;
        config = data;
        health = config.maxHealth * game.GetEnemyHealthMultiplier();
        pathIndex = 1;
        slowTimer = 0f;
        slowMultiplier = 1f;
        spriteRenderer.sprite = config.sprite != null ? config.sprite : MakeSprite(config.tint);
        FitSprite(spriteRenderer, 0.92f);
        RefreshHp();
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                slowMultiplier = 1f;
            }
        }

        Vector3 target = game.GetPathPoint(pathIndex);
        transform.position = Vector3.MoveTowards(transform.position, target, config.speed * slowMultiplier * Time.deltaTime);
        Progress = game.GetPathProgress(pathIndex, transform.position);

        if (Vector3.Distance(transform.position, target) <= 0.025f)
        {
            pathIndex++;
            if (pathIndex >= game.PathPointCount)
            {
                game.NotifyEnemyReachedBase(this, config.baseDamage);
            }
        }
    }

    public void Damage(float amount)
    {
        game.SpawnDamageText(transform.position, amount);
        health -= amount;
        RefreshHp();
        if (health <= 0f)
        {
            game.NotifyEnemyKilled(this, config.rewardGold);
        }
    }

    public void Slow(float multiplier, float duration)
    {
        if (config.ignoresSlow)
        {
            return;
        }

        slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    private void RefreshHp()
    {
        hpFill.localScale = new Vector3(Mathf.Clamp01(health / config.maxHealth), 1f, 1f);
    }

    private static Sprite MakeSprite(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void FitSprite(SpriteRenderer renderer, float targetHeight)
    {
        if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
        {
            return;
        }

        float scale = targetHeight / renderer.sprite.bounds.size.y;
        renderer.transform.localScale = Vector3.one * scale;
    }
}

public sealed class CompleteTower : MonoBehaviour
{
    private const int MaxUpgradeLevel = 3;

    private CompleteTowerDefenseGame game;
    private TowerConfig config;
    private SpriteRenderer spriteRenderer;
    private float cooldown;
    private int speedLevel;
    private int damageLevel;
    private int rangeLevel;
    private bool selected;

    public float Range => config != null ? config.range + rangeLevel * 0.35f : 0f;
    public float Damage => config != null ? config.damage * (1f + damageLevel * 0.2f) : 0f;
    public float FireRate => config != null ? config.fireRate * (1f + speedLevel * 0.22f) : 0f;

    public void Initialize(CompleteTowerDefenseGame owner, TowerConfig data)
    {
        game = owner;
        config = data;
        speedLevel = 0;
        damageLevel = 0;
        rangeLevel = 0;
        selected = false;
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = config.sprite;
        spriteRenderer.sortingOrder = 8;
        UpdateVisuals();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        UpdateVisuals();
    }

    public int GetUpgradeCost(TowerUpgradeType type)
    {
        int level = GetUpgradeLevel(type);
        if (level >= MaxUpgradeLevel)
        {
            return -1;
        }

        int baseCost;
        switch (type)
        {
            case TowerUpgradeType.Speed:
                baseCost = 70;
                break;
            case TowerUpgradeType.Damage:
                baseCost = 95;
                break;
            default:
                baseCost = 125;
                break;
        }

        return baseCost + level * 65 + Mathf.RoundToInt(config.cost * 0.18f);
    }

    public void Upgrade(TowerUpgradeType type)
    {
        switch (type)
        {
            case TowerUpgradeType.Speed:
                speedLevel = Mathf.Min(MaxUpgradeLevel, speedLevel + 1);
                break;
            case TowerUpgradeType.Damage:
                damageLevel = Mathf.Min(MaxUpgradeLevel, damageLevel + 1);
                break;
            case TowerUpgradeType.Range:
                rangeLevel = Mathf.Min(MaxUpgradeLevel, rangeLevel + 1);
                break;
        }

        UpdateVisuals();
    }

    public int GetUpgradeLevel(TowerUpgradeType type)
    {
        switch (type)
        {
            case TowerUpgradeType.Speed:
                return speedLevel;
            case TowerUpgradeType.Damage:
                return damageLevel;
            default:
                return rangeLevel;
        }
    }

    private void UpdateVisuals()
    {
        Color color = config.sprite == null ? config.tint : Color.white;
        color.a = selected ? 0.58f : 1f;
        spriteRenderer.color = color;
        FitSprite(spriteRenderer, 1.38f + (speedLevel + damageLevel + rangeLevel) * 0.035f);
    }

    private void Update()
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
        {
            return;
        }

        CompleteEnemy target = FindTarget();
        if (target == null)
        {
            return;
        }

        CompleteProjectile projectile = game.GetProjectile();
        projectile.Initialize(game, config, target, transform.position + new Vector3(0f, 0.25f, 0f), Damage);
        game.PlayTowerAttackSound(config.name);
        cooldown = 1f / FireRate;
    }

    private CompleteEnemy FindTarget()
    {
        CompleteEnemy best = null;
        float bestProgress = -1f;
        foreach (CompleteEnemy enemy in game.ActiveEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (Vector3.Distance(transform.position, enemy.transform.position) > Range)
            {
                continue;
            }

            if (enemy.Progress > bestProgress)
            {
                bestProgress = enemy.Progress;
                best = enemy;
            }
        }

        return best;
    }

    private static void FitSprite(SpriteRenderer renderer, float targetHeight)
    {
        if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
        {
            return;
        }

        float scale = targetHeight / renderer.sprite.bounds.size.y;
        renderer.transform.localScale = Vector3.one * scale;
    }
}

public sealed class CompleteProjectile : MonoBehaviour
{
    private CompleteTowerDefenseGame game;
    private TowerConfig config;
    private CompleteEnemy target;
    private SpriteRenderer spriteRenderer;
    private float lifetime;
    private float damage;

    private void Awake()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 20;
    }

    public void Initialize(CompleteTowerDefenseGame owner, TowerConfig data, CompleteEnemy newTarget, Vector3 position, float shotDamage)
    {
        game = owner;
        config = data;
        target = newTarget;
        transform.position = position;
        lifetime = 4f;
        damage = shotDamage;
        spriteRenderer.sprite = config.projectileSprite;
        bool isCannon = config.name == "Cannon";
        bool isArcher = config.name == "Archer";
        bool isFreezer = config.name == "Freezer";
        spriteRenderer.color = isCannon || isArcher || isFreezer ? Color.white : config.tint;
        FitSprite(spriteRenderer, GetProjectileHeight(config.name));
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f || target == null || !target.gameObject.activeInHierarchy)
        {
            game.ReturnProjectile(this);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, config.projectileSpeed * Time.deltaTime);
        Vector3 direction = target.transform.position - transform.position;
        if (direction.sqrMagnitude > 0.001f && config.name != "Cannon")
        {
            Vector3 sourceAxis = config.name == "Archer" ? Vector3.up : new Vector3(1f, 1f, 0f).normalized;
            transform.rotation = Quaternion.FromToRotation(sourceAxis, direction.normalized);
        }

        if (Vector3.Distance(transform.position, target.transform.position) < 0.12f)
        {
            Hit();
        }
    }

    private void Hit()
    {
        if (config.splashRadius > 0.01f)
        {
            List<CompleteEnemy> snapshot = new List<CompleteEnemy>(game.ActiveEnemies);
            foreach (CompleteEnemy enemy in snapshot)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && Vector3.Distance(transform.position, enemy.transform.position) <= config.splashRadius)
                {
                    Apply(enemy);
                }
            }
        }
        else
        {
            Apply(target);
        }

        game.ReturnProjectile(this);
    }

    private void Apply(CompleteEnemy enemy)
    {
        enemy.Damage(damage);
        if (config.slowDuration > 0f)
        {
            enemy.Slow(config.slowMultiplier, config.slowDuration);
        }
    }

    private static void FitSprite(SpriteRenderer renderer, float targetHeight)
    {
        if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
        {
            return;
        }

        float scale = targetHeight / renderer.sprite.bounds.size.y;
        renderer.transform.localScale = Vector3.one * scale;
    }

    private static float GetProjectileHeight(string towerName)
    {
        switch (towerName)
        {
            case "Archer":
                return 0.52f;
            case "Freezer":
                return 0.48f;
            case "Cannon":
                return 0.42f;
            default:
                return 0.26f;
        }
    }
}

public sealed class TowerConfig
{
    public readonly string name;
    public readonly int cost;
    public readonly float range;
    public readonly float fireRate;
    public readonly float damage;
    public readonly float projectileSpeed;
    public readonly float splashRadius;
    public readonly float slowDuration;
    public readonly float slowMultiplier;
    public readonly Sprite sprite;
    public readonly Sprite projectileSprite;
    public readonly Color tint;

    public TowerConfig(string name, int cost, float range, float fireRate, float damage, float projectileSpeed, float splashRadius, float slowDuration, float slowMultiplier, Sprite sprite, Sprite projectileSprite, Color tint)
    {
        this.name = name;
        this.cost = cost;
        this.range = range;
        this.fireRate = fireRate;
        this.damage = damage;
        this.projectileSpeed = projectileSpeed;
        this.splashRadius = splashRadius;
        this.slowDuration = slowDuration;
        this.slowMultiplier = slowMultiplier;
        this.sprite = sprite;
        this.projectileSprite = projectileSprite;
        this.tint = tint;
    }
}

public sealed class EnemyConfig
{
    public readonly string name;
    public readonly float maxHealth;
    public readonly float speed;
    public readonly int attackCost;
    public readonly int rewardGold;
    public readonly int baseDamage;
    public readonly bool ignoresSlow;
    public readonly Sprite sprite;
    public readonly Color tint;

    public EnemyConfig(string name, float maxHealth, float speed, int attackCost, int rewardGold, int baseDamage, bool ignoresSlow, Sprite sprite, Color tint)
    {
        this.name = name;
        this.maxHealth = maxHealth;
        this.speed = speed;
        this.attackCost = attackCost;
        this.rewardGold = rewardGold;
        this.baseDamage = baseDamage;
        this.ignoresSlow = ignoresSlow;
        this.sprite = sprite;
        this.tint = tint;
    }
}

public enum TowerUpgradeType
{
    Speed,
    Damage,
    Range
}
