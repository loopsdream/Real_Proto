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

//        // �����δ� Ư���� �ʱ�ȭ�� �ʿ� ����
//        // GridManager���� ������ Awake/Start���� �ʱ�ȭ��

//        switch (scene.name)
//        {
//            case "StageModeScene":
//                // �������� ���� StageManager�� �˾Ƽ� �ʱ�ȭ
//                // �߰� �۾� �ʿ� ����
//                break;

//            case "InfiniteModeScene":
//                // ���� ���� InfiniteModeManager�� �˾Ƽ� �ʱ�ȭ
//                // �߰� �۾� �ʿ� ����
//                break;

//            case "LobbyScene":
//                // �޸� ���� (���� ���� ������ ���ƿ� ���)
//                Resources.UnloadUnusedAssets();
//                System.GC.Collect();
//                break;
//        }
//    }
//}