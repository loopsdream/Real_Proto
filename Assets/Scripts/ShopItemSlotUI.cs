// ShopItemSlotUI.cs - 개별 상점 아이템 슬롯 UI
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText; // 설명 (선택)
    public TextMeshProUGUI priceText;
    public Image priceIcon; // 코인/다이아 아이콘
    public Button buyButton;
    public Image background;

    [Header("Price Icons")]
    public Sprite coinIcon;
    public Sprite diamondIcon;

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color cannotAffordColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private ShopItemData itemData;
    private bool canAfford = true;

    // 이벤트
    public System.Action<ShopItemData> OnBuyButtonClicked;

    void Awake()
    {
        // Raycast Target 설정
        SetupRaycastTargets();
    }

    void Start()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    void SetupRaycastTargets()
    {
        // 버튼만 Raycast Target으로 설정
        if (buyButton != null)
        {
            var buttonImage = buyButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }
        }

        // 다른 Image들은 Raycast Target 비활성화
        if (itemIcon != null) itemIcon.raycastTarget = false;
        if (background != null) background.raycastTarget = false;
        if (priceIcon != null) priceIcon.raycastTarget = false;
    }

    public void Setup(ShopItemData data)
    {
        if (data == null)
        {
            Debug.LogError("ShopItemSlotUI: ShopItemData is null!");
            return;
        }

        itemData = data;

        // 아이콘 설정
        if (itemIcon != null && data.itemIcon != null)
        {
            itemIcon.sprite = data.itemIcon;
            itemIcon.enabled = true;
        }

        // 이름 설정
        if (itemNameText != null)
        {
            itemNameText.text = data.itemName;
        }

        // 설명 설정 (선택)
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = data.description;
        }

        // 가격 표시 업데이트
        UpdatePriceDisplay();

        // 구매 가능 여부 체크
        UpdateAffordability();
    }

    void UpdatePriceDisplay()
    {
        if (itemData == null) return;

        // 가격 텍스트 설정
        if (priceText != null)
        {
            string priceString = "";

            switch (itemData.priceType)
            {
                case PriceType.Coins:
                    priceString = itemData.price.ToString();
                    break;
                case PriceType.Diamonds:
                    priceString = itemData.price.ToString();
                    break;
                case PriceType.RealMoney:
                    priceString = "$" + (itemData.price / 100f).ToString("0.00");
                    break;
            }

            priceText.text = priceString;
        }

        // 가격 아이콘 설정
        if (priceIcon != null)
        {
            switch (itemData.priceType)
            {
                case PriceType.Coins:
                    priceIcon.sprite = coinIcon;
                    priceIcon.enabled = coinIcon != null;
                    break;
                case PriceType.Diamonds:
                    priceIcon.sprite = diamondIcon;
                    priceIcon.enabled = diamondIcon != null;
                    break;
                case PriceType.RealMoney:
                    priceIcon.enabled = false; // 달러는 아이콘 없음
                    break;
            }
        }
    }

    public void UpdateAffordability()
    {
        if (itemData == null || UserDataManager.Instance == null)
        {
            canAfford = false;
            UpdateVisualState();
            return;
        }

        // 구매 가능 여부 체크
        switch (itemData.priceType)
        {
            case PriceType.Coins:
                canAfford = UserDataManager.Instance.GetGameCoins() >= itemData.price;
                break;
            case PriceType.Diamonds:
                canAfford = UserDataManager.Instance.GetDiamonds() >= itemData.price;
                break;
            case PriceType.RealMoney:
                canAfford = true; // IAP는 항상 구매 가능
                break;
        }

        UpdateVisualState();
    }

    void UpdateVisualState()
    {
        // 버튼 활성화/비활성화
        if (buyButton != null)
        {
            buyButton.interactable = canAfford && itemData.isAvailable;
        }

        // 배경 색상 변경
        if (background != null)
        {
            background.color = canAfford ? normalColor : cannotAffordColor;
        }

        // 텍스트 색상 변경 (선택)
        Color textColor = canAfford ? Color.black : Color.gray;
        if (itemNameText != null) itemNameText.color = textColor;
        if (priceText != null) priceText.color = textColor;
    }

    void OnBuyClicked()
    {
        if (itemData == null)
        {
            Debug.LogError("ShopItemSlotUI: Cannot buy - itemData is null");
            return;
        }

        if (!canAfford)
        {
            Debug.Log("ShopItemSlotUI: Cannot afford this item");
            // 여기에 "재화 부족" 피드백 추가 가능
            return;
        }

        // 구매 이벤트 발생
        OnBuyButtonClicked?.Invoke(itemData);
    }

    // 외부에서 재화 변경 시 호출
    public void RefreshAffordability()
    {
        UpdateAffordability();
    }

    void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuyClicked);
        }
    }
}