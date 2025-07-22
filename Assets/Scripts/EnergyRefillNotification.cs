// EnergyRefillNotification.cs - ������ ���� �˸� �ý���
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
        // UserDataManager �̺�Ʈ ����
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnEnergyChanged += OnEnergyChanged;
        }

        // ���� ��ư �̺�Ʈ ����
        if (collectButton != null)
        {
            collectButton.onClick.AddListener(CollectEnergy);
        }

        // �ʱ⿡�� �˸� �г� ����
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
        // �������� �ڵ� �����Ǿ����� Ȯ��
        CheckForEnergyRefill();
    }

    void CheckForEnergyRefill()
    {
        if (UserDataManager.Instance == null) return;

        // �������� �ڵ� ���� ������ �������� Ȯ��
        TimeSpan timeUntilNext = UserDataManager.Instance.GetTimeUntilNextEnergy();
        if (timeUntilNext.TotalSeconds <= 0 &&
            UserDataManager.Instance.GetEnergy() < UserDataManager.Instance.GetMaxEnergy())
        {
            // ������ �ڵ� ���� ���� - �˸� ǥ��
            ShowEnergyRefillNotification();
        }
    }

    void ShowEnergyRefillNotification()
    {
        if (notificationPanel == null) return;

        // ���� ������ ������ �� ���
        int currentEnergy = UserDataManager.Instance.GetEnergy();
        int maxEnergy = UserDataManager.Instance.GetMaxEnergy();
        pendingEnergyRefill = maxEnergy - currentEnergy;

        if (pendingEnergyRefill <= 0) return;

        // �˸� �ؽ�Ʈ ����
        if (notificationText != null)
        {
            notificationText.text = $"������ {pendingEnergyRefill}���� �����Ǿ����ϴ�!";
        }

        // �˸� �г� ǥ�� �ִϸ��̼�
        notificationPanel.SetActive(true);
        notificationPanel.transform.localScale = Vector3.zero;

        LeanTween.scale(notificationPanel, Vector3.one, 0.5f)
            .setEase(showCurve)
            .setOnComplete(() => {
                // �ڵ����� ����� (���û���)
                Invoke(nameof(HideNotification), showDuration);
            });
    }

    void CollectEnergy()
    {
        if (pendingEnergyRefill > 0)
        {
            // ������ ����
            UserDataManager.Instance.AddEnergy(pendingEnergyRefill);
            pendingEnergyRefill = 0;

            // �˸� ����
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