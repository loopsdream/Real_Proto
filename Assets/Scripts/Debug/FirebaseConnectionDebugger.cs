using System.Collections;
using UnityEngine;

public class FirebaseConnectionDebugger : MonoBehaviour
{
    [Header("실시간 모니터링")]
    public bool enableRealTimeLogging = true;
    public float logInterval = 1f;
    
    void Start()
    {
        if (enableRealTimeLogging)
        {
            StartCoroutine(RealTimeFirebaseMonitoring());
        }
    }
    
    [ContextMenu("Manual Check Now")]
    public void ManualCheck()
    {
        StartCoroutine(DetailedConnectionCheck());
    }
    
    IEnumerator RealTimeFirebaseMonitoring()
    {
        Debug.Log("🔍 실시간 Firebase 연결 모니터링 시작");
        
        while (true)
        {
            yield return new WaitForSeconds(logInterval);
            LogCurrentState();
        }
    }
    
    IEnumerator DetailedConnectionCheck()
    {
        Debug.Log("📊 ===== 상세 연결 상태 체크 =====");
        
        // 1. 각 매니저의 존재 여부
        Debug.Log("=== 매니저 존재 여부 ===");
        Debug.Log($"CleanFirebaseManager.Instance: {(CleanFirebaseManager.Instance != null ? "존재" : "NULL")}");
        Debug.Log($"FirebaseDataManager.Instance: {(FirebaseDataManager.Instance != null ? "존재" : "NULL")}");
        Debug.Log($"UserDataManager.Instance: {(UserDataManager.Instance != null ? "존재" : "NULL")}");
        
        // 2. CleanFirebaseManager 상세 상태
        if (CleanFirebaseManager.Instance != null)
        {
            Debug.Log("=== CleanFirebaseManager 상태 ===");
            Debug.Log($"IsReady: {CleanFirebaseManager.Instance.IsReady}");
            Debug.Log($"IsLoggedIn: {CleanFirebaseManager.Instance.IsLoggedIn}");
            Debug.Log($"IsOnline: {CleanFirebaseManager.Instance.IsOnline}");
            Debug.Log($"IsConnected: {CleanFirebaseManager.Instance.IsConnected}");
            
            if (CleanFirebaseManager.Instance.IsLoggedIn)
            {
                string userId = CleanFirebaseManager.Instance.CurrentUserId;
                Debug.Log($"Current User ID: {userId}");
            }
        }
        
        // 3. FirebaseDataManager 상세 상태
        if (FirebaseDataManager.Instance != null)
        {
            Debug.Log("=== FirebaseDataManager 상태 ===");
            Debug.Log($"IsConnected: {FirebaseDataManager.Instance.IsConnected}");
            Debug.Log($"IsFirebaseReady: {FirebaseDataManager.Instance.IsFirebaseReady}");
            Debug.Log($"IsPartiallyConnected: {FirebaseDataManager.Instance.IsPartiallyConnected}");
        }
        
        // 4. 연결 시퀀스 테스트
        Debug.Log("=== 연결 시퀀스 테스트 ===");
        yield return StartCoroutine(TestConnectionSequence());
    }
    
    IEnumerator TestConnectionSequence()
    {
        if (CleanFirebaseManager.Instance != null && CleanFirebaseManager.Instance.IsReady)
        {
            Debug.Log("🧪 수동 익명 로그인 테스트 시작");
            
            // 로그인 이벤트 수신 대기
            bool loginEventReceived = false;
            bool loginSuccess = false;
            
            System.Action<bool> onLoginComplete = (success) =>
            {
                loginEventReceived = true;
                loginSuccess = success;
                Debug.Log($"🎭 로그인 이벤트 수신: {success}");
            };
            
            // 이벤트 구독
            CleanFirebaseManager.Instance.OnUserSignedIn += onLoginComplete;
            
            // 로그인 시도
            CleanFirebaseManager.Instance.SignInAnonymously();
            Debug.Log("🎭 익명 로그인 요청 전송");
            
            // 결과 대기 (최대 10초)
            float timeout = 10f;
            float elapsed = 0f;
            
            while (!loginEventReceived && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 이벤트 구독 해제
            CleanFirebaseManager.Instance.OnUserSignedIn -= onLoginComplete;
            
            if (loginEventReceived)
            {
                Debug.Log($"✅ 로그인 완료: {(loginSuccess ? "성공" : "실패")}");
                
                if (loginSuccess)
                {
                    yield return new WaitForSeconds(1f);
                    LogCurrentState();
                }
            }
            else
            {
                Debug.LogError("❌ 로그인 타임아웃 - 이벤트 수신 안됨");
            }
        }
        else
        {
            Debug.LogError("❌ CleanFirebaseManager가 준비되지 않음");
        }
    }
    
    void LogCurrentState()
    {
        string status = "[Firebase Status] ";
        
        if (CleanFirebaseManager.Instance != null)
        {
            status += $"Ready:{CleanFirebaseManager.Instance.IsReady} ";
            status += $"Logged:{CleanFirebaseManager.Instance.IsLoggedIn} ";
        }
        else
        {
            status += "CleanFB:NULL ";
        }
        
        if (FirebaseDataManager.Instance != null)
        {
            status += $"Connected:{FirebaseDataManager.Instance.IsConnected} ";
            status += $"Partial:{FirebaseDataManager.Instance.IsPartiallyConnected}";
        }
        else
        {
            status += "DataMgr:NULL";
        }
        
        Debug.Log(status);
    }
}
