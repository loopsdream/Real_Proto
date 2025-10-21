// ShopUIManager.cs - 상점 UI 전체 관리
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [Header("Main UI References")]
    public GameObject shopPanel;
    public Transform itemContainer; // ScrollView Content
    public ShopItemSlotUI itemSlotPrefab;

    [Header("Buttons")]
    public Button closeButton;
    public Button itemsTabButton;
    public Button currencyTabButton;

    [Header("Confirm Popup")]
    public GameObject confirmPopup;
    public TextMeshProUGUI confirmMessageText;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Tab Visual")]
    public Color selectedTabColor = Color.green;
    public Color normalTabColor = Color.white;

    private List<ShopItemSlotUI> activeSlots = new List<ShopItemSlotUI>();
    private ShopTab currentTab = ShopTab.Items;
    private ShopItemData pendingPurchaseItem;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple ShopUIManager instances detected!");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SetupButtons();

        // 초기 상태: 상점 닫힘
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
        }
    }

    void SetupButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideShop);
        }

        if (itemsTabButton != null)
        {
            itemsTabButton.onClick.RemoveAllListeners();
            itemsTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Items));
        }

        if (currencyTabButton != null)
        {
            currencyTabButton.onClick.RemoveAllListeners();
            currencyTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Currency));
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnPurchaseConfirmed);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HideConfirmPopup);
        }
    }

    public void ShowShop(ShopTab tab = ShopTab.Items)
    {
        if (shopPanel == null)
        {
            Debug.LogError("ShopUIManager: shopPanel is null!");
            return;
        }

        shopPanel.SetActive(true);
        SwitchTab(tab);
        PlayOpenAnimation();

        Debug.Log("Shop opened");
    }

    public void HideShop()
    {
        if (shopPanel == null) return;

        PlayCloseAnimation();

        // 애니메이션 후 비활성화 (0.3초 대기)
        Invoke(nameof(DeactivateShopPanel), 0.3f);

        Debug.Log("Shop closed");
    }

    void DeactivateShopPanel()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public void SwitchTab(ShopTab tab)
    {
        currentTab = tab;
        UpdateTabVisuals();
        LoadTabContent(tab);
    }

    void UpdateTabVisuals()
    {
        // 탭 버튼 색상 변경
        if (itemsTabButton != null)
        {
            var itemsImage = itemsTabButton.GetComponent<Image>();
            if (itemsImage != null)
            {
                itemsImage.color = (currentTab == ShopTab.Items) ? selectedTabColor : normalTabColor;
            }
        }

        if (currencyTabButton != null)
        {
            var currencyImage = currencyTabButton.GetComponent<Image>();
            if (currencyImage != null)
            {
                currencyImage.color = (currentTab == ShopTab.Currency) ? selectedTabColor : normalTabColor;
            }
        }
    }

    void LoadTabContent(ShopTab tab)
    {
        // 기존 슬롯 정리
        ClearItemSlots();

        // ShopManager에서 해당 탭의 아이템 가져오기
        if (ShopManager.Instance == null)
        {
            Debug.LogError("ShopUIManager: ShopManager.Instance is null!");
            return;
        }

        //TODO
        //List<ShopItemData> items = ShopManager.Instance.GetItemsForTab(tab);
        //CreateItemSlots(items);

        //Debug.Log($"Loaded {items.Count} items for tab: {tab}");
    }

    void ClearItemSlots()
    {
        // 기존 슬롯 파괴
        foreach (var slot in activeSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        activeSlots.Clear();
    }

    void CreateItemSlots(List<ShopItemData> items)
    {
        if (itemContainer == null || itemSlotPrefab == null)
        {
            Debug.LogError("ShopUIManager: itemContainer or itemSlotPrefab is null!");
            return;
        }

        foreach (var item in items)
        {
            if (item == null || !item.isAvailable) continue;

            // 슬롯 생성
            ShopItemSlotUI slot = Instantiate(itemSlotPrefab, itemContainer);
            slot.Setup(item);

            // 구매 버튼 이벤트 연결
            slot.OnBuyButtonClicked += OnItemBuyButtonClicked;

            activeSlots.Add(slot);
        }
    }

    void OnItemBuyButtonClicked(ShopItemData item)
    {
        if (item == null)
        {
            Debug.LogError("ShopUIManager: Clicked item is null!");
            return;
        }

        // 구매 확인 팝업 표시
        ShowConfirmPopup(item);
    }

    void ShowConfirmPopup(ShopItemData item)
    {
        if (confirmPopup == null)
        {
            Debug.LogWarning("ShopUIManager: confirmPopup is not assigned. Purchasing directly.");
            ProcessPurchase(item);
            return;
        }

        pendingPurchaseItem = item;

        // 확인 메시지 설정
        if (confirmMessageText != null)
        {
            string priceString = GetPriceString(item);
            confirmMessageText.text = $"Purchase {item.itemName}?\n\nPrice: {priceString}";
        }

        confirmPopup.SetActive(true);
        Debug.Log($"Confirm popup shown for: {item.itemName}");
    }

    void HideConfirmPopup()
    {
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
        }
        pendingPurchaseItem = null;
    }

    void OnPurchaseConfirmed()
    {
        if (pendingPurchaseItem == null)
        {
            Debug.LogError("ShopUIManager: No pending purchase item!");
            HideConfirmPopup();
            return;
        }

        ProcessPurchase(pendingPurchaseItem);
        HideConfirmPopup();
    }

    void ProcessPurchase(ShopItemData item)
    {
        if (ShopManager.Instance == null)
        {
            Debug.LogError("ShopUIManager: ShopManager.Instance is null!");
            return;
        }

        //TODO
        //bool success = ShopManager.Instance.TryPurchaseItem(item);
        bool success = false;

        if (success)
        {
            Debug.Log($"Purchase successful: {item.itemName}");

            // 모든 슬롯의 구매 가능 여부 갱신
            RefreshAllSlots();

            // 성공 피드백 (선택)
            ShowPurchaseSuccessFeedback(item);
        }
        else
        {
            Debug.Log($"Purchase failed: {item.itemName}");

            // 실패 피드백 (선택)
            ShowPurchaseFailedFeedback(item);
        }
    }

    void RefreshAllSlots()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null)
            {
                slot.RefreshAffordability();
            }
        }
    }

    string GetPriceString(ShopItemData item)
    {
        switch (item.priceType)
        {
            case PriceType.Coins:
                return $"{item.price} Coins";
            case PriceType.Diamonds:
                return $"{item.price} Diamonds";
            case PriceType.RealMoney:
                return $"${(item.price / 100f):0.00}";
            default:
                return item.price.ToString();
        }
    }

    void PlayOpenAnimation()
    {
        if (shopPanel == null) return;

        // LeanTween 애니메이션 (선택)
        shopPanel.transform.localScale = Vector3.zero;
        LeanTween.scale(shopPanel, Vector3.one, 0.3f).setEaseOutBack();
    }

    void PlayCloseAnimation()
    {
        if (shopPanel == null) return;

        // LeanTween 애니메이션 (선택)
        LeanTween.scale(shopPanel, Vector3.zero, 0.3f).setEaseInBack();
    }

    void ShowPurchaseSuccessFeedback(ShopItemData item)
    {
        // 간단한 로그 (나중에 UI 피드백 추가 가능)
        Debug.Log($"[SUCCESS] Purchased: {item.itemName}");

        // 여기에 성공 효과음/파티클 추가 가능
    }

    void ShowPurchaseFailedFeedback(ShopItemData item)
    {
        // 간단한 로그 (나중에 UI 피드백 추가 가능)
        Debug.Log($"[FAILED] Cannot purchase: {item.itemName}");

        // 여기에 실패 효과음/알림 추가 가능
    }

    void OnDestroy()
    {
        // 이벤트 정리
        foreach (var slot in activeSlots)
        {
            if (slot != null)
            {
                slot.OnBuyButtonClicked -= OnItemBuyButtonClicked;
            }
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}