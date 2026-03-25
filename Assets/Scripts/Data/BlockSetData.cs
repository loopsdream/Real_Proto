// BlockSetData.cs - 블록 프리팹 묶음을 정의하는 ScriptableObject
using UnityEngine;

[CreateAssetMenu(fileName = "New BlockSet", menuName = "Block Puzzle/Block Set Data")]
public class BlockSetData : ScriptableObject
{
    [Header("Block Set Info")]
    public string blockSetName = "Default";
    public string description;

    [Header("Empty Block")]
    [Tooltip("빈 셀에 사용할 프리팹 (클릭 가능한 투명/배경 블록)")]
    public GameObject emptyBlockPrefab;

    [Header("Colored Block Prefabs")]
    [Tooltip("Index 0=Red, 1=Blue, 2=Yellow, 3=Green, 4=Purple, 5=Pink, 6=Orange, 7=Lime, 8=Teal, 9=Cyan, 10=Indigo, 11=Magenta")]
    public GameObject[] blockPrefabs = new GameObject[12];

    // 블록 타입 인덱스(1~12)로 프리팹 반환
    public GameObject GetPrefabByType(int blockType)
    {
        if (blockType == 0)
            return emptyBlockPrefab;

        int index = blockType - 1;
        if (index >= 0 && index < blockPrefabs.Length)
            return blockPrefabs[index];

        Debug.LogWarning($"[BlockSetData] Invalid block type: {blockType}");
        return null;
    }

    // 에셋 유효성 검사
    public bool IsValid()
    {
        if (emptyBlockPrefab == null)
        {
            Debug.LogError($"[BlockSetData:{blockSetName}] emptyBlockPrefab is not assigned.");
            return false;
        }

        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogError($"[BlockSetData:{blockSetName}] blockPrefabs array is empty.");
            return false;
        }

        return true;
    }

    void OnValidate()
    {
        // 배열 크기를 항상 12로 유지
        if (blockPrefabs == null || blockPrefabs.Length != 12)
        {
            System.Array.Resize(ref blockPrefabs, 12);
        }
    }
}
