using UnityEngine;

[CreateAssetMenu(fileName = "InfiniteModeSettingsV2", menuName = "Block Puzzle/Infinite Mode Settings V2")]
public class InfiniteModeSettingsV2 : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 9;
    public int gridHeight = 9;

    [Header("Block Size")]
    [Range(0.1f, 2.0f)]
    [Tooltip("Block size scale multiplier. 1.0 = 9x9 standard size")]
    public float blockSizeScale = 1.0f;

    [Header("Time Settings")]
    public float initialTimeLimit = 60f;

    [Header("Difficulty Levels")]
    public DifficultyLevelV2[] difficultyLevels = new DifficultyLevelV2[]
    {
        new DifficultyLevelV2
        {
            difficultyName = "1 Phase",
            startTime = 0f,
            moveInterval = 6f,
            //cornerMode = CornerBlockMode.SingleCorner,
            bonusScoreMultiplier = 1.0f,
            topSide = new SideSpawnSetting(true, 0.1f, 0.2f),
            bottomSide = new SideSpawnSetting(false, 0.1f, 0.2f),
            leftSide = new SideSpawnSetting(false, 0.1f, 0.2f),
            rightSide = new SideSpawnSetting(false, 0.1f, 0.2f)
        }
    };

    [Header("Reward Settings")]
    public int score2Blocks = 10;
    public int score3Blocks = 25;
    public int score4Blocks = 50;

    [Header("Time Bonus")]
    public float timeBonus2Blocks = 1f;
    public float timeBonus3Blocks = 2f;
    public float timeBonus4Blocks = 3f;

    [Header("Penalty")]
    public float timePenalty = 2f;

    [Header("Combo Bonus")]
    public int[] comboBonusScores = new int[9] { 5, 10, 20, 35, 55, 80, 110, 145, 185 };

    // 현재 난이도 반환
    public DifficultyLevelV2 GetCurrentDifficulty(float gameTime)
    {
        for (int i = difficultyLevels.Length - 1; i >= 0; i--)
        {
            if (gameTime >= difficultyLevels[i].startTime)
            {
                return difficultyLevels[i];
            }
        }
        return difficultyLevels[0];
    }

    public int GetScoreForBlockCount(int blockCount)
    {
        switch (blockCount)
        {
            case 2: return score2Blocks;
            case 3: return score3Blocks;
            case 4: return score4Blocks;
            default: return 0;
        }
    }

    public float GetTimeBonusForBlockCount(int blockCount)
    {
        switch (blockCount)
        {
            case 2: return timeBonus2Blocks;
            case 3: return timeBonus3Blocks;
            case 4: return timeBonus4Blocks;
            default: return 0f;
        }
    }

    public int GetComboBonusScore(int comboCount)
    {
        if (comboCount >= 2 && comboCount <= 10)
        {
            return comboBonusScores[comboCount - 2];
        }
        return 0;
    }

    // V2 난이도 레벨 클래스
    [System.Serializable]
    public class DifficultyLevelV2
    {
        public string difficultyName = "Easy";
        public float startTime = 0f;
        public float moveInterval = 3f;

        //public CornerBlockMode cornerMode = CornerBlockMode.SingleCorner;

        [Header("Score Bonus")]
        public float bonusScoreMultiplier = 1.0f;

        [Header("Side Spawn Settings")]
        public SideSpawnSetting topSide = new SideSpawnSetting(true, 0.1f, 0.2f);
        public SideSpawnSetting bottomSide = new SideSpawnSetting(true, 0.1f, 0.2f);
        public SideSpawnSetting leftSide = new SideSpawnSetting(true, 0.1f, 0.2f);
        public SideSpawnSetting rightSide = new SideSpawnSetting(true, 0.1f, 0.2f);
    }
}