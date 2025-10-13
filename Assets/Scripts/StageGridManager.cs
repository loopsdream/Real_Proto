using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StageGridManager : BaseGridManager
{
    // �̱��� �ν��Ͻ�
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
        // �̱��� ����
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
        // �������� ���� StageManager�� �ʱ�ȭ�� ���
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

        // grid �迭 ���� Ȯ��
        if (grid == null)
        {
            Debug.LogError("[StageGridManager] grid array is NULL!");
            return;
        }

        Debug.Log($"[StageGridManager] grid size: {grid.GetLength(0)}x{grid.GetLength(1)}");
        Debug.Log($"[StageGridManager] Looking at grid[{x}, {y}]");

        // �ش� ��ġ�� �ֺ� ��� Ȯ��
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

        // matchingSystem üũ
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
            // ��� �ı� ����
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

    // ��� ����� ó�� ������...
    // (���� GridManagerRefactored�� �������� ���� ������)
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

        // ȸ�� �ʱ�ȭ
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

    // cellSize ������Ƽ �߰�
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

    // InitializeGridWithPattern �޼��� �߰�
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

    // CreateBlocksFromPattern2D �޼��� �߰� (2D �迭��)
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

    // InitializeGrid �޼��� �߰� (�׽�Ʈ��)
    public void InitializeGrid()
    {
        SetupGrid();
        CreateRandomBlocks();
        SetupCameraAndLayout();

        currentScore = 0;
        UpdateScoreText();
    }

    // CreateRandomBlocks �޼��� �߰�
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

    // Grid dimension methods - ���� width, height ���� ���
    public int GetGridWidth()
    {
        return width;
    }

    public int GetGridHeight()
    {
        return height;
    }

    // Block access methods - BaseGridManager�� ���� �޼����� ȣȯ
    public Block GetBlockComponentAt(int x, int y)
    {
        GameObject blockObj = GetBlockAt(x, y); // BaseGridManager�� ���� �޼��� ���
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
        if (blockComponent == null || blockComponent.isEmpty) // �� ����� �ı����� ����
            return;

        Debug.Log("Destroying block at position: " + x + ", " + y);

        // BlockFactory�� ����Ͽ� ��� �ı�
        if (blockFactory != null)
        {
            blockFactory.DestroyBlock(targetBlock);
            // �� ������� ��ü
            grid[x, y] = blockFactory.CreateEmptyBlock(x, y);
        }
        else
        {
            // Fallback: ���� �ı�
            Destroy(targetBlock);
            grid[x, y] = null;
        }

        // ������ ��� �� ��ġ üũ
        StartCoroutine(CheckMatchesAfterItemUse(0.2f));
    }

    // World position conversion - BaseGridManager�� GridToWorldPosition ���
    public Vector3 GetWorldPositionFromGrid(int x, int y)
    {
        return GridToWorldPosition(x, y); // BaseGridManager�� ���� �޼��� ���
    }

    // Score management - ���� AddScore �޼��� �����ε�
    public void AddScoreFromItem(int points)
    {
        AddScore(points); // ���� private AddScore �޼��� ȣ��
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

        // ���� ��Ī �ý��� ���
        if (matchingSystem != null)
        {
            // ��ġ ������ ����� �ִ��� Ȯ��
            bool hasMatches = matchingSystem.HasAnyPossibleMatch(grid);
            Debug.Log("Has possible matches after item use: " + hasMatches);

            // �ʿ��ϴٸ� ����� ��Ȳ ó��
            if (!hasMatches && CountRemainingBlocks() > 0)
            {
                HandleDeadlockSituation(); // ���� �޼��� ȣ��
            }
        }
    }

    #endregion
}