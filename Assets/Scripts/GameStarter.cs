// GameStarter.cs - ���� ���� �� ������ �Ҹ� ó��
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStarter : MonoBehaviour
{
    [Header("Energy Cost")]
    public int energyCostPerGame = 1;

    [Header("UI References")]
    public GameObject notEnoughEnergyPanel;
    public TMPro.TextMeshProUGUI notEnoughEnergyText;

    public void StartGame()
    {
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager not found!");
            return;
        }

        // ������ Ȯ��
        if (UserDataManager.Instance.GetEnergy() >= energyCostPerGame)
        {
            // ������ �Ҹ� �� ���� ����
            if (UserDataManager.Instance.SpendEnergy(energyCostPerGame))
            {
                Debug.Log($"Game started! Energy spent: {energyCostPerGame}");
                SceneManager.LoadScene("GameScene");
            }
        }
        else
        {
            // ������ ���� �˸�
            ShowNotEnoughEnergyDialog();
        }
    }

    void ShowNotEnoughEnergyDialog()
    {
        if (notEnoughEnergyPanel != null)
        {
            notEnoughEnergyPanel.SetActive(true);

            if (notEnoughEnergyText != null)
            {
                TimeSpan timeUntilNext = UserDataManager.Instance.GetTimeUntilNextEnergy();
                if (timeUntilNext.TotalSeconds > 0)
                {
                    string timeString = string.Format("{0:D2}:{1:D2}",
                        timeUntilNext.Minutes,
                        timeUntilNext.Seconds);
                    notEnoughEnergyText.text = $"�������� �����մϴ�!\n���� ��������: {timeString}";
                }
                else
                {
                    notEnoughEnergyText.text = "�������� �����մϴ�!";
                }
            }
        }
    }

    public void CloseNotEnoughEnergyDialog()
    {
        if (notEnoughEnergyPanel != null)
        {
            notEnoughEnergyPanel.SetActive(false);
        }
    }

    public void PurchaseEnergyButton()
    {
        // ������ ���� ���� ȣ��
        //FindObjectOfType<CurrencyUI>()?.OnEnergyButtonClicked();
        //CloseNotEnoughEnergyDialog();
    }
}