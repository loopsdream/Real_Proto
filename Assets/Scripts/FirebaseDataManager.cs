// FirebaseDataManager.cs - 래퍼 클래스를 사용하도록 최종 수정
using System;
using UnityEngine;

public class FirebaseDataManager : MonoBehaviour
{
    public static FirebaseDataManager Instance { get; private set; }

    [Header("Settings")]
    public bool autoSyncEnabled = true;
    public float autoSyncInterval = 30f;
    
    private bool isConnected = false;
    private float lastSyncTime = 0f;
    private FirebaseUserDataWrapper dataWrapper;

    // 이벤트
    public event Action<bool> OnSyncCompleted;
    public event Action<string> OnSyncError;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

void Start()
    {
        // 안전한 초기화를 위해 코루틴 사용
        StartCoroutine(SafeInitialization());
    }
    
System.Collections.IEnumerator SafeInitialization()
    {
        // 다른 매니저들이 초기화될 때까지 충분히 대기
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[DataManager] 안전한 초기화 시작");
        
        // UserDataManager 먼저 대기
        yield return StartCoroutine(WaitForUserDataManager());
        
        // CleanFirebaseManager 이벤트 구독
        if (CleanFirebaseManager.Instance != null)
        {
            CleanFirebaseManager.Instance.OnFirebaseReady += OnFirebaseReady;
            CleanFirebaseManager.Instance.OnUserSignedIn += OnUserSignedIn;
            CleanFirebaseManager.Instance.OnError += OnFirebaseError;
            Debug.Log("[DataManager] CleanFirebaseManager 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("[DataManager] CleanFirebaseManager가 아직 없음");
        }
        
        Debug.Log("[DataManager] 초기화 완료 - 연결 대기 중");
    }
    
System.Collections.IEnumerator WaitForUserDataManager()
    {
        Debug.Log("[DataManager] UserDataManager 대기 중...");
        
        float timeout = 5f;
        float elapsed = 0f;
        
        while (UserDataManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnDataChanged += OnLocalDataChanged;
            dataWrapper = new FirebaseUserDataWrapper(UserDataManager.Instance);
            Debug.Log("[DataManager] UserDataManager 연결 완료!");
        }
        else
        {
            Debug.LogError("[DataManager] UserDataManager 타임아웃!");
        }
        
        // CleanFirebaseManager 이벤트 구독도 여기서 재시도
        yield return StartCoroutine(WaitForCleanFirebaseManager());
    }
    
    System.Collections.IEnumerator WaitForCleanFirebaseManager()
    {
        Debug.Log("[DataManager] CleanFirebaseManager 대기 중...");
        
        float timeout = 5f;
        float elapsed = 0f;
        
        while (CleanFirebaseManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (CleanFirebaseManager.Instance != null)
        {
            // 이벤트 중복 구독 방지
            CleanFirebaseManager.Instance.OnFirebaseReady -= OnFirebaseReady;
            CleanFirebaseManager.Instance.OnUserSignedIn -= OnUserSignedIn;
            CleanFirebaseManager.Instance.OnError -= OnFirebaseError;
            
            // 새로 구독
            CleanFirebaseManager.Instance.OnFirebaseReady += OnFirebaseReady;
            CleanFirebaseManager.Instance.OnUserSignedIn += OnUserSignedIn;
            CleanFirebaseManager.Instance.OnError += OnFirebaseError;
            Debug.Log("[DataManager] CleanFirebaseManager 이벤트 구독 완료!");
            
            // 이미 로그인되어 있는지 확인
            if (CleanFirebaseManager.Instance.IsLoggedIn)
            {
                Debug.Log("[DataManager] 이미 로그인되어 있음 - 연결 상태 업데이트");
                isConnected = true;
            }
        }
        else
        {
            Debug.LogError("[DataManager] CleanFirebaseManager 타임아웃!");
        }
    }

    void Update()
    {
        // 자동 동기화
        if (autoSyncEnabled && isConnected && Time.time - lastSyncTime >= autoSyncInterval)
        {
            SyncUserData();
        }
    }

    #region Firebase 이벤트 처리

void OnFirebaseReady()
    {
        Debug.Log("[DataManager] Firebase 준비 완료 - 자동 로그인 시도");
        
        // Firebase가 준비되면 자동으로 익명 로그인 시도
        if (CleanFirebaseManager.Instance != null && CleanFirebaseManager.Instance.IsReady)
        {
            Debug.Log("[DataManager] 자동 익명 로그인 시작");
            CleanFirebaseManager.Instance.SignInAnonymously();
        }
    }

void OnUserSignedIn(bool success)
    {
        Debug.Log($"[DataManager] 📨 OnUserSignedIn 이벤트 수신: {success}");
        
        if (success && CleanFirebaseManager.Instance != null)
        {
            isConnected = true;
            Debug.Log("[DataManager] ✅ Firebase 연결됨 - 로그인 성공!");
            
            // 연결 상태 재확인
            Debug.Log($"[DataManager] 연결 상태 재확인 - IsConnected: {isConnected}, IsReady: {CleanFirebaseManager.Instance.IsReady}");
            
            // 로그인 시 데이터 로드
            LoadUserData();
        }
        else
        {
            isConnected = false;
            Debug.LogWarning($"[DataManager] ❌ Firebase 로그인 실패 또는 매니저 없음. Success: {success}, Manager: {(CleanFirebaseManager.Instance != null ? "존재" : "NULL")}");
        }
    }

    void OnFirebaseError(string error)
    {
        Debug.LogError($"[DataManager] Firebase 오류: {error}");
        OnSyncError?.Invoke(error);
    }

    void OnLocalDataChanged(string dataType)
    {
        // 로컬 데이터 변경 시 자동 저장 (연결된 경우)
        if (isConnected)
        {
            Debug.Log($"[DataManager] 로컬 데이터 변경 감지: {dataType}");
            SyncUserData();
        }
    }

    #endregion

    #region 데이터 동기화

    /// <summary>
    /// 사용자 데이터 동기화 (저장)
    /// </summary>
    public void SyncUserData()
    {
        if (!isConnected || dataWrapper == null || CleanFirebaseManager.Instance == null)
        {
            Debug.LogWarning("[DataManager] ⚠️ 동기화 불가 - 연결 안됨 또는 매니저 없음");
            return;
        }

        try
        {
            var userData = dataWrapper.GetCurrentUserData();
            string userId = CleanFirebaseManager.Instance.CurrentUserId;

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("[DataManager] ⚠️ 사용자 ID가 없음");
                return;
            }

            Debug.Log("[DataManager] 📤 데이터 동기화 중...");
            CleanFirebaseManager.Instance.SaveUserData(userId, userData);
            
            lastSyncTime = Time.time;
            OnSyncCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] ❌ 동기화 실패: {ex.Message}");
            OnSyncError?.Invoke($"동기화 실패: {ex.Message}");
            OnSyncCompleted?.Invoke(false);
        }
    }

