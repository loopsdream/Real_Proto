// ItemData.cs - 아이템 정보를 정의하는 ScriptableObject
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "CROxCRO/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public ItemType itemType;
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite itemIcon;

    [Header("Usage Settings")]
    public bool usableInStageMode = true;
    public bool usableInInfiniteMode = false;

    [Header("Shop Settings")]
    public int coinPrice = 100;
    public int diamondPrice = 0;
    public bool isAvailableInShop = true;
}

public enum ItemType
{
    Hammer,     // 망치 - 블록 1개 파괴
    Tornado,    // 회오리 - 블록 위치 랜덤 셔플
    Brush       // 붓 - 블록 색상 변경
}