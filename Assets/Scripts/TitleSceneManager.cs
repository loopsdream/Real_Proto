// TitleSceneManager.cs - Firebase 비동기 로그인 지원 버전
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleSceneManager : MonoBehaviour
{
    [Header("UI 컴포넌트들")]
    public Button loginButton;
    public TextMeshProUGUI versionText;
    public GameObject loadingPanel;
    public Slider loadingProgressBar;
    public TextMeshProUGUI loadingStatusText;

    [Header("로딩 설정")]
    public float firebaseTimeout = 8f;
    public float loginTimeout = 15f;      // 로그인 타임아웃 시간
    public string gameVersion = "1.0.0";

    private bool isInitialized = false;
    private bool firebaseReady = false;
    private bool isProcessingLogin = false;
    private bool loginSuccessful = false;

    void Start()
    {
        // UI 초기화
        SetButtonsInteractable(false);
        ShowLoadingPanel();

        // Firebase 매니저 확인 및 생성
        EnsureManagers();

        // 로딩 및 인증 처리 시작
        StartCoroutine(LoadingAndInitialization());

        // 버튼 이벤트 설정
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
    }

    void EnsureManagers()
    {
        // CleanFirebaseManager 확인
        if (CleanFirebaseManager.Instance == null)
        {
            var firebaseGO = new GameObject("CleanFirebaseManager");
            firebaseGO.AddComponent<CleanFirebaseManager>();
            Debug.Log("[TitleScene] CleanFirebaseManager 생성됨");
        }

        // FirebaseDataManager 확인
        if (FirebaseDataManager.Instance == null)
        {
            var dataGO = new GameObject("FirebaseDataManager");
            dataGO.AddComponent<FirebaseDataManager>();
            Debug.Log("[TitleScene] FirebaseDataManager 생성됨");
        }

        // UserDataManager 확인
        if (UserDataManager.Instance == null)
        {
            var userGO = new GameObject("UserDataManager");
            userGO.AddComponent<UserDataManager>();
            Debug.Log("[TitleScene] UserDataManager 생성됨");
        }
    }

    IEnumerator LoadingAndInitialization()
    {
        ShowStatus("게임 초기화 중...", 0.1f);
        yield return new WaitForSeconds(0.5f);

        ShowStatus("Firebase 연결 중...", 0.3f);
        float elapsedTime = 0f;

        // Firebase 매니저 대기 (최대 3초)
        while (CleanFirebaseManager.Instance == null && elapsedTime < 3f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (CleanFirebaseManager.Instance == null)
        {
            Debug.LogWarning("[TitleScene] Firebase 매니저 없음 - 로컬 모드로 진행");
            CompleteInitializationDirectly("📱 로컬");
            yield break;
        }

        // Firebase 이벤트 구독
        SubscribeToFirebaseEvents();

        // Firebase 초기화 대기
        ShowStatus("Firebase 초기화 중...", 0.5f);
        elapsedTime = 0f;
        
        while (!CleanFirebaseManager.Instance.IsReady && elapsedTime < firebaseTimeout)
        {
            elapsedTime += Time.deltaTime;
            float progress = 0.5f + (elapsedTime / firebaseTimeout) * 0.3f;
            ShowStatus("Firebase 초기화 중...", progress);
            yield return null;
        }

        if (!CleanFirebaseManager.Instance.IsReady)
        {
            Debug.LogWarning("[TitleScene] Firebase 타임아웃 - 로컬 모드로 진행");
            ShowStatus("로컬 모드로 전환 중...", 0.8f);
            yield return new WaitForSeconds(1f);
            CompleteInitializationDirectly("📱 로컬");
        }
        else
        {
            Debug.Log("[TitleScene] ✅ Firebase 준비 완료");
            ShowStatus("Firebase 연결 완료!", 0.8f);
            yield return new WaitForSeconds(0.5f);
            CompleteInitializationDirectly("🌐 온라인");
        }
    }

    void SubscribeToFirebaseEvents()
    {
        if (CleanFirebaseManager.Instance != null)
        {
            CleanFirebaseManager.Instance.OnFirebaseReady += OnFirebaseReady;
            CleanFirebaseManager.Instance.OnUserSignedIn += OnUserSignedIn;
            CleanFirebaseManager.Instance.OnError += OnFirebaseError;
        }
    }

    void CompleteInitializationDirectly(string connectionStatus)
    {
        ShowStatus("초기화 완료!", 1.0f);
        StartCoroutine(FinalizeInitialization(connectionStatus));
    }

    IEnumerator FinalizeInitialization(string connectionStatus)
    {
        yield return new WaitForSeconds(0.5f);
        
        HideLoadingPanel();
        SetButtonsInteractable(true);
        isInitialized = true;

        // 버전 텍스트 업데이트
        if (versionText != null)
        {
            versionText.text = $"v{gameVersion} {connectionStatus}";
        }

        Debug.Log("[TitleScene] ✅ 타이틀 초기화 완료!");
    }

    #region Firebase 이벤트 처리

    void OnFirebaseReady()
    {
        Debug.Log("[TitleScene] Firebase 준비 완료!");
        firebaseReady = true;
    }

    void OnUserSignedIn(bool success)
    {
        if (success)
        {
            Debug.Log("[TitleScene] ✅ Firebase 로그인 성공!");
            loginSuccessful = true;
            
            // 로그인이 진행 중일 때만 씬 전환
            if (isProcessingLogin)
            {
                StartCoroutine(StartGameSequenceAfterLogin());
            }
        }
        else
        {
            Debug.Log("[TitleScene] ❌ Firebase 로그인 실패");
            loginSuccessful = false;
        }
    }

    void OnFirebaseError(string error)
    {
        Debug.LogError($"[TitleScene] Firebase 오류: {error}");
        
        // 로그인 진행 중이면 오류에도 불구하고 계속 진행
        if (isProcessingLogin)
        {
            Debug.LogWarning("[TitleScene] 오류 발생했지만 로컬 모드로 계속 진행");
            loginSuccessful = false;
            StartCoroutine(StartGameSequenceAfterLogin());
        }
    }

    #endregion

    #region 버튼 이벤트

    void OnLoginButtonClicked()
    {
        if (!isInitialized || isProcessingLogin)
        {
            Debug.LogWarning("[TitleScene] 초기화 미완료 또는 로그인 처리 중");
            return;
        }


        // 실제 서비스 전에 활성화
        //Debug.Log("[TitleScene] 🎭 로그인 시작");
        //StartCoroutine(HandleLoginProcess());

        isProcessingLogin = true;
        StartCoroutine(StartGameSequenceAfterLogin());
    }

    IEnumerator HandleLoginProcess()
    {
        isProcessingLogin = true;
        loginSuccessful = false;
        
        ShowLoadingPanel();
        ShowStatus("로그인 중...", 0.3f);

        if (CleanFirebaseManager.Instance != null && CleanFirebaseManager.Instance.IsReady)
        {
            Debug.Log("[TitleScene] Firebase 익명 로그인 시작");
            ShowStatus("Firebase 로그인 중...", 0.5f);
            
            // Firebase 익명 로그인 시도
            CleanFirebaseManager.Instance.SignInAnonymously();
            
            // 로그인 완료 또는 타임아웃까지 대기
            float elapsedTime = 0f;
            while (!loginSuccessful && elapsedTime < loginTimeout)
            {
                elapsedTime += Time.deltaTime;
                float progress = 0.5f + (elapsedTime / loginTimeout) * 0.3f;
                ShowStatus($"Firebase 로그인 중... {(int)(loginTimeout - elapsedTime)}초", progress);
                yield return null;
            }
            
            if (loginSuccessful)
            {
                Debug.Log("[TitleScene] ✅ Firebase 로그인 완료!");
                ShowStatus("로그인 성공!", 0.9f);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Debug.LogWarning("[TitleScene] ⏰ 로그인 타임아웃 - 로컬 모드로 진행");
                ShowStatus("타임아웃 - 로컬 모드로 진행", 0.8f);
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            Debug.Log("[TitleScene] 📱 Firebase 없음 - 로컬 모드로 진행");
            ShowStatus("로컬 모드로 진행", 0.7f);
            yield return new WaitForSeconds(1f);
        }

        // 로그인 성공 여부와 관계없이 게임 시작
        StartCoroutine(StartGameSequenceAfterLogin());
    }

    #endregion

    #region 게임 시작

    IEnumerator StartGameSequenceAfterLogin()
    {
        // 중복 실행 방지
        if (!isProcessingLogin) yield break;
        
        ShowStatus("게임 시작 중...", 0.9f);
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[TitleScene] 🚀 로비 씬으로 전환 시작");

        // FirebaseDataManager 연결 상태 로그
        if (FirebaseDataManager.Instance != null)
        {
            bool isConnected = FirebaseDataManager.Instance.IsConnected;
            Debug.Log($"[TitleScene] FirebaseDataManager 연결 상태: {isConnected}");
        }

        // 씬 전환을 안전하게 처리
        yield return StartCoroutine(SafeSceneTransition());
    }

    IEnumerator SafeSceneTransition()
    {
        bool sceneLoadSuccess = false;

        // 첫 번째 시도: SceneTransitionManager 사용
        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log("[TitleScene] SceneTransitionManager로 씬 전환 시도");
            
            System.Exception caughtException = null;
            try
            {
                SceneTransitionManager.Instance.LoadScene("LobbyScene");
                sceneLoadSuccess = true;
            }
            catch (System.Exception ex)
            {
                caughtException = ex;
            }

            if (caughtException != null)
            {
                Debug.LogError($"[TitleScene] SceneTransitionManager 실패: {caughtException.Message}");
            }
        }

        // 첫 번째 시도가 실패하면 잠시 대기 후 직접 전환
        if (!sceneLoadSuccess)
        {
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("[TitleScene] 직접 씬 전환 시도");
            
            System.Exception directException = null;
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
                sceneLoadSuccess = true;
            }
            catch (System.Exception ex)
            {
                directException = ex;
            }

            if (directException != null)
            {
                Debug.LogError($"[TitleScene] 직접 씬 전환도 실패: {directException.Message}");
                
                // 최후의 시도: 1초 대기 후 재시도
                yield return new WaitForSeconds(1f);
                
                try
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
                }
                catch (System.Exception finalEx)
                {
                    Debug.LogError($"[TitleScene] 최종 씬 전환 실패: {finalEx.Message}");
                    ShowStatus("씬 전환 오류 - 재시도 필요", 0.5f);
                }
            }
        }
    }

    #endregion

    #region UI 헬퍼 메서드

    void ShowLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
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
        if (loadingStatusText != null)
        {
            loadingStatusText.text = message;
        }

        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = progress;
        }

        Debug.Log($"[TitleScene] {message} ({progress * 100:F0}%)");
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (loginButton != null)
        {
            loginButton.interactable = interactable;
        }
    }

    #endregion

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (CleanFirebaseManager.Instance != null)
        {
            CleanFirebaseManager.Instance.OnFirebaseReady -= OnFirebaseReady;
            CleanFirebaseManager.Instance.OnUserSignedIn -= OnUserSignedIn;
            CleanFirebaseManager.Instance.OnError -= OnFirebaseError;
        }

        isProcessingLogin = false;
    }
}