    /// <summary>
    /// 사용자 데이터 로드
    /// </summary>
/// <summary>
    /// 사용자 데이터 로드
    /// </summary>
    public void LoadUserData()
    {
        // 더 상세한 조건 체크와 로그
        if (!isConnected)
        {
            Debug.LogWarning("[DataManager] ⚠️ 로드 불가 - Firebase 연결 안됨");
            return;
        }
        
        if (dataWrapper == null)
        {
            Debug.LogWarning("[DataManager] ⚠️ 로드 불가 - UserDataManager 래퍼 없음");
            return;
        }
        
        if (CleanFirebaseManager.Instance == null)
        {
            Debug.LogWarning("[DataManager] ⚠️ 로드 불가 - CleanFirebaseManager 없음");
            return;
        }
        
        if (UserDataManager.Instance == null)
        {
            Debug.LogWarning("[DataManager] ⚠️ 로드 불가 - UserDataManager 없음");
            return;
        }

        try
        {
            string userId = CleanFirebaseManager.Instance.CurrentUserId;
            
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("[DataManager] ⚠️ 사용자 ID가 없음 - 익명 로그인 전?");
                return;
            }

            Debug.Log($"[DataManager] 📥 데이터 로드 시작: {userId}");
            CleanFirebaseManager.Instance.LoadUserData(userId, OnDataLoaded);
            
            lastSyncTime = Time.time;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] ❌ 로드 실패: {ex.Message}");
            OnSyncError?.Invoke($"로드 실패: {ex.Message}");
        }
    }

    void OnDataLoaded(string jsonData)
    {
        if (dataWrapper == null) return;

        try
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.Log("[DataManager] 📝 클라우드 데이터 없음 - 새 사용자로 초기화");
                SyncUserData(); // 현재 로컬 데이터를 클라우드에 저장
                return;
            }

            // JSON 데이터를 UserData로 변환
            var cloudData = JsonUtility.FromJson<UserData>(jsonData);
            
            if (cloudData != null)
            {
                Debug.Log("[DataManager] ✅ 클라우드 데이터 로드 성공");
                
                // 로컬 데이터와 병합/업데이트
                MergeCloudData(cloudData);
            }
            else
            {
                Debug.LogWarning("[DataManager] ⚠️ 클라우드 데이터 파싱 실패");
                SyncUserData(); // 파싱 실패시 로컬 데이터로 덮어쓰기
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] ❌ 데이터 처리 실패: {ex.Message}");
            OnSyncError?.Invoke($"데이터 처리 실패: {ex.Message}");
        }
    }

    void MergeCloudData(UserData cloudData)
    {
        if (dataWrapper == null) return;

        var localData = dataWrapper.GetCurrentUserData();
        
        // 간단한 병합 로직: 더 높은 값 선택
        var mergedData = new UserData
        {
            playerInfo = new PlayerInfo
            {
                playerName = !string.IsNullOrEmpty(cloudData.playerInfo.playerName) ? 
                            cloudData.playerInfo.playerName : localData.playerInfo.playerName,
                level = Math.Max(cloudData.playerInfo.level, localData.playerInfo.level),
                currentStage = Math.Max(cloudData.playerInfo.currentStage, localData.playerInfo.currentStage),
                lastLoginTime = DateTime.UtcNow.ToBinary().ToString()
            },
            currencies = new Currencies
            {
                gameCoins = Math.Max(cloudData.currencies.gameCoins, localData.currencies.gameCoins),
                diamonds = Math.Max(cloudData.currencies.diamonds, localData.currencies.diamonds),
                energy = Math.Max(cloudData.currencies.energy, localData.currencies.energy),
                maxEnergy = Math.Max(cloudData.currencies.maxEnergy, localData.currencies.maxEnergy),
                lastEnergyTime = cloudData.currencies.lastEnergyTime
            },
            stageProgress = cloudData.stageProgress ?? localData.stageProgress,
            gameStats = MergeGameStats(cloudData.gameStats, localData.gameStats),
            settings = cloudData.settings ?? localData.settings
        };

        // 병합된 데이터를 로컬에 적용
        dataWrapper.LoadUserData(mergedData);
        
        Debug.Log("[DataManager] ✅ 클라우드 데이터 병합 완료");
    }

    GameStats MergeGameStats(GameStats cloudStats, GameStats localStats)
    {
        if (cloudStats == null && localStats == null) return new GameStats();
        if (cloudStats == null) return localStats;
        if (localStats == null) return cloudStats;

        return new GameStats
        {
            infiniteBestScore = Math.Max(cloudStats.infiniteBestScore, localStats.infiniteBestScore),
            infiniteBestTime = Math.Max(cloudStats.infiniteBestTime, localStats.infiniteBestTime),
            totalGamesPlayed = Math.Max(cloudStats.totalGamesPlayed, localStats.totalGamesPlayed),
            totalBlocksDestroyed = Math.Max(cloudStats.totalBlocksDestroyed, localStats.totalBlocksDestroyed),
            totalPlayTime = Math.Max(cloudStats.totalPlayTime, localStats.totalPlayTime),
            totalStagesCleared = Math.Max(cloudStats.totalStagesCleared, localStats.totalStagesCleared),
            totalScoreEarned = Math.Max(cloudStats.totalScoreEarned, localStats.totalScoreEarned),
            consecutiveWins = Math.Max(cloudStats.consecutiveWins, localStats.consecutiveWins),
            maxConsecutiveWins = Math.Max(cloudStats.maxConsecutiveWins, localStats.maxConsecutiveWins),
            firstPlayDate = string.IsNullOrEmpty(cloudStats.firstPlayDate) ? localStats.firstPlayDate : cloudStats.firstPlayDate,
            lastPlayDate = string.IsNullOrEmpty(localStats.lastPlayDate) ? cloudStats.lastPlayDate : localStats.lastPlayDate
        };
    }

    #endregion

    #region 리더보드

    /// <summary>
    /// 리더보드 업데이트
    /// </summary>
    public void UpdateLeaderboard(string leaderboardType, int score, string displayName = "")
    {
        if (!isConnected || CleanFirebaseManager.Instance == null)
        {
            Debug.LogWarning("[DataManager] ⚠️ 리더보드 업데이트 불가 - 연결 안됨");
            return;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = "Player";
        }

        Debug.Log($"[DataManager] 🏆 리더보드 업데이트: {leaderboardType} - {score}점");
        CleanFirebaseManager.Instance.UpdateLeaderboard(leaderboardType, score, displayName);
    }

    #endregion

    #region 공개 API

    
    
    /// <summary>
    /// Firebase가 초기화되었는지 확인 (로그인 상태와 무관)
    /// </summary>
    public bool IsFirebaseReady => CleanFirebaseManager.Instance != null && CleanFirebaseManager.Instance.IsReady;
    
    /// <summary>
    /// 부분적 연결 상태 (초기화되었지만 로그인 안됨)
    /// </summary>
    public bool IsPartiallyConnected => IsFirebaseReady && dataWrapper != null && UserDataManager.Instance != null;
