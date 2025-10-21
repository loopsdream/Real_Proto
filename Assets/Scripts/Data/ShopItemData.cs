using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/Item Data")]
public class ShopItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public string description;
    public Sprite itemIcon;

    // 가격 정보
    public PriceType priceType;
    public int price;

    // 보상 정보
    public RewardType rewardType;
    public int rewardAmount;

    // 상점 설정
    public ShopTab shopTab;
    public bool isAvailable = true;
    public bool isLimitedTime = false;
}

public enum PriceType
{
    Coins,
    Diamonds,
    RealMoney  // IAP 연동 시
}

public enum RewardType
{
    Hammer,
    Tornado,
    Brush,
    Coins,
    Diamonds,
    Energy
}