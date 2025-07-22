// CurrencyUI.cs - ��ȭ ǥ�� UI ����
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CurrencyUI : MonoBehaviour
{
    [Header("Currency Display")]
    public TextMeshProUGUI gameCoinsText;
    public TextMeshProUGUI diamondsText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI playerLevelText;

    [Header("Energy Timer")]
    public TextMeshProUGUI energyTimerText;
    public GameObject energyTimerPanel;

    [Header("Animation")]
    public float animationDuration = 0.5f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Start()
    {
        // UserDataManager �̺�Ʈ ����
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnGameCoinsChanged += UpdateGameCoinsDisplay;
            UserDataManager.Instance.OnDiamondsChanged += UpdateDiamondsDisplay;
            UserDataManager.Instance.OnEnergyChanged += UpdateEnergyDisplay;
            UserDataManager.Instance.OnPlayerLevelChanged += UpdatePlayerLevelDisplay;

            // �ʱ� �� ����
            UpdateAllDisplays();
        }

        // ������ Ÿ�̸� ������Ʈ (1�ʸ���)
        InvokeRepeating(nameof(UpdateEnergyTimer), 1f, 1f);
    }

    void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnGameCoinsChanged -= UpdateGameCoinsDisplay;
            UserDataManager.Instance.OnDiamondsChanged -= UpdateDiamondsDisplay;
            UserDataManager.Instance.OnEnergyChanged -= UpdateEnergyDisplay;
            UserDataManager.Instance.OnPlayerLevelChanged -= UpdatePlayerLevelDisplay;
        }
    }

    void UpdateAllDisplays()
    {
        if (UserDataManager.Instance == null) return;

        UpdateGameCoinsDisplay(UserDataManager.Instance.GetGameCoins());
        UpdateDiamondsDisplay(UserDataManager.Instance.GetDiamonds());
        UpdateEnergyDisplay(UserDataManager.Instance.GetEnergy());
        UpdatePlayerLevelDisplay(UserDataManager.Instance.GetPlayerLevel());
    }

    void UpdateGameCoinsDisplay(int coins)
    {
        if (gameCoinsText != null)
        {
            gameCoinsText.text = FormatNumber(coins);
            AnimateText(gameCoinsText);
        }
    }

    void UpdateDiamondsDisplay(int diamonds)
    {
        if (diamondsText != null)
        {
            diamondsText.text = diamonds.ToString();
            AnimateText(diamondsText);
        }
    }

    void UpdateEnergyDisplay(int energy)
    {
        if (energyText != null)
        {
            int maxEnergy = UserDataManager.Instance.GetMaxEnergy();
            energyText.text = $"{energy}/{maxEnergy}";
            AnimateText(energyText);
        }

        UpdateEnergyTimer();
    }

    void UpdatePlayerLevelDisplay(int level)
    {
        if (playerLevelText != null)
        {
            playerLevelText.text = $"Lv.{level}";
            AnimateText(playerLevelText);
        }
    }

    void UpdateEnergyTimer()
    {
        if (UserDataManager.Instance == null) return;

        TimeSpan timeUntilNext = UserDataManager.Instance.GetTimeUntilNextEnergy();

        if (timeUntilNext.TotalSeconds > 0 && UserDataManager.Instance.GetEnergy() < UserDataManager.Instance.GetMaxEnergy())
        {
            // �������� �ִ밡 �ƴϰ� ���� ��� ���� �� Ÿ�̸� ǥ��
            if (energyTimerPanel != null)
                energyTimerPanel.SetActive(true);

            if (energyTimerText != null)
            {
                string timeString = string.Format("{0:D2}:{1:D2}",
                    timeUntilNext.Minutes,
                    timeUntilNext.Seconds);
                energyTimerText.text = timeString;
            }
        }
        else
        {
            // �������� �ִ��̰ų� ��� ���� ������ �� Ÿ�̸� ����
            if (energyTimerPanel != null)
                energyTimerPanel.SetActive(false);
        }
    }

    void AnimateText(TextMeshProUGUI text)
    {
        if (text != null)
        {
            // ������ �޽� �ִϸ��̼�
            LeanTween.cancel(text.gameObject);
            LeanTween.scale(text.gameObject, Vector3.one * 1.2f, animationDuration * 0.5f)
                .setEase(animationCurve)
                .setOnComplete(() => {
                    LeanTween.scale(text.gameObject, Vector3.one, animationDuration * 0.5f)
                        .setEase(animationCurve);
                });
        }
    }

    string FormatNumber(int number)
    {
        if (number >= 1000000)
            return (number / 1000000f).ToString("0.0") + "M";
        else if (number >= 1000)
            return (number / 1000f).ToString("0.0") + "K";
        else
            return number.ToString();
    }

    // ��ư �̺�Ʈ�� �޼����
    public void OnGameCoinsButtonClicked()
    {
        // ���� ���� �����̳� ���� ǥ��
        Debug.Log("Game Coins button clicked");
    }

    public void OnDiamondsButtonClicked()
    {
        // ���̾Ƹ�� ���� ����
        Debug.Log("Diamonds button clicked - Open shop");
    }

    public void OnEnergyButtonClicked()
    {
        // ������ ���� �Ǵ� ���� ǥ��
        Debug.Log("Energy button clicked");
        ShowEnergyPurchaseDialog();
    }

    void ShowEnergyPurchaseDialog()
    {
        // ������ ���� ���̾�α� ǥ�� ����
        // ��: ���̾Ƹ�� 10���� ������ ��ü ����
        int diamondCost = 10;
        int energyToAdd = UserDataManager.Instance.GetMaxEnergy() - UserDataManager.Instance.GetEnergy();

        if (energyToAdd <= 0)
        {
            Debug.Log("Energy is already full!");
            return;
        }

        // ������ Ȯ�� ���̾�α� (�����δ� UI �г� ���)
        if (UserDataManager.Instance.GetDiamonds() >= diamondCost)
        {
            Debug.Log($"Purchase {energyToAdd} energy for {diamondCost} diamonds?");
            // ���� ���������� Ȯ�� �˾� ǥ��
            PurchaseEnergyWithDiamonds(diamondCost, energyToAdd);
        }
        else
        {
            Debug.Log("Not enough diamonds!");
        }
    }

    void PurchaseEnergyWithDiamonds(int diamondCost, int energyAmount)
    {
        if (UserDataManager.Instance.PurchaseEnergyWithDiamonds(diamondCost, energyAmount))
        {
            Debug.Log($"Successfully purchased {energyAmount} energy for {diamondCost} diamonds");
        }
        else
        {
            Debug.Log("Purchase failed - not enough diamonds");
        }
    }

    // ���� UI���� ����� ���ο� �޼����
    public void RefreshAllDisplays()
    {
        if (UserDataManager.Instance != null)
        {
            UpdateGameCoinsDisplay(UserDataManager.Instance.GetGameCoins());
            UpdateDiamondsDisplay(UserDataManager.Instance.GetDiamonds());
            UpdateEnergyDisplay(UserDataManager.Instance.GetEnergy());
            UpdatePlayerLevelDisplay(UserDataManager.Instance.GetPlayerLevel());
            Debug.Log("Currency UI refreshed");
        }
    }

    public void ForceUpdateAll()
    {
        // ������ ��� �̺�Ʈ �籸�� �� ������Ʈ
        OnDestroy(); // ���� ���� ����
        Start();     // �籸�� �� �ʱ�ȭ
    }
}