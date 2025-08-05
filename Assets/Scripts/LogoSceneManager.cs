// LogoSceneManager.cs - 로고 씬 관리 스크립트
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogoSceneManager : MonoBehaviour
{
    [Header("Logo Display")]
    public Image logoImage;
    public CanvasGroup logoCanvasGroup;

    [Header("Timing")]
    public float logoDisplayDuration = 3.0f; // 로고 표시 시간
    public float fadeInDuration = 1.0f;      // 페이드 인 시간
    public float fadeOutDuration = 1.0f;     // 페이드 아웃 시간

    [Header("Skip Option")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Space;

    private bool isTransitioning = false;

    void Start()
    {
        StartCoroutine(LogoSequence());
    }

    void Update()
    {
        // 스킵 기능
        if (allowSkip && !isTransitioning)
        {
            if (Input.GetKeyDown(skipKey) || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                SkipToTitle();
            }
        }
    }

    IEnumerator LogoSequence()
    {
        // 초기 설정
        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f;
        }

        // 페이드 인
        yield return StartCoroutine(FadeIn());

        // 로고 표시 시간 대기
        yield return new WaitForSeconds(logoDisplayDuration);

        // 페이드 아웃
        yield return StartCoroutine(FadeOut());

        // 타이틀 씬으로 이동
        GoToTitleScene();
    }

    IEnumerator FadeIn()
    {
        if (logoCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            logoCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }
        logoCanvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        if (logoCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            logoCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeOutDuration));
            yield return null;
        }
        logoCanvasGroup.alpha = 0f;
    }

    public void SkipToTitle()
    {
        if (isTransitioning) return;

        StopAllCoroutines();
        GoToTitleScene();
    }

    void GoToTitleScene()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        Debug.Log("Moving to Title Scene...");
        SceneManager.LoadScene("TitleScene");
    }
}