// TitleSceneManager.cs - Firebase 타임아웃 처리 포함 버전
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TitleSceneManager : MonoBehaviour
{
    [Header("Title UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI versionText;
    public Button startButton;
    public Button loginButton;
    public GameObject loadingPanel;
    
    [Header("Loading UI")]
    public Slider progressBar;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI progressText;
    
    [Header("Settings")]
    public string gameVersion = "1.0.0";
    public float minLoadingTime = 2.0f;
    public float firebaseTimeout = 10f;
    
    [Header("Animation")]
    public CanvasGroup titleCanvasGroup;
    public float titleFadeInDuration = 1.5f;
    
    private bool isInitialized = false;
    private bool isLoading = false;
    private bool firebaseReady = false;

    void Start()
    {
        InitializeTitle();
    }

    void InitializeTitle()
    {
        // 버전 정보 설정
        if (versionText != null)
        {
            versionText.text = $"v{gameVersion}";
        }

        // 타이틀 텍스트 설정
        if (titleText != null)
        {
            titleText.text = "CROxCRO";
        }

        // 초기 UI 상태 설정
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        // 버튼 초기 비활성화 (Firebase 준비까지)
        SetButtonsInteractable(false);

        // 타이틀 페이드 인
        StartCoroutine(FadeInTitle());

        // BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySceneBGM("TitleScene");
        }

        // Firebase 초기화 시작
        StartCoroutine(InitializeFirebaseWithFallback());
    }

    IEnumerator InitializeFirebaseWithFallback()
    {
        ShowLoadingPanel();
        ShowStatus("Firebase 초기화 중...", 0.1f);

        // SafeFirebaseManager 찾기 또는 생성
        if (RealFirebaseManager.Instance == null)
        {
            Debug.Log("RealFirebaseManager 생성 중...");
            GameObject firebaseGO = new GameObject("RealFirebaseManager");
            firebaseGO.AddComponent<RealFirebaseManager>();
        }

        // Firebase 초기화 대기 (타임아웃 포함)
        float elapsedTime = 0f;
        while (RealFirebaseManager.Instance == null && elapsedTime < 2f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (RealFirebaseManager.Instance == null)
        {
            Debug.LogError("RealFirebaseManager 생성 실패 - 오프라인 모드로 진행");
            ShowStatus("오프라인 모드로 진행", 0.8f);
            yield return new WaitForSeconds(1f);
            CompleteInitialization();
            yield break;
        }

        // Firebase 이벤트 구독
        RealFirebaseManager.Instance.OnFirebaseInitialized += OnFirebaseReady;
        RealFirebaseManager.Instance.OnUserSignedIn += OnUserSignedIn;
        RealFirebaseManager.Instance.OnAuthError += OnFirebaseError;

        // Firebase 초기화 대기 (타임아웃 체크)
        elapsedTime = 0f;
        while (!RealFirebaseManager.Instance.IsFirebaseReady() && elapsedTime < firebaseTimeout)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / firebaseTimeout);
            ShowStatus("Firebase 초기화 중...", 0.1f + (progress * 0.6f));
            yield return null;
        }

        if (!RealFirebaseManager.Instance.IsFirebaseReady())
        {
            Debug.LogWarning("Firebase 초기화 타임아웃 - 오프라인 모드로 진행");
            ShowStatus("네트워크 연결 확인 중...", 0.7f);
            yield return new WaitForSeconds(1f);
            ShowStatus("오프라인 모드로 진행", 0.8f);
            yield return new WaitForSeconds(1f);
        }

        CompleteInitialization();
    }

    void OnFirebaseReady()
    {
        Debug.Log("Firebase 준비 완료!");
        firebaseReady = true;
        
        if (RealFirebaseManager.Instance.IsOnlineMode())
        {
            ShowStatus("Firebase 연결 완료!", 0.7f);
        }
        else
        {
            ShowStatus("오프라인 모드 활성화", 0.7f);
        }
    }

    void OnUserSignedIn(bool success)
    {
        if (success)
        {
            Debug.Log("사용자 로그인 완료");
            StartCoroutine(StartGameSequence());
        }
    }

    void OnFirebaseError(string error)
    {
        Debug.LogError($"Firebase 오류: {error}");
        ShowStatus($"오류: {error}", 0.5f);
    }

    void CompleteInitialization()
    {
        ShowStatus("초기화 완료!", 1.0f);
        
        StartCoroutine(FinalizeInitialization());
    }

    IEnumerator FinalizeInitialization()
    {
        yield return new WaitForSeconds(0.5f);
        
        HideLoadingPanel();
        SetButtonsInteractable(true);
        isInitialized = true;

        // 연결 상태 표시
        string connectionStatus = "";
        if (RealFirebaseManager.Instance != null)
        {
            if (RealFirebaseManager.Instance.IsOnlineMode())
            {
                connectionStatus = "🌐 온라인";
            }
            else
            {
                connectionStatus = "📱 오프라인";
            }
        }
        else
        {
            connectionStatus = "📱 로컬";
        }

        if (versionText != null)
        {
            versionText.text = $"v{gameVersion} {connectionStatus}";
        }

        Debug.Log("타이틀 초기화 완료!");
    }

    IEnumerator FadeInTitle()
    {
        if (titleCanvasGroup == null) yield break;

        titleCanvasGroup.alpha = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < titleFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            titleCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / titleFadeInDuration);
            yield return null;
        }

        titleCanvasGroup.alpha = 1f;
    }

    // 게임 시작 버튼 (게스트 로그인)
    public void OnStartButtonClicked()
    {
        if (!isInitialized || isLoading) return;

        PlayUISound("ButtonClick");
        
        if (RealFirebaseManager.Instance != null)
        {
            StartCoroutine(GuestLoginSequence());
        }
        else
        {
            // Firebase 없이 바로 게임 시작
            StartCoroutine(StartGameSequence());
        }
    }

    // 로그인 버튼 (향후 구현)
    public void OnLoginButtonClicked()
    {
        if (!isInitialized || isLoading) return;

        PlayUISound("ButtonClick");
        Debug.Log("로그인 기능은 향후 구현 예정");
        
        // 현재는 게스트 로그인과 동일하게 처리
        OnStartButtonClicked();
    }

    IEnumerator GuestLoginSequence()
    {
        isLoading = true;
        SetButtonsInteractable(false);
        ShowLoadingPanel();

        ShowStatus("게스트로 로그인 중...", 0.2f);

        bool loginSuccess = false;
        yield return StartCoroutine(
            RealFirebaseManager.Instance.SignInAnonymously((success) => loginSuccess = success)
        );

        if (loginSuccess)
        {
            ShowStatus("게스트 로그인 성공!", 0.6f);
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(StartGameSequence());
        }
        else
        {
            ShowStatus("로그인 실패 - 오프라인으로 진행", 0.4f);
            yield return new WaitForSeconds(1f);
            StartCoroutine(StartGameSequence());
        }
    }

    IEnumerator StartGameSequence()
    {
        isLoading = true;
        SetButtonsInteractable(false);

        ShowStatus("게임 데이터 로드 중...", 0.7f);
        yield return new WaitForSeconds(1f);

        ShowStatus("에셋 로드 중...", 0.9f);
        yield return new WaitForSeconds(0.5f);

        ShowStatus("로드 완료!", 1.0f);
        yield return new WaitForSeconds(0.5f);

        // 로비 씬으로 이동
        GoToLobbyScene();
    }

    void ShowLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
    }

    void HideLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    void ShowStatus(string message, float progress)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        Debug.Log($"Loading: {message} ({Mathf.RoundToInt(progress * 100)}%)");
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (startButton != null) startButton.interactable = interactable;
        if (loginButton != null) loginButton.interactable = interactable;
    }

    void GoToLobbyScene()
    {
        PlayUISound("MenuTransition");
        Debug.Log("Moving to Lobby Scene...");
        SceneManager.LoadScene("LobbyScene");
    }

    // 게임 종료
    public void QuitGame()
    {
        PlayUISound("ButtonClick");
        Debug.Log("Quitting the game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // UI 사운드 재생 헬퍼 메서드
    void PlayUISound(string soundName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(soundName);
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (RealFirebaseManager.Instance != null)
        {
            RealFirebaseManager.Instance.OnFirebaseInitialized -= OnFirebaseReady;
            RealFirebaseManager.Instance.OnUserSignedIn -= OnUserSignedIn;
            RealFirebaseManager.Instance.OnAuthError -= OnFirebaseError;
        }
    }
}