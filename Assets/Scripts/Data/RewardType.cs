/// <summary>
/// 보상 종류를 정의하는 열거형
/// 재화, 아이템, 특수 보상 등을 포함
/// </summary>
public enum RewardType
{
    // 재화 보상
    Coins,              // 게임 코인
    Diamonds,           // 다이아몬드
    Energy,             // 에너지

    // 아이템 보상 (ShopItemData와 호환)
    Hammer,             // 망치 아이템 (HammerItem에서 변경)
    Tornado,            // 회오리 아이템 (TornadoItem에서 변경)
    Brush,              // 붓 아이템 (BrushItem에서 변경)

    // 특수 보상
    UnlockStage,        // 스테이지 해금
    ExperiencePoints    // 경험치
}