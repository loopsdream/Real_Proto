using UnityEngine;
using UnityEngine.EventSystems;

public class BlockInteraction : MonoBehaviour
{
    private Block block;
    private Camera mainCamera;
    private bool isProcessing = false; // 중복 처리 방지

    [Header("Touch Settings")]
    public float touchAreaMultiplier = 1.2f;

    [Header("Item System")]
    private ItemManager itemManager;
    private BlockHighlightSystem highlightSystem;

    [Header("Visual Feedback")]
    public float hoverScaleMultiplier = 1.1f;
    private Vector3 originalScale;
    private bool isHovered = false;

    void Start()
    {
        Debug.Log($"[BlockInteraction] Start() - {gameObject.name}");

        // 터치 영역 1.2배 확대
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size *= touchAreaMultiplier;
        }

        block = GetComponent<Block>();

        // Find BlockHighlightSystem
        GameObject highlightSystemObj = GameObject.Find("BlockHighlightSystem");
        if (highlightSystemObj != null)
        {
            highlightSystem = highlightSystemObj.GetComponent<BlockHighlightSystem>();
        }
        
        // Store original scale
        originalScale = transform.localScale;

        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    void OnMouseDown()
    {
        // 이미 처리 중이면 무시
        if (isProcessing) return;

        // UI 터치 체크
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 모바일 터치에서 UI 체크
        if (Input.touchCount > 0)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;
        }

        HandleBlockTouch();
    }

void HandleBlockTouch()
    {
        if (block == null) return;

        isProcessing = true;

        Debug.Log($"[BlockTouch] Block ({block.x}, {block.y}) clicked");

        // 현재 활성화된 GridManager 찾기
        BaseGridManager activeGridManager = FindActiveGridManager();

        if (activeGridManager == null)
        {
            Debug.LogError("[BlockTouch] No active GridManager found!");
            isProcessing = false;
            return;
        }

        if (activeGridManager is StageGridManager)
        {
            // 아이템 매니저 찾기 및 아이템 모드 체크
            ItemManager itemManager = Object.FindAnyObjectByType<ItemManager>();
            if (itemManager != null && itemManager.IsItemModeActive())
            {
                ItemType? selectedItem = itemManager.GetActiveItemType();
                if (selectedItem.HasValue)
                {
                    Debug.Log($"[ItemMode] Using {selectedItem.Value} on block ({block.x}, {block.y})");
                    HandleItemUse(selectedItem.Value, itemManager);
                    isProcessing = false;
                    return;
                }
            }
        }

        // 일반 블록 클릭 처리 (빈 블록만)
        if (block.isEmpty)
        {
            activeGridManager.OnEmptyBlockClicked(block.x, block.y);
        }

        isProcessing = false;
    }

    void HandleItemUse(ItemType itemType, ItemManager itemManager)
    {
        Debug.Log($"[ItemUse] Type: {itemType} at ({block.x}, {block.y})");

        if (itemManager != null && block != null)
        {
            Vector2Int gridPos = new Vector2Int(block.x, block.y);
            bool success = itemManager.TryUseItemOnBlock(gridPos);

            if (success)
            {
                Debug.Log($"[SUCCESS] Item used at ({block.x}, {block.y})");

                // 모바일 햅틱 피드백
                if (Application.isMobilePlatform)
                {
                    Handheld.Vibrate();
                }
            }
            else
            {
                Debug.Log($"[FAILED] Cannot use item at ({block.x}, {block.y})");
            }
        }
    }

    BaseGridManager FindActiveGridManager()
    {
        // GameManager GameObject에서 찾기 (스테이지 모드)
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager != null)
        {
            StageGridManager stageGrid = gameManager.GetComponent<StageGridManager>();
            if (stageGrid != null && stageGrid.enabled)
            {
                Debug.Log("[BlockTouch] Found StageGridManager on GameManager");
                return stageGrid;
            }
        }

        // InfiniteGridManager 찾기
        InfiniteGridManager infiniteGrid = Object.FindAnyObjectByType<InfiniteGridManager>();
        if (infiniteGrid != null && infiniteGrid.enabled)
        {
            Debug.Log("[BlockTouch] Found InfiniteGridManager");
            return infiniteGrid;
        }

        // 기타 BaseGridManager
        BaseGridManager baseGrid = Object.FindAnyObjectByType<BaseGridManager>();
        if (baseGrid != null && baseGrid.enabled)
        {
            Debug.Log("[BlockTouch] Found BaseGridManager");
            return baseGrid;
        }

        return null;
    }

    // Visual feedback methods for item mode
    void OnMouseEnter()
    {
        if (!isHovered)
        {
            isHovered = true;
            ShowHoverEffect();
        }
    }

    void OnMouseExit()
    {
        if (isHovered)
        {
            isHovered = false;
            HideHoverEffect();
        }
    }

    void ShowHoverEffect()
    {
        // Scale effect
        transform.localScale = originalScale * hoverScaleMultiplier;

        // Highlight system integration
        if (highlightSystem != null)
        {
            highlightSystem.HighlightBlock(gameObject, true);
        }
    }

    void HideHoverEffect()
    {
        // Reset scale
        transform.localScale = originalScale;

        // Highlight system integration
        if (highlightSystem != null)
        {
            highlightSystem.HighlightBlock(gameObject, false);
        }
    }

    void PlayTouchFeedback()
    {
        // Simple scale animation for touch feedback
        StartCoroutine(TouchFeedbackAnimation());
    }

    void PlayItemUsageFeedback(bool success)
    {
        if (success)
        {
            // Success feedback - green flash
            StartCoroutine(ItemUsageFeedbackAnimation(Color.green));
        }
        else
        {
            // Failure feedback - red flash
            StartCoroutine(ItemUsageFeedbackAnimation(Color.red));
        }
    }

    void ShowInvalidUsageFeedback()
    {
        // Shake effect for invalid usage
        StartCoroutine(InvalidUsageShakeAnimation());

        Debug.Log("Cannot use current item on this block");
    }

    // Animation coroutines
    System.Collections.IEnumerator TouchFeedbackAnimation()
    {
        Vector3 targetScale = originalScale * 0.9f;
        float duration = 0.1f;
        float elapsed = 0f;

        // Scale down
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }

        // Scale back up
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    System.Collections.IEnumerator ItemUsageFeedbackAnimation(Color flashColor)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;

        Color originalColor = renderer.color;
        float duration = 0.3f;
        float elapsed = 0f;

        // Flash to feedback color
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);
            renderer.color = Color.Lerp(originalColor, flashColor, progress * 0.5f);
            yield return null;
        }

        // Flash back to original
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);
            renderer.color = Color.Lerp(Color.Lerp(originalColor, flashColor, 0.5f), originalColor, progress);
            yield return null;
        }

        renderer.color = originalColor;
    }

    System.Collections.IEnumerator InvalidUsageShakeAnimation()
    {
        Vector3 originalPosition = transform.position;
        float shakeIntensity = 0.1f;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float currentIntensity = shakeIntensity * (1f - progress);

            Vector3 randomOffset = new Vector3(
                Random.Range(-currentIntensity, currentIntensity),
                Random.Range(-currentIntensity, currentIntensity),
                0f
            );

            transform.position = originalPosition + randomOffset;
            yield return null;
        }

        transform.position = originalPosition;
    }
 }