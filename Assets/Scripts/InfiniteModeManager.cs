using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;
using static InfiniteModeSettings;

public class InfiniteModeManager : MonoBehaviour
{
    [Header("Settings")]
    public InfiniteModeSettings settings;

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
    public float warningBlinkSpeed = 3f; // ������ �ӵ�
    public Color warningColor = Color.red; // ���� ȿ�� ����
    public GameObject warningBorderPrefab; // �׵θ� ������

    [Header("Movement Animation")]
    public float blockMoveAnimationDuration = 0.1f; // ��� �̵� �ִϸ��̼� �ð�

    [Header("Pause System")]
    public GameObject pausePanel; // �Ͻ����� �г�
    public Button pauseButton; // �Ͻ����� ��ư
    public Button resumeButton; // �簳 ��ư
    public Button pauseMenuButton; // �Ͻ����� �� �޴� ��ư
    public GameObject gridBlocker; // �׸��带 ������ ������Ʈ (��ο� ���)

    [Header("Effect System")]
    public CROxCROBlockEffectSystem blockEffectSystem;

    // ���� ����
    private bool isGameActive = false;
    private float currentTimeLimit;
    private int currentScore = 0;
    private int currentCombo = 0;
    private float gameStartTime;
    private float elapsedTime;

    // Ÿ�̸� (���յ� �ϳ��� Ÿ�̸�)
    private float moveAndGenerateTimer = 0f;

    // �׸��� ����
    private GameObject[,] infiniteGrid;
    private List<Vector2Int> edgePositions = new List<Vector2Int>();

    // �Ͻ����� ����
    private bool isPaused = false;
    private float pausedTimeLimit; // �Ͻ����� �� �ð� ����
    private float pausedMoveTimer; // �Ͻ����� �� �̵� Ÿ�̸� ����

    // ���� ȿ�� ���� (WarningEffect ���� ����)
    private List<GameObject> warningBlocks = new List<GameObject>();
    private Dictionary<GameObject, GameObject> warningBorders = new Dictionary<GameObject, GameObject>();
    private Coroutine warningEffectCoroutine;

    private DifficultyLevel currentDifficulty;

    void Awake()
    {
        gridManager = FindFirstObjectByType<InfiniteGridManager>();
    }

    void Start()
    {
        gameStartTime = Time.time;
        InitializeInfiniteMode();
        SetupUI();
        SetupPauseSystem(); // �Ͻ����� �ý��� �ʱ�ȭ
        AudioManager.Instance.PlaySceneBGM("InfiniteModeScene");
    }

    void SetupPauseSystem()
    {
        // �Ͻ����� ���� UI ����
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (pauseMenuButton != null)
            pauseMenuButton.onClick.AddListener(ReturnToMenuFromPause);

        // �ʱ� ���� ����
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gridBlocker != null)
            gridBlocker.SetActive(false);

