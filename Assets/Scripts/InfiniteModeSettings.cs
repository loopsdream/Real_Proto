using UnityEngine;

[CreateAssetMenu(fileName = "InfiniteModeSettings", menuName = "Block Puzzle/Infinite Mode Settings")]
public class InfiniteModeSettings : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 9;
    public int gridHeight = 9;

    [Header("Time Settings")]
    public float initialTimeLimit = 60f;           // 초기 제한 시간 (초)

    [Header("Difficulty Levels")]
    public DifficultyLevel[] difficultyLevels = new DifficultyLevel[]
    {
        new DifficultyLevel { difficultyName = "1 Phase", startTime = 0f, moveInterval = 6f, minSpawnChance = 0.1f, maxSpawnChance = 0.2f, cornerMode = CornerBlockMode.SingleCorner, bonusScoreMultiplier = 1.0f },
        new DifficultyLevel { difficultyName = "2 Phase", startTime = 30f, moveInterval = 6f, minSpawnChance = 0.2f, maxSpawnChance = 0.3f, cornerMode = CornerBlockMode.SingleCorner, bonusScoreMultiplier = 1.2f },
        new DifficultyLevel { difficultyName = "3 Phase", startTime = 90f, moveInterval = 6f, minSpawnChance = 0.3f, maxSpawnChance = 0.4f, cornerMode = CornerBlockMode.FourCorners, bonusScoreMultiplier = 1.4f },
        new DifficultyLevel { difficultyName = "4 Phase", startTime = 120f, moveInterval = 6f, minSpawnChance = 0.4f, maxSpawnChance = 0.5f, cornerMode = CornerBlockMode.FourCorners, bonusScoreMultiplier = 1.6f }
    };

    [Header("Reward Settings")]
    [Tooltip("2개 블록 파괴 시 획득 점수")]
    public int score2Blocks = 10;
    [Tooltip("3개 블록 파괴 시 획득 점수")]
    public int score3Blocks = 25;
    [Tooltip("4개 블록 파괴 시 획득 점수")]
    public int score4Blocks = 50;

    [Header("Time Bonus")]
    [Tooltip("2개 블록 파괴 시 추가 시간")]
    public float timeBonus2Blocks = 1f;
    [Tooltip("3개 블록 파괴 시 추가 시간")]
    public float timeBonus3Blocks = 2f;
    [Tooltip("4개 블록 파괴 시 추가 시간")]
    public float timeBonus4Blocks = 3f;

    [Header("Penalty")]
    [Tooltip("파괴할 블록이 없을 때 차감되는 시간")]
    public float timePenalty = 2f;

    [Header("Combo Bonus")]
    [Tooltip("콤보별 추가 점수 (2콤보~10콤보)")]
    public int[] comboBonusScores = new int[9] { 5, 10, 20, 35, 55, 80, 110, 145, 185 };

    [System.Serializable]
    public class DifficultyLevel
    {
        public string difficultyName = "Easy";
        public float startTime = 0f;  // 이 난이도가 시작되는 시간 (초)
        public float moveInterval = 3f;

        [Header("Block Spawn Settings")]
        [Range(0f, 1f)]
        public float minSpawnChance = 0.25f;  // 최소 생성 확률
        [Range(0f, 1f)]
        public float maxSpawnChance = 0.5f;   // 최대 생성 확률

        public CornerBlockMode cornerMode = CornerBlockMode.SingleCorner;

        [Header("Score Bonus")]
        public float bonusScoreMultiplier = 1.0f;  // 기본 점수에 곱해지는 보너스 배수
    }

    // 난이도별 설정 가져오기
    public DifficultyLevel GetCurrentDifficulty(float gameTime)
    {
        // 역순으로 검사하여 가장 늦게 시작하는 난이도부터 확인
        for (int i = difficultyLevels.Length - 1; i >= 0; i--)
        {
            if (gameTime >= difficultyLevels[i].startTime)
            {
                Debug.Log($"Game time: {gameTime}s, Current difficulty: {difficultyLevels[i].difficultyName}");
                return difficultyLevels[i];
            }
        }

        Debug.Log($"Game time: {gameTime}s, Current difficulty: {difficultyLevels[0].difficultyName}");

        // 기본값으로 첫 번째 난이도 반환
        return difficultyLevels[0];
    }

    // 유틸리티 메서드
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
}

// 모서리 블록 모드 열거형
public enum CornerBlockMode
{
    SingleCorner,   // 네 모서리 각 1칸씩만 제외
    FourCorners     // 네 모서리 각 4칸씩 제외 (2x2 영역)
}

