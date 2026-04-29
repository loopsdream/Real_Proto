// CheatManager.cs - 테스트용 치트 패널 (에디터 및 개발 빌드 전용)
#if UNITY_EDITOR || DEVELOPMENT_BUILD

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CheatManager : MonoBehaviour
{
    [Header("Cheat Activation")]
    [SerializeField] private int tapCountToActivate = 3;    // 활성화에 필요한 탭 횟수
    [SerializeField] private float tapTimeWindow = 2f;      // 탭 인식 시간 창 (초)
    [SerializeField] private float cornerSize = 150f;       // 우측 상단 탭 인식 영역 (픽셀)

    private bool isVisible = false;
    private int tapCount = 0;
    private float lastTapTime = 0f;
    private string stageInput = "1";
    private string coinTestAmount = "100";
    private string gemTestAmount = "5";
    private bool stylesReady = false;

    private Texture2D texPanelBg;       // 패널 배경 (짙은 회색)
    private Texture2D texButtonNormal;  // 버튼 기본 배경
    private Texture2D texButtonHover;   // 버튼 호버 배경
    private Texture2D texFieldBg;       // 텍스트 필드 배경

    private GUIStyle styleBox;
    private GUIStyle styleButton;
    private GUIStyle styleLabel;
    private GUIStyle styleField;

    private static CheatManager instance;

    void Awake()
    {
        // 씬 이동 후 중복 생성 방지
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        DetectCornerTap();
        DetectFiveFingerTouch();
    }

    void DetectCornerTap()
    {
        bool tapped = false;
        Vector2 tapPos = Vector2.zero;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            tapped = true;
            tapPos = Input.mousePosition;
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            tapped = true;
            tapPos = Input.GetTouch(0).position;
        }
#endif

        if (!tapped) return;

        // 우측 상단 코너 영역 체크
        bool inCorner = tapPos.x > Screen.width - cornerSize &&
                        tapPos.y > Screen.height - cornerSize;

        if (!inCorner) return;

        float now = Time.realtimeSinceStartup;
        if (now - lastTapTime > tapTimeWindow)
        {
            tapCount = 0;
        }
        lastTapTime = now;
        tapCount++;

        if (tapCount >= tapCountToActivate)
        {
            tapCount = 0;
            isVisible = !isVisible;
            Debug.Log($"[Cheat] Panel {(isVisible ? "opened" : "closed")}");
        }
    }

    void DetectFiveFingerTouch()
    {
#if !UNITY_EDITOR
        if (Input.touchCount >= 5)
        {
            int newTouchCount = 0;
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                    newTouchCount++;
            }

            if (newTouchCount >= 5)
            {
                isVisible = !isVisible;
                Debug.Log($"[Cheat] Five-finger touch - Panel {(isVisible ? \"opened\" : \"closed\")}");
            }
        }
