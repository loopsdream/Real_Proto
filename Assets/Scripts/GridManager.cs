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
    public bool centerGrid = true; // 그리드를 중앙에 배치할지 여부
    public Vector2 gridOffset = Vector2.zero; // 추가 오프셋

    [Header("Camera Settings")]
    public float cameraMargin = 1.5f; // 그리드 주변 여백
    public float minCameraSize = 3f;   // 최소 카메라 크기
    public float maxCameraSize = 10f;  // 최대 카메라 크기

    [Header("UI Spacing")]
    public float topUISpace = 2f;    // 상단 UI를 위한 여백
    public float bottomUISpace = 1f; // 하단 UI를 위한 여백

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
    private Vector2 gridCenterOffset; // 그리드 중앙 정렬을 위한 오프셋

    void Start()
    {
        grid = new GameObject[width, height];
        InitializeGrid();
        UpdateScoreText();
    }

    // 그리드 중앙 오프셋 계산
    void CalculateGridCenterOffset()
    {
        if (centerGrid)
        {
            // 그리드의 실제 크기 계산
            float gridWorldWidth = (width - 1) * cellSize;
            float gridWorldHeight = (height - 1) * cellSize;

            // 중앙 정렬을 위한 오프셋 계산
            gridCenterOffset = new Vector2(
                -gridWorldWidth * 0.5f,
                -gridWorldHeight * 0.5f
            );

            // 추가 오프셋 적용
            gridCenterOffset += gridOffset;
        }
        else
        {
            gridCenterOffset = gridOffset;
        }

        Debug.Log($"Grid center offset calculated: {gridCenterOffset}");
    }

    // 그리드 좌표를 월드 좌표로 변환
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

                // 30% 확률로 빈 블록 생성, 70% 확률로 랜덤 블록 생성
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

        // 카메라 위치 자동 조정 (선택사항)
        AdjustCameraPosition();
    }

    // 스테이지 데이터를 사용하여 그리드 초기화
    public void InitializeStageGrid(StageData stageData)
    {
        // 기존 그리드 정리
        ClearGrid();

        // 새 그리드 크기 설정
        width = stageData.gridWidth;
        height = stageData.gridHeight;
        targetScore = stageData.targetScore;

        // 그리드 중앙 정렬 오프셋 재계산
        CalculateGridCenterOffset();

        // 그리드 배열 재생성
        grid = new GameObject[width, height];

        // 스테이지 패턴에 따라 블록 생성
        CreateBlocksFromPattern(stageData.blockPattern);

        // 점수 UI 업데이트
        currentScore = 0;
        UpdateScoreText();

        // 카메라 위치 자동 조정
        AdjustCameraPosition();
    }

    // 패턴에 따라 블록 생성
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

    // 블록 타입에 따른 프리팹 반환
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

    // 카메라 위치를 그리드 중앙에 맞게 자동 조정
    void AdjustCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 그리드의 중앙점 계산
            Vector3 gridCenter = GridToWorldPosition((int)(width / 2f), (int)(height / 2f));

            // UI 간격을 고려한 카메라 위치 조정
            Vector3 cameraPositionOffset = new Vector3(
                0,
                (topUISpace - bottomUISpace) * 0.5f, // UI 간격 차이만큼 조정
                0
            );

            // 카메라 위치 조정 (Z 좌표는 유지)
            Vector3 newCameraPosition = new Vector3(
                gridCenter.x + cameraPositionOffset.x,
                gridCenter.y + cameraPositionOffset.y,
                mainCamera.transform.position.z
            );

            mainCamera.transform.position = newCameraPosition;

            // 카메라 크기 자동 조정 (Orthographic인 경우)
            if (mainCamera.orthographic)
            {
                float gridWorldWidth = width * cellSize;
                float gridWorldHeight = height * cellSize;

                // UI 공간을 고려한 사용 가능한 화면 영역 계산
                float availableHeight = gridWorldHeight + topUISpace + bottomUISpace;
                float availableWidth = gridWorldWidth;

                // 카메라 마진을 고려한 필요 크기 계산
                float requiredSizeForWidth = (availableWidth + cameraMargin * 2) * 0.5f;
                float requiredSizeForHeight = (availableHeight + cameraMargin * 2) * 0.5f;

                // 더 큰 값을 선택하여 모든 요소가 화면에 들어오도록 함
                float requiredSize = Mathf.Max(requiredSizeForWidth, requiredSizeForHeight);

                // 최소/최대 크기 제한 적용
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
        // 중앙 정렬된 위치 계산
        Vector3 position = GridToWorldPosition(x, y);

        GameObject newBlock = Instantiate(prefab, position, Quaternion.identity, gridParent);
        newBlock.name = $"Block_{x}_{y}";

        // 블록에 좌표 정보 저장
        Block blockComponent = newBlock.GetComponent<Block>();
        if (blockComponent == null)
        {
            blockComponent = newBlock.AddComponent<Block>();
        }
        blockComponent.x = x;
        blockComponent.y = y;
        blockComponent.isEmpty = prefab == emptyBlockPrefab;

        // 빈 블록에는 클릭 이벤트 추가
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

    // UI 요소들이 그리드와 겹치지 않도록 안전 영역 계산
    public Vector2 GetSafeAreaBounds()
    {
        // 그리드 영역 계산
        float gridWorldWidth = width * cellSize;
        float gridWorldHeight = height * cellSize;

        // 카메라 크기 고려
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.orthographic)
        {
            float cameraHeight = mainCamera.orthographicSize * 2;
            float cameraWidth = cameraHeight * mainCamera.aspect;

            // UI를 위한 안전 영역 계산
            Vector2 safeArea = new Vector2(
                (cameraWidth - gridWorldWidth) * 0.5f,
                (cameraHeight - gridWorldHeight - topUISpace - bottomUISpace) * 0.5f
            );

            return safeArea;
        }

        return Vector2.zero;
    }

    // UI 요소를 적절한 위치에 배치하기 위한 도우미 메서드
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

    // 승리 조건 체크 시 스테이지 매니저에게 알림
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

            Debug.Log("스테이지 클리어!");
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

            // 스테이지 매니저에게 이동 알림
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
            new Vector2Int(0, 1),  // 상
            new Vector2Int(0, -1), // 하
            new Vector2Int(-1, 0), // 좌
            new Vector2Int(1, 0)   // 우
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

    // 디버깅을 위한 그리드 시각화 (Scene 뷰에서만 보임)
    void OnDrawGizmos()
    {
        if (grid == null) return;

        Gizmos.color = Color.yellow;

        // 그리드 경계선 그리기
        Vector3 bottomLeft = GridToWorldPosition(0, 0) + Vector3.one * (-cellSize * 0.5f);
        Vector3 topRight = GridToWorldPosition(width - 1, height - 1) + Vector3.one * (cellSize * 0.5f);

        // 그리드 외곽선
        Gizmos.DrawLine(bottomLeft, new Vector3(topRight.x, bottomLeft.y, 0));
        Gizmos.DrawLine(new Vector3(topRight.x, bottomLeft.y, 0), topRight);
        Gizmos.DrawLine(topRight, new Vector3(bottomLeft.x, topRight.y, 0));
        Gizmos.DrawLine(new Vector3(bottomLeft.x, topRight.y, 0), bottomLeft);

        // 그리드 중앙점 표시
        Vector3 centerPoint = GridToWorldPosition((int)(width / 2f), (int)(height / 2f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centerPoint, 0.2f);
    }

    // UI 위치를 지정하기 위한 열거형
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