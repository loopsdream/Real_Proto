using UnityEngine;
using UnityEngine.InputSystem; // 새 Input System 네임스페이스

public class BlockInteraction : MonoBehaviour
{
    public GridManager gridManager;
    private Block block;
    private Camera mainCamera;

    void Start()
    {
        block = GetComponent<Block>();
        if (gridManager == null)
        {
            gridManager = Object.FindAnyObjectByType<GridManager>();
        }
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 새 Input System에서 마우스 클릭 감지
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 마우스 위치 가져오기
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

            // 레이캐스트로 충돌 확인
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            // 현재 오브젝트가 클릭되었는지 확인
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (block != null && block.isEmpty && gridManager != null)
                {
                    Debug.Log($"Block clicked at ({block.x}, {block.y})");
                    gridManager.OnEmptyBlockClicked(block.x, block.y);
                }
            }
        }
    }
}