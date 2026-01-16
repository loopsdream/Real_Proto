// Collectible.cs - Collectible item component
using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Collectible Info")]
    public CollectibleType collectibleType = CollectibleType.None;
    public int x;
    public int y;
    public int collectedCount = 0;  // How many times collected (can be collected multiple times)

    [Header("Visual Settings")]
    public SpriteRenderer spriteRenderer;
    public float alphaWhenCollected = 0.5f;  // Transparency after collection

    private Color originalColor;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // Collect this collectible (can be called multiple times)
    public void Collect()
    {
        collectedCount++;
        UpdateVisual();
        Debug.Log($"Collectible {collectibleType} at ({x},{y}) collected. Total: {collectedCount}");
    }

    // Update visual based on collection state
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // Reduce alpha slightly with each collection
        float alpha = Mathf.Max(alphaWhenCollected, 1f - (collectedCount * 0.1f));
        Color newColor = originalColor;
        newColor.a = alpha;
        spriteRenderer.color = newColor;
    }

    // Reset collection state
    public void ResetCollection()
    {
        collectedCount = 0;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    // Check if this collectible has been collected
    public bool HasBeenCollected()
    {
        return collectedCount > 0;
    }
}