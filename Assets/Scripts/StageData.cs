// StageData.cs - 스테이지 정보를 담는 ScriptableObject
using UnityEngine;
using System.Collections.Generic;

public enum CollectibleType
{
    None = 0,
    Heart = 1,
    Clover = 2
}

public enum ClearGoalType
{
    DestroyAllBlocks = 0,      // 모든 블록 파괴
    CollectColorBlocks = 1,     // 특정 색상 블록 개수
    CollectCollectibles = 2     // 수집품 개수
}

[System.Serializable]
public class ClearGoalData
{
    public ClearGoalType goalType;

    // For CollectColorBlocks
    public int targetColor;     // 1-6 (Red, Blue, Yellow, Green, Purple, Pink)
    public int targetColorCount;

    // For CollectCollectibles
    public CollectibleType collectibleType;
    public int targetCollectibleCount;
}

[CreateAssetMenu(fileName = "New Stage", menuName = "Block Puzzle/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Information")]
    public int stageNumber;
    public string stageName;
    public string stageDescription;
    public int targetScore;

    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 8;

    [Header("Block Pattern")]
    [Tooltip("0 = Empty, 1 = Red, 2 = Blue, 3 = Yellow, 4 = Green, 5 = Purple, 6 = Pink")]
    public int[] blockPattern;

    [Header("Collectible System")]
    [Tooltip("0 = None, 1 = Heart, 2 = Clover")]
    public int[] collectiblePattern;  // Same size as blockPattern

    [Header("Clear Goals")]
    [Tooltip("DestroyAllBlocks: single goal only. Others: multiple goals possible")]
    public List<ClearGoalData> clearGoals = new List<ClearGoalData>();

    [Header("Clear Conditions")]
    public bool hasTimeLimit = true;
    public float timeLimit = 180f; // 3분 기본 시간 제한
    public int maxTaps = 0; // 0 = 무제한

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
        // Block pattern validation
        if (blockPattern != null && blockPattern.Length != gridWidth * gridHeight)
        {
            Debug.LogWarning($"Block pattern length ({blockPattern.Length}) doesn't match grid size ({gridWidth * gridHeight})");
            System.Array.Resize(ref blockPattern, gridWidth * gridHeight);
        }

        // Collectible pattern validation
        if (collectiblePattern != null && collectiblePattern.Length != gridWidth * gridHeight)
        {
            Debug.LogWarning($"Collectible pattern length ({collectiblePattern.Length}) doesn't match grid size ({gridWidth * gridHeight})");
            System.Array.Resize(ref collectiblePattern, gridWidth * gridHeight);
        }

        // Time limit minimum value
        if (hasTimeLimit && timeLimit < 30f)
        {
            timeLimit = 30f;
            Debug.LogWarning("Time limit cannot be less than 30 seconds");
        }

        // Reward minimum values
        if (coinReward < 0) coinReward = 0;
        if (diamondReward < 0) diamondReward = 0;
        if (experienceReward < 0) experienceReward = 0;

        // Clear goals validation
        ValidateClearGoals();
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

    // Validate clear goals structure
    private void ValidateClearGoals()
    {
        if (clearGoals == null || clearGoals.Count == 0)
        {
            Debug.LogWarning($"[{stageName}] No clear goals defined. Defaulting to DestroyAllBlocks.");
            return;
        }

        // Check if DestroyAllBlocks exists with other goals
        bool hasDestroyAll = false;
        foreach (var goal in clearGoals)
        {
            if (goal.goalType == ClearGoalType.DestroyAllBlocks)
            {
                hasDestroyAll = true;
                break;
            }
        }

        if (hasDestroyAll && clearGoals.Count > 1)
        {
            Debug.LogWarning($"[{stageName}] DestroyAllBlocks should be a single goal. Removing other goals.");
            clearGoals.RemoveAll(g => g.goalType != ClearGoalType.DestroyAllBlocks);
        }
    }

    // Get clear goal description for UI display
    public string GetClearGoalDescription()
    {
        if (clearGoals == null || clearGoals.Count == 0)
            return "Destroy all blocks";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var goal in clearGoals)
        {
            switch (goal.goalType)
            {
                case ClearGoalType.DestroyAllBlocks:
                    return "Destroy all blocks";

                case ClearGoalType.CollectColorBlocks:
                    string colorName = GetColorName(goal.targetColor);
                    sb.Append($"{colorName} x{goal.targetColorCount} ");
                    break;

                case ClearGoalType.CollectCollectibles:
                    string collectibleName = goal.collectibleType.ToString();
                    sb.Append($"{collectibleName} x{goal.targetCollectibleCount} ");
                    break;
            }
        }
        return sb.ToString().Trim();
    }

    private string GetColorName(int colorIndex)
    {
        switch (colorIndex)
        {
            case 1: return "Red";
            case 2: return "Blue";
            case 3: return "Yellow";
            case 4: return "Green";
            case 5: return "Purple";
            case 6: return "Pink";
            default: return "Unknown";
        }
    }
}