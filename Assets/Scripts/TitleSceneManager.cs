// TitleSceneManager.cs - Ÿ��Ʋ �� ���� ��ũ��Ʈ (�α���, ��ġ ��)
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
    public float minLoadingTime = 2.0f; // �ּ� �ε� �ð�

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
        // ���� ���� ����
        if (versionText != null)
        {
            versionText.text = $"v{gameVersion}";
        }

        // Ÿ��Ʋ �ؽ�Ʈ ����
        if (titleText != null)
        {
            titleText.text = "CROxCRO";
        }

        // �ε� �г� ��Ȱ��ȭ
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        // Ÿ��Ʋ ���̵� ��
        StartCoroutine(FadeInTitle());

        // �ʱ�ȭ �Ϸ�
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

    // ���� ���� ��ư
    public void OnStartButtonClicked()
    {
        if (!isInitialized || isLoading) return;

        StartCoroutine(StartGameSequence());
    }

    // �α��� ��ư (Firebase ���� ����)
    public void OnLoginButtonClicked()
    {
        if (!isInitialized || isLoading) return;

        // TODO: Firebase �α��� ����
        Debug.Log("Login functionality will be implemented with Firebase");

        // �ӽ÷� �ٷ� ���� ����
        OnStartButtonClicked();
    }

    IEnumerator StartGameSequence()
    {
        isLoading = true;

        // �ε� UI Ȱ��ȭ
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // ��ư ��Ȱ��ȭ
        if (startButton != null) startButton.interactable = false;
        if (loginButton != null) loginButton.interactable = false;

        // ��ġ üũ �ùķ��̼�
        yield return StartCoroutine(CheckForUpdates());

        // ���� ������ �ε� �ùķ��̼�
        yield return StartCoroutine(LoadGameData());

        // �ּ� �ε� �ð� ����
        yield return new WaitForSeconds(minLoadingTime);

        // �κ� ������ �̵�
        GoToLobbyScene();
    }

    IEnumerator CheckForUpdates()
    {
        UpdateStatus("��ġ ������ Ȯ���ϴ� ��...", 0f);
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("�ֽ� �����Դϴ�.", 0.3f);
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator LoadGameData()
    {
        UpdateStatus("���� �����͸� �ε��ϴ� ��...", 0.4f);
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("����� �����͸� �ε��ϴ� ��...", 0.6f);
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("������ �ε��ϴ� ��...", 0.8f);
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("�ε� �Ϸ�!", 1.0f);
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
        Debug.Log("Moving to Lobby Scene...");
        SceneManager.LoadScene("LobbyScene");
    }

    // ���� ����
    public void QuitGame()
    {
        Debug.Log("Quitting the game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ����׿�: ���� �κ�� �̵�
    public void SkipToLobby()
    {
        if (isLoading) return;

        Debug.Log("Skipping to Lobby Scene...");
        SceneManager.LoadScene("LobbyScene");
    }
}