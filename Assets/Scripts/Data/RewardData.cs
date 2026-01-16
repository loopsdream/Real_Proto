using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject 기반 보상 데이터
/// 여러 개의 보상 항목을 묶어서 관리
/// 스테이지 클리어, 일일 로그인, 업적 등에 사용
/// </summary>
[CreateAssetMenu(fileName = "New Reward", menuName = "CROxCRO/Reward Data")]
public class RewardData : ScriptableObject
{
    [Header("Reward Information")]
    [Tooltip("Unique identifier for this reward")]
    public string rewardId = "";                // 고유 ID (예: "stage_1_clear", "daily_day1")

    [Tooltip("Display name for this reward")]
    public string rewardName = "";              // 보상 이름 (예: "Stage 1 Clear Reward")

    [TextArea(3, 5)]
    [Tooltip("Description of this reward")]
    public string description = "";             // 보상 설명

    [Header("Reward Contents")]
    [Tooltip("List of reward items in this reward pack")]
    public List<RewardItem> rewards = new List<RewardItem>();

    [Header("Display Settings")]
    [Tooltip("Show popup when this reward is granted")]
    public bool showPopup = true;               // 보상 팝업 표시 여부

    [Tooltip("Message to show in reward popup")]
    public string popupMessage = "Reward Received!";  // 팝업 메시지

    /// <summary>
    /// 이 보상 데이터가 유효한지 검증
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(rewardId))
        {
            Debug.LogWarning("RewardData has empty rewardId");
            return false;
        }

        if (rewards == null || rewards.Count == 0)
        {
            Debug.LogWarning($"RewardData {rewardId} has no rewards");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 특정 타입의 보상이 포함되어 있는지 확인
    /// </summary>
    public bool ContainsRewardType(RewardType type)
    {
        foreach (var reward in rewards)
        {
            if (reward.rewardType == type)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 특정 타입의 보상 수량 반환
    /// </summary>
    public int GetRewardAmount(RewardType type)
    {
        foreach (var reward in rewards)
        {
            if (reward.rewardType == type)
            {
                return reward.amount;
            }
        }
        return 0;
    }
}