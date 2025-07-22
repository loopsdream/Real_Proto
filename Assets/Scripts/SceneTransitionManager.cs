// SceneTransitionManager.cs - �� ��ȯ �� ���� UI ����
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Transition Settings")]
    public float transitionDuration = 1f;
    public bool useLoadingScreen = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        // �ε� ȭ�� ǥ��
        if (useLoadingScreen && CommonUIManager.Instance != null)
        {
            CommonUIManager.Instance.ShowLoadingScreen();
        }

        // ��� ��� (�ε� ȭ���� ���̵���)
        yield return new WaitForSeconds(0.5f);

        // �� �񵿱� �ε�
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // �ε� �Ϸ� �� ��� ���
        yield return new WaitForSeconds(0.5f);

        // �ε� ȭ�� ����
        if (useLoadingScreen && CommonUIManager.Instance != null)
        {
            CommonUIManager.Instance.HideLoadingScreen();
        }

        Debug.Log($"Scene transition completed: {sceneName}");
    }

    // ���� �޼����
    public void LoadMainMenu()
    {
        LoadScene("MainMenu");
    }

    public void LoadGameScene()
    {
        LoadScene("GameScene");
    }

    public void LoadStageSelect()
    {
        LoadScene("StageSelect");
    }
}