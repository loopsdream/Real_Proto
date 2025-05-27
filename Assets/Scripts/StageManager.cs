// StageManager.cs - �������� ���� �� �ε�
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    public GridManager gridManager;

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
    }

    void Start()
    {
        LoadStage(0); // ù ��° �������� �ε�
    }

    void Update()
    {
        if (isTimerActive && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();

            if (timeRemaining <= 0)
            {
                GameOver();
            }
        }
    }

    public void LoadStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= allStages.Count)
        {
            Debug.LogError("Invalid stage index: " + stageIndex);
            return;
        }

        currentStageIndex = stageIndex;
        currentStage = allStages[stageIndex];

        // �������� ���� UI ������Ʈ
        UpdateStageUI();

        // �׸��� �Ŵ��� ���� ������Ʈ
        UpdateGridManagerSettings();

        // ���� ���� �ʱ�ȭ
        ResetGameState();

        // �׸��� ����
        if (gridManager != null)
        {
            gridManager.InitializeStageGrid(currentStage);
        }

        Debug.Log($"Loaded Stage {currentStage.stageNumber}: {currentStage.stageName}");
    }

    void UpdateStageUI()
    {
        if (stageNumberText != null)
            stageNumberText.text = $"STAGE {currentStage.stageNumber}";

        if (stageNameText != null)
            stageNameText.text = currentStage.stageName;

        if (stageDescriptionText != null)
            stageDescriptionText.text = currentStage.stageDescription;

        UpdateMovesUI();

        // Ÿ�̸� ����
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

    void UpdateGridManagerSettings()
    {
        if (gridManager != null)
        {
            gridManager.width = currentStage.gridWidth;
            gridManager.height = currentStage.gridHeight;
            gridManager.targetScore = currentStage.targetScore;
        }
    }

    void ResetGameState()
    {
        movesUsed = 0;
        timeRemaining = currentStage.timeLimit;
        isTimerActive = currentStage.hasTimeLimit;

        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(false);
    }

    public void OnBlocksDestroyed()
    {
        movesUsed++;
        UpdateMovesUI();

        // �ִ� �̵� Ƚ�� üũ
        if (currentStage.maxMoves > 0 && movesUsed >= currentStage.maxMoves)
        {
            CheckGameOver();
        }
    }

    void UpdateMovesUI()
    {
        if (movesLeftText != null && currentStage.maxMoves > 0)
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
        // �� �̻� �̵��� �� ���ų� �ð��� �� �� ���
        if ((currentStage.maxMoves > 0 && movesUsed >= currentStage.maxMoves) ||
            (isTimerActive && timeRemaining <= 0))
        {
            GameOver();
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over!");
        // ���� ���� ó��
        isTimerActive = false;
        // ���� ���� �г� ǥ�� ��
    }

    public void OnStageComplete()
    {
        Debug.Log($"Stage {currentStage.stageNumber} Complete!");
        isTimerActive = false;

        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(true);

        // ���� �������� ��ư Ȱ��ȭ
        if (nextStageButton != null)
        {
            bool hasNextStage = currentStageIndex + 1 < allStages.Count;
            nextStageButton.interactable = hasNextStage;
        }
    }

    public void LoadNextStage()
    {
        if (currentStageIndex + 1 < allStages.Count)
        {
            LoadStage(currentStageIndex + 1);
        }
        else
        {
            Debug.Log("All stages completed!");
            // ��� �������� �Ϸ� ó��
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
}