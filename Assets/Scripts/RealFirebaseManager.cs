// RealFirebaseManager.cs - try-catch yield 오류 수정 버전
using System;
using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class RealFirebaseManager : MonoBehaviour
{
    public static RealFirebaseManager Instance { get; private set; }

    [Header("Firebase Status")]
    public bool isInitialized = false;
    public bool isAuthenticated = false;
    public bool useOfflineMode = false;

    [Header("Timeout Settings")]
    public float initializationTimeout = 15f;

    [Header("Firebase Settings")]
    [Tooltip("Firebase Database URL (예: https://croxcro-default-rtdb.asia-southeast1.firebasedatabase.app/)")]
    public string databaseURL = "https://croxcro-default-rtdb.asia-southeast1.firebasedatabase.app/";

    // Firebase 인스턴스들
    private FirebaseApp app;
    private FirebaseAuth auth;
    private DatabaseReference databaseRef;

    // 현재 사용자 정보
    public FirebaseUser CurrentUser => auth?.CurrentUser;
    public string CurrentUserId => CurrentUser?.UserId ?? "";
    public string CurrentUserEmail => CurrentUser?.Email ?? "";

    // 이벤트
    public event Action OnFirebaseInitialized;
    public event Action<bool> OnUserSignedIn;
    public event Action OnUserSignedOut;
    public event Action<string> OnAuthError;

    private bool initializationStarted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("🔥 RealFirebaseManager 초기화");
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

        Debug.Log("🚀 실제 Firebase 초기화 시작...");

        float elapsedTime = 0f;
        bool firebaseReady = false;

        StartCoroutine(InitializeRealFirebase((success) => { firebaseReady = success; }));

        while (elapsedTime < initializationTimeout && !firebaseReady && !isInitialized)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("⚠️ Firebase 초기화 타임아웃 - 오프라인 모드로 전환");
            FallbackToOfflineMode();
        }

        Debug.Log($"🏁 Firebase 최종 상태 - 초기화됨: {isInitialized}, 오프라인 모드: {useOfflineMode}");
    }

    IEnumerator InitializeRealFirebase(System.Action<bool> callback)
    {
        Debug.Log("🔍 Firebase 의존성 확인 중...");

        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        
        while (!dependencyTask.IsCompleted)
        {
            yield return null;
        }

        DependencyStatus dependencyStatus = dependencyTask.Result;
        
        if (dependencyStatus == DependencyStatus.Available)
        {
            Debug.Log("✅ Firebase 의존성 확인 완료 - 앱 초기화 중...");
            
            // try-catch를 코루틴 밖으로 분리
            bool initSuccess = false;
            string errorMessage = "";
            
            yield return StartCoroutine(SafeInitializeFirebase((success, error) =>
            {
                initSuccess = success;
                errorMessage = error;
            }));

            if (initSuccess)
            {
                isInitialized = true;
                OnFirebaseInitialized?.Invoke();
                Debug.Log("🎉 Firebase 초기화 완료!");
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError($"❌ Firebase 초기화 실패: {errorMessage}");
                callback?.Invoke(false);
            }
        }
        else
        {
            Debug.LogError($"❌ Firebase 의존성 오류: {dependencyStatus}");
            callback?.Invoke(false);
        }
    }

    IEnumerator SafeInitializeFirebase(System.Action<bool, string> callback)
    {
        string errorMessage = "";
        bool success = false;

        // Firebase 앱 초기화 (예외 처리 분리)
        if (InitializeFirebaseApp())
        {
            // Database 초기화 (예외 처리 분리)
            yield return StartCoroutine(SafeInitializeDatabase());
            
            // Auth 설정
            if (SetupAuthentication())
            {
                success = true;
            }
            else
            {
                errorMessage = "Authentication 설정 실패";
            }
        }
        else
        {
            errorMessage = "Firebase App 초기화 실패";
        }

        callback?.Invoke(success, errorMessage);
    }

    bool InitializeFirebaseApp()
    {
        try
        {
            app = FirebaseApp.DefaultInstance;
            Debug.Log("✅ Firebase App 초기화 성공");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Firebase App 초기화 실패: {ex.Message}");
            return false;
        }
    }

    IEnumerator SafeInitializeDatabase()
    {
        Debug.Log("🗄️ Firebase Database 초기화 중...");
        
        bool dbSuccess = InitializeDatabaseWithURL();
        
        if (!dbSuccess)
        {
            Debug.LogWarning("⚠️ Database 초기화 실패 - Auth만 사용");
        }
        
        yield return null; // 코루틴 요구사항
    }

    bool InitializeDatabaseWithURL()
    {
        // 방법 1: Inspector에서 설정한 URL 사용
        if (!string.IsNullOrEmpty(databaseURL))
        {
            if (TryDatabaseURL(databaseURL))
            {
                Debug.Log($"✅ 설정된 Database URL 성공: {databaseURL}");
                return true;
            }
        }

        // 방법 2: 일반적인 URL 패턴으로 시도
        string[] possibleURLs = {
            "https://croxcro-default-rtdb.asia-southeast1.firebasedatabase.app/",
            "https://croxcro-default-rtdb.firebaseio.com/",
            "https://croxcro.firebaseio.com/"
        };

        foreach (string url in possibleURLs)
        {
            if (TryDatabaseURL(url))
            {
                databaseURL = url; // 성공한 URL 저장
                Debug.Log($"✅ Database URL 성공: {url}");
                return true;
            }
        }

        // 방법 3: 기본 인스턴스 시도
        if (TryDefaultDatabase())
        {
            Debug.Log("✅ 기본 Database 인스턴스 성공");
            return true;
        }

        Debug.LogWarning("⚠️ 모든 Database URL 시도 실패");
        return false;
    }

    bool TryDatabaseURL(string url)
    {
        try
        {
            Debug.Log($"🔗 Database URL 시도: {url}");
            databaseRef = FirebaseDatabase.GetInstance(app, url).RootReference;
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"⚠️ URL 실패 ({url}): {ex.Message}");
            return false;
        }
    }

    bool TryDefaultDatabase()
    {
        try
        {
            Debug.Log("🔗 기본 Database 인스턴스 시도...");
            databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"⚠️ 기본 인스턴스 실패: {ex.Message}");
            return false;
        }
    }

    bool SetupAuthentication()
    {
        try
        {
            auth = FirebaseAuth.DefaultInstance;
            auth.StateChanged += OnAuthStateChanged;
            CheckExistingUser();
            Debug.Log("✅ Authentication 설정 완료");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Authentication 설정 실패: {ex.Message}");
            return false;
        }
    }

    void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != null)
        {
            isAuthenticated = true;
            Debug.Log($"🔐 사용자 인증됨: {auth.CurrentUser.Email ?? "익명"}");
            OnUserSignedIn?.Invoke(true);
        }
        else
        {
            isAuthenticated = false;
            Debug.Log("🔓 사용자 로그아웃");
            OnUserSignedOut?.Invoke();
        }
    }

    void CheckExistingUser()
    {
        if (auth?.CurrentUser != null)
        {
            Debug.Log($"🔄 기존 로그인 사용자 감지: {auth.CurrentUser.Email ?? "익명"}");
            isAuthenticated = true;
        }
    }

    void FallbackToOfflineMode()
    {
        useOfflineMode = true;
        isInitialized = true;
        
        Debug.Log("📱 오프라인 모드로 전환");
        OnFirebaseInitialized?.Invoke();
    }

    #region 실제 Firebase 인증 API

    public IEnumerator SignInAnonymously(System.Action<bool> callback)
    {
        Debug.Log("🎭 익명 로그인 시도...");

        if (useOfflineMode)
        {
            Debug.Log("📱 오프라인 모드 - 로컬 익명 로그인");
            isAuthenticated = true;
            OnUserSignedIn?.Invoke(true);
            callback?.Invoke(true);
            yield break;
        }

        if (auth == null)
        {
            Debug.LogError("❌ Firebase Auth가 초기화되지 않음");
            OnAuthError?.Invoke("Firebase 인증이 준비되지 않았습니다.");
            callback?.Invoke(false);
            yield break;
        }

        var signInTask = auth.SignInAnonymouslyAsync();
        
        while (!signInTask.IsCompleted)
        {
            yield return null;
        }

        if (signInTask.Exception != null)
        {
            Debug.LogError($"❌ 익명 로그인 실패: {signInTask.Exception}");
            HandleAuthException(signInTask.Exception);
            callback?.Invoke(false);
        }
        else
        {
            FirebaseUser newUser = signInTask.Result.User;
            Debug.Log($"✅ 익명 로그인 성공: {newUser.UserId}");
            
            if (databaseRef != null)
            {
                yield return StartCoroutine(InitializeUserData(newUser.UserId));
            }
            else
            {
                Debug.Log("ℹ️ Database 없이 인증만 완료");
            }
            
            callback?.Invoke(true);
        }
    }

    public IEnumerator SignInWithEmail(string email, string password, System.Action<bool> callback)
    {
        Debug.Log($"📧 이메일 로그인 시도: {email}");

        if (useOfflineMode)
        {
            bool success = ValidateOfflineCredentials(email, password);
            
            if (success)
            {
                isAuthenticated = true;
                OnUserSignedIn?.Invoke(true);
            }
            else
            {
                OnAuthError?.Invoke("오프라인 모드: 계정 정보가 일치하지 않습니다.");
            }
            
            callback?.Invoke(success);
            yield break;
        }

        if (auth == null)
        {
            OnAuthError?.Invoke("Firebase 인증이 준비되지 않았습니다.");
            callback?.Invoke(false);
            yield break;
        }

        var signInTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        
        while (!signInTask.IsCompleted)
        {
            yield return null;
        }

        if (signInTask.Exception != null)
        {
            Debug.LogError($"❌ 이메일 로그인 실패: {signInTask.Exception}");
            HandleAuthException(signInTask.Exception);
            callback?.Invoke(false);
        }
        else
        {
            FirebaseUser user = signInTask.Result.User;
            Debug.Log($"✅ 이메일 로그인 성공: {user.Email}");
            callback?.Invoke(true);
        }
    }

    public IEnumerator SignUpWithEmail(string email, string password, System.Action<bool> callback)
    {
        Debug.Log($"📝 회원가입 시도: {email}");

        if (useOfflineMode)
        {
            CreateOfflineAccount(email, password);
            isAuthenticated = true;
            OnUserSignedIn?.Invoke(true);
            callback?.Invoke(true);
            yield break;
        }

        if (auth == null)
        {
            OnAuthError?.Invoke("Firebase 인증이 준비되지 않았습니다.");
            callback?.Invoke(false);
            yield break;
        }

        var createTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        
        while (!createTask.IsCompleted)
        {
            yield return null;
        }

        if (createTask.Exception != null)
        {
            Debug.LogError($"❌ 회원가입 실패: {createTask.Exception}");
            HandleAuthException(createTask.Exception);
            callback?.Invoke(false);
        }
        else
        {
            FirebaseUser newUser = createTask.Result.User;
            Debug.Log($"✅ 회원가입 성공: {newUser.Email}");
            
            if (databaseRef != null)
            {
                yield return StartCoroutine(InitializeUserData(newUser.UserId));
            }
            
            callback?.Invoke(true);
        }
    }

    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            Debug.Log("🔓 로그아웃 완료");
        }
        
        isAuthenticated = false;
        OnUserSignedOut?.Invoke();
    }

    #endregion

    #region 사용자 데이터 관리

    IEnumerator InitializeUserData(string userId)
    {
        if (databaseRef == null)
        {
            Debug.LogWarning("⚠️ Database reference가 없음 - 사용자 데이터 초기화 생략");
            yield break;
        }

        Debug.Log($"🗄️ 사용자 데이터 초기화: {userId}");

        var userData = new
        {
            userId = userId,
            email = CurrentUserEmail,
            displayName = CurrentUser?.DisplayName ?? "Player",
            createdAt = DateTime.UtcNow.ToBinary(),
            lastLoginAt = DateTime.UtcNow.ToBinary(),
            coins = 1000,
            diamonds = 50,
            energy = 5,
            lastEnergyTime = DateTime.UtcNow.ToBinary(),
            currentStage = 1,
            highestStage = 1,
            totalScore = 0
        };

        string json = JsonUtility.ToJson(userData, true);
        var setTask = databaseRef.Child("users").Child(userId).SetRawJsonValueAsync(json);
        
        while (!setTask.IsCompleted)
        {
            yield return null;
        }

        if (setTask.Exception != null)
        {
            Debug.LogError($"❌ 사용자 데이터 초기화 실패: {setTask.Exception}");
        }
        else
        {
            Debug.Log("✅ 사용자 데이터 초기화 완료");
        }
    }

    #endregion

    #region 오류 처리

    void HandleAuthException(AggregateException exception)
    {
        foreach (var innerException in exception.InnerExceptions)
        {
            if (innerException is FirebaseException firebaseException)
            {
                string errorMessage = GetAuthErrorMessage((int)firebaseException.ErrorCode);
                OnAuthError?.Invoke(errorMessage);
                return;
            }
        }
        
        OnAuthError?.Invoke("인증 중 알 수 없는 오류가 발생했습니다.");
    }

    string GetAuthErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            17007 => "이미 사용 중인 이메일입니다.",
            17008 => "잘못된 이메일 형식입니다.",
            17009 => "잘못된 패스워드입니다.",
            17011 => "존재하지 않는 사용자입니다.",
            17026 => "패스워드가 너무 약합니다. (6자 이상 입력해주세요)",
            17020 => "네트워크 연결을 확인해주세요.",
            _ => "알 수 없는 오류가 발생했습니다."
        };
    }

    #endregion

    #region 오프라인 모드 지원

    bool ValidateOfflineCredentials(string email, string password)
    {
        string savedEmail = PlayerPrefs.GetString("OfflineAccount_Email", "");
        string savedPassword = PlayerPrefs.GetString("OfflineAccount_Password", "");
        return email == savedEmail && password == savedPassword;
    }

    void CreateOfflineAccount(string email, string password)
    {
        PlayerPrefs.SetString("OfflineAccount_Email", email);
        PlayerPrefs.SetString("OfflineAccount_Password", password);
        PlayerPrefs.Save();
        Debug.Log("📱 오프라인 계정 생성 완료");
    }

    #endregion

    #region 상태 확인

    public bool IsFirebaseReady() => isInitialized;
    public bool IsOnlineMode() => isInitialized && !useOfflineMode;
    public bool IsOfflineMode() => useOfflineMode;
    public bool HasDatabase() => databaseRef != null;

    public DatabaseReference GetDatabaseReference(string path = "")
    {
        if (databaseRef == null) return null;
        return string.IsNullOrEmpty(path) ? databaseRef : databaseRef.Child(path);
    }

    #endregion

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= OnAuthStateChanged;
        }
    }
}