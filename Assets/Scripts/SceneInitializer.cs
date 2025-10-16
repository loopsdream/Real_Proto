//using UnityEngine;
//using UnityEngine.SceneManagement;

//public static class SceneInitializer
//{
//    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//    static void Initialize()
//    {
//        SceneManager.sceneLoaded += OnSceneLoaded;
//        Debug.Log("[SceneInitializer] Registered scene load handler");
//    }

//    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//    {
//        Debug.Log($"[SceneInitializer] Scene loaded: {scene.name}");

//        // 실제로는 특별한 초기화가 필요 없음
//        // GridManager들은 각자의 Awake/Start에서 초기화됨

//        switch (scene.name)
//        {
//            case "StageModeScene":
//                // 스테이지 모드는 StageManager가 알아서 초기화
//                // 추가 작업 필요 없음
//                break;

//            case "InfiniteModeScene":
//                // 무한 모드는 InfiniteModeManager가 알아서 초기화
//                // 추가 작업 필요 없음
//                break;

//            case "LobbyScene":
//                // 메모리 정리 (이전 게임 씬에서 돌아온 경우)
//                Resources.UnloadUnusedAssets();
//                System.GC.Collect();
//                break;
//        }
//    }
//}