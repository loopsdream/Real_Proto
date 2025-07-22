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
    public int targetScore = 100;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public GameObject winPanel;

    [Header("Components")]
    public CameraController cameraController;
    public GridLayoutManager layoutManager;
    public BlockFactory blockFactory;
    public MatchingSystem matchingSystem;

    public int currentScore = 0;
    private GameObject[,] grid;

    void Awake()
    {
        InitializeComponents();
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

        ClearGrid();
        SetupGridFromStageData(stageData);
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
        targetScore = stageData.targetScore;

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
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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
            scoreText.text = $"Score: {currentScore} / {targetScore}";
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