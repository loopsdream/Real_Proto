// LogoSceneManager.cs - �ΰ� �� ���� ��ũ��Ʈ
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
    public float logoDisplayDuration = 3.0f; // �ΰ� ǥ�� �ð�
    public float fadeInDuration = 1.0f;      // ���̵� �� �ð�
    public float fadeOutDuration = 1.0f;     // ���̵� �ƿ� �ð�

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
        // ��ŵ ���
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
        // �ʱ� ����
        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f;
        }

        // ���̵� ��
        yield return StartCoroutine(FadeIn());

        // �ΰ� ǥ�� �ð� ���
        yield return new WaitForSeconds(logoDisplayDuration);

        // ���̵� �ƿ�
        yield return StartCoroutine(FadeOut());

        // Ÿ��Ʋ ������ �̵�
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