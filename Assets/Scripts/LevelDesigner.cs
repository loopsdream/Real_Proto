// LevelDesigner.cs - 레벨 디자인 편집기 시스템 (완성)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

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

    [Header("StageData Creation - NEW")]
    public Button createStageDataButton;  // StageData 생성 버튼
    public TMP_InputField stageNumberInput;
    public TMP_InputField stageNameInput;
    public TMP_InputField timeLimitInput;
    public TMP_Text feedbackText; // 피드백 메시지 표시

    [Header("Block Selection")]
    public Button[] blockTypeButtons;
    public Image[] blockTypeImages;
    public Button emptyBlockButton;

    [Header("Grid Container")]
    public Transform gridContainer;
    public ScrollRect gridScrollRect;

    [Header("Block Prefabs")]
    public GameObject[] blockPrefabs; // 각 색상 블록 프리팹
    public GameObject emptyBlockPrefab;

    [Header("Designer Settings")]
    public int defaultWidth = 6;
    public int defaultHeight = 8;
    public float defaultCellSize = 80f;
    public int defaultTargetScore = 100;
    public int defaultMaxMoves = 20;

    [Header("Grid Center Lines")]
    public Color centerLineColor = Color.white;
    public float centerLineWidth = 2f;

    private int currentWidth;
    private int currentHeight;
    private float currentCellSize;
    private int selectedBlockType = 0; // 0: 빈 블록, 1-5: 각 색상 블록
    private DesignerBlock[,] designerGrid;
    private List<GameObject> gridButtons = new List<GameObject>();
    private List<GameObject> centerLineObjects = new List<GameObject>(); // 중앙선 오브젝트들

    void Start()
    {
        InitializeDesigner();
        SetupButtonEvents();
        SetDefaultValues();

        // 이전에 작업하던 내용이 있다면 복원
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

        // NEW: StageData 생성 버튼
        if (createStageDataButton != null)
            createStageDataButton.onClick.AddListener(CreateStageDataFromCurrentDesign);
    }

    // 새 레벨 시작 (현재 작업 초기화)
    void StartNewLevel()
    {
        if (designerGrid != null)
        {
            Debug.Log("Starting new level (clearing current work)");
        }

        ClearSavedState();
        SetDefaultValues();
        ClearGridButtons();
        designerGrid = null;
        SelectBlockType(0);

        Debug.Log("New level started");
    }

    // 현재 레벨 저장
    void SaveCurrentLevel()
    {
        if (designerGrid == null)
        {
            Debug.LogWarning("No level to save!");
            return;
        }

        SaveDesignerState();

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        PlayerPrefs.SetString($"SavedLevel_{timestamp}_Pattern", PlayerPrefs.GetString("DesignerState_Pattern"));
        PlayerPrefs.SetInt($"SavedLevel_{timestamp}_Width", currentWidth);
        PlayerPrefs.SetInt($"SavedLevel_{timestamp}_Height", currentHeight);

        Debug.Log($"Level saved with timestamp: {timestamp}");
    }

    // 저장된 레벨 불러오기
    void LoadSavedLevel()
    {
        LoadDesignerState();
        Debug.Log("Saved level loaded");
    }

    // NEW: StageData 생성 기능
    void CreateStageDataFromCurrentDesign()
    {
        #if UNITY_EDITOR
        if (designerGrid == null)
        {
            ShowFeedback("그리드가 생성되지 않았습니다!", Color.red);
            return;
        }

        try
        {
            StageData newStageData = CreateStageDataAsset();
            
            if (newStageData != null)
            {
                ShowFeedback($"StageData '{newStageData.name}' 생성 완료!", Color.green);
                
                // 스테이지 번호 자동 증가
                int nextStageNumber = GetNextStageNumber() + 1;
                if (stageNumberInput)
                    stageNumberInput.text = nextStageNumber.ToString();
                if (stageNameInput)
                    stageNameInput.text = $"Stage {nextStageNumber}";
            }
        }
        catch (System.Exception e)
        {
            ShowFeedback($"StageData 생성 실패: {e.Message}", Color.red);
            Debug.LogError($"StageData creation failed: {e}");
        }
        #else
        ShowFeedback("StageData 생성은 에디터에서만 가능합니다.", Color.yellow);
        #endif
    }

    #if UNITY_EDITOR
    private StageData CreateStageDataAsset()
    {
        StageData stageData = ScriptableObject.CreateInstance<StageData>();
        
        int stageNumber = GetStageNumber();
        string stageName = GetStageName();
        float timeLimit = GetTimeLimit();
        
        stageData.stageNumber = stageNumber;
        stageData.stageName = stageName;
        stageData.gridWidth = currentWidth;
        stageData.gridHeight = currentHeight;
        stageData.blockPattern = GetCurrentBlockPattern();
        stageData.timeLimit = timeLimit;
        stageData.coinReward = CalculateCoinReward(stageNumber);
        stageData.experienceReward = 10;
        stageData.difficultyLevel = CalculateDifficulty(stageNumber);
        stageData.allowColorTransform = true;
        stageData.showHints = true;
        
        string fileName = $"Stage_{stageNumber:D3}.asset";
        string assetPath = $"Assets/StageData/{fileName}";
        
        string directory = Path.GetDirectoryName(assetPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        if (File.Exists(assetPath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "파일 덮어쓰기", 
                $"{fileName}이 이미 존재합니다. 덮어쓰시겠습니까?", 
                "덮어쓰기", "취소");
                
            if (!overwrite)
            {
                return null;
            }
        }
        
        AssetDatabase.CreateAsset(stageData, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = stageData;
        
        Debug.Log($"StageData created: {assetPath}");
        return stageData;
    }
    #endif

    private int[] GetCurrentBlockPattern()
    {
        int width = currentWidth;
        int height = currentHeight;
        int[] pattern = new int[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (designerGrid != null && x < designerGrid.GetLength(0) && y < designerGrid.GetLength(1))
                {
                    pattern[y * width + x] = designerGrid[x, y].blockType;
                }
                else
                {
                    pattern[y * width + x] = 0;
                }
            }
        }
        
        return pattern;
    }

    private int GetStageNumber()
    {
        if (stageNumberInput && int.TryParse(stageNumberInput.text, out int number))
            return number;
        return GetNextStageNumber();
    }
    
    private string GetStageName()
    {
        if (stageNameInput && !string.IsNullOrEmpty(stageNameInput.text))
            return stageNameInput.text;
        return $"Stage {GetStageNumber()}";
    }
    
    private float GetTimeLimit()
    {
        if (timeLimitInput && float.TryParse(timeLimitInput.text, out float time))
            return time;
        return 180f;
    }
    
    private int GetNextStageNumber()
    {
        #if UNITY_EDITOR
        string[] stageDataGuids = AssetDatabase.FindAssets("t:StageData", new[] { "Assets/StageData" });
        int maxStageNumber = 0;
        
        foreach (string guid in stageDataGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageData stageData = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stageData != null && stageData.stageNumber > maxStageNumber)
            {
                maxStageNumber = stageData.stageNumber;
            }
        }
        
        return maxStageNumber + 1;
        #else
        return 1;
        #endif
    }
    
    private int CalculateCoinReward(int stageNumber)
    {
        return Mathf.Max(50, 100 + (stageNumber * 10));
    }
    
    private int CalculateDifficulty(int stageNumber)
    {
        if (stageNumber <= 10) return 1;
        if (stageNumber <= 30) return 2;
        if (stageNumber <= 60) return 3;
        if (stageNumber <= 100) return 4;
        return 5;
    }
    
    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText)
        {
            feedbackText.text = message;
            feedbackText.color = color;
            
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 3f);
        }
        
        Debug.Log(message);
    }
    
    private void ClearFeedback()
    {
        if (feedbackText)
        {
            feedbackText.text = "";
        }
    }

    void SetupButtonEvents()
    {
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

        if (stageNumberInput)
            stageNumberInput.text = GetNextStageNumber().ToString();
        if (stageNameInput)
            stageNameInput.text = $"Stage {GetNextStageNumber()}";
        if (timeLimitInput)
            timeLimitInput.text = "180";
    }

    void SelectBlockType(int blockType)
    {
        if (selectedBlockType == blockType)
        {
            return;
        }
        selectedBlockType = blockType;

        for (int i = 0; i < blockTypeButtons.Length; i++)
        {
            blockTypeButtons[i].GetComponent<Image>().color = Color.white;
        }
        emptyBlockButton.GetComponent<Image>().color = Color.white;

        if (blockType == 0)
        {
            emptyBlockButton.GetComponent<Image>().color = Color.cyan;
        }
        else
        {
            blockTypeButtons[blockType - 1].GetComponent<Image>().color = Color.cyan;
        }
    }

    void GenerateDesignerGrid()
    {
        ClearGridButtons();
        ClearCenterLines(); // 기존 중앙선 제거

        if (!int.TryParse(widthInput.text, out currentWidth)) currentWidth = defaultWidth;
        if (!int.TryParse(heightInput.text, out currentHeight)) currentHeight = defaultHeight;
        if (!float.TryParse(cellSizeInput.text, out currentCellSize)) currentCellSize = defaultCellSize;

        currentWidth = Mathf.Clamp(currentWidth, 3, 15);
        currentHeight = Mathf.Clamp(currentHeight, 3, 20);
        currentCellSize = Mathf.Clamp(currentCellSize, 40f, 120f);

        designerGrid = new DesignerBlock[currentWidth, currentHeight];

        CreateUIGrid();
        CreateCenterLines(); // 중앙선 생성
        AdjustScrollArea();

        Debug.Log($"Designer grid created: {currentWidth}x{currentHeight}, cell size: {currentCellSize}");
    }

    void CreateUIGrid()
    {
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

        ContentSizeFitter contentFitter = gridContainer.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = gridContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 정확히 설정된 개수만큼만 생성
        for (int y = currentHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < currentWidth; x++)
            {
                CreateGridButton(x, y);
            }
        }

        Debug.Log($"Created exactly {currentWidth * currentHeight} grid buttons");
    }

    void CreateGridButton(int x, int y)
    {
        GameObject buttonObj = new GameObject($"GridButton_{x}_{y}");
        buttonObj.transform.SetParent(gridContainer);

        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();

        buttonImage.color = Color.gray;
        buttonImage.sprite = null;

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = currentCellSize;
        layoutElement.preferredHeight = currentCellSize;
        layoutElement.flexibleWidth = 0;
        layoutElement.flexibleHeight = 0;

        button.onClick.AddListener(() => OnGridButtonClicked(x, y));

        designerGrid[x, y] = new DesignerBlock(x, y, 0);

        gridButtons.Add(buttonObj);
    }

    void OnGridButtonClicked(int x, int y)
    {
        if (designerGrid == null) return;

        designerGrid[x, y].blockType = selectedBlockType;
        UpdateGridButtonVisual(x, y);

        Debug.Log($"Grid button clicked: ({x}, {y}) - Block type: {selectedBlockType}");
    }

    void UpdateGridButtonVisual(int x, int y)
    {
        int buttonIndex = (currentHeight - 1 - y) * currentWidth + x;
        if (buttonIndex >= 0 && buttonIndex < gridButtons.Count)
        {
            Image buttonImage = gridButtons[buttonIndex].GetComponent<Image>();

            switch (designerGrid[x, y].blockType)
            {
                case 0: buttonImage.color = Color.gray; break;
                case 1: buttonImage.color = Color.red; break;
                case 2: buttonImage.color = Color.blue; break;
                case 3: buttonImage.color = Color.yellow; break;
                case 4: buttonImage.color = Color.green; break;
                case 5: buttonImage.color = Color.magenta; break;
                default: buttonImage.color = Color.white; break;
            }
        }
    }

    void ClearGrid()
    {
        if (designerGrid == null) return;

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
        StartCoroutine(AdjustScrollAreaDelayed());
    }

    System.Collections.IEnumerator AdjustScrollAreaDelayed()
    {
        yield return null;

        RectTransform contentRect = gridContainer.GetComponent<RectTransform>();

        if (gridScrollRect != null)
        {
            gridScrollRect.verticalNormalizedPosition = 1f;
            gridScrollRect.horizontalNormalizedPosition = 0.5f;

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

        SaveDesignerState();
        CreateTestStageData();
        SceneManager.LoadScene("StageModeScene");
    }

    void CreateTestStageData()
    {
        int targetScore = defaultTargetScore;
        int maxMoves = defaultMaxMoves;

        if (!int.TryParse(targetScoreInput.text, out targetScore)) targetScore = defaultTargetScore;
        if (!int.TryParse(maxMovesInput.text, out maxMoves)) maxMoves = defaultMaxMoves;

        PlayerPrefs.SetInt("TestLevel_Width", currentWidth);
        PlayerPrefs.SetInt("TestLevel_Height", currentHeight);
        PlayerPrefs.SetInt("TestLevel_TargetScore", targetScore);
        PlayerPrefs.SetInt("TestLevel_MaxMoves", maxMoves);
        PlayerPrefs.SetFloat("TestLevel_CellSize", currentCellSize);

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
        SaveDesignerState();
        SceneManager.LoadScene("LobbyScene");
    }

    void SaveDesignerState()
    {
        if (designerGrid == null) return;

        try
        {
            PlayerPrefs.SetInt("DesignerState_Width", currentWidth);
            PlayerPrefs.SetInt("DesignerState_Height", currentHeight);
            PlayerPrefs.SetFloat("DesignerState_CellSize", currentCellSize);
            PlayerPrefs.SetString("DesignerState_TargetScore", targetScoreInput.text);
            PlayerPrefs.SetString("DesignerState_MaxMoves", maxMovesInput.text);
            PlayerPrefs.SetInt("DesignerState_SelectedBlockType", selectedBlockType);

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
            PlayerPrefs.SetInt("DesignerState_HasData", 1);

            Debug.Log("Designer state saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save designer state: {e.Message}");
        }
    }

    void LoadDesignerState()
    {
        try
        {
            if (PlayerPrefs.GetInt("DesignerState_HasData", 0) == 0)
            {
                Debug.Log("No saved designer state found");
                return;
            }

            int savedWidth = PlayerPrefs.GetInt("DesignerState_Width", defaultWidth);
            int savedHeight = PlayerPrefs.GetInt("DesignerState_Height", defaultHeight);
            float savedCellSize = PlayerPrefs.GetFloat("DesignerState_CellSize", defaultCellSize);
            string savedTargetScore = PlayerPrefs.GetString("DesignerState_TargetScore", defaultTargetScore.ToString());
            string savedMaxMoves = PlayerPrefs.GetString("DesignerState_MaxMoves", defaultMaxMoves.ToString());
            int savedSelectedBlockType = PlayerPrefs.GetInt("DesignerState_SelectedBlockType", 0);
            string savedPattern = PlayerPrefs.GetString("DesignerState_Pattern", "");

            widthInput.text = savedWidth.ToString();
            heightInput.text = savedHeight.ToString();
            cellSizeInput.text = savedCellSize.ToString();
            targetScoreInput.text = savedTargetScore;
            maxMovesInput.text = savedMaxMoves;

            SelectBlockType(savedSelectedBlockType);

            currentWidth = savedWidth;
            currentHeight = savedHeight;
            currentCellSize = savedCellSize;

            GenerateDesignerGrid();

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

    // 중앙선 생성
    void CreateCenterLines()
    {
        // 한 프레임 기다린 후 실행
        Invoke(nameof(CreateCenterLinesDelayed), 0.1f);
    }

    void CreateCenterLinesDelayed()
    {
        // 항상 십자선 표시 (홀수/짝수 관계없이)
        CreateHorizontalCenterLine();
        CreateVerticalCenterLine();

        Debug.Log($"Center lines created for {currentWidth}x{currentHeight} grid");
    }

    void CreateHorizontalCenterLine()
    {
        GameObject horizontalLine = new GameObject("HorizontalCenterLine");
        horizontalLine.transform.SetParent(gridContainer, false);

        RectTransform lineRect = horizontalLine.AddComponent<RectTransform>();
        Image lineImage = horizontalLine.AddComponent<Image>();

        lineImage.color = centerLineColor;
        lineImage.raycastTarget = false;

        // GridLayoutGroup에서 완전히 제외
        LayoutElement layoutElement = horizontalLine.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        // 전체 그리드의 실제 폭 계산
        float totalWidth = currentWidth * currentCellSize + (currentWidth - 1) * 2f;
        lineRect.sizeDelta = new Vector2(totalWidth, centerLineWidth);

        // 정확히 중앙에 위치 (Y축 중앙)
        lineRect.anchoredPosition = new Vector2(0, 0);
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);

        horizontalLine.transform.SetAsLastSibling();
        centerLineObjects.Add(horizontalLine);
    }

    void CreateVerticalCenterLine()
    {
        GameObject verticalLine = new GameObject("VerticalCenterLine");
        verticalLine.transform.SetParent(gridContainer, false);

        RectTransform lineRect = verticalLine.AddComponent<RectTransform>();
        Image lineImage = verticalLine.AddComponent<Image>();

        lineImage.color = centerLineColor;
        lineImage.raycastTarget = false;

        // GridLayoutGroup에서 완전히 제외
        LayoutElement layoutElement = verticalLine.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        // 전체 그리드의 실제 높이 계산
        float totalHeight = currentHeight * currentCellSize + (currentHeight - 1) * 2f;
        lineRect.sizeDelta = new Vector2(centerLineWidth, totalHeight);

        // 정확히 중앙에 위치 (X축 중앙)
        lineRect.anchoredPosition = new Vector2(0, 0);
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);

        verticalLine.transform.SetAsLastSibling();
        centerLineObjects.Add(verticalLine);
    }

    // 중앙선 제거
    void ClearCenterLines()
    {
        foreach (GameObject line in centerLineObjects)
        {
            if (line != null)
                DestroyImmediate(line);
        }
        centerLineObjects.Clear();
    }

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
