using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 스테이지 클리어 보상 패널 - 광고 보고 2배 받기 기능 포함
public class StageClearRewardPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("버튼")]
    [SerializeField] private Button doubleRewardButton;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button closeButton;

    [Header("광고 불가 시 버튼 비활성화 색상")]
    [SerializeField] private Color adNotReadyColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [SerializeField] private TextMeshProUGUI energyText;

    // 현재 보상 데이터 저장
    private List<RewardItem> pendingRewards;

    // 보상 지급 완료 여부 (중복 지급 방지)
    private bool rewardGranted = false;

    void Awake()
    {
        RegisterButtonListeners();
    }

    private void RegisterButtonListeners()
    {
        if (doubleRewardButton != null)
        {
            doubleRewardButton.onClick.RemoveAllListeners();
            doubleRewardButton.onClick.AddListener(OnDoubleRewardButtonClicked);
        }
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveAllListeners();
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    // StageManager에서 호출
    public void Show(List<RewardItem> rewards)
    {
        pendingRewards = rewards;
        rewardGranted = false;

        // panelRoot가 지정되어 있으면 panelRoot를, 아니면 자기 자신을 활성화
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        // 패널 표시 시점에 호출
        if (energyText != null && UserDataManager.Instance != null)
        {
            int current = UserDataManager.Instance.GetEnergy();
            int max = UserDataManager.Instance.GetMaxEnergy();
            energyText.text = $"{current}/{max}";
        }

        RegisterButtonListeners();
        UpdateAdButtonState();

        // 기본 보상 즉시 지급
        GrantBaseReward();

        Debug.Log("[StageClearRewardPanel] Panel shown.");
    }

    private string BuildRewardString(List<RewardItem> rewards, int multiplier)
    {
        if (rewards == null || rewards.Count == 0)
            return "No Reward";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var item in rewards)
        {
            int amount = item.amount * multiplier;
            sb.AppendLine($"{item.displayName}: {amount}");
        }
        return sb.ToString().TrimEnd();
    }

    // 광고 준비 상태에 따라 버튼 상태 업데이트
    private void UpdateAdButtonState()
    {
        if (doubleRewardButton == null) return;

        bool adReady = AdManager.Instance != null && AdManager.Instance.IsDoubleRewardedAdReady();
        doubleRewardButton.interactable = adReady;

        Image btnImage = doubleRewardButton.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = adReady ? Color.white : adNotReadyColor;
        }
    }

    // 패널 열릴 때 기본 보상 즉시 지급
    private void GrantBaseReward()
    {
        if (pendingRewards == null) return;

        StageGridManager gridManager = Object.FindAnyObjectByType<StageGridManager>();
        if (gridManager != null)
        {
            gridManager.GrantRewardItems(pendingRewards);
        }

        Debug.Log("[StageClearRewardPanel] Base reward granted immediately.");
    }

    // 광고 보고 2배 받기 버튼
    private void OnDoubleRewardButtonClicked()
    {
        if (AdManager.Instance == null)
        {
            Debug.LogError("[StageClearRewardPanel] AdManager not found!");
            OnNextStageClicked();
            return;
        }

        // 버튼 비활성화 (광고 시청 중)
        SetAllButtonsInteractable(false);

        AdManager.Instance.ShowDoubleRewardedAd(
            onSuccess: () =>
            {
                Debug.Log("[StageClearRewardPanel] Ad watched - granting bonus reward.");
                // 추가 보상(기본 보상 1배 추가 지급)
                StageGridManager gridManager = Object.FindAnyObjectByType<StageGridManager>();
                if (gridManager != null)
                {
                    gridManager.GrantRewardItems(pendingRewards);
                }
                OnNextStageClicked();
            },
            onFailed: () =>
            {
                Debug.Log("[StageClearRewardPanel] Ad failed - going to next stage.");
                SetAllButtonsInteractable(true);
            }
        );
    }
    
    private void OnNextStageClicked()
    {
        Debug.Log("[StageClearRewardPanel] Next stage clicked.");
        // 기존 StageManager의 다음 스테이지 로드 로직 호출
        StageManager stageManager = Object.FindAnyObjectByType<StageManager>();
        if (stageManager != null)
        {
            stageManager.LoadNextStage();
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[StageClearRewardPanel] Main menu clicked.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    // 광고 시청 중 모든 버튼 비활성화 (중복 터치 방지)
    private void SetAllButtonsInteractable(bool interactable)
    {
        if (doubleRewardButton != null) doubleRewardButton.interactable = interactable;
        if (nextStageButton != null) nextStageButton.interactable = interactable;
    }
}