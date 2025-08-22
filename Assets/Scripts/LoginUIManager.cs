// LoginUIManager.cs - 로그인 UI 관리
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Auth;

public class LoginUIManager : MonoBehaviour
{
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

    [Header("Animation")]
    public CanvasGroup mainCanvasGroup;
    public float transitionDuration = 0.5f;

    private bool isProcessing = false;

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

        // FirebaseManager 이벤트 구독
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseInitialized += OnFirebaseReady;
            FirebaseManager.Instance.OnUserSignedIn += OnUserSignedIn;
            FirebaseManager.Instance.OnUserSignedOut += OnUserSignedOut;
            FirebaseManager.Instance.OnAuthError += OnAuthError;

            // 이미 초기화된 경우
            if (FirebaseManager.Instance.isInitialized)
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
        if (loginEmailInput != null)
            loginEmailInput.onEndEdit.AddListener(OnEmailInputChanged);
        if (loginPasswordInput != null)
        {
            loginPasswordInput.onEndEdit.AddListener(OnPasswordInputChanged);
            loginPasswordInput.onSubmit.AddListener((_) => OnLoginButtonClicked());
        }
        if (confirmPasswordInput != null)
            confirmPasswordInput.onSubmit.AddListener((_) => OnSignupButtonClicked());
    }

    #region Firebase 이벤트 처리

    void OnFirebaseReady()
    {
        Debug.Log("Firebase 준비 완료 - 로그인 UI 활성화");
        SetButtonsInteractable(true);

        // 이미 로그인된 사용자가 있는지 확인
        if (FirebaseManager.Instance.isAuthenticated)
        {
            StartCoroutine(AutoLoginSequence());
        }
    }

    void OnUserSignedIn(FirebaseUser user)
    {
        Debug.Log($"사용자 로그인 완료: {user.Email}");
        ShowStatus($"환영합니다, {user.DisplayName ?? user.Email}!", true);
        
        StartCoroutine(MoveToGameAfterDelay());
    }

    void OnUserSignedOut()
    {
        Debug.Log("사용자 로그아웃");
        ShowLoginPanel();
        SetButtonsInteractable(true);
        isProcessing = false;
    }

    void OnAuthError(string error)
    {
        Debug.LogError($"인증 오류: {error}");
        ShowStatus(error, false);
        HideLoadingPanel();
        SetButtonsInteractable(true);
        isProcessing = false;
    }

    #endregion

    #region 로그인 처리

    async void OnLoginButtonClicked()
    {
        if (isProcessing || !ValidateLoginInput()) return;

        PlayUISound("ButtonClick");
        isProcessing = true;
        SetButtonsInteractable(false);

        string email = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;

        ShowLoadingPanel("로그인 중...");

        bool success = await FirebaseManager.Instance.SignInWithEmail(email, password);

        if (success)
        {
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
            HideLoadingPanel();
            SetButtonsInteractable(true);
            isProcessing = false;
        }
    }

    async void OnGuestLoginButtonClicked()
    {
        if (isProcessing) return;

        PlayUISound("ButtonClick");
        isProcessing = true;
        SetButtonsInteractable(false);

        ShowLoadingPanel("게스트로 로그인 중...");

        bool success = await FirebaseManager.Instance.SignInAnonymously();

        if (!success)
        {
            HideLoadingPanel();
            SetButtonsInteractable(true);
            isProcessing = false;
        }
    }

    async void OnSignupButtonClicked()
    {
        if (isProcessing || !ValidateSignupInput()) return;

        PlayUISound("ButtonClick");
        isProcessing = true;
        SetButtonsInteractable(false);

        string email = signupEmailInput.text.Trim();
        string password = signupPasswordInput.text;

        ShowLoadingPanel("회원가입 중...");

        bool success = await FirebaseManager.Instance.SignUpWithEmail(email, password);

        if (!success)
        {
            HideLoadingPanel();
            SetButtonsInteractable(true);
            isProcessing = false;
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
            statusText.color = isSuccess ? Color.green : Color.red;
        }
        if (statusPanel != null) statusPanel.SetActive(true);

        // 자동으로 숨기기
        StartCoroutine(HideStatusAfterDelay(3f));
    }

    void HideStatusPanel()
    {
        if (statusPanel != null) statusPanel.SetActive(false);
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (guestLoginButton != null) guestLoginButton.interactable = interactable;
        if (signupButton != null) signupButton.interactable = interactable;
        if (showSignupButton != null) showSignupButton.interactable = interactable;
        if (backToLoginButton != null) backToLoginButton.interactable = interactable;
    }

    void ClearInputFields()
    {
        if (loginPasswordInput != null) loginPasswordInput.text = "";
        if (signupEmailInput != null) signupEmailInput.text = "";
        if (signupPasswordInput != null) signupPasswordInput.text = "";
        if (confirmPasswordInput != null) confirmPasswordInput.text = "";
    }

    #endregion

    #region 저장된 로그인 정보 처리

    void LoadSavedLoginInfo()
    {
        if (PlayerPrefs.HasKey("SavedEmail") && loginEmailInput != null)
        {
            string savedEmail = PlayerPrefs.GetString("SavedEmail");
            loginEmailInput.text = savedEmail;
            if (rememberMeToggle != null) rememberMeToggle.isOn = true;
        }
    }

    void SaveLoginInfo(string email)
    {
        PlayerPrefs.SetString("SavedEmail", email);
        PlayerPrefs.Save();
    }

    void ClearSavedLoginInfo()
    {
        PlayerPrefs.DeleteKey("SavedEmail");
        PlayerPrefs.Save();
    }

    #endregion

    #region 애니메이션 및 시퀀스

    IEnumerator AutoLoginSequence()
    {
        ShowLoadingPanel("자동 로그인 중...");
        yield return new WaitForSeconds(1f);
        // OnUserSignedIn에서 처리됨
    }

    IEnumerator MoveToGameAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        
        ShowLoadingPanel("게임 데이터 로드 중...");
        yield return new WaitForSeconds(1f);

        // 로비 씬으로 이동
        PlayUISound("MenuTransition");
        SceneManager.LoadScene("LobbyScene");
    }

    IEnumerator AnimateLoadingProgress()
    {
        if (loadingProgress == null) yield break;
        
        float progress = 0f;
        while (progress < 1f)
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

    #region 이벤트 처리

    void OnEmailInputChanged(string value)
    {
        // 실시간 이메일 유효성 검사 (선택사항)
    }

    void OnPasswordInputChanged(string value)
    {
        // 실시간 패스워드 강도 체크 (선택사항)
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
        // 이벤트 구독 해제
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseInitialized -= OnFirebaseReady;
            FirebaseManager.Instance.OnUserSignedIn -= OnUserSignedIn;
            FirebaseManager.Instance.OnUserSignedOut -= OnUserSignedOut;
            FirebaseManager.Instance.OnAuthError -= OnAuthError;
        }
    }
}