// TitleSceneManager.cs - 타이틀 씬 관리 스크립트 (사운드 추가 버전)
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TitleSceneManager : MonoBehaviour
{
    [Header("Title UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI versionText;
    public Button startButton;
    public Button loginButton;
    public GameObject loadingPanel;
    
    [Header("Loading UI")]
    public Slider progressBar;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI progressText;
    
    [Header("Settings")]
    public string gameVersion = "1.0.0";
    public float minLoadingTime = 2.0f;
    
    [Header("Animation")]
    public CanvasGroup titleCanvasGroup;
    public float titleFadeInDuration = 1.5f;
    
    private bool isInitialized = false;
    private bool isLoading = false;

    void Start()
    {
        InitializeTitle();
    }

    void InitializeTitle()
    {
        // 버전 정보 설정
        if (versionText != null)
        {
            versionText.text = $"v{gameVersion}";
        }

        // 타이틀 텍스트 설정
        if (titleText != null)
        {
            titleText.text = "CROxCRO";
        }

        // 로딩 패널 비활성화
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        // 타이틀 페이드 인
        StartCoroutine(FadeInTitle());

        // BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySceneBGM("TitleScene");
        }

        // 초기화 완료
        isInitialized = true;
    }

    IEnumerator FadeInTitle()
    {
        if (titleCanvasGroup == null) yield break;

        titleCanvasGroup.alpha = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < titleFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            titleCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / titleFadeInDuration);
            yield return null;
        }

        titleCanvasGroup.alpha = 1f;
    }

    // 게임 시작 버튼
    public void OnStartButtonClicked()
    {
        if (!isInitialized || isLoading) return;

        PlayUISound("ButtonClick");
        StartCoroutine(StartGameSequence());
    }

    // 로그인 버튼
    public void OnLoginButtonClicked()
    {
        if (!isInitialized || isLoading) return;

        PlayUISound("ButtonClick");
        
        // TODO: Firebase 로그인 연동
        Debug.Log("Login functionality will be implemented with Firebase");
        
        // 임시로 바로 게임 시작
        OnStartButtonClicked();
    }

    IEnumerator StartGameSequence()
    {
        isLoading = true;

        // 로딩 UI 활성화
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // 버튼 비활성화
        if (startButton != null) startButton.interactable = false;
        if (loginButton != null) loginButton.interactable = false;

        // 패치 체크 시뮬레이션
        yield return StartCoroutine(CheckForUpdates());

        // 게임 데이터 로드 시뮬레이션
        yield return StartCoroutine(LoadGameData());

        // 최소 로딩 시간 보장
        yield return new WaitForSeconds(minLoadingTime);

        // 로비 씬으로 이동
        GoToLobbyScene();
    }

    IEnumerator CheckForUpdates()
    {
        UpdateStatus("패치 파일을 확인하는 중...", 0f);
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("최신 버전입니다.", 0.3f);
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator LoadGameData()
    {
        UpdateStatus("게임 데이터를 로드하는 중...", 0.4f);
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("사용자 데이터를 로드하는 중...", 0.6f);
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("에셋을 로드하는 중...", 0.8f);
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("로드 완료!", 1.0f);
        yield return new WaitForSeconds(0.3f);
    }

    void UpdateStatus(string message, float progress)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        Debug.Log($"Loading: {message} ({Mathf.RoundToInt(progress * 100)}%)");
    }

    void GoToLobbyScene()
    {
        PlayUISound("MenuTransition");
        Debug.Log("Moving to Lobby Scene...");
        SceneManager.LoadScene("LobbyScene");
    }

    // 게임 종료
    public void QuitGame()
    {
        PlayUISound("ButtonClick");
        Debug.Log("Quitting the game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 디버그용: 직접 로비로 이동
    public void SkipToLobby()
    {
        if (isLoading) return;
        
        PlayUISound("ButtonClick");
        Debug.Log("Skipping to Lobby Scene...");
        SceneManager.LoadScene("LobbyScene");
    }

    // UI 사운드 재생 헬퍼 메서드
    void PlayUISound(string soundName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(soundName);
        }
    }
}