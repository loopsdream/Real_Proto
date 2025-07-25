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
        // ������Ʈ �ڵ� �Ҵ�
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

        // ���� �׸��� ���� ���� �� ũ��� �׸��� ����
        ClearGrid();
        SetupGrid();  // ���ο� ũ��� �׸��� ����

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

        // �θ� �����̳ʵ� ����
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

        // ���� ���
        int scoreGained = 0;
        if (matchingSystem != null)
        {
            scoreGained = matchingSystem.CalculateScore(matchedBlocks, scorePerBlock);
        }

        // ��� �ı� �� �� ������� ��ü
        DestroyMatchedBlocks(matchedBlocks);

        // ���� �߰�
        AddScore(scoreGained);

        // �ٸ� �ý��ۿ� �˸�
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

            // ���� ������ ������Ʈ
            if (UserDataManager.Instance != null)
            {
                int currentStageNumber = StageManager.Instance != null ?
                    StageManager.Instance.GetCurrentStageNumber() : 1;

                UserDataManager.Instance.GiveStageReward(currentStageNumber, currentScore);
                UserDataManager.Instance.UpdateStageProgress(currentStageNumber, currentScore, true);
            }

            Debug.Log("�������� �Ϸ�!");
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

        // 1��: �ı� ������ ��� ������ ���� ��Ȳ (�̹� Ȯ�ε�)

        // 2��: ���� ��� ���� üũ
        int remainingBlocks = CountRemainingBlocks();
        Debug.Log($"Remaining blocks: {remainingBlocks}");

        if (remainingBlocks == 0)
        {
            CheckWinCondition();
            yield break;
        }
        else if (remainingBlocks == 1)
        {
            // �ڵ� �ı� �� �������� Ŭ����
            yield return StartCoroutine(AutoDestroyLastBlock());
            yield break;
        }

        // 3��: �Ϸ� üũ
        List<ShuffleBlockData> currentState = SaveCurrentState();
        if (AreAllBlocksInLine(currentState))
        {
            // 4��: ���� ����
            Debug.Log("All blocks in line - Game Over!");
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnStageFailed("����� �Ϸķ� ��ġ�Ǿ� �� �̻� ������ �� �����ϴ�!");
            }
            yield break;
        }

        // 5��: 2~3���� ���� ������ ����
        if (remainingBlocks <= 3)
        {
            yield return StartCoroutine(TransformToSameColor(currentState));
            yield break; // ���� �÷��� �簳
        }

        // 6��: 4�� �̻� - ���÷� �ذ� �������� üũ
        bool canSolveWithShuffle = CheckIfShuffleCanSolve(currentState);

        if (canSolveWithShuffle)
        {
            // 7��: ��ġ ��ü ����
            yield return StartCoroutine(ShuffleRemainingBlocks());
            yield break; // ���� �÷��� �簳
        }
        else
        {
            // 8��: (��� ��/2)���� ������ ����
            yield return StartCoroutine(TransformToHalfColors(currentState));

            // �ٽ� 1������ - ��Ī �������� üũ
            if (!CanMakeAnyMatch())
            {
                // ������ ��Ī �Ұ����ϸ� �ٽ� �÷ο� ����
                yield return StartCoroutine(HandleDeadlockFlow());
            }
        }
    }

    // 3. ���� ������ ���� (2~3��)
    private IEnumerator TransformToSameColor(List<ShuffleBlockData> blocks)
    {
        Debug.Log($"Transforming {blocks.Count} blocks to same color");

        List<GameObject> blockObjects = new List<GameObject>();
        foreach (var data in blocks)
        {
            blockObjects.Add(data.originalBlock);
        }

        // ȸ�� �ִϸ��̼�
        yield return StartCoroutine(RotateBlocksAnimation(blockObjects));

        // ��� ���� ������ ����
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

    // 4. ���� ������ ���� (4�� �̻�)
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

        // ȸ�� �ִϸ��̼�
        yield return StartCoroutine(RotateBlocksAnimation(blockObjects));

        // ���� ���� ����
        List<int> colorTypes = new List<int>();
        for (int i = 0; i < colorTypeCount; i++)
        {
            colorTypes.Add(Random.Range(1, 6));
        }

        // �� ������ �ּ� 2���� ��ġ
        List<int> finalColors = new List<int>();
        for (int i = 0; i < blockCount; i++)
        {
            finalColors.Add(colorTypes[i % colorTypeCount]);
        }

        // ����
        for (int i = finalColors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = finalColors[i];
            finalColors[i] = finalColors[j];
            finalColors[j] = temp;
        }

        // ����
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

    // 5. ���÷� �ذ� �������� üũ�ϴ� �ڷ�ƾ
    private bool CheckIfShuffleCanSolve(List<ShuffleBlockData> currentState)
    {
        Debug.Log("Checking if shuffle can solve...");

        // ��� ������ 5���̹Ƿ� 6�� �̻��̸� ������ ����
        if (currentState.Count >= 6)
        {
            return true;
        }

        // 5�� ���ϸ� ��� ���� üũ
        List<Vector2Int> positions = new List<Vector2Int>();
        List<int> blockTypes = new List<int>();

        foreach (var data in currentState)
        {
            positions.Add(data.position);
            blockTypes.Add(data.blockType);
        }

        // ��� ���� ����
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

        // ���� ��� ���� ����
        List<ShuffleBlockData> currentState = SaveCurrentState();

        // 1. ���� ��� ����� �Ϸķ� �پ��ִ��� Ȯ��
        if (AreAllBlocksInLine(currentState))
        {
            Debug.Log("All blocks are in a straight line - shuffle won't help!");

            // ���÷ε� �ذ� �Ұ��� - Ư�� ó���� �̵�
            int remainingBlocks = CountRemainingBlocks();
            StartCoroutine(HandleSingleColorBlocks(remainingBlocks));
            yield break;
        }

        // 2. �Ϸ��� �ƴ϶�� ��ѱ��� ���� ����
        int blockTypeCount = 5; // ��� ���� ��

        if (currentState.Count > blockTypeCount)
        {
            Debug.Log($"Have {currentState.Count} blocks with {blockTypeCount} types - shuffle guaranteed to work!");

            // ���� ���� �ý��� ��� (������ ��Ī ������ ������ ����)
            yield return StartCoroutine(ShuffleRemainingBlocks());
            yield break;
        }

        // 3. ����� ���� �� �����̰� �Ϸĵ� �ƴ� ���
        Debug.Log($"Only {currentState.Count} blocks remaining but not in line - shuffle will help!");
        yield return StartCoroutine(ShuffleRemainingBlocks());
    }

    // ��� ����� �Ϸķ� �پ��ִ��� Ȯ��
    private bool AreAllBlocksInLine(List<ShuffleBlockData> blocks)
    {
        if (blocks.Count <= 1) return true;

        // ��� ����� ��ġ ����
        List<Vector2Int> positions = new List<Vector2Int>();
        foreach (var block in blocks)
        {
            positions.Add(block.position);
        }

        // ��ġ ���� (x �켱, �� ���� y)
        positions.Sort((a, b) => {
            int xCompare = a.x.CompareTo(b.x);
            return xCompare != 0 ? xCompare : a.y.CompareTo(b.y);
        });

        // ���� �Ϸ� üũ
        bool isHorizontalLine = true;
        int firstY = positions[0].y;
        for (int i = 1; i < positions.Count; i++)
        {
            // Y ��ǥ�� �ٸ��ų�, X ��ǥ�� ���������� ������ ���� �Ϸ��� �ƴ�
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

        // ���� �Ϸ� üũ�� ���� y �������� ������
        positions.Sort((a, b) => {
            int yCompare = a.y.CompareTo(b.y);
            return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
        });

        bool isVerticalLine = true;
        int firstX = positions[0].x;
        for (int i = 1; i < positions.Count; i++)
        {
            // X ��ǥ�� �ٸ��ų�, Y ��ǥ�� ���������� ������ ���� �Ϸ��� �ƴ�
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

    // ���� ���� �޼���
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

        // ��������� ��� ���� ����
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

    // Ư�� ������ ��Ī �������� Ȯ��
    private bool CheckIfPermutationHasMatch(List<Vector2Int> positions, List<int> blockTypes)
    {
        // �ӽ� �׸��� ����
        GameObject[,] testGrid = new GameObject[width, height];

        // �� ��� ����
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

        // �׽�Ʈ ��� ��ġ
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            GameObject dummyBlock = new GameObject($"TestBlock_{blockTypes[i]}");
            dummyBlock.tag = blockFactory.GetTagFromBlockType(blockTypes[i]);

            // Block ������Ʈ�� �߰��ؾ� MatchingSystem�� ����� üũ ����
            Block blockComp = dummyBlock.AddComponent<Block>();
            blockComp.x = pos.x;
            blockComp.y = pos.y;
            blockComp.isEmpty = false;

            testGrid[pos.x, pos.y] = dummyBlock;
        }

        // ��Ī �������� Ȯ��
        bool canMatch = matchingSystem.HasAnyPossibleMatch(testGrid);

        // �׽�Ʈ�� ���� ������Ʈ ����
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

    // Ư�� ���� ��� ����
    private IEnumerator ApplySpecificShuffle(List<Vector2Int> positions, List<int> blockTypes)
    {
        // �ִϸ��̼ǰ� �Բ� ��� ���ġ
        if (shuffleSystem != null)
        {
            // StageShuffleSystem�� �ִϸ��̼� Ȱ��
            yield return StartCoroutine(shuffleSystem.ExecuteShuffle(grid, width, height));
        }
        else
        {
            // ���� ��� ���ġ
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

    // 3. ���� ���� ����
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

    // 4. ���� �ùķ��̼�
    private GameObject[,] SimulateShuffleResult(List<ShuffleBlockData> currentState)
    {
        GameObject[,] simulatedGrid = new GameObject[width, height];

        // �� ��� ����
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

        // ��� Ÿ�Ե� ����
        List<int> blockTypes = new List<int>();
        foreach (var data in currentState)
        {
            blockTypes.Add(data.blockType);
        }

        // Fisher-Yates ����
        for (int i = blockTypes.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = blockTypes[i];
            blockTypes[i] = blockTypes[randomIndex];
            blockTypes[randomIndex] = temp;
        }

        // ���õ� Ÿ������ ���� ��� ��ġ
        for (int i = 0; i < currentState.Count; i++)
        {
            Vector2Int pos = currentState[i].position;
            // �ùķ��̼��� ���� ���� ���ӿ�����Ʈ ����
            GameObject dummyBlock = new GameObject($"SimBlock_{blockTypes[i]}");
            dummyBlock.tag = blockFactory.GetTagFromBlockType(blockTypes[i]);
            simulatedGrid[pos.x, pos.y] = dummyBlock;
        }

        return simulatedGrid;
    }

    // 5. ���� ��� ����
    private IEnumerator ApplyShuffleResult(GameObject[,] validResult)
    {
        // �ùķ��̼ǿ��� ã�� ��ȿ�� ����� ������ ����
        if (shuffleSystem != null)
        {
            yield return StartCoroutine(shuffleSystem.ExecuteShuffle(grid, width, height));
        }

        // �ùķ��̼ǿ� ���� ������Ʈ ����
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

        // ���� ��ϵ� ����
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

        // ȸ�� �ִϸ��̼�
        yield return StartCoroutine(RotateBlocksAnimation(blocks));

        // ��Ī ������ ���� ���� ã��
        bool foundValidConfiguration = false;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            List<int> testColors = GenerateColorConfiguration(blockCount);

            // �׽�Ʈ �������� ��Ī �������� Ȯ��
            if (CanMakeMatchWithColors(blockPositions, testColors))
            {
                // ��ȿ�� ���� ã�� - ����
                ApplyColorConfiguration(blockPositions, testColors);
                foundValidConfiguration = true;
                break;
            }
        }

        if (!foundValidConfiguration)
        {
            // ��Ī ������ ������ �� ã�� ��� - ���� ����
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

    // 7. ���� ���� ����
    private List<int> GenerateColorConfiguration(int blockCount)
    {
        List<int> colors = new List<int>();

        if (blockCount <= 3)
        {
            // 2~3���� �� - ��� ���� ��
            int color = Random.Range(1, 6);
            for (int i = 0; i < blockCount; i++)
            {
                colors.Add(color);
            }
        }
        else
        {
            // 4�� �̻��� �� - (��� ��/2)���� ������
            int colorCount = blockCount / 2;
            List<int> availableColors = new List<int>();

            for (int i = 0; i < colorCount; i++)
            {
                availableColors.Add(Random.Range(1, 6));
            }

            // �� ������ �ּ� 2���� ��ġ
            for (int i = 0; i < blockCount; i++)
            {
                colors.Add(availableColors[i % colorCount]);
            }

            // ����
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

    // 8. ���� �������� ��Ī �������� Ȯ��
    private bool CanMakeMatchWithColors(List<Vector2Int> positions, List<int> colors)
    {
        // �ӽ� �׸��忡 ���� ��ġ�Ͽ� �׽�Ʈ
        GameObject[,] testGrid = new GameObject[width, height];

        // �� ��� ����
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

        // �׽�Ʈ ���� ��ġ
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            GameObject dummyBlock = new GameObject($"TestBlock_{colors[i]}");
            dummyBlock.tag = blockFactory.GetTagFromBlockType(colors[i]);
            testGrid[pos.x, pos.y] = dummyBlock;
        }

        // ��Ī �������� Ȯ��
        bool canMatch = matchingSystem.HasAnyPossibleMatch(testGrid);

        // �׽�Ʈ�� ���� ������Ʈ ����
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

    // 9. ���� ���� ����
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

        // ������ ��� ã��
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

        // �ִϸ��̼� ȿ�� (���߿� �߰�)
        yield return new WaitForSeconds(0.5f);

        // ��� �ı�
        if (lastBlock != null)
        {
            blockFactory.DestroyBlock(lastBlock);
            grid[lastBlockPos.x, lastBlockPos.y] = blockFactory.CreateEmptyBlock(lastBlockPos.x, lastBlockPos.y);
        }

        // �¸� ó��
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

        // ȸ�� �ʱ�ȭ
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

        // 2�� �̻��� ������ �ϳ��� �ִ��� Ȯ��
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
        // ���� üũ �߰�
        if (grid == null || x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
        {
            Debug.LogWarning($"Invalid click position: ({x}, {y})");
            return;
        }

        Debug.Log($"Empty block clicked at ({x}, {y})");

        // ���Ѹ�� �ݹ� ó��
        if (onEmptyBlockClicked != null)
        {
            onEmptyBlockClicked(x, y);
            return;
        }

        // �Ϲ� ��Ī ó��
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

        // StageShuffleSystem ����ϵ��� ����
        StageShuffleSystem shuffleSystem = GetComponent<StageShuffleSystem>();
        if (shuffleSystem != null)
        {
            yield return StartCoroutine(shuffleSystem.ExecuteShuffle(grid, width, height));

            // ���� �� �ٽ� ��Ī �������� Ȯ��
            if (!matchingSystem.HasAnyPossibleMatch(grid))
            {
                Debug.Log("Still no matches after shuffle!");
                HandleDeadlockSituation(); // ��������� �ٽ� �õ�
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

            // ���� ��ϵ��� ��ġ ������Ʈ
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

        // ���� ��ϵ��� ��ġ ����
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

        // ī�޶� ��ġ ����
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
        // InfiniteModeManager�� �����ϴ��� Ȯ��
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
            // GridLayoutManager���� �ڵ����� ó���ǹǷ� ���� �۾� ���ʿ�
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

    // 3. ȣȯ���� ���� �߰� �޼����
    public void SetGridParent(Transform parent)
    {
        if (blockFactory != null)
        {
            blockFactory.SetGridParent(parent);
        }
    }

    // 4. cellSize ���� ������Ƽ
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

    // ����׿� ����� �׸���
    void OnDrawGizmos()
    {
        if (Application.isPlaying && grid != null && layoutManager != null)
        {
            // �׸��� ��� �׸��� (�����)
            Gizmos.color = Color.yellow;
            Rect bounds = layoutManager.GetGridBounds();
            Vector3 center = new Vector3(bounds.center.x, bounds.center.y, 0);
            Vector3 size = new Vector3(bounds.width, bounds.height, 0.1f);
            Gizmos.DrawWireCube(center, size);

            // �׸��� �߽��� �׸��� (������)
            Gizmos.color = Color.red;
            Vector3 gridCenter = layoutManager.GetGridCenter();
            Gizmos.DrawWireSphere(gridCenter, 0.3f);
        }
    }
}