using UnityEngine;
using UnityEngine.InputSystem; // �� Input System ���ӽ����̽�

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
        // �� Input System���� ���콺 Ŭ�� ����
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // ���콺 ��ġ ��������
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

            // ����ĳ��Ʈ�� �浹 Ȯ��
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            // ���� ������Ʈ�� Ŭ���Ǿ����� Ȯ��
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