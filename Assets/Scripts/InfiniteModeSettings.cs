using UnityEngine;

[CreateAssetMenu(fileName = "InfiniteModeSettings", menuName = "Block Puzzle/Infinite Mode Settings")]
public class InfiniteModeSettings : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 9;
    public int gridHeight = 9;

    [Header("Time Settings")]
    public float initialTimeLimit = 60f;           // 초기 제한 시간 (초)

    [Header("Difficulty Progression")]
    [Tooltip("1단계 난이도 지속 시간 (초)")]
    public float easyDuration = 30f;
    [Tooltip("2단계 난이도 지속 시간 (초)")]
    public float mediumDuration = 60f;
    // 3단계는 그 이후 계속

    [Header("Easy Difficulty (0-30초)")]
    public float easyMoveInterval = 3f;            // 블록 이동/생성 간격
    public float easyBlockSpawnChance = 0.3f;      // 블록 생성 확률 (30%)
    public CornerBlockMode easyCornerMode = CornerBlockMode.SingleCorner;

    [Header("Medium Difficulty (30-90초)")]
    public float mediumMoveInterval = 2f;          // 블록 이동/생성 간격
    public float mediumBlockSpawnChance = 0.5f;    // 블록 생성 확률 (50%)
    public CornerBlockMode mediumCornerMode = CornerBlockMode.SingleCorner;

    [Header("Hard Difficulty (90초 이후)")]
    public float hardMoveInterval = 1.5f;          // 블록 이동/생성 간격
    public float hardBlockSpawnChance = 0.7f;      // 블록 생성 확률 (70%)
    public CornerBlockMode hardCornerMode = CornerBlockMode.FourCorners;

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

    // 난이도별 설정 가져오기
    public DifficultySettings GetCurrentDifficulty(float gameTime)
    {
        if (gameTime <= easyDuration)
        {
            return new DifficultySettings(easyMoveInterval, easyBlockSpawnChance, easyCornerMode);
        }
        else if (gameTime <= easyDuration + mediumDuration)
        {
            return new DifficultySettings(mediumMoveInterval, mediumBlockSpawnChance, mediumCornerMode);
        }
        else
        {
            return new DifficultySettings(hardMoveInterval, hardBlockSpawnChance, hardCornerMode);
        }
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

// 난이도별 설정 클래스
[System.Serializable]
public class DifficultySettings
{
    public float moveInterval;
    public float blockSpawnChance;
    public CornerBlockMode cornerMode;

    public DifficultySettings(float moveInterval, float blockSpawnChance, CornerBlockMode cornerMode)
    {
        this.moveInterval = moveInterval;
        this.blockSpawnChance = blockSpawnChance;
        this.cornerMode = cornerMode;
    }
}
