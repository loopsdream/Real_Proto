using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TouchPhase = UnityEngine.TouchPhase;

public class BlockInteraction : MonoBehaviour
{
    public BaseGridManager gridManager;
    private Block block;
    private Camera mainCamera;
    private static BlockInteraction inputHandler;
    private static bool isSceneListenerRegistered = false;

    [Header("Touch Settings")]
    public float touchAreaMultiplier = 1.2f;

    [Header("Item System")]
    private ItemManager itemManager;
    private BlockHighlightSystem highlightSystem;

    [Header("Visual Feedback")]
    public float hoverScaleMultiplier = 1.1f;
    private Vector3 originalScale;
    private bool isHovered = false;

    public static void ResetInputHandler()
    {
        Debug.Log("[BlockInteraction] Force reset inputHandler!");
        inputHandler = null;
    }

    void Awake()
    {
        // 씬 로드 이벤트 리스너 등록 (한 번만)
        if (!isSceneListenerRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isSceneListenerRegistered = true;
            Debug.Log("[BlockInteraction] Scene listener registered");
        }
    }

    // 씬이 로드될 때마다 호출
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[BlockInteraction] Scene loaded: {scene.name} - Resetting inputHandler");
        inputHandler = null;
    }

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

        FindGridManager();

        // Find ItemManager
        GameObject itemManagerObj = GameObject.Find("ItemManager");
        if (itemManagerObj != null)
        {
            itemManager = itemManagerObj.GetComponent<ItemManager>();
            Debug.Log("ItemManager found in BlockInteraction");
        }
        else
        {
            Debug.LogWarning("ItemManager not found in BlockInteraction");
        }

        // Find BlockHighlightSystem
        GameObject highlightSystemObj = GameObject.Find("BlockHighlightSystem");
        if (highlightSystemObj != null)
        {
            highlightSystem = highlightSystemObj.GetComponent<BlockHighlightSystem>();
        }
        
        // Store original scale
        originalScale = transform.localScale;

        mainCamera = Camera.main;

        // inputHandler 할당 로직
        Debug.Log($"[BlockInteraction] Current inputHandler: {(inputHandler != null ? inputHandler.name : "NULL")}");

        if (inputHandler == null)
        {
            inputHandler = this;
            Debug.Log($"[BlockInteraction] ⭐ NEW Input handler assigned to: {gameObject.name}");
        }
        else
        {
            // 기존 inputHandler가 유효한지 확인
            bool isValid = false;
            try
            {
                isValid = (inputHandler.gameObject != null && inputHandler.gameObject.activeInHierarchy);
            }
            catch
            {
                isValid = false;
            }

            if (!isValid)
            {
                Debug.Log($"[BlockInteraction] Old inputHandler is INVALID - reassigning to: {gameObject.name}");
                inputHandler = this;
            }
            else
            {
                Debug.Log($"[BlockInteraction] inputHandler already exists: {inputHandler.gameObject.name}");
            }
        }
    }

    // ⭐ 새로운 메서드: gridManager 찾기
    private void FindGridManager()
    {
        Debug.Log("[BlockInteraction] Finding gridManager...");

        // ⭐ 1순위: 싱글톤 인스턴스 사용
        if (StageGridManager.Instance != null)
        {
            gridManager = StageGridManager.Instance;
            Debug.Log($"[BlockInteraction] ✅ Using StageGridManager.Instance: {gridManager.gameObject.name} (ID: {gridManager.GetInstanceID()})");
            return;
        }

        // 2순위: FindAnyObjectByType (싱글톤 실패 시)
        gridManager = Object.FindAnyObjectByType<StageGridManager>();
        if (gridManager != null)
        {
            Debug.Log($"[BlockInteraction] ✅ Found StageGridManager: {gridManager.gameObject.name} (ID: {gridManager.GetInstanceID()})");
            return;
        }

        // 3순위: InfiniteGridManager (무한모드)
        gridManager = Object.FindAnyObjectByType<InfiniteGridManager>();
        if (gridManager != null)
        {
            Debug.Log($"[BlockInteraction] ✅ Found InfiniteGridManager: {gridManager.gameObject.name}");
            return;
        }

        Debug.LogError("[BlockInteraction] ❌ gridManager NOT FOUND!");
    }

    void OnDestroy()
    {
        // 이 인스턴스가 inputHandler일 때만 초기화
        if (inputHandler == this)
        {
            inputHandler = null;
            Debug.Log($"[BlockInteraction] Input handler cleared: {gameObject.name}");
        }
    }

    void Update()
    {
        // 디버깅: inputHandler 확인 (터치가 있을 때만)
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began))
        {
            Debug.Log($"[Update] Input detected! inputHandler == this: {inputHandler == this}, this: {gameObject.name}, inputHandler: {(inputHandler != null ? inputHandler.gameObject.name : "NULL")}");
        }

        // 입력 처리 담당이 아니면 아무것도 안 함
        if (inputHandler != this)
        {
            return;
        }

        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        // 터치 입력 확인
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                inputDetected = true;
                inputPosition = touch.position;
                Debug.Log($"[Input] Touch detected at: {inputPosition}");
            }
        }
        // 마우스 클릭 확인 (에디터/PC용)
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
            inputPosition = Input.mousePosition;
            Debug.Log($"[Input] Mouse click detected at: {inputPosition}");
        }

        if (inputDetected)
        {
            Debug.Log("[Input] Input was detected, processing...");

            // 터치에서만 UI 체크
            if (Input.touchCount > 0 && EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                Debug.Log("[Input] Touch over UI - ignoring");
                return;
            }

            // Ray를 사용해서 터치된 콜라이더 직접 찾기
            Ray ray = mainCamera.ScreenPointToRay(inputPosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            Debug.Log($"[Input] Raycast hit: {(hit.collider != null ? hit.collider.gameObject.name : "NOTHING")}");

            if (hit.collider != null)
            {
                Debug.Log($"[HIT] Collider touched: {hit.collider.gameObject.name}");

                // 터치된 블록의 BlockInteraction 컴포넌트 찾기
                BlockInteraction touchedBlock = hit.collider.GetComponent<BlockInteraction>();
                if (touchedBlock != null)
                {
                    Debug.Log($"[HIT] Block at ({touchedBlock.block.x}, {touchedBlock.block.y}) was touched");
                    touchedBlock.HandleBlockTouch();
                }
                else
                {
                    Debug.Log("[HIT] No BlockInteraction component found");
                }
            }
            else
            {
                Debug.Log("[Input] Raycast hit NOTHING!");
            }
        }
    }

