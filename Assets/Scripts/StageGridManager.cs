using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StageGridManager : BaseGridManager
{
    // 싱글톤 인스턴스
    public static StageGridManager Instance { get; private set; }

    [Header("Stage Settings")]
    public int scorePerBlock = 10;

    [Header("Stage UI")]
    public TextMeshProUGUI scoreText;
    public GameObject winPanel;

    [Header("Stage Systems")]
    public StageShuffleSystem shuffleSystem;

    [Header("Stage State")]
    public int currentScore = 0;
    private int shuffleAttemptCount = 0;
    private const int MAX_SHUFFLE_ATTEMPTS = 3;
    private bool isCheckingWinCondition = false;

    protected override void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[StageGridManager] Singleton instance set: {gameObject.name} (ID: {GetInstanceID()})");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[StageGridManager] Duplicate instance found! Destroying: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        base.Awake();

        if (shuffleSystem == null)
            shuffleSystem = GetComponent<StageShuffleSystem>();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("[StageGridManager] Singleton instance cleared");
            Instance = null;
        }
    }

    void Start()
    {
        // 스테이지 모드는 StageManager가 초기화를 담당
        UpdateScoreText();
    }

    public void InitializeStageGrid(StageData stageData)
    {
        Debug.Log($"[InitializeStageGrid] Starting with stage: {stageData.stageName}");
        Debug.Log($"[InitializeStageGrid] Grid size: {stageData.gridWidth}x{stageData.gridHeight}");

        if (stageData == null) return;

        width = stageData.gridWidth;
        height = stageData.gridHeight;

        ClearGrid();
        Debug.Log("[InitializeStageGrid] Grid cleared");

        SetupGrid();
        Debug.Log($"[InitializeStageGrid] SetupGrid() called - grid array: {(grid != null ? $"{grid.GetLength(0)}x{grid.GetLength(1)}" : "NULL")}");

        CreateBlocksFromPattern(stageData.blockPattern);
        Debug.Log("[InitializeStageGrid] Pattern initialized");

        int nullCount = 0;
        int blockCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                    nullCount++;
                else
                    blockCount++;
            }
        }
        Debug.Log($"[InitializeStageGrid] Grid status - Blocks: {blockCount}, NULL: {nullCount}");

        SetupCameraAndLayout();

        Debug.Log("[InitializeStageGrid] Completed");

        currentScore = 0;
        UpdateScoreText();
    }

    private void CreateBlocksFromPattern(int[] pattern)
    {
        if (pattern == null || blockFactory == null) return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index < pattern.Length)
                {
                    int blockType = pattern[index];
                    grid[x, y] = blockFactory.CreateBlockFromType(blockType, x, y);
                }
                else
                {
                    grid[x, y] = blockFactory.CreateEmptyBlock(x, y);
                }
            }
        }
    }

    public override void OnEmptyBlockClicked(int x, int y)
    {
        Debug.Log($"[StageGridManager] OnEmptyBlockClicked({x}, {y}) called");

        // grid 배열 상태 확인
        if (grid == null)
        {
            Debug.LogError("[StageGridManager] grid array is NULL!");
            return;
        }

        Debug.Log($"[StageGridManager] grid size: {grid.GetLength(0)}x{grid.GetLength(1)}");
        Debug.Log($"[StageGridManager] Looking at grid[{x}, {y}]");

        // 해당 위치와 주변 블록 확인
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int checkX = x + dx;
                int checkY = y + dy;

                if (checkX >= 0 && checkX < grid.GetLength(0) &&
                    checkY >= 0 && checkY < grid.GetLength(1))
                {
                    GameObject blockObj = grid[checkX, checkY];
                    if (blockObj != null)
                    {
                        Block blockComp = blockObj.GetComponent<Block>();
                        string blockInfo = blockComp != null ? $"isEmpty:{blockComp.isEmpty}, tag:{blockObj.tag}" : "NO BLOCK COMPONENT";
                        Debug.Log($"[Grid] ({checkX},{checkY}): {blockObj.name} - {blockInfo}");
                    }
                    else
                    {
                        Debug.Log($"[Grid] ({checkX},{checkY}): NULL");
                    }
                }
            }
        }

        // matchingSystem 체크
        if (matchingSystem == null)
        {
            Debug.LogError("[StageGridManager] matchingSystem is NULL!");
            return;
        }

        Debug.Log($"[StageGridManager] Calling matchingSystem.FindMatchingBlocks({x}, {y})");
        List<GameObject> matchedBlocks = matchingSystem.FindMatchingBlocks(x, y, grid);

        Debug.Log($"[StageGridManager] Found {matchedBlocks.Count} matches");

        if (matchedBlocks.Count > 0)
        {
            Debug.Log("[StageGridManager] Destroying matched blocks...");
            // 블록 파괴 로직
            ProcessMatchedBlocks(matchedBlocks);
        }
        else
        {
            Debug.LogWarning("[StageGridManager] No matches found!");
        }

        //if (grid == null || x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
        //{
        //    Debug.LogWarning($"Invalid click position: ({x}, {y})");
        //    return;
        //}

        //Debug.Log($"Stage Mode: Empty block clicked at ({x}, {y})");

        //if (matchingSystem != null)
        //{
        //    Debug.Log("matchingSystem is not null)");

        //    List<GameObject> matchedBlocks = matchingSystem.FindMatchingBlocks(x, y, grid);

        //    if (matchedBlocks.Count > 0)
        //    {
        //        Debug.Log("Stage Mode: matchedBlocks.Count > 0)");

        //        ProcessMatchedBlocks(matchedBlocks);
        //    }
        //}
    }

    protected override void ProcessMatchedBlocks(List<GameObject> matchedBlocks)
    {
        if (matchedBlocks == null || matchedBlocks.Count == 0) return;

        Debug.Log($"Stage Mode: Processing {matchedBlocks.Count} matched blocks");

        int scoreGained = 0;
        if (matchingSystem != null)
        {
            scoreGained = matchingSystem.CalculateScore(matchedBlocks, scorePerBlock);
        }

        DestroyMatchedBlocks(matchedBlocks);
        AddScore(scoreGained);

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnBlocksDestroyed();
        }

        CheckWinCondition();
    }

    private void DestroyMatchedBlocks(List<GameObject> blocks)
    {
        foreach (GameObject block in blocks)
        {
            if (block == null) continue;

            Block blockComponent = block.GetComponent<Block>();
            if (blockComponent != null)
            {
                int x = blockComponent.x;
                int y = blockComponent.y;

                if (blockFactory != null)
                {
                    blockFactory.DestroyBlock(block);
                    grid[x, y] = blockFactory.CreateEmptyBlock(x, y);
                }
            }
        }
    }

    private void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    private void CheckWinCondition()
    {
        if (isCheckingWinCondition) return;
        isCheckingWinCondition = true;

        int remainingBlocks = CountRemainingBlocks();

        if (remainingBlocks == 0)
        {
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnStageCleared();
            }

            //if (winPanel != null)
            //{
            //    winPanel.SetActive(true);
            //}
        }
        else if (!CanMakeAnyMatch())
        {
            HandleDeadlockSituation();
        }

        isCheckingWinCondition = false;
    }

    private int CountRemainingBlocks()
    {
        int count = 0;
        if (grid == null) return 0;

        int gridWidth = grid.GetLength(0);
        int gridHeight = grid.GetLength(1);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject block = grid[x, y];
                if (block != null)
                {
                    Block blockComponent = block.GetComponent<Block>();
                    if (blockComponent != null && !blockComponent.isEmpty)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    private bool CanMakeAnyMatch()
    {
        return matchingSystem.HasAnyPossibleMatch(grid);
    }

    private void HandleDeadlockSituation()
    {
        StartCoroutine(HandleDeadlockFlow());
    }

    // 모든 데드락 처리 로직들...
    // (기존 GridManagerRefactored의 스테이지 관련 로직들)
    private IEnumerator HandleDeadlockFlow()
    {
        Debug.Log("=== Starting Deadlock Flow ===");

        // 1번: 파괴 가능한 블록 조합이 없는 상황 (이미 확인됨)

        // 2번: 남은 블록 개수 체크
        int remainingBlocks = CountRemainingBlocks();
        Debug.Log($"Remaining blocks: {remainingBlocks}");

        if (remainingBlocks == 0)
        {
            CheckWinCondition();
            yield break;
        }
        else if (remainingBlocks == 1)
        {
            // 자동 파괴 → 스테이지 클리어
            yield return StartCoroutine(AutoDestroyLastBlock());
            yield break;
        }

        // 3번: 일렬 체크
        List<ShuffleBlockData> currentState = SaveCurrentState();
        if (AreAllBlocksInLine(currentState))
        {
            // 4번: 게임 오버
            Debug.Log("All blocks in line - Game Over!");
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnStageFailed("블록이 일렬로 배치되어 더 이상 진행할 수 없습니다!");
            }
            yield break;
        }

        // 5번: 2~3개면 같은 색으로 변경
        if (remainingBlocks <= 3)
        {
            yield return StartCoroutine(TransformToSameColor(currentState));
            yield break; // 유저 플레이 재개
        }

        // 6번: 4개 이상 - 셔플로 해결 가능한지 체크
        bool canSolveWithShuffle = CheckIfShuffleCanSolve(currentState);

        if (canSolveWithShuffle)
        {
            // 7번: 위치 교체 셔플
            yield return StartCoroutine(ShuffleRemainingBlocks());
            yield break; // 유저 플레이 재개
        }
        else
        {
            // 8번: (블록 수/2)개의 색으로 변경
            yield return StartCoroutine(TransformToHalfColors(currentState));

            // 다시 1번으로 - 매칭 가능한지 체크
            if (!CanMakeAnyMatch())
            {
                // 여전히 매칭 불가능하면 다시 플로우 시작
                yield return StartCoroutine(HandleDeadlockFlow());
            }
        }
    }

    // 3. 같은 색으로 변경 (2~3개)
    private IEnumerator TransformToSameColor(List<ShuffleBlockData> blocks)
    {
        Debug.Log($"Transforming {blocks.Count} blocks to same color");

        List<GameObject> blockObjects = new List<GameObject>();
        foreach (var data in blocks)
        {
            blockObjects.Add(data.originalBlock);
        }

        // 회전 애니메이션
        yield return StartCoroutine(RotateBlocksAnimation(blockObjects));

        // 모두 같은 색으로 변경
        int sameColor = Random.Range(1, 6);
        foreach (var data in blocks)
        {
            Vector2Int pos = data.position;
            if (grid[pos.x, pos.y] != null)
            {
                blockFactory.DestroyBlock(grid[pos.x, pos.y]);
                grid[pos.x, pos.y] = blockFactory.CreateBlockFromType(sameColor, pos.x, pos.y);
            }
        }

        yield return new WaitForSeconds(0.5f);
        Debug.Log("All blocks transformed to same color - Ready to play!");
    }

    // 4. 절반 색으로 변경 (4개 이상)
    private IEnumerator TransformToHalfColors(List<ShuffleBlockData> blocks)
    {
        int blockCount = blocks.Count;
        int colorTypeCount = blockCount / 2;
        Debug.Log($"Transforming {blockCount} blocks to {colorTypeCount} color types");

        List<GameObject> blockObjects = new List<GameObject>();
        foreach (var data in blocks)
        {
            blockObjects.Add(data.originalBlock);
        }

        // 회전 애니메이션
        yield return StartCoroutine(RotateBlocksAnimation(blockObjects));

        // 색상 종류 결정
        List<int> colorTypes = new List<int>();
        for (int i = 0; i < colorTypeCount; i++)
        {
            colorTypes.Add(Random.Range(1, 6));
        }

        // 각 색상을 최소 2개씩 배치
        List<int> finalColors = new List<int>();
        for (int i = 0; i < blockCount; i++)
        {
            finalColors.Add(colorTypes[i % colorTypeCount]);
        }

        // 셔플
        for (int i = finalColors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = finalColors[i];
            finalColors[i] = finalColors[j];
            finalColors[j] = temp;
        }

        // 적용
        for (int i = 0; i < blocks.Count; i++)
        {
            Vector2Int pos = blocks[i].position;
            if (grid[pos.x, pos.y] != null)
            {
                blockFactory.DestroyBlock(grid[pos.x, pos.y]);
                grid[pos.x, pos.y] = blockFactory.CreateBlockFromType(finalColors[i], pos.x, pos.y);
            }
        }

        yield return new WaitForSeconds(0.5f);
        Debug.Log("Blocks transformed to half colors");
    }

    // 5. 셔플로 해결 가능한지 체크하는 코루틴
    private bool CheckIfShuffleCanSolve(List<ShuffleBlockData> currentState)
    {
        Debug.Log("Checking if shuffle can solve...");

        // 블록 종류가 5개이므로 6개 이상이면 무조건 가능
        if (currentState.Count >= 6)
        {
            return true;
        }

        // 5개 이하면 모든 순열 체크
        List<Vector2Int> positions = new List<Vector2Int>();
        List<int> blockTypes = new List<int>();

        foreach (var data in currentState)
        {
            positions.Add(data.position);
            blockTypes.Add(data.blockType);
        }

        // 모든 순열 생성
        List<List<int>> allPermutations = GeneratePermutations(blockTypes);

        foreach (var permutation in allPermutations)
        {
            if (CheckIfPermutationHasMatch(positions, permutation))
            {
                return true;
            }
        }

        return false;
    }

    // 모든 블록이 일렬로 붙어있는지 확인
    private bool AreAllBlocksInLine(List<ShuffleBlockData> blocks)
    {
        if (blocks.Count <= 1) return true;

        // 모든 블록의 위치 추출
        List<Vector2Int> positions = new List<Vector2Int>();
        foreach (var block in blocks)
        {
            positions.Add(block.position);
        }

        // 위치 정렬 (x 우선, 그 다음 y)
        positions.Sort((a, b) => {
            int xCompare = a.x.CompareTo(b.x);
            return xCompare != 0 ? xCompare : a.y.CompareTo(b.y);
        });

        // 가로 일렬 체크
        bool isHorizontalLine = true;
        int firstY = positions[0].y;
        for (int i = 1; i < positions.Count; i++)
        {
            // Y 좌표가 다르거나, X 좌표가 연속적이지 않으면 가로 일렬이 아님
            if (positions[i].y != firstY || positions[i].x != positions[i - 1].x + 1)
            {
                isHorizontalLine = false;
                break;
            }
        }

        if (isHorizontalLine)
        {
            Debug.Log("Blocks form a horizontal line");
            return true;
        }

        // 세로 일렬 체크를 위해 y 기준으로 재정렬
        positions.Sort((a, b) => {
            int yCompare = a.y.CompareTo(b.y);
            return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
        });

        bool isVerticalLine = true;
        int firstX = positions[0].x;
        for (int i = 1; i < positions.Count; i++)
        {
            // X 좌표가 다르거나, Y 좌표가 연속적이지 않으면 세로 일렬이 아님
            if (positions[i].x != firstX || positions[i].y != positions[i - 1].y + 1)
            {
                isVerticalLine = false;
                break;
            }
        }

        if (isVerticalLine)
        {
            Debug.Log("Blocks form a vertical line");
            return true;
        }

        return false;
    }

    // 순열 생성 메서드
    private List<List<int>> GeneratePermutations(List<int> items)
    {
        List<List<int>> result = new List<List<int>>();

        if (items.Count == 0)
        {
            result.Add(new List<int>());
            return result;
        }

        if (items.Count == 1)
        {
            result.Add(new List<int> { items[0] });
            return result;
        }

        // 재귀적으로 모든 순열 생성
        for (int i = 0; i < items.Count; i++)
        {
            int current = items[i];
            List<int> remaining = new List<int>(items);
            remaining.RemoveAt(i);

            List<List<int>> subPermutations = GeneratePermutations(remaining);

            foreach (var subPerm in subPermutations)
            {
                List<int> permutation = new List<int> { current };
                permutation.AddRange(subPerm);
                result.Add(permutation);
            }
        }

        return result;
    }

    // 특정 순열이 매칭 가능한지 확인
    private bool CheckIfPermutationHasMatch(List<Vector2Int> positions, List<int> blockTypes)
    {
        // 임시 그리드 생성
        GameObject[,] testGrid = new GameObject[width, height];

        // 빈 블록 복사
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject block = grid[x, y];
                if (block != null)
                {
                    Block blockComponent = block.GetComponent<Block>();
                    if (blockComponent != null && blockComponent.isEmpty)
                    {
                        testGrid[x, y] = block;
                    }
                }
            }
        }

        // 테스트 블록 배치
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            GameObject dummyBlock = new GameObject($"TestBlock_{blockTypes[i]}");
            dummyBlock.tag = blockFactory.GetTagFromBlockType(blockTypes[i]);

            // Block 컴포넌트도 추가해야 MatchingSystem이 제대로 체크 가능
            Block blockComp = dummyBlock.AddComponent<Block>();
            blockComp.x = pos.x;
            blockComp.y = pos.y;
            blockComp.isEmpty = false;

            testGrid[pos.x, pos.y] = dummyBlock;
        }

        // 매칭 가능한지 확인
        bool canMatch = matchingSystem.HasAnyPossibleMatch(testGrid);

        // 테스트용 더미 오브젝트 정리
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            if (testGrid[pos.x, pos.y] != null && testGrid[pos.x, pos.y].name.StartsWith("TestBlock_"))
            {
                Destroy(testGrid[pos.x, pos.y]);
            }
        }

        return canMatch;
    }

    // 3. 현재 상태 저장
    private List<ShuffleBlockData> SaveCurrentState()
    {
        List<ShuffleBlockData> state = new List<ShuffleBlockData>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject block = grid[x, y];
                if (block != null)
                {
                    Block blockComponent = block.GetComponent<Block>();
                    if (blockComponent != null && !blockComponent.isEmpty)
                    {
                        ShuffleBlockData data = new ShuffleBlockData
                        {
                            position = new Vector2Int(x, y),
                            blockType = blockFactory.GetBlockTypeFromTag(block.tag),
                            originalBlock = block
                        };
                        state.Add(data);
                    }
                }
            }
        }

        return state;
    }

    private IEnumerator AutoDestroyLastBlock()
    {
        Debug.Log("Auto-destroying last block!");

        // 마지막 블록 찾기
        GameObject lastBlock = null;
        Vector2Int lastBlockPos = new Vector2Int(-1, -1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject block = grid[x, y];
                if (block != null)
                {
                    Block blockComponent = block.GetComponent<Block>();
                    if (blockComponent != null && !blockComponent.isEmpty)
                    {
                        lastBlock = block;
                        lastBlockPos = new Vector2Int(x, y);
                        break;
                    }
                }
            }
            if (lastBlock != null) break;
        }

        // 애니메이션 효과 (나중에 추가)
        yield return new WaitForSeconds(0.5f);

        // 블록 파괴
        if (lastBlock != null)
        {
            blockFactory.DestroyBlock(lastBlock);
            grid[lastBlockPos.x, lastBlockPos.y] = blockFactory.CreateEmptyBlock(lastBlockPos.x, lastBlockPos.y);
        }

        // 승리 처리
        StageManager stageManager = FindFirstObjectByType<StageManager>();
        if (stageManager != null)
        {
            stageManager.OnStageCleared();
        }

        //if (winPanel != null)
        //{
        //    winPanel.SetActive(true);
        //}
    }

    private IEnumerator RotateBlocksAnimation(List<GameObject> blocks)
    {
        Debug.Log("Rotating blocks animation (placeholder)");

        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float rotation = progress * 360f;

            foreach (var block in blocks)
            {
                if (block != null)
                {
                    block.transform.rotation = Quaternion.Euler(0, 0, rotation);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 회전 초기화
        foreach (var block in blocks)
        {
            if (block != null)
            {
                block.transform.rotation = Quaternion.identity;
            }
        }
    }

    private System.Collections.IEnumerator ShuffleRemainingBlocks()
    {
        Debug.Log("Shuffling remaining blocks...");

        // StageShuffleSystem 사용하도록 변경
        StageShuffleSystem shuffleSystem = GetComponent<StageShuffleSystem>();
        if (shuffleSystem != null)
        {
            yield return StartCoroutine(shuffleSystem.ExecuteShuffle(grid, width, height));

            // 셔플 후 다시 매칭 가능한지 확인
            if (!matchingSystem.HasAnyPossibleMatch(grid))
            {
                Debug.Log("Still no matches after shuffle!");
                HandleDeadlockSituation(); // 재귀적으로 다시 시도
            }
        }
        else
        {
            Debug.LogError("StageShuffleSystem not found!");
        }
    }

    // cellSize 프로퍼티 추가
    public float cellSize
    {
        get
        {
            return layoutManager != null ? layoutManager.cellSize : 1.0f;
        }
        set
        {
            if (layoutManager != null)
                layoutManager.UpdateCellSize(value);
        }
    }

    // InitializeGridWithPattern 메서드 추가
    public void InitializeGridWithPattern(int[,] pattern)
    {
        if (pattern == null)
        {
            Debug.LogWarning("Pattern is null, initializing with random blocks");
            InitializeGrid();
            return;
        }

        SetupGrid();
        CreateBlocksFromPattern2D(pattern);
        SetupCameraAndLayout();

        currentScore = 0;
        UpdateScoreText();
    }

    // CreateBlocksFromPattern2D 메서드 추가 (2D 배열용)
    private void CreateBlocksFromPattern2D(int[,] pattern)
    {
        if (pattern == null || blockFactory == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int blockType = 0;

                if (x < pattern.GetLength(0) && y < pattern.GetLength(1))
                {
                    blockType = pattern[x, y];
                }

                grid[x, y] = blockFactory.CreateBlockFromType(blockType, x, y);
            }
        }
    }

    // InitializeGrid 메서드 추가 (테스트용)
    public void InitializeGrid()
    {
        SetupGrid();
        CreateRandomBlocks();
        SetupCameraAndLayout();

        currentScore = 0;
        UpdateScoreText();
    }

    // CreateRandomBlocks 메서드 추가
    private void CreateRandomBlocks()
    {
        if (blockFactory == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Random.value < 0.3f)
                {
                    grid[x, y] = blockFactory.CreateEmptyBlock(x, y);
                }
                else
                {
                    grid[x, y] = blockFactory.CreateRandomBlock(x, y);
                }
            }
        }
    }

    #region Item System Support Methods

    // Grid dimension methods - 기존 width, height 변수 사용
    public int GetGridWidth()
    {
        return width;
    }

    public int GetGridHeight()
    {
        return height;
    }

    // Block access methods - BaseGridManager의 기존 메서드들과 호환
    public Block GetBlockComponentAt(int x, int y)
    {
        GameObject blockObj = GetBlockAt(x, y); // BaseGridManager의 기존 메서드 사용
        if (blockObj != null)
        {
            return blockObj.GetComponent<Block>();
        }
        return null;
    }

    public void DestroyBlockAt(int x, int y)
    {
        GameObject targetBlock = GetBlockAt(x, y);
        if (targetBlock == null)
            return;

        Block blockComponent = targetBlock.GetComponent<Block>();
        if (blockComponent == null || blockComponent.isEmpty) // 빈 블록은 파괴하지 않음
            return;

        Debug.Log("Destroying block at position: " + x + ", " + y);

        // BlockFactory를 사용하여 블록 파괴
        if (blockFactory != null)
        {
            blockFactory.DestroyBlock(targetBlock);
            // 빈 블록으로 교체
            grid[x, y] = blockFactory.CreateEmptyBlock(x, y);
        }
        else
        {
            // Fallback: 직접 파괴
            Destroy(targetBlock);
            grid[x, y] = null;
        }

        // 아이템 사용 후 매치 체크
        StartCoroutine(CheckMatchesAfterItemUse(0.2f));
    }

    // World position conversion - BaseGridManager의 GridToWorldPosition 사용
    public Vector3 GetWorldPositionFromGrid(int x, int y)
    {
        return GridToWorldPosition(x, y); // BaseGridManager의 기존 메서드 사용
    }

    // Score management - 기존 AddScore 메서드 오버로드
    public void AddScoreFromItem(int points)
    {
        AddScore(points); // 기존 private AddScore 메서드 호출
    }

    // Helper method to get all non-empty blocks
    public List<Block> GetAllNonEmptyBlocks()
    {
        List<Block> nonEmptyBlocks = new List<Block>();

        if (grid == null) return nonEmptyBlocks;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Block block = GetBlockComponentAt(x, y);
                if (block != null && !block.isEmpty)
                {
                    nonEmptyBlocks.Add(block);
                }
            }
        }

        return nonEmptyBlocks;
    }

    // Helper method to get all blocks in the grid
    public List<Block> GetAllBlocks()
    {
        List<Block> allBlocks = new List<Block>();

        if (grid == null) return allBlocks;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Block block = GetBlockComponentAt(x, y);
                if (block != null)
                {
                    allBlocks.Add(block);
                }
            }
        }

        return allBlocks;
    }

    // Coroutine for checking matches after item usage
    private System.Collections.IEnumerator CheckMatchesAfterItemUse(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("Checking for matches after item use");

        // 기존 매칭 시스템 사용
        if (matchingSystem != null)
        {
            // 매치 가능한 블록이 있는지 확인
            bool hasMatches = matchingSystem.HasAnyPossibleMatch(grid);
            Debug.Log("Has possible matches after item use: " + hasMatches);

            // 필요하다면 데드락 상황 처리
            if (!hasMatches && CountRemainingBlocks() > 0)
            {
                HandleDeadlockSituation(); // 기존 메서드 호출
            }
        }
    }

    #endregion
}