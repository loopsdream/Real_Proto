// GameStarter.cs - 게임 진입 시 에너지 체크 및 에너지 부족 패널 관리
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameStarter : MonoBehaviour
{
    [Header("Energy Cost")]
    public int energyCostPerGame = 1;

    [Header("Ad Button State")]
    public Color adNotReadyColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    // notEnoughEnergyPanel은 DontDestroyOnLoad인 CommonUIManager에서 런타임에 가져옴
    private GameObject notEnoughEnergyPanel => CommonUIManager.Instance?.notEnoughEnergyPanel;

    // 모든 UI 참조를 런타임에 패널에서 이름으로 찾음 (직렬화 참조 없음)
    private TextMeshProUGUI timerText;
    private Button watchAdButton;
    private Button goToShopButton;
    private Button closeButton;

    // 씬 전환 대기 중인 씬 이름
    private string pendingSceneName = "";

    // 스테이지 모드 진입
    public void StartStageMode()
    {
        TryStartGame("StageModeScene");
    }

    // 무한 모드 진입
    public void StartInfiniteMode()
    {
        TryStartGame("InfiniteModeScene");
    }

    // 에너지 체크 후 씬 전환
    private void TryStartGame(string sceneName)
    {
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("[GameStarter] UserDataManager not found!");
            return;
        }

        if (UserDataManager.Instance.GetEnergy() >= energyCostPerGame)
        {
            if (UserDataManager.Instance.SpendEnergy(energyCostPerGame))
            {
                Debug.Log($"[GameStarter] Energy spent. Loading {sceneName}");
                SceneManager.LoadScene(sceneName);
            }
        }
        else
        {
            pendingSceneName = sceneName;
            ShowNotEnoughEnergyPanel();
        }
    }

    private void ShowNotEnoughEnergyPanel()
    {
        if (notEnoughEnergyPanel == null)
        {
            Debug.LogWarning("[GameStarter] notEnoughEnergyPanel is null. CommonUIManager.Instance may not be ready.");
            return;
        }

        // 패널 활성화마다 자식 참조를 이름으로 재취득 (씬 재로드 대비)
        ResolveUIReferences();
        RegisterButtonListeners();

        notEnoughEnergyPanel.SetActive(true);
        UpdateTimerUI();
        UpdateAdButtonState();
    }

    // notEnoughEnergyPanel 자식에서 이름으로 참조 취득
    private void ResolveUIReferences()
    {
        Transform dialog = notEnoughEnergyPanel.transform.Find("DialogPanel");
        if (dialog == null)
        {
            // DialogPanel이 없으면 패널 직접에서 찾기
            dialog = notEnoughEnergyPanel.transform;
        }

        timerText = dialog.Find("EnergyTimer")?.GetComponent<TextMeshProUGUI>();
        watchAdButton = dialog.Find("ADButton")?.GetComponent<Button>();
        goToShopButton = dialog.Find("ShopButton")?.GetComponent<Button>();
        closeButton = dialog.Find("CloseButton")?.GetComponent<Button>();

        if (timerText == null) Debug.LogWarning("[GameStarter] EnergyTimer TMP not found in panel.");
        if (watchAdButton == null) Debug.LogWarning("[GameStarter] ADButton not found in panel.");
        if (goToShopButton == null) Debug.LogWarning("[GameStarter] ShopButton not found in panel.");
        if (closeButton == null) Debug.LogWarning("[GameStarter] CloseButton not found in panel.");
    }

    private void RegisterButtonListeners()
    {
        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnWatchAdButtonClicked);
        }
        if (goToShopButton != null)
        {
            goToShopButton.onClick.RemoveAllListeners();
            goToShopButton.onClick.AddListener(OnGoToShopButtonClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null || UserDataManager.Instance == null) return;

        TimeSpan timeUntilNext = UserDataManager.Instance.GetTimeUntilNextEnergy();
        timerText.text = timeUntilNext.TotalSeconds > 0
            ? string.Format("{0:D2}:{1:D2}", timeUntilNext.Minutes, timeUntilNext.Seconds)
            : "";
    }

    private void UpdateAdButtonState()
    {
        if (watchAdButton == null) return;

        bool adReady = AdManager.Instance != null && AdManager.Instance.IsEnergyRewardedAdReady();
        watchAdButton.interactable = adReady;

        Image btnImage = watchAdButton.GetComponent<Image>();
        if (btnImage != null)
            btnImage.color = adReady ? Color.white : adNotReadyColor;
    }

    // 광고 보고 에너지 리필
    private void OnWatchAdButtonClicked()
    {
        if (AdManager.Instance == null)
        {
            Debug.LogError("[GameStarter] AdManager not found!");
            return;
        }

        if (watchAdButton != null) watchAdButton.interactable = false;

        AdManager.Instance.ShowEnergyRewardedAd(
            onSuccess: () =>
            {
                Debug.Log("[GameStarter] Energy ad watched - refilling energy.");
                if (UserDataManager.Instance != null)
                    UserDataManager.Instance.AddEnergy(energyCostPerGame);

                ClosePanel();

                if (!string.IsNullOrEmpty(pendingSceneName))
                    TryStartGame(pendingSceneName);
            },
            onFailed: () =>
            {
                Debug.Log("[GameStarter] Energy ad failed.");
                UpdateAdButtonState();
            }
        );
    }

    // 상점으로 이동 (추후 구현)
    private void OnGoToShopButtonClicked()
    {
        Debug.Log("[GameStarter] Go to shop - not yet implemented.");
        // TODO: 상점 씬 또는 패널 열기
    }

    public void ClosePanel()
    {
        pendingSceneName = "";
        if (notEnoughEnergyPanel != null)
            notEnoughEnergyPanel.SetActive(false);
    }
}