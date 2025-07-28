using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // 새 Input System 네임스페이스

public class BlockInteraction : MonoBehaviour
{
    public BaseGridManager gridManager;
    private Block block;
    private Camera mainCamera;

    [Header("Touch Settings")]
    public float touchAreaMultiplier = 1.2f; // 터치 영역을 20% 확대

    void Start()
    {
        // 콜라이더 크기를 터치하기 쉽게 확대
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size *= touchAreaMultiplier;
        }

        block = GetComponent<Block>();
        if (gridManager == null)
        {
            gridManager = Object.FindAnyObjectByType<BaseGridManager>();
        }
        mainCamera = Camera.main;
    }

    void Update()
    {
        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        // 모바일 터치 입력 확인 (우선순위 1)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                inputDetected = true;
                inputPosition = touch.position;
                Debug.Log("Touch detected at: " + inputPosition);
            }
        }
        // 마우스 클릭 확인 (에디터/PC용)
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
            inputPosition = Input.mousePosition;
            Debug.Log("Mouse click detected at: " + inputPosition);
        }

        if (inputDetected)
        {
            // UI 요소 위에 있는지 확인
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Input over UI - ignoring");
                return;
            }

            // 모바일에서는 터치 ID도 확인
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                Debug.Log("Touch over UI - ignoring");
                return;
            }

            // 월드 포지션으로 변환
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(inputPosition);
            Debug.Log("World position: " + worldPos);

            // 오브젝트와 충돌 확인
            Collider2D hitCollider = Physics2D.OverlapPoint(worldPos);
            Debug.Log("Hit collider: " + (hitCollider ? hitCollider.name : "none"));

            if (hitCollider != null && hitCollider.gameObject == gameObject)
            {
                if (block != null && block.isEmpty && gridManager != null)
                {
                    Debug.Log($"Block clicked at grid position ({block.x}, {block.y})");
                    gridManager.OnEmptyBlockClicked(block.x, block.y);
                }
                else
                {
                    Debug.Log($"Click invalid - Block: {(block != null)}, IsEmpty: {(block != null ? block.isEmpty : false)}, GridManager: {(gridManager != null)}");
                }
            }
        }
    }
}