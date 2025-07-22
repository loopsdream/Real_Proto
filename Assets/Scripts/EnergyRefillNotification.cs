// EnergyRefillNotification.cs - 에너지 충전 알림 시스템
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EnergyRefillNotification : MonoBehaviour
{
    [Header("Notification UI")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    public Button collectButton;

    [Header("Animation")]
    public float showDuration = 3f;
    public AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int pendingEnergyRefill = 0;

    void Start()
    {
        // UserDataManager 이벤트 구독
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnEnergyChanged += OnEnergyChanged;
        }

        // 수집 버튼 이벤트 연결
        if (collectButton != null)
        {
            collectButton.onClick.AddListener(CollectEnergy);
        }

        // 초기에는 알림 패널 숨김
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnEnergyChanged -= OnEnergyChanged;
        }
    }

    void OnEnergyChanged(int newEnergy)
    {
        // 에너지가 자동 충전되었는지 확인
        CheckForEnergyRefill();
    }

    void CheckForEnergyRefill()
    {
        if (UserDataManager.Instance == null) return;

        // 에너지가 자동 충전 가능한 상태인지 확인
        TimeSpan timeUntilNext = UserDataManager.Instance.GetTimeUntilNextEnergy();
        if (timeUntilNext.TotalSeconds <= 0 &&
            UserDataManager.Instance.GetEnergy() < UserDataManager.Instance.GetMaxEnergy())
        {
            // 에너지 자동 충전 가능 - 알림 표시
            ShowEnergyRefillNotification();
        }
    }

    void ShowEnergyRefillNotification()
    {
        if (notificationPanel == null) return;

        // 충전 가능한 에너지 양 계산
        int currentEnergy = UserDataManager.Instance.GetEnergy();
        int maxEnergy = UserDataManager.Instance.GetMaxEnergy();
        pendingEnergyRefill = maxEnergy - currentEnergy;

        if (pendingEnergyRefill <= 0) return;

        // 알림 텍스트 설정
        if (notificationText != null)
        {
            notificationText.text = $"에너지 {pendingEnergyRefill}개가 충전되었습니다!";
        }

        // 알림 패널 표시 애니메이션
        notificationPanel.SetActive(true);
        notificationPanel.transform.localScale = Vector3.zero;

        LeanTween.scale(notificationPanel, Vector3.one, 0.5f)
            .setEase(showCurve)
            .setOnComplete(() => {
                // 자동으로 숨기기 (선택사항)
                Invoke(nameof(HideNotification), showDuration);
            });
    }

    void CollectEnergy()
    {
        if (pendingEnergyRefill > 0)
        {
            // 에너지 수집
            UserDataManager.Instance.AddEnergy(pendingEnergyRefill);
            pendingEnergyRefill = 0;

            // 알림 숨김
            HideNotification();
        }
    }

    void HideNotification()
    {
        if (notificationPanel != null && notificationPanel.activeInHierarchy)
        {
            LeanTween.scale(notificationPanel, Vector3.zero, 0.3f)
                .setEase(showCurve)
                .setOnComplete(() => {
                    notificationPanel.SetActive(false);
                });
        }
    }
}