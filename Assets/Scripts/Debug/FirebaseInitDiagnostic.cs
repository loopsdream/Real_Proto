using System.Collections;
using UnityEngine;

public class FirebaseInitDiagnostic : MonoBehaviour
{
    [Header("진단 설정")]
    public bool autoRunOnStart = true;
    public float checkInterval = 0.5f;
    
    void Start()
    {
        if (autoRunOnStart)
        {
            StartCoroutine(DiagnoseInitializationProcess());
        }
    }
    
    [ContextMenu("Run Firebase Init Diagnostic")]
    public void RunDiagnostic()
    {
        StartCoroutine(DiagnoseInitializationProcess());
    }
    
IEnumerator DiagnoseInitializationProcess()
    {
        Debug.Log("🔍 ===== Firebase 초기화 진단 시작 =====");
        
        for (int i = 0; i < 20; i++) // 10초간 모니터링
        {
            Debug.Log($"=== {i * checkInterval:F1}초 ===");
            
            // 1. CleanFirebaseManager 상태
            if (CleanFirebaseManager.Instance != null)
            {
                Debug.Log($"✅ CleanFirebaseManager: 존재함, IsReady={CleanFirebaseManager.Instance.IsReady}, IsLoggedIn={CleanFirebaseManager.Instance.IsLoggedIn}");
            }
            else
            {
                Debug.LogWarning("❌ CleanFirebaseManager: null");
            }
            
            // 2. FirebaseDataManager 상태
            if (FirebaseDataManager.Instance != null)
            {
                bool isConnected = FirebaseDataManager.Instance.IsConnected;
                bool isFirebaseReady = FirebaseDataManager.Instance.IsFirebaseReady;
                bool isPartiallyConnected = FirebaseDataManager.Instance.IsPartiallyConnected;
                
                Debug.Log($"✅ FirebaseDataManager: 존재함");
                Debug.Log($"  - IsConnected: {isConnected}");
                Debug.Log($"  - IsFirebaseReady: {isFirebaseReady}");
                Debug.Log($"  - IsPartiallyConnected: {isPartiallyConnected}");
            }
            else
            {
                Debug.LogWarning("❌ FirebaseDataManager: null");
            }
            
            // 3. UserDataManager 상태
            if (UserDataManager.Instance != null)
            {
                Debug.Log("✅ UserDataManager: 존재함");
            }
            else
            {
                Debug.LogWarning("❌ UserDataManager: null");
            }
            
            // 4. LoadUserData 조건 체크 (개선된 버전)
            if (FirebaseDataManager.Instance != null)
            {
                bool canLoad = FirebaseDataManager.Instance.IsConnected && 
                              UserDataManager.Instance != null && 
                              CleanFirebaseManager.Instance != null;
                              
                bool canPartialLoad = FirebaseDataManager.Instance.IsPartiallyConnected;
                              
                Debug.Log($"LoadUserData 가능 여부: {canLoad}");
                Debug.Log($"Partial Load 가능 여부: {canPartialLoad}");
                
                if (!canLoad)
                {
                    string reason = "";
                    if (!FirebaseDataManager.Instance.IsConnected) reason += "연결안됨 ";
                    if (UserDataManager.Instance == null) reason += "UserDataManager없음 ";
                    if (CleanFirebaseManager.Instance == null) reason += "FirebaseManager없음 ";
                    
                    Debug.LogWarning($"LoadUserData 불가 이유: {reason}");
                }
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
        
        Debug.Log("🏁 ===== Firebase 초기화 진단 완료 =====");
    }
}
