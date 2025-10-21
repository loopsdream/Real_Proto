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

    // 구매 처리
    //public bool TryPurchaseItem(ShopItemData item);
    //private bool CanAfford(ShopItemData item);
    //private void ProcessPurchase(ShopItemData item);
}

public enum ShopTab
{
    Items,      // 아이템 탭
    Currency,   // 재화 구매 탭
    Special     // 특별 상품 탭 (선택)
}