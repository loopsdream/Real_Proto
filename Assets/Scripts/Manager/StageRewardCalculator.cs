using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스테이지 클리어 보상 계산 유틸리티
/// 점수, 이동 횟수 등을 기반으로 별 개수 계산
/// </summary>
public static class StageRewardCalculator
{
    /// <summary>
    /// 점수와 목표 점수를 기반으로 별 개수 계산
    /// </summary>
    public static int CalculateStars(int currentScore, int targetScore)
    {
        if (currentScore < targetScore)
        {
            return 0; // 클리어 실패
        }

        float scoreRatio = (float)currentScore / targetScore;

        if (scoreRatio >= 2.0f)
        {
            return 3; // 목표의 200% 이상
        }
        else if (scoreRatio >= 1.5f)
        {
            return 2; // 목표의 150% 이상
        }
        else
        {
            return 1; // 목표 달성
        }
    }

    /// <summary>
    /// 완벽 클리어 조건 확인
    /// 3별 + 아이템 미사용 + 추가 조건
    /// </summary>
    public static bool IsPerfectClear(int stars, int itemsUsed, int movesLeft)
    {
        // 3별 클리어
        if (stars < 3)
            return false;

        // 아이템 미사용
        if (itemsUsed > 0)
            return false;

        // 이동 횟수 여유 (선택사항)
        // if (movesLeft < 5)
        //     return false;

        return true;
    }

    /// <summary>
    /// 스테이지별 보상 데이터 로드
    /// </summary>
    public static StageRewardData LoadStageRewardData(int stageNumber)
    {
        string path = $"StageRewards/Stage_{stageNumber:D2}_Reward";
        StageRewardData rewardData = Resources.Load<StageRewardData>(path);

        if (rewardData == null)
        {
            Debug.LogWarning($"StageRewardData not found for stage {stageNumber}");
        }

        return rewardData;
    }

    /// <summary>
    /// 첫 클리어 여부 확인
    /// </summary>
    public static bool IsFirstClear(int stageNumber)
    {
        UserDataManager userDataManager = UserDataManager.Instance;
        if (userDataManager == null)
            return false;

        // UserData에서 스테이지 클리어 기록 확인
        // TODO: UserData.stageProgress 구조 확인 필요
        return !userDataManager.IsStageCleared(stageNumber);
    }
}