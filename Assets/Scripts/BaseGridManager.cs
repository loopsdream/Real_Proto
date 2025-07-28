using UnityEngine;
using System.Collections.Generic;
using TMPro;

public abstract class BaseGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 8;
    public int height = 8;
    public Transform gridParent;

    [Header("Components")]
    public CameraController cameraController;
    public GridLayoutManager layoutManager;
    public BlockFactory blockFactory;
    public MatchingSystem matchingSystem;

    protected GameObject[,] grid;

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void InitializeComponents()
    {
        if (cameraController == null)
            cameraController = GetComponent<CameraController>();

        if (layoutManager == null)
            layoutManager = GetComponent<GridLayoutManager>();

        if (blockFactory == null)
            blockFactory = GetComponent<BlockFactory>();

        if (matchingSystem == null)
            matchingSystem = GetComponent<MatchingSystem>();
    }

    // 공통 메서드들
    public virtual void SetupGrid()
    {
        grid = new GameObject[width, height];

        if (layoutManager != null)
        {
            layoutManager.SetupLayout(width, height, 1.0f);
        }

        if (blockFactory != null)
        {
            blockFactory.SetGridParent(gridParent);
        }
    }

    public virtual void ClearGrid()
    {
        if (grid != null)
        {
            int gridWidth = grid.GetLength(0);
            int gridHeight = grid.GetLength(1);

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        if (blockFactory != null)
                        {
                            blockFactory.DestroyBlock(grid[x, y]);
                        }
                        else
                        {
                            Destroy(grid[x, y]);
                        }
                        grid[x, y] = null;
                    }
                }
            }
        }

        if (gridParent != null)
        {
            for (int i = gridParent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(gridParent.transform.GetChild(i).gameObject);
            }
        }
    }

    protected void SetupCameraAndLayout()
    {
        if (cameraController != null && layoutManager != null)
        {
            cameraController.AdjustCameraForGrid(width, height, layoutManager.cellSize);
            Vector3 gridCenter = layoutManager.GetGridCenter();
            cameraController.CenterCameraOnGrid(gridCenter);
        }
    }

    // 공통 메서드들
    public GameObject GetBlockAt(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height && grid != null)
        {
            return grid[x, y];
        }
        return null;
    }

    public void SetBlockAt(int x, int y, GameObject block)
    {
        if (x >= 0 && x < width && y >= 0 && y < height && grid != null)
        {
            grid[x, y] = block;
        }
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        if (layoutManager != null)
        {
            return layoutManager.GridToWorldPosition(x, y);
        }
        return Vector3.zero;
    }

    // 추상 메서드 - 각 모드에서 구현
    public abstract void OnEmptyBlockClicked(int x, int y);
    protected abstract void ProcessMatchedBlocks(List<GameObject> matchedBlocks);
}