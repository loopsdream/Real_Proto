using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1.0f;
    public Transform gridParent;

    [Header("Grid Centering")]
    public bool centerGrid = true; // �׸��带 �߾ӿ� ��ġ���� ����
    public Vector2 gridOffset = Vector2.zero; // �߰� ������

    [Header("Camera Settings")]
    public float cameraMargin = 1.5f; // �׸��� �ֺ� ����
    public float minCameraSize = 3f;   // �ּ� ī�޶� ũ��
    public float maxCameraSize = 10f;  // �ִ� ī�޶� ũ��

    [Header("UI Spacing")]
    public float topUISpace = 2f;    // ��� UI�� ���� ����
    public float bottomUISpace = 1f; // �ϴ� UI�� ���� ����

    [Header("Block Settings")]
    public GameObject[] blockPrefabs;
    public GameObject emptyBlockPrefab;

    [Header("Game Settings")]
    public int scorePerBlock = 10;
    public int targetScore = 100;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public GameObject winPanel;

    private GameObject[,] grid;
    private int currentScore = 0;
    private Vector2 gridCenterOffset; // �׸��� �߾� ������ ���� ������

    void Start()
    {
        grid = new GameObject[width, height];
        InitializeGrid();
        UpdateScoreText();
    }

    // �׸��� �߾� ������ ���
    void CalculateGridCenterOffset()
    {
        if (centerGrid)
        {
            // �׸����� ���� ũ�� ���
            float gridWorldWidth = (width - 1) * cellSize;
            float gridWorldHeight = (height - 1) * cellSize;

            // �߾� ������ ���� ������ ���
            gridCenterOffset = new Vector2(
                -gridWorldWidth * 0.5f,
                -gridWorldHeight * 0.5f
            );

            // �߰� ������ ����
            gridCenterOffset += gridOffset;
        }
        else
        {
            gridCenterOffset = gridOffset;
        }

        Debug.Log($"Grid center offset calculated: {gridCenterOffset}");
    }

    // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ
    Vector3 GridToWorldPosition(int x, int y)
    {
        Vector3 basePosition = new Vector3(x * cellSize, y * cellSize, 0);
        Vector3 centeredPosition = basePosition + (Vector3)gridCenterOffset;

        return centeredPosition;
    }

    void InitializeGrid()
    {
        CalculateGridCenterOffset();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);

                // 30% Ȯ���� �� ��� ����, 70% Ȯ���� ���� ��� ����
                if (Random.value < 0.3f)
                {
                    grid[x, y] = CreateBlock(emptyBlockPrefab, x, y);
                }
                else
                {
                    int randomBlockIndex = Random.Range(0, blockPrefabs.Length);
                    grid[x, y] = CreateBlock(blockPrefabs[randomBlockIndex], x, y);
                }
            }
        }

        // ī�޶� ��ġ �ڵ� ���� (���û���)
        AdjustCameraPosition();
    }

    // �������� �����͸� ����Ͽ� �׸��� �ʱ�ȭ
    public void InitializeStageGrid(StageData stageData)
    {
        // ���� �׸��� ����
        ClearGrid();

        // �� �׸��� ũ�� ����
        width = stageData.gridWidth;
        height = stageData.gridHeight;
        targetScore = stageData.targetScore;

        // �׸��� �߾� ���� ������ ����
        CalculateGridCenterOffset();

        // �׸��� �迭 �����
        grid = new GameObject[width, height];

        // �������� ���Ͽ� ���� ��� ����
        CreateBlocksFromPattern(stageData.blockPattern);

        // ���� UI ������Ʈ
        currentScore = 0;
        UpdateScoreText();

        // ī�޶� ��ġ �ڵ� ����
        AdjustCameraPosition();
    }

    // ���Ͽ� ���� ��� ����
    void CreateBlocksFromPattern(int[] pattern)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index < pattern.Length)
                {
                    int blockType = pattern[index];
                    GameObject blockToCreate = GetBlockPrefabByType(blockType);

                    if (blockToCreate != null)
                    {
                        grid[x, y] = CreateBlock(blockToCreate, x, y);
                    }
                }
            }
        }
    }

    // ��� Ÿ�Կ� ���� ������ ��ȯ
    GameObject GetBlockPrefabByType(int blockType)
    {
        if (blockType == 0)
            return emptyBlockPrefab;
        else if (blockType > 0 && blockType <= blockPrefabs.Length)
            return blockPrefabs[blockType - 1];
        else
            return null;
    }

    void ClearGrid()
    {
        if (grid != null)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != null)
                    {
                        Destroy(grid[x, y]);
                    }
                }
            }
        }
    }

    // ī�޶� ��ġ�� �׸��� �߾ӿ� �°� �ڵ� ����
    void AdjustCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // �׸����� �߾��� ���
            Vector3 gridCenter = GridToWorldPosition((int)(width / 2f), (int)(height / 2f));

            // UI ������ ����� ī�޶� ��ġ ����
            Vector3 cameraPositionOffset = new Vector3(
                0,
                (topUISpace - bottomUISpace) * 0.5f, // UI ���� ���̸�ŭ ����
                0
            );

            // ī�޶� ��ġ ���� (Z ��ǥ�� ����)
            Vector3 newCameraPosition = new Vector3(
                gridCenter.x + cameraPositionOffset.x,
                gridCenter.y + cameraPositionOffset.y,
                mainCamera.transform.position.z
            );

            mainCamera.transform.position = newCameraPosition;

            // ī�޶� ũ�� �ڵ� ���� (Orthographic�� ���)
            if (mainCamera.orthographic)
            {
                float gridWorldWidth = width * cellSize;
                float gridWorldHeight = height * cellSize;

                // UI ������ ����� ��� ������ ȭ�� ���� ���
                float availableHeight = gridWorldHeight + topUISpace + bottomUISpace;
                float availableWidth = gridWorldWidth;

                // ī�޶� ������ ����� �ʿ� ũ�� ���
                float requiredSizeForWidth = (availableWidth + cameraMargin * 2) * 0.5f;
                float requiredSizeForHeight = (availableHeight + cameraMargin * 2) * 0.5f;

                // �� ū ���� �����Ͽ� ��� ��Ұ� ȭ�鿡 �������� ��
                float requiredSize = Mathf.Max(requiredSizeForWidth, requiredSizeForHeight);

                // �ּ�/�ִ� ũ�� ���� ����
                requiredSize = Mathf.Clamp(requiredSize, minCameraSize, maxCameraSize);

                mainCamera.orthographicSize = requiredSize;
            }

            Debug.Log($"Camera adjusted to position: {newCameraPosition}");
            Debug.Log($"Camera size: {mainCamera.orthographicSize}");
            Debug.Log($"Grid size: {width}x{height}, UI spacing: top={topUISpace}, bottom={bottomUISpace}");
        }
    }

    GameObject CreateBlock(GameObject prefab, int x, int y)
    {
        // �߾� ���ĵ� ��ġ ���
        Vector3 position = GridToWorldPosition(x, y);

        GameObject newBlock = Instantiate(prefab, position, Quaternion.identity, gridParent);
        newBlock.name = $"Block_{x}_{y}";

        // ��Ͽ� ��ǥ ���� ����
        Block blockComponent = newBlock.GetComponent<Block>();
        if (blockComponent == null)
        {
            blockComponent = newBlock.AddComponent<Block>();
        }
        blockComponent.x = x;
        blockComponent.y = y;
        blockComponent.isEmpty = prefab == emptyBlockPrefab;

        // �� ��Ͽ��� Ŭ�� �̺�Ʈ �߰�
        if (blockComponent.isEmpty)
        {
            BlockInteraction interaction = newBlock.GetComponent<BlockInteraction>();
            if (interaction == null)
            {
                interaction = newBlock.AddComponent<BlockInteraction>();
            }
            interaction.gridManager = this;
        }

        return newBlock;
    }

    // UI ��ҵ��� �׸���� ��ġ�� �ʵ��� ���� ���� ���
    public Vector2 GetSafeAreaBounds()
    {
        // �׸��� ���� ���
        float gridWorldWidth = width * cellSize;
        float gridWorldHeight = height * cellSize;

        // ī�޶� ũ�� ���
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.orthographic)
        {
            float cameraHeight = mainCamera.orthographicSize * 2;
            float cameraWidth = cameraHeight * mainCamera.aspect;

            // UI�� ���� ���� ���� ���
            Vector2 safeArea = new Vector2(
                (cameraWidth - gridWorldWidth) * 0.5f,
                (cameraHeight - gridWorldHeight - topUISpace - bottomUISpace) * 0.5f
            );

            return safeArea;
        }

        return Vector2.zero;
    }

    // UI ��Ҹ� ������ ��ġ�� ��ġ�ϱ� ���� ����� �޼���
    public Vector3 GetUIPosition(UIPosition position)
    {
        Vector3 gridCenter = GridToWorldPosition((int)(width / 2f), (int)(height / 2f));
        float gridWorldHeight = height * cellSize;

        switch (position)
        {
            case UIPosition.TopCenter:
                return new Vector3(gridCenter.x, gridCenter.y + gridWorldHeight * 0.5f + topUISpace * 0.5f, 0);

            case UIPosition.BottomCenter:
                return new Vector3(gridCenter.x, gridCenter.y - gridWorldHeight * 0.5f - bottomUISpace * 0.5f, 0);

            case UIPosition.TopLeft:
                return new Vector3(gridCenter.x - width * cellSize * 0.5f, gridCenter.y + gridWorldHeight * 0.5f + topUISpace * 0.5f, 0);

            case UIPosition.TopRight:
                return new Vector3(gridCenter.x + width * cellSize * 0.5f, gridCenter.y + gridWorldHeight * 0.5f + topUISpace * 0.5f, 0);

            case UIPosition.BottomLeft:
                return new Vector3(gridCenter.x - width * cellSize * 0.5f, gridCenter.y - gridWorldHeight * 0.5f - bottomUISpace * 0.5f, 0);

            case UIPosition.BottomRight:
                return new Vector3(gridCenter.x + width * cellSize * 0.5f, gridCenter.y - gridWorldHeight * 0.5f - bottomUISpace * 0.5f, 0);

            default:
                return gridCenter;
        }
    }

    // �¸� ���� üũ �� �������� �Ŵ������� �˸�
    void CheckWinCondition()
    {
        if (currentScore >= targetScore)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageComplete();
            }

            //if (winPanel != null)
            //{
            //    winPanel.SetActive(true);
            //}

            Debug.Log("�������� Ŭ����!");
        }
    }

    public void OnEmptyBlockClicked(int x, int y)
    {
        Debug.Log($"Empty block clicked at ({x}, {y})");

        List<GameObject> matchedBlocks = FindMatchingBlocksInFourDirections(x, y);

        Debug.Log($"Found {matchedBlocks.Count} matching blocks");

        if (matchedBlocks.Count > 0)
        {
            Debug.Log($"Proceeding to destroy {matchedBlocks.Count} blocks");
            DestroyMatchedBlocks(matchedBlocks);
            AddScore(matchedBlocks.Count);

            // �������� �Ŵ������� �̵� �˸�
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnBlocksDestroyed();
            }

            CheckWinCondition();
        }
        else
        {
            Debug.Log("No blocks to destroy");
        }
    }

    List<GameObject> FindMatchingBlocksInFourDirections(int x, int y)
    {
        List<GameObject> allMatchedBlocks = new List<GameObject>();
        Dictionary<string, List<GameObject>> blocksByType = new Dictionary<string, List<GameObject>>();

        Vector2Int[] directions =
        {
            new Vector2Int(0, 1),  // ��
            new Vector2Int(0, -1), // ��
            new Vector2Int(-1, 0), // ��
            new Vector2Int(1, 0)   // ��
        };

        foreach (Vector2Int dir in directions)
        {
            GameObject foundBlock = FindFirstBlockInDirection(x, y, dir.x, dir.y);

            if (foundBlock != null)
            {
                Block blockComponent = foundBlock.GetComponent<Block>();

                if (!blockComponent.isEmpty)
                {
                    string blockType = foundBlock.tag;

                    if (!blocksByType.ContainsKey(blockType))
                    {
                        blocksByType[blockType] = new List<GameObject>();
                    }

                    blocksByType[blockType].Add(foundBlock);
                    Debug.Log($"Found block of type {blockType} at direction ({dir.x}, {dir.y})");
                }
            }
        }

        foreach (var entry in blocksByType)
        {
            string blockType = entry.Key;
            List<GameObject> blocks = entry.Value;

            Debug.Log($"Block type {blockType} has {blocks.Count} matches");

            if (blocks.Count >= 2)
            {
                Debug.Log($"Adding {blocks.Count} blocks of type {blockType} to matched blocks");
                allMatchedBlocks.AddRange(blocks);
            }
        }

        Debug.Log($"Total matched blocks: {allMatchedBlocks.Count}");
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

            if (currX < 0 || currX >= width || currY < 0 || currY >= height)
            {
                return null;
            }

            if (grid[currX, currY] == null)
            {
                continue;
            }

            Block blockComponent = grid[currX, currY].GetComponent<Block>();

            if (!blockComponent.isEmpty)
            {
                return grid[currX, currY];
            }
        }
    }

    void DestroyMatchedBlocks(List<GameObject> blocks)
    {
        if (blocks.Count == 0)
        {
            Debug.LogWarning("Attempting to destroy empty block list");
            return;
        }

        Debug.Log($"Destroying {blocks.Count} matched blocks");

        foreach (GameObject block in blocks)
        {
            if (block == null)
            {
                Debug.LogWarning("Null block in matched blocks list");
                continue;
            }

            Block blockComponent = block.GetComponent<Block>();
            if (blockComponent == null)
            {
                Debug.LogWarning("Block component missing");
                continue;
            }

            int x = blockComponent.x;
            int y = blockComponent.y;

            Debug.Log($"Destroying block at ({x}, {y})");

            if (grid[x, y] == null)
            {
                Debug.LogWarning($"Grid position ({x}, {y}) is already null");
                continue;
            }

            Destroy(block);
            grid[x, y] = CreateBlock(emptyBlockPrefab, x, y);

            if (grid[x, y] == null)
            {
                Debug.LogError($"Failed to create empty block at ({x}, {y})");
            }
            else
            {
                Debug.Log($"Replaced with empty block at ({x}, {y})");
            }
        }
    }

    void AddScore(int blockCount)
    {
        currentScore += blockCount * scorePerBlock;
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore} / {targetScore}";
        }
        else
        {
            Debug.LogWarning("ScoreText is not assigned!");
        }
    }

    // ������� ���� �׸��� �ð�ȭ (Scene �信���� ����)
    void OnDrawGizmos()
    {
        if (grid == null) return;

        Gizmos.color = Color.yellow;

        // �׸��� ��輱 �׸���
        Vector3 bottomLeft = GridToWorldPosition(0, 0) + Vector3.one * (-cellSize * 0.5f);
        Vector3 topRight = GridToWorldPosition(width - 1, height - 1) + Vector3.one * (cellSize * 0.5f);

        // �׸��� �ܰ���
        Gizmos.DrawLine(bottomLeft, new Vector3(topRight.x, bottomLeft.y, 0));
        Gizmos.DrawLine(new Vector3(topRight.x, bottomLeft.y, 0), topRight);
        Gizmos.DrawLine(topRight, new Vector3(bottomLeft.x, topRight.y, 0));
        Gizmos.DrawLine(new Vector3(bottomLeft.x, topRight.y, 0), bottomLeft);

        // �׸��� �߾��� ǥ��
        Vector3 centerPoint = GridToWorldPosition((int)(width / 2f), (int)(height / 2f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centerPoint, 0.2f);
    }

    // UI ��ġ�� �����ϱ� ���� ������
    public enum UIPosition
    {
        TopCenter,
        TopLeft,
        TopRight,
        BottomCenter,
        BottomLeft,
        BottomRight
    }
}