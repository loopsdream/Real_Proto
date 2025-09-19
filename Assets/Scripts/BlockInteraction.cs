using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // �� Input System ���ӽ����̽�

public class BlockInteraction : MonoBehaviour
{
    public BaseGridManager gridManager;
    private Block block;
    private Camera mainCamera;

    [Header("Touch Settings")]
    public float touchAreaMultiplier = 1.2f; // ��ġ ������ 20% Ȯ��

void Start()
    {
        // 터치 영역 1.2배 확대
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size *= touchAreaMultiplier;
        }

        block = GetComponent<Block>();
        
        // gridManager 우선순위 검색
        if (gridManager == null)
        {
            // 1순위: 현재 씬의 StageGridManager
            gridManager = Object.FindAnyObjectByType<StageGridManager>();
            
            // 2순위: InfiniteGridManager (무한모드)
            if (gridManager == null)
            {
                gridManager = Object.FindAnyObjectByType<InfiniteGridManager>();
            }
            
            // 3순위: 기본 BaseGridManager (폴백)
            if (gridManager == null)
            {
                gridManager = Object.FindAnyObjectByType<BaseGridManager>();
            }
            
            // 디버그 로그 추가
            if (gridManager != null)
            {
                Debug.Log($"BlockInteraction found gridManager: {gridManager.GetType().Name}");
            }
            else
            {
                Debug.LogWarning("BlockInteraction: No gridManager found!");
            }
        }
        
        mainCamera = Camera.main;
    }

    void Update()
    {
        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        // ����� ��ġ �Է� Ȯ�� (�켱���� 1)
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
        // ���콺 Ŭ�� Ȯ�� (������/PC��)
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
            inputPosition = Input.mousePosition;
            Debug.Log("Mouse click detected at: " + inputPosition);
        }

        if (inputDetected)
        {
            // UI ��� ���� �ִ��� Ȯ��
            //if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            //{
            //    Debug.Log("Input over UI - ignoring");
            //    return;
            //}

            // ����Ͽ����� ��ġ ID�� Ȯ��
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                Debug.Log("Touch over UI - ignoring");
                return;
            }

            // ���� ���������� ��ȯ
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(inputPosition);
            Debug.Log("World position: " + worldPos);

            // ������Ʈ�� �浹 Ȯ��
            Collider2D hitCollider = Physics2D.OverlapPoint(worldPos);
            Debug.Log("Hit collider: " + (hitCollider ? hitCollider.name : "none"));

            if (hitCollider != null)
            {
                Debug.Log("hitCollider != null");

                if (hitCollider.gameObject == gameObject)
                {
                    Debug.Log("hitCollider.gameObject == gameObject");
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
                else
                {
                    Debug.Log("hitCollider.gameObject != gameObject");
                }
            }
            else
            {
                Debug.Log("hitCollider == null");
            }


            //if (hitCollider != null && hitCollider.gameObject == gameObject)
            //{
            //    if (block != null && block.isEmpty && gridManager != null)
            //    {
            //        Debug.Log($"Block clicked at grid position ({block.x}, {block.y})");
            //        gridManager.OnEmptyBlockClicked(block.x, block.y);
            //    }
            //    else
            //    {
            //        Debug.Log($"Click invalid - Block: {(block != null)}, IsEmpty: {(block != null ? block.isEmpty : false)}, GridManager: {(gridManager != null)}");
            //    }
            //}
        }
    }
}