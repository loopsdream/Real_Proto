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
        // ��ũ��Ʈ�� ���� ���Ǵ
        EnsureEssentialManagers();
        
        StartCoroutine(LogoSequence());
    }
    
    /// <summary>
    /// 기본 매니저들이 없으면 생성
    /// </summary>
    void EnsureEssentialManagers()
    {
        Debug.Log("[LogoScene] 필수 매니저 확인 시작");
        
        // 1. UserDataManager 부터 생성 (가장 기본)
        if (UserDataManager.Instance == null)
        {
            var userDataGO = new GameObject("UserDataManager");
            userDataGO.AddComponent<UserDataManager>();
            Debug.Log("[LogoScene] ✅ UserDataManager 생성됨");
        }
        else
        {
            Debug.Log("[LogoScene] ✅ UserDataManager 이미 존재함");
        }
        
        // 2. CleanFirebaseManager 생성
        if (CleanFirebaseManager.Instance == null)
        {
            var firebaseGO = new GameObject("CleanFirebaseManager");
            firebaseGO.AddComponent<CleanFirebaseManager>();
            Debug.Log("[LogoScene] ✅ CleanFirebaseManager 생성됨");
        }
        else
        {
            Debug.Log("[LogoScene] ✅ CleanFirebaseManager 이미 존재함");
        }
        
        // 3. FirebaseDataManager 생성
        if (FirebaseDataManager.Instance == null)
        {
            var dataGO = new GameObject("FirebaseDataManager");
            dataGO.AddComponent<FirebaseDataManager>();
            Debug.Log("[LogoScene] ✅ FirebaseDataManager 생성됨");
        }
        else
        {
            Debug.Log("[LogoScene] ✅ FirebaseDataManager 이미 존재함");
        }
        
        // 4. AudioManager 확인
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[LogoScene] ⚠️ AudioManager가 없음 - 수동 추가 권장");
        }
        else
        {
            Debug.Log("[LogoScene] ✅ AudioManager 존재함");
        }
        
        
        
        // Firebase 자동 로그인 시도
        StartCoroutine(TryFirebaseAutoLogin());
Debug.Log("[LogoScene] 필수 매니저 확인 완료");
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

    public 
    /// <summary>
    /// Firebase 자동 로그인 시도
    /// </summary>
    IEnumerator TryFirebaseAutoLogin()
    {
        // 매니저들이 생성될 때까지 대기
        yield return new WaitForSeconds(1f);
        
        if (CleanFirebaseManager.Instance != null)
        {
            Debug.Log("[LogoScene] Firebase 자동 로그인 대기 중...");
            
            // Firebase 초기화 대기 (최대 5초)
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!CleanFirebaseManager.Instance.IsReady && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (CleanFirebaseManager.Instance.IsReady)
            {
                Debug.Log("[LogoScene] Firebase 준비 완룼 - 익명 로그인 시도");
                CleanFirebaseManager.Instance.SignInAnonymously();
            }
            else
            {
                Debug.LogWarning("[LogoScene] Firebase 초기화 타임아웃");
            }
        }
        else
        {
            Debug.LogWarning("[LogoScene] CleanFirebaseManager가 없음");
        }
    }
void SkipToTitle()
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