// BatchStageCreator.cs - 배치 스테이지 생성 도구 (완성)
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class BatchStageCreator : EditorWindow
{
    [System.Serializable]
    public class StageTemplate
    {
        public string templateName;
        public int gridWidth = 8;
        public int gridHeight = 8;
        public float timeLimit = 180f;
        public int baseCoinReward = 100;
        public int difficultyLevel = 1;
        public string patternType = "Random"; // Random, Cross, Border, Center
        public int blockDensity = 50; // 0-100%
    }

    private List<StageTemplate> templates = new List<StageTemplate>();
    private int selectedTemplateIndex = 0;
    private Vector2 scrollPosition;
    
    // 배치 생성 설정
    private int startStageNumber = 1;
    private int endStageNumber = 50;
    private bool overwriteExisting = false;
    private bool autoIncrementDifficulty = true;
    
    // 패턴 생성 설정
    private int[] availableColors = {1, 2, 3, 4, 5}; // Red, Blue, Yellow, Green, Purple

    [MenuItem("Tools/Batch Stage Creator")]
    public static void ShowWindow()
    {
        GetWindow<BatchStageCreator>("Batch Stage Creator");
    }
    
    private void OnEnable()
    {
        LoadTemplates();
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Batch Stage Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        DrawTemplateSection();
        GUILayout.Space(10);
        
        DrawBatchSettingsSection();
        GUILayout.Space(10);
        
        DrawActionButtonsSection();
    }
    
    private void DrawTemplateSection()
    {
        GUILayout.Label("Stage Templates", EditorStyles.boldLabel);
        
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Template"))
            {
                templates.Add(new StageTemplate() { templateName = $"Template {templates.Count + 1}" });
            }
            
            if (GUILayout.Button("Load Default Templates"))
            {
                LoadDefaultTemplates();
            }
            
            if (GUILayout.Button("Save Templates"))
            {
                SaveTemplates();
            }
        }
        
        if (templates.Count == 0)
        {
            EditorGUILayout.HelpBox("No templates available. Add a template or load defaults.", MessageType.Info);
            return;
        }
        
        // 템플릿 선택
        string[] templateNames = new string[templates.Count];
        for (int i = 0; i < templates.Count; i++)
        {
            templateNames[i] = templates[i].templateName;
        }
        
        selectedTemplateIndex = EditorGUILayout.Popup("Selected Template", selectedTemplateIndex, templateNames);
        selectedTemplateIndex = Mathf.Clamp(selectedTemplateIndex, 0, templates.Count - 1);
        
        GUILayout.Space(5);
        
        // 선택된 템플릿 편집
        StageTemplate template = templates[selectedTemplateIndex];
        
        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(200)))
        {
            scrollPosition = scrollView.scrollPosition;
            
            template.templateName = EditorGUILayout.TextField("Template Name", template.templateName);
            
            GUILayout.Space(5);
            
            template.gridWidth = EditorGUILayout.IntSlider("Grid Width", template.gridWidth, 3, 15);
            template.gridHeight = EditorGUILayout.IntSlider("Grid Height", template.gridHeight, 3, 20);
            template.timeLimit = EditorGUILayout.FloatField("Time Limit", template.timeLimit);
            template.baseCoinReward = EditorGUILayout.IntField("Base Coin Reward", template.baseCoinReward);
            template.difficultyLevel = EditorGUILayout.IntSlider("Difficulty Level", template.difficultyLevel, 1, 5);
            
            GUILayout.Space(5);
            
            template.patternType = GetPatternTypeName(EditorGUILayout.Popup("Pattern Type", 
                GetPatternTypeIndex(template.patternType), 
                new string[] {"Random", "Cross", "Border", "Center", "Checkerboard"}));
            
            template.blockDensity = EditorGUILayout.IntSlider("Block Density (%)", template.blockDensity, 10, 90);
        }
        
        if (GUILayout.Button("Remove Selected Template") && templates.Count > 1)
        {
            templates.RemoveAt(selectedTemplateIndex);
            selectedTemplateIndex = Mathf.Clamp(selectedTemplateIndex, 0, templates.Count - 1);
        }
    }
    
    private void DrawBatchSettingsSection()
    {
        GUILayout.Label("Batch Generation Settings", EditorStyles.boldLabel);
        
        startStageNumber = EditorGUILayout.IntField("Start Stage Number", startStageNumber);
        endStageNumber = EditorGUILayout.IntField("End Stage Number", endStageNumber);
        
        if (startStageNumber > endStageNumber)
        {
            endStageNumber = startStageNumber;
        }
        
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        autoIncrementDifficulty = EditorGUILayout.Toggle("Auto Increment Difficulty", autoIncrementDifficulty);
        
        int totalStages = endStageNumber - startStageNumber + 1;
        EditorGUILayout.HelpBox($"Will create {totalStages} stages (Stage {startStageNumber} to Stage {endStageNumber})", MessageType.Info);
    }
    
    private void DrawActionButtonsSection()
    {
        GUILayout.Space(10);
        
        if (templates.Count == 0)
        {
            GUI.enabled = false;
        }
        
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview First 5 Stages"))
            {
                PreviewStages();
            }
            
            if (GUILayout.Button("Generate All Stages", GUILayout.Height(30)))
            {
                int totalStages = endStageNumber - startStageNumber + 1;
                if (EditorUtility.DisplayDialog("Confirm Batch Creation", 
                    $"Are you sure you want to create {totalStages} stages?\n\nThis may take a while.", 
                    "Create", "Cancel"))
                {
                    GenerateStages();
                }
            }
        }
        
        GUI.enabled = true;
    }
    
    private void PreviewStages()
    {
        Debug.Log("=== Stage Preview ===");
        
        int previewCount = Mathf.Min(5, endStageNumber - startStageNumber + 1);
        
        for (int i = 0; i < previewCount; i++)
        {
            int stageNumber = startStageNumber + i;
            StageTemplate template = GetTemplateForStage(stageNumber);
            
            Debug.Log($"Stage {stageNumber}: {template.templateName} - " +
                     $"{template.gridWidth}x{template.gridHeight}, " +
                     $"Difficulty {template.difficultyLevel}, " +
                     $"Pattern: {template.patternType}");
        }
        
        if (endStageNumber - startStageNumber + 1 > 5)
        {
            Debug.Log($"... and {endStageNumber - startStageNumber + 1 - 5} more stages");
        }
    }
    
    private void GenerateStages()
    {
        int successCount = 0;
        int totalStages = endStageNumber - startStageNumber + 1;
        
        // StageData 폴더 생성
        string stageDataFolder = "Assets/StageData";
        if (!Directory.Exists(stageDataFolder))
        {
            Directory.CreateDirectory(stageDataFolder);
        }
        
        try
        {
            AssetDatabase.StartAssetEditing();
            
            for (int stageNumber = startStageNumber; stageNumber <= endStageNumber; stageNumber++)
            {
                try
                {
                    string fileName = $"Stage_{stageNumber:D3}.asset";
                    string assetPath = $"{stageDataFolder}/{fileName}";
                    
                    // 기존 파일 확인
                    if (File.Exists(assetPath) && !overwriteExisting)
                    {
                        Debug.Log($"Skipping Stage {stageNumber} - already exists");
                        continue;
                    }
                    
                    StageData stageData = CreateStageData(stageNumber);
                    
                    if (File.Exists(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                    
                    AssetDatabase.CreateAsset(stageData, assetPath);
                    successCount++;
                    
                    // 진행률 표시
                    float progress = (float)(stageNumber - startStageNumber + 1) / totalStages;
                    if (EditorUtility.DisplayCancelableProgressBar("Creating Stages", 
                        $"Creating Stage {stageNumber}...", progress))
                    {
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to create Stage {stageNumber}: {e.Message}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }
        
        EditorUtility.DisplayDialog("Batch Creation Complete", 
            $"Successfully created {successCount} stages.", "OK");
            
        Debug.Log($"Batch stage creation completed. {successCount} stages created.");
    }
    
    private StageData CreateStageData(int stageNumber)
    {
        StageTemplate template = GetTemplateForStage(stageNumber);
        
        StageData stageData = ScriptableObject.CreateInstance<StageData>();
        
        stageData.stageNumber = stageNumber;
        stageData.stageName = $"Stage {stageNumber}";
        stageData.gridWidth = template.gridWidth;
        stageData.gridHeight = template.gridHeight;
        stageData.timeLimit = template.timeLimit;
        stageData.coinReward = CalculateCoinReward(stageNumber, template.baseCoinReward);
        stageData.experienceReward = 10;
        stageData.allowColorTransform = true;
        stageData.showHints = true;
        
        // 난이도 설정
        if (autoIncrementDifficulty)
        {
            stageData.difficultyLevel = CalculateDifficultyByStage(stageNumber);
        }
        else
        {
            stageData.difficultyLevel = template.difficultyLevel;
        }
        
        // 패턴 생성
        stageData.blockPattern = GeneratePattern(template);
        
        return stageData;
    }
    
    private StageTemplate GetTemplateForStage(int stageNumber)
    {
        if (templates.Count == 1)
        {
            return templates[0];
        }
        
        // 여러 템플릿이 있을 경우 순환적으로 사용
        int templateIndex = (stageNumber - 1) % templates.Count;
        return templates[templateIndex];
    }
    
    private int[] GeneratePattern(StageTemplate template)
    {
        int width = template.gridWidth;
        int height = template.gridHeight;
        int[] pattern = new int[width * height];
        
        System.Random random = new System.Random(template.GetHashCode() + System.DateTime.Now.Millisecond);
        
        switch (template.patternType)
        {
            case "Random":
                GenerateRandomPattern(pattern, width, height, template.blockDensity, random);
                break;
            case "Cross":
                GenerateCrossPattern(pattern, width, height, random);
                break;
            case "Border":
                GenerateBorderPattern(pattern, width, height, random);
                break;
            case "Center":
                GenerateCenterPattern(pattern, width, height, random);
                break;
            case "Checkerboard":
                GenerateCheckerboardPattern(pattern, width, height, random);
                break;
            default:
                GenerateRandomPattern(pattern, width, height, template.blockDensity, random);
                break;
        }
        
        return pattern;
    }
    
    private void GenerateRandomPattern(int[] pattern, int width, int height, int density, System.Random random)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            if (random.Next(100) < density)
            {
                pattern[i] = availableColors[random.Next(availableColors.Length)];
            }
            else
            {
                pattern[i] = 0; // Empty
            }
        }
    }
    
    private void GenerateCrossPattern(int[] pattern, int width, int height, System.Random random)
    {
        int centerX = width / 2;
        int centerY = height / 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                
                if (x == centerX || y == centerY)
                {
                    pattern[index] = availableColors[random.Next(availableColors.Length)];
                }
                else if (random.Next(100) < 20) // 20% chance for other positions
                {
                    pattern[index] = availableColors[random.Next(availableColors.Length)];
                }
                else
                {
                    pattern[index] = 0;
                }
            }
        }
    }
    
    private void GenerateBorderPattern(int[] pattern, int width, int height, System.Random random)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    pattern[index] = availableColors[random.Next(availableColors.Length)];
                }
                else if (random.Next(100) < 15) // 15% chance for inner positions
                {
                    pattern[index] = availableColors[random.Next(availableColors.Length)];
                }
                else
                {
                    pattern[index] = 0;
                }
            }
        }
    }
    
    private void GenerateCenterPattern(int[] pattern, int width, int height, System.Random random)
    {
        int centerX = width / 2;
        int centerY = height / 2;
        int maxRadius = Mathf.Min(width, height) / 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                
                if (distance < maxRadius * 0.7f)
                {
                    pattern[index] = availableColors[random.Next(availableColors.Length)];
                }
                else if (random.Next(100) < 10) // 10% chance for outer positions
                {
                    pattern[index] = availableColors[random.Next(availableColors.Length)];
                }
                else
                {
                    pattern[index] = 0;
                }
            }
        }
    }
    
    private void GenerateCheckerboardPattern(int[] pattern, int width, int height, System.Random random)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                
                if ((x + y) % 2 == 0)
                {
                    pattern[index] = availableColors[random.Next(availableColors.Length)];
                }
                else if (random.Next(100) < 25) // 25% chance for other squares
                {
                    pattern[index] = availableColors[random.Next(availableColors.Length)];
                }
                else
                {
                    pattern[index] = 0;
                }
            }
        }
    }
    
    private int CalculateCoinReward(int stageNumber, int baseReward)
    {
        return baseReward + (stageNumber * 10);
    }
    
    private int CalculateDifficultyByStage(int stageNumber)
    {
        if (stageNumber <= 10) return 1;
        if (stageNumber <= 30) return 2;
        if (stageNumber <= 60) return 3;
        if (stageNumber <= 100) return 4;
        return 5;
    }
    
    private int GetPatternTypeIndex(string patternType)
    {
        switch (patternType)
        {
            case "Random": return 0;
            case "Cross": return 1;
            case "Border": return 2;
            case "Center": return 3;
            case "Checkerboard": return 4;
            default: return 0;
        }
    }
    
    private string GetPatternTypeName(int index)
    {
        switch (index)
        {
            case 0: return "Random";
            case 1: return "Cross";
            case 2: return "Border";
            case 3: return "Center";
            case 4: return "Checkerboard";
            default: return "Random";
        }
    }
    
    private void LoadDefaultTemplates()
    {
        templates.Clear();
        
        templates.Add(new StageTemplate()
        {
            templateName = "Easy Random",
            gridWidth = 6,
            gridHeight = 6,
            timeLimit = 240f,
            baseCoinReward = 100,
            difficultyLevel = 1,
            patternType = "Random",
            blockDensity = 40
        });
        
        templates.Add(new StageTemplate()
        {
            templateName = "Medium Cross",
            gridWidth = 8,
            gridHeight = 8,
            timeLimit = 180f,
            baseCoinReward = 150,
            difficultyLevel = 2,
            patternType = "Cross",
            blockDensity = 60
        });
        
        templates.Add(new StageTemplate()
        {
            templateName = "Hard Border",
            gridWidth = 10,
            gridHeight = 10,
            timeLimit = 120f,
            baseCoinReward = 200,
            difficultyLevel = 3,
            patternType = "Border",
            blockDensity = 70
        });
        
        Debug.Log("Default templates loaded");
    }
    
    private void SaveTemplates()
    {
        string json = JsonUtility.ToJson(new SerializableList<StageTemplate>(templates), true);
        string path = "Assets/Editor/StageTemplates.json";
        
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        
        Debug.Log($"Templates saved to {path}");
    }
    
    private void LoadTemplates()
    {
        string path = "Assets/Editor/StageTemplates.json";
        
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SerializableList<StageTemplate> loadedTemplates = JsonUtility.FromJson<SerializableList<StageTemplate>>(json);
                templates = loadedTemplates.items;
                
                Debug.Log($"Templates loaded from {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load templates: {e.Message}");
                LoadDefaultTemplates();
            }
        }
        else
        {
            LoadDefaultTemplates();
        }
    }
    
    [System.Serializable]
    private class SerializableList<T>
    {
        public List<T> items;
        
        public SerializableList(List<T> items)
        {
            this.items = items;
        }
    }
}
#endif