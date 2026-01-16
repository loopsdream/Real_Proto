using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StageGridManager : BaseGridManager
{
    [Header("Stage Settings")]
    public int scorePerBlock = 10;

    [Header("Stage UI")]
    public TextMeshProUGUI scoreText;
    public GameObject winPanel;

    [Header("Stage Systems")]
    public StageShuffleSystem shuffleSystem;
    public CollectibleFactory collectibleFactory;
    public ClearGoalUI clearGoalUI;

    [Header("Stage State")]
    public int currentScore = 0;
    private int shuffleAttemptCount = 0;
    private const int MAX_SHUFFLE_ATTEMPTS = 3;
    private bool isCheckingWinCondition = false;

    [Header("Stage Header UI")]
    public StageHeaderUI stageHeaderUI;

    private GameObject[,] collectibleGrid;

    private Dictionary<int, int> collectedColorBlocks = new Dictionary<int, int>();  // color -> count
    private Dictionary<CollectibleType, int> collectedCollectibles = new Dictionary<CollectibleType, int>();  // type -> count
    private List<ClearGoalData> currentClearGoals = new List<ClearGoalData>();
    
    private int currentStageNumber = 0;
    private int targetScore = 0;
    private int itemsUsed = 0;

    private int maxTaps = 0;
    private int remainingTaps = 0;

    private Vector2Int lastClickPosition;

    protected override void Awake()
    {
        base.Awake();

        if (shuffleSystem == null)
            shuffleSystem = GetComponent<StageShuffleSystem>();

        if (collectibleFactory == null)
            collectibleFactory = GetComponent<CollectibleFactory>();

        Debug.Log($"[StageGridManager] Awake called on {gameObject.name}");
    }

    void OnEnable()
    {
        Debug.Log($"[StageGridManager] OnEnable called on {gameObject.name}");
    }

    void Start()
    {
        UpdateScoreText();
    }

    public void InitializeStageGrid(StageData stageData)
    {
        Debug.Log($"[InitializeStageGrid] Starting with stage: {stageData.stageName}");

        if (stageData == null)
        {
            Debug.LogError("[InitializeStageGrid] StageData is null!");
            return;
        }

        width = stageData.gridWidth;
        height = stageData.gridHeight;

        ClearGrid();
        Debug.Log("[InitializeStageGrid] Grid cleared");

        SetupGrid();
        Debug.Log($"[InitializeStageGrid] SetupGrid() called - grid array: {(grid != null ? $"{grid.GetLength(0)}x{grid.GetLength(1)}" : "NULL")}");

        CreateBlocksFromPattern(stageData.blockPattern);
        Debug.Log("[InitializeStageGrid] Pattern initialized");

        int blockCount = 0;
        if (gridParent != null)
        {
            Debug.Log("[InitializeStageGrid] gridParent != null");
            blockCount = gridParent.childCount;
        }
        Debug.Log($"[InitializeStageGrid] Total blocks in scene: {blockCount}");

        SetupCameraAndLayout();

        currentScore = 0;
        UpdateScoreText();

        currentStageNumber = stageData.stageNumber;
        targetScore = stageData.targetScore;
        itemsUsed = 0;

        // Add new code
        maxTaps = stageData.maxTaps > 0 ? stageData.maxTaps : 50;
        remainingTaps = maxTaps;

        CreateCollectiblesFromPattern(stageData.collectiblePattern);

        InitializeClearGoals(stageData.clearGoals);

        if (clearGoalUI != null)
        {
            clearGoalUI.InitializeGoals(stageData.clearGoals);
        }

        if (stageHeaderUI != null)
        {
            stageHeaderUI.Initialize(stageData.clearGoals, maxTaps);
        }

        Debug.Log("[InitializeStageGrid] Completed");
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

    private void CreateCollectiblesFromPattern(int[] pattern)
    {
        if (pattern == null || collectibleFactory == null)
        {
            Debug.Log("No collectible pattern or factory not assigned");
            return;
        }

        // Initialize collectible grid
        collectibleGrid = new GameObject[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index < pattern.Length)
                {
                    int collectibleType = pattern[index];
                    if (collectibleType > 0)  // 0 = None
                    {
                        collectibleGrid[x, y] = collectibleFactory.CreateCollectibleFromType(collectibleType, x, y);
                    }
                }
            }
        }

        Debug.Log($"Created collectibles from pattern");
    }

    private void InitializeClearGoals(List<ClearGoalData> goals)
    {
        currentClearGoals = goals != null ? new List<ClearGoalData>(goals) : new List<ClearGoalData>();

        collectedColorBlocks.Clear();
        collectedCollectibles.Clear();

        Debug.Log($"Initialized {currentClearGoals.Count} clear goals");
    }

    private void CollectCollectiblesOnMatchingLines(List<GameObject> matchedBlocks)
    {
        if (matchedBlocks == null || matchedBlocks.Count == 0) return;
        if (collectibleGrid == null) return;

        // Use the saved click position
        Vector2Int startPos = lastClickPosition;

        Debug.Log($"[Collectibles] Click position: ({startPos.x}, {startPos.y})");

        // Create a set of matched block positions to exclude
        HashSet<Vector2Int> matchedPositions = new HashSet<Vector2Int>();
        foreach (GameObject matchedBlock in matchedBlocks)
        {
            if (matchedBlock == null) continue;
            Block blockComp = matchedBlock.GetComponent<Block>();
            if (blockComp != null)
            {
                matchedPositions.Add(new Vector2Int(blockComp.x, blockComp.y));
                Debug.Log($"[Collectibles] Matched block at: ({blockComp.x}, {blockComp.y})");
            }
        }

        // Add this: Track already checked positions to avoid duplicates
        HashSet<Vector2Int> checkedPositions = new HashSet<Vector2Int>();

        // Collect collectibles on each line from start to matched block
        foreach (GameObject matchedBlock in matchedBlocks)
        {
            if (matchedBlock == null) continue;

            Block blockComp = matchedBlock.GetComponent<Block>();
            if (blockComp == null) continue;

            Vector2Int endPos = new Vector2Int(blockComp.x, blockComp.y);
            List<Vector2Int> linePositions = GetLinePositions(startPos, endPos);

            Debug.Log($"[Collectibles] Line from ({startPos.x},{startPos.y}) to ({endPos.x},{endPos.y}) has {linePositions.Count} positions");

            foreach (Vector2Int pos in linePositions)
            {
                // Add this: Skip if already checked
                if (checkedPositions.Contains(pos))
                {
                    Debug.Log($"[Collectibles] Already checked position ({pos.x}, {pos.y}), skipping");
                    continue;
                }

                Debug.Log($"[Collectibles] Checking position ({pos.x}, {pos.y})");

                // Skip if position is a matched block position
                if (matchedPositions.Contains(pos))
                {
                    Debug.Log($"[Collectibles] Skipping matched block position ({pos.x}, {pos.y})");
                    continue;
                }

                // Add this: Mark as checked
                checkedPositions.Add(pos);

                CollectCollectibleAt(pos.x, pos.y);
            }
        }

        // After updating collectedCollectibles dictionary
        UpdateHeaderGoalProgress();
    }

    // NEW: Get all positions on a line between two points
    private List<Vector2Int> GetLinePositions(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        int dx = end.x - start.x;
        int dy = end.y - start.y;

        // Check if it's horizontal, vertical, or invalid
        if (dx != 0 && dy != 0)
        {
            // Not a straight line - should not happen in this game
            Debug.LogWarning($"Invalid line from ({start.x},{start.y}) to ({end.x},{end.y}) - not horizontal or vertical");
            return positions;
        }

        if (dx == 0 && dy == 0)
        {
            // Same position
            return positions;
        }

        // Horizontal line (same y)
        if (dy == 0)
        {
            int startX = Mathf.Min(start.x, end.x);
            int endX = Mathf.Max(start.x, end.x);

            // Start from startX (include click position), end before endX (exclude matched block)
            for (int x = startX; x < endX; x++)
            {
                positions.Add(new Vector2Int(x, start.y));
            }
        }
        // Vertical line (same x)
        else if (dx == 0)
        {
            int startY = Mathf.Min(start.y, end.y);
            int endY = Mathf.Max(start.y, end.y);

            // Start from startY (include click position), end before endY (exclude matched block)
            for (int y = startY; y < endY; y++)
            {
                positions.Add(new Vector2Int(start.x, y));
            }
        }

        return positions;
    }

    // NEW: Collect collectible at position
    private void CollectCollectibleAt(int x, int y)
    {
        if (collectibleGrid == null) return;
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        GameObject collectibleObj = collectibleGrid[x, y];
        if (collectibleObj == null) return;

        Collectible collectible = collectibleObj.GetComponent<Collectible>();
        if (collectible == null) return;

        // Collect the collectible
        collectible.Collect();

        // Track for clear goals
        if (collectedCollectibles.ContainsKey(collectible.collectibleType))
            collectedCollectibles[collectible.collectibleType]++;
        else
            collectedCollectibles[collectible.collectibleType] = 1;

        UpdateGoalUI();

        Debug.Log($"Collected {collectible.collectibleType} at ({x},{y}). Total: {collectedCollectibles[collectible.collectibleType]}");
    }

    // NEW: Track destroyed color blocks for clear goals
    private void TrackDestroyedColorBlocks(List<GameObject> blocks)
    {
        if (blocks == null) return;

        foreach (GameObject block in blocks)
        {
            if (block == null) continue;

            int blockType = blockFactory.GetBlockTypeFromTag(block.tag);
            if (blockType > 0)  // Not empty
            {
                if (collectedColorBlocks.ContainsKey(blockType))
                    collectedColorBlocks[blockType]++;
                else
                    collectedColorBlocks[blockType] = 1;
            }
        }

        UpdateGoalUI();
        UpdateHeaderGoalProgress();
    }

    // NEW: Check if all clear goals are completed
    private bool AreClearGoalsCompleted()
    {
        if (currentClearGoals == null || currentClearGoals.Count == 0)
        {
            // No goals defined, fallback to destroy all blocks
            return CountRemainingBlocks() == 0;
        }

        foreach (ClearGoalData goal in currentClearGoals)
        {
            switch (goal.goalType)
            {
                case ClearGoalType.DestroyAllBlocks:
                    if (CountRemainingBlocks() > 0)
                        return false;
                    break;

                case ClearGoalType.CollectColorBlocks:
                    int collectedCount = collectedColorBlocks.ContainsKey(goal.targetColor)
                        ? collectedColorBlocks[goal.targetColor] : 0;
                    if (collectedCount < goal.targetColorCount)
                        return false;
                    break;

                case ClearGoalType.CollectCollectibles:
                    int collectibleCount = collectedCollectibles.ContainsKey(goal.collectibleType)
                        ? collectedCollectibles[goal.collectibleType] : 0;
                    if (collectibleCount < goal.targetCollectibleCount)
                        return false;
                    break;
            }
        }

        return true;
    }

    // NEW: Get current clear goal progress (for UI)
    public string GetClearGoalProgress()
    {
        if (currentClearGoals == null || currentClearGoals.Count == 0)
            return $"Blocks: {CountRemainingBlocks()}";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (ClearGoalData goal in currentClearGoals)
        {
            switch (goal.goalType)
            {
                case ClearGoalType.DestroyAllBlocks:
                    sb.Append($"All Blocks ");
                    break;

                case ClearGoalType.CollectColorBlocks:
                    int collectedCount = collectedColorBlocks.ContainsKey(goal.targetColor)
                        ? collectedColorBlocks[goal.targetColor] : 0;
                    string colorName = GetColorNameFromType(goal.targetColor);
                    sb.Append($"{colorName} {collectedCount}/{goal.targetColorCount} ");
                    break;

                case ClearGoalType.CollectCollectibles:
                    int collectibleCount = collectedCollectibles.ContainsKey(goal.collectibleType)
                        ? collectedCollectibles[goal.collectibleType] : 0;
                    sb.Append($"{goal.collectibleType} {collectibleCount}/{goal.targetCollectibleCount} ");
                    break;
            }
        }

        return sb.ToString().Trim();
    }

    private void UpdateGoalUI()
    {
        if (clearGoalUI != null)
        {
            clearGoalUI.UpdateGoalProgress(collectedColorBlocks, collectedCollectibles);
        }
    }

    // Helper method for color names
    private string GetColorNameFromType(int colorType)
    {
        switch (colorType)
        {
            case 1: return "Red";
            case 2: return "Blue";
            case 3: return "Yellow";
            case 4: return "Green";
            case 5: return "Purple";
            case 6: return "Pink";
            default: return "Unknown";
        }
    }

    public override void OnEmptyBlockClicked(int x, int y)
    {
        Debug.Log($"[StageGridManager] OnEmptyBlockClicked({x}, {y}) called");

        if (grid == null)
        {
            Debug.LogError("[StageGridManager] grid array is NULL!");
            return;
        }

        Debug.Log($"[StageGridManager] grid size: {grid.GetLength(0)}x{grid.GetLength(1)}");
        Debug.Log($"[StageGridManager] Looking at grid[{x}, {y}]");

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

        // After validation checks, before FindMatchingBlocks call
        Debug.Log($"[StageGridManager] Calling matchingSystem.FindMatchingBlocks({x}, {y})");

        lastClickPosition = new Vector2Int(x, y);

        if (maxTaps > 0)
        {
            remainingTaps--;
            UpdateHeaderTapCount();

            if (remainingTaps <= 0)
            {
                Debug.Log("No more taps remaining!");
                // Handle game over - you might want to show a game over panel
                return;
            }
        }

        List<GameObject> matchedBlocks = matchingSystem.FindMatchingBlocks(x, y, grid);

        Debug.Log($"[StageGridManager] Found {matchedBlocks.Count} matches");

        if (matchedBlocks.Count > 0)
        {
            Debug.Log("[StageGridManager] Destroying matched blocks...");
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

        CollectCollectiblesOnMatchingLines(matchedBlocks);

        TrackDestroyedColorBlocks(matchedBlocks);

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

        // Check if clear goals are completed
        if (AreClearGoalsCompleted())
        {
            Debug.Log("Clear goals completed!");
            CalculateAndGrantReward();

            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnStageCleared();
            }

            isCheckingWinCondition = false;
            return;
        }

        // If goals are NOT completed, check if we can still continue
        int remainingBlocks = CountRemainingBlocks();

        // IMPORTANT: Only clear if NO goals are defined (fallback behavior)
        // If goals ARE defined but not completed, do NOT clear even if blocks are gone
        if (remainingBlocks == 0 && (currentClearGoals == null || currentClearGoals.Count == 0))
        {
            // No specific goals defined, fallback to destroy all blocks
            Debug.Log("No goals defined - clearing stage (fallback)");
            CalculateAndGrantReward();

            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnStageCleared();
            }
        }
        else if (remainingBlocks == 0 && currentClearGoals != null && currentClearGoals.Count > 0)
        {
            // Blocks are gone but goals not completed - GAME OVER
            Debug.Log("All blocks destroyed but goals not completed - Game Over!");
            
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnStageFailed("Goals not completed!");
            }
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

    private IEnumerator HandleDeadlockFlow()
    {
        Debug.Log("=== Starting Deadlock Flow ===");

        int remainingBlocks = CountRemainingBlocks();
        Debug.Log($"Remaining blocks: {remainingBlocks}");

        if (remainingBlocks == 0)
        {
            CheckWinCondition();
            yield break;
        }
        else if (remainingBlocks == 1)
        {
            yield return StartCoroutine(AutoDestroyLastBlock());
            yield break;
        }

        List<ShuffleBlockData> currentState = SaveCurrentState();
        if (AreAllBlocksInLine(currentState))
        {
            Debug.Log("All blocks in line - Game Over!");
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                stageManager.OnStageFailed("����� �Ϸķ� ��ġ�Ǿ� �� �̻� ������ �� �����ϴ�!");
            }
            yield break;
        }

        if (remainingBlocks <= 3)
        {
            yield return StartCoroutine(TransformToSameColor(currentState));
            yield break;
        }

        bool canSolveWithShuffle = CheckIfShuffleCanSolve(currentState);

        if (canSolveWithShuffle)
        {
            yield return StartCoroutine(ShuffleRemainingBlocks());
            yield break;
        }
        else
        {
            yield return StartCoroutine(TransformToHalfColors(currentState));

            if (!CanMakeAnyMatch())
            {
                yield return StartCoroutine(HandleDeadlockFlow());
            }
        }
    }

    private IEnumerator TransformToSameColor(List<ShuffleBlockData> blocks)
    {
        Debug.Log($"Transforming {blocks.Count} blocks to same color");

        List<GameObject> blockObjects = new List<GameObject>();
        foreach (var data in blocks)
        {
            blockObjects.Add(data.originalBlock);
        }

        yield return StartCoroutine(RotateBlocksAnimation(blockObjects));

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

        yield return StartCoroutine(RotateBlocksAnimation(blockObjects));

        List<int> colorTypes = new List<int>();
        for (int i = 0; i < colorTypeCount; i++)
        {
            colorTypes.Add(Random.Range(1, 6));
        }

        List<int> finalColors = new List<int>();
        for (int i = 0; i < blockCount; i++)
        {
            finalColors.Add(colorTypes[i % colorTypeCount]);
        }

        for (int i = finalColors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = finalColors[i];
            finalColors[i] = finalColors[j];
            finalColors[j] = temp;
        }

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

    private bool CheckIfShuffleCanSolve(List<ShuffleBlockData> currentState)
    {
        Debug.Log("Checking if shuffle can solve...");

        if (currentState.Count >= 6)
        {
            return true;
        }

        List<Vector2Int> positions = new List<Vector2Int>();
        List<int> blockTypes = new List<int>();

        foreach (var data in currentState)
        {
            positions.Add(data.position);
            blockTypes.Add(data.blockType);
        }

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

    private bool AreAllBlocksInLine(List<ShuffleBlockData> blocks)
    {
        if (blocks.Count <= 1) return true;

        List<Vector2Int> positions = new List<Vector2Int>();
        foreach (var block in blocks)
        {
            positions.Add(block.position);
        }

        positions.Sort((a, b) => {
            int xCompare = a.x.CompareTo(b.x);
            return xCompare != 0 ? xCompare : a.y.CompareTo(b.y);
        });

        bool isHorizontalLine = true;
        int firstY = positions[0].y;
        for (int i = 1; i < positions.Count; i++)
        {
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

        positions.Sort((a, b) => {
            int yCompare = a.y.CompareTo(b.y);
            return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
        });

        bool isVerticalLine = true;
        int firstX = positions[0].x;
        for (int i = 1; i < positions.Count; i++)
        {
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

    private bool CheckIfPermutationHasMatch(List<Vector2Int> positions, List<int> blockTypes)
    {
        GameObject[,] testGrid = new GameObject[width, height];

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

        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            GameObject dummyBlock = new GameObject($"TestBlock_{blockTypes[i]}");
            dummyBlock.tag = blockFactory.GetTagFromBlockType(blockTypes[i]);

            Block blockComp = dummyBlock.AddComponent<Block>();
            blockComp.x = pos.x;
            blockComp.y = pos.y;
            blockComp.isEmpty = false;

            testGrid[pos.x, pos.y] = dummyBlock;
        }

        bool canMatch = matchingSystem.HasAnyPossibleMatch(testGrid);

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

        yield return new WaitForSeconds(0.5f);

        if (lastBlock != null)
        {
            blockFactory.DestroyBlock(lastBlock);
            grid[lastBlockPos.x, lastBlockPos.y] = blockFactory.CreateEmptyBlock(lastBlockPos.x, lastBlockPos.y);
        }

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

        StageShuffleSystem shuffleSystem = GetComponent<StageShuffleSystem>();
        if (shuffleSystem != null)
        {
            yield return StartCoroutine(shuffleSystem.ExecuteShuffle(grid, width, height));

            if (!matchingSystem.HasAnyPossibleMatch(grid))
            {
                Debug.Log("Still no matches after shuffle!");
                HandleDeadlockSituation();
            }
        }
        else
        {
            Debug.LogError("StageShuffleSystem not found!");
        }
    }

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

    public void InitializeGrid()
    {
        SetupGrid();
        CreateRandomBlocks();
        SetupCameraAndLayout();

        currentScore = 0;
        UpdateScoreText();
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

    private void UpdateHeaderTapCount()
    {
        if (stageHeaderUI != null)
        {
            stageHeaderUI.UpdateTapCount(remainingTaps);
        }
    }

    private void UpdateHeaderGoalProgress()
    {
        if (stageHeaderUI != null)
        {
            stageHeaderUI.UpdateGoalProgress(collectedColorBlocks, collectedCollectibles);
        }
    }

    #region Item System Support Methods

    public int GetGridWidth()
    {
        return width;
    }

    public int GetGridHeight()
    {
        return height;
    }

    public Block GetBlockComponentAt(int x, int y)
    {
        GameObject blockObj = GetBlockAt(x, y);
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
        if (blockComponent == null || blockComponent.isEmpty)
            return;

        Debug.Log("Destroying block at position: " + x + ", " + y);

        if (blockFactory != null)
        {
            blockFactory.DestroyBlock(targetBlock);
            grid[x, y] = blockFactory.CreateEmptyBlock(x, y);
        }
        else
        {
            Destroy(targetBlock);
            grid[x, y] = null;
        }

        StartCoroutine(CheckMatchesAfterItemUse(0.2f));
    }

    public Vector3 GetWorldPositionFromGrid(int x, int y)
    {
        return GridToWorldPosition(x, y);
    }

    public void AddScoreFromItem(int points)
    {
        AddScore(points);
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

        if (matchingSystem != null)
        {
            bool hasMatches = matchingSystem.HasAnyPossibleMatch(grid);
            Debug.Log("Has possible matches after item use: " + hasMatches);

            if (!hasMatches && CountRemainingBlocks() > 0)
            {
                HandleDeadlockSituation();
            }
        }
    }

    public void OnItemUsed()
    {
        itemsUsed++;
        Debug.Log($"Item used. Total items used: {itemsUsed}");
    }

    private void CalculateAndGrantReward()
    {
        Debug.Log("=== Calculating Stage Clear Rewards ===");

        int stars = StageRewardCalculator.CalculateStars(currentScore, targetScore);
        Debug.Log($"Stars earned: {stars} (Score: {currentScore}/{targetScore})");

        bool isFirstClear = StageRewardCalculator.IsFirstClear(currentStageNumber);
        Debug.Log($"First clear: {isFirstClear}");

        bool isPerfectClear = StageRewardCalculator.IsPerfectClear(stars, itemsUsed, 0);
        Debug.Log($"Perfect clear: {isPerfectClear}");

        UserDataManager userDataManager = UserDataManager.Instance;
        if (userDataManager != null)
        {
            userDataManager.SetStageCleared(currentStageNumber, stars, currentScore);
        }

        StageRewardData rewardData = StageRewardCalculator.LoadStageRewardData(currentStageNumber);

        if (rewardData != null && rewardData.IsValid())
        {
            List<RewardItem> totalRewards = rewardData.CalculateTotalRewards(stars, isFirstClear, isPerfectClear);

            Debug.Log($"Total rewards count: {totalRewards.Count}");

            RewardManager rewardManager = RewardManager.Instance;
            if (rewardManager != null)
            {
                rewardManager.GrantRewards(totalRewards);
                Debug.Log("Rewards granted successfully!");
            }
            else
            {
                Debug.LogError("RewardManager not found!");
            }
        }
        else
        {
            Debug.LogWarning($"No reward data found for stage {currentStageNumber}");
        }

        Debug.Log("=== Reward Calculation Complete ===");
    }

    #endregion
}