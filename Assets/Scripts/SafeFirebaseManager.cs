// SafeFirebaseManager.cs - 오타 수정 버전
using System;
using System.Collections;
using UnityEngine;

public class SafeFirebaseManager : MonoBehaviour
{
    public static SafeFirebaseManager Instance { get; private set; }

    [Header("Firebase Status")]
    public bool isInitialized = false;
    public bool isAuthenticated = false;
    public bool useOfflineMode = false;

    [Header("Timeout Settings")]
    public float initializationTimeout = 10f; // 10초 타임아웃

    // 이벤트 (Firebase 없이도 작동)
    public event Action OnFirebaseInitialized;
    public event Action<bool> OnUserSignedIn; // bool로 성공/실패 표시
    public event Action OnUserSignedOut;
    public event Action<string> OnAuthError;

    private bool initializationStarted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SafeFirebaseManager 초기화");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeFirebaseWithTimeout());
    }

    IEnumerator InitializeFirebaseWithTimeout()
    {
        if (initializationStarted) yield break;
        initializationStarted = true;

        Debug.Log("Firebase 초기화 시작...");

        // 타임아웃 체크
        float elapsedTime = 0f;
        bool firebaseReady = false;

        // Firebase 초기화 시도
        StartCoroutine(TryInitializeFirebase((success) => { firebaseReady = success; }));

        // 타임아웃까지 대기
        while (elapsedTime < initializationTimeout && !firebaseReady && !isInitialized)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("Firebase 초기화 실패 또는 타임아웃 - 오프라인 모드로 전환");
            FallbackToOfflineMode();
        }

        Debug.Log($"Firebase 상태 - 초기화됨: {isInitialized}, 오프라인 모드: {useOfflineMode}");
    }

    IEnumerator TryInitializeFirebase(System.Action<bool> callback)
    {
        bool initSuccess = false;
        string errorMessage = "";

        // try-catch 블록 분리
        bool firebaseAvailable = SafeCheckFirebaseAvailability();

        if (firebaseAvailable)
        {
            Debug.Log("Firebase SDK 사용 가능 - 실제 초기화 시작");
            yield return StartCoroutine(SafeInitializeFirebaseSDK((success, error) => 
            {
                initSuccess = success;
                errorMessage = error;
            }));
        }
        else
        {
            Debug.LogWarning("Firebase SDK 사용 불가");
            errorMessage = "Firebase SDK not available";
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogError($"Firebase 초기화 오류: {errorMessage}");
        }

        callback?.Invoke(initSuccess);
    }

    bool SafeCheckFirebaseAvailability()
    {
        try
        {
            // Firebase 클래스가 사용 가능한지 확인
            var firebaseType = System.Type.GetType("Firebase.FirebaseApp, Firebase.App");
            return firebaseType != null;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Firebase 가용성 확인 실패: {ex.Message}");
            return false;
        }
    }

    IEnumerator SafeInitializeFirebaseSDK(System.Action<bool, string> callback)
    {
        bool success = false;
        string errorMessage = "";

        // 예외 처리를 코루틴 밖에서 수행
        yield return StartCoroutine(PerformFirebaseInitialization((result, error) =>
        {
            success = result;
            errorMessage = error;
        }));

        callback?.Invoke(success, errorMessage);
    }

    IEnumerator PerformFirebaseInitialization(System.Action<bool, string> callback)
    {
        Debug.Log("Firebase 의존성 확인 중...");
        yield return new WaitForSeconds(1f);

        Debug.Log("Firebase 앱 초기화 중...");
        yield return new WaitForSeconds(1f);

        // 실제 환경에서는 여기서 Firebase.FirebaseApp.CheckAndFixDependenciesAsync() 호출
        // 현재는 시뮬레이션으로 처리
        bool initResult = SimulateFirebaseInit();

        if (initResult)
        {
            isInitialized = true;
            OnFirebaseInitialized?.Invoke();
            Debug.Log("Firebase 초기화 완료!");
            callback?.Invoke(true, "");
        }
        else
        {
            callback?.Invoke(false, "Firebase initialization failed");
        }
    }

    bool SimulateFirebaseInit()
    {
        // 실제로는 Firebase SDK 초기화 결과
        // 현재는 성공으로 시뮬레이션 (테스트용)
        return true; // 항상 성공으로 테스트
    }

    void FallbackToOfflineMode()
    {
        useOfflineMode = true;
        isInitialized = true; // 오프라인 모드로 초기화 완료
        
        Debug.Log("오프라인 모드로 전환 - 로컬 데이터만 사용");
        OnFirebaseInitialized?.Invoke();
    }

    #region 공개 API (Firebase/오프라인 호환)

    /// <summary>
    /// 게스트 로그인 (익명)
    /// </summary>
    public IEnumerator SignInAnonymously(System.Action<bool> callback)
    {
        Debug.Log("게스트 로그인 시도...");

        if (useOfflineMode)
        {
            Debug.Log("오프라인 모드 - 게스트 로그인 성공");
            isAuthenticated = true;
            OnUserSignedIn?.Invoke(true);
            callback?.Invoke(true);
            yield break;
        }

        // Firebase 익명 로그인 시뮬레이션
        yield return new WaitForSeconds(1f);
        
        bool loginSuccess = PerformAnonymousLogin();
        
        if (loginSuccess)
        {
            isAuthenticated = true;
            OnUserSignedIn?.Invoke(true);
            Debug.Log("게스트 로그인 성공");
        }
        else
        {
            OnAuthError?.Invoke("게스트 로그인에 실패했습니다.");
        }
        
        callback?.Invoke(loginSuccess);
    }

    bool PerformAnonymousLogin()
    {
        // 실제로는 Firebase 익명 로그인
        // 현재는 시뮬레이션
        return true; // 게스트 로그인은 일반적으로 성공
    }

    /// <summary>
    /// 이메일 로그인
    /// </summary>
    public IEnumerator SignInWithEmail(string email, string password, System.Action<bool> callback)
    {
        Debug.Log($"이메일 로그인 시도: {email}");

        if (useOfflineMode)
        {
            Debug.Log("오프라인 모드 - 로컬 계정 확인");
            bool loginSuccess = ValidateOfflineCredentials(email, password);
            
            if (loginSuccess)
            {
                isAuthenticated = true;
                OnUserSignedIn?.Invoke(true);
            }
            else
            {
                OnAuthError?.Invoke("이메일 또는 패스워드를 확인해주세요.");
            }
            
            callback?.Invoke(loginSuccess);
            yield break;
        }

        // Firebase 이메일 로그인 시뮬레이션
        yield return new WaitForSeconds(1.5f);
        
        bool emailLoginSuccess = ValidateEmailCredentials(email, password);
        
        if (emailLoginSuccess)
        {
            isAuthenticated = true;
            OnUserSignedIn?.Invoke(true);
            Debug.Log("이메일 로그인 성공");
        }
        else
        {
            OnAuthError?.Invoke("이메일 형식이 올바르지 않거나 패스워드가 너무 짧습니다.");
        }
        
        callback?.Invoke(emailLoginSuccess);
    }

    bool ValidateOfflineCredentials(string email, string password)
    {
        // 오프라인 계정 검증
        string savedEmail = PlayerPrefs.GetString("OfflineAccount_Email", "");
        string savedPassword = PlayerPrefs.GetString("OfflineAccount_Password", "");
        
        return email == savedEmail && password == savedPassword;
    }

    bool ValidateEmailCredentials(string email, string password)
    {
        // 간단한 이메일/패스워드 검증
        return email.Contains("@") && password.Length >= 6;
    }

    /// <summary>
    /// 회원가입
    /// </summary>
    public IEnumerator SignUpWithEmail(string email, string password, System.Action<bool> callback)
    {
        Debug.Log($"회원가입 시도: {email}");

        if (useOfflineMode)
        {
            Debug.Log("오프라인 모드 - 로컬 계정 생성");
            CreateOfflineAccount(email, password);
            
            isAuthenticated = true;
            OnUserSignedIn?.Invoke(true);
            callback?.Invoke(true);
            yield break;
        }

        // Firebase 회원가입 시뮬레이션
        yield return new WaitForSeconds(2f);
        
        bool signupSuccess = ValidateSignupCredentials(email, password);
        
        if (signupSuccess)
        {
            isAuthenticated = true;
            OnUserSignedIn?.Invoke(true);
            Debug.Log("회원가입 성공");
        }
        else
        {
            OnAuthError?.Invoke("이메일 형식이 올바르지 않거나 패스워드가 너무 짧습니다.");
        }
        
        callback?.Invoke(signupSuccess);
    }

    void CreateOfflineAccount(string email, string password)
    {
        // 오프라인 계정 생성 (실제로는 해시 처리 필요)
        PlayerPrefs.SetString("OfflineAccount_Email", email);
        PlayerPrefs.SetString("OfflineAccount_Password", password);
        PlayerPrefs.Save();
    }

    bool ValidateSignupCredentials(string email, string password)
    {
        return email.Contains("@") && password.Length >= 6;
    }

    /// <summary>
    /// 로그아웃
    /// </summary>
    public void SignOut()
    {
        isAuthenticated = false;
        OnUserSignedOut?.Invoke();
        Debug.Log("로그아웃 완료");
    }

    #endregion

    #region 상태 확인

    public bool IsFirebaseReady()
    {
        return isInitialized;
    }

    public bool IsOnlineMode()
    {
        return isInitialized && !useOfflineMode;
    }

    public bool IsOfflineMode()
    {
        return useOfflineMode;
    }

    #endregion
}