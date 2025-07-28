// StageManager.cs - �������� ���� �� ���ο� Ŭ���� ���� ����
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [Header("Stage Data")]
    public List<StageData> allStages = new List<StageData>();
    public StageData currentStage;

    [Header("UI References")]
    public TextMeshProUGUI stageNumberText;
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI stageDescriptionText;
    public TextMeshProUGUI movesLeftText;
    public TextMeshProUGUI timerText;
    public Button nextStageButton;
    public Button restartStageButton;
    public GameObject stageCompletePanel;

    [Header("Game References")]
    public StageGridManager gridManager;

    [Header("Stage Failure Detection")]
    public StageGridManager gridManagerRef; // GridManager ����
    public MatchingSystem matchingSystemRef; // MatchingSystem ����

    [Header("Game State")]
    private float gameStartTime;
    private bool isGameActive = false;
    private bool hasFailedOnce = false; // ���÷ε� �ذ� �Ұ����� ��Ȳ ������

    [Header("Test Level Support")]
    public GameObject testPanel;
    public bool isTestLevel = false;
    private TestStageData currentTestStage;

    private int currentStageIndex = 0;
    private int movesUsed = 0;
    private float timeRemaining;
    private bool isTimerActive = false;

    public static StageManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (gridManagerRef == null)
        {
            gridManagerRef = gridManager;  // �̹� �ִ� gridManager ���� ���
        }

        if (matchingSystemRef == null)
        {
            matchingSystemRef = FindFirstObjectByType<MatchingSystem>();
        }
    }

    void Start()
    {
        // �׽�Ʈ ���� üũ�� ����
        CheckForTestLevel();

        // �׽�Ʈ ������ �ƴ� ���� �Ϲ� �������� �ε�
        if (!isTestLevel && PlayerPrefs.GetInt("IsTestLevel", 0) == 0)
        {
            LoadStage(0);
        }
    }

    void Update()
    {
        if (isTimerActive && timeRemaining > 0)
        {
            // �ð� ���� üũ
            if (isGameActive && currentStage != null && currentStage.hasTimeLimit)
            {
                CheckTimeLimit();
            }
        }
    }

    void CheckForTestLevel()
    {
        if (PlayerPrefs.GetInt("IsTestLevel", 0) == 1)
        {
            Debug.Log("Test level detected, waiting for TestStageLoader");
            isTestLevel = true;  // �÷��� ����
        }
    }

    public void LoadStage(int stageIndex)
    {
        if (isTestLevel)
        {
            Debug.Log("Test level is active, skipping normal stage load");
            return;
        }

        if (stageIndex < 0 || stageIndex >= allStages.Count)
        {
            Debug.LogError("Invalid stage index: " + stageIndex);
            return;
        }

        Debug.Log("start LoadStage()");

        currentStageIndex = stageIndex;
        currentStage = allStages[stageIndex];

        UpdateStageUI();
        UpdateGridManagerSettings();
        ResetGameState();
        StartStageTimer();

        if (gridManager != null)
        {
            gridManager.InitializeStageGrid(currentStage);
        }

        Debug.Log($"Loaded Stage {currentStage.stageNumber}: {currentStage.stageName}");
    }

    public void LoadTestStage(TestStageData testStage)
    {
        if (testStage == null)
        {
            Debug.LogError("Test stage data is null!");
            return;
        }

        isTimerActive = false;
        currentTestStage = testStage;
        isTestLevel = true;

        Debug.Log($"Loading test stage: {testStage.stageName} ({testStage.width}x{testStage.height})");

        ApplyTestStageToGrid();
        UpdateStageUI();
    }

    void UpdateStageUI()
    {
        if (!isTestLevel && currentStage != null)
        {
            if (stageNumberText != null)
                stageNumberText.text = $"STAGE {currentStage.stageNumber}";

            if (stageNameText != null)
                stageNameText.text = currentStage.stageName;

            if (stageDescriptionText != null)
                stageDescriptionText.text = currentStage.stageDescription;

            UpdateMovesUI();

            if (currentStage.hasTimeLimit)
            {
                timeRemaining = currentStage.timeLimit;
                isTimerActive = true;
                UpdateTimerUI();
                if (timerText != null)
                    timerText.gameObject.SetActive(true);
            }
            else
            {
                isTimerActive = false;
                if (timerText != null)
                    timerText.gameObject.SetActive(false);
            }
        }

        if (isTestLevel && currentTestStage != null)
        {
            if (stageNumberText != null)
            {
                stageNumberText.text = $"Test Level: {currentTestStage.stageName}";
            }

            if (movesLeftText != null)
            {
                movesLeftText.text = $"Move Left: {currentTestStage.maxMoves}";
            }

            if (testPanel != null)
                testPanel.SetActive(true);
        }
    }

    void UpdateGridManagerSettings()
    {
        if (gridManager != null && currentStage != null)
        {
            gridManager.width = currentStage.gridWidth;
            gridManager.height = currentStage.gridHeight;
        }
    }

    void ApplyTestStageToGrid()
    {
        if (gridManager == null || currentTestStage == null)
        {
            Debug.LogError("GridManager or test stage is null!");
            return;
        }

        gridManager.width = currentTestStage.width;
        gridManager.height = currentTestStage.height;

        gridManager.ClearGrid();
        gridManager.InitializeGridWithPattern(currentTestStage.pattern);

        Debug.Log($"Test stage applied: {currentTestStage.width}x{currentTestStage.height}, Target: {currentTestStage.targetScore}");
    }

    void ResetGameState()
    {
        movesUsed = 0;

        if (currentStage != null)
        {
            timeRemaining = currentStage.timeLimit;
            isTimerActive = currentStage.hasTimeLimit;
        }

        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(false);
    }

    public void OnBlocksDestroyed()
    {
        movesUsed++;
        UpdateMovesUI();

        if (currentStage != null && currentStage.maxMoves > 0 && movesUsed >= currentStage.maxMoves)
        {
            CheckGameOver();
        }
    }

    void UpdateMovesUI()
    {
        if (movesLeftText != null && currentStage != null && currentStage.maxMoves > 0)
        {
            int movesLeft = currentStage.maxMoves - movesUsed;
            movesLeftText.text = $"Move left: {movesLeft}";
            movesLeftText.gameObject.SetActive(true);
        }
        else if (movesLeftText != null)
        {
            movesLeftText.gameObject.SetActive(false);
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null && isTimerActive)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    void CheckGameOver()
    {
        if ((currentStage != null && currentStage.maxMoves > 0 && movesUsed >= currentStage.maxMoves) ||
            (isTimerActive && timeRemaining <= 0))
        {
            GameOver();
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over!");
        isTimerActive = false;
    }

    public void OnStageComplete()
    {
        Debug.Log($"Stage {(currentStage != null ? currentStage.stageNumber.ToString() : "Test")} Complete!");
        isTimerActive = false;

        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(true);

        if (nextStageButton != null && !isTestLevel)
        {
            bool hasNextStage = currentStageIndex + 1 < allStages.Count;
            nextStageButton.interactable = hasNextStage;
        }
    }

    // ������ �޼����...
    public void LoadNextStage()
    {
        if (currentStageIndex + 1 < allStages.Count)
        {
            LoadStage(currentStageIndex + 1);
        }
        else
        {
            Debug.Log("All stages completed!");
        }
    }

    public void RestartCurrentStage()
    {
        LoadStage(currentStageIndex);
    }

    public void GoToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public int GetCurrentStageNumber()
    {
        return currentStage != null ? currentStage.stageNumber : 1;
    }

    public bool IsTestLevel()
    {
        return isTestLevel;
    }

    private void StartStageTimer()
    {
        if (currentStage != null && currentStage.hasTimeLimit)
        {
            gameStartTime = Time.time;
            isGameActive = true;
            hasFailedOnce = false;

            Debug.Log($"Stage timer started. Time limit: {currentStage.timeLimit} seconds");
        }
    }

    // ���� ���� �� GridManagerRefactored���� ȣ���� �޼���
    public void OnShuffleAttemptFailed()
    {
        Debug.Log("Shuffle attempt failed - no more matches possible!");
        hasFailedOnce = true;

        // ���� ���� ó��
        OnStageFailed("No More Matching!");
    }

    // �������� Ŭ���� �� GridManagerRefactored���� ȣ���� �޼���  
    public void OnStageCleared()
    {
        Debug.Log("All blocks destroyed - Stage cleared!");
        OnStageComplete();  // ���� �޼��� Ȱ��

        // ���� ����
        if (currentStage != null && UserDataManager.Instance != null)
        {
            UserDataManager.Instance.AddGameCoins(currentStage.coinReward);
            if (currentStage.diamondReward > 0)
            {
                UserDataManager.Instance.AddDiamonds(currentStage.diamondReward);
            }
            // ����ġ �ý����� �ִٸ�
            // UserDataManager.Instance.AddExperience(currentStage.experienceReward);
        }
    }

    // �������� ���� ó�� �޼��� �߰�
    public void OnStageFailed(string reason)
    {
        isGameActive = false;
        Debug.Log($"Stage failed: {reason}");

        // TODO: ���� UI ǥ�� (���߿� ���� ���� �г� �߰�)
        // �ӽ÷� ���� ���� ó��
        GameOver();
    }

    // ���ο� �ð� üũ �޼��� �߰�
    private void CheckTimeLimit()
    {
        if (isTimerActive && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();

            if (timeRemaining <= 0)
            {
                OnStageFailed("Time Over!");
            }
        }
    }
}