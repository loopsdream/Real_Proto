// FirebaseManager.cs - Firebase 초기화 및 인증 관리
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Header("Firebase Status")]
    public bool isInitialized = false;
    public bool isAuthenticated = false;

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
    public event Action<FirebaseUser> OnUserSignedIn;
    public event Action OnUserSignedOut;
    public event Action<string> OnAuthError;

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
            
            InitializeFirebase();
        }
        else
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }
    }

    void InitializeFirebase()
    {
        Debug.Log("Firebase 초기화 시작...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase 앱 초기화
                app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                databaseRef = FirebaseDatabase.DefaultInstance.RootReference;

                // 인증 상태 변경 리스너 등록
                auth.StateChanged += OnAuthStateChanged;

                isInitialized = true;
                Debug.Log("Firebase 초기화 완료!");
                OnFirebaseInitialized?.Invoke();

                // 자동 로그인 체크
                CheckExistingUser();
            }
            else
            {
                Debug.LogError($"Firebase 초기화 실패: {dependencyStatus}");
            }
        });
    }

    void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != null && auth.CurrentUser != null)
        {
            // 사용자 로그인됨
            isAuthenticated = true;
            Debug.Log($"사용자 로그인: {auth.CurrentUser.Email}");
            OnUserSignedIn?.Invoke(auth.CurrentUser);
        }
        else
        {
            // 사용자 로그아웃됨
            isAuthenticated = false;
            Debug.Log("사용자 로그아웃");
            OnUserSignedOut?.Invoke();
        }
    }

    void CheckExistingUser()
    {
        if (auth.CurrentUser != null)
        {
            Debug.Log($"기존 로그인 사용자 감지: {auth.CurrentUser.Email}");
            isAuthenticated = true;
        }
    }

    #region 이메일/패스워드 인증

    /// <summary>
    /// 이메일로 회원가입
    /// </summary>
    public async Task<bool> SignUpWithEmail(string email, string password)
    {
        if (!isInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            return false;
        }

        try
        {
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            
            if (result.User != null)
            {
                Debug.Log($"회원가입 성공: {result.User.Email}");
                
                // 사용자 데이터 초기화
                await InitializeUserData(result.User.UserId);
                return true;
            }
        }
        catch (FirebaseException ex)
        {
            string errorMessage = GetAuthErrorMessage(ex.ErrorCode);
            Debug.LogError($"회원가입 실패: {errorMessage}");
            OnAuthError?.Invoke(errorMessage);
        }

        return false;
    }

    /// <summary>
    /// 이메일로 로그인
    /// </summary>
    public async Task<bool> SignInWithEmail(string email, string password)
    {
        if (!isInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            return false;
        }

        try
        {
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            
            if (result.User != null)
            {
                Debug.Log($"로그인 성공: {result.User.Email}");
                return true;
            }
        }
        catch (FirebaseException ex)
        {
            string errorMessage = GetAuthErrorMessage(ex.ErrorCode);
            Debug.LogError($"로그인 실패: {errorMessage}");
            OnAuthError?.Invoke(errorMessage);
        }

        return false;
    }

    #endregion

    #region 익명 로그인

    /// <summary>
    /// 익명 사용자로 로그인 (게스트)
    /// </summary>
    public async Task<bool> SignInAnonymously()
    {
        if (!isInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            return false;
        }

        try
        {
            var result = await auth.SignInAnonymouslyAsync();
            
            if (result.User != null)
            {
                Debug.Log($"익명 로그인 성공: {result.User.UserId}");
                
                // 익명 사용자 데이터 초기화
                await InitializeUserData(result.User.UserId);
                return true;
            }
        }
        catch (FirebaseException ex)
        {
            string errorMessage = GetAuthErrorMessage(ex.ErrorCode);
            Debug.LogError($"익명 로그인 실패: {errorMessage}");
            OnAuthError?.Invoke(errorMessage);
        }

        return false;
    }

    #endregion

    #region 로그아웃 및 계정 관리

    /// <summary>
    /// 로그아웃
    /// </summary>
    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            Debug.Log("로그아웃 완료");
        }
    }

    /// <summary>
    /// 계정 삭제
    /// </summary>
    public async Task<bool> DeleteAccount()
    {
        if (auth?.CurrentUser != null)
        {
            try
            {
                // 사용자 데이터 삭제
                await DeleteUserData(auth.CurrentUser.UserId);
                
                // 계정 삭제
                await auth.CurrentUser.DeleteAsync();
                Debug.Log("계정 삭제 완료");
                return true;
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"계정 삭제 실패: {ex.Message}");
                OnAuthError?.Invoke("계정 삭제에 실패했습니다.");
            }
        }

        return false;
    }

    #endregion

    #region 사용자 데이터 관리

    /// <summary>
    /// 새 사용자 데이터 초기화
    /// </summary>
    async Task InitializeUserData(string userId)
    {
        try
        {
            var userData = new UserData
            {
                playerInfo = new PlayerInfo
                {
                    playerName = CurrentUser?.DisplayName ?? "Player",
                    level = 1,
                    currentStage = 1,
                    lastLoginTime = DateTime.UtcNow.ToBinary().ToString()
                },
                currencies = new Currencies
                {
                    gameCoins = 1000,
                    diamonds = 50,
                    energy = 5,
                    maxEnergy = 5,
                    lastEnergyTime = DateTime.UtcNow.ToBinary().ToString()
                }
            };

            string json = JsonUtility.ToJson(userData, true);
            await databaseRef.Child("users").Child(userId).SetRawJsonValueAsync(json);
            
            Debug.Log($"사용자 데이터 초기화 완료: {userId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"사용자 데이터 초기화 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 사용자 데이터 삭제
    /// </summary>
    async Task DeleteUserData(string userId)
    {
        try
        {
            await databaseRef.Child("users").Child(userId).RemoveValueAsync();
            Debug.Log($"사용자 데이터 삭제 완료: {userId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"사용자 데이터 삭제 실패: {ex.Message}");
        }
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// Firebase 인증 오류 메시지 변환
    /// </summary>
    string GetAuthErrorMessage(int errorCode)
    {
        switch (errorCode)
        {
            case 17007: return "이미 사용 중인 이메일입니다.";
            case 17008: return "잘못된 이메일 형식입니다.";
            case 17009: return "잘못된 패스워드입니다.";
            case 17011: return "존재하지 않는 사용자입니다.";
            case 17026: return "패스워드가 너무 약합니다.";
            case 17020: return "네트워크 연결을 확인해주세요.";
            default: return "알 수 없는 오류가 발생했습니다.";
        }
    }

    /// <summary>
    /// 데이터베이스 레퍼런스 반환
    /// </summary>
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