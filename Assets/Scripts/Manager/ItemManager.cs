// ItemManager.cs - Stage Mode Item System Manager
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    [Header("Grid References")]
    private StageGridManager stageGridManager;
    private ItemUIManager itemUIManager;

    [Header("Item Effects")]
    public GameObject hammerParticleEffect;
    public GameObject tornadoParticleEffect;
    public GameObject brushParticleEffect;

    private bool itemModeActive = false;
    private ItemType? activeItemType = null;

    void Start()
    {
        // GameManager GameObject에서 StageGridManager 찾기
        GameObject gameManagerObj = GameObject.Find("GameManager");
        if (gameManagerObj != null)
        {
            stageGridManager = gameManagerObj.GetComponent<StageGridManager>();
            Debug.Log("[ItemManager] Found StageGridManager on GameManager");
        }

        if (stageGridManager == null)
        {
            Debug.LogError("[ItemManager] StageGridManager not found!");
        }

        // ItemUIManager 찾기
        itemUIManager = Object.FindAnyObjectByType<ItemUIManager>();
        if (itemUIManager != null)
        {
            itemUIManager.OnItemModeActivated += OnItemModeActivated;
            itemUIManager.OnItemModeDeactivated += OnItemModeDeactivated;
            Debug.Log("[ItemManager] Connected to ItemUIManager");
        }

        Debug.Log("[ItemManager] Initialization complete");
    }

    void OnDestroy()
    {
        // Unsubscribe events
        if (itemUIManager != null)
        {
            itemUIManager.OnItemModeActivated -= OnItemModeActivated;
            itemUIManager.OnItemModeDeactivated -= OnItemModeDeactivated;
        }
    }

    void OnItemModeActivated(ItemType itemType)
    {
        itemModeActive = true;
        activeItemType = itemType;
        Debug.Log("Item mode activated: " + itemType.ToString());
    }

    void OnItemModeDeactivated()
    {
        itemModeActive = false;
        activeItemType = null;
        Debug.Log("Item mode deactivated");
    }

    public bool TryUseItemOnBlock(Vector2Int gridPos)
    {
        Debug.Log("start TryUseItemOnBlock...");

        // StageGridManager 재확인
        if (stageGridManager == null)
        {
            GameObject gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj != null)
            {
                stageGridManager = gameManagerObj.GetComponent<StageGridManager>();
            }
        }

        if (!itemModeActive || !activeItemType.HasValue || stageGridManager == null)
        {
            Debug.LogWarning($"[TryUseItemOnBlock] Cannot use item - Active: {itemModeActive}, Type: {activeItemType.HasValue}, GridManager: {stageGridManager != null}");
            return false;
        }

        Debug.Log("progress TryUseItemOnBlock step 1");

        // Check if position is valid
        if (!IsValidGridPosition(gridPos))
            return false;

        Debug.Log("progress TryUseItemOnBlock step 2");

        // Execute item effect
        bool success = false;
        switch (activeItemType.Value)
        {
            case ItemType.Hammer:
                success = UseHammer(gridPos);
                break;
            case ItemType.Tornado:
                success = UseTornado();
                break;
            case ItemType.Brush:
                success = UseBrush(gridPos);
                break;
        }

        Debug.Log($"progress TryUseItemOnBlock step 3, success : {success}");

        if (success)
        {
            // Consume item from inventory
            if (UserDataManager.Instance != null)
            {
                UserDataManager.Instance.UseItem(activeItemType.Value);
            }

            // Play sound effect //TODO
            //PlayItemSFX(activeItemType.Value);

            // Deactivate item mode
            if (itemUIManager != null)
            {
                itemUIManager.ClearSelection();
            }
        }

        return success;
    }

    bool UseHammer(Vector2Int gridPos)
    {
        Debug.Log("Using Hammer at position: " + gridPos.ToString());

        // 디버깅: stageGridManager 체크
        if (stageGridManager == null)
        {
            Debug.LogError("[UseHammer] stageGridManager is null!");
            return false;
        }

        // Get block at position
        Block targetBlock = GetBlockAtPosition(gridPos);

        // 디버깅: 블록 찾기 결과
        if (targetBlock == null)
        {
            Debug.LogError($"[UseHammer] No block found at position {gridPos}");
            return false;
        }

        Debug.Log($"[UseHammer] Found block - isEmpty: {targetBlock.isEmpty}");

        if (targetBlock.isEmpty) // Cannot destroy empty blocks
        {
            Debug.LogWarning("[UseHammer] Cannot destroy empty block!");
            return false;
        }

        // Create particle effect
        if (hammerParticleEffect != null)
        {
            Vector3 worldPos = GridToWorldPosition(gridPos);
            GameObject effect = Instantiate(hammerParticleEffect, worldPos, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Destroy the block
        Debug.Log($"[UseHammer] Destroying block at {gridPos}");
        DestroyBlockAtPosition(gridPos);

        // Update score
        if (stageGridManager != null)
        {
            stageGridManager.AddScoreFromItem(10);
            Debug.Log("[UseHammer] Score updated");
        }

        return true;
    }

    bool UseTornado()
    {
        Debug.Log("Using Tornado - Shuffling blocks");

        if (stageGridManager == null) return false;

        // Get all non-empty blocks
        List<Block> allBlocks = GetAllNonEmptyBlocks();
        if (allBlocks.Count < 2) return false; // Need at least 2 blocks to shuffle

        // Create particle effect at center
        if (tornadoParticleEffect != null)
        {
            Vector3 centerPos = GetGridCenterPosition();
            GameObject effect = Instantiate(tornadoParticleEffect, centerPos, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Collect block types from tags
        List<int> blockTypes = new List<int>();
        for (int i = 0; i < allBlocks.Count; i++)
        {
            int blockType = GetBlockTypeFromGameObject(allBlocks[i].gameObject);
            blockTypes.Add(blockType);
        }

        // Shuffle the types
        for (int i = 0; i < blockTypes.Count; i++)
        {
            int randomIndex = Random.Range(i, blockTypes.Count);
            int temp = blockTypes[i];
            blockTypes[i] = blockTypes[randomIndex];
            blockTypes[randomIndex] = temp;
        }

        // Apply shuffled types back to blocks by replacing them
        for (int i = 0; i < allBlocks.Count; i++)
        {
            Vector2Int pos = new Vector2Int(allBlocks[i].x, allBlocks[i].y);
            ReplaceBlockWithNewType(pos, blockTypes[i]);
        }

        return true;
    }

    bool UseBrush(Vector2Int gridPos)
    {
        Debug.Log("Using Brush at position: " + gridPos.ToString());

        Block targetBlock = GetBlockAtPosition(gridPos);
        if (targetBlock == null || targetBlock.isEmpty) // Cannot change empty blocks
            return false;

        // Create particle effect
        if (brushParticleEffect != null)
        {
            Vector3 worldPos = GridToWorldPosition(gridPos);
            GameObject effect = Instantiate(brushParticleEffect, worldPos, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Get current block type from tag
        int currentBlockType = GetBlockTypeFromGameObject(targetBlock.gameObject);

        // Get random new color (different from current)
        int newBlockType = GetRandomBlockType();
        int attempts = 0;
        while (newBlockType == currentBlockType && attempts < 10)
        {
            newBlockType = GetRandomBlockType();
            attempts++;
        }

        // Replace with new block type
        ReplaceBlockWithNewType(gridPos, newBlockType);

        return true;
    }

    // Helper methods
    bool IsValidGridPosition(Vector2Int gridPos)
    {
        if (stageGridManager == null) return false;
        return gridPos.x >= 0 && gridPos.x < stageGridManager.GetGridWidth() &&
               gridPos.y >= 0 && gridPos.y < stageGridManager.GetGridHeight();
    }

    Block GetBlockAtPosition(Vector2Int gridPos)
    {
        if (stageGridManager == null) return null;
        return stageGridManager.GetBlockComponentAt(gridPos.x, gridPos.y); // Use updated method name
    }

    void DestroyBlockAtPosition(Vector2Int gridPos)
    {
        if (stageGridManager == null) return;
        stageGridManager.DestroyBlockAt(gridPos.x, gridPos.y);
    }

    Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        if (stageGridManager == null) return Vector3.zero;
        return stageGridManager.GetWorldPositionFromGrid(gridPos.x, gridPos.y); // Use updated method name
    }

    List<Block> GetAllNonEmptyBlocks()
    {
        List<Block> blocks = new List<Block>();
        if (stageGridManager == null) return blocks;

        return stageGridManager.GetAllNonEmptyBlocks(); // Use StageGridManager's method
    }

    Vector3 GetGridCenterPosition()
    {
        if (stageGridManager == null) return Vector3.zero;

        int centerX = stageGridManager.GetGridWidth() / 2;
        int centerY = stageGridManager.GetGridHeight() / 2;
        return GridToWorldPosition(new Vector2Int(centerX, centerY));
    }

    int GetBlockTypeFromGameObject(GameObject blockObj)
    {
        if (blockObj == null) return 0;

        // Use BlockFactory to get block type from tag
        if (stageGridManager != null && stageGridManager.blockFactory != null)
        {
            return stageGridManager.blockFactory.GetBlockTypeFromTag(blockObj.tag);
        }

        // Fallback: manual tag checking
        switch (blockObj.tag)
        {
            case "RedBlock": return 1;
            case "BlueBlock": return 2;
            case "YellowBlock": return 3;
            case "GreenBlock": return 4;
            case "PurpleBlock": return 5;
            default: return 0;
        }
    }

    void ReplaceBlockWithNewType(Vector2Int gridPos, int newBlockType)
    {
        if (stageGridManager == null || stageGridManager.blockFactory == null) return;

        // Destroy old block and create new one
        stageGridManager.DestroyBlockAt(gridPos.x, gridPos.y);

        // Create new block with the desired type
        GameObject newBlock = stageGridManager.blockFactory.CreateBlockFromType(newBlockType, gridPos.x, gridPos.y);
        stageGridManager.SetBlockAt(gridPos.x, gridPos.y, newBlock);
    }

    int GetRandomBlockType()
    {
        return Random.Range(1, 6); // Block types 1-5 (Red, Blue, Green, Yellow, Purple)
    }
    
    
    // Public getter for other scripts
    public bool IsItemModeActive()
    {
        return itemModeActive;
    }

    public ItemType? GetActiveItemType()
    {
        return activeItemType;
    }
}