        Debug.Log("Pause system initialized");
    }

    void InitializeInfiniteMode()
    {
        // ���� ����
        if (settings == null)
        {
            Debug.LogError("InfiniteModeSettings not assigned!");
            return;
        }

        // GridManager ����
        if (gridManager != null)
        {
            gridManager.width = settings.gridWidth;
            gridManager.height = settings.gridHeight;

            // InfiniteGridManager�� �׸��� �ʱ�ȭ
            gridManager.InitializeInfiniteGrid();

            // �ݹ� ����
            gridManager.onEmptyBlockClicked = OnEmptyBlockClicked;
        }
        else
        {
            Debug.LogError("InfiniteGridManager not found!");
            return;
        }

        // ��ü infiniteGrid�� gridManager�� grid�� ����ȭ
        infiniteGrid = new GameObject[settings.gridWidth, settings.gridHeight];

        // gridManager�� �׸��带 infiniteGrid�� ����
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                infiniteGrid[x, y] = gridManager.GetBlockAt(x, y);
            }
        }

        // �����ڸ� ��ġ ���
        CalculateEdgePositions();

        // �ʱ� ����
        currentTimeLimit = settings.initialTimeLimit;
        currentScore = 0;
        currentCombo = 0;

        // ���� ����
        StartGame();
    }

    void CalculateEdgePositions()
    {
        edgePositions.Clear();

        // ���� ���̵� ���� ��������
        float gameTime = Time.time - gameStartTime;
        DifficultyLevel currentDifficulty = settings.GetCurrentDifficulty(gameTime);

        // ������ �𼭸� ĭ �� ����
        int excludeCornerSize = (currentDifficulty.cornerMode == CornerBlockMode.FourCorners) ? 2 : 1;

        // ��� �����ڸ� (�𼭸� ����)
        for (int x = excludeCornerSize; x < settings.gridWidth - excludeCornerSize; x++)
        {
            edgePositions.Add(new Vector2Int(x, settings.gridHeight - 1));
        }

        // �ϴ� �����ڸ� (�𼭸� ����)
        for (int x = excludeCornerSize; x < settings.gridWidth - excludeCornerSize; x++)
        {
            edgePositions.Add(new Vector2Int(x, 0));
        }

        // ���� �����ڸ� (�𼭸� ����)
        for (int y = excludeCornerSize; y < settings.gridHeight - excludeCornerSize; y++)
        {
            edgePositions.Add(new Vector2Int(0, y));
        }

        // ���� �����ڸ� (�𼭸� ����)
        for (int y = excludeCornerSize; y < settings.gridHeight - excludeCornerSize; y++)
        {
            edgePositions.Add(new Vector2Int(settings.gridWidth - 1, y));
        }

        Debug.Log($"Edge positions calculated: {edgePositions.Count} positions (Corner mode: {currentDifficulty.cornerMode})");
    }

    void StartGame()
    {
        isGameActive = true;

        // ù ��ϵ��� ��� ����
        float gameTime = Time.time - gameStartTime;
        DifficultyLevel currentDifficulty = settings.GetCurrentDifficulty(gameTime);
        GenerateNewBlocks(currentDifficulty);

        // ���� �̵�/������ ���� Ÿ�̸� ����
        moveAndGenerateTimer = currentDifficulty.moveInterval;

        // GridManager�� ���Ѹ�� �ݹ� ���
        if (gridManager != null)
        {
            gridManager.onEmptyBlockClicked = OnEmptyBlockClicked;
        }

        UpdateUI();

        Debug.Log("Infinite mode started!");
    }

    void Update()
    {
        // �Ͻ����� ���̸� ���� ���� �������� ����
        if (!isGameActive || isPaused) return;

        // ESC Ű�� �Ͻ����� ���
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
            return;
        }

        // �ð� ����
        currentTimeLimit -= Time.deltaTime;

        // �ð� ���� üũ
        if (currentTimeLimit <= 0)
        {
            GameOver("Time Over!");
            return;
        }

        // ���� ���̵� ���� ��������
        float gameTime = Time.time - gameStartTime;
        currentDifficulty = settings.GetCurrentDifficulty(gameTime);

        // ���յ� ��� �̵�/���� Ÿ�̸�
        moveAndGenerateTimer -= Time.deltaTime;

        if (moveAndGenerateTimer <= 0)
        {
            // ���� ȿ�� ���� (�̵� ����)
            ClearWarningEffects();

            // ��� �̵� �ִϸ��̼� ����
            StartCoroutine(AnimatedBlockMove(currentDifficulty));

            // ���̵��� ����� �� �����Ƿ� �ٽ� Ȯ��
            currentDifficulty = settings.GetCurrentDifficulty(Time.time - gameStartTime);
            moveAndGenerateTimer = currentDifficulty.moveInterval;

            // �����ڸ� ��ġ�� ���̵��� ���� ����
            CalculateEdgePositions();
        }

        UpdateUI();
        UpdateDebugUI(currentDifficulty);
    }

    void GenerateNewBlocks(DifficultyLevel difficulty)
    {
        // �𼭸� ���� ũ�� ���
        int excludeCornerSize = (difficulty.cornerMode == CornerBlockMode.FourCorners) ? 2 : 1;

        // 1. ��� ���� ó��
        GenerateBlocksOnLine(
            excludeCornerSize,  // ���� �𼭸� ����
            settings.gridWidth - excludeCornerSize - 1,  // ������ �𼭸� ���� (�ε����̹Ƿ� -1 �߰�)
            settings.gridHeight - 1,
            settings.gridHeight - 1,
            difficulty
        );

        // 2. �ϴ� ���� ó��
        GenerateBlocksOnLine(
            excludeCornerSize,  // ���� �𼭸� ����
            settings.gridWidth - excludeCornerSize - 1,  // ������ �𼭸� ���� (�ε����̹Ƿ� -1 �߰�)
            0,
            0,
            difficulty
        );

        // 3. ���� ���� ó�� (���ϴ� �𼭸� ����)
        GenerateBlocksOnLine(
            0,
            0,
            excludeCornerSize,  // �ϴ� �𼭸� ����
            settings.gridHeight - excludeCornerSize - 1,  // ��� �𼭸� ���� (�ε����̹Ƿ� -1 �߰�)
            difficulty
        );

        // 4. ������ ���� ó�� (���ϴ� �𼭸� ����)
        GenerateBlocksOnLine(
            settings.gridWidth - 1,
            settings.gridWidth - 1,
            excludeCornerSize,  // �ϴ� �𼭸� ����
            settings.gridHeight - excludeCornerSize - 1,  // ��� �𼭸� ���� (�ε����̹Ƿ� -1 �߰�)
            difficulty
        );
    }

    // ���ο� ���� �޼��� �߰�
    void GenerateBlocksOnLine(int startX, int endX, int startY, int endY, DifficultyLevel difficulty)
    {
        // �ش� ������ �� ��ġ�� ã��
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

        // �� ���ο��� ������ ��� ���� ���
        int minBlocks = Mathf.Max(1, Mathf.RoundToInt(availablePositions.Count * difficulty.minSpawnChance));
        int maxBlocks = Mathf.RoundToInt(availablePositions.Count * difficulty.maxSpawnChance);
        int blocksToSpawn = Random.Range(minBlocks, maxBlocks + 1);

        // ����� �α�
        string lineType = "";
        if (startY == settings.gridHeight - 1) lineType = "Top";
        else if (startY == 0 && endY == 0) lineType = "Bottom";
        else if (startX == 0) lineType = "Left";
        else lineType = "Right";

        Debug.Log($"{lineType} line: {availablePositions.Count} available positions, spawning {blocksToSpawn} blocks (min: {minBlocks}, max: {maxBlocks})");

        // ���� ��ġ ����
        List<Vector2Int> tempPositions = new List<Vector2Int>(availablePositions);

        for (int i = 0; i < blocksToSpawn && tempPositions.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, tempPositions.Count);
            Vector2Int selectedPos = tempPositions[randomIndex];
            tempPositions.RemoveAt(randomIndex);

            // ��� ����
            CreateRandomBlock(selectedPos.x, selectedPos.y);
        }
    }

    // �̵� �� ����� ��ġ�� �� ��� ����
    void CreateEmptyBlocksAtVacatedPositions(List<BlockMoveData> moves)
    {
        HashSet<Vector2Int> vacatedPositions = new HashSet<Vector2Int>();

        // ����� ��ġ�� ����
        foreach (BlockMoveData move in moves)
        {
            Vector2Int fromPos = new Vector2Int(move.fromX, move.fromY);
            Vector2Int toPos = new Vector2Int(move.toX, move.toY);

            // ���� ��ġ�� �������� �ƴ� ��쿡�� �� ��� ���� �ʿ�
            bool positionIsStillOccupied = moves.Any(m => m.toX == move.fromX && m.toY == move.fromY);

            if (!positionIsStillOccupied)
            {
                vacatedPositions.Add(fromPos);
            }
        }

        // ����� ��ġ�� �� ��� ����
        foreach (Vector2Int pos in vacatedPositions)
        {
            if (infiniteGrid[pos.x, pos.y] == null)
            {
                CreateEmptyBlockAt(pos.x, pos.y);
                Debug.Log($"Created empty block at vacated position ({pos.x}, {pos.y})");
            }
        }
    }

    void CreateRandomBlock(int x, int y)
    {
        // ���� �� ��� ����
        if (infiniteGrid[x, y] != null)
        {
            gridManager.blockFactory.DestroyBlock(infiniteGrid[x, y]);
        }

        // ���� ��� ����
        int randomBlockType = Random.Range(0, blockPrefabs.Length);
        GameObject blockPrefab = blockPrefabs[randomBlockType];

        // �̵� ���� ����
        Vector2Int moveDirection = Vector2Int.zero;
        if (y == settings.gridHeight - 1)
            moveDirection = Vector2Int.down;
        else if (y == 0)
            moveDirection = Vector2Int.up;
        else if (x == 0)
            moveDirection = Vector2Int.right;
        else if (x == settings.gridWidth - 1)
            moveDirection = Vector2Int.left;

        // InfiniteGridManager�� ���� ��� ����
        gridManager.CreateInfiniteBlock(blockPrefab, x, y, moveDirection);

        // ���� �׸��� ����ȭ
        infiniteGrid[x, y] = gridManager.GetBlockAt(x, y);

        Debug.Log($"Created {blockPrefab.tag} block at ({x}, {y}) with direction {moveDirection}");
    }

    // �ִϸ��̼��� ���Ե� ��� �̵� ������ (���� ȿ�� �߰�)
    System.Collections.IEnumerator AnimatedBlockMove(DifficultyLevel difficulty)
    {
        // 1�ܰ�: �̵��� ��ϵ��� fade out �ִϸ��̼�
        List<BlockMoveData> allMoves = CalculateBlockMoves();
        if (allMoves == null) // ���� ���� �߻�
        {
            yield break;
        }

        Debug.Log($"Starting position-based animation for {allMoves.Count} blocks");

        // 2�ܰ�: ��� ��ϵ��� ���ÿ� �������� �̵� (position �ִϸ��̼�)
        yield return StartCoroutine(MoveBlocksToPositions(allMoves));

        // 3�ܰ�: �׸��� �迭 ������Ʈ (��ϵ��� �̹� �ùٸ� ��ġ�� ����)
        UpdateGridArrayAfterMove(allMoves);

        // 4�ܰ�: �̵� �� �� ��ġ�� ���ο� �� ��� ����
        CreateEmptyBlocksAtVacatedPositions(allMoves);

        // 5�ܰ�: �� ��� ����
        GenerateNewBlocks(difficulty);

        // 6�ܰ�: ���� ȿ�� ǥ��
        PredictAndShowCollisions();

        Debug.Log("Position-based block movement completed");
    }

    // ��� ��ϵ��� ���ÿ� �������� �̵��ϴ� �ִϸ��̼�
    System.Collections.IEnumerator MoveBlocksToPositions(List<BlockMoveData> moves)
    {
        if (moves.Count == 0) yield break;

        // �̵��� ��ϵ�� ����/��ǥ ��ġ ����
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

                    Debug.Log($"Block {block.tag} will move from ({move.fromX},{move.fromY}) to ({move.toX},{move.toY})");
                }
            }
            else
            {
                Debug.LogWarning($"No block found at ({move.fromX}, {move.fromY}) for movement!");
            }
        }

        Debug.Log($"Starting simultaneous movement animation for {moveInfos.Count} blocks");

        // ��� ��ϵ��� ���ÿ� �ִϸ��̼�
        float elapsed = 0f;
        while (elapsed < blockMoveAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / blockMoveAnimationDuration;

            // Ease-in-out ȿ���� ���� ������
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // ��� ��ϵ��� ��ġ�� ���ÿ� ������Ʈ
            foreach (BlockMoveInfo moveInfo in moveInfos)
            {
                if (moveInfo.block != null)
                {
                    Vector3 currentPos = Vector3.Lerp(moveInfo.startPosition, moveInfo.targetPosition, smoothProgress);
                    moveInfo.block.transform.position = currentPos;
                }
            }

            yield return null;
        }

        // ���� ��ġ�� ��Ȯ�� ���� (Block ������Ʈ�� UpdateGridArrayAfterMove���� ������Ʈ)
        foreach (BlockMoveInfo moveInfo in moveInfos)
        {
            if (moveInfo.block != null)
            {
                moveInfo.block.transform.position = moveInfo.targetPosition;
            }
        }

        Debug.Log("All blocks reached their target positions");
    }

    // �׸��� �迭 ������Ʈ (��ϵ��� �̹� �ùٸ� ��ġ�� ����)
    void UpdateGridArrayAfterMove(List<BlockMoveData> moves)
    {
        Debug.Log("=== Starting grid array update ===");

        GameObject[,] newGrid = new GameObject[settings.gridWidth, settings.gridHeight];

        // �̵����� �ʴ� ��ϵ��� �� �׸��忡 ����
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

        // �̵��� ��ϵ��� �� ��ġ�� ��ġ
        foreach (BlockMoveData move in moves)
        {
            GameObject movedBlock = infiniteGrid[move.fromX, move.fromY];
            if (movedBlock != null)
            {
                Block blockComponent = movedBlock.GetComponent<Block>();
                if (blockComponent != null && !blockComponent.isEmpty)
                {
                    newGrid[move.toX, move.toY] = movedBlock;
                    blockComponent.x = move.toX;
                    blockComponent.y = move.toY;

                    // gridManager���� ������Ʈ
                    gridManager.MoveBlock(movedBlock, move.fromX, move.fromY, move.toX, move.toY);
                }
            }
        }

        // ���� �׸��� ��ü
        infiniteGrid = newGrid;

        // gridManager �׸���� ����ȭ
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                gridManager.SetBlockAt(x, y, infiniteGrid[x, y]);
            }
        }

        Debug.Log("=== Grid array update completed ===");
    }

    // ������ �׸��� ���� ���
    void DebugPrintGridState()
    {
        Debug.Log("=== Current Grid State ===");
        for (int y = settings.gridHeight - 1; y >= 0; y--) // ������ �Ʒ���
        {
            string row = $"Row {y}: ";
            for (int x = 0; x < settings.gridWidth; x++)
            {
                GameObject block = infiniteGrid[x, y];
                if (block != null)
                {
                    Block blockComponent = block.GetComponent<Block>();
                    if (blockComponent != null)
                    {
                        if (blockComponent.isEmpty)
                            row += "[E] ";
                        else
                            row += $"[{block.tag.Substring(0, 1)}] ";
                    }
                    else
                    {
                        row += "[?] ";
                    }
                }
                else
                {
                    row += "[N] ";
                }
            }
            Debug.Log(row);
        }
        Debug.Log("=== End Grid State ===");
    }

    // ���� �� �浹 ���� �� ���� ȿ�� ����
    void PredictAndShowCollisions()
    {
        // ���� ���� ȿ�� ����
        ClearWarningEffects();

        // ���� �� �̵� �ùķ��̼� (���� ���� ������ �߻���Ű�� �ʴ� ����)
        List<BlockMoveData> nextMoves = SimulateNextMoves();
        if (nextMoves == null || nextMoves.Count == 0)
        {
            return;
        }

        Dictionary<Vector2Int, List<BlockMoveData>> collisionMap = new Dictionary<Vector2Int, List<BlockMoveData>>();

        // �� ���������� �̵��ϴ� ��ϵ� �׷�ȭ
        foreach (var move in nextMoves)
        {
            Vector2Int destination = new Vector2Int(move.toX, move.toY);

            if (!collisionMap.ContainsKey(destination))
            {
                collisionMap[destination] = new List<BlockMoveData>();
            }
            collisionMap[destination].Add(move);
        }

        // �浹 ���� ��ϵ� ã��
        foreach (var kvp in collisionMap)
        {
            Vector2Int destination = kvp.Key;
            List<BlockMoveData> movesToDestination = kvp.Value;

            // ���� �������� 2�� �̻� �̵��ϴ� ��� �浹
            bool willCollide = movesToDestination.Count > 1;

            // �������� �̵����� �ʴ� ����� �ִ��� Ȯ��
            if (!willCollide && !IsPositionEmpty(destination.x, destination.y))
            {
                GameObject destBlock = infiniteGrid[destination.x, destination.y];
                if (destBlock != null)
                {
                    Block destBlockComponent = destBlock.GetComponent<Block>();
                    if (destBlockComponent != null && !destBlockComponent.isEmpty)
                    {
                        // ������ ����� �̵����� �ʴ��� Ȯ��
                        bool destinationBlockWillMove = nextMoves.Any(move =>
                            move.fromX == destination.x && move.fromY == destination.y);

                        if (!destinationBlockWillMove)
                        {
                            willCollide = true; // �������� ���� ����� ����
                        }
                    }
                }
            }

            if (willCollide)
            {
                // �浹 ���� ��ϵ鿡 ���� ȿ�� ����
                foreach (var move in movesToDestination)
                {
                    GameObject block = infiniteGrid[move.fromX, move.fromY];
                    if (block != null)
                    {
                        AddWarningEffect(block);
                    }
                }

                // �������� ���� ����� �ִٸ� �װ͵� ���� ȿ�� ����
                if (!IsPositionEmpty(destination.x, destination.y))
                {
                    GameObject destBlock = infiniteGrid[destination.x, destination.y];
                    if (destBlock != null)
                    {
                        AddWarningEffect(destBlock);
                    }
                }
            }
        }

        // ���� ȿ�� ����
        if (warningBlocks.Count > 0)
        {
            Debug.Log($"Starting border warning effect for {warningBlocks.Count} blocks");
            Debug.Log($"Warning border prefab assigned: {warningBorderPrefab != null}");

            warningEffectCoroutine = StartCoroutine(WarningBlinkEffect());
        }
    }

    // ���� �̵� �ùķ��̼� (���� ���� �߻���Ű�� ����)
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

                        // �׸��� ���� üũ (�ùķ��̼ǿ����� ���� ���� �߻���Ű�� ����)
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

    // ��Ͽ� ���� ȿ�� �߰�
    void AddWarningEffect(GameObject block)
    {
        if (block == null || warningBlocks.Contains(block)) return;

        Debug.Log($"Adding border warning effect to {block.name}");

        warningBlocks.Add(block);

        // �׵θ� ������Ʈ ����
        GameObject warningBorder = CreateWarningBorder(block);
        if (warningBorder != null)
        {
            warningBorders[block] = warningBorder;
            Debug.Log($"Created warning border for {block.name}");
        }
    }

    // �׵θ� ������Ʈ ����
    GameObject CreateWarningBorder(GameObject targetBlock)
    {
        if (warningBorderPrefab == null)
        {
            Debug.LogError("Warning border prefab not assigned!");
            return null;
        }

        // ������ �ν��Ͻ� ����
        GameObject border = Instantiate(warningBorderPrefab);

        // Ÿ�� ����� �ڽ����� ����
        border.transform.SetParent(targetBlock.transform);
        border.transform.localPosition = Vector3.zero;
        border.transform.localRotation = Quaternion.identity;
        border.transform.localScale = Vector3.one; // ������ ���� ���ʿ� (�̹��� ��ü�� �׵θ�)

        // �׵θ� �̸� ����
        border.name = "WarningBorder";

        // SpriteRenderer ����
        SpriteRenderer borderRenderer = border.GetComponent<SpriteRenderer>();
        SpriteRenderer blockRenderer = targetBlock.GetComponent<SpriteRenderer>();

        if (borderRenderer != null && blockRenderer != null)
        {
            // ��Ϻ��� �տ� �׸���
            borderRenderer.sortingOrder = blockRenderer.sortingOrder + 1;
            // ������ White�� ���� (�̹��� ��ü�� ������)
            borderRenderer.color = Color.white;

            Debug.Log($"Border renderer setup complete. SortingOrder: {borderRenderer.sortingOrder}");
        }

        return border;
    }

    // ������ �׵θ� ���� (�������� ���� ��)
    GameObject CreateSimpleWarningBorder(GameObject targetBlock)
    {
        // �׵θ��� �� ������Ʈ ����
        GameObject border = new GameObject("WarningBorder");
        border.transform.SetParent(targetBlock.transform);
        border.transform.localPosition = Vector3.zero;

        // SpriteRenderer �߰�
        SpriteRenderer borderRenderer = border.AddComponent<SpriteRenderer>();

        // ���� ��ϰ� ���� ��������Ʈ ����ϵ� ���� �ٸ���
        SpriteRenderer blockRenderer = targetBlock.GetComponent<SpriteRenderer>();
        if (blockRenderer != null)
        {
            borderRenderer.sprite = blockRenderer.sprite;
            borderRenderer.color = warningColor;
            borderRenderer.sortingOrder = blockRenderer.sortingOrder + 1; // ��� ���� �׸���
        }

        // �׵θ��� ���̵��� ����ũ ȿ���� ���� �߰� ����
        WarningBorderEffect borderEffect = border.AddComponent<WarningBorderEffect>();
        borderEffect.originalBlockRenderer = blockRenderer;

        return border;
    }

    // ���� ȿ�� ����
    void ClearWarningEffects()
    {
        Debug.Log($"Clearing {warningBlocks.Count} warning effects");

        // ���� WarningEffect ������Ʈ ��� ���� (���� ����)
        foreach (GameObject block in warningBlocks)
        {
            if (block != null)
            {
                // �׵θ� ����
                if (warningBorders.ContainsKey(block))
                {
                    GameObject border = warningBorders[block];
                    if (border != null)
                    {
                        Destroy(border);
                    }
                }
            }
        }

        warningBlocks.Clear();
        warningBorders.Clear();

        if (warningEffectCoroutine != null)
        {
            StopCoroutine(warningEffectCoroutine);
            warningEffectCoroutine = null;
        }

        Debug.Log("Warning effects cleared");
    }

    // ���� ȿ�� ������ �ڷ�ƾ
    System.Collections.IEnumerator WarningBlinkEffect()
    {
        Debug.Log("Warning blink effect started with dedicated border sprite");
        float timer = 0f;

        while (warningBlocks.Count > 0 && isGameActive)
        {
            timer += Time.deltaTime;
            float blinkValue = (Mathf.Sin(timer * warningBlinkSpeed * Mathf.PI) + 1f) * 0.5f;

            // ��ȿ�� ��ϵ鸸 ���͸�
            List<GameObject> validBlocks = new List<GameObject>();
            foreach (GameObject block in warningBlocks)
            {
                if (block != null && warningBorders.ContainsKey(block) && warningBorders[block] != null)
                {
                    validBlocks.Add(block);
                }
            }
            warningBlocks = validBlocks;

            // �׵θ��� ������ ����
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
                            // ��ü �׵θ� ������Ʈ�� ������ ����
                            Color borderColor = Color.white;
                            borderColor.a = blinkValue;
                            borderRenderer.color = borderColor;
                        }
                    }
                }
            }

            yield return null;
        }

        Debug.Log("Warning blink effect ended");
    }

    // ��� �̵� ��ȹ ��� (���� MoveAllBlocks���� �и�)
    List<BlockMoveData> CalculateBlockMoves()
    {
        Debug.Log("=== Calculating block moves ===");
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

                        // �׸��� ���� üũ
                        if (IsValidPosition(newPos.x, newPos.y))
                        {
                            allMoves.Add(new BlockMoveData(x, y, newPos.x, newPos.y));
                            Debug.Log($"Added move: ({x},{y}) -> ({newPos.x},{newPos.y})");
                        }
                        else
                        {
                            // �׸��� ������ ����� �̵� - ���� ����
                            Debug.Log($"Block at ({x},{y}) trying to move out of bounds to ({newPos.x},{newPos.y})");
                            GameOver("�̵� ���� ����!");
                            return null;
                        }
                    }
                }
            }
        }

        Debug.Log($"Total moves calculated: {allMoves.Count}");

        // �浹 �˻� (���� ������ ����)
        if (!ValidateBlockMoves(allMoves))
        {
            return null; // ���� ����
        }

        return allMoves;
    }

    // �浹 �˻� ���� (���� MoveAllBlocks���� �и�)
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
                GameOver("Block Colide!");
                return false;
            }

            if (!IsPositionEmpty(destination.x, destination.y))
            {
                bool destinationBlockWillMove = allMoves.Exists(move =>
                    move.fromX == destination.x && move.fromY == destination.y);

                if (!destinationBlockWillMove)
                {
                    GameOver("Block Colide!");
                    return false;
                }
            }
        }

        return true;
    }

    // BlockMoveInfo Ŭ���� ������Ʈ
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
        if (block == null)
        {
            Debug.Log($"No block at ({x}, {y}) for move direction check");
            return Vector2Int.zero;
        }

        Block blockComponent = block.GetComponent<Block>();
        if (blockComponent != null && blockComponent.isEmpty)
        {
            Debug.Log($"Empty block at ({x}, {y}) - no movement");
            return Vector2Int.zero;
        }

        // InfiniteBlock ������Ʈ���� ������ ���� ��������
        InfiniteBlock infiniteBlockComponent = block.GetComponent<InfiniteBlock>();
        if (infiniteBlockComponent != null)
        {
            Vector2Int direction = infiniteBlockComponent.moveDirection;
            Debug.Log($"Block at ({x}, {y}) has direction {direction}");
            return direction;
        }

        // InfiniteBlock ������Ʈ�� ������ (���� ���) �̵����� ����
        Debug.Log($"Block at ({x}, {y}) has no InfiniteBlock component - no movement");
        return Vector2Int.zero;
    }

    void CreateEmptyBlockAt(int x, int y)
    {
        // ���� ����� �ִٸ� ����
        if (infiniteGrid[x, y] != null)
        {
            gridManager.blockFactory.DestroyBlock(infiniteGrid[x, y]);
            infiniteGrid[x, y] = null;
        }

        // gridManager�� ���� �� ��� ����
        GameObject emptyBlock = gridManager.blockFactory.CreateEmptyBlock(x, y);
        gridManager.SetBlockAt(x, y, emptyBlock);

        // BlockInteraction ����
        //BlockInteraction interaction = emptyBlock.GetComponent<BlockInteraction>();
        //if (interaction != null)
        //{
        //    interaction.SetGridManager(gridManager);
        //}

        // ���� �׸��� ����ȭ
        infiniteGrid[x, y] = emptyBlock;

        Debug.Log($"Empty block created at ({x}, {y})");
    }

    // GridManager �ݹ�
    void OnEmptyBlockClicked(int x, int y)
    {
        if (!isGameActive || isPaused) return; // �Ͻ����� �߿��� Ŭ�� ����

        List<GameObject> matchedBlocks = FindMatchingBlocks(x, y);

        if (matchedBlocks.Count >= 2)
        {
            // ��� �ı� ����
            DestroyBlocks(matchedBlocks);
            AddScore(matchedBlocks.Count);
            AddTime(matchedBlocks.Count);
            currentCombo++;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUI("BlockDestroy");
            }

            Debug.Log($"Destroyed {matchedBlocks.Count} blocks, Combo: {currentCombo}");

            // ����� �ı��Ǿ����Ƿ� ���� ��Ȳ�� ����� �� ���� - ���� ȿ�� ����
            PredictAndShowCollisions();
        }
        else
        {
            // �ı��� ��� ���� - �ð� ���Ƽ
            currentTimeLimit -= settings.timePenalty;
            currentCombo = 0; // �޺� ����

            Debug.Log($"No blocks to destroy, time penalty: {settings.timePenalty}");
        }
    }

    List<GameObject> FindMatchingBlocks(int x, int y)
    {
        List<GameObject> allMatchedBlocks = new List<GameObject>();
        Dictionary<string, List<GameObject>> blocksByType = new Dictionary<string, List<GameObject>>();

        // �����¿� ���� ����
        Vector2Int[] directions =
        {
            new Vector2Int(0, 1),  // ��
            new Vector2Int(0, -1), // ��
            new Vector2Int(-1, 0), // ��
            new Vector2Int(1, 0)   // ��
        };

        // �� �������� �˻� (���Ѹ�� ���� ���� ���)
        foreach (Vector2Int dir in directions)
        {
            GameObject foundBlock = FindFirstBlockInDirection(x, y, dir.x, dir.y);
            if (foundBlock != null)
            {
                Block blockComponent = foundBlock.GetComponent<Block>();
                if (blockComponent != null && !blockComponent.isEmpty)
                {
                    string blockType = foundBlock.tag;

                    // ��� Ÿ�Ժ��� �׷�ȭ
                    if (!blocksByType.ContainsKey(blockType))
                    {
                        blocksByType[blockType] = new List<GameObject>();
                    }
                    blocksByType[blockType].Add(foundBlock);

                    Debug.Log($"Found block of type {blockType} at direction ({dir.x}, {dir.y})");
                }
            }
        }

        // ��ġ�Ǵ� ��� Ÿ���� ����� ���� (2�� �̻��� Ÿ�Ը�)
        foreach (var entry in blocksByType)
        {
            string blockType = entry.Key;
            List<GameObject> blocks = entry.Value;

            Debug.Log($"Block type {blockType} has {blocks.Count} matches");

            // �� Ÿ���� ����� 2�� �̻��̸� ��ġ�� �߰�
            if (blocks.Count >= 2)
            {
                allMatchedBlocks.AddRange(blocks);
                Debug.Log($"Added {blocks.Count} blocks of type {blockType} to matches");
            }
        }

        Debug.Log($"Total matched blocks: {allMatchedBlocks.Count}");
        return allMatchedBlocks;
    }

    GameObject FindFirstBlockInDirection(int startX, int startY, int dirX, int dirY)
    {
        int currX = startX;
        int currY = startY;

        Debug.Log($"Searching in direction ({dirX}, {dirY}) from ({startX}, {startY})");

        while (true)
        {
            currX += dirX;
            currY += dirY;

            // �׸��� ������ ����� ����
            if (currX < 0 || currX >= settings.gridWidth || currY < 0 || currY >= settings.gridHeight)
            {
                Debug.Log($"Search in direction ({dirX}, {dirY}) reached grid boundary at ({currX}, {currY})");
                return null;
            }

            // �ش� ��ġ�� ��� Ȯ��
            GameObject block = infiniteGrid[currX, currY];

            if (block != null)
            {
                Block blockComponent = block.GetComponent<Block>();
                if (blockComponent != null)
                {
                    Debug.Log($"Found block at ({currX}, {currY}): isEmpty={blockComponent.isEmpty}, tag={block.tag}");

                    if (!blockComponent.isEmpty)
                    {
                        Debug.Log($"Found non-empty block at ({currX}, {currY}) with tag {block.tag}");
                        return block;
                    }
                    // �� ����̸� ��� �˻�
                }
                else
                {
                    Debug.LogWarning($"Block at ({currX}, {currY}) has no Block component!");
                }
            }
            else
            {
                Debug.LogWarning($"No block found at ({currX}, {currY}) in infiniteGrid");
            }
        }
    }

    void DestroyBlocks(List<GameObject> blocks)
    {
        Debug.Log($"DestroyBlocks called with {blocks.Count} blocks");

        foreach (GameObject block in blocks)
        {
            if (block == null)
            {
                Debug.LogWarning("Null block in destroy list!");
                continue;
            }

            Block blockComponent = block.GetComponent<Block>();
            if (blockComponent != null)
            {
                int x = blockComponent.x;
                int y = blockComponent.y;

                Debug.Log($"Destroying block at ({x}, {y}) with tag {block.tag}");

                if (blockEffectSystem != null)
                {
                    int blockType = (block.tag == "RedBlock") ? 1 : (block.tag == "BlueBlock") ? 2 : (block.tag == "YellowBlock") ? 3 : (block.tag == "GreenBlock") ? 4 : (block.tag == "PurpleBlock") ? 5 : 1;
                    blockEffectSystem.CreateBlockDestroyEffect(x, y, blockType);
                }

                // infiniteGrid �迭���� ���� ����
                if (infiniteGrid[x, y] == block)
                {
                    infiniteGrid[x, y] = null;
                }
                else
                {
                    Debug.LogWarning($"Grid mismatch at ({x}, {y})! Expected {block.name}, found {(infiniteGrid[x, y] ? infiniteGrid[x, y].name : "null")}");
                }

                // ��� ������Ʈ �ı�
                Destroy(block);
            }
            else
            {
                Debug.LogWarning("Block has no Block component!");
            }
        }

        // �ı��� ��ġ�� �� ��ϵ� ����
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

        Debug.Log($"Block destruction completed");
    }

    // �Ͻ�����
    public void PauseGame()
    {
        if (!isGameActive || isPaused) return;

        isPaused = true;

        // ���� ���� ����
        pausedTimeLimit = currentTimeLimit;
        pausedMoveTimer = moveAndGenerateTimer;

        // Time.timeScale�� 0���� �����Ͽ� ��� �ִϸ��̼ǰ� Ÿ�̸� ����
        Time.timeScale = 0f;

        // ���� ȿ�� �Ͻ� ����
        if (warningEffectCoroutine != null)
        {
            StopCoroutine(warningEffectCoroutine);
        }

        // UI ������Ʈ
        ShowPauseUI();

        // �׸��� �����
        HideGrid();

        Debug.Log("Game paused");
    }

    // ���� �簳
    public void ResumeGame()
    {
        if (!isGameActive || !isPaused) return;

        isPaused = false;

        // Time.timeScale ����
        Time.timeScale = 1f;

        // ����� ���� ����
        currentTimeLimit = pausedTimeLimit;
        moveAndGenerateTimer = pausedMoveTimer;

        // ���� ȿ�� ����� (�ʿ��� ���)
        if (warningBlocks.Count > 0)
        {
            warningEffectCoroutine = StartCoroutine(WarningBlinkEffect());
        }

        // UI ������Ʈ
        HidePauseUI();

        // �׸��� �ٽ� ���̱�
        ShowGrid();

        Debug.Log("Game resumed");
    }

    // �Ͻ����� �� �޴��� ���ư���
    public void ReturnToMenuFromPause()
    {
        // Time.timeScale ����
        Time.timeScale = 1f;

        AudioManager.Instance.StopBGM();

        // �޴��� �̵�
        SceneManager.LoadScene("LobbyScene");
    }

    // �Ͻ����� UI ǥ��
    void ShowPauseUI()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // �Ͻ����� ��ư �����
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    // �Ͻ����� UI �����
    void HidePauseUI()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // �Ͻ����� ��ư �ٽ� ���̱�
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }
    }

    // �׸��� �����
    void HideGrid()
    {
        // �׸��� ���Ŀ Ȱ��ȭ
        if (gridBlocker != null)
        {
            gridBlocker.SetActive(true);
        }

        // ��� �׸��� ������Ʈ �����
        //if (gridManager != null && gridManager.gridParent != null)
        //{
        //    gridManager.gridParent.SetActive(false);
        //}

        // ���: ���� ��ϵ� �����
        
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                if (infiniteGrid[x, y] != null)
                {
                    infiniteGrid[x, y].SetActive(false);
                }
            }
        }
    }

    // �׸��� �ٽ� ���̱�
    void ShowGrid()
    {
        // �׸��� ���Ŀ ��Ȱ��ȭ
        if (gridBlocker != null)
        {
            gridBlocker.SetActive(false);
        }

        // ��� �׸��� ������Ʈ �ٽ� ���̱�
        //if (gridManager != null && gridManager.gridParent != null)
        //{
        //    gridManager.gridParent.SetActive(true);
        //}

        // ���: ���� ��ϵ� �ٽ� ���̱�
        
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                if (infiniteGrid[x, y] != null)
                {
                    infiniteGrid[x, y].SetActive(true);
                }
            }
        }
    }

    void AddScore(int blockCount)
    {
        int baseScore = settings.GetScoreForBlockCount(blockCount);
        int comboBonus = settings.GetComboBonusScore(currentCombo);

        // ���� ���̵��� ���ʽ� ��� ����
        float gameTime = Time.time - gameStartTime;
        DifficultyLevel currentDifficulty = settings.GetCurrentDifficulty(gameTime);
        int difficultyBonus = (int)((float)baseScore * (currentDifficulty.bonusScoreMultiplier - 1.0f));

        currentScore += baseScore + comboBonus + difficultyBonus;
    }

    void AddTime(int blockCount)
    {
        float timeBonus = settings.GetTimeBonusForBlockCount(blockCount);
        currentTimeLimit += timeBonus;

        Debug.Log($"Time bonus: +{timeBonus} seconds");
    }

    void GameOver(string reason)
    {
        isGameActive = false;

        // Time.timeScale ����
        Time.timeScale = 1f;

        // ���� ȿ�� ����
        ClearWarningEffects();

        elapsedTime = Time.time - gameStartTime;

        Debug.Log($"Game Over: {reason}, Final Score: {currentScore}");

        // UI ������Ʈ
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {currentScore}";
        }

        if (elapsedTimeText != null)
        {
            elapsedTimeText.text = $"Elapsed Time: {elapsedTime}s";
        }

        SaveHighScore();
    }

    void SetupUI()
    {
        if (highScoreText != null)
        {
            int highScore = PlayerPrefs.GetInt("InfiniteMode_HighScore", 0);
            highScoreText.text = $"High Score: {highScore}";
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (menuButton != null)
            menuButton.onClick.AddListener(ReturnToMenu);
    }

    void UpdateUI()
    {
        if (timeText != null)
        {
            if (isPaused)
                timeText.text = "PAUSED";
            else
                timeText.text = $"{Mathf.Max(0, currentTimeLimit):F1}s";
        }

        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";

        if (comboText != null)
        {
            if (currentCombo > 1)
                comboText.text = $"Combo: {currentCombo}";
            else
                comboText.text = "";
        }
    }

    void UpdateDebugUI(DifficultyLevel difficulty)
    {
        if (difficultyText != null)
            difficultyText.text = $"Difficulty: {difficulty.difficultyName}";
    }

    void SaveHighScore()
    {
        int currentHigh = PlayerPrefs.GetInt("InfiniteMode_HighScore", 0);
        if (currentScore > currentHigh)
        {
            Debug.Log($"Old High Score: {currentHigh}, New High Score: {currentScore}");

            PlayerPrefs.SetInt("InfiniteMode_HighScore", currentScore);
            // �� ��� �޼� UI ǥ��
            newHighScoreText.SetActive(true);
            highScoreText.text = $"High Score: {currentScore}";
        }
    }

    // ��ƿ��Ƽ �޼���
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
        Time.timeScale = 1f; // Time.timeScale ����
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // Time.timeScale ����
        AudioManager.Instance.StopBGM();
        SceneManager.LoadScene("LobbyScene");
    }
}

// ��� �̵� ������ Ŭ����
[System.Serializable]
public class BlockMoveData
{
    public int fromX, fromY;
    public int toX, toY;

    public BlockMoveData(int fromX, int fromY, int toX, int toY)
    {
        this.fromX = fromX;
        this.fromY = fromY;
        this.toX = toX;
        this.toY = toY;
    }
}
