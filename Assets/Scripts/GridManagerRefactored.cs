using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GridManagerRefactored : MonoBehaviour
{
    [System.NonSerialized]
    public System.Action<int, int> onEmptyBlockClicked;

    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public Transform gridParent;

    [Header("Game Settings")]
    public int scorePerBlock = 10;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public GameObject winPanel;

    [Header("Components")]
    public CameraController cameraController;
    public GridLayoutManager layoutManager;
    public BlockFactory blockFactory;
    public MatchingSystem matchingSystem;

    [Header("Shuffle Settings")]
    private int shuffleAttemptCount = 0;
    private const int MAX_SHUFFLE_ATTEMPTS = 3;

    [Header("Stage Shuffle System")]
    public StageShuffleSystem shuffleSystem;

    private bool isCheckingWinCondition = false;

    public int currentScore = 0;
    private GameObject[,] grid;

    void Awake()
    {
        InitializeComponents();

        if (shuffleSystem == null)
            shuffleSystem = GetComponent<StageShuffleSystem>();
    }

    void Start()
    {
        if (IsInfiniteMode())
        {
            Debug.Log("Infinite mode detected, skipping auto initialization");
            return;
        }

        InitializeGrid();
        UpdateScoreText();
    }

    private void InitializeComponents()
    {
        // 컴포넌트 자동 할당
        if (cameraController == null)
            cameraController = GetComponent<CameraController>();

        if (layoutManager == null)
            layoutManager = GetComponent<GridLayoutManager>();

        if (blockFactory == null)
            blockFactory = GetComponent<BlockFactory>();

        if (matchingSystem == null)
            matchingSystem = GetComponent<MatchingSystem>();
    }

    public void InitializeGrid()
    {
        SetupGrid();
        CreateRandomBlocks();
        SetupCameraAndLayout();
    }

    public void InitializeStageGrid(StageData stageData)
    {
        if (stageData == null) return;

        width = stageData.gridWidth;
        height = stageData.gridHeight;

        // 기존 그리드 정리 전에 새 크기로 그리드 생성
        ClearGrid();
        SetupGrid();  // 새로운 크기로 그리드 생성

        CreateBlocksFromPattern(stageData.blockPattern);
        SetupCameraAndLayout();

        currentScore = 0;
        UpdateScoreText();
    }

    private void SetupGrid()
    {
        grid = new GameObject[width, height];

        if (layoutManager != null)
        {
            layoutManager.SetupLayout(width, height, 1.0f);
        }

        if (blockFactory != null)
        {
            blockFactory.SetGridParent(gridParent);
        }
    }

    private void SetupGridFromStageData(StageData stageData)
    {
        width = stageData.gridWidth;
        height = stageData.gridHeight;

        SetupGrid();
    }

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

    private void SetupCameraAndLayout()
    {
        if (cameraController != null && layoutManager != null)
        {
            cameraController.AdjustCameraForGrid(width, height, layoutManager.cellSize);
            Vector3 gridCenter = layoutManager.GetGridCenter();
            cameraController.CenterCameraOnGrid(gridCenter);
        }
    }

    public void ClearGrid()
    {
        if (grid != null)
        {
            int gridWidth = grid.GetLength(0);
            int gridHeight = grid.GetLength(1);

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        if (blockFactory != null)
                        {
                            blockFactory.DestroyBlock(grid[x, y]);
                        }
                        else
                        {
                            Destroy(grid[x, y]);
                        }
                        grid[x, y] = null;
                    }
                }
            }
        }

        // 부모 컨테이너도 정리
        if (gridParent != null)
        {
            for (int i = gridParent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(gridParent.transform.GetChild(i).gameObject);
            }
        }

        Debug.Log("Grid completely cleared");
    }

    private void ProcessMatchedBlocks(List<GameObject> matchedBlocks)
    {
        if (matchedBlocks == null || matchedBlocks.Count == 0) return;

        Debug.Log($"Processing {matchedBlocks.Count} matched blocks");

        // 점수 계산
        int scoreGained = 0;
        if (matchingSystem != null)
        {
            scoreGained = matchingSystem.CalculateScore(matchedBlocks, scorePerBlock);
        }

        // 블록 파괴 및 빈 블록으로 교체
        DestroyMatchedBlocks(matchedBlocks);

        // 점수 추가
        AddScore(scoreGained);

        // 다른 시스템에 알림
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

            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
        }
        else if (!CanMakeAnyMatch())
        {
            HandleDeadlockSituation();
        }

        isCheckingWinCondition = false;

        /*
        if (currentScore >= targetScore)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageComplete();
            }

            // 유저 데이터 업데이트
            if (UserDataManager.Instance != null)
            {
                int currentStageNumber = StageManager.Instance != null ?
                    StageManager.Instance.GetCurrentStageNumber() : 1;

                UserDataManager.Instance.GiveStageReward(currentStageNumber, currentScore);
                UserDataManager.Instance.UpdateStageProgress(currentStageNumber, currentScore, true);
            }

            Debug.Log("스테이지 완료!");
        }
        */
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

    private IEnumerator TrySmartShuffle()
    {
        Debug.Log("Attempting smart shuffle...");

        // 현재 블록 상태 저장
        List<ShuffleBlockData> currentState = SaveCurrentState();

        // 1. 먼저 모든 블록이 일렬로 붙어있는지 확인
        if (AreAllBlocksInLine(currentState))
        {
            Debug.Log("All blocks are in a straight line - shuffle won't help!");

            // 셔플로도 해결 불가능 - 특수 처리로 이동
            int remainingBlocks = CountRemainingBlocks();
            StartCoroutine(HandleSingleColorBlocks(remainingBlocks));
            yield break;
        }

        // 2. 일렬이 아니라면 비둘기집 원리 적용
        int blockTypeCount = 5; // 블록 종류 수

        if (currentState.Count > blockTypeCount)
        {
            Debug.Log($"Have {currentState.Count} blocks with {blockTypeCount} types - shuffle guaranteed to work!");

            // 기존 셔플 시스템 사용 (무조건 매칭 가능한 조합이 나옴)
            yield return StartCoroutine(ShuffleRemainingBlocks());
            yield break;
        }

        // 3. 블록이 종류 수 이하이고 일렬도 아닌 경우
        Debug.Log($"Only {currentState.Count} blocks remaining but not in line - shuffle will help!");
        yield return StartCoroutine(ShuffleRemainingBlocks());
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

    // 특정 셔플 결과 적용
    private IEnumerator ApplySpecificShuffle(List<Vector2Int> positions, List<int> blockTypes)
    {
        // 애니메이션과 함께 블록 재배치
        if (shuffleSystem != null)
        {
            // StageShuffleSystem의 애니메이션 활용
            yield return StartCoroutine(shuffleSystem.ExecuteShuffle(grid, width, height));
        }
        else
        {
            // 직접 블록 재배치
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int pos = positions[i];
                if (grid[pos.x, pos.y] != null)
                {
                    blockFactory.DestroyBlock(grid[pos.x, pos.y]);
                    grid[pos.x, pos.y] = blockFactory.CreateBlockFromType(blockTypes[i], pos.x, pos.y);
                }
            }
        }
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

    // 4. 셔플 시뮬레이션
    private GameObject[,] SimulateShuffleResult(List<ShuffleBlockData> currentState)
    {
        GameObject[,] simulatedGrid = new GameObject[width, height];

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
                        simulatedGrid[x, y] = block;
                    }
                }
            }
        }

        // 블록 타입들 셔플
        List<int> blockTypes = new List<int>();
        foreach (var data in currentState)
        {
            blockTypes.Add(data.blockType);
        }

        // Fisher-Yates 셔플
        for (int i = blockTypes.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = blockTypes[i];
            blockTypes[i] = blockTypes[randomIndex];
            blockTypes[randomIndex] = temp;
        }

        // 셔플된 타입으로 가상 블록 배치
        for (int i = 0; i < currentState.Count; i++)
        {
            Vector2Int pos = currentState[i].position;
            // 시뮬레이션을 위한 더미 게임오브젝트 생성
            GameObject dummyBlock = new GameObject($"SimBlock_{blockTypes[i]}");
            dummyBlock.tag = blockFactory.GetTagFromBlockType(blockTypes[i]);
            simulatedGrid[pos.x, pos.y] = dummyBlock;
        }

        return simulatedGrid;
    }

    // 5. 셔플 결과 적용
    private IEnumerator ApplyShuffleResult(GameObject[,] validResult)
    {
        // 시뮬레이션에서 찾은 유효한 결과를 실제로 적용
        if (shuffleSystem != null)
        {
            yield return StartCoroutine(shuffleSystem.ExecuteShuffle(grid, width, height));
        }

        // 시뮬레이션용 더미 오브젝트 정리
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (validResult[x, y] != null && validResult[x, y].name.StartsWith("SimBlock_"))
                {
                    Destroy(validResult[x, y]);
                }
            }
        }
    }

    private IEnumerator HandleSingleColorBlocks(int blockCount)
    {
        Debug.Log($"Handling {blockCount} single color blocks");

        List<Vector2Int> blockPositions = new List<Vector2Int>();
        List<GameObject> blocks = new List<GameObject>();

        // 남은 블록들 수집
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
                        blockPositions.Add(new Vector2Int(x, y));
                        blocks.Add(block);
                    }
                }
            }
        }

        // 회전 애니메이션
        yield return StartCoroutine(RotateBlocksAnimation(blocks));

        // 매칭 가능한 색상 조합 찾기
        bool foundValidConfiguration = false;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            List<int> testColors = GenerateColorConfiguration(blockCount);

            // 테스트 구성으로 매칭 가능한지 확인
            if (CanMakeMatchWithColors(blockPositions, testColors))
            {
                // 유효한 구성 찾음 - 적용
                ApplyColorConfiguration(blockPositions, testColors);
                foundValidConfiguration = true;
                break;
            }
        }

        if (!foundValidConfiguration)
        {
            // 매칭 가능한 구성을 못 찾은 경우 - 게임 오버
            Debug.Log("No valid color configuration found - Game Over");
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnShuffleAttemptFailed();
            }
        }
        else
        {
            Debug.Log("Found valid color configuration!");
        }

        yield return new WaitForSeconds(0.5f);
    }

    // 7. 색상 구성 생성
    private List<int> GenerateColorConfiguration(int blockCount)
    {
        List<int> colors = new List<int>();

        if (blockCount <= 3)
        {
            // 2~3개일 때 - 모두 같은 색
            int color = Random.Range(1, 6);
            for (int i = 0; i < blockCount; i++)
            {
                colors.Add(color);
            }
        }
        else
        {
            // 4개 이상일 때 - (블록 수/2)개의 색으로
            int colorCount = blockCount / 2;
            List<int> availableColors = new List<int>();

            for (int i = 0; i < colorCount; i++)
            {
                availableColors.Add(Random.Range(1, 6));
            }

            // 각 색상을 최소 2개씩 배치
            for (int i = 0; i < blockCount; i++)
            {
                colors.Add(availableColors[i % colorCount]);
            }

            // 섞기
            for (int i = colors.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                int temp = colors[i];
                colors[i] = colors[randomIndex];
                colors[randomIndex] = temp;
            }
        }

        return colors;
    }

    // 8. 색상 조합으로 매칭 가능한지 확인
    private bool CanMakeMatchWithColors(List<Vector2Int> positions, List<int> colors)
    {
        // 임시 그리드에 색상 배치하여 테스트
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

        // 테스트 색상 배치
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            GameObject dummyBlock = new GameObject($"TestBlock_{colors[i]}");
            dummyBlock.tag = blockFactory.GetTagFromBlockType(colors[i]);
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

    // 9. 색상 구성 적용
    private void ApplyColorConfiguration(List<Vector2Int> positions, List<int> colors)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            if (grid[pos.x, pos.y] != null)
            {
                blockFactory.DestroyBlock(grid[pos.x, pos.y]);
                grid[pos.x, pos.y] = blockFactory.CreateBlockFromType(colors[i], pos.x, pos.y);
            }
        }
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

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
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

    private bool HasMatchableColorGroups()
    {
        if (grid == null) return false;

        Dictionary<string, int> colorCounts = matchingSystem.CountBlocksByColor(grid);

        // 2개 이상인 색깔이 하나라도 있는지 확인
        foreach (var count in colorCounts.Values)
        {
            if (count >= 2)
                return true;
        }

        return false;
    }

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

        UpdateScoreText();
    }

    public void InitializeEmptyGrid()
    {
        ClearGrid();
        SetupGrid();
        SetupCameraAndLayout();

        Debug.Log($"Empty grid initialized: {width}x{height}");
    }

    public void OnEmptyBlockClicked(int x, int y)
    {
        // 범위 체크 추가
        if (grid == null || x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
        {
            Debug.LogWarning($"Invalid click position: ({x}, {y})");
            return;
        }

        Debug.Log($"Empty block clicked at ({x}, {y})");

        // 무한모드 콜백 처리
        if (onEmptyBlockClicked != null)
        {
            onEmptyBlockClicked(x, y);
            return;
        }

        // 일반 매칭 처리
        if (matchingSystem != null)
        {
            List<GameObject> matchedBlocks = matchingSystem.FindMatchingBlocks(x, y, grid);

            if (matchedBlocks.Count > 0)
            {
                ProcessMatchedBlocks(matchedBlocks);
            }
        }
    }

    public GameObject GetBlockAt(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height && grid != null)
        {
            return grid[x, y];
        }
        return null;
    }

    public void SetBlockAt(int x, int y, GameObject block)
    {
        if (x >= 0 && x < width && y >= 0 && y < height && grid != null)
        {
            grid[x, y] = block;
        }
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        if (layoutManager != null)
        {
            return layoutManager.GridToWorldPosition(x, y);
        }
        return Vector3.zero;
    }

    public void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
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

    public int[,] ExportCurrentPattern()
    {
        if (grid == null) return null;

        int[,] pattern = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    Block blockComponent = grid[x, y].GetComponent<Block>();
                    if (blockComponent != null && blockComponent.isEmpty)
                    {
                        pattern[x, y] = 0; // Empty block
                    }
                    else if (blockFactory != null)
                    {
                        string tag = grid[x, y].tag;
                        pattern[x, y] = blockFactory.GetBlockTypeFromTag(tag);
                    }
                    else
                    {
                        pattern[x, y] = 0;
                    }
                }
                else
                {
                    pattern[x, y] = 0; // Null = empty
                }
            }
        }

        return pattern;
    }

    public void UpdateCellSize(float newCellSize)
    {
        if (layoutManager != null)
        {
            layoutManager.UpdateCellSize(newCellSize);

            // 기존 블록들의 위치 업데이트
            if (grid != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (grid[x, y] != null && blockFactory != null)
                        {
                            blockFactory.UpdateBlockPosition(grid[x, y], x, y);
                        }
                    }
                }
            }

            SetupCameraAndLayout();
        }
    }

    public void RecalculateLayout()
    {
        SetupCameraAndLayout();

        // 기존 블록들의 위치 재계산
        if (layoutManager != null && grid != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        Vector3 newWorldPos = layoutManager.GridToWorldPosition(x, y);
                        grid[x, y].transform.position = newWorldPos;
                    }
                }
            }
        }

        // 카메라 위치 검증
        if (cameraController != null && layoutManager != null)
        {
            Vector3 expectedCenter = layoutManager.GetGridCenter();
            if (!cameraController.ValidateCameraPosition(expectedCenter))
            {
                cameraController.CenterCameraOnGrid(expectedCenter);
            }
        }
    }

    private bool IsInfiniteMode()
    {
        // InfiniteModeManager가 존재하는지 확인
        return FindFirstObjectByType<InfiniteModeManager>() != null;
    }

    public bool ValidateGridState()
    {
        if (grid == null)
        {
            Debug.LogError("Grid is null!");
            return false;
        }

        if (layoutManager == null)
        {
            Debug.LogError("GridLayoutManager is missing!");
            return false;
        }

        if (blockFactory == null)
        {
            Debug.LogError("BlockFactory is missing!");
            return false;
        }

        return true;
    }

    public void CalculateGridCenterOffset()
    {
        if (layoutManager != null)
        {
            // GridLayoutManager에서 자동으로 처리되므로 별도 작업 불필요
            Debug.Log("Grid center offset calculated through LayoutManager");
        }
    }

    public void CalculateOptimalCameraSize()
    {
        if (cameraController != null && layoutManager != null)
        {
            cameraController.AdjustCameraForGrid(width, height, layoutManager.cellSize);
            Debug.Log("Camera size optimized through CameraController");
        }
    }

    public void AdjustCameraPosition()
    {
        if (cameraController != null && layoutManager != null)
        {
            Vector3 gridCenter = layoutManager.GetGridCenter();
            cameraController.CenterCameraOnGrid(gridCenter);
            Debug.Log("Camera position adjusted through CameraController");
        }
    }

    // 3. 호환성을 위한 추가 메서드들
    public void SetGridParent(Transform parent)
    {
        if (blockFactory != null)
        {
            blockFactory.SetGridParent(parent);
        }
    }

    // 4. cellSize 접근 프로퍼티
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

    // 디버그용 기즈모 그리기
    void OnDrawGizmos()
    {
        if (Application.isPlaying && grid != null && layoutManager != null)
        {
            // 그리드 경계 그리기 (노란색)
            Gizmos.color = Color.yellow;
            Rect bounds = layoutManager.GetGridBounds();
            Vector3 center = new Vector3(bounds.center.x, bounds.center.y, 0);
            Vector3 size = new Vector3(bounds.width, bounds.height, 0.1f);
            Gizmos.DrawWireCube(center, size);

            // 그리드 중심점 그리기 (빨간색)
            Gizmos.color = Color.red;
            Vector3 gridCenter = layoutManager.GetGridCenter();
            Gizmos.DrawWireSphere(gridCenter, 0.3f);
        }
    }
}