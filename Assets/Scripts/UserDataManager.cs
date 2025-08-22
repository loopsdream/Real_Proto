// UserDataManager.cs - Firebase 연동이 포함된 사용자 데이터 관리 시스템
using System;
using System.Collections.Generic;
using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance;

    [Header("Energy Settings")]
    public int maxEnergy = 5;
    public int energyRechargeTimeMinutes = 30; // 30분마다 1 에너지 충전

    [Header("Firebase Integration")]
    public bool firebaseIntegrationEnabled = true;

    [Header("Events")]
    public System.Action<int> OnGameCoinsChanged;
    public System.Action<int> OnDiamondsChanged;
    public System.Action<int> OnEnergyChanged;
    public System.Action<int> OnPlayerLevelChanged;
    
    // Firebase 연동을 위한 이벤트
    public event Action<string> OnDataChanged;

    private UserData currentUserData;
    private const string SAVE_KEY = "UserData";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Application.isPlaying 체크로 DontDestroyOnLoad 오류 방지
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            LoadUserData();
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
        // 게임 시작 시 에너지 자동 충전 확인
        UpdateEnergyFromTime();

        // 주기적으로 에너지 업데이트 (1분마다)
        InvokeRepeating(nameof(UpdateEnergyFromTime), 60f, 60f);
    }

    #region 데이터 로드/저장

    void LoadUserData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string jsonData = PlayerPrefs.GetString(SAVE_KEY);
            try
            {
                currentUserData = JsonUtility.FromJson<UserData>(jsonData);
                Debug.Log("User data loaded successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load user data: " + e.Message);
                CreateNewUserData();
            }
        }
        else
        {
            CreateNewUserData();
        }

        // 로드 후 이벤트 발생
        InvokeAllEvents();
    }

    void CreateNewUserData()
    {
        currentUserData = new UserData();
        currentUserData.playerInfo.lastLoginTime = DateTime.Now.ToBinary().ToString();
        currentUserData.currencies.lastEnergyTime = DateTime.Now.ToBinary().ToString();
        currentUserData.currencies.maxEnergy = maxEnergy;

        SaveUserData();
        Debug.Log("New user data created");
    }

    public void SaveUserData()
    {
        try
        {
            currentUserData.playerInfo.lastLoginTime = DateTime.Now.ToBinary().ToString();
            string jsonData = JsonUtility.ToJson(currentUserData, true);
            PlayerPrefs.SetString(SAVE_KEY, jsonData);
            PlayerPrefs.Save();
            Debug.Log("User data saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save user data: " + e.Message);
        }
    }

    #endregion

    #region 게임 코인 관리

    public int GetGameCoins()
    {
        return currentUserData.currencies.gameCoins;
    }
    
    public int GetCoins() => GetGameCoins(); // Firebase 연동용 별칭

    public bool SpendGameCoins(int amount)
    {
        if (currentUserData.currencies.gameCoins >= amount)
        {
            currentUserData.currencies.gameCoins -= amount;
            OnGameCoinsChanged?.Invoke(currentUserData.currencies.gameCoins);
            OnDataChanged?.Invoke("coins");
            SaveUserData();
            return true;
        }
        return false;
    }

    public void AddGameCoins(int amount)
    {
        currentUserData.currencies.gameCoins += amount;
        OnGameCoinsChanged?.Invoke(currentUserData.currencies.gameCoins);
        OnDataChanged?.Invoke("coins");
        SaveUserData();
        Debug.Log($"Added {amount} game coins. Total: {currentUserData.currencies.gameCoins}");
    }
    
    public void SetCoins(int amount)
    {
        currentUserData.currencies.gameCoins = amount;
        OnGameCoinsChanged?.Invoke(amount);
        OnDataChanged?.Invoke("coins");
        SaveUserData();
    }

    #endregion

    #region 다이아몬드 관리

    public int GetDiamonds()
    {
        return currentUserData.currencies.diamonds;
    }

    public bool SpendDiamonds(int amount)
    {
        if (currentUserData.currencies.diamonds >= amount)
        {
            currentUserData.currencies.diamonds -= amount;
            OnDiamondsChanged?.Invoke(currentUserData.currencies.diamonds);
            OnDataChanged?.Invoke("diamonds");
            SaveUserData();
            return true;
        }
        return false;
    }

    public void AddDiamonds(int amount)
    {
        currentUserData.currencies.diamonds += amount;
        OnDiamondsChanged?.Invoke(currentUserData.currencies.diamonds);
        OnDataChanged?.Invoke("diamonds");
        SaveUserData();
        Debug.Log($"Added {amount} diamonds. Total: {currentUserData.currencies.diamonds}");
    }
    
    public void SetDiamonds(int amount)
    {
        currentUserData.currencies.diamonds = amount;
        OnDiamondsChanged?.Invoke(amount);
        OnDataChanged?.Invoke("diamonds");
        SaveUserData();
    }

    #endregion

    #region 에너지 관리

    public int GetEnergy()
    {
        return currentUserData.currencies.energy;
    }

    public int GetMaxEnergy()
    {
        return currentUserData.currencies.maxEnergy;
    }
    
    public long GetLastEnergyTime()
    {
        try
        {
            return Convert.ToInt64(currentUserData.currencies.lastEnergyTime);
        }
        catch
        {
            return DateTime.UtcNow.ToBinary();
        }
    }

    public bool SpendEnergy(int amount = 1)
    {
        if (currentUserData.currencies.energy >= amount)
        {
            currentUserData.currencies.energy -= amount;
            OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
            OnDataChanged?.Invoke("energy");
            SaveUserData();
            return true;
        }
        return false;
    }

    public void AddEnergy(int amount)
    {
        currentUserData.currencies.energy = Mathf.Min(
            currentUserData.currencies.energy + amount,
            currentUserData.currencies.maxEnergy
        );
        OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
        OnDataChanged?.Invoke("energy");
        SaveUserData();
        Debug.Log($"Added {amount} energy. Current: {currentUserData.currencies.energy}");
    }
    
    public void SetEnergy(int amount)
    {
        currentUserData.currencies.energy = Mathf.Min(amount, maxEnergy);
        OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
        OnDataChanged?.Invoke("energy");
        SaveUserData();
    }
    
    public void SetLastEnergyTime(long binaryTime)
    {
        currentUserData.currencies.lastEnergyTime = binaryTime.ToString();
        OnDataChanged?.Invoke("energy_time");
        SaveUserData();
    }

    public void UpdateEnergyFromTime()
    {
        if (currentUserData.currencies.energy >= currentUserData.currencies.maxEnergy)
            return;

        try
        {
            DateTime lastEnergyTime = DateTime.FromBinary(Convert.ToInt64(currentUserData.currencies.lastEnergyTime));
            DateTime currentTime = DateTime.Now;

            TimeSpan timeDifference = currentTime - lastEnergyTime;
            int minutesPassed = (int)timeDifference.TotalMinutes;

            if (minutesPassed >= energyRechargeTimeMinutes)
            {
                int energyToAdd = minutesPassed / energyRechargeTimeMinutes;
                int newEnergy = Mathf.Min(
                    currentUserData.currencies.energy + energyToAdd,
                    currentUserData.currencies.maxEnergy
                );

                if (newEnergy > currentUserData.currencies.energy)
                {
                    currentUserData.currencies.energy = newEnergy;

                    // 마지막 에너지 시간 업데이트
                    DateTime newLastEnergyTime = lastEnergyTime.AddMinutes(energyToAdd * energyRechargeTimeMinutes);
                    currentUserData.currencies.lastEnergyTime = newLastEnergyTime.ToBinary().ToString();

                    OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
                    OnDataChanged?.Invoke("energy");
                    SaveUserData();

                    Debug.Log($"Energy recharged: +{energyToAdd}, Current: {currentUserData.currencies.energy}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error updating energy from time: " + e.Message);
            // 오류 발생 시 현재 시간으로 설정
            currentUserData.currencies.lastEnergyTime = DateTime.Now.ToBinary().ToString();
            SaveUserData();
        }
    }

    public TimeSpan GetTimeUntilNextEnergy()
    {
        if (currentUserData.currencies.energy >= currentUserData.currencies.maxEnergy)
            return TimeSpan.Zero;

        try
        {
            DateTime lastEnergyTime = DateTime.FromBinary(Convert.ToInt64(currentUserData.currencies.lastEnergyTime));
            DateTime nextEnergyTime = lastEnergyTime.AddMinutes(energyRechargeTimeMinutes);
            DateTime currentTime = DateTime.Now;

            if (nextEnergyTime > currentTime)
            {
                return nextEnergyTime - currentTime;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error calculating next energy time: " + e.Message);
        }

        return TimeSpan.Zero;
    }

    #endregion

    #region 플레이어 정보 관리

    public string GetPlayerName()
    {
        return currentUserData.playerInfo.playerName;
    }

    public void SetPlayerName(string name)
    {
        currentUserData.playerInfo.playerName = name;
        OnDataChanged?.Invoke("player_info");
        SaveUserData();
    }

    public int GetPlayerLevel()
    {
        return currentUserData.playerInfo.level;
    }

    public void SetPlayerLevel(int level)
    {
        currentUserData.playerInfo.level = level;
        OnPlayerLevelChanged?.Invoke(level);
        OnDataChanged?.Invoke("player_info");
        SaveUserData();
    }

    public int GetCurrentStage()
    {
        return currentUserData.playerInfo.currentStage;
    }

    public void SetCurrentStage(int stage)
    {
        currentUserData.playerInfo.currentStage = Mathf.Max(currentUserData.playerInfo.currentStage, stage);
        OnDataChanged?.Invoke("stage_progress");
        SaveUserData();
    }

    #endregion

    #region Firebase 연동 추가 메서드들

    public int GetHighestStage()
    {
        int highest = 1;
        foreach (var progress in currentUserData.stageProgress.Values)
        {
            if (progress.completed && progress.stageNumber > highest)
            {
                highest = progress.stageNumber;
            }
        }
        return highest;
    }

    public void SetHighestStage(int stage)
    {
        // 새로운 최고 스테이지를 스테이지 진행도에 반영
        string stageKey = "stage" + stage;
        if (!currentUserData.stageProgress.ContainsKey(stageKey))
        {
            currentUserData.stageProgress[stageKey] = new StageProgress();
        }
        currentUserData.stageProgress[stageKey].completed = true;
        currentUserData.stageProgress[stageKey].stageNumber = stage;

        OnDataChanged?.Invoke("stage_progress");
        SaveUserData();
    }

    public long GetTotalScore()
    {
        long total = 0;
        foreach (var progress in currentUserData.stageProgress.Values)
        {
            total += progress.bestScore;
        }
        return total;
    }

    public long GetInfiniteBestScore()
    {
        return currentUserData.gameStats?.infiniteBestScore ?? 0;
    }

    public int GetInfiniteBestTime()
    {
        return currentUserData.gameStats?.infiniteBestTime ?? 0;
    }

    public void SetInfiniteBestScore(long score)
    {
        if (currentUserData.gameStats == null)
        {
            currentUserData.gameStats = new GameStats();
        }

        if (score > currentUserData.gameStats.infiniteBestScore)
        {
            currentUserData.gameStats.infiniteBestScore = score;
            OnDataChanged?.Invoke("infinite_best");
            SaveUserData();
        }
    }

    public void SetInfiniteBestTime(int time)
    {
        if (currentUserData.gameStats == null)
        {
            currentUserData.gameStats = new GameStats();
        }

        if (time > currentUserData.gameStats.infiniteBestTime)
        {
            currentUserData.gameStats.infiniteBestTime = time;
            OnDataChanged?.Invoke("infinite_best");
            SaveUserData();
        }
    }

    // 설정 관련 메서드들
    public bool IsSoundEnabled() => currentUserData.settings?.soundEnabled ?? true;
    public bool IsMusicEnabled() => currentUserData.settings?.musicEnabled ?? true;
    public bool IsVibrationEnabled() => currentUserData.settings?.vibrationEnabled ?? true;
    public float GetMasterVolume() => currentUserData.settings?.masterVolume ?? 1.0f;
    public float GetMusicVolume() => currentUserData.settings?.musicVolume ?? 0.7f;
    public float GetSFXVolume() => currentUserData.settings?.sfxVolume ?? 1.0f;

    public void SetSoundEnabled(bool enabled)
    {
        if (currentUserData.settings == null) currentUserData.settings = new GameSettings();
        currentUserData.settings.soundEnabled = enabled;
        OnDataChanged?.Invoke("settings");
        SaveUserData();
    }

    public void SetMusicEnabled(bool enabled)
    {
        if (currentUserData.settings == null) currentUserData.settings = new GameSettings();
        currentUserData.settings.musicEnabled = enabled;
        OnDataChanged?.Invoke("settings");
        SaveUserData();
    }

    public void SetVibrationEnabled(bool enabled)
    {
        if (currentUserData.settings == null) currentUserData.settings = new GameSettings();
        currentUserData.settings.vibrationEnabled = enabled;
        OnDataChanged?.Invoke("settings");
        SaveUserData();
    }

    public void SetMasterVolume(float volume)
    {
        if (currentUserData.settings == null) currentUserData.settings = new GameSettings();
        currentUserData.settings.masterVolume = Mathf.Clamp01(volume);
        OnDataChanged?.Invoke("settings");
        SaveUserData();
    }

    public void SetMusicVolume(float volume)
    {
        if (currentUserData.settings == null) currentUserData.settings = new GameSettings();
        currentUserData.settings.musicVolume = Mathf.Clamp01(volume);
        OnDataChanged?.Invoke("settings");
        SaveUserData();
    }

    public void SetSFXVolume(float volume)
    {
        if (currentUserData.settings == null) currentUserData.settings = new GameSettings();
        currentUserData.settings.sfxVolume = Mathf.Clamp01(volume);
        OnDataChanged?.Invoke("settings");
        SaveUserData();
    }

    #endregion

    #region 스테이지 진행도 관리

    public StageProgress GetStageProgress(int stageNumber)
    {
        string stageKey = "stage" + stageNumber;
        if (currentUserData.stageProgress.ContainsKey(stageKey))
        {
            return currentUserData.stageProgress[stageKey];
        }

        return new StageProgress();
    }

    public void UpdateStageProgress(int stageNumber, int score, bool completed)
    {
        string stageKey = "stage" + stageNumber;

        if (!currentUserData.stageProgress.ContainsKey(stageKey))
        {
            currentUserData.stageProgress[stageKey] = new StageProgress();
        }

        StageProgress progress = currentUserData.stageProgress[stageKey];
        progress.stageNumber = stageNumber;
        progress.bestScore = Mathf.Max(progress.bestScore, score);

        if (completed && !progress.completed)
        {
            progress.completed = true;
            progress.completedTime = DateTime.UtcNow.Ticks;

            // 다음 스테이지 클리어 시 다음 스테이지 해금
            SetCurrentStage(stageNumber + 1);

            // 레벨업 조건 (예: 5스테이지마다 레벨업)
            if (stageNumber % 5 == 0)
            {
                SetPlayerLevel(GetPlayerLevel() + 1);
            }
        }

        currentUserData.stageProgress[stageKey] = progress;
        OnDataChanged?.Invoke("stage_progress");
        SaveUserData();

        Debug.Log($"Stage {stageNumber} progress updated: Score={score}, Completed={completed}");
    }

    #endregion

    #region 무한모드 기록 업데이트

    public void UpdateInfiniteModeRecord(long score, int timeSeconds)
    {
        if (currentUserData.gameStats == null)
        {
            currentUserData.gameStats = new GameStats();
        }

        bool newRecord = false;

        if (score > currentUserData.gameStats.infiniteBestScore)
        {
            currentUserData.gameStats.infiniteBestScore = score;
            newRecord = true;
        }

        if (timeSeconds > currentUserData.gameStats.infiniteBestTime)
        {
            currentUserData.gameStats.infiniteBestTime = timeSeconds;
            newRecord = true;
        }

        if (newRecord)
        {
            OnDataChanged?.Invoke("infinite_best");
            SaveUserData();

            // Firebase에 리더보드 업로드
            if (firebaseIntegrationEnabled && FirebaseDataManager.Instance != null)
            {
                _ = FirebaseDataManager.Instance.UploadLeaderboardScore((int)score, "infinite");
            }
        }
    }

    #endregion

    #region 스테이지 완료 보상

    public void GiveStageReward(int stageNumber, int score)
    {
        // 기본 점수 보상 (점수 기반)
        int coinReward = score / 10;
        AddGameCoins(coinReward);

        // 첫 클리어 보너스
        if (!GetStageProgress(stageNumber).completed)
        {
            int bonusCoins = 50; // 첫 클리어 보너스
            AddGameCoins(bonusCoins);

            // 특정 스테이지마다 다이아몬드 보상
            if (stageNumber % 10 == 0) // 10, 20, 30... 스테이지
            {
                AddDiamonds(1);
            }
        }
    }

    #endregion

    #region 게임 구매 (IAP 연동 준비)

    public void PurchaseDiamonds(int amount)
    {
        // 실제 IAP 연동 시 호출될 메서드
        AddDiamonds(amount);
        Debug.Log($"Purchased {amount} diamonds via IAP");
    }

    public bool PurchaseEnergyWithDiamonds(int diamondCost, int energyAmount)
    {
        if (SpendDiamonds(diamondCost))
        {
            AddEnergy(energyAmount);
            return true;
        }
        return false;
    }

    #endregion

    #region Firebase 동기화 메서드들

    /// <summary>
    /// Firebase와 강제 동기화
    /// </summary>
    public void SyncWithFirebase()
    {
        if (firebaseIntegrationEnabled && FirebaseDataManager.Instance != null)
        {
            FirebaseDataManager.Instance.ForceSyncNow();
        }
    }

    /// <summary>
    /// Firebase 연결 상태 확인
    /// </summary>
    public bool IsConnectedToFirebase()
    {
        return firebaseIntegrationEnabled && 
               FirebaseDataManager.Instance != null && 
               FirebaseDataManager.Instance.IsConnected;
    }

    #endregion

    #region 유틸리티

    void InvokeAllEvents()
    {
        OnGameCoinsChanged?.Invoke(currentUserData.currencies.gameCoins);
        OnDiamondsChanged?.Invoke(currentUserData.currencies.diamonds);
        OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
        OnPlayerLevelChanged?.Invoke(currentUserData.playerInfo.level);
    }

    public void ResetUserData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        CreateNewUserData();
        InvokeAllEvents();
        Debug.Log("User data reset");
    }

    #endregion
}