void HandleBlockTouch()
    {
        Debug.Log($"[HandleBlockTouch] Block ({block.x}, {block.y}) processing...");

        // gridManager가 null이면 다시 찾기
        if (gridManager == null)
        {
            Debug.LogWarning("[HandleBlockTouch] gridManager is NULL! Trying to find it...");
            FindGridManager();
        }

        // 인스턴스 ID 확인
        if (gridManager != null)
        {
            Debug.Log($"[HandleBlockTouch] gridManager instance ID: {gridManager.GetInstanceID()}");
            Debug.Log($"[HandleBlockTouch] gridManager GameObject: {gridManager.gameObject.name}");
        }

        Debug.Log($"[HandleBlockTouch] gridManager: {(gridManager != null ? gridManager.GetType().Name : "NULL")}");

        // 아이템 매니저 찾기 및 아이템 모드 체크
        ItemManager itemManager = Object.FindAnyObjectByType<ItemManager>();
        if (itemManager != null && itemManager.IsItemModeActive())
        {
            ItemType? selectedItem = itemManager.GetActiveItemType();
            if (selectedItem.HasValue)
            {
                Debug.Log($"[ItemMode] Using {selectedItem.Value} on block ({block.x}, {block.y})");
                HandleItemUse(selectedItem.Value, itemManager);
                return;
            }
        }

        // 일반 블록 터치 처리 (빈 블록만)
        if (block != null && block.isEmpty && gridManager != null)
        {
            Debug.Log($"[EmptyBlock] Calling gridManager.OnEmptyBlockClicked({block.x}, {block.y})");

            // ⭐ matchingSystem 체크
            if (gridManager.matchingSystem == null)
            {
                Debug.LogError("[EmptyBlock] gridManager.matchingSystem is NULL!");
            }
            else
            {
                Debug.Log($"[EmptyBlock] matchingSystem exists: {gridManager.matchingSystem.GetType().Name}");
            }

            gridManager.OnEmptyBlockClicked(block.x, block.y);

            Debug.Log("[EmptyBlock] OnEmptyBlockClicked() returned");
        }
        else
        {
            Debug.LogError($"[BlockTouch] Cannot process - block: {block != null}, isEmpty: {block?.isEmpty}, gridManager: {gridManager != null}");
        }
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

    Vector2Int GetGridPosition()
    {
        if (block != null)
        {
            return new Vector2Int(block.x, block.y);
        }

        // Fallback: calculate from transform position
        if (gridManager != null)
        {
            // This needs to be implemented based on your specific grid layout
            Vector3 worldPos = transform.position;

            // Convert world position to grid coordinates
            // This is a placeholder - you'll need to implement the actual conversion
            int gridX = Mathf.RoundToInt(worldPos.x);
            int gridY = Mathf.RoundToInt(worldPos.y);

            return new Vector2Int(gridX, gridY);
        }

        return Vector2Int.zero;
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

        //void ShowItemHighlight()
        //{
        //    // Add visual feedback for item usage
        //    SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        //    if (renderer != null)
        //    {
        //        Color highlightColor = Color.white;
        //        ItemType? activeItem = itemManager.GetActiveItemType();

        //        // Different colors for different items
        //        if (activeItem.HasValue)
        //        {
        //            switch (activeItem.Value)
        //            {
        //                case ItemType.Hammer:
        //                    highlightColor = Color.red;
        //                    break;
        //                case ItemType.Tornado:
        //                    highlightColor = Color.cyan;
        //                    break;
        //                case ItemType.Brush:
        //                    highlightColor = Color.yellow;
        //                    break;
        //            }
        //        }

        //        renderer.color = Color.Lerp(renderer.color, highlightColor, 0.5f);
        //    }
        //}

        //void HideItemHighlight()
        //{
        //    // Remove highlight
        //    SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        //    if (renderer != null)
        //    {
        //        renderer.color = Color.white;
        //    }
        //}
    }