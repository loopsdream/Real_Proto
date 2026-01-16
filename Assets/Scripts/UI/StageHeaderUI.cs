using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StageHeaderUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform goalContainer;  // Parent for multiple goal items
    public GameObject goalItemPrefab;  // Prefab for individual goal display

    [Header("Tap Display")]
    public GameObject tapPanel;
    public TextMeshProUGUI tapCountText;

    [Header("Goal Icons")]
    public Sprite[] blockColorIcons;  // 0=Red, 1=Blue, 2=Yellow, 3=Green, 4=Purple, 5=Pink
    public Sprite heartIcon;
    public Sprite cloverIcon;

    private List<GoalHeaderItem> goalItems = new List<GoalHeaderItem>();

    public void Initialize(List<ClearGoalData> clearGoals, int maxTaps)
    {
        ClearGoals();

        // Create goal items
        if (clearGoals != null && clearGoals.Count > 0)
        {
            foreach (ClearGoalData goal in clearGoals)
            {
                CreateGoalItem(goal);
            }
        }

        // Initialize tap count
        UpdateTapCount(maxTaps);
    }

    private void CreateGoalItem(ClearGoalData goal)
    {
        if (goalItemPrefab == null || goalContainer == null) return;

        GameObject itemObj = Instantiate(goalItemPrefab, goalContainer);
        GoalHeaderItem item = itemObj.GetComponent<GoalHeaderItem>();

        if (item == null)
        {
            item = itemObj.AddComponent<GoalHeaderItem>();
        }

        // Setup based on goal type
        Sprite icon = null;
        int targetCount = 0;

        switch (goal.goalType)
        {
            case ClearGoalType.DestroyAllBlocks:
                // Don't show icon for destroy all
                break;

            case ClearGoalType.CollectColorBlocks:
                icon = GetBlockColorIcon(goal.targetColor);
                targetCount = goal.targetColorCount;
                break;

            case ClearGoalType.CollectCollectibles:
                icon = GetCollectibleIcon(goal.collectibleType);
                targetCount = goal.targetCollectibleCount;
                break;
        }

        item.Setup(icon, goal, targetCount);
        goalItems.Add(item);
    }

    public void UpdateGoalProgress(Dictionary<int, int> colorBlocks, Dictionary<CollectibleType, int> collectibles)
    {
        foreach (GoalHeaderItem item in goalItems)
        {
            if (item.goalData == null) continue;

            int currentCount = 0;

            switch (item.goalData.goalType)
            {
                case ClearGoalType.CollectColorBlocks:
                    colorBlocks.TryGetValue(item.goalData.targetColor, out currentCount);
                    break;

                case ClearGoalType.CollectCollectibles:
                    collectibles.TryGetValue(item.goalData.collectibleType, out currentCount);
                    break;
            }

            item.UpdateProgress(currentCount);
        }
    }

    public void UpdateTapCount(int remainingTaps)
    {
        if (tapCountText != null)
        {
            tapCountText.text = remainingTaps.ToString();
        }
    }

    private void ClearGoals()
    {
        foreach (GoalHeaderItem item in goalItems)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        goalItems.Clear();
    }

    private Sprite GetBlockColorIcon(int colorIndex)
    {
        if (blockColorIcons == null || colorIndex < 1 || colorIndex > blockColorIcons.Length)
            return null;
        return blockColorIcons[colorIndex - 1];
    }

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
}