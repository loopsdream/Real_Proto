// InfiniteBlock.cs - 무한모드 블록의 이동 방향을 저장하는 컴포넌트
using UnityEngine;

public class InfiniteBlock : MonoBehaviour
{
    [Header("Movement Direction")]
    public Vector2Int moveDirection = Vector2Int.zero;

    [Header("Debug Info")]
    public bool showDebugInfo = false;

    void OnDrawGizmosSelected()
    {
        if (showDebugInfo && moveDirection != Vector2Int.zero)
        {
            // 이동 방향을 화살표로 표시
            Gizmos.color = Color.green;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + new Vector3(moveDirection.x, moveDirection.y, 0) * 0.5f;

            Gizmos.DrawLine(startPos, endPos);

            // 화살표 끝부분
            Vector3 arrowHead1 = endPos + new Vector3(-moveDirection.x * 0.1f, -moveDirection.y * 0.1f, 0);
            Vector3 arrowHead2 = endPos + new Vector3(moveDirection.y * 0.1f, -moveDirection.x * 0.1f, 0);

            Gizmos.DrawLine(endPos, arrowHead1);
            Gizmos.DrawLine(endPos, arrowHead2);
        }
    }

    // 중앙에 도달했는지 확인
    public bool HasReachedCenter(int gridWidth, int gridHeight)
    {
        Block blockComponent = GetComponent<Block>();
        if (blockComponent == null) return false;

        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        return blockComponent.x == centerX && blockComponent.y == centerY;
    }

    // 이동 방향 정보를 문자열로 반환 (디버깅용)
    public string GetDirectionString()
    {
        if (moveDirection == Vector2Int.up) return "↑";
        if (moveDirection == Vector2Int.down) return "↓";
        if (moveDirection == Vector2Int.left) return "←";
        if (moveDirection == Vector2Int.right) return "→";
        return "●"; // 정지
    }
}