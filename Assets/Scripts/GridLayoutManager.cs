using UnityEngine;

public class GridLayoutManager : MonoBehaviour
{
    [Header("Grid Layout Settings")]
    public bool centerGrid = true;
    public Vector2 gridOffset = Vector2.zero;
    public float cellSize = 1.0f;
    
    private Vector2 gridCenterOffset;
    private int currentWidth;
    private int currentHeight;
    
    public void SetupLayout(int width, int height, float newCellSize)
    {
        currentWidth = width;
        currentHeight = height;
        cellSize = newCellSize;
        
        CalculateGridCenterOffset();
        
        Debug.Log($"Grid layout setup: {width}x{height}, cell size: {cellSize}");
    }
    
    public Vector3 GridToWorldPosition(int x, int y)
    {
        Vector3 worldPos = new Vector3(
            x * cellSize + gridCenterOffset.x,
            y * cellSize + gridCenterOffset.y,
            0f
        );
        return worldPos;
    }
    
    public Vector3 GridToWorldPosition(float x, float y)
    {
        Vector3 basePosition = new Vector3(x * cellSize, y * cellSize, 0);
        Vector3 centeredPosition = basePosition + (Vector3)gridCenterOffset;
        return centeredPosition;
    }
    
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float adjustedX = (worldPos.x - gridCenterOffset.x) / cellSize;
        float adjustedY = (worldPos.y - gridCenterOffset.y) / cellSize;
        
        return new Vector2Int(
            Mathf.RoundToInt(adjustedX),
            Mathf.RoundToInt(adjustedY)
        );
    }
    
    public bool IsValidGridPosition(int x, int y)
    {
        return x >= 0 && x < currentWidth && y >= 0 && y < currentHeight;
    }
    
    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return IsValidGridPosition(gridPos.x, gridPos.y);
    }
    
    public Rect GetGridBounds()
    {
        Vector3 bottomLeft = GridToWorldPosition(0, 0) + Vector3.one * (-cellSize * 0.5f);
        Vector3 topRight = GridToWorldPosition(currentWidth - 1, currentHeight - 1) + Vector3.one * (cellSize * 0.5f);
        
        return new Rect(bottomLeft.x, bottomLeft.y,
                       topRight.x - bottomLeft.x,
                       topRight.y - bottomLeft.y);
    }
    
    public Vector3 GetGridCenter()
    {
        return GridToWorldPosition((currentWidth - 1) / 2f, (currentHeight - 1) / 2f);
    }
    
    private void CalculateGridCenterOffset()
    {
        if (!centerGrid)
        {
            gridCenterOffset = gridOffset;
            return;
        }
        
        float gridWorldWidth = (currentWidth - 1) * cellSize;
        float gridWorldHeight = (currentHeight - 1) * cellSize;
        
        float xOffset = -gridWorldWidth * 0.5f;
        float yOffset = -gridWorldHeight * 0.5f;
        
        gridCenterOffset = new Vector2(xOffset, yOffset) + gridOffset;
        
        Debug.Log($"Grid center offset calculated: {gridCenterOffset}");
    }
    
    public void UpdateCellSize(float newCellSize)
    {
        cellSize = newCellSize;
        CalculateGridCenterOffset();
        
        Debug.Log($"Cell size updated to: {cellSize}");
    }
    
    public Vector3 GetUIPosition(UIPosition position)
    {
        Vector3 gridCenter = GetGridCenter();
        float gridWorldWidth = currentWidth * cellSize;
        float gridWorldHeight = currentHeight * cellSize;
        
        switch (position)
        {
            case UIPosition.TopCenter:
                return new Vector3(gridCenter.x, gridCenter.y + gridWorldHeight * 0.6f, 0);
            case UIPosition.BottomCenter:
                return new Vector3(gridCenter.x, gridCenter.y - gridWorldHeight * 0.6f, 0);
            case UIPosition.TopLeft:
                return new Vector3(gridCenter.x - gridWorldWidth * 0.4f, gridCenter.y + gridWorldHeight * 0.6f, 0);
            case UIPosition.TopRight:
                return new Vector3(gridCenter.x + gridWorldWidth * 0.4f, gridCenter.y + gridWorldHeight * 0.6f, 0);
            case UIPosition.BottomLeft:
                return new Vector3(gridCenter.x - gridWorldWidth * 0.4f, gridCenter.y - gridWorldHeight * 0.6f, 0);
            case UIPosition.BottomRight:
                return new Vector3(gridCenter.x + gridWorldWidth * 0.4f, gridCenter.y - gridWorldHeight * 0.6f, 0);
            default:
                return gridCenter;
        }
    }
    
    public bool CheckCenterAlignment()
    {
        Rect gridBounds = GetGridBounds();
        float gridCenterX = gridBounds.x + gridBounds.width * 0.5f;
        float screenCenterX = 0f;
        
        float difference = Mathf.Abs(gridCenterX - screenCenterX);
        bool isAligned = difference < 0.1f;
        
        if (!isAligned)
        {
            Debug.LogWarning($"Grid is not centered! Difference: {difference}");
        }
        else
        {
            Debug.Log("Grid is properly centered.");
        }
        
        return isAligned;
    }
    
    public GridLayoutInfo GetLayoutInfo()
    {
        return new GridLayoutInfo
        {
            width = currentWidth,
            height = currentHeight,
            cellSize = cellSize,
            centerOffset = gridCenterOffset,
            gridCenter = GetGridCenter(),
            bounds = GetGridBounds()
        };
    }
}

public enum UIPosition
{
    TopCenter,
    TopLeft,
    TopRight,
    BottomCenter,
    BottomLeft,
    BottomRight
}

[System.Serializable]
public struct GridLayoutInfo
{
    public int width;
    public int height;
    public float cellSize;
    public Vector2 centerOffset;
    public Vector3 gridCenter;
    public Rect bounds;
}