using UnityEngine;

[CreateAssetMenu(fileName = "InfiniteModeSettings", menuName = "Block Puzzle/Infinite Mode Settings")]
public class InfiniteModeSettings : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 9;
    public int gridHeight = 9;

    [Header("Time Settings")]
    public float initialTimeLimit = 60f;           // �ʱ� ���� �ð� (��)

    [Header("Difficulty Levels")]
    public DifficultyLevel[] difficultyLevels = new DifficultyLevel[]
    {
        new DifficultyLevel { difficultyName = "1 Phase", startTime = 0f, moveInterval = 6f, minSpawnChance = 0.1f, maxSpawnChance = 0.2f, cornerMode = CornerBlockMode.SingleCorner, bonusScoreMultiplier = 1.0f },
        new DifficultyLevel { difficultyName = "2 Phase", startTime = 30f, moveInterval = 6f, minSpawnChance = 0.2f, maxSpawnChance = 0.3f, cornerMode = CornerBlockMode.SingleCorner, bonusScoreMultiplier = 1.2f },
        new DifficultyLevel { difficultyName = "3 Phase", startTime = 90f, moveInterval = 6f, minSpawnChance = 0.3f, maxSpawnChance = 0.4f, cornerMode = CornerBlockMode.FourCorners, bonusScoreMultiplier = 1.4f },
        new DifficultyLevel { difficultyName = "4 Phase", startTime = 120f, moveInterval = 6f, minSpawnChance = 0.4f, maxSpawnChance = 0.5f, cornerMode = CornerBlockMode.FourCorners, bonusScoreMultiplier = 1.6f }
    };

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

    [System.Serializable]
    public class DifficultyLevel
    {
        public string difficultyName = "Easy";
        public float startTime = 0f;  // �� ���̵��� ���۵Ǵ� �ð� (��)
        public float moveInterval = 3f;

        [Header("Block Spawn Settings")]
        [Range(0f, 1f)]
        public float minSpawnChance = 0.25f;  // �ּ� ���� Ȯ��
        [Range(0f, 1f)]
        public float maxSpawnChance = 0.5f;   // �ִ� ���� Ȯ��

        public CornerBlockMode cornerMode = CornerBlockMode.SingleCorner;

        [Header("Score Bonus")]
        public float bonusScoreMultiplier = 1.0f;  // �⺻ ������ �������� ���ʽ� ���
    }

    // ���̵��� ���� ��������
    public DifficultyLevel GetCurrentDifficulty(float gameTime)
    {
        // �������� �˻��Ͽ� ���� �ʰ� �����ϴ� ���̵����� Ȯ��
        for (int i = difficultyLevels.Length - 1; i >= 0; i--)
        {
            if (gameTime >= difficultyLevels[i].startTime)
            {
                Debug.Log($"Game time: {gameTime}s, Current difficulty: {difficultyLevels[i].difficultyName}");
                return difficultyLevels[i];
            }
        }

        Debug.Log($"Game time: {gameTime}s, Current difficulty: {difficultyLevels[0].difficultyName}");

        // �⺻������ ù ��° ���̵� ��ȯ
        return difficultyLevels[0];
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

