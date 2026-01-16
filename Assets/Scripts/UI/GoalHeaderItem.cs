using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalHeaderItem : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI countText;

    [HideInInspector]
    public ClearGoalData goalData;

    private int targetCount;

    public void Setup(Sprite icon, ClearGoalData data, int target)
    {
        goalData = data;
        targetCount = target;

        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        UpdateProgress(0);
    }

    public void UpdateProgress(int current)
    {
        if (countText != null)
        {
            int remaining = targetCount - current;
            if (remaining < 0) remaining = 0;

            countText.text = remaining.ToString();

            // Change color when completed
            if (remaining <= 0)
            {
                countText.color = Color.green;
            }
            else
            {
                countText.color = Color.white;
            }
        }
    }
}