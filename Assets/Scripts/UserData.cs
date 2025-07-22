// ������ ���� Ŭ������
using System.Collections.Generic;

[System.Serializable]
public class UserData
{
    public PlayerInfo playerInfo;
    public Currencies currencies;
    public Dictionary<string, StageProgress> stageProgress;

    public UserData()
    {
        playerInfo = new PlayerInfo();
        currencies = new Currencies();
        stageProgress = new Dictionary<string, StageProgress>();
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string playerName = "Player";
    public int level = 1;
    public int currentStage = 1;
    public string lastLoginTime;
}

[System.Serializable]
public class Currencies
{
    public int gameCoins = 100; // ���� �� �⺻ ����
    public int diamonds = 5;    // ���� �� �⺻ ���̾Ƹ��
    public int energy = 5;      // ���� �� �ִ� ������
    public int maxEnergy = 5;
    public string lastEnergyTime;
}

[System.Serializable]
public class StageProgress
{
    public int bestScore = 0;
    public bool completed = false;
}