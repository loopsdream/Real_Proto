#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

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

// JSON ������ ���� (Unity JsonUtility ȣȯ)
[System.Serializable]
public class StageDataJSON
{
    public int stageNumber;
    public string stageName;
    public int gridWidth;
    public int gridHeight;
    public int[] blockPattern;  // 1D �迭�� ����
    public float timeLimit;
    public int coinReward;
    public int experienceReward;
    public int difficultyLevel;
    public bool allowColorTransform;
    public bool showHints;
}

// �ϰ� ��ȯ ����
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