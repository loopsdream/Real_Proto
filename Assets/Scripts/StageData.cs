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

    [Header("Clear Conditions")]
    public bool hasTimeLimit = true;
    public float timeLimit = 180f; // 3분 기본 시간 제한
    public int maxMoves = 0; // 0 = 무제한

    [Header("Stage Rewards")]
    public int coinReward = 100;
    public int diamondReward = 0;
    public int experienceReward = 10;

    [Header("Special Rules")]
    public bool allowColorTransform = true;  // 색상 변환 허용
    public float shuffleAnimationDuration = 1.0f;
    public float blockConversionDuration = 0.8f;

    [Header("Difficulty")]
    [Range(1, 5)]
    public int difficultyLevel = 1;
    public bool showHints = true;  // 힌트 표시 여부

    // 유효성 검사
    void OnValidate()
    {
        if (blockPattern != null && blockPattern.Length != gridWidth * gridHeight)
        {
            Debug.LogWarning($"Block pattern length ({blockPattern.Length}) doesn't match grid size ({gridWidth * gridHeight})");
            System.Array.Resize(ref blockPattern, gridWidth * gridHeight);
        }

        // 시간 제한 최소값 설정
        if (hasTimeLimit && timeLimit < 30f)
        {
            timeLimit = 30f;
            Debug.LogWarning("Time limit cannot be less than 30 seconds");
        }

        // 보상 최소값
        if (coinReward < 0) coinReward = 0;
        if (diamondReward < 0) diamondReward = 0;
        if (experienceReward < 0) experienceReward = 0;
    }

    // 스테이지가 클리어 가능한지 검증
    public bool ValidateStagePlayable()
    {
        int coloredBlockCount = 0;
        for (int i = 0; i < blockPattern.Length; i++)
        {
            if (blockPattern[i] > 0)  // 0이 아닌 블록
            {
                coloredBlockCount++;
            }
        }

        // 최소 2개 이상의 색깔 블록이 있어야 함
        return coloredBlockCount >= 2;
    }
}