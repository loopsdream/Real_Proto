using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 스테이지 클리어 보상 패널 - 광고 보고 2배 받기 기능 포함
public class StageClearRewardPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("보상 표시 텍스트")]
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI doubleRewardText;

    [Header("버튼")]
    [SerializeField] private Button doubleRewardButton;   // 광고 보고 2배 받기
    [SerializeField] private Button claimButton;           // 그냥 받기
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button mainMenuButton;

    [Header("광고 불가 시 버튼 비활성화 색상")]
    [SerializeField] private Color adNotReadyColor = new Color(0.5f, 0.5f, 0.5f, 1f);

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
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveAllListeners();
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
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

        RegisterButtonListeners();
        UpdateRewardUI();
        UpdateAdButtonState();

        SetClaimButtonsVisible(true);

        // 다음/메인메뉴 버튼 숨김 (보상 선택 전까지)
        SetNavigationButtonsActive(false);

        Debug.Log("[StageClearRewardPanel] Panel shown.");
    }

    // 보상 텍스트 업데이트
    private void UpdateRewardUI()
    {
        if (rewardText == null || pendingRewards == null) return;

        string rewardStr = BuildRewardString(pendingRewards, 1);
        rewardText.text = rewardStr;

        if (doubleRewardText != null)
        {
            doubleRewardText.text = BuildRewardString(pendingRewards, 2);
        }
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

    // 광고 보고 2배 받기 버튼
    private void OnDoubleRewardButtonClicked()
    {
        if (rewardGranted) return;

        if (AdManager.Instance == null)
        {
            Debug.LogError("[StageClearRewardPanel] AdManager not found!");
            GrantReward(false);
            return;
        }

        // 버튼 비활성화 (중복 클릭 방지)
        SetAllButtonsInteractable(false);

        AdManager.Instance.ShowDoubleRewardedAd(
            onSuccess: () =>
            {
                Debug.Log("[StageClearRewardPanel] Ad watched - granting double reward.");
                GrantReward(true);
            },
            onFailed: () =>
            {
                Debug.Log("[StageClearRewardPanel] Ad failed - granting normal reward.");
                GrantReward(false);
            }
        );
    }

    // 그냥 받기 버튼
    private void OnClaimButtonClicked()
    {
        if (rewardGranted) return;
        GrantReward(false);
    }

    // 실제 보상 지급
    private void GrantReward(bool isDouble)
    {
        if (rewardGranted) return;
        rewardGranted = true;

        List<RewardItem> rewardsToGrant = pendingRewards;

        // 2배인 경우 amount를 2배로 복사
        if (isDouble && pendingRewards != null)
        {
            rewardsToGrant = new List<RewardItem>();
            foreach (var item in pendingRewards)
            {
                RewardItem doubled = new RewardItem
                {
                    rewardType = item.rewardType,
                    amount = item.amount * 2,
                    displayName = item.displayName,
                    icon = item.icon
                };
                rewardsToGrant.Add(doubled);
            }
            Debug.Log("[StageClearRewardPanel] Double reward applied.");
        }

        // StageGridManager에 지급 위임
        StageGridManager gridManager = Object.FindAnyObjectByType<StageGridManager>();
        if (gridManager != null)
        {
            gridManager.GrantRewardItems(rewardsToGrant);
        }

        // 보상 지급 후 받기 버튼들 숨기고 다음/메인메뉴 버튼 표시
        SetClaimButtonsVisible(false);
        SetNavigationButtonsActive(true);

        Debug.Log($"[StageClearRewardPanel] Reward granted. Double: {isDouble}");
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

    private void SetNavigationButtonsActive(bool active)
    {
        if (nextStageButton != null) nextStageButton.gameObject.SetActive(active);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(active);
    }

    // 받기/광고 버튼 자체를 숨김/표시
    private void SetClaimButtonsVisible(bool visible)
    {
        if (rewardText != null) rewardText.gameObject.SetActive(visible);
        if (doubleRewardText != null) doubleRewardText.gameObject.SetActive(visible);
        if (doubleRewardButton != null) doubleRewardButton.gameObject.SetActive(visible);
        if (claimButton != null) claimButton.gameObject.SetActive(visible);
    }

    // 광고 시청 중 중복 클릭 방지용 (버튼은 유지하되 입력만 막음)
    private void SetAllButtonsInteractable(bool interactable)
    {
        if (doubleRewardButton != null) doubleRewardButton.interactable = interactable;
        if (claimButton != null) claimButton.interactable = interactable;
    }
}