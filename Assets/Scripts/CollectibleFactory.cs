// CollectibleFactory.cs - Factory for creating and managing collectibles
using UnityEngine;
using System.Collections.Generic;

public class CollectibleFactory : MonoBehaviour
{
    [Header("Collectible Prefabs")]
    public GameObject heartPrefab;      // Heart collectible prefab
    public GameObject cloverPrefab;     // Clover collectible prefab

    [Header("Parent Transform")]
    public Transform collectibleParent;

    private GridLayoutManager layoutManager;

    void Awake()
    {
        layoutManager = GetComponent<GridLayoutManager>();
        if (layoutManager == null)
        {
            Debug.LogError("GridLayoutManager not found!");
        }
    }

    // Create collectible by type
    public GameObject CreateCollectible(CollectibleType type, int x, int y)
    {
        GameObject prefab = GetCollectiblePrefab(type);
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab found for collectible type: {type}");
            return null;
        }

        Vector3 worldPos = layoutManager.GridToWorldPosition(x, y);
        GameObject collectibleObj = Instantiate(prefab, worldPos, Quaternion.identity);

        SetupCollectibleParent(collectibleObj);
        ApplyCollectibleScale(collectibleObj);
        SetupCollectibleComponent(collectibleObj, type, x, y);

        collectibleObj.name = $"Collectible_{type}_{x}_{y}";
        return collectibleObj;
    }

    // Create collectible from integer type
    public GameObject CreateCollectibleFromType(int collectibleType, int x, int y)
    {
        CollectibleType type = (CollectibleType)collectibleType;
        if (type == CollectibleType.None)
            return null;

        return CreateCollectible(type, x, y);
    }

    // Get prefab by collectible type
    private GameObject GetCollectiblePrefab(CollectibleType type)
    {
        switch (type)
        {
            case CollectibleType.Heart:
                return heartPrefab;
            case CollectibleType.Clover:
                return cloverPrefab;
            default:
                return null;
        }
    }

    private void SetupCollectibleParent(GameObject collectibleObj)
    {
        if (collectibleParent != null)
        {
            collectibleObj.transform.SetParent(collectibleParent);
        }
    }

    private void ApplyCollectibleScale(GameObject collectibleObj)
    {
        if (collectibleObj == null || layoutManager == null) return;

        float cellSize = layoutManager.cellSize;
        collectibleObj.transform.localScale = Vector3.one * cellSize * 0.8f;  // Slightly smaller than blocks
    }

    private void SetupCollectibleComponent(GameObject collectibleObj, CollectibleType type, int x, int y)
    {
        Collectible collectible = collectibleObj.GetComponent<Collectible>();
        if (collectible == null)
        {
            collectible = collectibleObj.AddComponent<Collectible>();
        }

        collectible.collectibleType = type;
        collectible.x = x;
        collectible.y = y;
    }

    // Destroy collectible
    public void DestroyCollectible(GameObject collectible)
    {
        if (collectible != null)
        {
            Destroy(collectible);
        }
    }

    // Validate prefabs
    public bool ValidateCollectiblePrefabs()
    {
        bool isValid = true;

        if (heartPrefab == null)
        {
            Debug.LogWarning("Heart collectible prefab is not assigned!");
            isValid = false;
        }

        if (cloverPrefab == null)
        {
            Debug.LogWarning("Clover collectible prefab is not assigned!");
            isValid = false;
        }

        return isValid;
    }

    // Set collectible parent
    public void SetCollectibleParent(Transform newParent)
    {
        collectibleParent = newParent;
    }
}