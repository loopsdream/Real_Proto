/*

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    [System.NonSerialized]
    public System.Action<int, int> onEmptyBlockClicked; // ���Ѹ�� �ݹ�

    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1.0f;
    public Transform gridParent;

    [Header("Grid Centering")]
    public bool centerGrid = true; // �׸��带 �߾ӿ� ��ġ���� ����
    public Vector2 gridOffset = Vector2.zero; // �߰� ������

    [Header("Portrait Mode Settings")]
    public bool portraitMode = true;
    public float topUISpacePixels = 200f;    // ��� UI �� ��ġ ���� (�ȼ�)
    public float bottomUISpacePixels = 150f; // �ϴ� UI ���� (�ȼ�)
    public float sideMarginPixels = 100f;     // �¿� ���� (�ȼ�)

    [Header("Camera Settings")]
    public float cameraMarginPercent = 0.1f; // �׸��� �ֺ� ����
    public float minCameraSize = 3f;   // �ּ� ī�޶� ũ��
    public float maxCameraSize = 15f;  // �ִ� ī�޶� ũ��

    [Header("Block Settings")]
    public GameObject[] blockPrefabs;
    public GameObject emptyBlockPrefab;

    [Header("Game Settings")]
    public int scorePerBlock = 10;
    public int targetScore = 100;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public GameObject winPanel;

    public int currentScore = 0;
    private GameObject[,] grid;
    private Vector2 gridCenterOffset; // �׸��� �߾� ������ ���� ������

    // ���θ�� ���� ���� ����
    private float pixelsToWorldUnit = 1f;
    private float screenAspectRatio = 1f;

    void Start()
    {
        // ���Ѹ�忡���� �ڵ� �ʱ�ȭ ��ŵ
        if (IsInfiniteMode())
        {
            Debug.Log("Infinite mode detected, skipping auto initialization");
            return;
        }

        grid = new GameObject[width, height];
        InitializeGrid();

        UpdateScoreText();
        // �׽�Ʈ ���� üũ �߰�
        //CheckForTestLevel();
    }

    // ���Ѹ������ Ȯ���ϴ� �޼��� �߰�
    bool IsInfiniteMode()
    {
        // InfiniteModeManager�� ���� ������ ���Ѹ��� �Ǵ�
        return FindFirstObjectByType<InfiniteModeManager>() != null;
    }

    void CalculateScreenMetrics()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // ȭ�� ���� ���
            screenAspectRatio = (float)Screen.width / Screen.height;

            // �ȼ��� ���� �������� ��ȯ�ϴ� ���� ���
            float worldHeight = mainCamera.orthographicSize * 2f;
            pixelsToWorldUnit = worldHeight / Screen.height;

            Debug.Log($"Screen: {Screen.width}x{Screen.height}, Aspect: {screenAspectRatio:F2}, PixelToWorld: {pixelsToWorldUnit:F4}");
        }
    }

    public void CalculateOptimalCameraSize()
    {
        if (!portraitMode)
        {
            // ���θ��� ���� ���� ����
            return;
        }

        // ȭ�� ��Ʈ���� ���
        CalculateScreenMetrics();

        // �׸��尡 �����ϴ� ���� ���� ��� (cellSize �ݿ�)
        float gridWorldWidth = width * cellSize + sideMarginPixels * pixelsToWorldUnit * 2f;
        float gridWorldHeight = height * cellSize;

        // UI ������ ���� �������� ��ȯ
        float topUISpace = topUISpacePixels * pixelsToWorldUnit;
        float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;

        // �ʿ��� ī�޶� ũ�� ���
        float cameraHeightFromHeight = (gridWorldHeight + topUISpace + bottomUISpace) * 0.5f;
        float cameraHeightFromWidth = gridWorldWidth / screenAspectRatio * 0.5f;

        float requiredCameraSize = Mathf.Max(cameraHeightFromHeight, cameraHeightFromWidth);

        // ī�޶� ũ�� ���� ����
        requiredCameraSize = Mathf.Clamp(requiredCameraSize,
            minCameraSize,
            maxCameraSize);

        // ���� �߰�
        requiredCameraSize *= (1f + cameraMarginPercent);

        // ī�޶� ũ�� ����
        if (Camera.main != null)
        {
            Camera.main.orthographicSize = requiredCameraSize;
            Debug.Log($"Camera size adjusted to: {requiredCameraSize} (considering cellSize: {cellSize})");
        }
    }

    // �׸��� �߾� ������ ���
    public void CalculateGridCenterOffset()
    {
        if (!centerGrid)
        {
            gridCenterOffset = gridOffset;
            return;
        }

        // �׸����� ���� ũ�� ��� (��� �� ���� ����)
        float gridWorldWidth = (width - 1) * cellSize; // cellSize ���
        float gridWorldHeight = (height - 1) * cellSize; // cellSize ���

        Debug.Log($"CalculateGridCenterOffset: cellSize = {cellSize}, gridWorldWidth = {gridWorldWidth}, gridWorldHeight = {gridWorldHeight}");

        // X�� �߾� ����: �׸��� ��ü�� ȭ�� �߾ӿ� ����
        float xOffset = -gridWorldWidth * 0.5f;

        // Y�� �߾� ����: �ܼ��� �׸��带 ȭ�� �߾ӿ� ��ġ
        float yOffset = -gridWorldHeight * 0.5f;

        gridCenterOffset = new Vector2(xOffset, yOffset) + gridOffset;

        Debug.Log($"Grid center offset calculated: {gridCenterOffset}");
    }

    // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ
    public Vector3 GridToWorldPosition(int x, int y)
    {
        Vector3 worldPos = new Vector3(
            x * cellSize + gridCenterOffset.x,  // cellSize ���
            y * cellSize + gridCenterOffset.y,  // cellSize ���
            0f
        );

        return worldPos;
    }

    Vector3 GridToWorldPosition(float x, float y)
    {
        Vector3 basePosition = new Vector3(x * cellSize, y * cellSize, 0);
        Vector3 centeredPosition = basePosition + (Vector3)gridCenterOffset;

        return centeredPosition;
    }

    // �׸����� ���� ��踦 ��Ȯ�� ����ϴ� �޼���
    public Rect GetGridBounds()
    {
        Vector3 bottomLeft = GridToWorldPosition(0, 0) + Vector3.one * (-cellSize * 0.5f);
        Vector3 topRight = GridToWorldPosition(width - 1, height - 1) + Vector3.one * (cellSize * 0.5f);

        return new Rect(bottomLeft.x, bottomLeft.y,
                       topRight.x - bottomLeft.x,
                       topRight.y - bottomLeft.y);
    }

    // ȭ�� �߾Ӱ� �׸��� �߾��� ���̸� Ȯ���ϴ� ����� �޼���
    public void CheckCenterAlignment()
    {
        Rect gridBounds = GetGridBounds();
        float gridCenterX = gridBounds.x + gridBounds.width * 0.5f;
        float screenCenterX = 0f; // ���� ��ǥ���� ȭ�� �߾��� 0

        Debug.Log($"Screen center X: {screenCenterX}");
        Debug.Log($"Grid center X: {gridCenterX}");
        Debug.Log($"Difference: {Mathf.Abs(gridCenterX - screenCenterX)}");

        if (Mathf.Abs(gridCenterX - screenCenterX) > 0.1f)
        {
            Debug.LogWarning("Grid is not centered on screen!");
        }
        else
        {
            Debug.Log("Grid is properly centered.");
        }
    }

    public void InitializeGrid()
    {
        // ������ �߿���: ī�޶� ũ�� ����, �� ���� ������ ���
        CalculateOptimalCameraSize();
        CalculateGridCenterOffset();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
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

        // �׸��� ���� �� ī�޶� ��ġ ����
        AdjustCameraPosition();
    }

    // ��� ũ�� ������ ���� ���� �޼���
    void ApplyBlockScale(GameObject blockObj)
    {
        if (blockObj == null) return;

        // ����� ���� �������� cellSize�� �°� ����
        // �⺻ ��� ũ�Ⱑ 1x1�̶�� ����
        blockObj.transform.localScale = Vector3.one * cellSize;

        Debug.Log($"Block scale applied: {blockObj.transform.localScale}");
    }

    GameObject CreateBlock(GameObject prefab, int x, int y)
    {
        //Vector3 position = GridToWorldPosition(x, y);

        //GameObject newBlock = Instantiate(prefab, position, Quaternion.identity, gridParent);
        //newBlock.name = $"Block_{x}_{y}";

        //Block blockComponent = newBlock.GetComponent<Block>();
        //if (blockComponent == null)
        //{
        //    blockComponent = newBlock.AddComponent<Block>();
        //}
        //blockComponent.x = x;
        //blockComponent.y = y;
        //blockComponent.isEmpty = prefab == emptyBlockPrefab;

        //if (blockComponent.isEmpty)
        //{
        //    BlockInteraction interaction = newBlock.GetComponent<BlockInteraction>();
        //    if (interaction == null)
        //    {
        //        interaction = newBlock.AddComponent<BlockInteraction>();
        //    }
        //    interaction.gridManager = this;
        //}

        //return newBlock;

        Vector3 worldPos = GridToWorldPosition(x, y);
        GameObject blockObj = Instantiate(prefab, worldPos, Quaternion.identity);

        if (gridParent != null)
        {
            blockObj.transform.SetParent(gridParent.transform);
        }

        // ��� ������ ����
        ApplyBlockScale(blockObj);

        // Block ������Ʈ ����
        Block blockComponent = blockObj.GetComponent<Block>();
        if (blockComponent != null)
        {
            blockComponent.x = x;
            blockComponent.y = y;
            blockComponent.isEmpty = (prefab == emptyBlockPrefab);
        }

        // BlockInteraction ������Ʈ ���� (�� ����� ���)
        if (prefab == emptyBlockPrefab)
        {
            BlockInteraction interaction = blockObj.GetComponent<BlockInteraction>();
            if (interaction != null)
            {
                interaction.gridManager = this;
            }
        }

        return blockObj;
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

        // �׸��� �迭 �����
        grid = new GameObject[width, height];

        // ���� �߿�: ī�޶� ũ�� ����, ������ ���߿�
        CalculateOptimalCameraSize();
        CalculateGridCenterOffset();

        // �������� ���Ͽ� ���� ��� ����
        CreateBlocksFromPattern(stageData.blockPattern);

        // ���� UI ������Ʈ
        currentScore = 0;
        UpdateScoreText();

        // ī�޶� ��ġ �ڵ� ����
        AdjustCameraPosition();
    }

    public void InitializeGridWithPattern(int[,] pattern)
    {
        if (pattern == null)
        {
            Debug.LogWarning("Pattern is null, initializing with default pattern");
            InitializeGrid();
            return;
        }

        // �׸��� �迭 �ʱ�ȭ
        grid = new GameObject[width, height];

        // �� ũ�� ���� Ȯ�� �� �α�
        Debug.Log($"InitializeGridWithPattern: cellSize = {cellSize}, width = {width}, height = {height}");

        // �߾� ���� ������ ���
        CalculateGridCenterOffset();
        CalculateOptimalCameraSize();
        AdjustCameraPosition();

        // ���Ͽ� ���� ��� ����
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int blockType = 0; // �⺻��: �� ���

                // ���� ���� ���� ������ ���� �� ���
                if (x < pattern.GetLength(0) && y < pattern.GetLength(1))
                {
                    blockType = pattern[x, y];
                }

                GameObject blockObj = CreateBlockFromType(blockType, x, y);
                if (blockObj != null)
                {
                    grid[x, y] = blockObj;

                    // ��� ũ�⸦ cellSize�� �°� ����
                    Transform blockTransform = blockObj.transform;
                    blockTransform.localScale = Vector3.one * cellSize;

                    Debug.Log($"Block created at ({x},{y}) with scale {blockTransform.localScale}");
                }
            }
        }

        // UI ������Ʈ
        UpdateScoreText();

        Debug.Log($"Grid initialized with custom pattern: {width}x{height}, cellSize: {cellSize}");
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

    GameObject CreateBlockFromType(int blockType, int x, int y)
    {
        GameObject prefabToUse = null;

        if (blockType == 0)
        {
            // �� ���
            prefabToUse = emptyBlockPrefab;
        }
        else if (blockType >= 1 && blockType <= blockPrefabs.Length)
        {
            // ���� ���
            prefabToUse = blockPrefabs[blockType - 1];
        }
        else
        {
            // �߸��� Ÿ���̸� �� �������
            prefabToUse = emptyBlockPrefab;
        }

        if (prefabToUse != null)
        {
            GameObject blockObj = CreateBlock(prefabToUse, x, y);
            return blockObj;
        }

        return null;
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
                        Destroy(grid[x, y]);
                        grid[x, y] = null;
                    }
                }
            }
        }

        //if (grid != null)
        //{
        //    for (int x = 0; x < grid.GetLength(0); x++)
        //    {
        //        for (int y = 0; y < grid.GetLength(1); y++)
        //        {
        //            if (grid[x, y] != null)
        //            {
        //                Destroy(grid[x, y]);
        //            }
        //        }
        //    }
        //}

        // GridContainer�� ��� �ڽ� ������Ʈ�� ����
        if (gridParent != null)
        {
            for (int i = gridParent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(gridParent.transform.GetChild(i).gameObject);
            }
        }

        grid = null;
        Debug.Log("Grid completely cleared");
    }

    // ���� �׸����� cellSize�� ��Ÿ�ӿ� ������Ʈ�ϴ� �޼���
    public void UpdateCellSize(float newCellSize)
    {
        cellSize = newCellSize;

        Debug.Log($"Updating cell size to: {cellSize}");

        // ���� ��ϵ��� ��ġ�� ũ�� ������Ʈ
        if (grid != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        // ��� ��ġ ������Ʈ
                        Vector3 newPos = GridToWorldPosition(x, y);
                        grid[x, y].transform.position = newPos;

                        // ��� ������ ������Ʈ
                        ApplyBlockScale(grid[x, y]);
                    }
                }
            }
        }

        // �׸��� ���̾ƿ� ����
        CalculateGridCenterOffset();
        CalculateOptimalCameraSize();
        AdjustCameraPosition();

        Debug.Log($"Cell size update completed: {cellSize}");
    }

    public void AdjustCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // ī�޶� ��Ȯ�� ���� ��ǥ (0, 0)�� ������ ����
            // �̰��� �ٽ�: �׸��� �߽��� �ƴ� ���� ������ ī�޶� �߽�����
            Vector3 worldCenter = Vector3.zero;

            if (portraitMode)
            {
                // ���θ�忡���� Y�ุ UI ������ ����ؼ� �ణ ����
                float topUISpace = topUISpacePixels * pixelsToWorldUnit;
                float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;

                // Y�� ������: ��ܰ� �ϴ� UI ������ ���̸�ŭ�� ����
                float yOffset = -(topUISpace - bottomUISpace) * 0.5f;
                worldCenter.y = yOffset;
            }

            Vector3 newCameraPosition = new Vector3(
                worldCenter.x,  // X���� �׻� 0 (���� �߽�)
                worldCenter.y,  // Y���� UI ���� ����� �߽�
                mainCamera.transform.position.z  // Z���� ���� �� ����
            );

            mainCamera.transform.position = newCameraPosition;

            Debug.Log($"Camera positioned at: {newCameraPosition}");
            Debug.Log($"Camera should show world center (0,0) at screen center");

            // ī�޶� �ùٸ� ��ġ�� �ִ��� Ȯ��
            Vector3 screenCenter = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f));
            Debug.Log($"Screen center maps to world position: ({screenCenter.x:F2}, {screenCenter.y:F2})");
        }
    }

    // ī�޶� ��ġ ���� �޼��� �߰�
    public void ValidateCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // ȭ�� �߾��� ���� ��ǥ (0, 0) ��ó�� ����Ű���� Ȯ��
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f);
        Vector3 worldCenter = mainCamera.ScreenToWorldPoint(screenCenter);

        Debug.Log($"Camera Position: {mainCamera.transform.position}");
        Debug.Log($"Screen Center: {screenCenter}");
        Debug.Log($"World Center (from screen): ({worldCenter.x:F2}, {worldCenter.y:F2})");

        float distanceFromWorldCenter = Vector2.Distance(new Vector2(worldCenter.x, worldCenter.y), Vector2.zero);

        if (distanceFromWorldCenter > 1f)
        {
            Debug.LogWarning($"Camera is not centered! Distance from world center: {distanceFromWorldCenter:F2}");
            Debug.LogWarning("Try clicking 'Fix Camera Position' button");
        }
        else
        {
            Debug.Log("Camera is properly centered.");
        }
    }

    // ī�޶� ��ġ ���� ���� �޼���
    public void FixCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // ī�޶� ������ ���� �߽����� �̵�
            Vector3 fixedPosition = new Vector3(0f, 0f, mainCamera.transform.position.z);

            if (portraitMode)
            {
                // UI ���� ����� Y�� ����
                float topUISpace = topUISpacePixels * pixelsToWorldUnit;
                float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;
                float yOffset = -(topUISpace - bottomUISpace) * 0.5f;
                fixedPosition.y = yOffset;
            }

            mainCamera.transform.position = fixedPosition;

            Debug.Log($"Camera position fixed to: {fixedPosition}");

            // ���� �� ����
            ValidateCameraPosition();
        }
    }

    // ���� �׸��� ������ �迭�� �������� (���� ��ɿ�)
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
                        pattern[x, y] = 0; // �� ���
                    }
                    else
                    {
                        // �±׳� �ٸ� ������� ��� Ÿ�� ����
                        string tag = grid[x, y].tag;
                        pattern[x, y] = GetBlockTypeFromTag(tag);
                    }
                }
                else
                {
                    pattern[x, y] = 0; // null�̸� �� ������� ó��
                }
            }
        }

        return pattern;
    }

    int GetBlockTypeFromTag(string tag)
    {
        switch (tag)
        {
            case "RedBlock": return 1;
            case "BlueBlock": return 2;
            case "YellowBlock": return 3;
            case "GreenBlock": return 4;
            case "PurpleBlock": return 5;
            default: return 0; // �� ���
        }
    }

    // �׽�Ʈ ������ �¸� ���� üũ �������̵�
    public void CheckTestLevelWinCondition()
    {
        if (currentScore >= targetScore)
        {
            Debug.Log("Test level completed!");

            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }

            // �׽�Ʈ ���� �Ϸ� ó��
            OnTestLevelCompleted();
        }
    }

    void OnTestLevelCompleted()
    {
        // �׽�Ʈ ���� �Ϸ� �� ó���� ����
        // ��: ��� ����, �޴��� ���ư��� ��

        Debug.Log($"Test level completed with score: {currentScore}/{targetScore}");

        // PlayerPrefs�� ��� ���� (���û���)
        PlayerPrefs.SetInt("LastTestScore", currentScore);
        PlayerPrefs.SetInt("LastTestSuccess", currentScore >= targetScore ? 1 : 0);
    }

    // ������ ��ƿ��Ƽ �޼���
    public void RecalculateLayout()
    {
        CalculateOptimalCameraSize();
        CalculateGridCenterOffset();
        AdjustCameraPosition();

        // ���� ��ϵ� ��ġ ������Ʈ
        if (grid != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        grid[x, y].transform.position = GridToWorldPosition(x, y);
                    }
                }
            }
        }

        // �߾� ���� �� ī�޶� ��ġ Ȯ��
        CheckCenterAlignment();
        ValidateCameraPosition();
    }

    // UI ��ġ�� ���� ���� �޼���
    public Rect GetGameAreaRect()
    {
        Vector3 bottomLeft = GridToWorldPosition(0, 0) - Vector3.one * (cellSize * 0.5f);
        Vector3 topRight = GridToWorldPosition(width - 1, height - 1) + Vector3.one * (cellSize * 0.5f);

        return new Rect(bottomLeft.x, bottomLeft.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
    }

    // UI ��Ҹ� ������ ��ġ�� ��ġ�ϱ� ���� ����� �޼���
    public Vector3 GetUIPosition(UIPosition position)
    {
        Vector3 gridCenter = GridToWorldPosition((int)(width / 2f), (int)(height / 2f));
        float gridWorldWidth = width * cellSize;
        float gridWorldHeight = height * cellSize;

        float topSafeAreaWorld = topUISpacePixels * screenAspectRatio;
        float bottomSafeAreaWorld = bottomUISpacePixels * screenAspectRatio;

        switch (position)
        {
            case UIPosition.TopCenter:
                return new Vector3(gridCenter.x, gridCenter.y + gridWorldHeight * 0.5f + topSafeAreaWorld * 0.3f, 0);

            case UIPosition.BottomCenter:
                return new Vector3(gridCenter.x, gridCenter.y - gridWorldHeight * 0.5f - bottomSafeAreaWorld * 0.3f, 0);

            case UIPosition.TopLeft:
                return new Vector3(gridCenter.x - gridWorldWidth * 0.4f, gridCenter.y + gridWorldHeight * 0.5f + topSafeAreaWorld * 0.3f, 0);

            case UIPosition.TopRight:
                return new Vector3(gridCenter.x + gridWorldWidth * 0.4f, gridCenter.y + gridWorldHeight * 0.5f + topSafeAreaWorld * 0.3f, 0);

            case UIPosition.BottomLeft:
                return new Vector3(gridCenter.x - gridWorldWidth * 0.4f, gridCenter.y - gridWorldHeight * 0.5f - bottomSafeAreaWorld * 0.3f, 0);

            case UIPosition.BottomRight:
                return new Vector3(gridCenter.x + gridWorldWidth * 0.4f, gridCenter.y - gridWorldHeight * 0.5f - bottomSafeAreaWorld * 0.3f, 0);

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

            // ���� ������ ������Ʈ (���� �߰�)
            if (UserDataManager.Instance != null)
            {
                int currentStageNumber = StageManager.Instance != null ?
                    StageManager.Instance.GetCurrentStageNumber() : 1;

                UserDataManager.Instance.GiveStageReward(currentStageNumber, currentScore);
                UserDataManager.Instance.UpdateStageProgress(currentStageNumber, currentScore, true);
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

        // ���Ѹ�� �ݹ��� ������ �켱 ����
        if (onEmptyBlockClicked != null)
        {
            Debug.Log("Calling infinite mode callback");
            onEmptyBlockClicked(x, y);
            return;
        }

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

    public List<GameObject> FindMatchingBlocksInFourDirections(int x, int y)
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
                if (blockComponent != null &&! blockComponent.isEmpty)
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
                Debug.Log($"Adding {blocks.Count} blocks of type {blockType} to matched blocks");
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

    // ���Ѹ�忡�� �׸��� ������ ���� ���� �޼���
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

    // ���Ѹ��� �� �׸��� �ʱ�ȭ
    public void InitializeEmptyGrid()
    {
        // ���� �׸��� ����
        ClearGrid();

        // �� �׸��� �迭 ����
        grid = new GameObject[width, height];

        // �߾� ���� ���
        CalculateGridCenterOffset();
        AdjustCameraPosition();

        Debug.Log($"Empty grid initialized: {width}x{height}");
    }

    void AddScore(int blockCount)
    {
        currentScore += blockCount * scorePerBlock;
        UpdateScoreText();
    }

    public void UpdateScoreText()
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
    // ���θ��� ����� �����
    void OnDrawGizmos()
    {
        if (Application.isPlaying && grid != null)
        {
            // �׸��� ��輱 (�����)
            Gizmos.color = Color.yellow;
            Vector3 bottomLeft = GridToWorldPosition(0, 0) + Vector3.one * (-cellSize * 0.5f);
            Vector3 topRight = GridToWorldPosition(width - 1, height - 1) + Vector3.one * (cellSize * 0.5f);

            // �׸��� �ܰ���
            Gizmos.DrawLine(bottomLeft, new Vector3(topRight.x, bottomLeft.y, 0));
            Gizmos.DrawLine(new Vector3(topRight.x, bottomLeft.y, 0), topRight);
            Gizmos.DrawLine(topRight, new Vector3(bottomLeft.x, topRight.y, 0));
            Gizmos.DrawLine(new Vector3(bottomLeft.x, topRight.y, 0), bottomLeft);

            // �׸��� �߾��� (������)
            Vector3 centerPoint = GridToWorldPosition(width / 2f - 0.5f, height / 2f - 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerPoint, 0.3f);

            if (portraitMode)
            {
                // UI ���� ǥ��
                float topUISpace = topUISpacePixels * pixelsToWorldUnit;
                float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;

                // ��� UI ���� (�Ķ���)
                Gizmos.color = Color.blue;
                Vector3 topUICenter = new Vector3(centerPoint.x, topRight.y + topUISpace * 0.5f, 0);
                Gizmos.DrawWireCube(topUICenter, new Vector3(width * cellSize, topUISpace, 0.1f));

                // �ϴ� UI ���� (�ʷϻ�)
                Gizmos.color = Color.green;
                Vector3 bottomUICenter = new Vector3(centerPoint.x, bottomLeft.y - bottomUISpace * 0.5f, 0);
                Gizmos.DrawWireCube(bottomUICenter, new Vector3(width * cellSize, bottomUISpace, 0.1f));

                // ī�޶� ��� (�����)
                Camera mainCamera = Camera.main;
                if (mainCamera != null && mainCamera.orthographic)
                {
                    Gizmos.color = Color.magenta;
                    float cameraHeight = mainCamera.orthographicSize * 2;
                    float cameraWidth = cameraHeight * ((float)Screen.width / Screen.height);

                    Vector3 cameraCenter = mainCamera.transform.position;
                    cameraCenter.z = 0;

                    Gizmos.DrawWireCube(cameraCenter, new Vector3(cameraWidth, cameraHeight, 0.1f));
                }
            }
        }
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

*/