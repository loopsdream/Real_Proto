// StageManager.cs - 스테이지 관리 및 새로운 클리어 조건 지원
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
    public GameObject pausePanel;
    public GameObject WarningPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI stageNumberText;
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI stageDescriptionText;
    public TextMeshProUGUI movesLeftText;
    public TextMeshProUGUI timerText;
    public Button nextStageButton;
    public Button restartStageButton;
    public Button settingButton;
    public GameObject stageCompletePanel;

    [Header("Not Enough Energy Panel")]
    public GameObject notEnoughEnergyPanel;
    public TMPro.TextMeshProUGUI notEnoughEnergyText;
    public Button watchAdButton;
    public Button goToShopButton;
    public Button closeEnergyPanelButton;

    [Header("Game References")]
    public StageGridManager gridManager;

    [Header("Stage Failure Detection")]
    public StageGridManager gridManagerRef; // GridManager 참조
    public MatchingSystem matchingSystemRef; // MatchingSystem 참조

    [Header("Game State")]
    private float gameStartTime;
    private bool isGameActive = false;
    private bool hasFailedOnce = false; // 셔플로도 해결 불가능한 상황 감지용

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
            gridManagerRef = gridManager;  // 이미 있는 gridManager 참조 사용
        }

        if (matchingSystemRef == null)
        {
            matchingSystemRef = FindFirstObjectByType<MatchingSystem>();
        }

        RegisterEnergyPanelListeners();
    }

    void Start()
    {
        // GameManager GameObject에서 StageGridManager 찾기
        GameObject gameManagerObj = GameObject.Find("GameManager");
        if (gameManagerObj != null)
        {
            gridManager = gameManagerObj.GetComponent<StageGridManager>();
            Debug.Log($"[StageManager] Found StageGridManager on GameManager");
        }

        if (gridManager == null)
        {
            Debug.LogError("[StageManager] StageGridManager not found!");
            return;
        }

        // 테스트 레벨 체크를 먼저
        CheckForTestLevel();

        // 씬 재진입 시 강제 초기화
        StartCoroutine(InitializeStageWithDelay());
    }

    private void RegisterEnergyPanelListeners()
    {
        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnWatchAdForEnergyClicked);
        }
        if (goToShopButton != null)
        {
            goToShopButton.onClick.RemoveAllListeners();
            goToShopButton.onClick.AddListener(OnGoToShopClicked);
        }
        if (closeEnergyPanelButton != null)
        {
            closeEnergyPanelButton.onClick.RemoveAllListeners();
            closeEnergyPanelButton.onClick.AddListener(CloseEnergyPanel);
        }
    }

    System.Collections.IEnumerator InitializeStageWithDelay()
    {
        // 한 프레임 대기 (모든 컴포넌트 초기화 대기)
        yield return null;

        if (!isTestLevel && PlayerPrefs.GetInt("IsTestLevel", 0) == 0)
        {
            Debug.Log("[StageManager] Loading first stage after delay");
            LoadStage(0);
        }
    }

    void Update()
    {
        if (isTimerActive && timeRemaining > 0)
        {
            // 시간 제한 체크
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
            isTestLevel = true;  // 플래그 설정
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

        // gridManager 재확인
        if (gridManager == null)
        {
            GameObject gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj != null)
            {
                gridManager = gameManagerObj.GetComponent<StageGridManager>();
            }

            //gridManager = Object.FindAnyObjectByType<StageGridManager>();
        }

        if (gridManager != null)
        {
            Debug.Log($"[StageManager] Initializing grid");
            gridManager.InitializeStageGrid(currentStage);

            // 그리드 초기화 확인
            StartCoroutine(VerifyGridInitialization());
        }
        else
        {
            Debug.LogError("[StageManager] GridManager is still null!");
        }

        Debug.Log($"Loaded Stage {currentStage.stageNumber}: {currentStage.stageName}");
    }

    System.Collections.IEnumerator VerifyGridInitialization()
    {
        yield return new WaitForSeconds(0.1f);

        // 블록이 제대로 생성되었는지 확인
        int blockCount = 0;
        Transform gridParent = gridManager.gridParent;
        if (gridParent != null)
        {
            blockCount = gridParent.childCount;
        }

        Debug.Log($"[StageManager] Grid initialization complete. Block count: {blockCount}");

        if (blockCount == 0)
        {
            Debug.LogError("[StageManager] No blocks created! Trying to reinitialize...");
            gridManager.InitializeStageGrid(currentStage);
        }
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
                movesLeftText.text = $"Move Left: {currentTestStage.maxTaps}";
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

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void OnBlocksDestroyed()
    {
        movesUsed++;
        UpdateMovesUI();

        // Tap-based game over is handled by StageGridManager.CheckWinCondition()
        // to ensure goal completion is checked before declaring game over.
    }

    void UpdateMovesUI()
    {
        if (movesLeftText != null && currentStage != null && currentStage.maxTaps > 0)
        {
            int movesLeft = currentStage.maxTaps - movesUsed;
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
        if ((currentStage != null && currentStage.maxTaps > 0 && movesUsed >= currentStage.maxTaps) ||
            (isTimerActive && timeRemaining <= 0))
        {
            GameOver();
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over!");
        isTimerActive = false;
        isGameActive = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[StageManager] gameOverPanel is not assigned!");
        }

        // 재도전 버튼 에너지 체크
        UpdateRetryButtonState();
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

    // 나머지 메서드들...
    public void LoadNextStage()
    {
        if (currentStageIndex + 1 >= allStages.Count)
        {
            Debug.Log("All stages completed!");
            return;
        }

        if (UserDataManager.Instance == null)
        {
            Debug.LogError("[StageManager] UserDataManager not found!");
            return;
        }

        LoadStage(currentStageIndex + 1);
    }

    private void ShowEnergyPanel()
    {
        if (notEnoughEnergyPanel == null) return;

        notEnoughEnergyPanel.SetActive(true);

        if (notEnoughEnergyText != null && UserDataManager.Instance != null)
        {
            System.TimeSpan timeUntilNext = UserDataManager.Instance.GetTimeUntilNextEnergy();
            if (timeUntilNext.TotalSeconds > 0)
            {
                string timeStr = string.Format("{0:D2}:{1:D2}", timeUntilNext.Minutes, timeUntilNext.Seconds);
                notEnoughEnergyText.text = $"에너지가 부족합니다.\n다음 에너지 충전까지: {timeStr}";
            }
            else
            {
                notEnoughEnergyText.text = "에너지가 부족합니다.";
            }
        }

        // 광고 버튼 준비 상태 반영
        if (watchAdButton != null)
        {
            bool adReady = AdManager.Instance != null && AdManager.Instance.IsEnergyRewardedAdReady();
            watchAdButton.interactable = adReady;
        }
    }

    private void CloseEnergyPanel()
    {
        if (notEnoughEnergyPanel != null)
            notEnoughEnergyPanel.SetActive(false);
    }

    private void OnWatchAdForEnergyClicked()
    {
        if (AdManager.Instance == null)
        {
            Debug.LogError("[StageManager] AdManager not found!");
            return;
        }

        if (watchAdButton != null) watchAdButton.interactable = false;

        AdManager.Instance.ShowEnergyRewardedAd(
            onSuccess: () =>
            {
                Debug.Log("[StageManager] Energy ad success - adding energy.");
                if (UserDataManager.Instance != null)
                    UserDataManager.Instance.AddEnergy(1);

                CloseEnergyPanel();
                // 에너지 충전 후 재도전 실행
                RestartCurrentStage();
            },
            onFailed: () =>
            {
                Debug.Log("[StageManager] Energy ad failed.");
                if (watchAdButton != null) watchAdButton.interactable = true;
            }
        );
    }

    private void OnGoToShopClicked()
    {
        Debug.Log("[StageManager] Go to shop - not yet implemented.");
        // TODO: 상점 패널 열기
    }

    public void RestartCurrentStage()
    {
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("[StageManager] UserDataManager not found!");
            return;
        }

        if (UserDataManager.Instance.GetEnergy() >= 1)
        {
            UserDataManager.Instance.SpendEnergy(1, (success) =>
            {
                if (success)
                {
                    Debug.Log("[StageManager] Energy spent for retry.");
                    LoadStage(currentStageIndex);
                }
                else
                {
                    Debug.LogError("[StageManager] SpendEnergy failed on retry.");
                    ShowEnergyPanel();
                }
            });
        }
        else
        {
            Debug.Log("[StageManager] Not enough energy for retry.");
            ShowEnergyPanel();
        }

    }

    public void GoToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
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

    // 셔플 실패 시 GridManagerRefactored에서 호출할 메서드
    public void OnShuffleAttemptFailed()
    {
        Debug.Log("Shuffle attempt failed - no more matches possible!");
        hasFailedOnce = true;

        // 게임 오버 처리
        OnStageFailed("No More Matching!");
    }

    // 스테이지 클리어 시 GridManagerRefactored에서 호출할 메서드  
    public void OnStageCleared(List<RewardItem> pendingRewards)
    {
        Debug.Log("All blocks destroyed - Stage cleared!");

        // StageClearRewardPanel에 보상 데이터 전달하고 표시
        StageClearRewardPanel rewardPanel = Object.FindAnyObjectByType<StageClearRewardPanel>(FindObjectsInactive.Include);
        if (rewardPanel != null)
        {
            rewardPanel.Show(pendingRewards);
        }
        else
        {
            Debug.LogError("[StageManager] StageClearRewardPanel not found! Granting rewards directly.");
            // 패널이 없으면 바로 지급 (안전장치)
            StageGridManager gridManager = Object.FindAnyObjectByType<StageGridManager>();
            if (gridManager != null)
            {
                gridManager.GrantRewardItems(pendingRewards);
            }
        }
    }

    // 스테이지 실패 처리 메서드 추가
    public void OnStageFailed(string reason)
    {
        isGameActive = false;
        Debug.Log($"Stage failed: {reason}");

        // TODO: 실패 UI 표시 (나중에 실패 전용 패널 추가)
        // 임시로 게임 오버 처리
        GameOver();
    }

    // 새로운 시간 체크 메서드 추가
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

    public void ReturnToLevelDesigner()
    {
        isTestLevel = false;        // 플래그 초기화
        currentTestStage = null;    // 테스트 데이터 정리

        SceneManager.LoadScene("LevelDesigner");
    }

    public void OnTouchesSettingButton()
    {
        AudioManager.Instance.PlayUI("ButtonClick");
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void OnTouchesHomeButton()
    {
        AudioManager.Instance.PlayUI("ButtonClick");
        if (WarningPanel != null)
        {
            WarningPanel.SetActive(true);
        }
    }

    public void OnTouchesResumeButton()
    {
        AudioManager.Instance.PlayUI("ButtonClick");
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void OnTouchesBackButton()
    {
        AudioManager.Instance.PlayUI("ButtonClick");
        if (WarningPanel != null)
        {
            WarningPanel.SetActive(false);
        }
    }

    // 새로 추가하는 메서드
    private void UpdateRetryButtonState()
    {
        if (restartStageButton != null && UserDataManager.Instance != null)
        {
            bool hasEnergy = UserDataManager.Instance.GetEnergy() >= 1;
            restartStageButton.interactable = hasEnergy;
            Debug.Log($"[StageManager] Retry button interactable: {hasEnergy}");
        }
    }
}