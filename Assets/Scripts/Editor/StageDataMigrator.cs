// StageDataMigrator.cs - StageData 구조 변경 시 일괄 마이그레이션 도구
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class StageDataMigrator : EditorWindow
{
    private Vector2 scrollPosition;
    private List<StageData> foundStageData = new List<StageData>();
    private bool showPreview = false;
    
    // 마이그레이션 옵션들
    private bool updateTimeLimit = false;
    private float newTimeLimit = 180f;
    
    private bool updateRewards = false;
    private int baseCoinReward = 100;
    private int baseDiamondReward = 0;
    
    private bool updateDifficulty = false;
    private bool recalculateDifficulty = true;
    
    private bool updateNewFields = false;
    private bool allowColorTransform = true;
    private bool showHints = true;

    [MenuItem("Tools/StageData Migrator")]
    public static void ShowWindow()
    {
        GetWindow<StageDataMigrator>("StageData Migrator");
    }
    
    private void OnEnable()
    {
        RefreshStageDataList();
    }
    
    private void OnGUI()
    {
        GUILayout.Label("StageData Migration Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 스테이지 데이터 목록 섹션
        DrawStageDataListSection();
        
        GUILayout.Space(10);
        
        // 마이그레이션 옵션 섹션
        DrawMigrationOptionsSection();
        
        GUILayout.Space(10);
        
        // 실행 버튼들
        DrawActionButtonsSection();
        
        // 미리보기
        if (showPreview)
        {
            DrawPreviewSection();
        }
    }
    
    private void DrawStageDataListSection()
    {
        GUILayout.Label($"Found StageData Assets: {foundStageData.Count}", EditorStyles.boldLabel);
        
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh List"))
            {
                RefreshStageDataList();
            }
            
            if (GUILayout.Button("Select All in Project"))
            {
                Selection.objects = foundStageData.ToArray();
            }
        }
        
        // 스크롤 뷰로 스테이지 데이터 목록 표시
        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(150)))
        {
            scrollPosition = scrollView.scrollPosition;
            
            foreach (var stageData in foundStageData)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(stageData, typeof(StageData), false);
                    GUILayout.Label($"Stage {stageData.stageNumber}", GUILayout.Width(80));
                    GUILayout.Label($"Lv.{stageData.difficultyLevel}", GUILayout.Width(50));
                }
            }
        }
    }
    
    private void DrawMigrationOptionsSection()
    {
        GUILayout.Label("Migration Options", EditorStyles.boldLabel);
        
        // 시간 제한 업데이트
        updateTimeLimit = EditorGUILayout.Toggle("Update Time Limit", updateTimeLimit);
        if (updateTimeLimit)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                newTimeLimit = EditorGUILayout.FloatField("New Time Limit", newTimeLimit);
            }
        }
        
        // 보상 업데이트
        updateRewards = EditorGUILayout.Toggle("Update Rewards", updateRewards);
        if (updateRewards)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                baseCoinReward = EditorGUILayout.IntField("Base Coin Reward", baseCoinReward);
                baseDiamondReward = EditorGUILayout.IntField("Base Diamond Reward", baseDiamondReward);
                EditorGUILayout.HelpBox("Final reward = Base × (Difficulty Level × 0.5 + Stage Number × 0.01)", MessageType.Info);
            }
        }
        
        // 난이도 업데이트
        updateDifficulty = EditorGUILayout.Toggle("Update Difficulty", updateDifficulty);
        if (updateDifficulty)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                recalculateDifficulty = EditorGUILayout.Toggle("Recalculate Based on Stage Number", recalculateDifficulty);
                if (!recalculateDifficulty)
                {
                    EditorGUILayout.HelpBox("Manual difficulty assignment not implemented in this example", MessageType.Warning);
                }
            }
        }
        
        // 새 필드 업데이트 (StageData에 새 필드가 추가된 경우)
        updateNewFields = EditorGUILayout.Toggle("Update New Fields", updateNewFields);
        if (updateNewFields)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                allowColorTransform = EditorGUILayout.Toggle("Allow Color Transform", allowColorTransform);
                showHints = EditorGUILayout.Toggle("Show Hints", showHints);
            }
        }
    }
    
    private void DrawActionButtonsSection()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Changes"))
            {
                showPreview = !showPreview;
            }
            
            GUI.enabled = HasAnyMigrationOption();
            if (GUILayout.Button("Apply Migration", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Migration", 
                    $"Are you sure you want to migrate {foundStageData.Count} StageData assets?\n\nThis action cannot be undone.", 
                    "Apply", "Cancel"))
                {
                    ApplyMigration();
                }
            }
            GUI.enabled = true;
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Create Backup of All StageData"))
        {
            CreateBackup();
        }
    }
    
    private void DrawPreviewSection()
    {
        GUILayout.Label("Preview Changes", EditorStyles.boldLabel);
        
        using (var scrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(200)))
        {
            foreach (var stageData in foundStageData.GetRange(0, Mathf.Min(5, foundStageData.Count))) // 처음 5개만 미리보기
            {
                GUILayout.Label($"Stage {stageData.stageNumber} - {stageData.stageName}", EditorStyles.boldLabel);
                
                using (new EditorGUI.IndentLevelScope())
                {
                    if (updateTimeLimit)
                    {
                        GUILayout.Label($"Time Limit: {stageData.timeLimit} → {newTimeLimit}");
                    }
                    
                    if (updateRewards)
                    {
                        int newCoinReward = CalculateNewCoinReward(stageData);
                        GUILayout.Label($"Coin Reward: {stageData.coinReward} → {newCoinReward}");
                    }
                    
                    if (updateDifficulty && recalculateDifficulty)
                    {
                        int newDifficulty = CalculateNewDifficulty(stageData.stageNumber);
                        GUILayout.Label($"Difficulty: {stageData.difficultyLevel} → {newDifficulty}");
                    }
                }
                
                GUILayout.Space(5);
            }
            
            if (foundStageData.Count > 5)
            {
                GUILayout.Label($"... and {foundStageData.Count - 5} more");
            }
        }
    }
    
    private void RefreshStageDataList()
    {
        foundStageData.Clear();
        
        string[] guids = AssetDatabase.FindAssets("t:StageData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageData stageData = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stageData != null)
            {
                foundStageData.Add(stageData);
            }
        }
        
        // 스테이지 번호순으로 정렬
        foundStageData.Sort((a, b) => a.stageNumber.CompareTo(b.stageNumber));
    }
    
    private bool HasAnyMigrationOption()
    {
        return updateTimeLimit || updateRewards || updateDifficulty || updateNewFields;
    }
    
    private void ApplyMigration()
    {
        int processedCount = 0;
        
        try
        {
            AssetDatabase.StartAssetEditing();
            
            foreach (var stageData in foundStageData)
            {
                bool changed = false;
                
                if (updateTimeLimit && stageData.timeLimit != newTimeLimit)
                {
                    stageData.timeLimit = newTimeLimit;
                    changed = true;
                }
                
                if (updateRewards)
                {
                    int newCoinReward = CalculateNewCoinReward(stageData);
                    if (stageData.coinReward != newCoinReward)
                    {
                        stageData.coinReward = newCoinReward;
                        changed = true;
                    }
                    
                    if (stageData.diamondReward != baseDiamondReward)
                    {
                        stageData.diamondReward = baseDiamondReward;
                        changed = true;
                    }
                }
                
                if (updateDifficulty && recalculateDifficulty)
                {
                    int newDifficulty = CalculateNewDifficulty(stageData.stageNumber);
                    if (stageData.difficultyLevel != newDifficulty)
                    {
                        stageData.difficultyLevel = newDifficulty;
                        changed = true;
                    }
                }
                
                if (updateNewFields)
                {
                    if (stageData.allowColorTransform != allowColorTransform)
                    {
                        stageData.allowColorTransform = allowColorTransform;
                        changed = true;
                    }
                    
                    if (stageData.showHints != showHints)
                    {
                        stageData.showHints = showHints;
                        changed = true;
                    }
                }
                
                if (changed)
                {
                    EditorUtility.SetDirty(stageData);
                    processedCount++;
                }
                
                // 진행률 표시
                if (EditorUtility.DisplayCancelableProgressBar("Migrating StageData", 
                    $"Processing {stageData.stageName}...", 
                    (float)processedCount / foundStageData.Count))
                {
                    break;
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
        
        EditorUtility.DisplayDialog("Migration Complete", 
            $"Successfully migrated {processedCount} StageData assets.", "OK");
            
        Debug.Log($"StageData migration completed. {processedCount} assets were updated.");
    }
    
    private void CreateBackup()
    {
        string backupFolder = $"Assets/StageData_Backup_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        Directory.CreateDirectory(backupFolder);
        
        foreach (var stageData in foundStageData)
        {
            string originalPath = AssetDatabase.GetAssetPath(stageData);
            string fileName = Path.GetFileName(originalPath);
            string backupPath = Path.Combine(backupFolder, fileName);
            
            AssetDatabase.CopyAsset(originalPath, backupPath);
        }
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Backup Complete", 
            $"Backup created at: {backupFolder}\n{foundStageData.Count} files backed up.", "OK");
    }
    
    private int CalculateNewCoinReward(StageData stageData)
    {
        float multiplier = (stageData.difficultyLevel * 0.5f) + (stageData.stageNumber * 0.01f);
        return Mathf.RoundToInt(baseCoinReward * multiplier);
    }
    
    private int CalculateNewDifficulty(int stageNumber)
    {
        if (stageNumber <= 10) return 1;
        if (stageNumber <= 30) return 2;
        if (stageNumber <= 60) return 3;
        if (stageNumber <= 100) return 4;
        return 5;
    }
}
#endif