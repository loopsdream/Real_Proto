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
    public float warningBlinkSpeed = 3f; // 깜빡임 속도
    public Color warningColor = Color.red; // 위험 효과 색상
    public GameObject warningBorderPrefab; // 테두리 프리팹

    [Header("Movement Animation")]
    public float blockMoveAnimationDuration = 0.1f; // 블록 이동 애니메이션 시간

    [Header("Pause System")]
    public GameObject pausePanel; // 일시정지 패널
    public Button pauseButton; // 일시정지 버튼
    public Button resumeButton; // 재개 버튼
    public Button pauseMenuButton; // 일시정지 중 메뉴 버튼
    public GameObject gridBlocker; // 그리드를 가리는 오브젝트 (어두운 배경)

    [Header("Effect System")]
    public CROxCROBlockEffectSystem blockEffectSystem;

    // 게임 상태
    private bool isGameActive = false;
    private float currentTimeLimit;
    private int currentScore = 0;
    private int currentCombo = 0;
    private float gameStartTime;
    private float elapsedTime;

    // 타이머 (통합된 하나의 타이머)
    private float moveAndGenerateTimer = 0f;

    // 그리드 상태
    private GameObject[,] infiniteGrid;
    private List<Vector2Int> edgePositions = new List<Vector2Int>();

    // 일시정지 상태
    private bool isPaused = false;
    private float pausedTimeLimit; // 일시정지 전 시간 저장
    private float pausedMoveTimer; // 일시정지 전 이동 타이머 저장

    // 위험 효과 관련 (WarningEffect 완전 제거)
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
        SetupPauseSystem(); // 일시정지 시스템 초기화
        AudioManager.Instance.PlaySceneBGM("InfiniteModeScene");
    }

    void SetupPauseSystem()
    {
        // 일시정지 관련 UI 설정
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (pauseMenuButton != null)
            pauseMenuButton.onClick.AddListener(ReturnToMenuFromPause);

        // 초기 상태 설정
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gridBlocker != null)
            gridBlocker.SetActive(false);

        Debug.Log("Pause system initialized");
    }

    void InitializeInfiniteMode()
    {
        // 설정 적용
        if (settings == null)
        {
            Debug.LogError("InfiniteModeSettings not assigned!");
            return;
        }

        // GridManager 설정
        if (gridManager != null)
        {
            gridManager.width = settings.gridWidth;
            gridManager.height = settings.gridHeight;

            // InfiniteGridManager의 그리드 초기화
            gridManager.InitializeInfiniteGrid();

            // 콜백 설정
            gridManager.onEmptyBlockClicked = OnEmptyBlockClicked;
        }
        else
        {
            Debug.LogError("InfiniteGridManager not found!");
            return;
        }

        // 자체 infiniteGrid를 gridManager의 grid와 동기화
        infiniteGrid = new GameObject[settings.gridWidth, settings.gridHeight];

        // gridManager의 그리드를 infiniteGrid에 복사
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                infiniteGrid[x, y] = gridManager.GetBlockAt(x, y);
            }
        }

        // 가장자리 위치 계산
        CalculateEdgePositions();

        // 초기 설정
        currentTimeLimit = settings.initialTimeLimit;
        currentScore = 0;
        currentCombo = 0;

        // 게임 시작
        StartGame();
    }

    void CalculateEdgePositions()
    {
        edgePositions.Clear();

        // 현재 난이도 설정 가져오기
        float gameTime = Time.time - gameStartTime;
        DifficultyLevel currentDifficulty = settings.GetCurrentDifficulty(gameTime);

        // 제외할 모서리 칸 수 결정
        int excludeCornerSize = (currentDifficulty.cornerMode == CornerBlockMode.FourCorners) ? 2 : 1;

        // 상단 가장자리 (모서리 제외)
        for (int x = excludeCornerSize; x < settings.gridWidth - excludeCornerSize; x++)
        {
            edgePositions.Add(new Vector2Int(x, settings.gridHeight - 1));
        }

        // 하단 가장자리 (모서리 제외)
        for (int x = excludeCornerSize; x < settings.gridWidth - excludeCornerSize; x++)
        {
            edgePositions.Add(new Vector2Int(x, 0));
        }

        // 좌측 가장자리 (모서리 제외)
        for (int y = excludeCornerSize; y < settings.gridHeight - excludeCornerSize; y++)
        {
            edgePositions.Add(new Vector2Int(0, y));
        }

        // 우측 가장자리 (모서리 제외)
        for (int y = excludeCornerSize; y < settings.gridHeight - excludeCornerSize; y++)
        {
            edgePositions.Add(new Vector2Int(settings.gridWidth - 1, y));
        }

        Debug.Log($"Edge positions calculated: {edgePositions.Count} positions (Corner mode: {currentDifficulty.cornerMode})");
    }

    void StartGame()
    {
        isGameActive = true;

        // 첫 블록들을 즉시 생성
        float gameTime = Time.time - gameStartTime;
        DifficultyLevel currentDifficulty = settings.GetCurrentDifficulty(gameTime);
        GenerateNewBlocks(currentDifficulty);

        // 다음 이동/생성을 위한 타이머 설정
        moveAndGenerateTimer = currentDifficulty.moveInterval;

        // GridManager에 무한모드 콜백 등록
        if (gridManager != null)
        {
            gridManager.onEmptyBlockClicked = OnEmptyBlockClicked;
        }

        UpdateUI();

        Debug.Log("Infinite mode started!");
    }

    void Update()
    {
        // 일시정지 중이면 게임 로직 실행하지 않음
        if (!isGameActive || isPaused) return;

        // ESC 키로 일시정지 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
            return;
        }

        // 시간 감소
        currentTimeLimit -= Time.deltaTime;

        // 시간 종료 체크
        if (currentTimeLimit <= 0)
        {
            GameOver("Time Over!");
            return;
        }

        // 현재 난이도 설정 가져오기
        float gameTime = Time.time - gameStartTime;
        currentDifficulty = settings.GetCurrentDifficulty(gameTime);

        // 통합된 블록 이동/생성 타이머
        moveAndGenerateTimer -= Time.deltaTime;

        if (moveAndGenerateTimer <= 0)
        {
            // 위험 효과 제거 (이동 직전)
            ClearWarningEffects();

            // 블록 이동 애니메이션 시작
            StartCoroutine(AnimatedBlockMove(currentDifficulty));

            // 난이도가 변경될 수 있으므로 다시 확인
            currentDifficulty = settings.GetCurrentDifficulty(Time.time - gameStartTime);
            moveAndGenerateTimer = currentDifficulty.moveInterval;

            // 가장자리 위치도 난이도에 따라 재계산
            CalculateEdgePositions();
        }

        UpdateUI();
        UpdateDebugUI(currentDifficulty);
    }

    void GenerateNewBlocks(DifficultyLevel difficulty)
    {
        // 모서리 제외 크기 계산
        int excludeCornerSize = (difficulty.cornerMode == CornerBlockMode.FourCorners) ? 2 : 1;

        // 1. 상단 라인 처리
        GenerateBlocksOnLine(
            excludeCornerSize,  // 왼쪽 모서리 제외
            settings.gridWidth - excludeCornerSize - 1,  // 오른쪽 모서리 제외 (인덱스이므로 -1 추가)
            settings.gridHeight - 1,
            settings.gridHeight - 1,
            difficulty
        );

        // 2. 하단 라인 처리
        GenerateBlocksOnLine(
            excludeCornerSize,  // 왼쪽 모서리 제외
            settings.gridWidth - excludeCornerSize - 1,  // 오른쪽 모서리 제외 (인덱스이므로 -1 추가)
            0,
            0,
            difficulty
        );

        // 3. 왼쪽 라인 처리 (상하단 모서리 제외)
        GenerateBlocksOnLine(
            0,
            0,
            excludeCornerSize,  // 하단 모서리 제외
            settings.gridHeight - excludeCornerSize - 1,  // 상단 모서리 제외 (인덱스이므로 -1 추가)
            difficulty
        );

        // 4. 오른쪽 라인 처리 (상하단 모서리 제외)
        GenerateBlocksOnLine(
            settings.gridWidth - 1,
            settings.gridWidth - 1,
            excludeCornerSize,  // 하단 모서리 제외
            settings.gridHeight - excludeCornerSize - 1,  // 상단 모서리 제외 (인덱스이므로 -1 추가)
            difficulty
        );
    }

    // 새로운 헬퍼 메서드 추가
    void GenerateBlocksOnLine(int startX, int endX, int startY, int endY, DifficultyLevel difficulty)
    {
        // 해당 라인의 빈 위치들 찾기
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

        // 이 라인에서 생성할 블록 개수 계산
        int minBlocks = Mathf.Max(1, Mathf.RoundToInt(availablePositions.Count * difficulty.minSpawnChance));
        int maxBlocks = Mathf.RoundToInt(availablePositions.Count * difficulty.maxSpawnChance);
        int blocksToSpawn = Random.Range(minBlocks, maxBlocks + 1);

        // 디버그 로그
        string lineType = "";
        if (startY == settings.gridHeight - 1) lineType = "Top";
        else if (startY == 0 && endY == 0) lineType = "Bottom";
        else if (startX == 0) lineType = "Left";
        else lineType = "Right";

        Debug.Log($"{lineType} line: {availablePositions.Count} available positions, spawning {blocksToSpawn} blocks (min: {minBlocks}, max: {maxBlocks})");

        // 랜덤 위치 선택
        List<Vector2Int> tempPositions = new List<Vector2Int>(availablePositions);

        for (int i = 0; i < blocksToSpawn && tempPositions.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, tempPositions.Count);
            Vector2Int selectedPos = tempPositions[randomIndex];
            tempPositions.RemoveAt(randomIndex);

            // 블록 생성
            CreateRandomBlock(selectedPos.x, selectedPos.y);
        }
    }

    // 이동 후 비워진 위치에 빈 블록 생성
    void CreateEmptyBlocksAtVacatedPositions(List<BlockMoveData> moves)
    {
        HashSet<Vector2Int> vacatedPositions = new HashSet<Vector2Int>();

        // 비워진 위치들 수집
        foreach (BlockMoveData move in moves)
        {
            Vector2Int fromPos = new Vector2Int(move.fromX, move.fromY);
            Vector2Int toPos = new Vector2Int(move.toX, move.toY);

            // 원래 위치가 목적지가 아닌 경우에만 빈 블록 생성 필요
            bool positionIsStillOccupied = moves.Any(m => m.toX == move.fromX && m.toY == move.fromY);

            if (!positionIsStillOccupied)
            {
                vacatedPositions.Add(fromPos);
            }
        }

        // 비워진 위치에 빈 블록 생성
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
        // 기존 빈 블록 제거
        if (infiniteGrid[x, y] != null)
        {
            gridManager.blockFactory.DestroyBlock(infiniteGrid[x, y]);
        }

        // 랜덤 블록 생성
        int randomBlockType = Random.Range(0, blockPrefabs.Length);
        GameObject blockPrefab = blockPrefabs[randomBlockType];

        // 이동 방향 결정
        Vector2Int moveDirection = Vector2Int.zero;
        if (y == settings.gridHeight - 1)
            moveDirection = Vector2Int.down;
        else if (y == 0)
            moveDirection = Vector2Int.up;
        else if (x == 0)
            moveDirection = Vector2Int.right;
        else if (x == settings.gridWidth - 1)
            moveDirection = Vector2Int.left;

        // InfiniteGridManager를 통해 블록 생성
        gridManager.CreateInfiniteBlock(blockPrefab, x, y, moveDirection);

        // 로컬 그리드 동기화
        infiniteGrid[x, y] = gridManager.GetBlockAt(x, y);

        Debug.Log($"Created {blockPrefab.tag} block at ({x}, {y}) with direction {moveDirection}");
    }

    // 애니메이션이 포함된 블록 이동 시퀀스 (위험 효과 추가)
    System.Collections.IEnumerator AnimatedBlockMove(DifficultyLevel difficulty)
    {
        // 1단계: 이동할 블록들의 fade out 애니메이션
        List<BlockMoveData> allMoves = CalculateBlockMoves();
        if (allMoves == null) // 게임 오버 발생
        {
            yield break;
        }

        Debug.Log($"Starting position-based animation for {allMoves.Count} blocks");

        // 2단계: 모든 블록들을 동시에 목적지로 이동 (position 애니메이션)
        yield return StartCoroutine(MoveBlocksToPositions(allMoves));

        // 3단계: 그리드 배열 업데이트 (블록들은 이미 올바른 위치에 있음)
        UpdateGridArrayAfterMove(allMoves);

        // 4단계: 이동 후 빈 위치에 새로운 빈 블록 생성
        CreateEmptyBlocksAtVacatedPositions(allMoves);

        // 5단계: 새 블록 생성
        GenerateNewBlocks(difficulty);

        // 6단계: 위험 효과 표시
        PredictAndShowCollisions();

        Debug.Log("Position-based block movement completed");
    }

    // 모든 블록들을 동시에 목적지로 이동하는 애니메이션
    System.Collections.IEnumerator MoveBlocksToPositions(List<BlockMoveData> moves)
    {
        if (moves.Count == 0) yield break;

        // 이동할 블록들과 시작/목표 위치 수집
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

        // 모든 블록들을 동시에 애니메이션
        float elapsed = 0f;
        while (elapsed < blockMoveAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / blockMoveAnimationDuration;

            // Ease-in-out 효과를 위한 스무딩
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // 모든 블록들의 위치를 동시에 업데이트
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

        // 최종 위치로 정확히 맞춤 (Block 컴포넌트는 UpdateGridArrayAfterMove에서 업데이트)
        foreach (BlockMoveInfo moveInfo in moveInfos)
        {
            if (moveInfo.block != null)
            {
                moveInfo.block.transform.position = moveInfo.targetPosition;
            }
        }

        Debug.Log("All blocks reached their target positions");
    }

    // 그리드 배열 업데이트 (블록들이 이미 올바른 위치에 있음)
    void UpdateGridArrayAfterMove(List<BlockMoveData> moves)
    {
        Debug.Log("=== Starting grid array update ===");

        GameObject[,] newGrid = new GameObject[settings.gridWidth, settings.gridHeight];

        // 이동하지 않는 블록들을 새 그리드에 복사
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

        // 이동한 블록들을 새 위치에 배치
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

                    // gridManager에도 업데이트
                    gridManager.MoveBlock(movedBlock, move.fromX, move.fromY, move.toX, move.toY);
                }
            }
        }

        // 로컬 그리드 교체
        infiniteGrid = newGrid;

        // gridManager 그리드와 동기화
        for (int x = 0; x < settings.gridWidth; x++)
        {
            for (int y = 0; y < settings.gridHeight; y++)
            {
                gridManager.SetBlockAt(x, y, infiniteGrid[x, y]);
            }
        }

        Debug.Log("=== Grid array update completed ===");
    }

    // 디버깅용 그리드 상태 출력
    void DebugPrintGridState()
    {
        Debug.Log("=== Current Grid State ===");
        for (int y = settings.gridHeight - 1; y >= 0; y--) // 위에서 아래로
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

    // 다음 턴 충돌 예측 및 위험 효과 적용
    void PredictAndShowCollisions()
    {
        // 기존 위험 효과 제거
        ClearWarningEffects();

        // 다음 턴 이동 시뮬레이션 (실제 게임 오버를 발생시키지 않는 버전)
        List<BlockMoveData> nextMoves = SimulateNextMoves();
        if (nextMoves == null || nextMoves.Count == 0)
        {
            return;
        }

        Dictionary<Vector2Int, List<BlockMoveData>> collisionMap = new Dictionary<Vector2Int, List<BlockMoveData>>();

        // 각 목적지별로 이동하는 블록들 그룹화
        foreach (var move in nextMoves)
        {
            Vector2Int destination = new Vector2Int(move.toX, move.toY);

            if (!collisionMap.ContainsKey(destination))
            {
                collisionMap[destination] = new List<BlockMoveData>();
            }
            collisionMap[destination].Add(move);
        }

        // 충돌 예정 블록들 찾기
        foreach (var kvp in collisionMap)
        {
            Vector2Int destination = kvp.Key;
            List<BlockMoveData> movesToDestination = kvp.Value;

            // 같은 목적지로 2개 이상 이동하는 경우 충돌
            bool willCollide = movesToDestination.Count > 1;

            // 목적지에 이동하지 않는 블록이 있는지 확인
            if (!willCollide && !IsPositionEmpty(destination.x, destination.y))
            {
                GameObject destBlock = infiniteGrid[destination.x, destination.y];
                if (destBlock != null)
                {
                    Block destBlockComponent = destBlock.GetComponent<Block>();
                    if (destBlockComponent != null && !destBlockComponent.isEmpty)
                    {
                        // 목적지 블록이 이동하지 않는지 확인
                        bool destinationBlockWillMove = nextMoves.Any(move =>
                            move.fromX == destination.x && move.fromY == destination.y);

                        if (!destinationBlockWillMove)
                        {
                            willCollide = true; // 목적지에 정적 블록이 있음
                        }
                    }
                }
            }

            if (willCollide)
            {
                // 충돌 예정 블록들에 위험 효과 적용
                foreach (var move in movesToDestination)
                {
                    GameObject block = infiniteGrid[move.fromX, move.fromY];
                    if (block != null)
                    {
                        AddWarningEffect(block);
                    }
                }

                // 목적지에 정적 블록이 있다면 그것도 위험 효과 적용
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

        // 위험 효과 시작
        if (warningBlocks.Count > 0)
        {
            Debug.Log($"Starting border warning effect for {warningBlocks.Count} blocks");
            Debug.Log($"Warning border prefab assigned: {warningBorderPrefab != null}");

            warningEffectCoroutine = StartCoroutine(WarningBlinkEffect());
        }
    }

    // 다음 이동 시뮬레이션 (게임 오버 발생시키지 않음)
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

                        // 그리드 범위 체크 (시뮬레이션에서는 게임 오버 발생시키지 않음)
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

    // 블록에 위험 효과 추가
    void AddWarningEffect(GameObject block)
    {
        if (block == null || warningBlocks.Contains(block)) return;

        Debug.Log($"Adding border warning effect to {block.name}");

        warningBlocks.Add(block);

        // 테두리 오브젝트 생성
        GameObject warningBorder = CreateWarningBorder(block);
        if (warningBorder != null)
        {
            warningBorders[block] = warningBorder;
            Debug.Log($"Created warning border for {block.name}");
        }
    }

    // 테두리 오브젝트 생성
    GameObject CreateWarningBorder(GameObject targetBlock)
    {
        if (warningBorderPrefab == null)
        {
            Debug.LogError("Warning border prefab not assigned!");
            return null;
        }

        // 프리팹 인스턴스 생성
        GameObject border = Instantiate(warningBorderPrefab);

        // 타겟 블록의 자식으로 설정
        border.transform.SetParent(targetBlock.transform);
        border.transform.localPosition = Vector3.zero;
        border.transform.localRotation = Quaternion.identity;
        border.transform.localScale = Vector3.one; // 스케일 조정 불필요 (이미지 자체가 테두리)

        // 테두리 이름 설정
        border.name = "WarningBorder";

        // SpriteRenderer 설정
        SpriteRenderer borderRenderer = border.GetComponent<SpriteRenderer>();
        SpriteRenderer blockRenderer = targetBlock.GetComponent<SpriteRenderer>();

        if (borderRenderer != null && blockRenderer != null)
        {
            // 블록보다 앞에 그리기
            borderRenderer.sortingOrder = blockRenderer.sortingOrder + 1;
            // 색상은 White로 유지 (이미지 자체가 빨간색)
            borderRenderer.color = Color.white;

            Debug.Log($"Border renderer setup complete. SortingOrder: {borderRenderer.sortingOrder}");
        }

        return border;
    }

    // 간단한 테두리 생성 (프리팹이 없을 때)
    GameObject CreateSimpleWarningBorder(GameObject targetBlock)
    {
        // 테두리용 빈 오브젝트 생성
        GameObject border = new GameObject("WarningBorder");
        border.transform.SetParent(targetBlock.transform);
        border.transform.localPosition = Vector3.zero;

        // SpriteRenderer 추가
        SpriteRenderer borderRenderer = border.AddComponent<SpriteRenderer>();

        // 기존 블록과 같은 스프라이트 사용하되 색상만 다르게
        SpriteRenderer blockRenderer = targetBlock.GetComponent<SpriteRenderer>();
        if (blockRenderer != null)
        {
            borderRenderer.sprite = blockRenderer.sprite;
            borderRenderer.color = warningColor;
            borderRenderer.sortingOrder = blockRenderer.sortingOrder + 1; // 블록 위에 그리기
        }

        // 테두리만 보이도록 마스크 효과를 위한 추가 설정
        WarningBorderEffect borderEffect = border.AddComponent<WarningBorderEffect>();
        borderEffect.originalBlockRenderer = blockRenderer;

        return border;
    }

    // 위험 효과 제거
    void ClearWarningEffects()
    {
        Debug.Log($"Clearing {warningBlocks.Count} warning effects");

        // 기존 WarningEffect 컴포넌트 방식 정리 (완전 제거)
        foreach (GameObject block in warningBlocks)
        {
            if (block != null)
            {
                // 테두리 제거
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

    // 위험 효과 깜빡임 코루틴
    System.Collections.IEnumerator WarningBlinkEffect()
    {
        Debug.Log("Warning blink effect started with dedicated border sprite");
        float timer = 0f;

        while (warningBlocks.Count > 0 && isGameActive)
        {
            timer += Time.deltaTime;
            float blinkValue = (Mathf.Sin(timer * warningBlinkSpeed * Mathf.PI) + 1f) * 0.5f;

            // 유효한 블록들만 필터링
            List<GameObject> validBlocks = new List<GameObject>();
            foreach (GameObject block in warningBlocks)
            {
                if (block != null && warningBorders.ContainsKey(block) && warningBorders[block] != null)
                {
                    validBlocks.Add(block);
                }
            }
            warningBlocks = validBlocks;

            // 테두리의 투명도만 조절
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
                            // 전체 테두리 오브젝트의 투명도만 조절
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

    // 블록 이동 계획 계산 (기존 MoveAllBlocks에서 분리)
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

                        // 그리드 범위 체크
                        if (IsValidPosition(newPos.x, newPos.y))
                        {
                            allMoves.Add(new BlockMoveData(x, y, newPos.x, newPos.y));
                            Debug.Log($"Added move: ({x},{y}) -> ({newPos.x},{newPos.y})");
                        }
                        else
                        {
                            // 그리드 범위를 벗어나는 이동 - 게임 오버
                            Debug.Log($"Block at ({x},{y}) trying to move out of bounds to ({newPos.x},{newPos.y})");
                            GameOver("이동 공간 부족!");
                            return null;
                        }
                    }
                }
            }
        }

        Debug.Log($"Total moves calculated: {allMoves.Count}");

        // 충돌 검사 (기존 로직과 동일)
        if (!ValidateBlockMoves(allMoves))
        {
            return null; // 게임 오버
        }

        return allMoves;
    }

    // 충돌 검사 로직 (기존 MoveAllBlocks에서 분리)
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

    // BlockMoveInfo 클래스 업데이트
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

        // InfiniteBlock 컴포넌트에서 고정된 방향 가져오기
        InfiniteBlock infiniteBlockComponent = block.GetComponent<InfiniteBlock>();
        if (infiniteBlockComponent != null)
        {
            Vector2Int direction = infiniteBlockComponent.moveDirection;
            Debug.Log($"Block at ({x}, {y}) has direction {direction}");
            return direction;
        }

        // InfiniteBlock 컴포넌트가 없으면 (기존 블록) 이동하지 않음
        Debug.Log($"Block at ({x}, {y}) has no InfiniteBlock component - no movement");
        return Vector2Int.zero;
    }

    void CreateEmptyBlockAt(int x, int y)
    {
        // 기존 블록이 있다면 제거
        if (infiniteGrid[x, y] != null)
        {
            gridManager.blockFactory.DestroyBlock(infiniteGrid[x, y]);
            infiniteGrid[x, y] = null;
        }

        // gridManager를 통해 빈 블록 생성
        GameObject emptyBlock = gridManager.blockFactory.CreateEmptyBlock(x, y);
        gridManager.SetBlockAt(x, y, emptyBlock);

        // BlockInteraction 설정
        //BlockInteraction interaction = emptyBlock.GetComponent<BlockInteraction>();
        //if (interaction != null)
        //{
        //    interaction.SetGridManager(gridManager);
        //}

        // 로컬 그리드 동기화
        infiniteGrid[x, y] = emptyBlock;

        Debug.Log($"Empty block created at ({x}, {y})");
    }

    // GridManager 콜백
    void OnEmptyBlockClicked(int x, int y)
    {
        if (!isGameActive || isPaused) return; // 일시정지 중에는 클릭 무시

        List<GameObject> matchedBlocks = FindMatchingBlocks(x, y);

        if (matchedBlocks.Count >= 2)
        {
            // 블록 파괴 성공
            DestroyBlocks(matchedBlocks);
            AddScore(matchedBlocks.Count);
            AddTime(matchedBlocks.Count);
            currentCombo++;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUI("BlockDestroy");
            }

            Debug.Log($"Destroyed {matchedBlocks.Count} blocks, Combo: {currentCombo}");

            // 블록이 파괴되었으므로 위험 상황이 변경될 수 있음 - 위험 효과 재계산
            PredictAndShowCollisions();
        }
        else
        {
            // 파괴할 블록 없음 - 시간 페널티
            currentTimeLimit -= settings.timePenalty;
            currentCombo = 0; // 콤보 리셋

            Debug.Log($"No blocks to destroy, time penalty: {settings.timePenalty}");
        }
    }

    List<GameObject> FindMatchingBlocks(int x, int y)
    {
        List<GameObject> allMatchedBlocks = new List<GameObject>();
        Dictionary<string, List<GameObject>> blocksByType = new Dictionary<string, List<GameObject>>();

        // 상하좌우 방향 정의
        Vector2Int[] directions =
        {
            new Vector2Int(0, 1),  // 상
            new Vector2Int(0, -1), // 하
            new Vector2Int(-1, 0), // 좌
            new Vector2Int(1, 0)   // 우
        };

        // 각 방향으로 검색 (무한모드 전용 로직 사용)
        foreach (Vector2Int dir in directions)
        {
            GameObject foundBlock = FindFirstBlockInDirection(x, y, dir.x, dir.y);
            if (foundBlock != null)
            {
                Block blockComponent = foundBlock.GetComponent<Block>();
                if (blockComponent != null && !blockComponent.isEmpty)
                {
                    string blockType = foundBlock.tag;

                    // 블록 타입별로 그룹화
                    if (!blocksByType.ContainsKey(blockType))
                    {
                        blocksByType[blockType] = new List<GameObject>();
                    }
                    blocksByType[blockType].Add(foundBlock);

                    Debug.Log($"Found block of type {blockType} at direction ({dir.x}, {dir.y})");
                }
            }
        }

        // 매치되는 모든 타입의 블록을 수집 (2개 이상인 타입만)
        foreach (var entry in blocksByType)
        {
            string blockType = entry.Key;
            List<GameObject> blocks = entry.Value;

            Debug.Log($"Block type {blockType} has {blocks.Count} matches");

            // 이 타입의 블록이 2개 이상이면 매치에 추가
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

            // 그리드 범위를 벗어나면 종료
            if (currX < 0 || currX >= settings.gridWidth || currY < 0 || currY >= settings.gridHeight)
            {
                Debug.Log($"Search in direction ({dirX}, {dirY}) reached grid boundary at ({currX}, {currY})");
                return null;
            }

            // 해당 위치의 블록 확인
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
                    // 빈 블록이면 계속 검색
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

                // infiniteGrid 배열에서 참조 제거
                if (infiniteGrid[x, y] == block)
                {
                    infiniteGrid[x, y] = null;
                }
                else
                {
                    Debug.LogWarning($"Grid mismatch at ({x}, {y})! Expected {block.name}, found {(infiniteGrid[x, y] ? infiniteGrid[x, y].name : "null")}");
                }

                // 블록 오브젝트 파괴
                Destroy(block);
            }
            else
            {
                Debug.LogWarning("Block has no Block component!");
            }
        }

        // 파괴된 위치에 빈 블록들 생성
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

    // 일시정지
    public void PauseGame()
    {
        if (!isGameActive || isPaused) return;

        isPaused = true;

        // 현재 상태 저장
        pausedTimeLimit = currentTimeLimit;
        pausedMoveTimer = moveAndGenerateTimer;

        // Time.timeScale을 0으로 설정하여 모든 애니메이션과 타이머 정지
        Time.timeScale = 0f;

        // 위험 효과 일시 정지
        if (warningEffectCoroutine != null)
        {
            StopCoroutine(warningEffectCoroutine);
        }

        // UI 업데이트
        ShowPauseUI();

        // 그리드 숨기기
        HideGrid();

        Debug.Log("Game paused");
    }

    // 게임 재개
    public void ResumeGame()
    {
        if (!isGameActive || !isPaused) return;

        isPaused = false;

        // Time.timeScale 복원
        Time.timeScale = 1f;

        // 저장된 상태 복원
        currentTimeLimit = pausedTimeLimit;
        moveAndGenerateTimer = pausedMoveTimer;

        // 위험 효과 재시작 (필요한 경우)
        if (warningBlocks.Count > 0)
        {
            warningEffectCoroutine = StartCoroutine(WarningBlinkEffect());
        }

        // UI 업데이트
        HidePauseUI();

        // 그리드 다시 보이기
        ShowGrid();

        Debug.Log("Game resumed");
    }

    // 일시정지 중 메뉴로 돌아가기
    public void ReturnToMenuFromPause()
    {
        // Time.timeScale 복원
        Time.timeScale = 1f;

        AudioManager.Instance.StopBGM();

        // 메뉴로 이동
        SceneManager.LoadScene("LobbyScene");
    }

    // 일시정지 UI 표시
    void ShowPauseUI()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // 일시정지 버튼 숨기기
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    // 일시정지 UI 숨기기
    void HidePauseUI()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // 일시정지 버튼 다시 보이기
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }
    }

    // 그리드 숨기기
    void HideGrid()
    {
        // 그리드 블로커 활성화
        if (gridBlocker != null)
        {
            gridBlocker.SetActive(true);
        }

        // 모든 그리드 오브젝트 숨기기
        //if (gridManager != null && gridManager.gridParent != null)
        //{
        //    gridManager.gridParent.SetActive(false);
        //}

        // 대안: 개별 블록들 숨기기
        
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

    // 그리드 다시 보이기
    void ShowGrid()
    {
        // 그리드 블로커 비활성화
        if (gridBlocker != null)
        {
            gridBlocker.SetActive(false);
        }

        // 모든 그리드 오브젝트 다시 보이기
        //if (gridManager != null && gridManager.gridParent != null)
        //{
        //    gridManager.gridParent.SetActive(true);
        //}

        // 대안: 개별 블록들 다시 보이기
        
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

        // 현재 난이도의 보너스 배수 적용
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

        // Time.timeScale 복원
        Time.timeScale = 1f;

        // 위험 효과 제거
        ClearWarningEffects();

        elapsedTime = Time.time - gameStartTime;

        Debug.Log($"Game Over: {reason}, Final Score: {currentScore}");

        // UI 업데이트
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
            // 새 기록 달성 UI 표시
            newHighScoreText.SetActive(true);
            highScoreText.text = $"High Score: {currentScore}";
        }
    }

    // 유틸리티 메서드
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
        Time.timeScale = 1f; // Time.timeScale 복원
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // Time.timeScale 복원
        AudioManager.Instance.StopBGM();
        SceneManager.LoadScene("LobbyScene");
    }
}

// 블록 이동 데이터 클래스
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
