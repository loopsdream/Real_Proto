using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;
using static InfiniteModeSettingsV2; // === V2 CHANGED ===

public class InfiniteModeManagerV2 : MonoBehaviour
{
    [Header("Settings")]
    public InfiniteModeSettingsV2 settings; // === V2 CHANGED ===

    [Header("Game References")]
    public InfiniteGridManager gridManager;

    [Header("UI References")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI elapsedTimeText;
    public GameObject newHighScoreText;
    public Button restartButton;
    public Button menuButton;

    [Header("Block Prefabs")]
    public GameObject[] blockPrefabs;
    public GameObject emptyBlockPrefab;

    [Header("Warning Effects")]
    public float warningBlinkSpeed = 3f;
    public Color warningColor = Color.red;
    public GameObject warningBorderPrefab;

    [Header("Movement Animation")]
    public float blockMoveAnimationDuration = 0.1f;

    [Header("Pause System")]
    public GameObject pausePanel;
    public Button pauseButton;
    public Button resumeButton;
    public Button pauseMenuButton;
    public GameObject gridBlocker;

    [Header("Effect System")]
    public CROxCROBlockEffectSystem blockEffectSystem;

    // 게임 상태
    private bool isGameActive = false;
    private float currentTimeLimit;
    private int currentScore = 0;
    private int currentCombo = 0;
    private float gameStartTime;
    private float elapsedTime;

    // 타이머
    private float moveAndGenerateTimer = 0f;

    // 그리드 데이터
    private GameObject[,] infiniteGrid;
    private List<Vector2Int> edgePositions = new List<Vector2Int>();

    // 일시정지
    private bool isPaused = false;
    private float pausedTimeLimit;
    private float pausedMoveTimer;

    // 경고 효과
    private List<GameObject> warningBlocks = new List<GameObject>();
    private Dictionary<GameObject, GameObject> warningBorders = new Dictionary<GameObject, GameObject>();
    private Coroutine warningEffectCoroutine;

    private DifficultyLevelV2 currentDifficulty; // === V2 CHANGED ===

    void Awake()
    {
        gridManager = FindFirstObjectByType<InfiniteGridManager>();
    }

    void Start()
    {
        gameStartTime = Time.time;
        InitializeInfiniteMode();
        SetupUI();
        SetupPauseSystem();
        AudioManager.Instance.PlaySceneBGM("InfiniteModeV2Scene");
        AdManager.Instance?.ShowBanner();
    }

    void SetupPauseSystem()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        if (pauseMenuButton != null)
            pauseMenuButton.onClick.AddListener(ReturnToMenuFromPause);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (gridBlocker != null)
            gridBlocker.SetActive(false);

        Debug.Log("Pause system initialized");
    }

