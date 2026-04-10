// CheatManager.cs - 테스트용 치트 패널 (에디터 및 개발 빌드 전용)
#if UNITY_EDITOR || DEVELOPMENT_BUILD

using UnityEngine;
using System.Collections.Generic;

public class CheatManager : MonoBehaviour
{
    [Header("Cheat Activation")]
    [SerializeField] private int tapCountToActivate = 5;    // 활성화에 필요한 탭 횟수
    [SerializeField] private float tapTimeWindow = 2f;      // 탭 인식 시간 창 (초)
    [SerializeField] private float cornerSize = 150f;       // 우측 상단 탭 인식 영역 (픽셀)

    private bool isVisible = false;
    private int tapCount = 0;
    private float lastTapTime = 0f;
    private string stageInput = "1";
    private bool stylesReady = false;

    private Texture2D texPanelBg;       // 패널 배경 (짙은 회색)
    private Texture2D texButtonNormal;  // 버튼 기본 배경
    private Texture2D texButtonHover;   // 버튼 호버 배경
    private Texture2D texFieldBg;       // 텍스트 필드 배경

    private GUIStyle styleBox;
    private GUIStyle styleButton;
    private GUIStyle styleLabel;
    private GUIStyle styleField;

    void Update()
    {
        DetectCornerTap();
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
        float panelH = btnH * 5 + gap * 6 + 80f;
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        // 패널 외곽 여백 (테두리 효과)
        GUI.DrawTexture(new Rect(px - 2f, py - 2f, panelW + 4f, panelH + 4f),
                        MakeTex(new Color(0.5f, 0.5f, 0.5f, 1f)));
        GUI.Box(new Rect(px, py, panelW, panelH), "[ CHEAT PANEL ]", styleBox);

        float bx = px + 20f;
        float bw = panelW - 40f;
        float cy = py + 70f;

        // --- 강제 스테이지 클리어 ---
        if (GUI.Button(new Rect(bx, cy, bw, btnH), "Force Clear Stage", styleButton))
        {
            ForceClearStage();
        }
        cy += btnH + gap;

        // --- 스테이지 점프 ---
        // 레이블
        GUI.Label(new Rect(bx, cy, bw, 36f), "Stage Number (1-based):", styleLabel);
        cy += 36f;

        float fieldW = bw * 0.4f;
        float jumpBtnW = bw - fieldW - 10f;
        stageInput = GUI.TextField(new Rect(bx, cy, fieldW, btnH), stageInput, 4, styleField);
        if (GUI.Button(new Rect(bx + fieldW + 10f, cy, jumpBtnW, btnH), "Jump To Stage", styleButton))
        {
            JumpToStage();
        }
        cy += btnH + gap;

        // --- 에너지 충전 ---
        if (GUI.Button(new Rect(bx, cy, bw, btnH), "Fill Energy (Max)", styleButton))
        {
            FillEnergy();
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

    void FillEnergy()
    {
        if (UserDataManager.Instance == null)
        {
            Debug.Log("[Cheat] UserDataManager.Instance not found");
            return;
        }
        UserDataManager.Instance.SetEnergy(UserDataManager.Instance.maxEnergy);
        Debug.Log($"[Cheat] Energy filled to {UserDataManager.Instance.maxEnergy}");
    }
}

#endif