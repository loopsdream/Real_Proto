// CommonUIManager.cs - ������ ���� UI ���� �ý���
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CommonUIManager : MonoBehaviour
{
    [Header("Common UI References")]
    public GameObject commonUICanvas;
    public GameObject topUIPanel;
    public CurrencyUI currencyUI;
    public Button settingsButton;
    public Button pauseButton;
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    public GameObject loadingScreen;

    [Header("Scene-specific Visibility")]
    public bool showTopUIInMainMenu = false;
    public bool showTopUIInGameScene = true;
    public bool showTopUIInStageSelect = true;
    public bool showSettingsInAllScenes = true;

    [Header("UI Positioning")]
    public Vector2 topUIPositionMainMenu = new Vector2(0, -80);
    public Vector2 topUIPositionGame = new Vector2(0, -40);
    public Vector2 topUIPositionStageSelect = new Vector2(0, -60);

    public static CommonUIManager Instance;

    private string currentSceneName;
    private Canvas commonCanvas;

    void Awake()
    {
        // �̱��� ����
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Canvas ����
            SetupCanvas();

            // �� ���� �̺�Ʈ ����
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("CommonUIManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // �ʱ� �� ����
        currentSceneName = SceneManager.GetActiveScene().name;
        AdjustUIForScene(currentSceneName);

        // ��ư �̺�Ʈ ����
        SetupButtonEvents();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void SetupCanvas()
    {
        if (commonUICanvas != null)
        {
            commonCanvas = commonUICanvas.GetComponent<Canvas>();
            if (commonCanvas != null)
            {
                // �ֻ��� ������ ���� ����
                commonCanvas.sortingOrder = 100;
                commonCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
    }

    void SetupButtonEvents()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(PauseGame);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        Debug.Log($"CommonUI: Scene loaded - {currentSceneName}");

        // �� �ε� �Ϸ� �� UI ����
        Invoke(nameof(DelayedSceneSetup), 0.1f);
    }

    void DelayedSceneSetup()
    {
        AdjustUIForScene(currentSceneName);

        // UserDataManager �翬�� (�ʿ��� ���)
        if (currencyUI != null && UserDataManager.Instance != null)
        {
            currencyUI.RefreshAllDisplays();
        }
    }

    void AdjustUIForScene(string sceneName)
    {
        switch (sceneName.ToLower())
        {
            case "mainmenu":
                SetupForMainMenu();
                break;
            case "gamescene":
                SetupForGameScene();
                break;
            case "stageselect":
                SetupForStageSelect();
                break;
            default:
                SetupDefault();
                break;
        }
    }

    void SetupForMainMenu()
    {
        Debug.Log("Setting up UI for Main Menu");

        // ���� �޴������� UI ǥ�� ����
        SetUIVisibility(
            showTopUI: showTopUIInMainMenu,
            showSettings: showSettingsInAllScenes,
            showPause: false
        );

        // ��ġ ����
        if (topUIPanel != null)
        {
            SetTopUIPosition(topUIPositionMainMenu);
        }
    }

    void SetupForGameScene()
    {
        Debug.Log("Setting up UI for Game Scene");

        // ���� �������� UI ǥ�� ����
        SetUIVisibility(
            showTopUI: showTopUIInGameScene,
            showSettings: showSettingsInAllScenes,
            showPause: true
        );

        // ��ġ ����
        if (topUIPanel != null)
        {
            SetTopUIPosition(topUIPositionGame);
        }
    }

    void SetupForStageSelect()
    {
        Debug.Log("Setting up UI for Stage Select");

        // �������� ���ÿ����� UI ǥ�� ����
        SetUIVisibility(
            showTopUI: showTopUIInStageSelect,
            showSettings: showSettingsInAllScenes,
            showPause: false
        );

        // ��ġ ����
        if (topUIPanel != null)
        {
            SetTopUIPosition(topUIPositionStageSelect);
        }
    }

    void SetupDefault()
    {
        Debug.Log("Setting up UI with default settings");

        // �⺻ ����
        SetUIVisibility(
            showTopUI: true,
            showSettings: true,
            showPause: false
        );

        if (topUIPanel != null)
        {
            SetTopUIPosition(topUIPositionGame);
        }
    }

    void SetUIVisibility(bool showTopUI, bool showSettings, bool showPause)
    {
        if (topUIPanel != null)
            topUIPanel.SetActive(showTopUI);

        if (settingsButton != null)
            settingsButton.gameObject.SetActive(showSettings);

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(showPause);
    }

    void SetTopUIPosition(Vector2 position)
    {
        if (topUIPanel != null)
        {
            RectTransform rect = topUIPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = position;
            }
        }
    }

    // ���� �޼����
    public void ShowLoadingScreen()
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            Debug.Log("Loading screen shown");
        }
    }

    public void HideLoadingScreen()
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
            Debug.Log("Loading screen hidden");
        }
    }

    public void ShowNotification(string message, float duration = 3f)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);

            // duration �� �ڵ� ����
            CancelInvoke(nameof(HideNotification));
            Invoke(nameof(HideNotification), duration);

            Debug.Log($"Notification shown: {message}");
        }
    }

    public void HideNotification()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        // ���� â ���� ����
        Debug.Log("Opening settings");

        // ���� �Ŵ����� �ִٸ� ȣ��
        //SettingsManager settingsManager = FindObjectOfType<SettingsManager>();
        //if (settingsManager != null)
        //{
        //    settingsManager.OpenSettings();
        //}
        //else
        //{
        //    ShowNotification("���� â�� �غ� ���Դϴ�...");
        //}
    }

    public void PauseGame()
    {
        // ���� �Ͻ����� ����
        Debug.Log("Game paused");

        if (currentSceneName.ToLower() == "gamescene")
        {
            Time.timeScale = Time.timeScale == 0 ? 1 : 0;

            string message = Time.timeScale == 0 ? "���� �Ͻ�����" : "���� �簳";
            ShowNotification(message, 2f);
        }
    }

    public void RefreshCurrencyUI()
    {
        if (currencyUI != null)
        {
            currencyUI.RefreshAllDisplays();
        }
    }

    public bool IsCurrentScene(string sceneName)
    {
        return currentSceneName.ToLower() == sceneName.ToLower();
    }

    public string GetCurrentSceneName()
    {
        return currentSceneName;
    }
}