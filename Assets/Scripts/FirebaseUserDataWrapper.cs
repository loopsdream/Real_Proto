// FirebaseUserDataWrapper.cs - UserDataManager와 Firebase 데이터 포맷 간 변환을 담당하는 래퍼 클래스
using System;
using UnityEngine;

/// <summary>
/// UserDataManager와 Firebase 사이의 데이터 변환을 처리하는 래퍼 클래스
/// </summary>
public class FirebaseUserDataWrapper
{
    private UserDataManager userDataManager;

    public FirebaseUserDataWrapper(UserDataManager manager)
    {
        userDataManager = manager;
    }

    /// <summary>
    /// 현재 로컬 UserData를 가져옴
    /// </summary>
    public UserData GetCurrentUserData()
    {
        if (userDataManager == null) return new UserData();

        return new UserData
        {
            playerInfo = new PlayerInfo
            {
                playerName = userDataManager.GetPlayerName(),
                level = userDataManager.GetPlayerLevel(),
                currentStage = userDataManager.GetCurrentStage(),
                lastLoginTime = DateTime.UtcNow.ToBinary().ToString()
            },
            currencies = new Currencies
            {
                gameCoins = userDataManager.GetGameCoins(),
                diamonds = userDataManager.GetDiamonds(),
                energy = userDataManager.GetEnergy(),
                maxEnergy = userDataManager.GetMaxEnergy(),
                lastEnergyTime = userDataManager.GetLastEnergyTime().ToString()
            },
            stageProgress = GetAllStageProgress(),
            gameStats = GetCurrentGameStats(),
            settings = GetCurrentSettings()
        };
    }

    /// <summary>
    /// Firebase에서 로드한 UserData를 로컬에 적용
    /// </summary>
    public void LoadUserData(UserData cloudData)
    {
        if (userDataManager == null || cloudData == null) return;

        try
        {
            // 플레이어 정보 업데이트
            if (!string.IsNullOrEmpty(cloudData.playerInfo.playerName))
            {
                userDataManager.SetPlayerName(cloudData.playerInfo.playerName);
            }
            userDataManager.SetPlayerLevel(cloudData.playerInfo.level);
            userDataManager.SetCurrentStage(cloudData.playerInfo.currentStage);

            // 재화 정보 업데이트
            userDataManager.SetCoins(cloudData.currencies.gameCoins);
            userDataManager.SetDiamonds(cloudData.currencies.diamonds);
            userDataManager.SetEnergy(cloudData.currencies.energy);
            
            if (long.TryParse(cloudData.currencies.lastEnergyTime, out long energyTime))
            {
                userDataManager.SetLastEnergyTime(energyTime);
            }

            // 게임 통계 업데이트
            if (cloudData.gameStats != null)
            {
                userDataManager.SetInfiniteBestScore(cloudData.gameStats.infiniteBestScore);
                userDataManager.SetInfiniteBestTime(cloudData.gameStats.infiniteBestTime);
            }

            // 설정 업데이트
            if (cloudData.settings != null)
            {
                ApplySettings(cloudData.settings);
            }

            // 스테이지 진행도는 개별적으로 병합
            if (cloudData.stageProgress != null)
            {
                ApplyStageProgress(cloudData.stageProgress);
            }

            Debug.Log("[FirebaseWrapper] ✅ 클라우드 데이터 로컬 적용 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseWrapper] ❌ 데이터 적용 실패: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private System.Collections.Generic.Dictionary<string, StageProgress> GetAllStageProgress()
    {
        var stageProgress = new System.Collections.Generic.Dictionary<string, StageProgress>();

        // 현재 스테이지까지의 진행도를 가져옴
        int currentStage = userDataManager.GetCurrentStage();
        
        for (int i = 1; i <= currentStage; i++)
        {
            var progress = userDataManager.GetStageProgress(i);
            if (progress.stageNumber > 0) // 유효한 데이터가 있는 경우만
            {
                stageProgress[$"stage{i}"] = progress;
            }
        }

        return stageProgress;
    }

    private GameStats GetCurrentGameStats()
    {
        return new GameStats
        {
            infiniteBestScore = userDataManager.GetInfiniteBestScore(),
            infiniteBestTime = userDataManager.GetInfiniteBestTime(),
            totalGamesPlayed = 0, // 아직 추적되지 않는 통계들
            totalBlocksDestroyed = 0,
            totalPlayTime = 0,
            totalStagesCleared = userDataManager.GetHighestStage(),
            totalScoreEarned = (int)userDataManager.GetTotalScore(),
            consecutiveWins = 0,
            maxConsecutiveWins = 0,
            firstPlayDate = DateTime.UtcNow.ToBinary().ToString(),
            lastPlayDate = DateTime.UtcNow.ToBinary().ToString()
        };
    }

    private GameSettings GetCurrentSettings()
    {
        return new GameSettings
        {
            soundEnabled = userDataManager.IsSoundEnabled(),
            musicEnabled = userDataManager.IsMusicEnabled(),
            vibrationEnabled = userDataManager.IsVibrationEnabled(),
            masterVolume = userDataManager.GetMasterVolume(),
            musicVolume = userDataManager.GetMusicVolume(),
            sfxVolume = userDataManager.GetSFXVolume()
        };
    }

    private void ApplySettings(GameSettings settings)
    {
        userDataManager.SetSoundEnabled(settings.soundEnabled);
        userDataManager.SetMusicEnabled(settings.musicEnabled);
        userDataManager.SetVibrationEnabled(settings.vibrationEnabled);
        userDataManager.SetMasterVolume(settings.masterVolume);
        userDataManager.SetMusicVolume(settings.musicVolume);
        userDataManager.SetSFXVolume(settings.sfxVolume);
    }

    private void ApplyStageProgress(System.Collections.Generic.Dictionary<string, StageProgress> cloudProgress)
    {
        foreach (var kvp in cloudProgress)
        {
            var progress = kvp.Value;
            if (progress.stageNumber > 0)
            {
                // 로컬 진행도와 클라우드 진행도를 비교하여 더 높은 점수 유지
                var localProgress = userDataManager.GetStageProgress(progress.stageNumber);
                int bestScore = Math.Max(localProgress.bestScore, progress.bestScore);
                bool completed = localProgress.completed || progress.completed;

                userDataManager.UpdateStageProgress(progress.stageNumber, bestScore, completed);
            }
        }
    }

    #endregion
}
