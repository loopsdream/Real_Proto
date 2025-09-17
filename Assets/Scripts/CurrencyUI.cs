// CurrencyUI.cs - ��� ǥ�õǴ� ��ȭ UI �ý���
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class CurrencyUI : MonoBehaviour
{
    [Header("Currency Display Components")]
    public TextMeshProUGUI gameCoinsText;
    public TextMeshProUGUI diamondsText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI playerLevelText;

    [Header("Currency Icons")]
    public Image gameCoinsIcon;
    public Image diamondsIcon;
    public Image energyIcon;
    public Button gameCoinsButton;
    public Button diamondsButton;
    public Button energyButton;

    [Header("Energy Timer")]
    public TextMeshProUGUI energyTimerText;
    public GameObject energyTimerPanel;
    public Image energyFillBar; // ������ ���� �����

    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public float scaleMultiplier = 1.15f;
    public AnimationCurve scaleAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Number Formatting")]
    public bool useShortFormat = true; // K, M ���� ��� ����
    public bool showChangeAnimation = true; // ��ġ ���� �ִϸ��̼�

    [Header("Visual Effects")]
    public GameObject coinGainEffect;
    public GameObject diamondGainEffect;
    public GameObject energyGainEffect;
    public Color gainColor = Color.green;
    public Color lossColor = Color.red;

    // ���� �� ���� (���� ������)
    private int previousCoins = 0;
    private int previousDiamonds = 0;
    private int previousEnergy = 0;
    private int previousLevel = 0;

    // �ִϸ��̼� �ڷ�ƾ ����
    private Coroutine coinsAnimationCoroutine;
    private Coroutine diamondsAnimationCoroutine;
    private Coroutine energyAnimationCoroutine;

    void Start()
    {
        InitializeCurrencyUI();
        ConnectToUserDataManager();
        SetupButtonEvents();
        StartEnergyTimer();
    }

    void OnDestroy()
    {
        DisconnectFromUserDataManager();
    }

    #region �ʱ�ȭ �� ����

    void InitializeCurrencyUI()
    {
        // �ʱ� UI ���� ����
        if (energyTimerPanel != null)
            energyTimerPanel.SetActive(false);

        // �⺻������ �ʱ�ȭ
        if (gameCoinsText != null) gameCoinsText.text = "0";
        if (diamondsText != null) diamondsText.text = "0";
        if (energyText != null) energyText.text = "0/5";
        if (playerLevelText != null) playerLevelText.text = "Lv.1";

        Debug.Log("[CurrencyUI] �ʱ�ȭ �Ϸ�");
    }

    void ConnectToUserDataManager()
    {
        if (UserDataManager.Instance != null)
        {
            // �̺�Ʈ ����
            UserDataManager.Instance.OnGameCoinsChanged += OnGameCoinsChanged;
            UserDataManager.Instance.OnDiamondsChanged += OnDiamondsChanged;
            UserDataManager.Instance.OnEnergyChanged += OnEnergyChanged;
            UserDataManager.Instance.OnPlayerLevelChanged += OnPlayerLevelChanged;

            // ���� ������ �ʱ� ������Ʈ
            RefreshAllDisplays();
            Debug.Log("[CurrencyUI] UserDataManager ���� �Ϸ�");
        }
        else
        {
            Debug.LogWarning("[CurrencyUI] UserDataManager�� ã�� �� �����ϴ�.");
        }
    }

    void DisconnectFromUserDataManager()
    {
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.OnGameCoinsChanged -= OnGameCoinsChanged;
            UserDataManager.Instance.OnDiamondsChanged -= OnDiamondsChanged;
            UserDataManager.Instance.OnEnergyChanged -= OnEnergyChanged;
            UserDataManager.Instance.OnPlayerLevelChanged -= OnPlayerLevelChanged;
        }
    }

    void SetupButtonEvents()
    {
        if (gameCoinsButton != null)
            gameCoinsButton.onClick.AddListener(OnGameCoinsButtonClicked);

        if (diamondsButton != null)
            diamondsButton.onClick.AddListener(OnDiamondsButtonClicked);

        if (energyButton != null)
            energyButton.onClick.AddListener(OnEnergyButtonClicked);
    }

    #endregion

    #region ��ȭ ������Ʈ �̺�Ʈ �ڵ鷯

    void OnGameCoinsChanged(int newCoins)
    {
        if (gameCoinsText == null) return;

        bool isIncrease = newCoins > previousCoins;

        if (showChangeAnimation && previousCoins != 0)
        {
            ShowCurrencyChangeEffect(gameCoinsText, isIncrease);
            if (isIncrease && coinGainEffect != null)
                ShowGainEffect(coinGainEffect);
        }

        // �ִϸ��̼����� ���� ����
        if (coinsAnimationCoroutine != null)
            StopCoroutine(coinsAnimationCoroutine);

        coinsAnimationCoroutine = StartCoroutine(AnimateNumberChange(
            gameCoinsText, previousCoins, newCoins, FormatNumber));

        previousCoins = newCoins;
    }

    void OnDiamondsChanged(int newDiamonds)
    {
        if (diamondsText == null) return;

        bool isIncrease = newDiamonds > previousDiamonds;

        if (showChangeAnimation && previousDiamonds != 0)
        {
            ShowCurrencyChangeEffect(diamondsText, isIncrease);
            if (isIncrease && diamondGainEffect != null)
                ShowGainEffect(diamondGainEffect);
        }

        // ���̾Ƹ��� ���� ���� ���̹Ƿ� �ܼ� ǥ��
        if (diamondsAnimationCoroutine != null)
            StopCoroutine(diamondsAnimationCoroutine);

        diamondsAnimationCoroutine = StartCoroutine(AnimateNumberChange(
            diamondsText, previousDiamonds, newDiamonds, num => num.ToString()));

        previousDiamonds = newDiamonds;
    }

    void OnEnergyChanged(int newEnergy)
    {
        if (energyText == null) return;

        bool isIncrease = newEnergy > previousEnergy;

        if (showChangeAnimation && previousEnergy != 0)
        {
            ShowCurrencyChangeEffect(energyText, isIncrease);
            if (isIncrease && energyGainEffect != null)
                ShowGainEffect(energyGainEffect);
        }

        // �������� �м� ���·� ǥ��
        int maxEnergy = UserDataManager.Instance?.GetMaxEnergy() ?? 5;

        if (energyAnimationCoroutine != null)
            StopCoroutine(energyAnimationCoroutine);

        energyAnimationCoroutine = StartCoroutine(AnimateEnergyChange(
            previousEnergy, newEnergy, maxEnergy));

        // ������ ������ ������Ʈ
        UpdateEnergyFillBar(newEnergy, maxEnergy);

        previousEnergy = newEnergy;
    }

    void OnPlayerLevelChanged(int newLevel)
    {
        if (playerLevelText == null) return;

        if (showChangeAnimation && previousLevel != 0)
        {
            ShowCurrencyChangeEffect(playerLevelText, newLevel > previousLevel);
        }

        playerLevelText.text = $"Lv.{newLevel}";
        AnimateScale(playerLevelText.transform);

        previousLevel = newLevel;
    }

    #endregion

    #region �ִϸ��̼� �ý���

    IEnumerator AnimateNumberChange(TextMeshProUGUI textComponent, int fromValue, int toValue, System.Func<int, string> formatter)
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = scaleAnimationCurve.Evaluate(elapsedTime / animationDuration);

            int currentValue = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, progress));
            textComponent.text = formatter(currentValue);

            yield return null;
        }

        textComponent.text = formatter(toValue);
    }

    IEnumerator AnimateEnergyChange(int fromEnergy, int toEnergy, int maxEnergy)
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = scaleAnimationCurve.Evaluate(elapsedTime / animationDuration);

            int currentEnergy = Mathf.RoundToInt(Mathf.Lerp(fromEnergy, toEnergy, progress));
            energyText.text = $"{currentEnergy}/{maxEnergy}";

            yield return null;
        }

        energyText.text = $"{toEnergy}/{maxEnergy}";
    }

    void AnimateScale(Transform target)
    {
        if (target == null) return;

        // LeanTween�� ������ ���, ������ �ڷ�ƾ ���
        StartCoroutine(ScaleAnimation(target));
    }

    IEnumerator ScaleAnimation(Transform target)
    {
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * scaleMultiplier;

        float halfDuration = animationDuration * 0.5f;

        // Scale up
        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / halfDuration;
            target.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }

        // Scale down
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / halfDuration;
            target.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }

        target.localScale = originalScale;
    }

    void ShowCurrencyChangeEffect(TextMeshProUGUI textComponent, bool isIncrease)
    {
        if (textComponent == null) return;

        Color originalColor = textComponent.color;
        Color effectColor = isIncrease ? gainColor : lossColor;

        StartCoroutine(ColorFlashEffect(textComponent, originalColor, effectColor));
    }

    IEnumerator ColorFlashEffect(TextMeshProUGUI textComponent, Color originalColor, Color effectColor)
    {
        textComponent.color = effectColor;
        yield return new WaitForSecondsRealtime(0.2f);
        textComponent.color = originalColor;
    }

    void ShowGainEffect(GameObject effectPrefab)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, transform);
            Destroy(effect, 2f); // 2�� �� ����
        }
    }

    void UpdateEnergyFillBar(int currentEnergy, int maxEnergy)
    {
        if (energyFillBar == null) return;

        float fillAmount = (float)currentEnergy / maxEnergy;
        energyFillBar.fillAmount = fillAmount;
    }

    #endregion

    #region ������ Ÿ�̸�

    void StartEnergyTimer()
    {
        InvokeRepeating(nameof(UpdateEnergyTimer), 1f, 1f);
    }

    void UpdateEnergyTimer()
    {
        if (UserDataManager.Instance == null) return;

        TimeSpan timeUntilNext = UserDataManager.Instance.GetTimeUntilNextEnergy();
        int currentEnergy = UserDataManager.Instance.GetEnergy();
        int maxEnergy = UserDataManager.Instance.GetMaxEnergy();

        bool shouldShowTimer = timeUntilNext.TotalSeconds > 0 && currentEnergy < maxEnergy;

        if (energyTimerPanel != null)
            energyTimerPanel.SetActive(shouldShowTimer);

        if (shouldShowTimer && energyTimerText != null)
        {
            string timeString = string.Format("{0:D2}:{1:D2}",
                timeUntilNext.Minutes,
                timeUntilNext.Seconds);
            energyTimerText.text = timeString;
        }
    }

    #endregion

    #region ��ư �̺�Ʈ

    public void OnGameCoinsButtonClicked()
    {
        Debug.Log("[CurrencyUI] ���� ���� ��ư Ŭ��");
        // TODO: ���� ���� �����̳� ���� ǥ��
        ShowCurrencyInfo("���� ����", UserDataManager.Instance?.GetGameCoins() ?? 0);
    }

    public void OnDiamondsButtonClicked()
    {
        Debug.Log("[CurrencyUI] ���̾Ƹ�� ��ư Ŭ�� - ���� ����");
        // TODO: ���̾Ƹ�� ���� ���� ����
        OpenDiamondShop();
    }

    public void OnEnergyButtonClicked()
    {
        Debug.Log("[CurrencyUI] ������ ��ư Ŭ��");
        ShowEnergyOptions();
    }

    void ShowCurrencyInfo(string currencyName, int amount)
    {
        string formattedAmount = FormatNumber(amount);
        if (CommonUIManager.Instance != null)
        {
            CommonUIManager.Instance.ShowNotification($"{currencyName}: {formattedAmount}", 2f);
        }
    }

    void OpenDiamondShop()
    {
        if (CommonUIManager.Instance != null)
        {
            CommonUIManager.Instance.ShowNotification("���̾Ƹ�� ������ �� ���µ˴ϴ�!", 3f);
        }
    }

    void ShowEnergyOptions()
    {
        if (UserDataManager.Instance == null) return;

        int currentEnergy = UserDataManager.Instance.GetEnergy();
        int maxEnergy = UserDataManager.Instance.GetMaxEnergy();

        if (currentEnergy >= maxEnergy)
        {
            if (CommonUIManager.Instance != null)
            {
                CommonUIManager.Instance.ShowNotification("�������� ���� á���ϴ�!", 2f);
            }
            return;
        }

        // ���̾Ƹ��� ������ ���� �ɼ� ǥ��
        int diamondCost = 10; // ���������� ���� ����
        int energyToRestore = maxEnergy - currentEnergy;

        ShowEnergyPurchaseDialog(diamondCost, energyToRestore);
    }

    void ShowEnergyPurchaseDialog(int diamondCost, int energyAmount)
    {
        if (UserDataManager.Instance == null) return;

        int currentDiamonds = UserDataManager.Instance.GetDiamonds();

        if (currentDiamonds >= diamondCost)
        {
            string message = $"���̾Ƹ�� {diamondCost}���� ������ {energyAmount}���� �����Ͻðڽ��ϱ�?";

            // TODO: ���� ���� Ȯ�� ���̾�α� ����
            Debug.Log($"[CurrencyUI] {message}");

            // �ӽ�: �ڵ� ���� (�����δ� ����� Ȯ�� �ʿ�)
            if (UserDataManager.Instance.PurchaseEnergyWithDiamonds(diamondCost, energyAmount))
            {
                if (CommonUIManager.Instance != null)
                {
                    CommonUIManager.Instance.ShowNotification($"������ {energyAmount}�� ���� �Ϸ�!", 2f);
                }
            }
        }
        else
        {
            if (CommonUIManager.Instance != null)
            {
                CommonUIManager.Instance.ShowNotification("���̾Ƹ�尡 �����մϴ�!", 2f);
            }
        }
    }

    #endregion

    #region ��ƿ��Ƽ �޼���

    string FormatNumber(int number)
    {
        if (!useShortFormat) return number.ToString();

        if (number >= 1000000)
            return (number / 1000000f).ToString("0.0") + "M";
        else if (number >= 1000)
            return (number / 1000f).ToString("0.0") + "K";
        else
            return number.ToString();
    }

    #endregion

    #region ���� �޼���

    /// <summary>
    /// ��� ��ȭ ǥ�ø� ������ ���ΰ�ħ
    /// </summary>
    public void RefreshAllDisplays()
    {
        if (UserDataManager.Instance == null)
        {
            Debug.LogWarning("[CurrencyUI] UserDataManager�� ���� ���ΰ�ħ �Ұ�");
            return;
        }

        // ���� �� ������Ʈ (�ִϸ��̼� ����)
        previousCoins = UserDataManager.Instance.GetGameCoins();
        previousDiamonds = UserDataManager.Instance.GetDiamonds();
        previousEnergy = UserDataManager.Instance.GetEnergy();
        previousLevel = UserDataManager.Instance.GetPlayerLevel();

        // UI ������Ʈ
        if (gameCoinsText != null)
            gameCoinsText.text = FormatNumber(previousCoins);

        if (diamondsText != null)
            diamondsText.text = previousDiamonds.ToString();

        if (energyText != null)
        {
            int maxEnergy = UserDataManager.Instance.GetMaxEnergy();
            energyText.text = $"{previousEnergy}/{maxEnergy}";
            UpdateEnergyFillBar(previousEnergy, maxEnergy);
        }

        if (playerLevelText != null)
            playerLevelText.text = $"Lv.{previousLevel}";

        Debug.Log("[CurrencyUI] ��� ��ȭ ǥ�� ���ΰ�ħ �Ϸ�");
    }

    /// <summary>
    /// �ִϸ��̼ǰ� �Բ� ��� ǥ�� ������Ʈ
    /// </summary>
    public void ForceUpdateWithAnimation()
    {
        if (UserDataManager.Instance == null) return;

        OnGameCoinsChanged(UserDataManager.Instance.GetGameCoins());
        OnDiamondsChanged(UserDataManager.Instance.GetDiamonds());
        OnEnergyChanged(UserDataManager.Instance.GetEnergy());
        OnPlayerLevelChanged(UserDataManager.Instance.GetPlayerLevel());
    }

    /// <summary>
    /// Ư�� ��ȭ�� ���� �׽�Ʈ ȿ�� (����׿�)
    /// </summary>
    public void TestCurrencyEffect(string currencyType)
    {
        switch (currencyType.ToLower())
        {
            case "coins":
                if (coinGainEffect != null) ShowGainEffect(coinGainEffect);
                if (gameCoinsText != null) ShowCurrencyChangeEffect(gameCoinsText, true);
                break;
            case "diamonds":
                if (diamondGainEffect != null) ShowGainEffect(diamondGainEffect);
                if (diamondsText != null) ShowCurrencyChangeEffect(diamondsText, true);
                break;
            case "energy":
                if (energyGainEffect != null) ShowGainEffect(energyGainEffect);
                if (energyText != null) ShowCurrencyChangeEffect(energyText, true);
                break;
        }
    }

    #endregion
}