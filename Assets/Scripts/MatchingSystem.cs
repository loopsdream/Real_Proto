using System.Collections.Generic;
using UnityEngine;

public class MatchingSystem : MonoBehaviour
{
    [Header("Matching Settings")]
    public int minimumMatchCount = 2;
    
    private GridLayoutManager layoutManager;
    
    void Awake()
    {
        layoutManager = GetComponent<GridLayoutManager>();
        if (layoutManager == null)
        {
            Debug.LogError("GridLayoutManager not found!");
        }
    }
    
    public List<GameObject> FindMatchingBlocks(int startX, int startY, GameObject[,] grid)
    {
        List<GameObject> allMatchedBlocks = new List<GameObject>();
        Dictionary<string, List<GameObject>> blocksByType = new Dictionary<string, List<GameObject>>();
        
        Vector2Int[] directions = {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0)   // Right
        };
        
        foreach (Vector2Int dir in directions)
        {
            GameObject foundBlock = FindFirstBlockInDirection(startX, startY, dir.x, dir.y, grid);
            if (foundBlock != null)
            {
                Block blockComponent = foundBlock.GetComponent<Block>();
                if (blockComponent != null && !blockComponent.isEmpty)
                {
                    string blockType = foundBlock.tag;
                    
                    if (!blocksByType.ContainsKey(blockType))
                    {
                        blocksByType[blockType] = new List<GameObject>();
                    }
                    blocksByType[blockType].Add(foundBlock);
                }
            }
        }
        
        // Find types with minimum matches
        foreach (var entry in blocksByType)
        {
            string blockType = entry.Key;
            List<GameObject> blocks = entry.Value;
            
            if (blocks.Count >= minimumMatchCount)
            {
                allMatchedBlocks.AddRange(blocks);
                Debug.Log($"Found {blocks.Count} matching blocks of type {blockType}");
            }
        }
        
        return allMatchedBlocks;
    }
    
    public GameObject FindFirstBlockInDirection(int startX, int startY, int dirX, int dirY, GameObject[,] grid)
    {
        if (grid == null) return null;
        
        int gridWidth = grid.GetLength(0);
        int gridHeight = grid.GetLength(1);
        
        int currX = startX;
        int currY = startY;
        
        while (true)
        {
            currX += dirX;
            currY += dirY;
            
            // Check bounds
            if (currX < 0 || currX >= gridWidth || currY < 0 || currY >= gridHeight)
            {
                return null;
            }
            
            GameObject block = grid[currX, currY];
            if (block != null)
            {
                Block blockComponent = block.GetComponent<Block>();
                if (blockComponent != null)
                {
                    if (!blockComponent.isEmpty)
                    {
                        return block;
                    }
                }
            }
        }
    }
    
    public bool HasValidMatches(int x, int y, GameObject[,] grid)
    {
        List<GameObject> matches = FindMatchingBlocks(x, y, grid);
        return matches.Count >= minimumMatchCount;
    }
    
    public int CalculateScore(List<GameObject> matchedBlocks, int scorePerBlock = 10)
    {
        if (matchedBlocks == null || matchedBlocks.Count == 0)
            return 0;
        
        int baseScore = matchedBlocks.Count * scorePerBlock;
        
        // Bonus for larger matches
        if (matchedBlocks.Count >= 4)
            baseScore *= 2;
        else if (matchedBlocks.Count >= 6)
            baseScore *= 3;
        
        return baseScore;
    }
    
    public Dictionary<string, int> GetMatchingStats(List<GameObject> matchedBlocks)
    {
        Dictionary<string, int> stats = new Dictionary<string, int>();
        
        foreach (GameObject block in matchedBlocks)
        {
            if (block != null)
            {
                string blockType = block.tag;
                if (stats.ContainsKey(blockType))
                {
                    stats[blockType]++;
                }
                else
                {
                    stats[blockType] = 1;
                }
            }
        }
        
        return stats;
    }
    
    public List<Vector2Int> GetMatchingPositions(List<GameObject> matchedBlocks)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        foreach (GameObject block in matchedBlocks)
        {
            if (block != null)
            {
                Block blockComponent = block.GetComponent<Block>();
                if (blockComponent != null)
                {
                    positions.Add(new Vector2Int(blockComponent.x, blockComponent.y));
                }
            }
        }
        
        return positions;
    }
    
    public bool ValidateMatch(List<GameObject> matchedBlocks)
    {
        if (matchedBlocks == null || matchedBlocks.Count < minimumMatchCount)
        {
            return false;
        }
        
        // Check if all blocks have the same type
        string firstBlockType = null;
        foreach (GameObject block in matchedBlocks)
        {
            if (block == null) return false;
            
            if (firstBlockType == null)
            {
                firstBlockType = block.tag;
            }
            else if (block.tag != firstBlockType)
            {
                Debug.LogWarning("Match validation failed: blocks have different types");
                return false;
            }
        }
        
        return true;
    }
    
    public void SetMinimumMatchCount(int count)
    {
        minimumMatchCount = Mathf.Max(2, count);
        Debug.Log($"Minimum match count set to: {minimumMatchCount}");
    }
}