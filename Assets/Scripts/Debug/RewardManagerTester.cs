using UnityEngine;

/// <summary>
/// RewardManager 테스트용 스크립트
/// Inspector에서 테스트 버튼을 통해 보상 지급 테스트
/// </summary>
public class RewardManagerTester : MonoBehaviour
{
    [Header("Test Settings")]
    public RewardData testRewardData;
    public RewardType testRewardType = RewardType.Coins;
    public int testAmount = 100;

    private void Update()
    {
        // 키보드 단축키로 테스트
        if (Input.GetKeyDown(KeyCode.C))
        {
            RewardManager.Instance?.GrantReward(RewardType.Coins, 100);
            Debug.Log("Test: Granted 100 Coins");
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            RewardManager.Instance?.GrantReward(RewardType.Diamonds, 10);
            Debug.Log("Test: Granted 10 Diamonds");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            RewardManager.Instance?.GrantReward(RewardType.Energy, 1);
            Debug.Log("Test: Granted 1 Energy");
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            RewardManager.Instance?.GrantReward(RewardType.Hammer, 1);
            Debug.Log("Test: Granted 1 Hammer");
        }

        if (Input.GetKeyDown(KeyCode.R) && testRewardData != null)
        {
            RewardManager.Instance?.GrantReward(testRewardData);
            Debug.Log($"Test: Granted RewardData - {testRewardData.rewardName}");
        }
    }
}