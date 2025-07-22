// SceneTransitionManager.cs - 씬 전환 시 공통 UI 관리
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
        // 로딩 화면 표시
        if (useLoadingScreen && CommonUIManager.Instance != null)
        {
            CommonUIManager.Instance.ShowLoadingScreen();
        }

        // 잠깐 대기 (로딩 화면이 보이도록)
        yield return new WaitForSeconds(0.5f);

        // 씬 비동기 로딩
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 로딩 완료 후 잠깐 대기
        yield return new WaitForSeconds(0.5f);

        // 로딩 화면 숨김
        if (useLoadingScreen && CommonUIManager.Instance != null)
        {
            CommonUIManager.Instance.HideLoadingScreen();
        }

        Debug.Log($"Scene transition completed: {sceneName}");
    }

    // 편의 메서드들
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