public bool IsConnected => isConnected;
    
    
    
    /// <summary>
    /// 강제로 연결 상태를 다시 확인하고 업데이트
    /// </summary>
    [ContextMenu("Force Check Connection")]
    public void ForceCheckConnection()
    {
        Debug.Log("[DataManager] 🔍 강제 연결 상태 체크 시작");
        
        if (CleanFirebaseManager.Instance != null)
        {
            Debug.Log($"[DataManager] CleanFirebaseManager 상태: Ready={CleanFirebaseManager.Instance.IsReady}, LoggedIn={CleanFirebaseManager.Instance.IsLoggedIn}");
            
            if (CleanFirebaseManager.Instance.IsLoggedIn)
            {
                if (!isConnected)
                {
                    Debug.Log("[DataManager] ✅ 로그인되어 있지만 isConnected가 false였음 - 수정");
                    isConnected = true;
                }
            }
            else if (CleanFirebaseManager.Instance.IsReady)
            {
                Debug.Log("[DataManager] Firebase 준비되었지만 로그인 안됨 - 자동 로그인 시도");
                CleanFirebaseManager.Instance.SignInAnonymously();
            }
        }
        else
        {
            Debug.LogWarning("[DataManager] CleanFirebaseManager가 없음");
        }
        
        Debug.Log($"[DataManager] 현재 연결 상태: IsConnected={isConnected}");
    }
public void ForceSyncNow()
    {
        if (isConnected)
        {
            SyncUserData();
        }
        else
        {
            Debug.LogWarning("[DataManager] ⚠️ 강제 동기화 불가 - 연결 안됨");
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

        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnDataChanged -= OnLocalDataChanged;
        }
    }
}
