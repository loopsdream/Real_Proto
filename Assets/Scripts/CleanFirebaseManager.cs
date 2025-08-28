// CleanFirebaseManager.cs - Database URL 설정 추가된 버전
using System;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class CleanFirebaseManager : MonoBehaviour
{
    public static CleanFirebaseManager Instance { get; private set; }

    [Header("Firebase Settings")]
    [Tooltip("Firebase Database URL - Firebase Console에서 확인 가능")]
    public string databaseURL = "https://croxcro-default-rtdb.asia-southeast1.firebasedatabase.app/";
    
    [Header("Firebase Status")]
    public bool isInitialized = false;
    public bool isAuthenticated = false;

    // Firebase 인스턴스들
    private FirebaseApp app;
    private FirebaseAuth auth;
    private DatabaseReference database;

    // 현재 사용자 정보
    public FirebaseUser CurrentUser => auth?.CurrentUser;
    public string CurrentUserId => CurrentUser?.UserId ?? "";

    // 간단한 이벤트들
    public event Action OnFirebaseReady;
    public event Action<bool> OnUserSignedIn;
    public event Action<string> OnError;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeFirebase()
    {
        Debug.Log("[Firebase] 초기화 시작...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            // Task.Result 대신 task.IsCompleted와 task.Exception 사용
            if (task.IsCompleted && !task.IsFaulted)
            {
                var dependencyStatus = task.Result;
                
                if (dependencyStatus == DependencyStatus.Available)
                {
                    try
                    {
                        app = FirebaseApp.DefaultInstance;
                        auth = FirebaseAuth.DefaultInstance;
                        
                        // Database URL 설정하여 초기화
                        InitializeDatabaseWithURL();

                        // 인증 상태 변경 감지
                        auth.StateChanged += OnAuthStateChanged;

                        isInitialized = true;
                        Debug.Log("[Firebase] ✅ 초기화 완료!");
                        OnFirebaseReady?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Firebase] ❌ 초기화 실패: {ex.Message}");
                        OnError?.Invoke($"초기화 실패: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[Firebase] ❌ 의존성 문제: {dependencyStatus}");
                    OnError?.Invoke($"Firebase 의존성 문제: {dependencyStatus}");
                }
            }
            else if (task.IsFaulted)
            {
                Debug.LogError($"[Firebase] ❌ 의존성 체크 실패: {task.Exception}");
                OnError?.Invoke($"의존성 체크 실패: {task.Exception?.InnerException?.Message}");
            }
        });
    }

    void InitializeDatabaseWithURL()
    {
        try
        {
            // 방법 1: Inspector에서 설정한 URL 사용
            if (!string.IsNullOrEmpty(databaseURL))
            {
                Debug.Log($"[Firebase] 🔗 Database URL 설정: {databaseURL}");
                database = FirebaseDatabase.GetInstance(app, databaseURL).RootReference;
                Debug.Log("[Firebase] ✅ Database URL 설정 성공");
                return;
            }

            // 방법 2: 일반적인 URL 패턴들 시도
            string[] possibleURLs = {
                "https://croxcro-default-rtdb.asia-southeast1.firebasedatabase.app/",
                "https://croxcro-default-rtdb.firebaseio.com/",
                "https://croxcro.firebaseio.com/"
            };

            foreach (string url in possibleURLs)
            {
                try
                {
                    Debug.Log($"[Firebase] 🔍 URL 시도: {url}");
                    database = FirebaseDatabase.GetInstance(app, url).RootReference;
                    databaseURL = url; // 성공한 URL 저장
                    Debug.Log($"[Firebase] ✅ Database URL 성공: {url}");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Firebase] ⚠️ URL 실패 ({url}): {ex.Message}");
                }
            }

            // 방법 3: 기본 인스턴스 시도 (URL 없이)
            try
            {
                Debug.Log("[Firebase] 🔍 기본 Database 인스턴스 시도...");
                database = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("[Firebase] ✅ 기본 Database 인스턴스 성공");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Firebase] ⚠️ 기본 인스턴스도 실패: {ex.Message}");
                Debug.LogWarning("[Firebase] 🚨 Database를 사용할 수 없습니다. Auth만 사용됩니다.");
                database = null; // Database 없이 Auth만 사용
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Database 초기화 완전 실패: {ex.Message}");
            database = null;
        }
    }

    void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        bool wasAuthenticated = isAuthenticated;
        isAuthenticated = auth?.CurrentUser != null;

        if (isAuthenticated != wasAuthenticated)
        {
            if (isAuthenticated)
            {
                Debug.Log($"[Firebase] ✅ 사용자 로그인: {CurrentUserId.Substring(0, 8)}...");
                OnUserSignedIn?.Invoke(true);
            }
            else
            {
                Debug.Log("[Firebase] 🚪 사용자 로그아웃");
                OnUserSignedIn?.Invoke(false);
            }
        }
    }

    #region 인증 메서드들

    public void SignInAnonymously()
    {
        if (!isInitialized)
        {
            OnError?.Invoke("Firebase가 초기화되지 않았습니다.");
            return;
        }

        Debug.Log("[Firebase] 🎭 익명 로그인 시도...");
        
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("[Firebase] ❌ 익명 로그인 취소");
                OnError?.Invoke("로그인이 취소되었습니다.");
                return;
            }
            
            if (task.IsFaulted)
            {
                Debug.LogError($"[Firebase] ❌ 익명 로그인 실패: {task.Exception}");
                OnError?.Invoke("익명 로그인에 실패했습니다.");
                return;
            }

            // Task.Result 대신 task.IsCompleted 체크 후 접근
            if (task.IsCompleted && !task.IsFaulted)
            {
                var authResult = task.Result;
                Debug.Log($"[Firebase] ✅ 익명 로그인 성공: {authResult.User.UserId.Substring(0, 8)}...");
            }
        });
    }

    public void SignInWithEmailPassword(string email, string password)
    {
        if (!isInitialized)
        {
            OnError?.Invoke("Firebase가 초기화되지 않았습니다.");
            return;
        }

        Debug.Log("[Firebase] 📧 이메일 로그인 시도...");
        
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("[Firebase] ❌ 이메일 로그인 취소");
                OnError?.Invoke("로그인이 취소되었습니다.");
                return;
            }
            
            if (task.IsFaulted)
            {
                Debug.LogError($"[Firebase] ❌ 이메일 로그인 실패: {task.Exception}");
                OnError?.Invoke("이메일 또는 패스워드가 잘못되었습니다.");
                return;
            }

            if (task.IsCompleted && !task.IsFaulted)
            {
                var authResult = task.Result;
                Debug.Log($"[Firebase] ✅ 이메일 로그인 성공: {authResult.User.Email}");
            }
        });
    }

    public void CreateUserWithEmailPassword(string email, string password)
    {
        if (!isInitialized)
        {
            OnError?.Invoke("Firebase가 초기화되지 않았습니다.");
            return;
        }

        Debug.Log("[Firebase] 👤 계정 생성 시도...");
        
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("[Firebase] ❌ 계정 생성 취소");
                OnError?.Invoke("계정 생성이 취소되었습니다.");
                return;
            }
            
            if (task.IsFaulted)
            {
                Debug.LogError($"[Firebase] ❌ 계정 생성 실패: {task.Exception}");
                OnError?.Invoke("계정 생성에 실패했습니다.");
                return;
            }

            if (task.IsCompleted && !task.IsFaulted)
            {
                var authResult = task.Result;
                Debug.Log($"[Firebase] ✅ 계정 생성 성공: {authResult.User.Email}");
            }
        });
    }

    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            Debug.Log("[Firebase] 🚪 로그아웃");
        }
    }

    #endregion

    #region 데이터베이스 메서드들

    public void SaveUserData(string userId, object data)
    {
        if (!isInitialized)
        {
            OnError?.Invoke("Firebase가 초기화되지 않았습니다.");
            return;
        }

        if (database == null)
        {
            Debug.LogWarning("[Firebase] ⚠️ Database가 없어 데이터 저장을 건너뜁니다.");
            return;
        }

        string json = JsonUtility.ToJson(data, true);
        
        database.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[Firebase] ❌ 데이터 저장 실패: {task.Exception}");
                OnError?.Invoke("데이터 저장에 실패했습니다.");
                return;
            }

            Debug.Log($"[Firebase] ✅ 데이터 저장 성공: {userId}");
        });
    }

    public void LoadUserData(string userId, System.Action<string> onComplete)
    {
        if (!isInitialized)
        {
            OnError?.Invoke("Firebase가 초기화되지 않았습니다.");
            return;
        }

        if (database == null)
        {
            Debug.LogWarning("[Firebase] ⚠️ Database가 없어 데이터 로드를 건너뜁니다.");
            onComplete?.Invoke(null);
            return;
        }

        database.Child("users").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[Firebase] ❌ 데이터 로드 실패: {task.Exception}");
                OnError?.Invoke("데이터 로드에 실패했습니다.");
                return;
            }

            if (task.IsCompleted && !task.IsFaulted)
            {
                var snapshot = task.Result;
                if (snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    Debug.Log($"[Firebase] ✅ 데이터 로드 성공: {userId}");
                    onComplete?.Invoke(json);
                }
                else
                {
                    Debug.Log($"[Firebase] ⚠️ 사용자 데이터 없음: {userId}");
                    onComplete?.Invoke(null);
                }
            }
        });
    }

    public void UpdateLeaderboard(string leaderboardType, int score, string displayName)
    {
        if (!isInitialized || !isAuthenticated)
        {
            OnError?.Invoke("Firebase 연결 또는 로그인이 필요합니다.");
            return;
        }

        if (database == null)
        {
            Debug.LogWarning("[Firebase] ⚠️ Database가 없어 리더보드 업데이트를 건너뜁니다.");
            return;
        }

        var leaderboardData = new
        {
            userId = CurrentUserId,
            displayName = displayName,
            score = score,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        string json = JsonUtility.ToJson(leaderboardData, true);
        
        database.Child("leaderboards").Child(leaderboardType).Child(CurrentUserId)
            .SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[Firebase] ❌ 리더보드 업데이트 실패: {task.Exception}");
                OnError?.Invoke("리더보드 업데이트에 실패했습니다.");
                return;
            }

            Debug.Log($"[Firebase] ✅ 리더보드 업데이트 성공: {displayName} - {score}점");
        });
    }

    #endregion

    #region 상태 확인

    public bool IsReady => isInitialized;
    public bool IsLoggedIn => isAuthenticated;
    public bool IsOnline => isInitialized && Application.internetReachability != NetworkReachability.NotReachable;
    public bool IsConnected => isInitialized;

    #endregion

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= OnAuthStateChanged;
        }
    }
}
