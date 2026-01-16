#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class JSONToStageConverter : EditorWindow
{
    private string jsonInput = "";
    private Vector2 scrollPosition;

    [MenuItem("Tools/JSON to StageData Converter")]
    public static void ShowWindow()
    {
        GetWindow<JSONToStageConverter>("JSON to StageData");
    }

private void OnGUI()
    {
        GUILayout.Label("JSON to StageData Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Paste JSON data from Stage Editor below:");

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        jsonInput = EditorGUILayout.TextArea(jsonInput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        if (GUILayout.Button("Convert to StageData", GUILayout.Height(30)))
        {
            ConvertJSONToStageData();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Load JSON from File"))
        {
            LoadJSONFromFile();
        }

        if (GUILayout.Button("Paste from Clipboard"))
        {
            jsonInput = GUIUtility.systemCopyBuffer;
        }
    }

private void ConvertJSONToStageData()
    {
        if (string.IsNullOrEmpty(jsonInput))
        {
            EditorUtility.DisplayDialog("Error", "JSON data is empty.", "OK");
            return;
        }

        try
        {
            // JSON Parse
            StageDataJSON jsonData = JsonUtility.FromJson<StageDataJSON>(jsonInput);

            // StageData Create
            StageData stageData = ScriptableObject.CreateInstance<StageData>();

            // Basic info
            stageData.stageNumber = jsonData.stageNumber;
            stageData.stageName = jsonData.stageName;
            stageData.stageDescription = jsonData.stageDescription;

            // Grid settings
            stageData.gridWidth = jsonData.gridWidth;
            stageData.gridHeight = jsonData.gridHeight;

            // Block pattern
            stageData.blockPattern = jsonData.blockPattern;

            // Collectible pattern
            if (jsonData.collectiblePattern != null)
            {
                stageData.collectiblePattern = jsonData.collectiblePattern;
            }

            // Clear Goals
            if (jsonData.clearGoals != null && jsonData.clearGoals.Length > 0)
            {
                stageData.clearGoals = new List<ClearGoalData>();
                foreach (var jsonGoal in jsonData.clearGoals)
                {
                    ClearGoalData goal = new ClearGoalData
                    {
                        goalType = (ClearGoalType)jsonGoal.goalType,
                        targetColor = jsonGoal.targetColor,
                        targetColorCount = jsonGoal.targetColorCount,
                        collectibleType = (CollectibleType)jsonGoal.collectibleType,
                        targetCollectibleCount = jsonGoal.targetCollectibleCount
                    };
                    stageData.clearGoals.Add(goal);
                }
            }

            // Clear Conditions
            stageData.hasTimeLimit = jsonData.hasTimeLimit;
            stageData.timeLimit = jsonData.timeLimit;
            stageData.maxTaps = jsonData.maxTaps;

            // Rewards
            stageData.coinReward = jsonData.coinReward;
            stageData.diamondReward = jsonData.diamondReward;
            stageData.experienceReward = jsonData.experienceReward;

            // Special Rules
            stageData.allowColorTransform = jsonData.allowColorTransform;
            stageData.shuffleAnimationDuration = jsonData.shuffleAnimationDuration;
            stageData.blockConversionDuration = jsonData.blockConversionDuration;

            // Difficulty
            stageData.difficultyLevel = jsonData.difficultyLevel;
            stageData.showHints = jsonData.showHints;

            // blockPattern은 1차원 배열로 직접 할당
            //stageData.blockPattern = new int[jsonData.blockPattern.Length];
            //for (int i = 0; i < jsonData.blockPattern.Length; i++)
            //{
            //    stageData.blockPattern[i] = jsonData.blockPattern[i];
            //}

            // File save
            string fileName = $"Stage_{jsonData.stageNumber:D3}.asset";
            string assetPath = $"Assets/Scripts/Data/StageDatas/{fileName}";

            // Directory create
            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Overwrite check
            if (File.Exists(assetPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "File Exists",
                    $"{fileName} already exists. Overwrite?",
                    "Yes", "No");

                if (!overwrite)
                {
                    return;
                }
            }

            AssetDatabase.CreateAsset(stageData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select created asset
            Selection.activeObject = stageData;
            EditorGUIUtility.PingObject(stageData);

            EditorUtility.DisplayDialog("Success",
                $"StageData '{fileName}' created successfully!\nPath: {assetPath}", "OK");

            // Clear JSON input
            jsonInput = "";
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error",
                $"JSON conversion failed:\n{e.Message}", "OK");
            Debug.LogError($"JSON to StageData conversion failed: {e}");
        }
    }

private void LoadJSONFromFile()
    {
        string filePath = EditorUtility.OpenFilePanel("Select JSON file", "", "json");
        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                jsonInput = File.ReadAllText(filePath);
                EditorUtility.DisplayDialog("Success", "JSON file loaded successfully.", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to load file:\n{e.Message}", "OK");
            }
        }
    }
}

[System.Serializable]
public class StageDataJSON
{
    public int stageNumber;
    public string stageName;
    public string stageDescription;
    public int gridWidth;
    public int gridHeight;
    public int[] blockPattern;
    public int[] collectiblePattern;

    // Clear Goals
    public ClearGoalDataJSON[] clearGoals;

    // Conditions
    public bool hasTimeLimit;
    public float timeLimit;
    public int maxTaps;

    // Rewards
    public int coinReward;
    public int diamondReward;
    public int experienceReward;

    // Special Rules
    public bool allowColorTransform;
    public float shuffleAnimationDuration;
    public float blockConversionDuration;

    // Difficulty
    public int difficultyLevel;
    public bool showHints;
}

// Clear Goal을 위한 JSON 구조
[System.Serializable]
public class ClearGoalDataJSON
{
    public int goalType;  // 0=DestroyAll, 1=CollectColor, 2=CollectItems
    public int targetColor;  // For CollectColorBlocks
    public int targetColorCount;
    public int collectibleType;  // For CollectCollectibles
    public int targetCollectibleCount;
}

public class BatchJSONConverter : EditorWindow
{
    [MenuItem("Tools/Batch JSON to StageData")]
    public static void ShowBatchWindow()
    {
        GetWindow<BatchJSONConverter>("Batch JSON Converter");
    }

private void OnGUI()
    {
        GUILayout.Label("Batch JSON to StageData Converter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Select a folder containing JSON files to convert all at once.");
        GUILayout.Space(10);

        if (GUILayout.Button("Convert All JSON Files in Folder", GUILayout.Height(30)))
        {
            BatchConvertJSONFiles();
        }
    }

private void BatchConvertJSONFiles()
    {
        string folderPath = EditorUtility.OpenFolderPanel("Select JSON folder", "", "");
        if (string.IsNullOrEmpty(folderPath))
            return;

        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
        int successCount = 0;
        int failCount = 0;

        foreach (string jsonFile in jsonFiles)
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonFile);
                StageDataJSON jsonData = JsonUtility.FromJson<StageDataJSON>(jsonContent);

                StageData stageData = ScriptableObject.CreateInstance<StageData>();

                stageData.stageNumber = jsonData.stageNumber;
                stageData.stageName = jsonData.stageName;
                stageData.gridWidth = jsonData.gridWidth;
                stageData.gridHeight = jsonData.gridHeight;
                stageData.timeLimit = jsonData.timeLimit;
                stageData.coinReward = jsonData.coinReward;
                stageData.experienceReward = jsonData.experienceReward;
                stageData.difficultyLevel = jsonData.difficultyLevel;
                stageData.allowColorTransform = jsonData.allowColorTransform;
                stageData.showHints = jsonData.showHints;

                // blockPattern은 1차원 배열로 직접 할당
                stageData.blockPattern = new int[jsonData.blockPattern.Length];
                for (int i = 0; i < jsonData.blockPattern.Length; i++)
                {
                    stageData.blockPattern[i] = jsonData.blockPattern[i];
                }

                string fileName = $"Stage_{jsonData.stageNumber:D3}.asset";
                string assetPath = $"Assets/Scripts/Data/StageDatas/{fileName}";

                // Directory create
                string directory = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(stageData, assetPath);
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to convert {Path.GetFileName(jsonFile)}: {e.Message}");
                failCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Batch conversion completed",
            $"Success: {successCount} files\nFailed: {failCount} files\n\nStageData assets saved to Assets/StageData/", "OK");
    }
}
#endif