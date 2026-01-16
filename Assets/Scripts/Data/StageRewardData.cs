using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스테이지 클리어 보상 데이터 (ScriptableObject)
/// 별 개수에 따른 차등 보상을 정의
/// </summary>
[CreateAssetMenu(fileName = "StageReward", menuName = "CROxCRO/Stage Reward Data")]
public class StageRewardData : ScriptableObject
{
    [Header("Stage Information")]
    public int stageNumber;
    public string stageName;

    [Header("Star Rewards")]
    [Tooltip("1별 클리어 보상")]
    public List<RewardItem> oneStarRewards = new List<RewardItem>();

    [Tooltip("2별 클리어 보상")]
    public List<RewardItem> twoStarRewards = new List<RewardItem>();

    [Tooltip("3별 클리어 보상 (완벽 클리어)")]
    public List<RewardItem> threeStarRewards = new List<RewardItem>();

    [Header("Bonus Rewards")]
    [Tooltip("첫 클리어 보너스 (1회만 지급)")]
    public List<RewardItem> firstClearBonus = new List<RewardItem>();

    [Tooltip("완벽 클리어 보너스 (3별 + 추가 조건)")]
    public List<RewardItem> perfectClearBonus = new List<RewardItem>();

    /// <summary>
    /// 별 개수에 따른 보상 리스트 반환
    /// </summary>
    public List<RewardItem> GetRewardsByStars(int stars)
    {
        switch (stars)
        {
            case 1:
                return oneStarRewards;
            case 2:
                return twoStarRewards;
            case 3:
                return threeStarRewards;
            default:
                Debug.LogWarning($"Invalid star count: {stars}");
                return new List<RewardItem>();
        }
    }

    /// <summary>
    /// 총 보상 계산 (별 보상 + 보너스)
    /// </summary>
    public List<RewardItem> CalculateTotalRewards(int stars, bool isFirstClear, bool isPerfectClear)
    {
        List<RewardItem> totalRewards = new List<RewardItem>();

        // 별 개수에 따른 기본 보상
        totalRewards.AddRange(GetRewardsByStars(stars));

        // 첫 클리어 보너스
        if (isFirstClear)
        {
            totalRewards.AddRange(firstClearBonus);
        }

        // 완벽 클리어 보너스
        if (isPerfectClear && stars == 3)
        {
            totalRewards.AddRange(perfectClearBonus);
        }

        return totalRewards;
    }

    /// <summary>
    /// 데이터 유효성 검증
    /// </summary>
    public bool IsValid()
    {
        if (stageNumber <= 0)
        {
            Debug.LogWarning("StageRewardData: Invalid stage number");
            return false;
        }

        return true;
    }
}