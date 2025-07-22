// LevelDesigner.cs - 인게임 레벨 디자인 시스템
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class LevelDesigner : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public TMP_InputField cellSizeInput;
    public TMP_InputField targetScoreInput;
    public TMP_InputField maxMovesInput;
    public Button generateGridButton;
    public Button testLevelButton;
    public Button clearGridButton;
    public Button backButton;

    [Header("Additional UI")]
    public Button newLevelButton;    // 새 레벨 시작 버튼
    public Button saveLevelButton;   // 레벨 저장 버튼
    public Button loadLevelButton;   // 레벨 불러오기 버튼

    [Header("Block Selection")]
    public Button[] blockTypeButtons;
    public Image[] blockTypeImages;
    public Button emptyBlockButton;

    [Header("Grid Container")]
    public Transform gridContainer;
    public ScrollRect gridScrollRect;

    [Header("Block Prefabs")]
    public GameObject[] blockPrefabs; // 각 색상별 블록 프리팹
    public GameObject emptyBlockPrefab;

    [Header("Designer Settings")]
    public int defaultWidth = 6;
    public int defaultHeight = 8;
    public float defaultCellSize = 80f;
    public int defaultTargetScore = 100;
    public int defaultMaxMoves = 20;

    private int currentWidth;
    private int currentHeight;
    private float currentCellSize;
    private int selectedBlockType = 0; // 0: 빈 블록, 1-5: 각 색상 블록
    private DesignerBlock[,] designerGrid;
    private List<GameObject> gridButtons = new List<GameObject>();

    void Start()
    {
        InitializeDesigner();
        SetupButtonEvents();
        SetDefaultValues();

        // 이전에 작업하던 데이터 복원
        LoadDesignerState();
    }

    void InitializeDesigner()
    {
        // 버튼 이벤트 설정
        generateGridButton.onClick.AddListener(GenerateDesignerGrid);
        testLevelButton.onClick.AddListener(TestLevel);
        clearGridButton.onClick.AddListener(ClearGrid);
        backButton.onClick.AddListener(BackToMenu);

        // 블록 타입 선택 버튼 설정
        for (int i = 0; i < blockTypeButtons.Length; i++)
        {
            int blockIndex = i;
            blockTypeButtons[i].onClick.AddListener(() => SelectBlockType(blockIndex + 1));
        }

        emptyBlockButton.onClick.AddListener(() => SelectBlockType(0));

        // 기본 선택: 빈 블록
        SelectBlockType(0);

        // 추가 버튼 이벤트 설정
        if (newLevelButton != null)
            newLevelButton.onClick.AddListener(StartNewLevel);

        if (saveLevelButton != null)
            saveLevelButton.onClick.AddListener(SaveCurrentLevel);

        if (loadLevelButton != null)
            loadLevelButton.onClick.AddListener(LoadSavedLevel);
    }

    // 새 레벨 시작 (기존 작업 삭제)
    void StartNewLevel()
    {
        // 확인 대화상자 표시 (간단한 방법)
        if (designerGrid != null)
        {
            Debug.Log("Starting new level (clearing current work)");
        }

        // 저장된 상태 삭제
        ClearSavedState();

        // UI 초기화
        SetDefaultValues();

        // 기존 그리드 삭제
        ClearGridButtons();
        designerGrid = null;

        // 기본 블록 타입 선택
        SelectBlockType(0);

        Debug.Log("New level started");
    }

    // 현재 레벨 저장 (PlayerPrefs 외에 파일로도 저장 가능)
    void SaveCurrentLevel()
    {
        if (designerGrid == null)
        {
            Debug.LogWarning("No level to save!");
            return;
        }

        // 현재 상태를 별도 슬롯에 저장
        SaveDesignerState();

        // 추가로 타임스탬프와 함께 저장
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        PlayerPrefs.SetString($"SavedLevel_{timestamp}_Pattern", PlayerPrefs.GetString("DesignerState_Pattern"));
        PlayerPrefs.SetInt($"SavedLevel_{timestamp}_Width", currentWidth);
        PlayerPrefs.SetInt($"SavedLevel_{timestamp}_Height", currentHeight);

        Debug.Log($"Level saved with timestamp: {timestamp}");
    }

    // 저장된 레벨 불러오기
    void LoadSavedLevel()
    {
        // 가장 최근 저장된 상태 불러오기
        LoadDesignerState();

        Debug.Log("Saved level loaded");
    }

    void SetupButtonEvents()
    {
        // 입력 필드 검증
        widthInput.onEndEdit.AddListener(ValidateWidthInput);
        heightInput.onEndEdit.AddListener(ValidateHeightInput);
        cellSizeInput.onEndEdit.AddListener(ValidateCellSizeInput);
        targetScoreInput.onEndEdit.AddListener(ValidateTargetScoreInput);
        maxMovesInput.onEndEdit.AddListener(ValidateMaxMovesInput);
    }

    void SetDefaultValues()
    {
        widthInput.text = defaultWidth.ToString();
        heightInput.text = defaultHeight.ToString();
        cellSizeInput.text = defaultCellSize.ToString();
        targetScoreInput.text = defaultTargetScore.ToString();
        maxMovesInput.text = defaultMaxMoves.ToString();
    }

    void SelectBlockType(int blockType)
    {
        if (selectedBlockType == blockType)
        {
            return;
        }
        selectedBlockType = blockType;

        // 모든 버튼 기본 색상으로 변경
        for (int i = 0; i < blockTypeButtons.Length; i++)
        {
            blockTypeButtons[i].GetComponent<Image>().color = Color.white;
        }
        emptyBlockButton.GetComponent<Image>().color = Color.white;

        // 선택된 버튼 하이라이트
        if (blockType == 0)
        {
            emptyBlockButton.GetComponent<Image>().color = Color.pink;
        }
        else
        {
            blockTypeButtons[blockType - 1].GetComponent<Image>().color = Color.pink;
        }
    }

    void GenerateDesignerGrid()
    {
        // 기존 그리드 삭제
        ClearGridButtons();

        // 입력값 가져오기
        if (!int.TryParse(widthInput.text, out currentWidth)) currentWidth = defaultWidth;
        if (!int.TryParse(heightInput.text, out currentHeight)) currentHeight = defaultHeight;
        if (!float.TryParse(cellSizeInput.text, out currentCellSize)) currentCellSize = defaultCellSize;

        // 값 제한
        currentWidth = Mathf.Clamp(currentWidth, 3, 15);
        currentHeight = Mathf.Clamp(currentHeight, 3, 20);
        currentCellSize = Mathf.Clamp(currentCellSize, 40f, 120f);

        // 그리드 데이터 초기화
        designerGrid = new DesignerBlock[currentWidth, currentHeight];

        // UI 그리드 생성
        CreateUIGrid();

        // 스크롤 영역 크기 조정
        AdjustScrollArea();

        Debug.Log($"Designer grid created: {currentWidth}x{currentHeight}, cell size: {currentCellSize}");
    }

    void CreateUIGrid()
    {
        // Grid Layout Group 설정
        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.cellSize = new Vector2(currentCellSize, currentCellSize);
        gridLayout.spacing = new Vector2(2f, 2f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = currentWidth;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // Content Size Fitter 추가 (자동 크기 조정)
        ContentSizeFitter contentFitter = gridContainer.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = gridContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 그리드 버튼 생성
        for (int y = currentHeight - 1; y >= 0; y--) // 위에서 아래로
        {
            for (int x = 0; x < currentWidth; x++) // 왼쪽에서 오른쪽으로
            {
                CreateGridButton(x, y);
            }
        }
    }

    void CreateGridButton(int x, int y)
    {
        // 버튼 생성
        GameObject buttonObj = new GameObject($"GridButton_{x}_{y}");
        buttonObj.transform.SetParent(gridContainer);

        // 버튼 컴포넌트 추가
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();

        // 버튼 스타일 설정
        buttonImage.color = Color.gray;
        buttonImage.sprite = null;

        // 버튼 크기 설정
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one; // 스케일 고정

        // Layout Element 추가 (크기 강제 지정)
        LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = currentCellSize;
        layoutElement.preferredHeight = currentCellSize;
        layoutElement.flexibleWidth = 0;
        layoutElement.flexibleHeight = 0;

        // 버튼 이벤트 설정
        button.onClick.AddListener(() => OnGridButtonClicked(x, y));

        // 그리드 데이터 초기화
        designerGrid[x, y] = new DesignerBlock(x, y, 0); // 0: 빈 블록

        // 버튼 리스트에 추가
        gridButtons.Add(buttonObj);
    }

    void OnGridButtonClicked(int x, int y)
    {
        if (designerGrid == null) return;

        // 그리드 데이터 업데이트
        designerGrid[x, y].blockType = selectedBlockType;

        // 버튼 시각 업데이트
        UpdateGridButtonVisual(x, y);

        Debug.Log($"Grid button clicked: ({x}, {y}) - Block type: {selectedBlockType}");
    }

    void UpdateGridButtonVisual(int x, int y)
    {
        // 해당 위치의 버튼 찾기
        int buttonIndex = (currentHeight - 1 - y) * currentWidth + x;
        if (buttonIndex >= 0 && buttonIndex < gridButtons.Count)
        {
            Image buttonImage = gridButtons[buttonIndex].GetComponent<Image>();

            // 블록 타입에 따른 색상 설정
            switch (designerGrid[x, y].blockType)
            {
                case 0: // 빈 블록
                    buttonImage.color = Color.gray;
                    break;
                case 1: // 빨강
                    buttonImage.color = Color.red;
                    break;
                case 2: // 파랑
                    buttonImage.color = Color.blue;
                    break;
                case 3: // 노랑
                    buttonImage.color = Color.yellow;
                    break;
                case 4: // 초록
                    buttonImage.color = Color.green;
                    break;
                case 5: // 보라
                    buttonImage.color = Color.magenta;
                    break;
                default:
                    buttonImage.color = Color.white;
                    break;
            }
        }
    }

    void ClearGrid()
    {
        if (designerGrid == null) return;

        // 모든 그리드를 빈 블록으로 초기화
        for (int x = 0; x < currentWidth; x++)
        {
            for (int y = 0; y < currentHeight; y++)
            {
                designerGrid[x, y].blockType = 0;
                UpdateGridButtonVisual(x, y);
            }
        }

        Debug.Log("Grid cleared");
    }

    void ClearGridButtons()
    {
        foreach (GameObject button in gridButtons)
        {
            if (button != null)
                Destroy(button);
        }
        gridButtons.Clear();
    }

    void AdjustScrollArea()
    {
        // Content 크기 조정
        //RectTransform contentRect = gridContainer.GetComponent<RectTransform>();
        //float contentHeight = currentHeight * (currentCellSize + 2f) + 20f; // 여백 포함
        //float contentWidth = currentWidth * (currentCellSize + 2f) + 20f;

        //contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);

        // 스크롤 위치 초기화
        //if (gridScrollRect != null)
        //{
        //    gridScrollRect.verticalNormalizedPosition = 1f;
        //    gridScrollRect.horizontalNormalizedPosition = 0.5f;
        //}

        // Grid Layout Group 설정 완료 후 잠깐 대기
        StartCoroutine(AdjustScrollAreaDelayed());
    }

    System.Collections.IEnumerator AdjustScrollAreaDelayed()
    {
        // 한 프레임 대기 (Layout 계산 완료 대기)
        yield return null;

        // Content 크기는 ContentSizeFitter가 자동으로 조정
        RectTransform contentRect = gridContainer.GetComponent<RectTransform>();

        // 스크롤 위치 초기화
        if (gridScrollRect != null)
        {
            gridScrollRect.verticalNormalizedPosition = 1f;
            gridScrollRect.horizontalNormalizedPosition = 0.5f;

            // Viewport 크기 확인 및 조정
            RectTransform viewport = gridScrollRect.viewport;
            if (viewport != null)
            {
                Debug.Log($"Viewport size: {viewport.rect.size}");
                Debug.Log($"Content size: {contentRect.rect.size}");
            }
        }
    }

    void TestLevel()
    {
        if (designerGrid == null)
        {
            Debug.LogWarning("Grid not generated yet!");
            return;
        }

        // 현재 작업 상태 저장
        SaveDesignerState();

        // 테스트용 스테이지 데이터 생성
        CreateTestStageData();

        // 게임 씬으로 전환
        SceneManager.LoadScene("GameScene");
    }

    void CreateTestStageData()
    {
        // 입력값 가져오기
        int targetScore = defaultTargetScore;
        int maxMoves = defaultMaxMoves;

        if (!int.TryParse(targetScoreInput.text, out targetScore)) targetScore = defaultTargetScore;
        if (!int.TryParse(maxMovesInput.text, out maxMoves)) maxMoves = defaultMaxMoves;

        // 임시 스테이지 데이터를 PlayerPrefs에 저장
        PlayerPrefs.SetInt("TestLevel_Width", currentWidth);
        PlayerPrefs.SetInt("TestLevel_Height", currentHeight);
        PlayerPrefs.SetInt("TestLevel_TargetScore", targetScore);
        PlayerPrefs.SetInt("TestLevel_MaxMoves", maxMoves);
        PlayerPrefs.SetFloat("TestLevel_CellSize", currentCellSize);

        // 그리드 패턴 저장
        string gridPattern = "";
        for (int y = 0; y < currentHeight; y++)
        {
            for (int x = 0; x < currentWidth; x++)
            {
                gridPattern += designerGrid[x, y].blockType.ToString();
                if (x < currentWidth - 1) gridPattern += ",";
            }
            if (y < currentHeight - 1) gridPattern += ";";
        }

        PlayerPrefs.SetString("TestLevel_Pattern", gridPattern);
        PlayerPrefs.SetInt("IsTestLevel", 1);

        Debug.Log($"Test level data saved: {currentWidth}x{currentHeight}, Target: {targetScore}, Moves: {maxMoves}");
        Debug.Log($"Pattern: {gridPattern}");
    }

    void BackToMenu()
    {
        // 현재 작업 상태 저장
        SaveDesignerState();

        // 메인 메뉴로 돌아가기
        SceneManager.LoadScene("MainMenu");
    }

    // 레벨 디자이너 상태 저장
    void SaveDesignerState()
    {
        if (designerGrid == null) return;

        try
        {
            // 기본 설정 저장
            PlayerPrefs.SetInt("DesignerState_Width", currentWidth);
            PlayerPrefs.SetInt("DesignerState_Height", currentHeight);
            PlayerPrefs.SetFloat("DesignerState_CellSize", currentCellSize);
            PlayerPrefs.SetString("DesignerState_TargetScore", targetScoreInput.text);
            PlayerPrefs.SetString("DesignerState_MaxMoves", maxMovesInput.text);
            PlayerPrefs.SetInt("DesignerState_SelectedBlockType", selectedBlockType);

            // 그리드 패턴 저장
            string gridPattern = "";
            for (int y = 0; y < currentHeight; y++)
            {
                for (int x = 0; x < currentWidth; x++)
                {
                    gridPattern += designerGrid[x, y].blockType.ToString();
                    if (x < currentWidth - 1) gridPattern += ",";
                }
                if (y < currentHeight - 1) gridPattern += ";";
            }

            PlayerPrefs.SetString("DesignerState_Pattern", gridPattern);
            PlayerPrefs.SetInt("DesignerState_HasData", 1); // 데이터 존재 플래그

            Debug.Log("Designer state saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save designer state: {e.Message}");
        }
    }

    // 레벨 디자이너 상태 복원
    void LoadDesignerState()
    {
        try
        {
            // 저장된 데이터가 있는지 확인
            if (PlayerPrefs.GetInt("DesignerState_HasData", 0) == 0)
            {
                Debug.Log("No saved designer state found");
                return;
            }

            // 기본 설정 복원
            int savedWidth = PlayerPrefs.GetInt("DesignerState_Width", defaultWidth);
            int savedHeight = PlayerPrefs.GetInt("DesignerState_Height", defaultHeight);
            float savedCellSize = PlayerPrefs.GetFloat("DesignerState_CellSize", defaultCellSize);
            string savedTargetScore = PlayerPrefs.GetString("DesignerState_TargetScore", defaultTargetScore.ToString());
            string savedMaxMoves = PlayerPrefs.GetString("DesignerState_MaxMoves", defaultMaxMoves.ToString());
            int savedSelectedBlockType = PlayerPrefs.GetInt("DesignerState_SelectedBlockType", 0);
            string savedPattern = PlayerPrefs.GetString("DesignerState_Pattern", "");

            // UI에 값 설정
            widthInput.text = savedWidth.ToString();
            heightInput.text = savedHeight.ToString();
            cellSizeInput.text = savedCellSize.ToString();
            targetScoreInput.text = savedTargetScore;
            maxMovesInput.text = savedMaxMoves;

            // 블록 타입 선택 복원
            SelectBlockType(savedSelectedBlockType);

            // 저장된 그리드 설정으로 그리드 생성
            currentWidth = savedWidth;
            currentHeight = savedHeight;
            currentCellSize = savedCellSize;

            // 그리드 생성
            GenerateDesignerGrid();

            // 패턴 복원
            if (!string.IsNullOrEmpty(savedPattern))
            {
                RestoreGridPattern(savedPattern);
            }

            Debug.Log("Designer state loaded successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load designer state: {e.Message}");
        }
    }

    // 그리드 패턴 복원
    void RestoreGridPattern(string pattern)
    {
        try
        {
            string[] rows = pattern.Split(';');

            for (int y = 0; y < currentHeight && y < rows.Length; y++)
            {
                if (string.IsNullOrEmpty(rows[y])) continue;

                string[] cells = rows[y].Split(',');

                for (int x = 0; x < currentWidth && x < cells.Length; x++)
                {
                    if (int.TryParse(cells[x], out int blockType))
                    {
                        if (designerGrid != null && x < designerGrid.GetLength(0) && y < designerGrid.GetLength(1))
                        {
                            designerGrid[x, y].blockType = blockType;
                            UpdateGridButtonVisual(x, y);
                        }
                    }
                }
            }

            Debug.Log("Grid pattern restored successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to restore grid pattern: {e.Message}");
        }
    }

    // 저장된 상태 삭제 (새로 시작할 때)
    public void ClearSavedState()
    {
        PlayerPrefs.DeleteKey("DesignerState_Width");
        PlayerPrefs.DeleteKey("DesignerState_Height");
        PlayerPrefs.DeleteKey("DesignerState_CellSize");
        PlayerPrefs.DeleteKey("DesignerState_TargetScore");
        PlayerPrefs.DeleteKey("DesignerState_MaxMoves");
        PlayerPrefs.DeleteKey("DesignerState_SelectedBlockType");
        PlayerPrefs.DeleteKey("DesignerState_Pattern");
        PlayerPrefs.DeleteKey("DesignerState_HasData");

        Debug.Log("Saved designer state cleared");
    }

    // 입력 검증 메서드들
    void ValidateWidthInput(string value)
    {
        if (int.TryParse(value, out int width))
        {
            width = Mathf.Clamp(width, 3, 15);
            if (width.ToString() != value)
                widthInput.text = width.ToString();
        }
    }

    void ValidateHeightInput(string value)
    {
        if (int.TryParse(value, out int height))
        {
            height = Mathf.Clamp(height, 3, 20);
            if (height.ToString() != value)
                heightInput.text = height.ToString();
        }
    }

    void ValidateCellSizeInput(string value)
    {
        if (float.TryParse(value, out float cellSize))
        {
            cellSize = Mathf.Clamp(cellSize, 40f, 120f);
            if (cellSize.ToString() != value)
                cellSizeInput.text = cellSize.ToString();
        }
    }

    void ValidateTargetScoreInput(string value)
    {
        if (int.TryParse(value, out int score))
        {
            score = Mathf.Clamp(score, 10, 9999);
            if (score.ToString() != value)
                targetScoreInput.text = score.ToString();
        }
    }

    void ValidateMaxMovesInput(string value)
    {
        if (int.TryParse(value, out int moves))
        {
            moves = Mathf.Clamp(moves, 1, 999);
            if (moves.ToString() != value)
                maxMovesInput.text = moves.ToString();
        }
    }
}

// 디자이너 그리드 데이터 클래스
[System.Serializable]
public class DesignerBlock
{
    public int x;
    public int y;
    public int blockType; // 0: 빈 블록, 1-5: 각 색상 블록

    public DesignerBlock(int x, int y, int blockType)
    {
        this.x = x;
        this.y = y;
        this.blockType = blockType;
    }
}