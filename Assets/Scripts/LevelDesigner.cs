// LevelDesigner.cs - �ΰ��� ���� ������ �ý���
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
    public Button newLevelButton;    // �� ���� ���� ��ư
    public Button saveLevelButton;   // ���� ���� ��ư
    public Button loadLevelButton;   // ���� �ҷ����� ��ư

    [Header("Block Selection")]
    public Button[] blockTypeButtons;
    public Image[] blockTypeImages;
    public Button emptyBlockButton;

    [Header("Grid Container")]
    public Transform gridContainer;
    public ScrollRect gridScrollRect;

    [Header("Block Prefabs")]
    public GameObject[] blockPrefabs; // �� ���� ��� ������
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
    private int selectedBlockType = 0; // 0: �� ���, 1-5: �� ���� ���
    private DesignerBlock[,] designerGrid;
    private List<GameObject> gridButtons = new List<GameObject>();

    void Start()
    {
        InitializeDesigner();
        SetupButtonEvents();
        SetDefaultValues();

        // ������ �۾��ϴ� ������ ����
        LoadDesignerState();
    }

    void InitializeDesigner()
    {
        // ��ư �̺�Ʈ ����
        generateGridButton.onClick.AddListener(GenerateDesignerGrid);
        testLevelButton.onClick.AddListener(TestLevel);
        clearGridButton.onClick.AddListener(ClearGrid);
        backButton.onClick.AddListener(BackToMenu);

        // ��� Ÿ�� ���� ��ư ����
        for (int i = 0; i < blockTypeButtons.Length; i++)
        {
            int blockIndex = i;
            blockTypeButtons[i].onClick.AddListener(() => SelectBlockType(blockIndex + 1));
        }

        emptyBlockButton.onClick.AddListener(() => SelectBlockType(0));

        // �⺻ ����: �� ���
        SelectBlockType(0);

        // �߰� ��ư �̺�Ʈ ����
        if (newLevelButton != null)
            newLevelButton.onClick.AddListener(StartNewLevel);

        if (saveLevelButton != null)
            saveLevelButton.onClick.AddListener(SaveCurrentLevel);

        if (loadLevelButton != null)
            loadLevelButton.onClick.AddListener(LoadSavedLevel);
    }

    // �� ���� ���� (���� �۾� ����)
    void StartNewLevel()
    {
        // Ȯ�� ��ȭ���� ǥ�� (������ ���)
        if (designerGrid != null)
        {
            Debug.Log("Starting new level (clearing current work)");
        }

        // ����� ���� ����
        ClearSavedState();

        // UI �ʱ�ȭ
        SetDefaultValues();

        // ���� �׸��� ����
        ClearGridButtons();
        designerGrid = null;

        // �⺻ ��� Ÿ�� ����
        SelectBlockType(0);

        Debug.Log("New level started");
    }

    // ���� ���� ���� (PlayerPrefs �ܿ� ���Ϸε� ���� ����)
    void SaveCurrentLevel()
    {
        if (designerGrid == null)
        {
            Debug.LogWarning("No level to save!");
            return;
        }

        // ���� ���¸� ���� ���Կ� ����
        SaveDesignerState();

        // �߰��� Ÿ�ӽ������� �Բ� ����
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        PlayerPrefs.SetString($"SavedLevel_{timestamp}_Pattern", PlayerPrefs.GetString("DesignerState_Pattern"));
        PlayerPrefs.SetInt($"SavedLevel_{timestamp}_Width", currentWidth);
        PlayerPrefs.SetInt($"SavedLevel_{timestamp}_Height", currentHeight);

        Debug.Log($"Level saved with timestamp: {timestamp}");
    }

    // ����� ���� �ҷ�����
    void LoadSavedLevel()
    {
        // ���� �ֱ� ����� ���� �ҷ�����
        LoadDesignerState();

        Debug.Log("Saved level loaded");
    }

    void SetupButtonEvents()
    {
        // �Է� �ʵ� ����
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

        // ��� ��ư �⺻ �������� ����
        for (int i = 0; i < blockTypeButtons.Length; i++)
        {
            blockTypeButtons[i].GetComponent<Image>().color = Color.white;
        }
        emptyBlockButton.GetComponent<Image>().color = Color.white;

        // ���õ� ��ư ���̶���Ʈ
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
        // ���� �׸��� ����
        ClearGridButtons();

        // �Է°� ��������
        if (!int.TryParse(widthInput.text, out currentWidth)) currentWidth = defaultWidth;
        if (!int.TryParse(heightInput.text, out currentHeight)) currentHeight = defaultHeight;
        if (!float.TryParse(cellSizeInput.text, out currentCellSize)) currentCellSize = defaultCellSize;

        // �� ����
        currentWidth = Mathf.Clamp(currentWidth, 3, 15);
        currentHeight = Mathf.Clamp(currentHeight, 3, 20);
        currentCellSize = Mathf.Clamp(currentCellSize, 40f, 120f);

        // �׸��� ������ �ʱ�ȭ
        designerGrid = new DesignerBlock[currentWidth, currentHeight];

        // UI �׸��� ����
        CreateUIGrid();

        // ��ũ�� ���� ũ�� ����
        AdjustScrollArea();

        Debug.Log($"Designer grid created: {currentWidth}x{currentHeight}, cell size: {currentCellSize}");
    }

    void CreateUIGrid()
    {
        // Grid Layout Group ����
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

        // Content Size Fitter �߰� (�ڵ� ũ�� ����)
        ContentSizeFitter contentFitter = gridContainer.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = gridContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // �׸��� ��ư ����
        for (int y = currentHeight - 1; y >= 0; y--) // ������ �Ʒ���
        {
            for (int x = 0; x < currentWidth; x++) // ���ʿ��� ����������
            {
                CreateGridButton(x, y);
            }
        }
    }

    void CreateGridButton(int x, int y)
    {
        // ��ư ����
        GameObject buttonObj = new GameObject($"GridButton_{x}_{y}");
        buttonObj.transform.SetParent(gridContainer);

        // ��ư ������Ʈ �߰�
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();

        // ��ư ��Ÿ�� ����
        buttonImage.color = Color.gray;
        buttonImage.sprite = null;

        // ��ư ũ�� ����
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one; // ������ ����

        // Layout Element �߰� (ũ�� ���� ����)
        LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = currentCellSize;
        layoutElement.preferredHeight = currentCellSize;
        layoutElement.flexibleWidth = 0;
        layoutElement.flexibleHeight = 0;

        // ��ư �̺�Ʈ ����
        button.onClick.AddListener(() => OnGridButtonClicked(x, y));

        // �׸��� ������ �ʱ�ȭ
        designerGrid[x, y] = new DesignerBlock(x, y, 0); // 0: �� ���

        // ��ư ����Ʈ�� �߰�
        gridButtons.Add(buttonObj);
    }

    void OnGridButtonClicked(int x, int y)
    {
        if (designerGrid == null) return;

        // �׸��� ������ ������Ʈ
        designerGrid[x, y].blockType = selectedBlockType;

        // ��ư �ð� ������Ʈ
        UpdateGridButtonVisual(x, y);

        Debug.Log($"Grid button clicked: ({x}, {y}) - Block type: {selectedBlockType}");
    }

    void UpdateGridButtonVisual(int x, int y)
    {
        // �ش� ��ġ�� ��ư ã��
        int buttonIndex = (currentHeight - 1 - y) * currentWidth + x;
        if (buttonIndex >= 0 && buttonIndex < gridButtons.Count)
        {
            Image buttonImage = gridButtons[buttonIndex].GetComponent<Image>();

            // ��� Ÿ�Կ� ���� ���� ����
            switch (designerGrid[x, y].blockType)
            {
                case 0: // �� ���
                    buttonImage.color = Color.gray;
                    break;
                case 1: // ����
                    buttonImage.color = Color.red;
                    break;
                case 2: // �Ķ�
                    buttonImage.color = Color.blue;
                    break;
                case 3: // ���
                    buttonImage.color = Color.yellow;
                    break;
                case 4: // �ʷ�
                    buttonImage.color = Color.green;
                    break;
                case 5: // ����
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

        // ��� �׸��带 �� ������� �ʱ�ȭ
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
        // Content ũ�� ����
        //RectTransform contentRect = gridContainer.GetComponent<RectTransform>();
        //float contentHeight = currentHeight * (currentCellSize + 2f) + 20f; // ���� ����
        //float contentWidth = currentWidth * (currentCellSize + 2f) + 20f;

        //contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);

        // ��ũ�� ��ġ �ʱ�ȭ
        //if (gridScrollRect != null)
        //{
        //    gridScrollRect.verticalNormalizedPosition = 1f;
        //    gridScrollRect.horizontalNormalizedPosition = 0.5f;
        //}

        // Grid Layout Group ���� �Ϸ� �� ��� ���
        StartCoroutine(AdjustScrollAreaDelayed());
    }

    System.Collections.IEnumerator AdjustScrollAreaDelayed()
    {
        // �� ������ ��� (Layout ��� �Ϸ� ���)
        yield return null;

        // Content ũ��� ContentSizeFitter�� �ڵ����� ����
        RectTransform contentRect = gridContainer.GetComponent<RectTransform>();

        // ��ũ�� ��ġ �ʱ�ȭ
        if (gridScrollRect != null)
        {
            gridScrollRect.verticalNormalizedPosition = 1f;
            gridScrollRect.horizontalNormalizedPosition = 0.5f;

            // Viewport ũ�� Ȯ�� �� ����
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

        // ���� �۾� ���� ����
        SaveDesignerState();

        // �׽�Ʈ�� �������� ������ ����
        CreateTestStageData();

        // ���� ������ ��ȯ
        SceneManager.LoadScene("GameScene");
    }

    void CreateTestStageData()
    {
        // �Է°� ��������
        int targetScore = defaultTargetScore;
        int maxMoves = defaultMaxMoves;

        if (!int.TryParse(targetScoreInput.text, out targetScore)) targetScore = defaultTargetScore;
        if (!int.TryParse(maxMovesInput.text, out maxMoves)) maxMoves = defaultMaxMoves;

        // �ӽ� �������� �����͸� PlayerPrefs�� ����
        PlayerPrefs.SetInt("TestLevel_Width", currentWidth);
        PlayerPrefs.SetInt("TestLevel_Height", currentHeight);
        PlayerPrefs.SetInt("TestLevel_TargetScore", targetScore);
        PlayerPrefs.SetInt("TestLevel_MaxMoves", maxMoves);
        PlayerPrefs.SetFloat("TestLevel_CellSize", currentCellSize);

        // �׸��� ���� ����
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
        // ���� �۾� ���� ����
        SaveDesignerState();

        // ���� �޴��� ���ư���
        SceneManager.LoadScene("MainMenu");
    }

    // ���� �����̳� ���� ����
    void SaveDesignerState()
    {
        if (designerGrid == null) return;

        try
        {
            // �⺻ ���� ����
            PlayerPrefs.SetInt("DesignerState_Width", currentWidth);
            PlayerPrefs.SetInt("DesignerState_Height", currentHeight);
            PlayerPrefs.SetFloat("DesignerState_CellSize", currentCellSize);
            PlayerPrefs.SetString("DesignerState_TargetScore", targetScoreInput.text);
            PlayerPrefs.SetString("DesignerState_MaxMoves", maxMovesInput.text);
            PlayerPrefs.SetInt("DesignerState_SelectedBlockType", selectedBlockType);

            // �׸��� ���� ����
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
            PlayerPrefs.SetInt("DesignerState_HasData", 1); // ������ ���� �÷���

            Debug.Log("Designer state saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save designer state: {e.Message}");
        }
    }

    // ���� �����̳� ���� ����
    void LoadDesignerState()
    {
        try
        {
            // ����� �����Ͱ� �ִ��� Ȯ��
            if (PlayerPrefs.GetInt("DesignerState_HasData", 0) == 0)
            {
                Debug.Log("No saved designer state found");
                return;
            }

            // �⺻ ���� ����
            int savedWidth = PlayerPrefs.GetInt("DesignerState_Width", defaultWidth);
            int savedHeight = PlayerPrefs.GetInt("DesignerState_Height", defaultHeight);
            float savedCellSize = PlayerPrefs.GetFloat("DesignerState_CellSize", defaultCellSize);
            string savedTargetScore = PlayerPrefs.GetString("DesignerState_TargetScore", defaultTargetScore.ToString());
            string savedMaxMoves = PlayerPrefs.GetString("DesignerState_MaxMoves", defaultMaxMoves.ToString());
            int savedSelectedBlockType = PlayerPrefs.GetInt("DesignerState_SelectedBlockType", 0);
            string savedPattern = PlayerPrefs.GetString("DesignerState_Pattern", "");

            // UI�� �� ����
            widthInput.text = savedWidth.ToString();
            heightInput.text = savedHeight.ToString();
            cellSizeInput.text = savedCellSize.ToString();
            targetScoreInput.text = savedTargetScore;
            maxMovesInput.text = savedMaxMoves;

            // ��� Ÿ�� ���� ����
            SelectBlockType(savedSelectedBlockType);

            // ����� �׸��� �������� �׸��� ����
            currentWidth = savedWidth;
            currentHeight = savedHeight;
            currentCellSize = savedCellSize;

            // �׸��� ����
            GenerateDesignerGrid();

            // ���� ����
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

    // �׸��� ���� ����
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

    // ����� ���� ���� (���� ������ ��)
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

    // �Է� ���� �޼����
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

// �����̳� �׸��� ������ Ŭ����
[System.Serializable]
public class DesignerBlock
{
    public int x;
    public int y;
    public int blockType; // 0: �� ���, 1-5: �� ���� ���

    public DesignerBlock(int x, int y, int blockType)
    {
        this.x = x;
        this.y = y;
        this.blockType = blockType;
    }
}