// ItemUIManager.cs - Stage Mode Item UI Manager
using UnityEngine;

public class ItemUIManager : MonoBehaviour
{
    [Header("Item UI References")]
    public GameObject itemPanel;
    public ItemSlotUI hammerSlot;
    public ItemSlotUI tornadoSlot;
    public ItemSlotUI brushSlot;

    [Header("Item Data")]
    public ItemData hammerData;
    public ItemData tornadoData;
    public ItemData brushData;

    private ItemType? selectedItemType = null;
    private StageGridManager stageGridManager;

    // Events
    public System.Action<ItemType> OnItemModeActivated;
    public System.Action OnItemModeDeactivated;

    void Start()
    {
        // Find StageGridManager using GameObject.Find for Unity 6.0 compatibility
        GameObject stageGridObj = GameObject.Find("StageGridManager");
        if (stageGridObj != null)
        {
            stageGridManager = stageGridObj.GetComponent<StageGridManager>();
        }

        // Setup item slots
        SetupItemSlots();

        // Connect UserDataManager events
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnItemCountChanged += OnItemCountChanged;
        }

        // Initial item count update
        RefreshAllItemCounts();
    }

    void OnDestroy()
    {
        // Unsubscribe events
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnItemCountChanged -= OnItemCountChanged;
        }
    }

    void SetupItemSlots()
    {
        if (hammerSlot != null)
        {
            hammerSlot.Setup(hammerData);
            hammerSlot.OnItemSelected += OnItemSlotSelected;
        }

        if (tornadoSlot != null)
        {
            tornadoSlot.Setup(tornadoData);
            tornadoSlot.OnItemSelected += OnItemSlotSelected;
        }

        if (brushSlot != null)
        {
            brushSlot.Setup(brushData);
            brushSlot.OnItemSelected += OnItemSlotSelected;
        }
    }

    void OnItemSlotSelected(ItemType itemType)
    {
        // Deselect previous selection
        if (selectedItemType.HasValue && selectedItemType != itemType)
        {
            DeselectAllItems();
        }

        // Select new item
        if (selectedItemType != itemType)
        {
            selectedItemType = itemType;
            OnItemModeActivated?.Invoke(itemType);
            Debug.Log("Item mode activated: " + itemType.ToString());
        }
        else
        {
            // Deselect if same item clicked again
            selectedItemType = null;
            OnItemModeDeactivated?.Invoke();
            Debug.Log("Item mode deactivated");
        }
    }

    void DeselectAllItems()
    {
        hammerSlot?.SetSelected(false);
        tornadoSlot?.SetSelected(false);
        brushSlot?.SetSelected(false);
    }

    void OnItemCountChanged(ItemType itemType, int newCount)
    {
        switch (itemType)
        {
            case ItemType.Hammer:
                hammerSlot?.UpdateCount(newCount);
                break;
            case ItemType.Tornado:
                tornadoSlot?.UpdateCount(newCount);
                break;
            case ItemType.Brush:
                brushSlot?.UpdateCount(newCount);
                break;
        }

        // Deselect item if count becomes 0
        if (selectedItemType == itemType && newCount <= 0)
        {
            selectedItemType = null;
            OnItemModeDeactivated?.Invoke();
        }
    }

    void RefreshAllItemCounts()
    {
        if (UserDataManager.Instance == null) return;

        hammerSlot?.UpdateCount(UserDataManager.Instance.GetItemCount(ItemType.Hammer));
        tornadoSlot?.UpdateCount(UserDataManager.Instance.GetItemCount(ItemType.Tornado));
        brushSlot?.UpdateCount(UserDataManager.Instance.GetItemCount(ItemType.Brush));
    }

    public ItemType? GetSelectedItemType()
    {
        return selectedItemType;
    }

    public void ClearSelection()
    {
        selectedItemType = null;
        DeselectAllItems();
        OnItemModeDeactivated?.Invoke();
    }
}