#endif
    }

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        float scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
        int fontSize = Mathf.RoundToInt(24 * scale);

        // 단색 텍스처 생성 헬퍼
        texPanelBg = MakeTex(new Color(0.10f, 0.10f, 0.10f, 1f));  // 거의 검정
        texButtonNormal = MakeTex(new Color(0.22f, 0.22f, 0.22f, 1f));  // 진한 회색
        texButtonHover = MakeTex(new Color(0.35f, 0.35f, 0.35f, 1f));  // 밝은 회색
        texFieldBg = MakeTex(new Color(0.15f, 0.15f, 0.15f, 1f));  // 입력창 배경

        // 패널 Box 스타일
        styleBox = new GUIStyle(GUI.skin.box);
        styleBox.fontSize = fontSize + 4;
        styleBox.fontStyle = FontStyle.Bold;
        styleBox.normal.textColor = Color.white;
        styleBox.normal.background = texPanelBg;
        styleBox.onNormal.background = texPanelBg;

        // 버튼 스타일
        styleButton = new GUIStyle(GUI.skin.button);
        styleButton.fontSize = fontSize;
        styleButton.fontStyle = FontStyle.Bold;
        styleButton.normal.textColor = Color.white;
        styleButton.hover.textColor = Color.yellow;
        styleButton.active.textColor = Color.yellow;
        styleButton.normal.background = texButtonNormal;
        styleButton.hover.background = texButtonHover;
        styleButton.active.background = texButtonHover;

        // 레이블 스타일
        styleLabel = new GUIStyle(GUI.skin.label);
        styleLabel.fontSize = fontSize;
        styleLabel.normal.textColor = Color.white;

        // 텍스트 필드 스타일
        styleField = new GUIStyle(GUI.skin.textField);
        styleField.fontSize = fontSize;
        styleField.normal.textColor = Color.white;
        styleField.focused.textColor = Color.white;
        styleField.normal.background = texFieldBg;
        styleField.focused.background = texFieldBg;
    }

    // 단색 1x1 텍스처 생성
    Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (!isVisible) return;

        InitStyles();

        float btnH = 70f;
        float gap = 14f;
        float panelW = Mathf.Min(560f, Screen.width - 40f);
        
        bool isStageScene = SceneManager.GetActiveScene().name == "StageModeScene";

        int stageOnlyRows = isStageScene ? 3 : 0;
        float panelH = btnH * (6 + stageOnlyRows) + gap * (7 + stageOnlyRows) + 80f + 36f * 3f;

        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        // 패널 외곽 여백 (테두리 효과)
        GUI.DrawTexture(new Rect(px - 2f, py - 2f, panelW + 4f, panelH + 4f),
                        MakeTex(new Color(0.5f, 0.5f, 0.5f, 1f)));
        GUI.Box(new Rect(px, py, panelW, panelH), "[ CHEAT PANEL ]", styleBox);

        float bx = px + 20f;
        float bw = panelW - 40f;
        float cy = py + 70f;
        float fieldW = bw * 0.4f;

        if (isStageScene)
        {
            // Force Clear
            if (GUI.Button(new Rect(bx, cy, bw, btnH), "Force Clear Stage", styleButton))
            {
                ForceClearStage();
            }
            cy += btnH + gap;

            // Stage Jump
            GUI.Label(new Rect(bx, cy, bw, 36f), "Stage Number (1-based):", styleLabel);
            cy += 36f;
            float jumpBtnW = bw - fieldW - 10f;   // fieldW 그대로 사용
            stageInput = GUI.TextField(new Rect(bx, cy, fieldW, btnH), stageInput, 4, styleField);
            if (GUI.Button(new Rect(bx + fieldW + 10f, cy, jumpBtnW, btnH), "Jump To Stage", styleButton))
            {
                JumpToStage();
            }
            cy += btnH + gap;
        }

        // --- 공통 치트 (모든 씬) ---
        if (GUI.Button(new Rect(bx, cy, bw, btnH), "Fill Energy (Max)", styleButton))
        {
            FillEnergy();
        }
        cy += btnH + gap;

        // --- 에너지 충전 ---
        if (GUI.Button(new Rect(bx, cy, bw, btnH), "Fill Energy (Max)", styleButton))
        {
            FillEnergy();
        }
        cy += btnH + gap;

        // --- [추가] 에너지 소비 테스트 ---
        if (GUI.Button(new Rect(bx, cy, bw, btnH), "Spend 1 Energy (Server)", styleButton))
        {
            SpendTestEnergy();
        }
        cy += btnH + gap;

        // --- [추가] 골드 테스트 ---
        float halfW = (bw - 10f) * 0.5f;
        GUI.Label(new Rect(bx, cy, bw, 36f), "Gold Test Amount:", styleLabel);
        cy += 36f;
        coinTestAmount = GUI.TextField(new Rect(bx, cy, fieldW, btnH), coinTestAmount, 8, styleField);
        if (GUI.Button(new Rect(bx + fieldW + 10f, cy, halfW, btnH), "Add Gold", styleButton))
        {
            AddTestGold();
        }
        cy += btnH + gap;

        if (GUI.Button(new Rect(bx, cy, bw, btnH), "Spend Gold (same amount)", styleButton))
        {
            SpendTestGold();
        }
        cy += btnH + gap;

        // --- [추가] 젬 테스트 ---
        GUI.Label(new Rect(bx, cy, bw, 36f), "Gem Test Amount:", styleLabel);
        cy += 36f;
        gemTestAmount = GUI.TextField(new Rect(bx, cy, fieldW, btnH), gemTestAmount, 8, styleField);
        if (GUI.Button(new Rect(bx + fieldW + 10f, cy, halfW, btnH), "Add Gems", styleButton))
        {
            AddTestGems();
        }
        cy += btnH + gap;

        if (GUI.Button(new Rect(bx, cy, bw, btnH), "Spend Gems (same amount)", styleButton))
        {
            SpendTestGems();
        }
        cy += btnH + gap;

        // --- 닫기 ---
        if (GUI.Button(new Rect(bx, cy, bw, btnH), "Close", styleButton))
        {
            isVisible = false;
        }
    }

    // --- 치트 기능 구현 ---

    void ForceClearStage()
    {
        if (StageManager.Instance == null)
        {
            Debug.Log("[Cheat] StageManager.Instance not found");
            return;
        }
        isVisible = false;
        // 빈 보상 리스트로 강제 클리어 호출
        StageManager.Instance.OnStageCleared(new List<RewardItem>());
        Debug.Log("[Cheat] Force stage clear triggered");
    }

    void JumpToStage()
    {
        if (StageManager.Instance == null)
        {
            Debug.Log("[Cheat] StageManager.Instance not found");
            return;
        }
        if (!int.TryParse(stageInput, out int stageNum) || stageNum < 1)
        {
            Debug.Log("[Cheat] Invalid stage number: " + stageInput);
            return;
        }
        int stageIndex = stageNum - 1; // UI는 1-based, LoadStage는 0-based
        isVisible = false;
        StageManager.Instance.LoadStage(stageIndex);
        Debug.Log($"[Cheat] Jumped to stage {stageNum} (index {stageIndex})");
    }

    // [수정] 서버 경유 방식으로 변경
    void FillEnergy()
    {
        if (UserDataManager.Instance == null)
        {
            Debug.Log("[Cheat] UserDataManager.Instance not found");
            return;
        }
        int current = UserDataManager.Instance.GetEnergy();
        int max = UserDataManager.Instance.GetMaxEnergy();
        int needed = max - current;
        if (needed <= 0)
        {
            Debug.Log("[Cheat] Energy already full");
            return;
        }
        Debug.Log($"[Cheat] Adding {needed} energy...");
        UserDataManager.Instance.AddEnergy(needed, "cheat_fill", (success) =>
        {
            Debug.Log($"[Cheat] FillEnergy result: {(success ? "SUCCESS" : "FAILED")} - Energy: {UserDataManager.Instance.GetEnergy()}/{max}");
        });
    }

    // [추가] 골드 추가 테스트
    void AddTestGold()
    {
        if (UserDataManager.Instance == null) { Debug.Log("[Cheat] UserDataManager not found"); return; }
        if (!int.TryParse(coinTestAmount, out int amount) || amount <= 0) { Debug.Log("[Cheat] Invalid gold amount"); return; }

        Debug.Log($"[Cheat] Adding {amount} gold...");
        UserDataManager.Instance.AddGameCoins(amount, "cheat_test", (success) =>
        {
            Debug.Log($"[Cheat] AddGold result: {(success ? "SUCCESS" : "FAILED")} - Balance: {UserDataManager.Instance.GetGameCoins()}");
        });
    }

    // [추가] 골드 소비 테스트
    void SpendTestGold()
    {
        if (UserDataManager.Instance == null) { Debug.Log("[Cheat] UserDataManager not found"); return; }
        if (!int.TryParse(coinTestAmount, out int amount) || amount <= 0) { Debug.Log("[Cheat] Invalid gold amount"); return; }

        Debug.Log($"[Cheat] Spending {amount} gold...");
        UserDataManager.Instance.SpendGameCoins(amount, (success) =>
        {
            Debug.Log($"[Cheat] SpendGold result: {(success ? "SUCCESS" : "FAILED")} - Balance: {UserDataManager.Instance.GetGameCoins()}");
        });
    }

    // [추가] 젬 추가 테스트
    void AddTestGems()
    {
        if (UserDataManager.Instance == null) { Debug.Log("[Cheat] UserDataManager not found"); return; }
        if (!int.TryParse(gemTestAmount, out int amount) || amount <= 0) { Debug.Log("[Cheat] Invalid gem amount"); return; }

        Debug.Log($"[Cheat] Adding {amount} gems...");
        UserDataManager.Instance.AddDiamonds(amount, "cheat_test", (success) =>
        {
            Debug.Log($"[Cheat] AddGems result: {(success ? "SUCCESS" : "FAILED")} - Balance: {UserDataManager.Instance.GetDiamonds()}");
        });
    }

    // [추가] 젬 소비 테스트
    void SpendTestGems()
    {
        if (UserDataManager.Instance == null) { Debug.Log("[Cheat] UserDataManager not found"); return; }
        if (!int.TryParse(gemTestAmount, out int amount) || amount <= 0) { Debug.Log("[Cheat] Invalid gem amount"); return; }

        Debug.Log($"[Cheat] Spending {amount} gems...");
        UserDataManager.Instance.SpendDiamonds(amount, (success) =>
        {
            Debug.Log($"[Cheat] SpendGems result: {(success ? "SUCCESS" : "FAILED")} - Balance: {UserDataManager.Instance.GetDiamonds()}");
        });
    }

    // [추가] 에너지 소비 테스트
    void SpendTestEnergy()
    {
        if (UserDataManager.Instance == null)
        {
            Debug.Log("[Cheat] UserDataManager.Instance not found");
            return;
        }
        Debug.Log("[Cheat] Spending 1 energy...");
        UserDataManager.Instance.SpendEnergy(1, (success) =>
        {
            Debug.Log($"[Cheat] SpendEnergy result: {(success ? "SUCCESS" : "FAILED")} - Energy: {UserDataManager.Instance.GetEnergy()}/{UserDataManager.Instance.GetMaxEnergy()}");
        });
    }
}

#endif