// CurrencyUI.cs - 재화 표시 UI 관리
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
        // UserDataManager 이벤트 구독
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnGameCoinsChanged += UpdateGameCoinsDisplay;
            UserDataManager.Instance.OnDiamondsChanged += UpdateDiamondsDisplay;
            UserDataManager.Instance.OnEnergyChanged += UpdateEnergyDisplay;
            UserDataManager.Instance.OnPlayerLevelChanged += UpdatePlayerLevelDisplay;

            // 초기 값 설정
            UpdateAllDisplays();
        }

        // 에너지 타이머 업데이트 (1초마다)
        InvokeRepeating(nameof(UpdateEnergyTimer), 1f, 1f);
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
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
            // 에너지가 최대가 아니고 충전 대기 중일 때 타이머 표시
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
            // 에너지가 최대이거나 즉시 충전 가능할 때 타이머 숨김
            if (energyTimerPanel != null)
                energyTimerPanel.SetActive(false);
        }
    }

    void AnimateText(TextMeshProUGUI text)
    {
        if (text != null)
        {
            // 간단한 펄스 애니메이션
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

    // 버튼 이벤트용 메서드들
    public void OnGameCoinsButtonClicked()
    {
        // 게임 코인 상점이나 정보 표시
        Debug.Log("Game Coins button clicked");
    }

    public void OnDiamondsButtonClicked()
    {
        // 다이아몬드 상점 열기
        Debug.Log("Diamonds button clicked - Open shop");
    }

    public void OnEnergyButtonClicked()
    {
        // 에너지 구매 또는 정보 표시
        Debug.Log("Energy button clicked");
        ShowEnergyPurchaseDialog();
    }

    void ShowEnergyPurchaseDialog()
    {
        // 에너지 구매 다이얼로그 표시 로직
        // 예: 다이아몬드 10개로 에너지 전체 충전
        int diamondCost = 10;
        int energyToAdd = UserDataManager.Instance.GetMaxEnergy() - UserDataManager.Instance.GetEnergy();

        if (energyToAdd <= 0)
        {
            Debug.Log("Energy is already full!");
            return;
        }

        // 간단한 확인 다이얼로그 (실제로는 UI 패널 사용)
        if (UserDataManager.Instance.GetDiamonds() >= diamondCost)
        {
            Debug.Log($"Purchase {energyToAdd} energy for {diamondCost} diamonds?");
            // 실제 구현에서는 확인 팝업 표시
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

    // 공통 UI에서 사용할 새로운 메서드들
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
        // 강제로 모든 이벤트 재구독 및 업데이트
        OnDestroy(); // 기존 구독 해제
        Start();     // 재구독 및 초기화
    }
}