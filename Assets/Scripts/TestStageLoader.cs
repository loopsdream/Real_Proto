// TestStageLoader.cs - ���� �����̳ʿ��� ���� ���������� �ε��ϴ� �ý���
using UnityEngine;

public class TestStageLoader : MonoBehaviour
{
    [Header("Grid Manager")]
    public StageGridManager gridManager;  // GridManagerRefactored �� StageGridManager�� ����

    void Start()
    {
        // �׽�Ʈ �������� Ȯ��
        if (PlayerPrefs.GetInt("IsTestLevel", 0) == 1)
        {
            LoadTestLevel();
        }
    }

    void LoadTestLevel()
    {
        try
        {
            Debug.Log("=== LoadTestLevel Started ===");
            Debug.Log($"IsTestLevel flag: {PlayerPrefs.GetInt("IsTestLevel", 0)}");

            // �׽�Ʈ ���� ������ �ε�
            int width = PlayerPrefs.GetInt("TestLevel_Width", 6);
            int height = PlayerPrefs.GetInt("TestLevel_Height", 8);
            int targetScore = PlayerPrefs.GetInt("TestLevel_TargetScore", 100);
            int maxMoves = PlayerPrefs.GetInt("TestLevel_MaxMoves", 20);
            float cellSize = PlayerPrefs.GetFloat("TestLevel_CellSize", 80f);
            string pattern = PlayerPrefs.GetString("TestLevel_Pattern", "");

            Debug.Log($"Loaded PlayerPrefs - Width: {width}, Height: {height}, TargetScore: {targetScore}");
            Debug.Log($"Pattern: {pattern}");

            // GridManager üũ
            if (gridManager == null)
            {
                Debug.LogError("GridManager is null! Please assign GridManager in TestStageLoader Inspector.");
                return;
            }

            Debug.Log($"GridManager found: {gridManager.name}");

            // GridManager ���� ������Ʈ
            gridManager.width = width;
            gridManager.height = height;

            // �߿�: cellSize�� �ùٸ��� ����
            float worldCellSize = cellSize / 100f; // �ȼ��� ���� �������� ��ȯ
            gridManager.cellSize = worldCellSize;

            Debug.Log($"GridManager settings updated - cellSize: {worldCellSize} (from {cellSize} pixels)");

            Debug.Log("GridManager settings updated");

            // �׽�Ʈ �������� ������ ����
            TestStageData testStage = CreateTestStageData(width, height, targetScore, maxMoves, pattern);

            if (testStage == null)
            {
                Debug.LogError("Failed to create test stage data");
                return;
            }

            Debug.Log($"TestStageData created: {testStage.stageName}");

            // StageManager�� �׽�Ʈ �������� ����
            StageManager stageManager = FindFirstObjectByType<StageManager>();
            if (stageManager != null)
            {
                Debug.Log($"StageManager found: {stageManager.name}");
                Debug.Log($"Calling LoadTestStage with pattern length: {testStage.pattern.GetLength(0)}x{testStage.pattern.GetLength(1)}");
                stageManager.LoadTestStage(testStage);
            }
            else
            {
                Debug.LogWarning("StageManager not found, applying directly to GridManager");
                // StageManager�� ������ ���� GridManager�� ����
                ApplyTestStageToGridManager(testStage);
            }

            // �׽�Ʈ ���� �÷��� ����
            PlayerPrefs.SetInt("IsTestLevel", 0);

            Debug.Log("=== Test level loaded successfully ===");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load test level: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");

            // ���� �� �⺻ �������� �ε�
            PlayerPrefs.SetInt("IsTestLevel", 0);
        }
    }

    TestStageData CreateTestStageData(int width, int height, int targetScore, int maxMoves, string pattern)
    {
        try
        {
            Debug.Log($"Creating TestStageData: {width}x{height}");

            TestStageData testStage = new TestStageData();
            testStage.width = width;
            testStage.height = height;
            testStage.targetScore = targetScore;
            testStage.maxMoves = maxMoves;
            testStage.stageNumber = 999; // �׽�Ʈ �������� ��ȣ
            testStage.stageName = "Test Level";

            // ���� �Ľ�
            if (!string.IsNullOrEmpty(pattern))
            {
                Debug.Log($"Parsing pattern: {pattern}");
                testStage.pattern = ParsePattern(pattern, width, height);
            }
            else
            {
                Debug.LogWarning("Pattern is empty, creating default pattern");
                // �⺻ ���� ���� (��� �� ���)
                testStage.pattern = new int[width, height];
            }

            // ������ ����� �����Ǿ����� Ȯ��
            if (testStage.pattern == null)
            {
                Debug.LogError("Pattern is null after creation!");
                testStage.pattern = new int[width, height]; // �� �������� ��ü
            }

            Debug.Log($"TestStageData created successfully");
            return testStage;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating TestStageData: {e.Message}");
            return null;
        }
    }

    int[,] ParsePattern(string pattern, int width, int height)
    {
        int[,] result = new int[width, height];

        try
        {
            Debug.Log($"Parsing pattern for {width}x{height} grid");

            if (string.IsNullOrEmpty(pattern))
            {
                Debug.LogWarning("Pattern string is empty");
                return result; // ��� ���� 0�� �迭 ��ȯ
            }

            string[] rows = pattern.Split(';');
            Debug.Log($"Pattern has {rows.Length} rows, expected {height}");

            for (int y = 0; y < height && y < rows.Length; y++)
            {
                if (string.IsNullOrEmpty(rows[y]))
                {
                    Debug.LogWarning($"Row {y} is empty");
                    continue;
                }

                string[] cells = rows[y].Split(',');
                Debug.Log($"Row {y} has {cells.Length} cells, expected {width}");

                for (int x = 0; x < width && x < cells.Length; x++)
                {
                    if (int.TryParse(cells[x], out int blockType))
                    {
                        result[x, y] = blockType;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to parse cell [{x},{y}]: '{cells[x]}'");
                        result[x, y] = 0; // �⺻��: �� ���
                    }
                }
            }

            Debug.Log("Pattern parsed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse pattern: {e.Message}");
            Debug.LogError($"Pattern string: '{pattern}'");

            // ���� �߻� �� �� ���� ��ȯ
            result = new int[width, height];
        }

        return result;
    }

    void ApplyTestStageToGridManager(TestStageData testStage)
    {
        if (gridManager == null)
        {
            Debug.LogError("StageGridManager is null in ApplyTestStageToGridManager!");
            return;
        }

        if (testStage == null)
        {
            Debug.LogError("TestStage is null in ApplyTestStageToGridManager!");
            return;
        }

        try
        {
            Debug.Log($"Applying test stage to GridManager: {testStage.width}x{testStage.height}");

            // GridManager ���� ����
            gridManager.width = testStage.width;
            gridManager.height = testStage.height;

            // ���� �׸��� ���� �� ���� ����
            gridManager.ClearGrid();
            gridManager.InitializeGridWithPattern(testStage.pattern);

            Debug.Log($"Test stage applied to GridManager successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error applying test stage to GridManager: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }
}

// �׽�Ʈ �������� ������ Ŭ����
[System.Serializable]
public class TestStageData
{
    public int stageNumber;
    public string stageName;
    public int width;
    public int height;
    public int targetScore;
    public int maxMoves;
    public int[,] pattern;

    public TestStageData()
    {
        stageNumber = 999;
        stageName = "Test Level";
        width = 6;
        height = 8;
        targetScore = 100;
        maxMoves = 20;
        pattern = new int[width, height];
    }
}