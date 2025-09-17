// CurrencyUI.cs - 상시 표시되는 재화 UI 시스템
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
    public Image energyFillBar; // 에너지 충전 진행바

    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public float scaleMultiplier = 1.15f;
    public AnimationCurve scaleAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Number Formatting")]
    public bool useShortFormat = true; // K, M 단위 사용 여부
    public bool showChangeAnimation = true; // 수치 변경 애니메이션

    [Header("Visual Effects")]
    public GameObject coinGainEffect;
    public GameObject diamondGainEffect;
    public GameObject energyGainEffect;
    public Color gainColor = Color.green;
    public Color lossColor = Color.red;

    // 이전 값 저장 (변경 감지용)
    private int previousCoins = 0;
    private int previousDiamonds = 0;
    private int previousEnergy = 0;
    private int previousLevel = 0;

    // 애니메이션 코루틴 관리
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

    #region 초기화 및 연결

    void InitializeCurrencyUI()
    {
        // 초기 UI 상태 설정
        if (energyTimerPanel != null)
            energyTimerPanel.SetActive(false);

        // 기본값으로 초기화
        if (gameCoinsText != null) gameCoinsText.text = "0";
        if (diamondsText != null) diamondsText.text = "0";
        if (energyText != null) energyText.text = "0/5";
        if (playerLevelText != null) playerLevelText.text = "Lv.1";

        Debug.Log("[CurrencyUI] 초기화 완료");
    }

    void ConnectToUserDataManager()
    {
        if (UserDataManager.Instance != null)
        {
            // 이벤트 연결
            UserDataManager.Instance.OnGameCoinsChanged += OnGameCoinsChanged;
            UserDataManager.Instance.OnDiamondsChanged += OnDiamondsChanged;
            UserDataManager.Instance.OnEnergyChanged += OnEnergyChanged;
            UserDataManager.Instance.OnPlayerLevelChanged += OnPlayerLevelChanged;

            // 현재 값으로 초기 업데이트
            RefreshAllDisplays();
            Debug.Log("[CurrencyUI] UserDataManager 연결 완료");
        }
        else
        {
            Debug.LogWarning("[CurrencyUI] UserDataManager를 찾을 수 없습니다.");
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

    #region 재화 업데이트 이벤트 핸들러

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

        // 애니메이션으로 숫자 변경
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

        // 다이아몬드는 보통 적은 수이므로 단순 표시
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

        // 에너지는 분수 형태로 표시
        int maxEnergy = UserDataManager.Instance?.GetMaxEnergy() ?? 5;

        if (energyAnimationCoroutine != null)
            StopCoroutine(energyAnimationCoroutine);

        energyAnimationCoroutine = StartCoroutine(AnimateEnergyChange(
            previousEnergy, newEnergy, maxEnergy));

        // 에너지 충전바 업데이트
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

    #region 애니메이션 시스템

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

        // LeanTween이 있으면 사용, 없으면 코루틴 사용
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
            Destroy(effect, 2f); // 2초 후 제거
        }
    }

    void UpdateEnergyFillBar(int currentEnergy, int maxEnergy)
    {
        if (energyFillBar == null) return;

        float fillAmount = (float)currentEnergy / maxEnergy;
        energyFillBar.fillAmount = fillAmount;
    }

    #endregion

    #region 에너지 타이머

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

    #region 버튼 이벤트

    public void OnGameCoinsButtonClicked()
    {
        Debug.Log("[CurrencyUI] 게임 코인 버튼 클릭");
        // TODO: 코인 관련 상점이나 정보 표시
        ShowCurrencyInfo("게임 코인", UserDataManager.Instance?.GetGameCoins() ?? 0);
    }

    public void OnDiamondsButtonClicked()
    {
        Debug.Log("[CurrencyUI] 다이아몬드 버튼 클릭 - 상점 열기");
        // TODO: 다이아몬드 구매 상점 열기
        OpenDiamondShop();
    }

    public void OnEnergyButtonClicked()
    {
        Debug.Log("[CurrencyUI] 에너지 버튼 클릭");
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
            CommonUIManager.Instance.ShowNotification("다이아몬드 상점이 곧 오픈됩니다!", 3f);
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
                CommonUIManager.Instance.ShowNotification("에너지가 가득 찼습니다!", 2f);
            }
            return;
        }

        // 다이아몬드로 에너지 구매 옵션 표시
        int diamondCost = 10; // 설정값으로 변경 가능
        int energyToRestore = maxEnergy - currentEnergy;

        ShowEnergyPurchaseDialog(diamondCost, energyToRestore);
    }

    void ShowEnergyPurchaseDialog(int diamondCost, int energyAmount)
    {
        if (UserDataManager.Instance == null) return;

        int currentDiamonds = UserDataManager.Instance.GetDiamonds();

        if (currentDiamonds >= diamondCost)
        {
            string message = $"다이아몬드 {diamondCost}개로 에너지 {energyAmount}개를 구매하시겠습니까?";

            // TODO: 실제 구매 확인 다이얼로그 구현
            Debug.Log($"[CurrencyUI] {message}");

            // 임시: 자동 구매 (실제로는 사용자 확인 필요)
            if (UserDataManager.Instance.PurchaseEnergyWithDiamonds(diamondCost, energyAmount))
            {
                if (CommonUIManager.Instance != null)
                {
                    CommonUIManager.Instance.ShowNotification($"에너지 {energyAmount}개 구매 완료!", 2f);
                }
            }
        }
        else
        {
            if (CommonUIManager.Instance != null)
            {
                CommonUIManager.Instance.ShowNotification("다이아몬드가 부족합니다!", 2f);
            }
        }
    }

    #endregion

    #region 유틸리티 메서드

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

    #region 공용 메서드

    /// <summary>
    /// 모든 재화 표시를 강제로 새로고침
    /// </summary>
    public void RefreshAllDisplays()
    {
        if (UserDataManager.Instance == null)
        {
            Debug.LogWarning("[CurrencyUI] UserDataManager가 없어 새로고침 불가");
            return;
        }

        // 이전 값 업데이트 (애니메이션 방지)
        previousCoins = UserDataManager.Instance.GetGameCoins();
        previousDiamonds = UserDataManager.Instance.GetDiamonds();
        previousEnergy = UserDataManager.Instance.GetEnergy();
        previousLevel = UserDataManager.Instance.GetPlayerLevel();

        // UI 업데이트
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

        Debug.Log("[CurrencyUI] 모든 재화 표시 새로고침 완료");
    }

    /// <summary>
    /// 애니메이션과 함께 모든 표시 업데이트
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
    /// 특정 재화에 대한 테스트 효과 (디버그용)
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