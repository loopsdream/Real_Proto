// SimpleFirebaseTest.cs - 컴파일 오류 수정된 최종 버전
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleFirebaseTest : MonoBehaviour
{
    [Header("Test UI")]
    public TextMeshProUGUI statusText;
    public Button testSaveButton;
    public Button testLoadButton;
    public Button testLeaderboardButton;
    public Button anonymousLoginButton;

    [Header("Debug")]
    public KeyCode toggleKey = KeyCode.F1;

    private bool panelVisible = false;
    private FirebaseUserDataWrapper dataWrapper;

    void Start()
    {
        // 데이터 래퍼 초기화
        if (UserDataManager.Instance != null)
        {
            dataWrapper = new FirebaseUserDataWrapper(UserDataManager.Instance);
        }

        SetupButtons();
        UpdateStatus();
        
        // 기본적으로 패널 숨김
        gameObject.SetActive(panelVisible);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }

        if (panelVisible)
        {
            UpdateStatus();
        }
    }

    void SetupButtons()
    {
        if (testSaveButton) testSaveButton.onClick.AddListener(TestSave);
        if (testLoadButton) testLoadButton.onClick.AddListener(TestLoad);
        if (testLeaderboardButton) testLeaderboardButton.onClick.AddListener(TestLeaderboard);
        if (anonymousLoginButton) anonymousLoginButton.onClick.AddListener(TestAnonymousLogin);
    }

    void TogglePanel()
    {
        panelVisible = !panelVisible;
        gameObject.SetActive(panelVisible);
        
        if (panelVisible)
        {
            Debug.Log("[FirebaseTest] 🧪 테스트 패널 열림 (F1키로 토글)");
        }
    }

    void UpdateStatus()
    {
        if (statusText == null || !panelVisible) return;

        string status = "🔥 Firebase 테스트 패널\n\n";
        
        // Firebase 상태
        if (CleanFirebaseManager.Instance != null)
        {
            status += $"Firebase: {(CleanFirebaseManager.Instance.IsReady ? "✅ 준비됨" : "❌ 준비 안됨")}\n";
            status += $"로그인: {(CleanFirebaseManager.Instance.IsLoggedIn ? "✅ 로그인됨" : "❌ 로그아웃")}\n";
            status += $"온라인: {(CleanFirebaseManager.Instance.IsOnline ? "🌐 온라인" : "📱 오프라인")}\n";
            
            if (CleanFirebaseManager.Instance.IsLoggedIn)
            {
                string userId = CleanFirebaseManager.Instance.CurrentUserId;
                status += $"사용자: {userId.Substring(0, Mathf.Min(8, userId.Length))}...\n";
            }
        }
        else
        {
            status += "Firebase: ❓ 매니저 없음\n";
        }

        status += "\n";

        // 데이터 매니저 상태
        if (FirebaseDataManager.Instance != null)
        {
            status += $"데이터 동기화: {(FirebaseDataManager.Instance.IsConnected ? "✅ 연결됨" : "❌ 연결 안됨")}\n";
        }
        else
        {
            status += "데이터 매니저: ❓ 없음\n";
        }

        status += "\n";

        // 사용자 데이터
        if (UserDataManager.Instance != null)
        {
            status += "👤 사용자 데이터:\n";
            status += $"코인: {UserDataManager.Instance.GetGameCoins():N0}\n";
            status += $"다이아: {UserDataManager.Instance.GetDiamonds():N0}\n";
            status += $"에너지: {UserDataManager.Instance.GetEnergy()}/{UserDataManager.Instance.GetMaxEnergy()}\n";
            status += $"스테이지: {UserDataManager.Instance.GetCurrentStage()}\n";
            status += $"레벨: {UserDataManager.Instance.GetPlayerLevel()}\n";
        }

        status += "\n📋 F1키로 패널 토글";

        statusText.text = status;
    }

    #region 테스트 메서드들

    void TestSave()
    {
        Debug.Log("[FirebaseTest] 🔄 데이터 저장 테스트");
        
        if (UserDataManager.Instance != null)
        {
            // 테스트 데이터 추가
            UserDataManager.Instance.AddGameCoins(100);
            UserDataManager.Instance.AddDiamonds(5);
            Debug.Log("[FirebaseTest] 📝 테스트 데이터 생성: +100 코인, +5 다이아");
        }

        if (FirebaseDataManager.Instance != null && FirebaseDataManager.Instance.IsConnected)
        {
            FirebaseDataManager.Instance.ForceSyncNow();
            Debug.Log("[FirebaseTest] 📤 강제 동기화 요청");
        }
        else
        {
            Debug.Log("[FirebaseTest] ⚠️ Firebase 연결되지 않음 - 로컬에만 저장됨");
        }
    }

    void TestLoad()
    {
        Debug.Log("[FirebaseTest] 🔄 데이터 로드 테스트");
        
        if (FirebaseDataManager.Instance != null && FirebaseDataManager.Instance.IsConnected)
        {
            FirebaseDataManager.Instance.LoadUserData();
            Debug.Log("[FirebaseTest] 📥 데이터 로드 요청");
        }
        else
        {
            Debug.Log("[FirebaseTest] ⚠️ Firebase 연결되지 않음");
        }
    }

    void TestLeaderboard()
    {
        Debug.Log("[FirebaseTest] 🔄 리더보드 테스트");
        
        if (FirebaseDataManager.Instance != null && FirebaseDataManager.Instance.IsConnected)
        {
            int testScore = UnityEngine.Random.Range(1000, 9999);
            string testName = "TestPlayer" + UnityEngine.Random.Range(100, 999);
            
            FirebaseDataManager.Instance.UpdateLeaderboard("infinite", testScore, testName);
            Debug.Log($"[FirebaseTest] 🏆 리더보드 업데이트: {testName} - {testScore}점");
        }
        else
        {
            Debug.Log("[FirebaseTest] ⚠️ Firebase 연결되지 않음");
        }
    }

    void TestAnonymousLogin()
    {
        Debug.Log("[FirebaseTest] 🔄 익명 로그인 테스트");
        
        if (CleanFirebaseManager.Instance != null && CleanFirebaseManager.Instance.IsReady)
        {
            CleanFirebaseManager.Instance.SignInAnonymously();
            Debug.Log("[FirebaseTest] 🎭 익명 로그인 요청");
        }
        else
        {
            Debug.Log("[FirebaseTest] ⚠️ Firebase 준비되지 않음");
        }
    }

    #endregion
}
