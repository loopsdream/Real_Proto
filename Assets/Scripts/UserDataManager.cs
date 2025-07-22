// UserDataManager.cs - ���� ������ ���� �ý���
using System;
using System.Collections.Generic;
using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance;

    [Header("Energy Settings")]
    public int maxEnergy = 5;
    public int energyRechargeTimeMinutes = 30; // 30�и��� 1 ������ ����

    [Header("Events")]
    public System.Action<int> OnGameCoinsChanged;
    public System.Action<int> OnDiamondsChanged;
    public System.Action<int> OnEnergyChanged;
    public System.Action<int> OnPlayerLevelChanged;

    private UserData currentUserData;
    private const string SAVE_KEY = "UserData";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadUserData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // ���� ���� �� ������ �ڵ� ���� Ȯ��
        UpdateEnergyFromTime();

        // �ֱ������� ������ ������Ʈ (1�и���)
        InvokeRepeating(nameof(UpdateEnergyFromTime), 60f, 60f);
    }

    #region ������ �ε�/����

    void LoadUserData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string jsonData = PlayerPrefs.GetString(SAVE_KEY);
            try
            {
                currentUserData = JsonUtility.FromJson<UserData>(jsonData);
                Debug.Log("User data loaded successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load user data: " + e.Message);
                CreateNewUserData();
            }
        }
        else
        {
            CreateNewUserData();
        }

        // �ε� �� �̺�Ʈ �߻�
        InvokeAllEvents();
    }

    void CreateNewUserData()
    {
        currentUserData = new UserData();
        currentUserData.playerInfo.lastLoginTime = DateTime.Now.ToBinary().ToString();
        currentUserData.currencies.lastEnergyTime = DateTime.Now.ToBinary().ToString();
        currentUserData.currencies.maxEnergy = maxEnergy;

        SaveUserData();
        Debug.Log("New user data created");
    }

    public void SaveUserData()
    {
        try
        {
            currentUserData.playerInfo.lastLoginTime = DateTime.Now.ToBinary().ToString();
            string jsonData = JsonUtility.ToJson(currentUserData, true);
            PlayerPrefs.SetString(SAVE_KEY, jsonData);
            PlayerPrefs.Save();
            Debug.Log("User data saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save user data: " + e.Message);
        }
    }

    #endregion

    #region ���� ���� ����

    public int GetGameCoins()
    {
        return currentUserData.currencies.gameCoins;
    }

    public bool SpendGameCoins(int amount)
    {
        if (currentUserData.currencies.gameCoins >= amount)
        {
            currentUserData.currencies.gameCoins -= amount;
            OnGameCoinsChanged?.Invoke(currentUserData.currencies.gameCoins);
            SaveUserData();
            return true;
        }
        return false;
    }

    public void AddGameCoins(int amount)
    {
        currentUserData.currencies.gameCoins += amount;
        OnGameCoinsChanged?.Invoke(currentUserData.currencies.gameCoins);
        SaveUserData();
        Debug.Log($"Added {amount} game coins. Total: {currentUserData.currencies.gameCoins}");
    }

    #endregion

    #region ���̾Ƹ�� ����

    public int GetDiamonds()
    {
        return currentUserData.currencies.diamonds;
    }

    public bool SpendDiamonds(int amount)
    {
        if (currentUserData.currencies.diamonds >= amount)
        {
            currentUserData.currencies.diamonds -= amount;
            OnDiamondsChanged?.Invoke(currentUserData.currencies.diamonds);
            SaveUserData();
            return true;
        }
        return false;
    }

    public void AddDiamonds(int amount)
    {
        currentUserData.currencies.diamonds += amount;
        OnDiamondsChanged?.Invoke(currentUserData.currencies.diamonds);
        SaveUserData();
        Debug.Log($"Added {amount} diamonds. Total: {currentUserData.currencies.diamonds}");
    }

    #endregion

    #region ������ ����

    public int GetEnergy()
    {
        return currentUserData.currencies.energy;
    }

    public int GetMaxEnergy()
    {
        return currentUserData.currencies.maxEnergy;
    }

    public bool SpendEnergy(int amount = 1)
    {
        if (currentUserData.currencies.energy >= amount)
        {
            currentUserData.currencies.energy -= amount;
            OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
            SaveUserData();
            return true;
        }
        return false;
    }

    public void AddEnergy(int amount)
    {
        currentUserData.currencies.energy = Mathf.Min(
            currentUserData.currencies.energy + amount,
            currentUserData.currencies.maxEnergy
        );
        OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
        SaveUserData();
        Debug.Log($"Added {amount} energy. Current: {currentUserData.currencies.energy}");
    }

    public void UpdateEnergyFromTime()
    {
        if (currentUserData.currencies.energy >= currentUserData.currencies.maxEnergy)
            return;

        try
        {
            DateTime lastEnergyTime = DateTime.FromBinary(Convert.ToInt64(currentUserData.currencies.lastEnergyTime));
            DateTime currentTime = DateTime.Now;

            TimeSpan timeDifference = currentTime - lastEnergyTime;
            int minutesPassed = (int)timeDifference.TotalMinutes;

            if (minutesPassed >= energyRechargeTimeMinutes)
            {
                int energyToAdd = minutesPassed / energyRechargeTimeMinutes;
                int newEnergy = Mathf.Min(
                    currentUserData.currencies.energy + energyToAdd,
                    currentUserData.currencies.maxEnergy
                );

                if (newEnergy > currentUserData.currencies.energy)
                {
                    currentUserData.currencies.energy = newEnergy;

                    // ������ ������ �ð� ������Ʈ
                    DateTime newLastEnergyTime = lastEnergyTime.AddMinutes(energyToAdd * energyRechargeTimeMinutes);
                    currentUserData.currencies.lastEnergyTime = newLastEnergyTime.ToBinary().ToString();

                    OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
                    SaveUserData();

                    Debug.Log($"Energy recharged: +{energyToAdd}, Current: {currentUserData.currencies.energy}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error updating energy from time: " + e.Message);
            // ���� �߻� �� ���� �ð����� ����
            currentUserData.currencies.lastEnergyTime = DateTime.Now.ToBinary().ToString();
            SaveUserData();
        }
    }

    public TimeSpan GetTimeUntilNextEnergy()
    {
        if (currentUserData.currencies.energy >= currentUserData.currencies.maxEnergy)
            return TimeSpan.Zero;

        try
        {
            DateTime lastEnergyTime = DateTime.FromBinary(Convert.ToInt64(currentUserData.currencies.lastEnergyTime));
            DateTime nextEnergyTime = lastEnergyTime.AddMinutes(energyRechargeTimeMinutes);
            DateTime currentTime = DateTime.Now;

            if (nextEnergyTime > currentTime)
            {
                return nextEnergyTime - currentTime;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error calculating next energy time: " + e.Message);
        }

        return TimeSpan.Zero;
    }

    #endregion

    #region �÷��̾� ���� ����

    public string GetPlayerName()
    {
        return currentUserData.playerInfo.playerName;
    }

    public void SetPlayerName(string name)
    {
        currentUserData.playerInfo.playerName = name;
        SaveUserData();
    }

    public int GetPlayerLevel()
    {
        return currentUserData.playerInfo.level;
    }

    public void SetPlayerLevel(int level)
    {
        currentUserData.playerInfo.level = level;
        OnPlayerLevelChanged?.Invoke(level);
        SaveUserData();
    }

    public int GetCurrentStage()
    {
        return currentUserData.playerInfo.currentStage;
    }

    public void SetCurrentStage(int stage)
    {
        currentUserData.playerInfo.currentStage = Mathf.Max(currentUserData.playerInfo.currentStage, stage);
        SaveUserData();
    }

    #endregion

    #region �������� ���൵ ����

    public StageProgress GetStageProgress(int stageNumber)
    {
        string stageKey = "stage" + stageNumber;
        if (currentUserData.stageProgress.ContainsKey(stageKey))
        {
            return currentUserData.stageProgress[stageKey];
        }

        return new StageProgress();
    }

    public void UpdateStageProgress(int stageNumber, int score, bool completed)
    {
        string stageKey = "stage" + stageNumber;

        if (!currentUserData.stageProgress.ContainsKey(stageKey))
        {
            currentUserData.stageProgress[stageKey] = new StageProgress();
        }

        StageProgress progress = currentUserData.stageProgress[stageKey];
        progress.bestScore = Mathf.Max(progress.bestScore, score);

        if (completed && !progress.completed)
        {
            progress.completed = true;

            // �� �������� Ŭ���� �� ���� �������� �ر�
            SetCurrentStage(stageNumber + 1);

            // ������ ���� (��: 5������������ ������)
            if (stageNumber % 5 == 0)
            {
                SetPlayerLevel(GetPlayerLevel() + 1);
            }
        }

        currentUserData.stageProgress[stageKey] = progress;
        SaveUserData();

        Debug.Log($"Stage {stageNumber} progress updated: Score={score}, Completed={completed}");
    }

    #endregion

    #region �������� �Ϸ� ����

    public void GiveStageReward(int stageNumber, int score)
    {
        // �⺻ ���� ���� (���� ���)
        int coinReward = score / 10;
        AddGameCoins(coinReward);

        // ù Ŭ���� ���ʽ�
        if (!GetStageProgress(stageNumber).completed)
        {
            int bonusCoins = 50; // ù Ŭ���� ���ʽ�
            AddGameCoins(bonusCoins);

            // Ư�� ������������ ���̾Ƹ�� ����
            if (stageNumber % 10 == 0) // 10, 20, 30... ��������
            {
                AddDiamonds(1);
            }
        }
    }

    #endregion

    #region ���� ���� (IAP ���� �غ�)

    public void PurchaseDiamonds(int amount)
    {
        // ���� IAP ���� �� ȣ��� �޼���
        AddDiamonds(amount);
        Debug.Log($"Purchased {amount} diamonds via IAP");
    }

    public bool PurchaseEnergyWithDiamonds(int diamondCost, int energyAmount)
    {
        if (SpendDiamonds(diamondCost))
        {
            AddEnergy(energyAmount);
            return true;
        }
        return false;
    }

    #endregion

    #region ��ƿ��Ƽ

    void InvokeAllEvents()
    {
        OnGameCoinsChanged?.Invoke(currentUserData.currencies.gameCoins);
        OnDiamondsChanged?.Invoke(currentUserData.currencies.diamonds);
        OnEnergyChanged?.Invoke(currentUserData.currencies.energy);
        OnPlayerLevelChanged?.Invoke(currentUserData.playerInfo.level);
    }

    public void ResetUserData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        CreateNewUserData();
        InvokeAllEvents();
        Debug.Log("User data reset");
    }

    #endregion
}