// ItemFeedbackUI.cs - UI Feedback System for Item Usage
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ItemFeedbackUI : MonoBehaviour
{
    [Header("Feedback UI References")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public Image feedbackIcon;
    public Button closeButton;

    [Header("Item Icons")]
    public Sprite hammerIcon;
    public Sprite tornadoIcon;
    public Sprite brushIcon;

    [Header("Animation Settings")]
    public float showDuration = 2f;
    public float fadeSpeed = 2f;
    private AnimationCurve showCurve;

    void Awake()
    {
        // Create custom ease-out-back curve
        showCurve = new AnimationCurve();
        showCurve.AddKey(0f, 0f);
        showCurve.AddKey(0.5f, 1.1f);
        showCurve.AddKey(1f, 1f);

        // Set tangents for smooth curve
        for (int i = 0; i < showCurve.keys.Length; i++)
        {
            showCurve.SmoothTangents(i, 0.3f);
        }
    }

    [Header("Feedback Messages")]
    public string hammerInvalidMessage = "Select a colored block to destroy";
    public string tornadoInvalidMessage = "Need at least 2 blocks to shuffle";
    public string brushInvalidMessage = "Select a colored block to change";
    public string noItemsMessage = "No items available";

    private CanvasGroup canvasGroup;
    private ItemManager itemManager;
    private Coroutine currentFeedbackCoroutine;

    void Start()
    {
        // Setup canvas group for fading
        canvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = feedbackPanel.AddComponent<CanvasGroup>();
        }

        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideFeedback);
        }

        // Find ItemManager
        GameObject itemManagerObj = GameObject.Find("ItemManager");
        if (itemManagerObj != null)
        {
            itemManager = itemManagerObj.GetComponent<ItemManager>();
        }

        // Initially hide feedback panel
        feedbackPanel.SetActive(false);

        Debug.Log("ItemFeedbackUI initialized");
    }

    public void ShowInvalidItemUsage(ItemType itemType, string customMessage = null)
    {
        string message = customMessage ?? GetDefaultMessage(itemType);
        Sprite icon = GetItemIcon(itemType);

        ShowFeedback(message, icon, Color.red);
    }

    public void ShowSuccessfulItemUsage(ItemType itemType)
    {
        string message = GetSuccessMessage(itemType);
        Sprite icon = GetItemIcon(itemType);

        ShowFeedback(message, icon, Color.green);
    }

    public void ShowNoItemsAvailable()
    {
        ShowFeedback(noItemsMessage, null, Color.orange);
    }

    public void ShowItemModeActivated(ItemType itemType)
    {
        string message = GetActivationMessage(itemType);
        Sprite icon = GetItemIcon(itemType);

        ShowFeedback(message, icon, Color.white);
    }

    void ShowFeedback(string message, Sprite icon, Color textColor)
    {
        // Stop current feedback if any
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
        }

        // Setup UI elements
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = textColor;
        }

        if (feedbackIcon != null && icon != null)
        {
            feedbackIcon.sprite = icon;
            feedbackIcon.gameObject.SetActive(true);
        }
        else if (feedbackIcon != null)
        {
            feedbackIcon.gameObject.SetActive(false);
        }

        // Start feedback animation
        currentFeedbackCoroutine = StartCoroutine(FeedbackAnimation());
    }

    public void HideFeedback()
    {
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
        }

        StartCoroutine(HideFeedbackAnimation());
    }

    IEnumerator FeedbackAnimation()
    {
        // Show panel
        feedbackPanel.SetActive(true);

        // Fade in animation
        canvasGroup.alpha = 0f;
        feedbackPanel.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            canvasGroup.alpha = progress;
            feedbackPanel.transform.localScale = Vector3.one * showCurve.Evaluate(progress);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        feedbackPanel.transform.localScale = Vector3.one;

        // Wait for show duration
        yield return new WaitForSeconds(showDuration);

        // Fade out animation
        yield return StartCoroutine(HideFeedbackAnimation());
    }

    IEnumerator HideFeedbackAnimation()
    {
        float elapsed = 0f;
        float duration = 0.2f;
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = feedbackPanel.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            canvasGroup.alpha = startAlpha * (1f - progress);
            feedbackPanel.transform.localScale = startScale * (1f - progress * 0.5f);

            yield return null;
        }

        feedbackPanel.SetActive(false);
        currentFeedbackCoroutine = null;
    }

    string GetDefaultMessage(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Hammer:
                return hammerInvalidMessage;
            case ItemType.Tornado:
                return tornadoInvalidMessage;
            case ItemType.Brush:
                return brushInvalidMessage;
            default:
                return "Cannot use item here";
        }
    }

    string GetSuccessMessage(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Hammer:
                return "Block destroyed!";
            case ItemType.Tornado:
                return "Blocks shuffled!";
            case ItemType.Brush:
                return "Block color changed!";
            default:
                return "Item used successfully!";
        }
    }

    string GetActivationMessage(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Hammer:
                return "Tap a colored block to destroy it";
            case ItemType.Tornado:
                return "Tap anywhere to shuffle all blocks";
            case ItemType.Brush:
                return "Tap a colored block to change its color";
            default:
                return "Item selected";
        }
    }

    Sprite GetItemIcon(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Hammer:
                return hammerIcon;
            case ItemType.Tornado:
                return tornadoIcon;
            case ItemType.Brush:
                return brushIcon;
            default:
                return null;
        }
    }
}