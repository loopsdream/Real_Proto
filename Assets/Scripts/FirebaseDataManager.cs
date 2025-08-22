// FirebaseDataManager.cs - 실제 Firebase SDK 연동 + 시뮬레이션 호환 버전
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseDataManager : MonoBehaviour
{
    public static FirebaseDataManager Instance { get; private set; }

    [Header("Sync Settings")]
    public bool autoSyncEnabled = true;
    public float autoSyncInterval = 30f; // 30초마다 자동 동기화
    public bool syncOnGameEvent = true; // 게임 이벤트시 즉시 동기화

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private DatabaseReference userDataRef;
    private string currentUserId;
    private bool isConnected = false;
    private float lastSyncTime;
    private bool useRealFirebase = false;

    // 이벤트
    public event Action<UserData> OnUserDataLoaded;
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
        // RealFirebaseManager가 있으면 실제 Firebase 사용
        if (RealFirebaseManager.Instance != null)
        {
            useRealFirebase = true;
            RealFirebaseManager.Instance.OnUserSignedIn += OnUserSignedIn;
            RealFirebaseManager.Instance.OnUserSignedOut += OnUserSignedOut;
        }
        // SafeFirebaseManager가 있으면 시뮬레이션 사용
        else if (SafeFirebaseManager.Instance != null)
        {
            useRealFirebase = false;
            SafeFirebaseManager.Instance.OnUserSignedIn += OnUserSignedInSimulation;
            SafeFirebaseManager.Instance.OnUserSignedOut += OnUserSignedOut;
        }

        // UserDataManager 이벤트 구독
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnDataChanged += OnLocalDataChanged;
        }
    }

    void Update()
    {
        // 자동 동기화
        if (autoSyncEnabled && isConnected && 
            Time.time - lastSyncTime >= autoSyncInterval)
        {
            if (useRealFirebase)
            {
                _ = SyncUserDataReal();
            }
            else
            {
                _ = SyncUserDataSimulation();
            }
        }
    }

    #region Firebase 연결 관리

    void OnUserSignedIn(bool success)
    {
        if (!success || RealFirebaseManager.Instance == null) return;

        currentUserId = RealFirebaseManager.Instance.CurrentUserId;
        
        if (RealFirebaseManager.Instance.HasDatabase())
        {
            userDataRef = RealFirebaseManager.Instance.GetDatabaseReference($"users/{currentUserId}");
            isConnected = true;

            LogDebug($"🔗 실제 Firebase 데이터 매니저 연결: {currentUserId}");

            // 사용자 데이터 로드
            _ = LoadUserDataReal();
        }
        else
        {
            LogDebug("⚠️ Database 없이 Auth만 연결됨");
        }
    }

    void OnUserSignedInSimulation(bool success)
    {
        if (!success) return;

        currentUserId = "simulation_user_" + UnityEngine.Random.Range(1000, 9999);
        isConnected = true;

        LogDebug($"🔗 시뮬레이션 Firebase 데이터 매니저 연결: {currentUserId}");

        // 시뮬레이션 데이터 로드
        _ = LoadUserDataSimulation();
    }

    void OnUserSignedOut()
    {
        currentUserId = null;
        userDataRef = null;
        isConnected = false;

        LogDebug("🔌 Firebase 데이터 매니저 연결 해제");
    }

    #endregion

    #region 실제 Firebase 데이터 처리

    /// <summary>
    /// 실제 Firebase에서 사용자 데이터 로드
    /// </summary>
    public async Task<bool> LoadUserDataReal()
    {
        if (!isConnected || userDataRef == null)
        {
            LogDebug("❌ Firebase 연결되지 않음 - 로컬 데이터 사용");
            return false;
        }

        try
        {
            LogDebug("📥 실제 클라우드에서 사용자 데이터 로드 중...");

            var snapshot = await userDataRef.GetValueAsync();
            
            if (snapshot.Exists && !string.IsNullOrEmpty(snapshot.GetRawJsonValue()))
            {
                string json = snapshot.GetRawJsonValue();
                
                // JSON 데이터 파싱 및 병합
                ParseAndMergeCloudData(json);
                
                OnUserDataLoaded?.Invoke(null);
                LogDebug("✅ 실제 사용자 데이터 로드 완료");
                return true;
            }
            else
            {
                LogDebug("📤 클라우드 데이터 없음 - 로컬 데이터를 클라우드에 업로드");
                await SaveUserDataReal();
                return true;
            }
        }
        catch (Exception ex)
        {
            LogError($"❌ 실제 사용자 데이터 로드 실패: {ex.Message}");
            OnSyncError?.Invoke($"데이터 로드 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 실제 Firebase에 사용자 데이터 저장
    /// </summary>
    public async Task<bool> SaveUserDataReal()
    {
        if (!isConnected || userDataRef == null)
        {
            LogDebug("❌ Firebase 연결되지 않음 - 로컬에만 저장");
            return false;
        }

        try
        {
            // 로컬 데이터를 Firebase 형식으로 변환
            var currentData = GetCurrentUserDataForFirebase();

            string json = JsonUtility.ToJson(currentData, true);
            
            LogDebug("📤 실제 클라우드에 사용자 데이터 저장 중...");
            await userDataRef.SetRawJsonValueAsync(json);
            
            lastSyncTime = Time.time;
            OnSyncCompleted?.Invoke(true);
            LogDebug("✅ 실제 사용자 데이터 저장 완료");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"❌ 실제 사용자 데이터 저장 실패: {ex.Message}");
            OnSyncError?.Invoke($"데이터 저장 실패: {ex.Message}");
            OnSyncCompleted?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// 실제 Firebase 양방향 데이터 동기화
    /// </summary>
    public async Task<bool> SyncUserDataReal()
    {
        if (!isConnected) return false;

        LogDebug("🔄 실제 데이터 동기화 시작...");
        
        bool loadSuccess = await LoadUserDataReal();
        if (loadSuccess)
        {
            bool saveSuccess = await SaveUserDataReal();
            return saveSuccess;
        }
        
        return false;
    }

    #endregion

    #region 시뮬레이션 데이터 처리

    /// <summary>
    /// 시뮬레이션 사용자 데이터 로드
    /// </summary>
    public async Task<bool> LoadUserDataSimulation()
    {
        LogDebug("📥 시뮬레이션 데이터 로드 중...");
        
        // 시뮬레이션 지연
        await Task.Delay(500);
        
        // PlayerPrefs에서 시뮬레이션 데이터 로드
        string savedData = PlayerPrefs.GetString($"SimulationUserData_{currentUserId}", "");
        
        if (!string.IsNullOrEmpty(savedData))
        {
            ParseAndMergeCloudData(savedData);
            OnUserDataLoaded?.Invoke(null);
            LogDebug("✅ 시뮬레이션 데이터 로드 완료");
        }
        else
        {
            LogDebug("📤 시뮬레이션 데이터 없음 - 로컬 데이터를 저장");
            await SaveUserDataSimulation();
        }
        
        return true;
    }

    /// <summary>
    /// 시뮬레이션 사용자 데이터 저장
    /// </summary>
    public async Task<bool> SaveUserDataSimulation()
    {
        LogDebug("📤 시뮬레이션 데이터 저장 중...");
        
        // 시뮬레이션 지연
        await Task.Delay(300);
        
        // 로컬 데이터를 PlayerPrefs에 저장
        var currentData = GetCurrentUserDataForFirebase();
        string json = JsonUtility.ToJson(currentData, true);
        
        PlayerPrefs.SetString($"SimulationUserData_{currentUserId}", json);
        PlayerPrefs.Save();
        
        lastSyncTime = Time.time;
        OnSyncCompleted?.Invoke(true);
        LogDebug("✅ 시뮬레이션 데이터 저장 완료");
        return true;
    }

    /// <summary>
    /// 시뮬레이션 양방향 데이터 동기화
    /// </summary>
    public async Task<bool> SyncUserDataSimulation()
    {
        LogDebug("🔄 시뮬레이션 데이터 동기화 시작...");
        
        bool loadSuccess = await LoadUserDataSimulation();
        if (loadSuccess)
        {
            bool saveSuccess = await SaveUserDataSimulation();
            return saveSuccess;
        }
        
        return false;
    }

    #endregion

    #region 데이터 변환 및 병합

    void ParseAndMergeCloudData(string json)
    {
        try
        {
            var cloudData = JsonUtility.FromJson<FirebaseUserData>(json);
            
            if (cloudData != null && UserDataManager.Instance != null)
            {
                var manager = UserDataManager.Instance;
                
                // 재화 (더 많은 값 사용)
                int currentCoins = manager.GetGameCoins();
                int currentDiamonds = manager.GetDiamonds();
                int currentEnergy = manager.GetEnergy();
                
                if (cloudData.coins > currentCoins)
                {
                    manager.AddGameCoins(cloudData.coins - currentCoins);
                }
                
                if (cloudData.diamonds > currentDiamonds)
                {
                    manager.AddDiamonds(cloudData.diamonds - currentDiamonds);
                }
                
                if (cloudData.energy > currentEnergy)
                {
                    manager.AddEnergy(cloudData.energy - currentEnergy);
                }
                
                // 진행도 (더 높은 값 사용)
                if (cloudData.currentStage > manager.GetCurrentStage())
                {
                    manager.SetCurrentStage(cloudData.currentStage);
                }
                
                LogDebug("🔄 클라우드 데이터 병합 완료");
            }
        }
        catch (Exception ex)
        {
            LogError($"❌ 클라우드 데이터 파싱 실패: {ex.Message}");
        }
    }

    FirebaseUserData GetCurrentUserDataForFirebase()
    {
        if (UserDataManager.Instance == null)
        {
            return new FirebaseUserData(); // 기본값 반환
        }

        var manager = UserDataManager.Instance;
        string userEmail = "";
        string displayName = "Player";
        
        if (useRealFirebase && RealFirebaseManager.Instance != null)
        {
            userEmail = RealFirebaseManager.Instance.CurrentUserEmail;
            displayName = RealFirebaseManager.Instance.CurrentUser?.DisplayName ?? "Player";
        }
        
        return new FirebaseUserData
        {
            userId = currentUserId,
            email = userEmail,
            displayName = displayName,
            lastLoginAt = DateTime.UtcNow.ToBinary(),
            
            // 게임 재화
            coins = manager.GetGameCoins(),
            diamonds = manager.GetDiamonds(),
            energy = manager.GetEnergy(),
            lastEnergyTime = DateTime.UtcNow.ToBinary(),
            
            // 진행도
            currentStage = manager.GetCurrentStage(),
            totalScore = GetTotalScore(manager)
        };
    }

    long GetTotalScore(UserDataManager manager)
    {
        return manager.GetCurrentStage() * 1000; // 예시
    }

    #endregion

    #region 실시간 동기화

    /// <summary>
    /// 로컬 데이터 변경 시 호출
    /// </summary>
    void OnLocalDataChanged(string dataType)
    {
        if (!syncOnGameEvent || !isConnected) return;

        LogDebug($"📝 로컬 데이터 변경 감지: {dataType}");
        
        // 중요한 데이터는 즉시 동기화
        if (IsImportantData(dataType))
        {
            if (useRealFirebase)
            {
                _ = SaveUserDataReal();
            }
            else
            {
                _ = SaveUserDataSimulation();
            }
        }
    }

    bool IsImportantData(string dataType)
    {
        return dataType switch
        {
            "coins" => true,
            "diamonds" => true,
            "stage_progress" => true,
            "infinite_best" => true,
            _ => false
        };
    }

    #endregion

    #region 리더보드 (실제 Firebase만 지원)

    /// <summary>
    /// 리더보드에 점수 업로드
    /// </summary>
    public async Task<bool> UploadLeaderboardScore(int score, string mode = "infinite")
    {
        if (!isConnected || !useRealFirebase || RealFirebaseManager.Instance == null) 
        {
            LogDebug("⚠️ 리더보드는 실제 Firebase 연결 시만 지원");
            return false;
        }

        try
        {
            var leaderboardRef = RealFirebaseManager.Instance.GetDatabaseReference($"leaderboards/{mode}");
            
            var scoreData = new LeaderboardData
            {
                userId = currentUserId,
                displayName = RealFirebaseManager.Instance.CurrentUser?.DisplayName ?? "Anonymous",
                score = score,
                timestamp = DateTime.UtcNow.Ticks
            };

            await leaderboardRef.Child(currentUserId).SetRawJsonValueAsync(JsonUtility.ToJson(scoreData));
            LogDebug($"🏆 리더보드 점수 업로드: {score}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"❌ 리더보드 업로드 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 리더보드 데이터 가져오기
    /// </summary>
    public async Task<List<LeaderboardEntry>> GetLeaderboard(string mode = "infinite", int limit = 100)
    {
        if (!isConnected || !useRealFirebase || RealFirebaseManager.Instance == null) 
        {
            LogDebug("⚠️ 리더보드는 실제 Firebase 연결 시만 지원");
            return new List<LeaderboardEntry>();
        }

        try
        {
            var leaderboardRef = RealFirebaseManager.Instance.GetDatabaseReference($"leaderboards/{mode}");
            var query = leaderboardRef.OrderByChild("score").LimitToLast(limit);
            
            var snapshot = await query.GetValueAsync();
            var entries = new List<LeaderboardEntry>();

            foreach (var child in snapshot.Children)
            {
                if (!string.IsNullOrEmpty(child.GetRawJsonValue()))
                {
                    var data = JsonUtility.FromJson<LeaderboardData>(child.GetRawJsonValue());
                    entries.Add(new LeaderboardEntry
                    {
                        rank = 0, // 나중에 계산
                        userId = data.userId,
                        displayName = data.displayName,
                        score = data.score,
                        timestamp = data.timestamp
                    });
                }
            }

            // 점수순 정렬 및 순위 설정
            entries.Sort((a, b) => b.score.CompareTo(a.score));
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].rank = i + 1;
            }

            LogDebug($"🏆 리더보드 로드 완료: {entries.Count}개 항목");
            return entries;
        }
        catch (Exception ex)
        {
            LogError($"❌ 리더보드 로드 실패: {ex.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    #endregion

    #region 유틸리티

    void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[FirebaseDataManager] {message}");
        }
    }

    void LogError(string message)
    {
        Debug.LogError($"[FirebaseDataManager] {message}");
    }

    /// <summary>
    /// 강제 동기화 (공개 메서드)
    /// </summary>
    public void ForceSyncNow()
    {
        if (isConnected)
        {
            if (useRealFirebase)
            {
                _ = SyncUserDataReal();
            }
            else
            {
                _ = SyncUserDataSimulation();
            }
        }
    }

    /// <summary>
    /// 연결 상태 확인
    /// </summary>
    public bool IsConnected => isConnected;

    #endregion

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (RealFirebaseManager.Instance != null)
        {
            RealFirebaseManager.Instance.OnUserSignedIn -= OnUserSignedIn;
            RealFirebaseManager.Instance.OnUserSignedOut -= OnUserSignedOut;
        }

        if (SafeFirebaseManager.Instance != null)
        {
            SafeFirebaseManager.Instance.OnUserSignedIn -= OnUserSignedInSimulation;
            SafeFirebaseManager.Instance.OnUserSignedOut -= OnUserSignedOut;
        }

        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnDataChanged -= OnLocalDataChanged;
        }
    }
}

/// <summary>
/// Firebase용 간소화된 사용자 데이터 구조
/// </summary>
[System.Serializable]
public class FirebaseUserData
{
    public string userId;
    public string email;
    public string displayName;
    public long lastLoginAt;
    
    // 게임 재화
    public int coins;
    public int diamonds;
    public int energy;
    public long lastEnergyTime;
    
    // 진행도
    public int currentStage;
    public long totalScore;
}

/// <summary>
/// 리더보드 데이터 구조 (Firebase용)
/// </summary>
[System.Serializable]
public class LeaderboardData
{
    public string userId;
    public string displayName;
    public int score;
    public long timestamp;
}

/// <summary>
/// 리더보드 엔트리 (클라이언트용)
/// </summary>
[System.Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string userId;
    public string displayName;
    public int score;
    public long timestamp;
    
    public DateTime GetDateTime()
    {
        return new DateTime(timestamp);
    }
}