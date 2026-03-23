using UnityEngine;

// 광고 단위 ID를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "AdConfig", menuName = "CROxCRO/Ad Config")]
public class AdConfig : ScriptableObject
{
    // 테스트 모드 여부 (출시 전까지 true 유지)
    [Header("Test Mode")]
    public bool useTestIds = true;

    // 실제 광고 단위 ID
    [Header("Real Ad Unit IDs")]
    public string realRewardedDoubleId = "ca-app-pub-5576181861134579/8732486508";
    public string realRewardedEnergyId = "ca-app-pub-5576181861134579/5451717042";

    // 구글 공식 테스트 광고 단위 ID
    private const string TEST_REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";

    // 외부에서 사용할 프로퍼티 (테스트/실제 자동 분기)
    public string RewardedDoubleId => useTestIds ? TEST_REWARDED_ID : realRewardedDoubleId;
    public string RewardedEnergyId => useTestIds ? TEST_REWARDED_ID : realRewardedEnergyId;
}