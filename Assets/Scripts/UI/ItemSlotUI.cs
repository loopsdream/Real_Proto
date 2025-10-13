// ItemSlotUI.cs - ���� ������ ���� UI ����
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI itemCountText;
    public Button itemButton;
    public Image buttonBackground;

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;
    public Color disabledColor = Color.gray;

    private ItemData itemData;
    private bool isSelected = false;
    private int currentCount = 0;

    // �̺�Ʈ
    public System.Action<ItemType> OnItemSelected;

        void Awake()
    {
        // UI 계층 구조 올바른 설정 - Raycast Target 설정
        SetupRaycastTargets();
    }

void Start()
    {
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    public void Setup(ItemData data)
    {
        itemData = data;

        if (itemIcon != null && data.itemIcon != null)
        {
            itemIcon.sprite = data.itemIcon;
        }

        UpdateDisplay();
    }

    public void UpdateCount(int count)
    {
        currentCount = count;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        // ���� ǥ�� ������Ʈ
        if (itemCountText != null)
        {
            itemCountText.text = currentCount.ToString();
        }

        // ��ư ���� ������Ʈ
        bool canUse = currentCount > 0;
        if (itemButton != null)
        {
            itemButton.interactable = canUse;
        }

        // ���� ������Ʈ
        Color targetColor = canUse ? (isSelected ? selectedColor : normalColor) : disabledColor;
        if (buttonBackground != null)
        {
            buttonBackground.color = targetColor;
        }
    }

    void OnItemClicked()
    {
        if (currentCount <= 0) return;

        OnItemSelected?.Invoke(itemData.itemType);
        SetSelected(!isSelected);

        // ���� ���
        if (AudioManager.Instance != null)
        {
            //TODO
            //AudioManager.Instance.PlaySFX("ItemSelect");
        }
    }
    
    /// <summary>
    /// UI 계층구조의 Raycast Target을 올바르게 설정
    /// Button만 터치를 받고, 내부 Image들은 터치를 차단하지 않도록 설정
    /// </summary>
    void SetupRaycastTargets()
    {
        // Button은 터치를 받아야 함
        if (itemButton != null)
        {
            Image buttonImage = itemButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }
        }

        // Button 내부의 다른 Image들은 터치를 차단하지 않아야 함
        if (itemIcon != null)
        {
            itemIcon.raycastTarget = false;
        }

        if (buttonBackground != null)
        {
            buttonBackground.raycastTarget = false;
        }

        // TextMeshPro는 기본적으로 raycastTarget이 true이므로 false로 설정
        if (itemCountText != null)
        {
            itemCountText.raycastTarget = false;
        }

        Debug.Log("ItemSlotUI: Raycast targets configured correctly");
    }
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateDisplay();
    }
}