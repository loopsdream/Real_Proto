using UnityEngine;

/// <summary>
/// 개별 보상 항목을 나타내는 클래스
/// 보상 타입, 수량, 표시 정보를 포함
/// </summary>
[System.Serializable]
public class RewardItem
{
    [Header("Reward Settings")]
    public RewardType rewardType;       // 보상 종류
    public int amount;                  // 보상 수량

    [Header("Display Information")]
    public string displayName;          // 표시 이름 (예: "Gold Coins")
    public Sprite icon;                 // 보상 아이콘

    /// <summary>
    /// 생성자 - 기본값 설정
    /// </summary>
    public RewardItem()
    {
        rewardType = RewardType.Coins;
        amount = 0;
        displayName = "";
        icon = null;
    }

    /// <summary>
    /// 생성자 - 보상 타입과 수량 지정
    /// </summary>
    public RewardItem(RewardType type, int value)
    {
        rewardType = type;
        amount = value;
        displayName = "";
        icon = null;
    }
}