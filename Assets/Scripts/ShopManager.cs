using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    // 상점 데이터
    public List<ShopItemData> shopItems;

    // 상점 상태
    private ShopTab currentTab = ShopTab.Items;

    // 이벤트
    public UnityEvent<ShopItemData> OnItemPurchased;

    // 상점 열기/닫기
    //public void OpenShop(ShopTab tab = ShopTab.Items);
    //public void CloseShop();

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("ShopManager Instance created");
        }
        else
        {
            Debug.LogWarning("Multiple ShopManager instances detected!");
            Destroy(gameObject);
        }
    }

    // 구매 처리
    // 1. TryPurchaseItem - 아이템 구매 시도
    public bool TryPurchaseItem(ShopItemData item)
    {
        if (item == null)
        {
            Debug.LogError("ShopManager: Item is null");
            return false;
        }

        if (!CanAfford(item))
        {
            Debug.Log("ShopManager: Cannot afford item");
            return false;
        }

        ProcessPurchase(item);
        return true;
    }

    // 2. CanAfford - 구매 가능 여부 확인
    private bool CanAfford(ShopItemData item)
    {
        if (UserDataManager.Instance == null) return false;

        switch (item.priceType)
        {
            case PriceType.Coins:
                return UserDataManager.Instance.GetGameCoins() >= item.price;
            case PriceType.Diamonds:
                return UserDataManager.Instance.GetDiamonds() >= item.price;
            case PriceType.RealMoney:
                return true; // IAP는 별도 처리
            default:
                return false;
        }
    }

    // 3. ProcessPurchase - 실제 구매 처리
    private void ProcessPurchase(ShopItemData item)
    {
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("ShopManager: UserDataManager is null");
            return;
        }

        // 재화 차감
        bool paymentSuccess = false;
        switch (item.priceType)
        {
            case PriceType.Coins:
                paymentSuccess = UserDataManager.Instance.SpendGameCoins(item.price);
                break;
            case PriceType.Diamonds:
                paymentSuccess = UserDataManager.Instance.SpendDiamonds(item.price);
                break;
            case PriceType.RealMoney:
                // IAP 처리 (나중에 구현)
                Debug.Log("IAP purchase not implemented yet");
                return;
        }

        if (!paymentSuccess)
        {
            Debug.LogError("ShopManager: Payment failed");
            return;
        }

        // 보상 지급
        GiveReward(item);

        // 이벤트 발생
        OnItemPurchased?.Invoke(item);

        Debug.Log($"Purchase completed: {item.itemName}");
    }

    // 4. GiveReward - 보상 지급
    private void GiveReward(ShopItemData item)
    {
        if (UserDataManager.Instance == null) return;

        switch (item.rewardType)
        {
            case RewardType.Hammer:
                UserDataManager.Instance.AddItem(ItemType.Hammer, item.rewardAmount);
                break;
            case RewardType.Tornado:
                UserDataManager.Instance.AddItem(ItemType.Tornado, item.rewardAmount);
                break;
            case RewardType.Brush:
                UserDataManager.Instance.AddItem(ItemType.Brush, item.rewardAmount);
                break;
            case RewardType.Coins:
                UserDataManager.Instance.AddGameCoins(item.rewardAmount);
                break;
            case RewardType.Diamonds:
                UserDataManager.Instance.AddDiamonds(item.rewardAmount);
                break;
            case RewardType.Energy:
                UserDataManager.Instance.AddEnergy(item.rewardAmount);
                break;
        }

        Debug.Log($"Reward given: {item.rewardType} x{item.rewardAmount}");
    }
}

public enum ShopTab
{
    Items,      // 아이템 탭
    Currency,   // 재화 구매 탭
    Special     // 특별 상품 탭 (선택)
}