    void InitializeInfiniteMode()
    {
        if (settings == null)
        {
            Debug.LogError("InfiniteModeSettingsV2 not assigned!");
            return;
        }

        if (gridManager != null)
        {
            gridManager.width = settings.gridWidth;
            gridManager.height = settings.gridHeight;
            gridManager.InitializeInfiniteGrid();
            gridManager.onEmptyBlockClicked = OnEmptyBlockClicked;
        }
        else
        {
            Debug.LogError("InfiniteGridManager not found!");
            return;
        }

        infiniteGrid = new GameObject[settings.gridWidth, settings.gridHeight];
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                infiniteGrid[x, y] = gridManager.GetBlockAt(x, y);
            }
        }

        CalculateEdgePositions();

        currentTimeLimit = settings.initialTimeLimit;
        currentScore = 0;
        currentCombo = 0;

        StartGame();
    }

    // === V2 CHANGED === 활성화된 면만 edgePositions에 포함
    void CalculateEdgePositions()
    {
        edgePositions.Clear();

        float gameTime = Time.time - gameStartTime;
        DifficultyLevelV2 difficulty = settings.GetCurrentDifficulty(gameTime);

        // Top
        if (difficulty.topSide.enabled)
        {
            int exclude = 1 + difficulty.topSide.extraExcludeCount;
            for (int x = exclude; x < settings.gridWidth - exclude; x++)
            {
                edgePositions.Add(new Vector2Int(x, settings.gridHeight - 1));
            }
        }

        // Bottom
        if (difficulty.bottomSide.enabled)
        {
            int exclude = 1 + difficulty.bottomSide.extraExcludeCount;
            for (int x = exclude; x < settings.gridWidth - exclude; x++)
            {
                edgePositions.Add(new Vector2Int(x, 0));
            }
        }

        // Left
        if (difficulty.leftSide.enabled)
        {
            int exclude = 1 + difficulty.leftSide.extraExcludeCount;
            for (int y = exclude; y < settings.gridHeight - exclude; y++)
            {
                edgePositions.Add(new Vector2Int(0, y));
            }
        }

        // Right
        if (difficulty.rightSide.enabled)
        {
            int exclude = 1 + difficulty.rightSide.extraExcludeCount;
            for (int y = exclude; y < settings.gridHeight - exclude; y++)
            {
                edgePositions.Add(new Vector2Int(settings.gridWidth - 1, y));
            }
        }

        Debug.Log($"V2 Edge positions: {edgePositions.Count} (T:{difficulty.topSide.enabled} B:{difficulty.bottomSide.enabled} L:{difficulty.leftSide.enabled} R:{difficulty.rightSide.enabled})");
    }

    void StartGame()
    {
        isGameActive = true;

        float gameTime = Time.time - gameStartTime;
        DifficultyLevelV2 difficulty = settings.GetCurrentDifficulty(gameTime);
        GenerateNewBlocks(difficulty);

        moveAndGenerateTimer = difficulty.moveInterval;

        if (gridManager != null)
        {
            gridManager.onEmptyBlockClicked = OnEmptyBlockClicked;
        }

        UpdateUI();
        Debug.Log("Infinite mode V2 started!");
    }

    void Update()
    {
        if (!isGameActive || isPaused) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
            return;
        }

        currentTimeLimit -= Time.deltaTime;
        if (currentTimeLimit <= 0)
        {
            GameOver("Time Over!");
            return;
        }

        float gameTime = Time.time - gameStartTime;
        currentDifficulty = settings.GetCurrentDifficulty(gameTime);

        moveAndGenerateTimer -= Time.deltaTime;
        if (moveAndGenerateTimer <= 0)
        {
            ClearWarningEffects();
            StartCoroutine(AnimatedBlockMove(currentDifficulty));

            currentDifficulty = settings.GetCurrentDifficulty(Time.time - gameStartTime);
            moveAndGenerateTimer = currentDifficulty.moveInterval;

            CalculateEdgePositions();
        }

        UpdateUI();
        UpdateDebugUI(currentDifficulty);
    }

    // === V2 CHANGED === 면별 토글 체크 + 개별 SideSpawnSetting 전달
    void GenerateNewBlocks(DifficultyLevelV2 difficulty)
    {
        // Top
        if (difficulty.topSide.enabled)
        {
            int exclude = 1 + difficulty.topSide.extraExcludeCount;
            GenerateBlocksOnLine(
                exclude,
                settings.gridWidth - exclude - 1,
                settings.gridHeight - 1,
                settings.gridHeight - 1,
                difficulty.topSide
            );
        }

        // Bottom
        if (difficulty.bottomSide.enabled)
        {
            int exclude = 1 + difficulty.bottomSide.extraExcludeCount;
            GenerateBlocksOnLine(
                exclude,
                settings.gridWidth - exclude - 1,
                0,
                0,
                difficulty.bottomSide
            );
        }

        // Left
        if (difficulty.leftSide.enabled)
        {
            int exclude = 1 + difficulty.leftSide.extraExcludeCount;
            GenerateBlocksOnLine(
                0,
                0,
                exclude,
                settings.gridHeight - exclude - 1,
                difficulty.leftSide
            );
        }

        // Right
        if (difficulty.rightSide.enabled)
        {
            int exclude = 1 + difficulty.rightSide.extraExcludeCount;
            GenerateBlocksOnLine(
                settings.gridWidth - 1,
                settings.gridWidth - 1,
                exclude,
                settings.gridHeight - exclude - 1,
                difficulty.rightSide
            );
        }
    }

    // === V2 CHANGED === DifficultyLevel 대신 SideSpawnSetting을 받음
    void GenerateBlocksOnLine(int startX, int endX, int startY, int endY, SideSpawnSetting spawnSetting)
    {
        List<Vector2Int> availablePositions = new List<Vector2Int>();

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (IsPositionEmpty(x, y))
                {
                    availablePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        if (availablePositions.Count == 0) return;

        // === V2: SideSpawnSetting에서 확률 가져오기 ===
        int minBlocks = Mathf.Max(1, Mathf.RoundToInt(availablePositions.Count * spawnSetting.minSpawnChance));
        int maxBlocks = Mathf.RoundToInt(availablePositions.Count * spawnSetting.maxSpawnChance);
        int blocksToSpawn = Random.Range(minBlocks, maxBlocks + 1);

        string lineType = "";
        if (startY == settings.gridHeight - 1) lineType = "Top";
        else if (startY == 0 && endY == 0) lineType = "Bottom";
        else if (startX == 0) lineType = "Left";
        else lineType = "Right";

        Debug.Log($"V2 {lineType}: {availablePositions.Count} available, spawning {blocksToSpawn} (min:{spawnSetting.minSpawnChance} max:{spawnSetting.maxSpawnChance})");

        List<Vector2Int> tempPositions = new List<Vector2Int>(availablePositions);
        for (int i = 0; i < blocksToSpawn && tempPositions.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, tempPositions.Count);
            Vector2Int selectedPos = tempPositions[randomIndex];
            tempPositions.RemoveAt(randomIndex);
            CreateRandomBlock(selectedPos.x, selectedPos.y);
        }
    }

    void CreateEmptyBlocksAtVacatedPositions(List<BlockMoveData> moves)
    {
        HashSet<Vector2Int> vacatedPositions = new HashSet<Vector2Int>();

        foreach (BlockMoveData move in moves)
        {
            bool positionIsStillOccupied = moves.Any(m => m.toX == move.fromX && m.toY == move.fromY);
            if (!positionIsStillOccupied)
            {
                vacatedPositions.Add(new Vector2Int(move.fromX, move.fromY));
            }
        }

        foreach (Vector2Int pos in vacatedPositions)
        {
            if (infiniteGrid[pos.x, pos.y] == null)
            {
                CreateEmptyBlockAt(pos.x, pos.y);
            }
        }
    }

    void CreateRandomBlock(int x, int y)
    {
        if (infiniteGrid[x, y] != null)
        {
            gridManager.blockFactory.DestroyBlock(infiniteGrid[x, y]);
        }

        int randomBlockType = Random.Range(0, blockPrefabs.Length);
        GameObject blockPrefab = blockPrefabs[randomBlockType];

        Vector2Int moveDirection = Vector2Int.zero;
        if (y == settings.gridHeight - 1)
            moveDirection = Vector2Int.down;
        else if (y == 0)
            moveDirection = Vector2Int.up;
        else if (x == 0)
            moveDirection = Vector2Int.right;
        else if (x == settings.gridWidth - 1)
            moveDirection = Vector2Int.left;

        gridManager.CreateInfiniteBlock(blockPrefab, x, y, moveDirection);
        infiniteGrid[x, y] = gridManager.GetBlockAt(x, y);

        Debug.Log($"Created {blockPrefab.tag} at ({x}, {y}) dir {moveDirection}");
    }

    IEnumerator AnimatedBlockMove(DifficultyLevelV2 difficulty)
    {
        List<BlockMoveData> allMoves = CalculateBlockMoves();
        if (allMoves == null) yield break;

        yield return StartCoroutine(MoveBlocksToPositions(allMoves));
        UpdateGridArrayAfterMove(allMoves);
        CreateEmptyBlocksAtVacatedPositions(allMoves);
        GenerateNewBlocks(difficulty);
        PredictAndShowCollisions();
    }

    IEnumerator MoveBlocksToPositions(List<BlockMoveData> moves)
    {
        if (moves.Count == 0) yield break;

        List<BlockMoveInfo> moveInfos = new List<BlockMoveInfo>();

        foreach (BlockMoveData move in moves)
        {
            GameObject block = infiniteGrid[move.fromX, move.fromY];
            if (block != null)
            {
                Block blockComponent = block.GetComponent<Block>();
                if (blockComponent != null && !blockComponent.isEmpty)
                {
                    Vector3 startPos = block.transform.position;
                    Vector3 targetPos = gridManager.GridToWorldPosition(move.toX, move.toY);

                    moveInfos.Add(new BlockMoveInfo
                    {
                        block = block,
                        startPosition = startPos,
                        targetPosition = targetPos,
                        fromX = move.fromX,
                        fromY = move.fromY,
                        toX = move.toX,
                        toY = move.toY
                    });
                }
            }
        }

        float elapsed = 0f;
        while (elapsed < blockMoveAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, elapsed / blockMoveAnimationDuration);

            foreach (BlockMoveInfo moveInfo in moveInfos)
            {
                if (moveInfo.block != null)
                {
                    moveInfo.block.transform.position = Vector3.Lerp(moveInfo.startPosition, moveInfo.targetPosition, smoothProgress);
                }
            }
            yield return null;
        }

        foreach (BlockMoveInfo moveInfo in moveInfos)
        {
            if (moveInfo.block != null)
            {
                moveInfo.block.transform.position = moveInfo.targetPosition;
            }
        }
    }

    void UpdateGridArrayAfterMove(List<BlockMoveData> moves)
    {
        GameObject[,] newGrid = new GameObject[settings.gridWidth, settings.gridHeight];

        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                GameObject block = infiniteGrid[x, y];
                if (block != null)
                {
                    bool isMovingBlock = moves.Any(move => move.fromX == x && move.fromY == y);
                    if (!isMovingBlock)
                    {
                        newGrid[x, y] = block;
                    }
                }
            }
        }

        foreach (BlockMoveData move in moves)
        {
            GameObject movedBlock = infiniteGrid[move.fromX, move.fromY];
            if (movedBlock != null)
            {
                Block blockComponent = movedBlock.GetComponent<Block>();
                if (blockComponent != null && !blockComponent.isEmpty)
                {
                    if (newGrid[move.toX, move.toY] != null)
                    {
                        Block destBlockComp = newGrid[move.toX, move.toY].GetComponent<Block>();
                        if (destBlockComp != null && destBlockComp.isEmpty)
                        {
                            gridManager.blockFactory.DestroyBlock(newGrid[move.toX, move.toY]);
                            newGrid[move.toX, move.toY] = null;
                        }
                    }

                    newGrid[move.toX, move.toY] = movedBlock;
                    blockComponent.x = move.toX;
                    blockComponent.y = move.toY;
                    gridManager.MoveBlock(movedBlock, move.fromX, move.fromY, move.toX, move.toY);
                }
            }
        }

        infiniteGrid = newGrid;

        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                gridManager.SetBlockAt(x, y, infiniteGrid[x, y]);
            }
        }
    }

    void PredictAndShowCollisions()
    {
        ClearWarningEffects();

        List<BlockMoveData> nextMoves = SimulateNextMoves();
        if (nextMoves == null || nextMoves.Count == 0) return;

        Dictionary<Vector2Int, List<BlockMoveData>> collisionMap = new Dictionary<Vector2Int, List<BlockMoveData>>();

        foreach (var move in nextMoves)
        {
            Vector2Int destination = new Vector2Int(move.toX, move.toY);
            if (!collisionMap.ContainsKey(destination))
            {
                collisionMap[destination] = new List<BlockMoveData>();
            }
            collisionMap[destination].Add(move);
        }

        foreach (var kvp in collisionMap)
        {
            Vector2Int destination = kvp.Key;
            List<BlockMoveData> movesToDestination = kvp.Value;

            bool willCollide = movesToDestination.Count > 1;

            if (!willCollide && !IsPositionEmpty(destination.x, destination.y))
            {
                GameObject destBlock = infiniteGrid[destination.x, destination.y];
                if (destBlock != null)
                {
                    Block destBlockComponent = destBlock.GetComponent<Block>();
                    if (destBlockComponent != null && !destBlockComponent.isEmpty)
                    {
                        bool destinationBlockWillMove = nextMoves.Any(move =>
                            move.fromX == destination.x && move.fromY == destination.y);
                        if (!destinationBlockWillMove)
                        {
                            willCollide = true;
                        }
                    }
                }
            }

            if (willCollide)
            {
                foreach (var move in movesToDestination)
                {
                    GameObject block = infiniteGrid[move.fromX, move.fromY];
                    if (block != null) AddWarningEffect(block);
                }

                if (!IsPositionEmpty(destination.x, destination.y))
                {
                    GameObject destBlock = infiniteGrid[destination.x, destination.y];
                    if (destBlock != null) AddWarningEffect(destBlock);
                }
            }
        }

        if (warningBlocks.Count > 0)
        {
            warningEffectCoroutine = StartCoroutine(WarningBlinkEffect());
        }
    }

    List<BlockMoveData> SimulateNextMoves()
    {
        List<BlockMoveData> allMoves = new List<BlockMoveData>();

        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                if (!IsPositionEmpty(x, y))
                {
                    Vector2Int moveDirection = GetMoveDirection(x, y);
                    if (moveDirection != Vector2Int.zero)
                    {
                        Vector2Int newPos = new Vector2Int(x + moveDirection.x, y + moveDirection.y);
                        if (IsValidPosition(newPos.x, newPos.y))
                        {
                            allMoves.Add(new BlockMoveData(x, y, newPos.x, newPos.y));
                        }
                    }
                }
            }
        }
        return allMoves;
    }

    void AddWarningEffect(GameObject block)
    {
        if (block == null || warningBlocks.Contains(block)) return;
        warningBlocks.Add(block);

        GameObject warningBorder = CreateWarningBorder(block);
        if (warningBorder != null)
        {
            warningBorders[block] = warningBorder;
        }
    }

    GameObject CreateWarningBorder(GameObject targetBlock)
    {
        if (warningBorderPrefab == null) return null;

        GameObject border = Instantiate(warningBorderPrefab);
        border.transform.SetParent(targetBlock.transform);
        border.transform.localPosition = Vector3.zero;
        border.transform.localRotation = Quaternion.identity;
        border.transform.localScale = Vector3.one;
        border.name = "WarningBorder";

        SpriteRenderer borderRenderer = border.GetComponent<SpriteRenderer>();
        SpriteRenderer blockRenderer = targetBlock.GetComponent<SpriteRenderer>();

        if (borderRenderer != null && blockRenderer != null)
        {
            borderRenderer.sortingOrder = blockRenderer.sortingOrder + 1;
            borderRenderer.color = Color.white;
        }

        return border;
    }

    void ClearWarningEffects()
    {
        foreach (GameObject block in warningBlocks)
        {
            if (block != null && warningBorders.ContainsKey(block))
            {
                GameObject border = warningBorders[block];
                if (border != null) Destroy(border);
            }
        }

        warningBlocks.Clear();
        warningBorders.Clear();

        if (warningEffectCoroutine != null)
        {
            StopCoroutine(warningEffectCoroutine);
            warningEffectCoroutine = null;
        }
    }

    IEnumerator WarningBlinkEffect()
    {
        float timer = 0f;
        while (warningBlocks.Count > 0 && isGameActive)
        {
            timer += Time.deltaTime;
            float blinkValue = (Mathf.Sin(timer * warningBlinkSpeed * Mathf.PI) + 1f) * 0.5f;

            List<GameObject> validBlocks = new List<GameObject>();
            foreach (GameObject block in warningBlocks)
            {
                if (block != null && warningBorders.ContainsKey(block) && warningBorders[block] != null)
                {
                    validBlocks.Add(block);
                }
            }
            warningBlocks = validBlocks;

            foreach (GameObject block in warningBlocks)
            {
                if (warningBorders.ContainsKey(block))
                {
                    GameObject border = warningBorders[block];
                    if (border != null)
                    {
                        SpriteRenderer borderRenderer = border.GetComponent<SpriteRenderer>();
                        if (borderRenderer != null)
                        {
                            Color borderColor = Color.white;
                            borderColor.a = blinkValue;
                            borderRenderer.color = borderColor;
                        }
                    }
                }
            }
            yield return null;
        }
    }

    List<BlockMoveData> CalculateBlockMoves()
    {
        List<BlockMoveData> allMoves = new List<BlockMoveData>();

        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                if (!IsPositionEmpty(x, y))
                {
                    Vector2Int moveDirection = GetMoveDirection(x, y);
                    if (moveDirection != Vector2Int.zero)
                    {
                        Vector2Int newPos = new Vector2Int(x + moveDirection.x, y + moveDirection.y);
                        if (IsValidPosition(newPos.x, newPos.y))
                        {
                            allMoves.Add(new BlockMoveData(x, y, newPos.x, newPos.y));
                        }
                        else
                        {
                            GameOver("Block out of bounds!");
                            return null;
                        }
                    }
                }
            }
        }

        if (!ValidateBlockMoves(allMoves))
        {
            return null;
        }

        return allMoves;
    }

    bool ValidateBlockMoves(List<BlockMoveData> allMoves)
    {
        Dictionary<Vector2Int, List<Vector2Int>> destinationMap = new Dictionary<Vector2Int, List<Vector2Int>>();

        foreach (BlockMoveData move in allMoves)
        {
            Vector2Int destination = new Vector2Int(move.toX, move.toY);
            Vector2Int source = new Vector2Int(move.fromX, move.fromY);

            if (!destinationMap.ContainsKey(destination))
            {
                destinationMap[destination] = new List<Vector2Int>();
            }
            destinationMap[destination].Add(source);
        }

        foreach (var entry in destinationMap)
        {
            Vector2Int destination = entry.Key;
            List<Vector2Int> sources = entry.Value;

            if (sources.Count > 1)
            {
                GameOver("Block Collide!");
                return false;
            }

            if (!IsPositionEmpty(destination.x, destination.y))
            {
                bool destinationBlockWillMove = allMoves.Exists(move =>
                    move.fromX == destination.x && move.fromY == destination.y);
                if (!destinationBlockWillMove)
                {
                    GameOver("Block Collide!");
                    return false;
                }
            }
        }
        return true;
    }

    [System.Serializable]
    public class BlockMoveInfo
    {
        public GameObject block;
        public Vector3 startPosition;
        public Vector3 targetPosition;
        public int fromX, fromY;
        public int toX, toY;
    }

    Vector2Int GetMoveDirection(int x, int y)
    {
        GameObject block = infiniteGrid[x, y];
        if (block == null) return Vector2Int.zero;

        Block blockComponent = block.GetComponent<Block>();
        if (blockComponent != null && blockComponent.isEmpty) return Vector2Int.zero;

        InfiniteBlock infiniteBlockComponent = block.GetComponent<InfiniteBlock>();
        if (infiniteBlockComponent != null)
        {
            return infiniteBlockComponent.moveDirection;
        }

        return Vector2Int.zero;
    }

    void CreateEmptyBlockAt(int x, int y)
    {
        if (infiniteGrid[x, y] != null)
        {
            gridManager.blockFactory.DestroyBlock(infiniteGrid[x, y]);
            infiniteGrid[x, y] = null;
        }

        GameObject emptyBlock = gridManager.blockFactory.CreateEmptyBlock(x, y);
        gridManager.SetBlockAt(x, y, emptyBlock);
        infiniteGrid[x, y] = emptyBlock;
    }

    void OnEmptyBlockClicked(int x, int y)
    {
        if (!isGameActive || isPaused) return;

        List<GameObject> matchedBlocks = FindMatchingBlocks(x, y);

        if (matchedBlocks.Count >= 2)
        {
            DestroyBlocks(matchedBlocks);
            AddScore(matchedBlocks.Count);
            AddTime(matchedBlocks.Count);
            currentCombo++;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUI("BlockDestroy");
            }

            PredictAndShowCollisions();
        }
        else
        {
            currentTimeLimit -= settings.timePenalty;
            currentCombo = 0;
        }
    }

    List<GameObject> FindMatchingBlocks(int x, int y)
    {
        List<GameObject> allMatchedBlocks = new List<GameObject>();
        Dictionary<string, List<GameObject>> blocksByType = new Dictionary<string, List<GameObject>>();

        Vector2Int[] directions =
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

        foreach (Vector2Int dir in directions)
        {
            GameObject foundBlock = FindFirstBlockInDirection(x, y, dir.x, dir.y);
            if (foundBlock != null)
            {
                Block blockComponent = foundBlock.GetComponent<Block>();
                if (blockComponent != null && !blockComponent.isEmpty)
                {
                    string blockType = foundBlock.tag;
                    if (!blocksByType.ContainsKey(blockType))
                    {
                        blocksByType[blockType] = new List<GameObject>();
                    }
                    blocksByType[blockType].Add(foundBlock);
                }
            }
        }

        foreach (var entry in blocksByType)
        {
            if (entry.Value.Count >= 2)
            {
                allMatchedBlocks.AddRange(entry.Value);
            }
        }

        return allMatchedBlocks;
    }

    GameObject FindFirstBlockInDirection(int startX, int startY, int dirX, int dirY)
    {
        int currX = startX;
        int currY = startY;

        while (true)
        {
            currX += dirX;
            currY += dirY;

            if (currX < 0 || currX >= settings.gridWidth || currY < 0 || currY >= settings.gridHeight)
                return null;

            GameObject block = infiniteGrid[currX, currY];
            if (block != null)
            {
                Block blockComponent = block.GetComponent<Block>();
                if (blockComponent != null && !blockComponent.isEmpty)
                {
                    return block;
                }
            }
        }
    }

    void DestroyBlocks(List<GameObject> blocks)
    {
        foreach (GameObject block in blocks)
        {
            if (block == null) continue;

            Block blockComponent = block.GetComponent<Block>();
            if (blockComponent != null)
            {
                int x = blockComponent.x;
                int y = blockComponent.y;

                if (blockEffectSystem != null)
                {
                    int blockType = (block.tag == "RedBlock") ? 1 : (block.tag == "BlueBlock") ? 2 : (block.tag == "YellowBlock") ? 3 : (block.tag == "GreenBlock") ? 4 : (block.tag == "PurpleBlock") ? 5 : 1;
                    blockEffectSystem.CreateBlockDestroyEffect(x, y, blockType);
                }

                if (infiniteGrid[x, y] == block)
                {
                    infiniteGrid[x, y] = null;
                }

                Destroy(block);
            }
        }

        foreach (GameObject block in blocks)
        {
            if (block != null)
            {
                Block blockComponent = block.GetComponent<Block>();
                if (blockComponent != null)
                {
                    CreateEmptyBlockAt(blockComponent.x, blockComponent.y);
                }
            }
        }
    }

    // 일시정지
    public void PauseGame()
    {
        if (!isGameActive || isPaused) return;

        isPaused = true;
        pausedTimeLimit = currentTimeLimit;
        pausedMoveTimer = moveAndGenerateTimer;
        Time.timeScale = 0f;

        if (warningEffectCoroutine != null)
            StopCoroutine(warningEffectCoroutine);

        ShowPauseUI();
        HideGrid();
    }

    public void ResumeGame()
    {
        if (!isGameActive || !isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;
        currentTimeLimit = pausedTimeLimit;
        moveAndGenerateTimer = pausedMoveTimer;

        if (warningBlocks.Count > 0)
            warningEffectCoroutine = StartCoroutine(WarningBlinkEffect());

        HidePauseUI();
        ShowGrid();
    }

    public void ReturnToMenuFromPause()
    {
        Time.timeScale = 1f;
        AudioManager.Instance.StopBGM();
        AdManager.Instance?.DestroyBanner();
        SceneManager.LoadScene("LobbyScene");
    }

    void ShowPauseUI()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);
    }

    void HidePauseUI()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(true);
    }

    void HideGrid()
    {
        if (gridBlocker != null) gridBlocker.SetActive(true);
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                if (infiniteGrid[x, y] != null)
                    infiniteGrid[x, y].SetActive(false);
            }
        }
    }

    void ShowGrid()
    {
        if (gridBlocker != null) gridBlocker.SetActive(false);
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                if (infiniteGrid[x, y] != null)
                    infiniteGrid[x, y].SetActive(true);
            }
        }
    }

    void AddScore(int blockCount)
    {
        int baseScore = settings.GetScoreForBlockCount(blockCount);
        int comboBonus = settings.GetComboBonusScore(currentCombo);

        float gameTime = Time.time - gameStartTime;
        DifficultyLevelV2 diff = settings.GetCurrentDifficulty(gameTime);
        int difficultyBonus = (int)((float)baseScore * (diff.bonusScoreMultiplier - 1.0f));

        currentScore += baseScore + comboBonus + difficultyBonus;
    }

    void AddTime(int blockCount)
    {
        float timeBonus = settings.GetTimeBonusForBlockCount(blockCount);
        currentTimeLimit += timeBonus;
    }

    void GameOver(string reason)
    {
        isGameActive = false;
        Time.timeScale = 1f;
        ClearWarningEffects();

        elapsedTime = Time.time - gameStartTime;
        Debug.Log($"V2 Game Over: {reason}, Score: {currentScore}");

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = $"Final Score: {currentScore}";
        if (elapsedTimeText != null) elapsedTimeText.text = $"Elapsed Time: {elapsedTime}s";

        SaveHighScore();

        if (restartButton != null && UserDataManager.Instance != null)
        {
            bool hasEnergy = UserDataManager.Instance.GetEnergy() >= 1;
            restartButton.interactable = hasEnergy;
        }
    }

    void SetupUI()
    {
        if (highScoreText != null)
        {
            int highScore = PlayerPrefs.GetInt("InfiniteModeV2_HighScore", 0); // === V2: 별도 키 ===
            highScoreText.text = $"High Score: {highScore}";
        }

        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (menuButton != null) menuButton.onClick.AddListener(ReturnToMenu);
    }

    void UpdateUI()
    {
        if (timeText != null)
            timeText.text = isPaused ? "PAUSED" : $"{Mathf.Max(0, currentTimeLimit):F1}s";
        if (scoreText != null)
            scoreText.text = $"{currentScore}";
        if (comboText != null)
            comboText.text = currentCombo > 1 ? $"Combo: {currentCombo}" : "";
    }

    void UpdateDebugUI(DifficultyLevelV2 difficulty)
    {
        if (difficultyText != null)
            difficultyText.text = $"Difficulty: {difficulty.difficultyName}";
    }

    void SaveHighScore()
    {
        int currentHigh = PlayerPrefs.GetInt("InfiniteModeV2_HighScore", 0);
        if (currentScore > currentHigh)
        {
            PlayerPrefs.SetInt("InfiniteModeV2_HighScore", currentScore);
            newHighScoreText.SetActive(true);
            highScoreText.text = $"High Score: {currentScore}";
        }
    }

    bool IsPositionEmpty(int x, int y)
    {
        if (!IsValidPosition(x, y)) return false;
        GameObject block = infiniteGrid[x, y];
        if (block == null) return true;
        Block blockComponent = block.GetComponent<Block>();
        return blockComponent != null && blockComponent.isEmpty;
    }

    bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < settings.gridWidth && y >= 0 && y < settings.gridHeight;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (UserDataManager.Instance == null) return;

        if (UserDataManager.Instance.GetEnergy() >= 1)
        {
            UserDataManager.Instance.SpendEnergy(1, (success) =>
            {
                if (success)
                {
                    AdManager.Instance?.DestroyBanner();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                else
                {
                    ShowNotEnoughEnergyForRestart();
                }
            });
        }
        else
        {
            ShowNotEnoughEnergyForRestart();
        }
    }

    private void ShowNotEnoughEnergyForRestart()
    {
        GameObject energyPanel = CommonUIManager.Instance?.notEnoughEnergyPanel;
        if (energyPanel != null)
            energyPanel.SetActive(true);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        AudioManager.Instance.StopBGM();
        AdManager.Instance?.DestroyBanner();
        SceneManager.LoadScene("LobbyScene");
    }
}