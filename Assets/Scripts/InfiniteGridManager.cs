using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteGridManager : BaseGridManager
{
    [System.NonSerialized]
    public System.Action<int, int> onEmptyBlockClicked;

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        // ���� ���� InfiniteModeManager�� �ʱ�ȭ
        Debug.Log("InfiniteGridManager ready");
    }

    public void InitializeInfiniteGrid()
    {
        SetupGrid();
        CreateEmptyGrid();
        SetupCameraAndLayout();
    }

    private void CreateEmptyGrid()
    {
        if (blockFactory == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = blockFactory.CreateEmptyBlock(x, y);
            }
        }
    }

    public override void OnEmptyBlockClicked(int x, int y)
    {
        // ����� �α� �߰�
        Debug.Log($"Infinite Mode: OnEmptyBlockClicked called - x:{x}, y:{y}");
        Debug.Log($"Grid status - grid:{(grid != null)}, width:{width}, height:{height}");

        if (grid == null)
        {
            Debug.LogError("Grid is null!");
            return;
        }

        Debug.Log($"Grid dimensions: {grid.GetLength(0)}x{grid.GetLength(1)}");

        if (x < 0 || x >= grid.GetLength(0) || y < 0 || y >= grid.GetLength(1))
        {
            Debug.LogWarning($"Invalid click position: ({x}, {y}) - Grid size: {grid.GetLength(0)}x{grid.GetLength(1)}");
            return;
        }

        Debug.Log($"Infinite Mode: Valid empty block clicked at ({x}, {y})");

        // ���Ѹ��� InfiniteModeManager�� ����
        if (onEmptyBlockClicked != null)
        {
            onEmptyBlockClicked(x, y);
        }
        else
        {
            Debug.LogWarning("onEmptyBlockClicked delegate is not set!");
        }
    }

    protected override void ProcessMatchedBlocks(List<GameObject> matchedBlocks)
    {
        // ���Ѹ��� InfiniteModeManager�� ó��
        Debug.Log($"Infinite Mode: ProcessMatchedBlocks called with {matchedBlocks.Count} blocks");
    }

    // ���Ѹ�� ���� �޼����
    public void CreateInfiniteBlock(GameObject blockPrefab, int x, int y, Vector2Int moveDirection)
    {
        GameObject block = blockFactory.CreateBlock(blockPrefab, x, y);

        InfiniteBlock infiniteBlock = block.GetComponent<InfiniteBlock>();
        if (infiniteBlock == null)
        {
            infiniteBlock = block.AddComponent<InfiniteBlock>();
        }

        infiniteBlock.moveDirection = moveDirection;
        grid[x, y] = block;
    }

    public void MoveBlock(GameObject block, int fromX, int fromY, int toX, int toY)
    {
        if (grid[fromX, fromY] == block)
        {
            grid[fromX, fromY] = null;
        }

        grid[toX, toY] = block;

        Block blockComponent = block.GetComponent<Block>();
        if (blockComponent != null)
        {
            blockComponent.x = toX;
            blockComponent.y = toY;
        }
    }
}