// LoginUIManager.cs - CleanFirebaseManager와 호환되도록 수정된 버전
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginUIManager : MonoBehaviour
{
    [Header("Scene Management")]
    public TitleSceneManager titleSceneManager;

    [Header("Main Panels")]
    public GameObject loginPanel;
    public GameObject signupPanel;
    public GameObject loadingPanel;

    [Header("Login Panel")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;
    public Button guestLoginButton;
    public Button showSignupButton;
    public Toggle rememberMeToggle;

    [Header("Signup Panel")]
    public TMP_InputField signupEmailInput;
    public TMP_InputField signupPasswordInput;
    public TMP_InputField confirmPasswordInput;
    public Button signupButton;
    public Button backToLoginButton;

    [Header("Loading Panel")]
    public TextMeshProUGUI loadingText;
    public Slider loadingProgress;

    [Header("Status Display")]
    public TextMeshProUGUI statusText;
    public GameObject statusPanel;

    [Header("UI Colors")]
    public Color successColor = Color.green;
    public Color errorColor = Color.red;

    [Header("Animation")]
    public CanvasGroup mainCanvasGroup;
    public float transitionDuration = 0.5f;

    // 상태 관리
    private bool isProcessing = false;
    private bool signupSuccess = false;
    private const string REMEMBER_EMAIL_KEY = "RememberedEmail";

    void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }

    void InitializeUI()
    {
        // 초기 패널 상태 설정
        ShowLoginPanel();
        HideLoadingPanel();
        HideStatusPanel();

        // CleanFirebaseManager 이벤트 구독 (수정된 부분)
        if (CleanFirebaseManager.Instance != null)
        {
            CleanFirebaseManager.Instance.OnFirebaseReady += OnFirebaseReady;
            CleanFirebaseManager.Instance.OnUserSignedIn += OnUserSignedIn;
            CleanFirebaseManager.Instance.OnError += OnAuthError;

            // 이미 초기화된 경우
            if (CleanFirebaseManager.Instance.IsReady)
            {
                OnFirebaseReady();
            }
        }
        else
        {
            ShowStatus("Firebase 매니저를 찾을 수 없습니다.", false);
        }

        // 저장된 로그인 정보 로드
        LoadSavedLoginInfo();
    }

    void SetupEventListeners()
    {
        // 로그인 패널 버튼들
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        if (guestLoginButton != null)
            guestLoginButton.onClick.AddListener(OnGuestLoginButtonClicked);
        if (showSignupButton != null)
            showSignupButton.onClick.AddListener(ShowSignupPanel);

        // 회원가입 패널 버튼들
        if (signupButton != null)
            signupButton.onClick.AddListener(OnSignupButtonClicked);
        if (backToLoginButton != null)
            backToLoginButton.onClick.AddListener(ShowLoginPanel);

        // 입력 필드 이벤트
        if (loginPasswordInput != null)
            loginPasswordInput.onSubmit.AddListener((_) => OnLoginButtonClicked());
        if (confirmPasswordInput != null)
            confirmPasswordInput.onSubmit.AddListener((_) => OnSignupButtonClicked());
    }

    #region Firebase 이벤트 처리

    void OnFirebaseReady()
    {
        Debug.Log("[LoginUI] Firebase 준비 완료 - 로그인 UI 활성화");
        SetButtonsInteractable(true);

        // 이미 로그인된 사용자가 있는지 확인
        if (CleanFirebaseManager.Instance != null && CleanFirebaseManager.Instance.IsLoggedIn)
        {
            StartCoroutine(AutoLoginSequence());
        }
    }

    void OnUserSignedIn(bool success)
    {
        if (success)
        {
            Debug.Log("[LoginUI] User signed in successfully");

            // 회원가입 후 자동 로그인인 경우
            if (signupSuccess)
            {
                Debug.Log("[LoginUI] Signup and auto-login successful");
                ShowStatus("Account created! Welcome!", true);
                signupSuccess = false;  // 플래그 리셋
                StartCoroutine(MoveToGameAfterDelay());
            }
            else
            {
                // 일반 로그인인 경우
                ShowStatus("Login successful!", true);
                StartCoroutine(MoveToGameAfterDelay());
            }
        }
        else
        {
            Debug.LogWarning("[LoginUI] Sign in failed");
            ShowStatus("Login failed", false);
            isProcessing = false;
            HideLoadingPanel();
        }
    }

    void OnAuthError(string error)
    {
        Debug.LogError($"[LoginUI] 인증 오류: {error}");
        ShowStatus(error, false);
        HideLoadingPanel();
        SetButtonsInteractable(true);
        isProcessing = false;
    }

    #endregion

    #region 로그인 처리

    void OnLoginButtonClicked()
    {
        if (isProcessing || !ValidateLoginInput()) return;

        PlayUISound("ButtonClick");
        isProcessing = true;
        signupSuccess = false;
        SetButtonsInteractable(false);

        string email = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;

        ShowLoadingPanel("Logining...");

        // CleanFirebaseManager 사용 (수정된 부분)
        if (CleanFirebaseManager.Instance != null)
        {
            CleanFirebaseManager.Instance.SignInWithEmailPassword(email, password);
            
            // Remember Me 처리
            if (rememberMeToggle != null && rememberMeToggle.isOn)
            {
                SaveLoginInfo(email);
            }
            else
            {
                ClearSavedLoginInfo();
            }
        }
        else
        {
            ShowStatus("Firebase가 초기화되지 않았습니다.", false);
            HideLoadingPanel();
            SetButtonsInteractable(true);
            isProcessing = false;
        }
    }

    void OnGuestLoginButtonClicked()
    {
        if (isProcessing) return;

        PlayUISound("ButtonClick");
        isProcessing = true;
        SetButtonsInteractable(false);

        ShowLoadingPanel("Guest Logining...");

        // CleanFirebaseManager 사용 (수정된 부분)
        if (CleanFirebaseManager.Instance != null)
        {
            CleanFirebaseManager.Instance.SignInAnonymously();
        }
        else
        {
            ShowStatus("Firebase가 초기화되지 않았습니다.", false);
            HideLoadingPanel();
            SetButtonsInteractable(true);
            isProcessing = false;
        }
    }

    void OnSignupButtonClicked()
    {
        if (isProcessing || !ValidateSignupInput()) return;

        PlayUISound("ButtonClick");
        isProcessing = true;
        signupSuccess = true;
        SetButtonsInteractable(false);

        string email = signupEmailInput.text.Trim();
        string password = signupPasswordInput.text;

        ShowLoadingPanel("Signing Up...");

        // CleanFirebaseManager 사용 (수정된 부분)
        if (CleanFirebaseManager.Instance != null)
        {
            CleanFirebaseManager.Instance.CreateUserWithEmailPassword(email, password);
        }
        else
        {
            ShowStatus("Firebase가 초기화되지 않았습니다.", false);
            HideLoadingPanel();
            SetButtonsInteractable(true);
            isProcessing = false;
            signupSuccess = false;
        }
    }

    #endregion

    #region 입력 검증

    bool ValidateLoginInput()
    {
        if (loginEmailInput == null || loginPasswordInput == null) return false;

        string email = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email))
        {
            ShowStatus("이메일을 입력해주세요.", false);
            loginEmailInput.Select();
            return false;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("올바른 이메일 형식을 입력해주세요.", false);
            loginEmailInput.Select();
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowStatus("패스워드를 입력해주세요.", false);
            loginPasswordInput.Select();
            return false;
        }

        return true;
    }

    bool ValidateSignupInput()
    {
        if (signupEmailInput == null || signupPasswordInput == null || confirmPasswordInput == null) 
            return false;

        string email = signupEmailInput.text.Trim();
        string password = signupPasswordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        if (string.IsNullOrEmpty(email))
        {
            ShowStatus("이메일을 입력해주세요.", false);
            signupEmailInput.Select();
            return false;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("올바른 이메일 형식을 입력해주세요.", false);
            signupEmailInput.Select();
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowStatus("패스워드를 입력해주세요.", false);
            signupPasswordInput.Select();
            return false;
        }

        if (password.Length < 6)
        {
            ShowStatus("패스워드는 6자 이상이어야 합니다.", false);
            signupPasswordInput.Select();
            return false;
        }

        if (password != confirmPassword)
        {
            ShowStatus("패스워드가 일치하지 않습니다.", false);
            confirmPasswordInput.Select();
            return false;
        }

        return true;
    }

    bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region UI 제어

    void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (signupPanel != null) signupPanel.SetActive(false);
        ClearInputFields();
    }

    void ShowSignupPanel()
    {
        PlayUISound("ButtonClick");
        if (loginPanel != null) loginPanel.SetActive(false);
        if (signupPanel != null) signupPanel.SetActive(true);
        ClearInputFields();
    }

    void ShowLoadingPanel(string message)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingText != null) loadingText.text = message;
        if (loadingProgress != null) loadingProgress.value = 0f;
        StartCoroutine(AnimateLoadingProgress());
    }

    void HideLoadingPanel()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        StopAllCoroutines();
    }

    void ShowStatus(string message, bool isSuccess)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isSuccess ? successColor : errorColor;
        }

        if (statusPanel != null) statusPanel.SetActive(true);

        // 일정 시간 후 자동으로 상태 메시지 숨김
        StartCoroutine(HideStatusAfterDelay(isSuccess ? 2f : 4f));
    }

    void HideStatusPanel()
    {
        if (statusPanel != null) statusPanel.SetActive(false);
    }

    void ClearInputFields()
    {
        if (loginPasswordInput != null) loginPasswordInput.text = "";
        if (signupEmailInput != null) signupEmailInput.text = "";
        if (signupPasswordInput != null) signupPasswordInput.text = "";
        if (confirmPasswordInput != null) confirmPasswordInput.text = "";
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (guestLoginButton != null) guestLoginButton.interactable = interactable;
        if (signupButton != null) signupButton.interactable = interactable;
        if (showSignupButton != null) showSignupButton.interactable = interactable;
        if (backToLoginButton != null) backToLoginButton.interactable = interactable;
    }

    #endregion

    #region Remember Me 기능

    void SaveLoginInfo(string email)
    {
        PlayerPrefs.SetString(REMEMBER_EMAIL_KEY, email);
        PlayerPrefs.Save();
        Debug.Log("[LoginUI] 로그인 정보 저장됨");
    }

    void LoadSavedLoginInfo()
    {
        if (PlayerPrefs.HasKey(REMEMBER_EMAIL_KEY))
        {
            string savedEmail = PlayerPrefs.GetString(REMEMBER_EMAIL_KEY);
            if (loginEmailInput != null && !string.IsNullOrEmpty(savedEmail))
            {
                loginEmailInput.text = savedEmail;
                if (rememberMeToggle != null) rememberMeToggle.isOn = true;
            }
        }
    }

    void ClearSavedLoginInfo()
    {
        PlayerPrefs.DeleteKey(REMEMBER_EMAIL_KEY);
        PlayerPrefs.Save();
        Debug.Log("[LoginUI] 저장된 로그인 정보 삭제됨");
    }

    #endregion

    #region 게임 진행

    IEnumerator AutoLoginSequence()
    {
        ShowLoadingPanel("Auto Logining...");
        yield return new WaitForSeconds(1f);
        StartCoroutine(MoveToGameAfterDelay());
    }

    IEnumerator MoveToGameAfterDelay()
    {
        ShowLoadingPanel("Move to Game...");
        yield return new WaitForSeconds(1.5f);

        isProcessing = false;

        // 수정: TitleSceneManager를 통해 로비로 이동
        if (titleSceneManager != null)
        {
            Debug.Log("[LoginUI] Using TitleSceneManager for scene transition");
            titleSceneManager.HideLoginPanel();
            titleSceneManager.StartGameTransition();
        }
        else
        {
            // TitleSceneManager가 없으면 직접 이동 (폴백)
            Debug.LogWarning("[LoginUI] TitleSceneManager not set, using direct scene load");
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadScene("LobbyScene");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
            }
        }
    }

    #endregion

    #region 애니메이션

    IEnumerator AnimateLoadingProgress()
    {
        if (loadingProgress == null) yield break;

        float progress = 0f;
        while (progress < 1f && loadingPanel.activeInHierarchy)
        {
            progress += Time.deltaTime * 0.5f;
            loadingProgress.value = progress;
            yield return null;
        }
    }

    IEnumerator HideStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideStatusPanel();
    }

    #endregion

    #region 유틸리티

    void PlayUISound(string soundName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(soundName);
        }
    }

    #endregion

    void OnDestroy()
    {
        // 이벤트 구독 해제 (수정된 부분)
        if (CleanFirebaseManager.Instance != null)
        {
            CleanFirebaseManager.Instance.OnFirebaseReady -= OnFirebaseReady;
            CleanFirebaseManager.Instance.OnUserSignedIn -= OnUserSignedIn;
            CleanFirebaseManager.Instance.OnError -= OnAuthError;
        }
    }
}
