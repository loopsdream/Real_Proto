// StagePatternCreator.cs - 에디터에서 스테이지 패턴을 시각적으로 생성하는 도구
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StageData))]
public class StagePatternCreator : Editor
{
    private int selectedBlockType = 0;
    private string[] blockTypeNames = { "Empty", "Red", "Blue", "Yellow", "Green", "Purple" };
    private Color[] blockColors = {
        Color.white,
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green,
        Color.magenta
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StageData stageData = (StageData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pattern Editor", EditorStyles.boldLabel);

        // 블록 타입 선택
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selected Block Type:", GUILayout.Width(150));
        selectedBlockType = EditorGUILayout.Popup(selectedBlockType, blockTypeNames);
        EditorGUILayout.EndHorizontal();

        // 패턴 배열 초기화 버튼
        if (GUILayout.Button("Initialize Pattern Array"))
        {
            InitializePattern(stageData);
        }

        EditorGUILayout.Space();

        // 패턴 그리드 그리기
        if (stageData.blockPattern != null && stageData.blockPattern.Length == stageData.gridWidth * stageData.gridHeight)
        {
            DrawPatternGrid(stageData);
        }

        EditorGUILayout.Space();

        // 패턴 유틸리티 버튼들
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            ClearPattern(stageData);
        }
        if (GUILayout.Button("Fill Random"))
        {
            FillRandomPattern(stageData);
        }
        EditorGUILayout.EndHorizontal();

        // 미리 정의된 패턴 버튼들
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Predefined Patterns", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Heart Shape"))
        {
            CreateHeartPattern(stageData);
        }
        if (GUILayout.Button("Star Shape"))
        {
            CreateStarPattern(stageData);
        }
        if (GUILayout.Button("Diamond Shape"))
        {
            CreateDiamondPattern(stageData);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Circle Shape"))
        {
            CreateCirclePattern(stageData);
        }
        if (GUILayout.Button("Cross Shape"))
        {
            CreateCrossPattern(stageData);
        }
        if (GUILayout.Button("Checkboard Pattern"))
        {
            CreateCheckerboardPattern(stageData);
        }
        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(stageData);
        }
    }

    void InitializePattern(StageData stageData)
    {
        stageData.blockPattern = new int[stageData.gridWidth * stageData.gridHeight];
        EditorUtility.SetDirty(stageData);
    }

    void DrawPatternGrid(StageData stageData)
    {
        int gridWidth = stageData.gridWidth;
        int gridHeight = stageData.gridHeight;

        // 그리드 셀 크기 계산
        float cellSize = Mathf.Min(300f / gridWidth, 300f / gridHeight);

        EditorGUILayout.BeginVertical();

        for (int y = gridHeight - 1; y >= 0; y--) // 위에서 아래로 그리기
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < gridWidth; x++)
            {
                int index = y * gridWidth + x;
                int currentBlockType = stageData.blockPattern[index];

                // 블록 색상 설정
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = blockColors[Mathf.Clamp(currentBlockType, 0, blockColors.Length - 1)];

                // 버튼 클릭 시 블록 타입 변경
                if (GUILayout.Button(currentBlockType.ToString(), GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                {
                    stageData.blockPattern[index] = selectedBlockType;
                    EditorUtility.SetDirty(stageData);
                }

                GUI.backgroundColor = originalColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    void ClearPattern(StageData stageData)
    {
        for (int i = 0; i < stageData.blockPattern.Length; i++)
        {
            stageData.blockPattern[i] = 0; // Empty
        }
        EditorUtility.SetDirty(stageData);
    }

    void FillRandomPattern(StageData stageData)
    {
        for (int i = 0; i < stageData.blockPattern.Length; i++)
        {
            stageData.blockPattern[i] = Random.Range(0, 6); // 0-5 random
        }
        EditorUtility.SetDirty(stageData);
    }

    void CreateHeartPattern(StageData stageData)
    {
        ClearPattern(stageData);

        int width = stageData.gridWidth;
        int height = stageData.gridHeight;
        int centerX = width / 2;
        int centerY = height / 2;

        // 간단한 하트 모양 생성
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                // 하트 모양 로직 (간단한 버전)
                float dx = x - centerX;
                float dy = y - centerY + 1;

                if ((dx * dx + dy * dy <= 4) ||
                    (Mathf.Abs(dx) <= 2 && dy <= 0 && dy >= -2))
                {
                    stageData.blockPattern[index] = 1; // Red for heart
                }
            }
        }

        EditorUtility.SetDirty(stageData);
    }

    void CreateStarPattern(StageData stageData)
    {
        ClearPattern(stageData);

        int width = stageData.gridWidth;
        int height = stageData.gridHeight;
        int centerX = width / 2;
        int centerY = height / 2;

        // 별 모양 생성
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                // 십자가 형태의 별
                if (x == centerX || y == centerY ||
                    Mathf.Abs(x - centerX) == Mathf.Abs(y - centerY))
                {
                    stageData.blockPattern[index] = 3; // Yellow for star
                }
            }
        }

        EditorUtility.SetDirty(stageData);
    }

    void CreateDiamondPattern(StageData stageData)
    {
        ClearPattern(stageData);

        int width = stageData.gridWidth;
        int height = stageData.gridHeight;
        int centerX = width / 2;
        int centerY = height / 2;
        int radius = Mathf.Min(width, height) / 3;

        // 다이아몬드 모양 생성
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                int dx = Mathf.Abs(x - centerX);
                int dy = Mathf.Abs(y - centerY);

                if (dx + dy <= radius)
                {
                    stageData.blockPattern[index] = 2; // Blue for diamond
                }
            }
        }

        EditorUtility.SetDirty(stageData);
    }

    void CreateCirclePattern(StageData stageData)
    {
        ClearPattern(stageData);

        int width = stageData.gridWidth;
        int height = stageData.gridHeight;
        int centerX = width / 2;
        int centerY = height / 2;
        float radius = Mathf.Min(width, height) / 3f;

        // 원 모양 생성
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                float dx = x - centerX;
                float dy = y - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance <= radius)
                {
                    stageData.blockPattern[index] = 4; // Green for circle
                }
            }
        }

        EditorUtility.SetDirty(stageData);
    }

    void CreateCrossPattern(StageData stageData)
    {
        ClearPattern(stageData);

        int width = stageData.gridWidth;
        int height = stageData.gridHeight;
        int centerX = width / 2;
        int centerY = height / 2;

        // 십자가 모양 생성
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                // 세로선 또는 가로선
                if (x == centerX || y == centerY)
                {
                    stageData.blockPattern[index] = 5; // Purple for cross
                }
            }
        }

        EditorUtility.SetDirty(stageData);
    }

    void CreateCheckerboardPattern(StageData stageData)
    {
        ClearPattern(stageData);

        int width = stageData.gridWidth;
        int height = stageData.gridHeight;

        // 체크보드 패턴 생성
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                // 체크보드 패턴
                if ((x + y) % 2 == 0)
                {
                    stageData.blockPattern[index] = 1; // Red
                }
                else
                {
                    stageData.blockPattern[index] = 2; // Blue
                }
            }
        }

        EditorUtility.SetDirty(stageData);
    }
}
#endif

// 추가적인 패턴 생성 유틸리티
public static class PatternUtils
{
    public static int[] CreateLetterPattern(char letter, int width, int height)
    {
        int[] pattern = new int[width * height];

        // 문자별 패턴 생성 로직
        switch (char.ToUpper(letter))
        {
            case 'A':
                return CreateLetterA(width, height);
            case 'B':
                return CreateLetterB(width, height);
            // 필요한 다른 문자들 추가...
            default:
                return pattern;
        }
    }

    static int[] CreateLetterA(int width, int height)
    {
        int[] pattern = new int[width * height];
        int centerX = width / 2;

        // A 모양 생성 로직
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                // A의 세로선과 가로선
                if ((x == centerX - 1 || x == centerX + 1) && y >= height / 3)
                {
                    pattern[index] = 1; // Red
                }
                else if (y == height / 2 && x >= centerX - 1 && x <= centerX + 1)
                {
                    pattern[index] = 1; // Red
                }
                else if (y == height - 1 && x == centerX)
                {
                    pattern[index] = 1; // Red
                }
            }
        }

        return pattern;
    }

    static int[] CreateLetterB(int width, int height)
    {
        int[] pattern = new int[width * height];
        // B 모양 생성 로직 구현...
        return pattern;
    }
}