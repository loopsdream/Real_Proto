using UnityEngine;

[CreateAssetMenu(fileName = "InfiniteModeSettings", menuName = "Block Puzzle/Infinite Mode Settings")]
public class InfiniteModeSettings : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 9;
    public int gridHeight = 9;

    [Header("Time Settings")]
    public float initialTimeLimit = 60f;           // �ʱ� ���� �ð� (��)

    [Header("Difficulty Progression")]
    [Tooltip("1�ܰ� ���̵� ���� �ð� (��)")]
    public float easyDuration = 30f;
    [Tooltip("2�ܰ� ���̵� ���� �ð� (��)")]
    public float mediumDuration = 60f;
    // 3�ܰ�� �� ���� ���

    [Header("Easy Difficulty (0-30��)")]
    public float easyMoveInterval = 3f;            // ��� �̵�/���� ����
    public float easyBlockSpawnChance = 0.3f;      // ��� ���� Ȯ�� (30%)
    public CornerBlockMode easyCornerMode = CornerBlockMode.SingleCorner;

    [Header("Medium Difficulty (30-90��)")]
    public float mediumMoveInterval = 2f;          // ��� �̵�/���� ����
    public float mediumBlockSpawnChance = 0.5f;    // ��� ���� Ȯ�� (50%)
    public CornerBlockMode mediumCornerMode = CornerBlockMode.SingleCorner;

    [Header("Hard Difficulty (90�� ����)")]
    public float hardMoveInterval = 1.5f;          // ��� �̵�/���� ����
    public float hardBlockSpawnChance = 0.7f;      // ��� ���� Ȯ�� (70%)
    public CornerBlockMode hardCornerMode = CornerBlockMode.FourCorners;

    [Header("Reward Settings")]
    [Tooltip("2�� ��� �ı� �� ȹ�� ����")]
    public int score2Blocks = 10;
    [Tooltip("3�� ��� �ı� �� ȹ�� ����")]
    public int score3Blocks = 25;
    [Tooltip("4�� ��� �ı� �� ȹ�� ����")]
    public int score4Blocks = 50;

    [Header("Time Bonus")]
    [Tooltip("2�� ��� �ı� �� �߰� �ð�")]
    public float timeBonus2Blocks = 1f;
    [Tooltip("3�� ��� �ı� �� �߰� �ð�")]
    public float timeBonus3Blocks = 2f;
    [Tooltip("4�� ��� �ı� �� �߰� �ð�")]
    public float timeBonus4Blocks = 3f;

    [Header("Penalty")]
    [Tooltip("�ı��� ����� ���� �� �����Ǵ� �ð�")]
    public float timePenalty = 2f;

    [Header("Combo Bonus")]
    [Tooltip("�޺��� �߰� ���� (2�޺�~10�޺�)")]
    public int[] comboBonusScores = new int[9] { 5, 10, 20, 35, 55, 80, 110, 145, 185 };

    // ���̵��� ���� ��������
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

    // ��ƿ��Ƽ �޼���
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

// �𼭸� ��� ��� ������
public enum CornerBlockMode
{
    SingleCorner,   // �� �𼭸� �� 1ĭ���� ����
    FourCorners     // �� �𼭸� �� 4ĭ�� ���� (2x2 ����)
}

// ���̵��� ���� Ŭ����
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
