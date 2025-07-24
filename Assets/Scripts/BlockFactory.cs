using UnityEngine;

public class BlockFactory : MonoBehaviour
{
    [Header("Block Prefabs")]
    public GameObject[] blockPrefabs;
    public GameObject emptyBlockPrefab;
    
    [Header("Parent Transform")]
    public Transform gridParent;
    
    private GridLayoutManager layoutManager;
    
    void Awake()
    {
        layoutManager = GetComponent<GridLayoutManager>();
        if (layoutManager == null)
        {
            Debug.LogError("GridLayoutManager not found!");
        }
    }
    
    public GameObject CreateBlock(GameObject prefab, int x, int y)
    {
        if (prefab == null || layoutManager == null) return null;
        
        Vector3 worldPos = layoutManager.GridToWorldPosition(x, y);
        GameObject blockObj = Instantiate(prefab, worldPos, Quaternion.identity);
        
        SetupBlockParent(blockObj);
        ApplyBlockScale(blockObj);
        SetupBlockComponent(blockObj, x, y, prefab == emptyBlockPrefab);
        
        blockObj.name = $"Block_{x}_{y}";
        return blockObj;
    }
    
    public GameObject CreateBlockFromType(int blockType, int x, int y)
    {
        GameObject prefabToUse = GetBlockPrefabByType(blockType);
        if (prefabToUse == null)
        {
            prefabToUse = emptyBlockPrefab;
        }
        return CreateBlock(prefabToUse, x, y);
    }
    
    public GameObject CreateEmptyBlock(int x, int y)
    {
        return CreateBlock(emptyBlockPrefab, x, y);
    }
    
    public GameObject CreateRandomBlock(int x, int y)
    {
        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            return CreateEmptyBlock(x, y);
        }
        
        int randomIndex = Random.Range(0, blockPrefabs.Length);
        return CreateBlock(blockPrefabs[randomIndex], x, y);
    }
    
    public void DestroyBlock(GameObject block)
    {
        if (block != null)
        {
            Destroy(block);
        }
    }
    
    public GameObject ReplaceWithEmptyBlock(GameObject blockToReplace, int x, int y)
    {
        if (blockToReplace != null)
        {
            DestroyBlock(blockToReplace);
        }
        return CreateEmptyBlock(x, y);
    }
    
    public void UpdateBlockPosition(GameObject block, int newX, int newY)
    {
        if (block == null || layoutManager == null) return;
        
        Vector3 newWorldPos = layoutManager.GridToWorldPosition(newX, newY);
        block.transform.position = newWorldPos;
        
        Block blockComponent = block.GetComponent<Block>();
        if (blockComponent != null)
        {
            blockComponent.x = newX;
            blockComponent.y = newY;
        }
        
        block.name = $"Block_{newX}_{newY}";
    }
    
    private void SetupBlockParent(GameObject blockObj)
    {
        if (gridParent != null)
        {
            blockObj.transform.SetParent(gridParent.transform);
        }
    }
    
    private void ApplyBlockScale(GameObject blockObj)
    {
        if (blockObj == null || layoutManager == null) return;
        
        float cellSize = layoutManager.cellSize;
        blockObj.transform.localScale = Vector3.one * cellSize;
    }
    
    public void SetGridParent(Transform newParent)
    {
        gridParent = newParent;
    }

    private void SetupBlockComponent(GameObject blockObj, int x, int y, bool isEmpty)
    {
        Block blockComponent = blockObj.GetComponent<Block>();
        if (blockComponent == null)
        {
            blockComponent = blockObj.AddComponent<Block>();
        }
        
        blockComponent.x = x;
        blockComponent.y = y;
        blockComponent.isEmpty = isEmpty;
        
        if (isEmpty)
        {
            BlockInteraction interaction = blockObj.GetComponent<BlockInteraction>();
            if (interaction == null)
            {
                interaction = blockObj.AddComponent<BlockInteraction>();
            }
        }
    }
    
    private GameObject GetBlockPrefabByType(int blockType)
    {
        if (blockType == 0)
        {
            return emptyBlockPrefab;
        }
        else if (blockType >= 1 && blockType <= blockPrefabs.Length)
        {
            return blockPrefabs[blockType - 1];
        }
        else
        {
            return null;
        }
    }
    
    public int GetBlockTypeFromTag(string tag)
    {
        switch (tag)
        {
            case "RedBlock": return 1;
            case "BlueBlock": return 2;
            case "YellowBlock": return 3;
            case "GreenBlock": return 4;
            case "PurpleBlock": return 5;
            default: return 0;
        }
    }
    
    public bool ValidateBlockPrefabs()
    {
        if (emptyBlockPrefab == null)
        {
            Debug.LogError("Empty block prefab is not assigned!");
            return false;
        }
        
        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogError("No block prefabs assigned!");
            return false;
        }
        
        return true;
    }

    public string GetTagFromBlockType(int blockType)
    {
        switch (blockType)
        {
            case 1: return "RedBlock";
            case 2: return "BlueBlock";
            case 3: return "YellowBlock";
            case 4: return "GreenBlock";
            case 5: return "PurpleBlock";
            default: return "EmptyBlock";
        }
    }
}