// StageData.cs - 스테이지 정보를 담는 ScriptableObject
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
    [System.Obsolete("targetScore is no longer used - stage clears when all blocks are destroyed")]
    public int targetScore = 100;
    public int maxMoves = 10; // 최대 움직임 수 (선택적)
    public bool hasTimeLimit = true;
    public float timeLimit = 180f; // 3분 기본 시간 제한

    [Header("Shuffle & Conversion Settings")]
    [Range(1, 10)]
    public int maxShuffleAttempts = 5; // 최대 셔플 시도 횟수
    public float shuffleAnimationDuration = 1.0f;
    public float blockConversionDuration = 0.8f;

    [Header("Difficulty")]
    public float emptyBlockChance = 0.3f; // 빈 블록 생성 확률

    // 유효성 검사
    void OnValidate()
    {
        if (blockPattern != null && blockPattern.Length != gridWidth * gridHeight)
        {
            Debug.LogWarning($"Block pattern length ({blockPattern.Length}) doesn't match grid size ({gridWidth * gridHeight})");
        }
        
        // 시간 제한 최소값 설정
        if (hasTimeLimit && timeLimit < 30f)
        {
            timeLimit = 30f;
            Debug.LogWarning("Time limit cannot be less than 30 seconds");
        }
    }
}