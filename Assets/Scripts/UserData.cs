// UserData.cs - Firebase 연동을 위해 확장된 사용자 데이터 구조
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UserData
{
    public PlayerInfo playerInfo;
    public Currencies currencies;
    public Dictionary<string, StageProgress> stageProgress;
    public GameStats gameStats;
    public GameSettings settings;
    public SyncMetadata syncMetadata;

    public UserData()
    {
        playerInfo = new PlayerInfo();
        currencies = new Currencies();
        stageProgress = new Dictionary<string, StageProgress>();
        gameStats = new GameStats();
        settings = new GameSettings();
        syncMetadata = new SyncMetadata();
        syncMetadata.createdTimestamp = GetCurrentUnixTimestamp();
        syncMetadata.lastModifiedTimestamp = GetCurrentUnixTimestamp();
    }

    /// <summary>
    /// Get current Unix timestamp in milliseconds
    /// </summary>
    public static long GetCurrentUnixTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Mark data as modified and update timestamp
    /// </summary>
    public void MarkAsModified(string changedField = "")
    {
        if (syncMetadata == null)
        {
            syncMetadata = new SyncMetadata();
        }

        syncMetadata.lastModifiedTimestamp = GetCurrentUnixTimestamp();
        syncMetadata.isPendingSync = true;

        // Track what changed
        if (!string.IsNullOrEmpty(changedField))
        {
            if (string.IsNullOrEmpty(syncMetadata.pendingChanges))
            {
                syncMetadata.pendingChanges = changedField;
            }
            else if (!syncMetadata.pendingChanges.Contains(changedField))
            {
                syncMetadata.pendingChanges += "," + changedField;
            }
        }
    }

    /// <summary>
    /// Mark data as successfully synced
    /// </summary>
    public void MarkAsSynced()
    {
        if (syncMetadata == null)
        {
            syncMetadata = new SyncMetadata();
        }

        syncMetadata.lastSyncTimestamp = GetCurrentUnixTimestamp();
        syncMetadata.isPendingSync = false;
        syncMetadata.pendingChanges = "";
        syncMetadata.syncCount++;
        syncMetadata.syncFailCount = 0;
        syncMetadata.lastSyncError = "";
    }

    /// <summary>
    /// Record sync failure
    /// </summary>
    public void RecordSyncFailure(string errorMessage)
    {
        if (syncMetadata == null)
        {
            syncMetadata = new SyncMetadata();
        }

        syncMetadata.syncFailCount++;
        syncMetadata.lastSyncError = errorMessage;
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string playerName = "Player";
    public int level = 1;
    public int currentStage = 1;
    public string lastLoginTime;
    public long accountCreatedTimestamp = 0;  // Account creation time
}

[System.Serializable]
public class Currencies
{
    public int gameCoins = 1000;
    public int diamonds = 50;
    public int energy = 5;
    public int maxEnergy = 5;
    public string lastEnergyTime;

    public int hammerCount = 3;
    public int tornadoCount = 2;
    public int brushCount = 2;

    public long lastCoinChange = 0;      // Last time coins were modified
    public long lastDiamondChange = 0;   // Last time diamonds were modified
    public long lastItemChange = 0;      // Last time items were modified
}

[System.Serializable]
public class StageProgress
{
    public int stageNumber = 0;  // 스테이지 번호 추가
    public int bestScore = 0;
    public int bestStars = 0;
    public bool completed = false;
    public long completedTime = 0; // 클리어 시간 (DateTime.Ticks)
}

/// <summary>
/// 게임 통계 데이터 구조
/// </summary>
[System.Serializable]
public class GameStats
{
    public long infiniteBestScore = 0;      // 무한모드 최고 점수
    public int infiniteBestTime = 0;        // 무한모드 최대 생존 시간 (초)
    public int totalGamesPlayed = 0;        // 총 게임 플레이 횟수
    public int totalBlocksDestroyed = 0;    // 총 파괴한 블록 수
    public long totalPlayTime = 0;          // 총 플레이 시간 (초)
    public int totalStagesCleared = 0;      // 총 클리어한 스테이지 수
    public long totalScoreEarned = 0;       // 총 획득 점수
    public int consecutiveWins = 0;         // 연속 승리 수
    public int maxConsecutiveWins = 0;      // 최대 연속 승리 수
    public string firstPlayDate = "";       // 첫 플레이 날짜
    public string lastPlayDate = "";        // 마지막 플레이 날짜
}

/// <summary>
/// 게임 설정 데이터 구조
/// </summary>
[System.Serializable]
public class GameSettings
{
    [Header("Audio Settings")]
    public bool soundEnabled = true;        // 사운드 활성화
    public bool musicEnabled = true;        // 음악 활성화
    public bool vibrationEnabled = true;    // 진동 활성화
    public float masterVolume = 1.0f;       // 마스터 볼륨 (0-1)
    public float musicVolume = 0.7f;        // 음악 볼륨 (0-1)
    public float sfxVolume = 1.0f;          // 효과음 볼륨 (0-1)

    [Header("Gameplay Settings")]
    public bool autoSaveEnabled = true;     // 자동 저장 활성화
    public bool showTutorial = true;        // 튜토리얼 표시
    public bool showHints = true;           // 힌트 표시
    public int preferredDifficulty = 1;     // 선호 난이도 (0-2)

    [Header("Graphics Settings")]
    public int graphicsQuality = 2;         // 그래픽 품질 (0-2: Low, Medium, High)
    public bool particleEffects = true;     // 파티클 효과
    public bool screenShake = true;         // 화면 흔들림
    public float animationSpeed = 1.0f;     // 애니메이션 속도

    [Header("UI Settings")]
    public bool colorBlindMode = false;     // 색맹 모드
    public float uiScale = 1.0f;           // UI 크기 (0.8-1.2)
    public string language = "ko";          // 언어 설정
    public bool showFPS = false;            // FPS 표시

    [Header("Notification Settings")]
    public bool energyNotification = true;  // 에너지 충전 알림
    public bool dailyRewardNotification = true; // 일일 보상 알림
    public bool eventNotification = true;   // 이벤트 알림
}

/// <summary>
/// 일일 보상 데이터 구조 (추후 확장용)
/// </summary>
[System.Serializable]
public class DailyReward
{
    public string lastClaimDate = "";       // 마지막 보상 수령 날짜
    public int consecutiveDays = 0;         // 연속 접속 일수
    public bool todayClaimedAlready = false; // 오늘 이미 수령했는지
}

/// <summary>
/// 업적 데이터 구조 (추후 확장용)
/// </summary>
[System.Serializable]
public class Achievement
{
    public string achievementId = "";       // 업적 ID
    public string achievementName = "";     // 업적 이름
    public string description = "";         // 업적 설명
    public bool isUnlocked = false;         // 달성 여부
    public int progress = 0;               // 진행도
    public int maxProgress = 100;          // 최대 진행도
    public string unlockedDate = "";       // 달성 날짜
    public int rewardCoins = 0;            // 보상 코인
    public int rewardDiamonds = 0;         // 보상 다이아몬드
}

/// <summary>
/// Firebase sync metadata - for conflict resolution and versioning
/// </summary>
[System.Serializable]
public class SyncMetadata
{
    // Timestamp tracking (Unix timestamp in milliseconds)
    public long lastModifiedTimestamp = 0;    // Last local modification time
    public long lastSyncTimestamp = 0;        // Last successful sync to server
    public long createdTimestamp = 0;         // Account creation time

    // Version control
    public int dataVersion = 1;               // Data schema version (for future migrations)
    public int syncCount = 0;                 // Total number of successful syncs

    // Device tracking
    public string deviceId = "";              // Unique device identifier
    public string lastSyncDeviceId = "";      // Device that performed last sync

    // Sync state
    public bool isPendingSync = false;        // Has unsaved changes
    public string pendingChanges = "";        // Comma-separated list of changed fields (e.g., "coins,diamonds,stage_progress")

    // Error tracking
    public int syncFailCount = 0;             // Consecutive sync failure count
    public string lastSyncError = "";         // Last sync error message
}