// ClearGoalUI.cs - Display clear goals in stage mode
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ClearGoalUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform goalContainer;  // Parent for goal items
    public GameObject goalItemPrefab;  // Prefab for individual goal display

    [Header("Goal Icons")]
    public Sprite[] blockColorIcons;  // 0=Red, 1=Blue, 2=Yellow, 3=Green, 4=Purple, 5=Pink
    public Sprite heartIcon;
    public Sprite cloverIcon;

    private List<ClearGoalItem> goalItems = new List<ClearGoalItem>();
    private StageGridManager gridManager;

    void Start()
    {
        gridManager = FindFirstObjectByType<StageGridManager>();
        if (gridManager == null)
        {
            Debug.LogError("StageGridManager not found!");
        }
    }

    // Initialize goal UI with stage clear goals
    public void InitializeGoals(List<ClearGoalData> clearGoals)
    {
        ClearGoals();

        if (clearGoals == null || clearGoals.Count == 0)
        {
            Debug.Log("No clear goals to display");
            return;
        }

        foreach (ClearGoalData goal in clearGoals)
        {
            CreateGoalItem(goal);
        }
    }

    // Create a single goal display item
    private void CreateGoalItem(ClearGoalData goal)
    {
        if (goalItemPrefab == null || goalContainer == null)
        {
            Debug.LogError("Goal item prefab or container not assigned!");
            return;
        }

        GameObject itemObj = Instantiate(goalItemPrefab, goalContainer);
        ClearGoalItem item = itemObj.GetComponent<ClearGoalItem>();

        if (item == null)
        {
            item = itemObj.AddComponent<ClearGoalItem>();
        }

        // Setup the goal item based on type
        switch (goal.goalType)
        {
            case ClearGoalType.DestroyAllBlocks:
                item.SetupGoal(null, "All Blocks", 0, 0);
                break;

            case ClearGoalType.CollectColorBlocks:
                Sprite colorIcon = GetBlockColorIcon(goal.targetColor);
                string colorName = GetColorName(goal.targetColor);
                item.SetupGoal(colorIcon, colorName, goal.targetColorCount, 0);
                break;

            case ClearGoalType.CollectCollectibles:
                Sprite collectibleIcon = GetCollectibleIcon(goal.collectibleType);
                string collectibleName = goal.collectibleType.ToString();
                item.SetupGoal(collectibleIcon, collectibleName, goal.targetCollectibleCount, 0);
                break;
        }

        item.goalData = goal;
        goalItems.Add(item);
    }

    // Update goal progress display
    public void UpdateGoalProgress(Dictionary<int, int> colorBlocks, Dictionary<CollectibleType, int> collectibles)
    {
        foreach (ClearGoalItem item in goalItems)
        {
            if (item.goalData == null) continue;

            switch (item.goalData.goalType)
            {
                case ClearGoalType.CollectColorBlocks:
                    int colorCount = colorBlocks.ContainsKey(item.goalData.targetColor)
                        ? colorBlocks[item.goalData.targetColor] : 0;
                    item.UpdateProgress(colorCount);
                    break;

                case ClearGoalType.CollectCollectibles:
                    int collectibleCount = collectibles.ContainsKey(item.goalData.collectibleType)
                        ? collectibles[item.goalData.collectibleType] : 0;
                    item.UpdateProgress(collectibleCount);
                    break;
            }
        }
    }

    // Clear all goal items
    private void ClearGoals()
    {
        foreach (ClearGoalItem item in goalItems)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        goalItems.Clear();
    }

    // Get block color icon by index
    private Sprite GetBlockColorIcon(int colorIndex)
    {
        if (blockColorIcons == null || colorIndex < 1 || colorIndex > blockColorIcons.Length)
            return null;
        return blockColorIcons[colorIndex - 1];  // Array is 0-indexed
    }

    // Get collectible icon
    private Sprite GetCollectibleIcon(CollectibleType type)
    {
        switch (type)
        {
            case CollectibleType.Heart:
                return heartIcon;
            case CollectibleType.Clover:
                return cloverIcon;
            default:
                return null;
        }
    }

    // Get color name
    private string GetColorName(int colorIndex)
    {
        switch (colorIndex)
        {
            case 1: return "Red";
            case 2: return "Blue";
            case 3: return "Yellow";
            case 4: return "Green";
            case 5: return "Purple";
            case 6: return "Pink";
            default: return "Unknown";
        }
    }
}

// Individual goal item component
public class ClearGoalItem : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI progressText;
    public Image progressBar;  // Optional progress bar

    [HideInInspector]
    public ClearGoalData goalData;

    private int targetCount;
    private int currentCount;

    void Awake()
    {
        // Try to find components if not assigned
        if (iconImage == null)
            iconImage = transform.Find("Icon")?.GetComponent<Image>();
        if (nameText == null)
            nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (progressText == null)
            progressText = transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
        if (progressBar == null)
            progressBar = transform.Find("ProgressBar")?.GetComponent<Image>();
    }

    // Setup goal item with initial data
    public void SetupGoal(Sprite icon, string goalName, int target, int current)
    {
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        if (nameText != null)
        {
            nameText.text = goalName;
        }

        targetCount = target;
        currentCount = current;
        UpdateProgress(current);
    }

    // Update progress display
    public void UpdateProgress(int current)
    {
        currentCount = current;

        if (progressText != null)
        {
            if (targetCount > 0)
            {
                progressText.text = $"{currentCount}/{targetCount}";

                // Change color when completed
                if (currentCount >= targetCount)
                {
                    progressText.color = Color.green;
                }
            }
            else
            {
                progressText.text = currentCount.ToString();
            }
        }

        // Update progress bar if available
        if (progressBar != null && targetCount > 0)
        {
            float progress = Mathf.Clamp01((float)currentCount / targetCount);
            progressBar.fillAmount = progress;
        }
    }
}