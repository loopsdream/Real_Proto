// StageData.cs - 스테이지 데이터를 저장하는 ScriptableObject
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage", menuName = "Block Puzzle/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Information")]
    public int stageNumber;
    public string stageName;
    public string stageDescription;

    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 8;

    [Header("Block Pattern")]
    [Tooltip("0 = Empty, 1 = Red, 2 = Blue, 3 = Yellow, 4 = Green, 5 = Purple")]
    public int[] blockPattern;

    [Header("Victory Conditions")]
    public int targetScore = 100;
    public int maxMoves = 10; // 최대 이동 횟수 (선택사항)
    public bool hasTimeLimit = false;
    public float timeLimit = 60f;

    [Header("Difficulty")]
    public float emptyBlockChance = 0.3f; // 빈 블록 생성 확률

    // 패턴 검증
    void OnValidate()
    {
        if (blockPattern != null && blockPattern.Length != gridWidth * gridHeight)
        {
            Debug.LogWarning($"Block pattern length ({blockPattern.Length}) doesn't match grid size ({gridWidth * gridHeight})");
        }
    }
}