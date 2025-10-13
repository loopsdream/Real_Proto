// BlockHighlightSystem.cs - Block Visual Feedback System for Item Usage
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class BlockHighlightSystem : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Color hammerHighlightColor = Color.red;
    public Color tornadoHighlightColor = Color.cyan;
    public Color brushHighlightColor = Color.yellow;
    public Color invalidBlockColor = Color.gray;

    [Header("Animation Settings")]
    public float highlightIntensity = 0.7f;
    public float pulseSpeed = 2f;
    public float fadeSpeed = 1f;

    [Header("Visual Effects")]
    public GameObject highlightBorderPrefab;
    public float borderThickness = 0.1f;

    private Dictionary<GameObject, HighlightData> highlightedBlocks = new Dictionary<GameObject, HighlightData>();
    private ItemType? currentItemType = null;
    private StageGridManager stageGridManager;
    private ItemManager itemManager;

    private class HighlightData
    {
        public SpriteRenderer spriteRenderer;
        public Color originalColor;
        public Color targetColor;
        public GameObject borderEffect;
        public bool isValid;
        public Coroutine animationCoroutine;
    }

    void Start()
    {
        // Find required managers
        GameObject stageGridObj = GameObject.Find("StageGridManager");
        if (stageGridObj != null)
        {
            stageGridManager = stageGridObj.GetComponent<StageGridManager>();
        }

        GameObject itemManagerObj = GameObject.Find("ItemManager");
        if (itemManagerObj != null)
        {
            itemManager = itemManagerObj.GetComponent<ItemManager>();
        }

        // Subscribe to item mode events
        if (itemManager != null)
        {
            GameObject itemUIObj = GameObject.Find("ItemUIManager");
            if (itemUIObj != null)
            {
                ItemUIManager itemUI = itemUIObj.GetComponent<ItemUIManager>();
                if (itemUI != null)
                {
                    itemUI.OnItemModeActivated += OnItemModeActivated;
                    itemUI.OnItemModeDeactivated += OnItemModeDeactivated;
                }
            }
        }

        Debug.Log("BlockHighlightSystem initialized");
    }

    void OnDestroy()
    {
        // Unsubscribe events
        GameObject itemUIObj = GameObject.Find("ItemUIManager");
        if (itemUIObj != null)
        {
            ItemUIManager itemUI = itemUIObj.GetComponent<ItemUIManager>();
            if (itemUI != null)
            {
                itemUI.OnItemModeActivated -= OnItemModeActivated;
                itemUI.OnItemModeDeactivated -= OnItemModeDeactivated;
            }
        }

        ClearAllHighlights();
    }

    void OnItemModeActivated(ItemType itemType)
    {
        currentItemType = itemType;
        StartHighlighting();
        Debug.Log("Highlight system activated for: " + itemType.ToString());
    }

    void OnItemModeDeactivated()
    {
        currentItemType = null;
        StopHighlighting();
        Debug.Log("Highlight system deactivated");
    }

    void StartHighlighting()
    {
        if (!currentItemType.HasValue || stageGridManager == null)
            return;

        ClearAllHighlights();

        List<Block> allBlocks = stageGridManager.GetAllBlocks();

        foreach (Block block in allBlocks)
        {
            if (block == null || block.gameObject == null) continue;

            bool canUseItem = CanUseItemOnBlock(block, currentItemType.Value);
            Color highlightColor = GetHighlightColor(currentItemType.Value, canUseItem);

            AddHighlight(block.gameObject, highlightColor, canUseItem);
        }
    }

    void StopHighlighting()
    {
        ClearAllHighlights();
    }

    bool CanUseItemOnBlock(Block block, ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Hammer:
                // Hammer can destroy non-empty blocks
                return block != null && !block.isEmpty;

            case ItemType.Tornado:
                // Tornado affects all non-empty blocks
                return block != null && !block.isEmpty;

            case ItemType.Brush:
                // Brush can change color of non-empty blocks
                return block != null && !block.isEmpty;

            default:
                return false;
        }
    }

    Color GetHighlightColor(ItemType itemType, bool canUse)
    {
        if (!canUse)
            return invalidBlockColor;

        switch (itemType)
        {
            case ItemType.Hammer:
                return hammerHighlightColor;
            case ItemType.Tornado:
                return tornadoHighlightColor;
            case ItemType.Brush:
                return brushHighlightColor;
            default:
                return Color.white;
        }
    }

    void AddHighlight(GameObject blockObj, Color highlightColor, bool isValid)
    {
        if (blockObj == null) return;

        SpriteRenderer renderer = blockObj.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        HighlightData data = new HighlightData
        {
            spriteRenderer = renderer,
            originalColor = renderer.color,
            targetColor = Color.Lerp(renderer.color, highlightColor, highlightIntensity),
            isValid = isValid
        };

        // Create border effect
        if (highlightBorderPrefab != null && isValid)
        {
            data.borderEffect = CreateBorderEffect(blockObj, highlightColor);
        }

        // Start animation
        data.animationCoroutine = StartCoroutine(AnimateHighlight(data));

        highlightedBlocks[blockObj] = data;
    }

    GameObject CreateBorderEffect(GameObject blockObj, Color borderColor)
    {
        GameObject border = Instantiate(highlightBorderPrefab, blockObj.transform);
        border.transform.localPosition = Vector3.zero;
        border.transform.localScale = Vector3.one * (1f + borderThickness);

        SpriteRenderer borderRenderer = border.GetComponent<SpriteRenderer>();
        if (borderRenderer != null)
        {
            borderRenderer.color = borderColor;
            borderRenderer.sortingOrder = -1; // Behind the main sprite
        }

        return border;
    }

    IEnumerator AnimateHighlight(HighlightData data)
    {
        if (data.spriteRenderer == null) yield break;

        while (highlightedBlocks.ContainsValue(data))
        {
            if (data.isValid)
            {
                // Pulse animation for valid blocks
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
                Color currentColor = Color.Lerp(data.originalColor, data.targetColor, pulse);
                data.spriteRenderer.color = currentColor;
            }
            else
            {
                // Static dim effect for invalid blocks
                data.spriteRenderer.color = Color.Lerp(data.originalColor, data.targetColor, 0.3f);
            }

            yield return null;
        }

        // Fade back to original color
        float fadeTimer = 0f;
        Color startColor = data.spriteRenderer.color;

        while (fadeTimer < 1f)
        {
            fadeTimer += Time.deltaTime * fadeSpeed;
            data.spriteRenderer.color = Color.Lerp(startColor, data.originalColor, fadeTimer);
            yield return null;
        }

        data.spriteRenderer.color = data.originalColor;
    }

    void ClearAllHighlights()
    {
        foreach (var kvp in highlightedBlocks)
        {
            HighlightData data = kvp.Value;

            // Stop animation
            if (data.animationCoroutine != null)
            {
                StopCoroutine(data.animationCoroutine);
            }

            // Restore original color
            if (data.spriteRenderer != null)
            {
                data.spriteRenderer.color = data.originalColor;
            }

            // Destroy border effect
            if (data.borderEffect != null)
            {
                Destroy(data.borderEffect);
            }
        }

        highlightedBlocks.Clear();
    }

    // Public method to manually highlight specific block (for hover effects)
    public void HighlightBlock(GameObject blockObj, bool highlight)
    {
        if (!currentItemType.HasValue) return;

        if (highlight && highlightedBlocks.ContainsKey(blockObj))
        {
            // Enhance existing highlight
            HighlightData data = highlightedBlocks[blockObj];
            if (data.isValid)
            {
                // Add extra glow or scale effect
                blockObj.transform.localScale = Vector3.one * 1.1f;
            }
        }
        else if (!highlight && highlightedBlocks.ContainsKey(blockObj))
        {
            // Remove enhancement
            blockObj.transform.localScale = Vector3.one;
        }
    }

    // Check if block can be used with current item
    public bool IsBlockUsableWithCurrentItem(GameObject blockObj)
    {
        if (!currentItemType.HasValue) return false;

        Block block = blockObj.GetComponent<Block>();
        if (block == null) return false;

        return CanUseItemOnBlock(block, currentItemType.Value);
    }
}