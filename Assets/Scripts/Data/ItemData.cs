// ItemData.cs - ������ ������ �����ϴ� ScriptableObject
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
    Hammer,     // ��ġ - ��� 1�� �ı�
    Tornado,    // ȸ���� - ��� ��ġ ���� ����
    Brush       // �� - ��� ���� ����
}