/*

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    [System.NonSerialized]
    public System.Action<int, int> onEmptyBlockClicked; // 무한모드 콜백

    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1.0f;
    public Transform gridParent;

    [Header("Grid Centering")]
    public bool centerGrid = true; // 그리드를 중앙에 배치할지 여부
    public Vector2 gridOffset = Vector2.zero; // 추가 오프셋

    [Header("Portrait Mode Settings")]
    public bool portraitMode = true;
    public float topUISpacePixels = 200f;    // 상단 UI 및 노치 영역 (픽셀)
    public float bottomUISpacePixels = 150f; // 하단 UI 영역 (픽셀)
    public float sideMarginPixels = 100f;     // 좌우 여백 (픽셀)

    [Header("Camera Settings")]
    public float cameraMarginPercent = 0.1f; // 그리드 주변 여백
    public float minCameraSize = 3f;   // 최소 카메라 크기
    public float maxCameraSize = 15f;  // 최대 카메라 크기

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
    private Vector2 gridCenterOffset; // 그리드 중앙 정렬을 위한 오프셋

    // 세로모드 관련 계산된 값들
    private float pixelsToWorldUnit = 1f;
    private float screenAspectRatio = 1f;

    void Start()
    {
        // 무한모드에서는 자동 초기화 스킵
        if (IsInfiniteMode())
        {
            Debug.Log("Infinite mode detected, skipping auto initialization");
            return;
        }

        grid = new GameObject[width, height];
        InitializeGrid();

        UpdateScoreText();
        // 테스트 레벨 체크 추가
        //CheckForTestLevel();
    }

    // 무한모드인지 확인하는 메서드 추가
    bool IsInfiniteMode()
    {
        // InfiniteModeManager가 씬에 있으면 무한모드로 판단
        return FindFirstObjectByType<InfiniteModeManager>() != null;
    }

    void CalculateScreenMetrics()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 화면 비율 계산
            screenAspectRatio = (float)Screen.width / Screen.height;

            // 픽셀을 월드 유닛으로 변환하는 비율 계산
            float worldHeight = mainCamera.orthographicSize * 2f;
            pixelsToWorldUnit = worldHeight / Screen.height;

            Debug.Log($"Screen: {Screen.width}x{Screen.height}, Aspect: {screenAspectRatio:F2}, PixelToWorld: {pixelsToWorldUnit:F4}");
        }
    }

    public void CalculateOptimalCameraSize()
    {
        if (!portraitMode)
        {
            // 가로모드는 기존 로직 유지
            return;
        }

        // 화면 메트릭스 계산
        CalculateScreenMetrics();

        // 그리드가 차지하는 실제 공간 계산 (cellSize 반영)
        float gridWorldWidth = width * cellSize + sideMarginPixels * pixelsToWorldUnit * 2f;
        float gridWorldHeight = height * cellSize;

        // UI 공간을 월드 유닛으로 변환
        float topUISpace = topUISpacePixels * pixelsToWorldUnit;
        float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;

        // 필요한 카메라 크기 계산
        float cameraHeightFromHeight = (gridWorldHeight + topUISpace + bottomUISpace) * 0.5f;
        float cameraHeightFromWidth = gridWorldWidth / screenAspectRatio * 0.5f;

        float requiredCameraSize = Mathf.Max(cameraHeightFromHeight, cameraHeightFromWidth);

        // 카메라 크기 제한 적용
        requiredCameraSize = Mathf.Clamp(requiredCameraSize,
            minCameraSize,
            maxCameraSize);

        // 여백 추가
        requiredCameraSize *= (1f + cameraMarginPercent);

        // 카메라 크기 적용
        if (Camera.main != null)
        {
            Camera.main.orthographicSize = requiredCameraSize;
            Debug.Log($"Camera size adjusted to: {requiredCameraSize} (considering cellSize: {cellSize})");
        }
    }

    // 그리드 중앙 오프셋 계산
    public void CalculateGridCenterOffset()
    {
        if (!centerGrid)
        {
            gridCenterOffset = gridOffset;
            return;
        }

        // 그리드의 실제 크기 계산 (블록 간 간격 포함)
        float gridWorldWidth = (width - 1) * cellSize; // cellSize 사용
        float gridWorldHeight = (height - 1) * cellSize; // cellSize 사용

        Debug.Log($"CalculateGridCenterOffset: cellSize = {cellSize}, gridWorldWidth = {gridWorldWidth}, gridWorldHeight = {gridWorldHeight}");

        // X축 중앙 정렬: 그리드 전체를 화면 중앙에 맞춤
        float xOffset = -gridWorldWidth * 0.5f;

        // Y축 중앙 정렬: 단순히 그리드를 화면 중앙에 배치
        float yOffset = -gridWorldHeight * 0.5f;

        gridCenterOffset = new Vector2(xOffset, yOffset) + gridOffset;

        Debug.Log($"Grid center offset calculated: {gridCenterOffset}");
    }

    // 그리드 좌표를 월드 좌표로 변환
    public Vector3 GridToWorldPosition(int x, int y)
    {
        Vector3 worldPos = new Vector3(
            x * cellSize + gridCenterOffset.x,  // cellSize 사용
            y * cellSize + gridCenterOffset.y,  // cellSize 사용
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

    // 그리드의 실제 경계를 정확히 계산하는 메서드
    public Rect GetGridBounds()
    {
        Vector3 bottomLeft = GridToWorldPosition(0, 0) + Vector3.one * (-cellSize * 0.5f);
        Vector3 topRight = GridToWorldPosition(width - 1, height - 1) + Vector3.one * (cellSize * 0.5f);

        return new Rect(bottomLeft.x, bottomLeft.y,
                       topRight.x - bottomLeft.x,
                       topRight.y - bottomLeft.y);
    }

    // 화면 중앙과 그리드 중앙의 차이를 확인하는 디버그 메서드
    public void CheckCenterAlignment()
    {
        Rect gridBounds = GetGridBounds();
        float gridCenterX = gridBounds.x + gridBounds.width * 0.5f;
        float screenCenterX = 0f; // 월드 좌표에서 화면 중앙은 0

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
        // 순서가 중요함: 카메라 크기 먼저, 그 다음 오프셋 계산
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

        // 그리드 생성 후 카메라 위치 조정
        AdjustCameraPosition();
    }

    // 블록 크기 적용을 위한 헬퍼 메서드
    void ApplyBlockScale(GameObject blockObj)
    {
        if (blockObj == null) return;

        // 블록의 로컬 스케일을 cellSize에 맞게 조정
        // 기본 블록 크기가 1x1이라고 가정
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

        // 블록 스케일 적용
        ApplyBlockScale(blockObj);

        // Block 컴포넌트 설정
        Block blockComponent = blockObj.GetComponent<Block>();
        if (blockComponent != null)
        {
            blockComponent.x = x;
            blockComponent.y = y;
            blockComponent.isEmpty = (prefab == emptyBlockPrefab);
        }

        // BlockInteraction 컴포넌트 설정 (빈 블록인 경우)
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

    // 스테이지 데이터를 사용하여 그리드 초기화
    public void InitializeStageGrid(StageData stageData)
    {
        // 기존 그리드 정리
        ClearGrid();

        // 새 그리드 크기 설정
        width = stageData.gridWidth;
        height = stageData.gridHeight;
        targetScore = stageData.targetScore;

        // 그리드 배열 재생성
        grid = new GameObject[width, height];

        // 순서 중요: 카메라 크기 먼저, 오프셋 나중에
        CalculateOptimalCameraSize();
        CalculateGridCenterOffset();

        // 스테이지 패턴에 따라 블록 생성
        CreateBlocksFromPattern(stageData.blockPattern);

        // 점수 UI 업데이트
        currentScore = 0;
        UpdateScoreText();

        // 카메라 위치 자동 조정
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

        // 그리드 배열 초기화
        grid = new GameObject[width, height];

        // 셀 크기 설정 확인 및 로그
        Debug.Log($"InitializeGridWithPattern: cellSize = {cellSize}, width = {width}, height = {height}");

        // 중앙 정렬 오프셋 계산
        CalculateGridCenterOffset();
        CalculateOptimalCameraSize();
        AdjustCameraPosition();

        // 패턴에 따라 블록 생성
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int blockType = 0; // 기본값: 빈 블록

                // 패턴 범위 내에 있으면 패턴 값 사용
                if (x < pattern.GetLength(0) && y < pattern.GetLength(1))
                {
                    blockType = pattern[x, y];
                }

                GameObject blockObj = CreateBlockFromType(blockType, x, y);
                if (blockObj != null)
                {
                    grid[x, y] = blockObj;

                    // 블록 크기를 cellSize에 맞게 조정
                    Transform blockTransform = blockObj.transform;
                    blockTransform.localScale = Vector3.one * cellSize;

                    Debug.Log($"Block created at ({x},{y}) with scale {blockTransform.localScale}");
                }
            }
        }

        // UI 업데이트
        UpdateScoreText();

        Debug.Log($"Grid initialized with custom pattern: {width}x{height}, cellSize: {cellSize}");
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

    GameObject CreateBlockFromType(int blockType, int x, int y)
    {
        GameObject prefabToUse = null;

        if (blockType == 0)
        {
            // 빈 블록
            prefabToUse = emptyBlockPrefab;
        }
        else if (blockType >= 1 && blockType <= blockPrefabs.Length)
        {
            // 색상 블록
            prefabToUse = blockPrefabs[blockType - 1];
        }
        else
        {
            // 잘못된 타입이면 빈 블록으로
            prefabToUse = emptyBlockPrefab;
        }

        if (prefabToUse != null)
        {
            GameObject blockObj = CreateBlock(prefabToUse, x, y);
            return blockObj;
        }

        return null;
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

        // GridContainer의 모든 자식 오브젝트도 삭제
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

    // 기존 그리드의 cellSize를 런타임에 업데이트하는 메서드
    public void UpdateCellSize(float newCellSize)
    {
        cellSize = newCellSize;

        Debug.Log($"Updating cell size to: {cellSize}");

        // 기존 블록들의 위치와 크기 업데이트
        if (grid != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        // 블록 위치 업데이트
                        Vector3 newPos = GridToWorldPosition(x, y);
                        grid[x, y].transform.position = newPos;

                        // 블록 스케일 업데이트
                        ApplyBlockScale(grid[x, y]);
                    }
                }
            }
        }

        // 그리드 레이아웃 재계산
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
            // 카메라가 정확히 월드 좌표 (0, 0)을 보도록 설정
            // 이것이 핵심: 그리드 중심이 아닌 월드 원점을 카메라 중심으로
            Vector3 worldCenter = Vector3.zero;

            if (portraitMode)
            {
                // 세로모드에서는 Y축만 UI 영역을 고려해서 약간 조정
                float topUISpace = topUISpacePixels * pixelsToWorldUnit;
                float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;

                // Y축 오프셋: 상단과 하단 UI 공간의 차이만큼만 조정
                float yOffset = -(topUISpace - bottomUISpace) * 0.5f;
                worldCenter.y = yOffset;
            }

            Vector3 newCameraPosition = new Vector3(
                worldCenter.x,  // X축은 항상 0 (월드 중심)
                worldCenter.y,  // Y축은 UI 영역 고려한 중심
                mainCamera.transform.position.z  // Z축은 기존 값 유지
            );

            mainCamera.transform.position = newCameraPosition;

            Debug.Log($"Camera positioned at: {newCameraPosition}");
            Debug.Log($"Camera should show world center (0,0) at screen center");

            // 카메라가 올바른 위치에 있는지 확인
            Vector3 screenCenter = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f));
            Debug.Log($"Screen center maps to world position: ({screenCenter.x:F2}, {screenCenter.y:F2})");
        }
    }

    // 카메라 위치 검증 메서드 추가
    public void ValidateCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // 화면 중앙이 월드 좌표 (0, 0) 근처를 가리키는지 확인
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

    // 카메라 위치 강제 수정 메서드
    public void FixCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 카메라를 강제로 월드 중심으로 이동
            Vector3 fixedPosition = new Vector3(0f, 0f, mainCamera.transform.position.z);

            if (portraitMode)
            {
                // UI 영역 고려한 Y축 조정
                float topUISpace = topUISpacePixels * pixelsToWorldUnit;
                float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;
                float yOffset = -(topUISpace - bottomUISpace) * 0.5f;
                fixedPosition.y = yOffset;
            }

            mainCamera.transform.position = fixedPosition;

            Debug.Log($"Camera position fixed to: {fixedPosition}");

            // 수정 후 검증
            ValidateCameraPosition();
        }
    }

    // 현재 그리드 패턴을 배열로 내보내기 (저장 기능용)
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
                        pattern[x, y] = 0; // 빈 블록
                    }
                    else
                    {
                        // 태그나 다른 방법으로 블록 타입 결정
                        string tag = grid[x, y].tag;
                        pattern[x, y] = GetBlockTypeFromTag(tag);
                    }
                }
                else
                {
                    pattern[x, y] = 0; // null이면 빈 블록으로 처리
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
            default: return 0; // 빈 블록
        }
    }

    // 테스트 레벨용 승리 조건 체크 오버라이드
    public void CheckTestLevelWinCondition()
    {
        if (currentScore >= targetScore)
        {
            Debug.Log("Test level completed!");

            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }

            // 테스트 레벨 완료 처리
            OnTestLevelCompleted();
        }
    }

    void OnTestLevelCompleted()
    {
        // 테스트 레벨 완료 시 처리할 로직
        // 예: 결과 저장, 메뉴로 돌아가기 등

        Debug.Log($"Test level completed with score: {currentScore}/{targetScore}");

        // PlayerPrefs에 결과 저장 (선택사항)
        PlayerPrefs.SetInt("LastTestScore", currentScore);
        PlayerPrefs.SetInt("LastTestSuccess", currentScore >= targetScore ? 1 : 0);
    }

    // 디버깅용 유틸리티 메서드
    public void RecalculateLayout()
    {
        CalculateOptimalCameraSize();
        CalculateGridCenterOffset();
        AdjustCameraPosition();

        // 기존 블록들 위치 업데이트
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

        // 중앙 정렬 및 카메라 위치 확인
        CheckCenterAlignment();
        ValidateCameraPosition();
    }

    // UI 배치를 위한 헬퍼 메서드
    public Rect GetGameAreaRect()
    {
        Vector3 bottomLeft = GridToWorldPosition(0, 0) - Vector3.one * (cellSize * 0.5f);
        Vector3 topRight = GridToWorldPosition(width - 1, height - 1) + Vector3.one * (cellSize * 0.5f);

        return new Rect(bottomLeft.x, bottomLeft.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
    }

    // UI 요소를 적절한 위치에 배치하기 위한 도우미 메서드
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

    // 승리 조건 체크 시 스테이지 매니저에게 알림
    void CheckWinCondition()
    {
        if (currentScore >= targetScore)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageComplete();
            }

            // 유저 데이터 업데이트 (새로 추가)
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

            Debug.Log("스테이지 클리어!");
        }
    }

    public void OnEmptyBlockClicked(int x, int y)
    {
        Debug.Log($"Empty block clicked at ({x}, {y})");

        // 무한모드 콜백이 있으면 우선 실행
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

    public List<GameObject> FindMatchingBlocksInFourDirections(int x, int y)
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
                if (blockComponent != null &&! blockComponent.isEmpty)
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

    // 무한모드에서 그리드 접근을 위한 헬퍼 메서드
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

    // 무한모드용 빈 그리드 초기화
    public void InitializeEmptyGrid()
    {
        // 기존 그리드 삭제
        ClearGrid();

        // 새 그리드 배열 생성
        grid = new GameObject[width, height];

        // 중앙 정렬 계산
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

    // 디버깅을 위한 그리드 시각화 (Scene 뷰에서만 보임)
    // 세로모드용 디버깅 기즈모
    void OnDrawGizmos()
    {
        if (Application.isPlaying && grid != null)
        {
            // 그리드 경계선 (노란색)
            Gizmos.color = Color.yellow;
            Vector3 bottomLeft = GridToWorldPosition(0, 0) + Vector3.one * (-cellSize * 0.5f);
            Vector3 topRight = GridToWorldPosition(width - 1, height - 1) + Vector3.one * (cellSize * 0.5f);

            // 그리드 외곽선
            Gizmos.DrawLine(bottomLeft, new Vector3(topRight.x, bottomLeft.y, 0));
            Gizmos.DrawLine(new Vector3(topRight.x, bottomLeft.y, 0), topRight);
            Gizmos.DrawLine(topRight, new Vector3(bottomLeft.x, topRight.y, 0));
            Gizmos.DrawLine(new Vector3(bottomLeft.x, topRight.y, 0), bottomLeft);

            // 그리드 중앙점 (빨간색)
            Vector3 centerPoint = GridToWorldPosition(width / 2f - 0.5f, height / 2f - 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerPoint, 0.3f);

            if (portraitMode)
            {
                // UI 영역 표시
                float topUISpace = topUISpacePixels * pixelsToWorldUnit;
                float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;

                // 상단 UI 영역 (파란색)
                Gizmos.color = Color.blue;
                Vector3 topUICenter = new Vector3(centerPoint.x, topRight.y + topUISpace * 0.5f, 0);
                Gizmos.DrawWireCube(topUICenter, new Vector3(width * cellSize, topUISpace, 0.1f));

                // 하단 UI 영역 (초록색)
                Gizmos.color = Color.green;
                Vector3 bottomUICenter = new Vector3(centerPoint.x, bottomLeft.y - bottomUISpace * 0.5f, 0);
                Gizmos.DrawWireCube(bottomUICenter, new Vector3(width * cellSize, bottomUISpace, 0.1f));

                // 카메라 경계 (보라색)
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

*/