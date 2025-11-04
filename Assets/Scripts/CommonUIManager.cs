// CommonUIManager.cs - 씬 간 공통 UI 관리 시스템
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
    public Button shopButton;
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
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            
            // Application.isPlaying 체크로 DontDestroyOnLoad 오류 방지
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Canvas 설정
            SetupCanvas();

            // 씬 로드 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("CommonUIManager initialized");
        }
        else
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }
    }

    void Start()
    {
        // 초기 씬 설정
        currentSceneName = SceneManager.GetActiveScene().name;
        AdjustUIForScene(currentSceneName);

        // 버튼 이벤트 설정
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
                // 최상위 레이어 순서 설정
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

        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners();
            shopButton.onClick.AddListener(OpenShop);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        Debug.Log($"CommonUI: Scene loaded - {currentSceneName}");

        // 씬 로드 완료 후 UI 설정
        Invoke(nameof(DelayedSceneSetup), 0.1f);
    }

    void DelayedSceneSetup()
    {
        AdjustUIForScene(currentSceneName);

        // UserDataManager 재연결 (필요한 경우)
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

        // 메인 메뉴에서의 UI 표시 설정
        SetUIVisibility(
            showTopUI: showTopUIInMainMenu,
            showSettings: showSettingsInAllScenes,
            showPause: false
        );

        // 위치 설정
        if (topUIPanel != null)
        {
            SetTopUIPosition(topUIPositionMainMenu);
        }
    }

    void SetupForGameScene()
    {
        Debug.Log("Setting up UI for Game Scene");

        // 게임 씬에서의 UI 표시 설정
        SetUIVisibility(
            showTopUI: showTopUIInGameScene,
            showSettings: showSettingsInAllScenes,
            showPause: true
        );

        // 위치 설정
        if (topUIPanel != null)
        {
            SetTopUIPosition(topUIPositionGame);
        }
    }

    void SetupForStageSelect()
    {
        Debug.Log("Setting up UI for Stage Select");

        // 스테이지 선택에서의 UI 표시 설정
        SetUIVisibility(
            showTopUI: showTopUIInStageSelect,
            showSettings: showSettingsInAllScenes,
            showPause: false
        );

        // 위치 설정
        if (topUIPanel != null)
        {
            SetTopUIPosition(topUIPositionStageSelect);
        }
    }

    void SetupDefault()
    {
        Debug.Log("Setting up UI with default settings");

        // 기본 설정
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

    // 공용 메서드들
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

            // duration 후 자동 숨김
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
        // 설정 창 열기 로직
        Debug.Log("Opening settings");

        // 설정 매니저가 있다면 호출
        //SettingsManager settingsManager = FindObjectOfType<SettingsManager>();
        //if (settingsManager != null)
        //{
        //    settingsManager.OpenSettings();
        //}
        //else
        //{
        //    ShowNotification("설정 창이 준비 중입니다...");
        //}
    }

    public void PauseGame()
    {
        // 게임 일시정지 로직
        Debug.Log("Game paused");

        if (currentSceneName.ToLower() == "gamescene")
        {
            Time.timeScale = Time.timeScale == 0 ? 1 : 0;

            string message = Time.timeScale == 0 ? "게임 일시정지" : "게임 재시작";
            ShowNotification(message, 2f);
        }
    }

    void OpenShop()
    {
        Debug.Log("Opening shop...");

        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.ShowShop(ShopTab.Items);
        }
        else
        {
            Debug.LogError("CommonUIManager: ShopUIManager.Instance is null!");
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