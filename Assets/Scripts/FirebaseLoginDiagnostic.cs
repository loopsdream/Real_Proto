// FirebaseLoginDiagnostic.cs - try-catch yield return 오류 수정 버전
using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class FirebaseLoginDiagnostic : MonoBehaviour
{
    [Header("테스트 설정")]
    public bool runDiagnosticOnStart = true;
    public bool detailedLogging = true;

    void Start()
    {
        if (runDiagnosticOnStart)
        {
            StartCoroutine(RunComprehensiveDiagnostic());
        }
    }

    [ContextMenu("Run Firebase Login Diagnostic")]
    public void RunDiagnostic()
    {
        StartCoroutine(RunComprehensiveDiagnostic());
    }

    IEnumerator RunComprehensiveDiagnostic()
    {
        Debug.Log("🔍 ===== Firebase 로그인 진단 시작 =====");

        // 1. 기본 환경 확인
        CheckBasicEnvironment();
        yield return new WaitForSeconds(0.5f);

        // 2. Firebase 초기화 상태 확인
        yield return StartCoroutine(CheckFirebaseInitialization());

        // 3. Authentication 서비스 확인
        yield return StartCoroutine(CheckAuthenticationService());

        // 4. 실제 로그인 테스트
        yield return StartCoroutine(TestAnonymousLogin());

        Debug.Log("🏁 ===== Firebase 로그인 진단 완료 =====");
    }

    void CheckBasicEnvironment()
    {
        Debug.Log("📱 === 기본 환경 확인 ===");
        Debug.Log($"Unity 버전: {Application.unityVersion}");
        Debug.Log($"플랫폼: {Application.platform}");
        Debug.Log($"인터넷 연결: {Application.internetReachability}");
        Debug.Log($"Bundle ID: {Application.identifier}");
        
        // google-services.json 파일 확인
        var googleServicesAsset = Resources.Load("google-services");
        if (googleServicesAsset != null)
        {
            Debug.Log("✅ google-services.json 파일 감지됨");
        }
        else
        {
            Debug.LogError("❌ google-services.json 파일을 찾을 수 없습니다!");
        }
    }

    IEnumerator CheckFirebaseInitialization()
    {
        Debug.Log("🔥 === Firebase 초기화 확인 ===");

        var initTask = FirebaseApp.CheckAndFixDependenciesAsync();
        
        while (!initTask.IsCompleted)
        {
            Debug.Log("Firebase 의존성 확인 중...");
            yield return new WaitForSeconds(0.5f);
        }

        if (initTask.Exception != null)
        {
            Debug.LogError($"❌ Firebase 의존성 체크 실패: {initTask.Exception}");
            yield break;
        }

        var dependencyStatus = initTask.Result;
        Debug.Log($"Firebase 의존성 상태: {dependencyStatus}");

        if (dependencyStatus == DependencyStatus.Available)
        {
            System.Exception caughtException = null;
            FirebaseApp app = null;
            
            // try-catch를 yield 없는 부분에서 처리
            try
            {
                app = FirebaseApp.DefaultInstance;
            }
            catch (System.Exception ex)
            {
                caughtException = ex;
            }
            
            if (caughtException != null)
            {
                Debug.LogError($"❌ Firebase App 초기화 실패: {caughtException.Message}");
            }
            else if (app != null)
            {
                Debug.Log($"✅ Firebase App 초기화 성공: {app.Name}");
                Debug.Log($"Firebase App Options: {app.Options}");
                
                if (detailedLogging && app.Options != null)
                {
                    Debug.Log($"API Key: {app.Options.ApiKey?.Substring(0, 10)}...");
                    Debug.Log($"App ID: {app.Options.AppId?.Substring(0, 15)}...");
                    Debug.Log($"Project ID: {app.Options.ProjectId}");
                }
            }
        }
        else
        {
            Debug.LogError($"❌ Firebase 의존성 문제: {dependencyStatus}");
        }
    }

    IEnumerator CheckAuthenticationService()
    {
        Debug.Log("🔐 === Firebase Authentication 서비스 확인 ===");

        System.Exception authException = null;
        FirebaseAuth auth = null;
        
        // try-catch를 yield 없는 부분에서 처리
        try
        {
            auth = FirebaseAuth.DefaultInstance;
        }
        catch (System.Exception ex)
        {
            authException = ex;
        }
        
        if (authException != null)
        {
            Debug.LogError($"❌ Authentication 서비스 확인 실패: {authException.Message}");
            Debug.LogError($"스택 트레이스: {authException.StackTrace}");
        }
        else if (auth != null)
        {
            Debug.Log("✅ FirebaseAuth 인스턴스 생성 성공");
            Debug.Log($"현재 사용자: {auth.CurrentUser?.UserId ?? "없음"}");
            Debug.Log($"App: {auth.App?.Name ?? "없음"}");

            // Auth 상태 변경 감지 설정
            auth.StateChanged += OnAuthStateChangedForDiagnostic;
        }
        else
        {
            Debug.LogError("❌ FirebaseAuth 인스턴스가 null입니다");
        }
        
        yield return new WaitForSeconds(1f);
    }

    IEnumerator TestAnonymousLogin()
    {
        Debug.Log("🎭 === 익명 로그인 테스트 ===");

        System.Exception authGetException = null;
        FirebaseAuth auth = null;
        
        // try-catch를 yield 없는 부분에서 처리
        try
        {
            auth = FirebaseAuth.DefaultInstance;
        }
        catch (System.Exception ex)
        {
            authGetException = ex;
        }
        
        if (authGetException != null)
        {
            Debug.LogError($"❌ FirebaseAuth 초기화 오류: {authGetException.Message}");
            yield break;
        }
        
        if (auth == null)
        {
            Debug.LogError("❌ FirebaseAuth가 초기화되지 않았습니다.");
            yield break;
        }

        Debug.Log("익명 로그인 시도 중...");
        var loginTask = auth.SignInAnonymouslyAsync();

        float timeout = 10f;
        float elapsed = 0f;

        while (!loginTask.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            if (elapsed % 1f < Time.deltaTime) // 1초마다 로그
            {
                Debug.Log($"로그인 대기 중... ({elapsed:F1}s)");
            }
            yield return null;
        }

        if (!loginTask.IsCompleted)
        {
            Debug.LogError("❌ 로그인 타임아웃 (10초)");
            yield break;
        }

        if (loginTask.Exception != null)
        {
            Debug.LogError($"❌ 로그인 실패: {loginTask.Exception}");
            
            // 상세 오류 분석
            AnalyzeAuthException(loginTask.Exception);
        }
        else if (loginTask.Result != null)
        {
            var user = loginTask.Result.User;
            Debug.Log($"✅ 익명 로그인 성공!");
            Debug.Log($"사용자 ID: {user.UserId}");
            Debug.Log($"익명 사용자: {user.IsAnonymous}");
            Debug.Log($"생성 시간: {user.Metadata?.CreationTimestamp}");
        }
    }

    void OnAuthStateChangedForDiagnostic(object sender, System.EventArgs eventArgs)
    {
        var auth = sender as FirebaseAuth;
        if (auth?.CurrentUser != null)
        {
            Debug.Log($"🔐 [인증 상태 변경] 사용자 로그인: {auth.CurrentUser.UserId.Substring(0, 8)}...");
        }
        else
        {
            Debug.Log("🔓 [인증 상태 변경] 사용자 로그아웃");
        }
    }

    void AnalyzeAuthException(System.Exception exception)
    {
        Debug.Log("🔍 === 오류 상세 분석 ===");

        if (exception is Firebase.FirebaseException firebaseEx)
        {
            Debug.LogError($"Firebase 오류 코드: {firebaseEx.ErrorCode}");
            Debug.LogError($"Firebase 오류 메시지: {firebaseEx.Message}");

            switch ((int)firebaseEx.ErrorCode)
            {
                case 17020: // NETWORK_ERROR
                    Debug.LogError("🌐 네트워크 연결 문제입니다. 인터넷 연결을 확인하세요.");
                    break;
                case 17999: // INTERNAL_ERROR
                    Debug.LogError("🔥 Firebase 내부 오류입니다. google-services.json 설정을 확인하세요.");
                    break;
                case 17008: // INVALID_EMAIL
                    Debug.LogError("📧 잘못된 이메일 형식입니다.");
                    break;
                case 17010: // USER_DISABLED
                    Debug.LogError("🚫 사용자 계정이 비활성화되었습니다.");
                    break;
                case 17011: // USER_NOT_FOUND
                    Debug.LogError("👤 사용자를 찾을 수 없습니다.");
                    break;
                case 17009: // WRONG_PASSWORD
                    Debug.LogError("🔑 잘못된 패스워드입니다.");
                    break;
                default:
                    Debug.LogError($"알 수 없는 Firebase 오류입니다. 오류 코드: {(int)firebaseEx.ErrorCode}");
                    Debug.LogError("Firebase Console에서 Authentication 설정을 확인하세요.");
                    break;
            }
        }
        else if (exception is System.AggregateException aggregateEx)
        {
            Debug.LogError("복합 오류 발생:");
            foreach (var innerEx in aggregateEx.InnerExceptions)
            {
                Debug.LogError($"- {innerEx.GetType().Name}: {innerEx.Message}");
            }
        }
        else
        {
            Debug.LogError($"일반 예외: {exception.GetType().Name} - {exception.Message}");
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        System.Exception cleanupException = null;
        
        try
        {
            var auth = FirebaseAuth.DefaultInstance;
            if (auth != null)
            {
                auth.StateChanged -= OnAuthStateChangedForDiagnostic;
            }
        }
        catch (System.Exception ex)
        {
            cleanupException = ex;
        }
        
        if (cleanupException != null)
        {
            Debug.LogWarning($"FirebaseAuth 정리 중 오류: {cleanupException.Message}");
        }
    }
}
