// UserData.cs - Firebase 연동을 위해 확장된 사용자 데이터 구조
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UserData
{
    public PlayerInfo playerInfo;
    public Currencies currencies;
    public Dictionary<string, StageProgress> stageProgress;
    public GameStats gameStats;      // 새로 추가
    public GameSettings settings;    // 새로 추가

    public UserData()
    {
        playerInfo = new PlayerInfo();
        currencies = new Currencies();
        stageProgress = new Dictionary<string, StageProgress>();
        gameStats = new GameStats();       // 기본값 초기화
        settings = new GameSettings();     // 기본값 초기화
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string playerName = "Player";
    public int level = 1;
    public int currentStage = 1;
    public string lastLoginTime;
}

[System.Serializable]
public class Currencies
{
    public int gameCoins = 1000; // 시작 시 기본 코인 (Firebase 연동 시 1000으로 증가)
    public int diamonds = 50;    // 시작 시 기본 다이아몬드 (Firebase 연동 시 50으로 증가)
    public int energy = 5;       // 시작 시 최대 에너지
    public int maxEnergy = 5;
    public string lastEnergyTime;

    // 아이템 보유 개수 추가
    [Header("Items")]
    public int hammerCount = 3;   // 시작 시 망치 3개
    public int tornadoCount = 2;  // 시작 시 회오리 2개
    public int brushCount = 2;    // 시작 시 붓 2개
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