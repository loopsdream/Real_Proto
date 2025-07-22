// GameStarter.cs - 게임 시작 시 에너지 소모 처리
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

        // 에너지 확인
        if (UserDataManager.Instance.GetEnergy() >= energyCostPerGame)
        {
            // 에너지 소모 후 게임 시작
            if (UserDataManager.Instance.SpendEnergy(energyCostPerGame))
            {
                Debug.Log($"Game started! Energy spent: {energyCostPerGame}");
                SceneManager.LoadScene("GameScene");
            }
        }
        else
        {
            // 에너지 부족 알림
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
                    notEnoughEnergyText.text = $"에너지가 부족합니다!\n다음 충전까지: {timeString}";
                }
                else
                {
                    notEnoughEnergyText.text = "에너지가 부족합니다!";
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
        // 에너지 구매 로직 호출
        //FindObjectOfType<CurrencyUI>()?.OnEnergyButtonClicked();
        //CloseNotEnoughEnergyDialog();
    }
}