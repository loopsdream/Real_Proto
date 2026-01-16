using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 보상 시스템 총괄 관리자
/// 모든 종류의 보상 지급을 처리하고 UserDataManager와 연동
/// </summary>
public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    // Dependencies
    private UserDataManager userDataManager;

    // Events
    public event Action<RewardType, int> OnRewardGranted;
    public event Action<List<RewardItem>> OnMultipleRewardsGranted;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeManager();
    }

    private void InitializeManager()
    {
        userDataManager = UserDataManager.Instance;

        if (userDataManager == null)
        {
            Debug.LogError("RewardManager: UserDataManager not found!");
        }
    }

    #region Public Methods

    /// <summary>
    /// RewardData를 받아서 모든 보상을 지급
    /// </summary>
    public void GrantReward(RewardData rewardData)
    {
        if (rewardData == null || !rewardData.IsValid())
        {
            Debug.LogError("RewardManager: Invalid RewardData");
            return;
        }

        Debug.Log($"Granting reward: {rewardData.rewardName}");

        // 모든 보상 항목 지급
        foreach (var rewardItem in rewardData.rewards)
        {
            GrantSingleReward(rewardItem.rewardType, rewardItem.amount);
        }

        // 이벤트 발생
        OnMultipleRewardsGranted?.Invoke(rewardData.rewards);

        // 팝업 표시 (향후 구현)
        if (rewardData.showPopup)
        {
            ShowRewardPopup(rewardData);
        }
    }

    /// <summary>
    /// 단일 보상 지급 (타입과 수량 직접 지정)
    /// </summary>
    public void GrantReward(RewardType rewardType, int amount)
    {
        GrantSingleReward(rewardType, amount);
        OnRewardGranted?.Invoke(rewardType, amount);
    }

    /// <summary>
    /// 여러 보상을 한번에 지급
    /// </summary>
    public void GrantRewards(List<RewardItem> rewards)
    {
        if (rewards == null || rewards.Count == 0)
        {
            Debug.LogWarning("RewardManager: Empty reward list");
            return;
        }

        foreach (var reward in rewards)
        {
            GrantSingleReward(reward.rewardType, reward.amount);
        }

        OnMultipleRewardsGranted?.Invoke(rewards);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 개별 보상 타입에 따라 실제 지급 처리
    /// </summary>
    private void GrantSingleReward(RewardType rewardType, int amount)
    {
        if (userDataManager == null)
        {
            Debug.LogError("RewardManager: UserDataManager is null!");
            return;
        }

        switch (rewardType)
        {
            case RewardType.Coins:
                userDataManager.AddGameCoins(amount);
                Debug.Log($"Granted {amount} Coins");
                break;

            case RewardType.Diamonds:
                userDataManager.AddDiamonds(amount);
                Debug.Log($"Granted {amount} Diamonds");
                break;

            case RewardType.Energy:
                userDataManager.AddEnergy(amount);
                Debug.Log($"Granted {amount} Energy");
                break;

            case RewardType.Hammer:
                userDataManager.AddItem(ItemType.Hammer, amount);
                Debug.Log($"Granted {amount} Hammer items");
                break;

            case RewardType.Tornado:
                userDataManager.AddItem(ItemType.Tornado, amount);
                Debug.Log($"Granted {amount} Tornado items");
                break;

            case RewardType.Brush:
                userDataManager.AddItem(ItemType.Brush, amount);
                Debug.Log($"Granted {amount} Brush items");
                break;

            case RewardType.UnlockStage:
                // 스테이지 해금 로직 (향후 구현)
                Debug.Log($"Unlocked stage {amount}");
                break;

            case RewardType.ExperiencePoints:
                // 경험치 지급 로직 (향후 구현)
                Debug.Log($"Granted {amount} Experience Points");
                break;

            default:
                Debug.LogWarning($"RewardManager: Unknown reward type {rewardType}");
                break;
        }
    }

    /// <summary>
    /// 보상 팝업 표시 (향후 UI 구현 시 사용)
    /// </summary>
    private void ShowRewardPopup(RewardData rewardData)
    {
        // TODO: UI 구현 시 RewardPopupUI 연동
        Debug.Log($"Show Reward Popup: {rewardData.popupMessage}");
    }

    #endregion

    #region Validation

    /// <summary>
    /// 보상을 지급할 수 있는지 검증 (재화 한도 등)
    /// </summary>
    public bool CanGrantReward(RewardType rewardType, int amount)
    {
        // 향후 확장: 재화 최대치, 인벤토리 공간 등 체크
        return true;
    }

    #endregion
}