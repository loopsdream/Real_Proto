// SceneAudioController.cs - 씬별 BGM 자동 재생 컨트롤러
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneAudioController : MonoBehaviour
{
    [Header("Scene BGM Settings")]
    public string sceneBGMName; // 이 씬에서 재생할 BGM 이름
    public bool playOnStart = true; // 씬 시작 시 자동 재생
    
    void Start()
    {
        if (playOnStart && AudioManager.Instance != null)
        {
            // 씬 이름으로 BGM 재생
            string currentScene = SceneManager.GetActiveScene().name;
            AudioManager.Instance.PlaySceneBGM(currentScene);
            
            Debug.Log($"Scene: {currentScene}, BGM: {sceneBGMName}");
        }
    }
    
    // 특정 BGM 재생 (Inspector에서 직접 설정한 경우)
    public void PlayCustomBGM()
    {
        if (!string.IsNullOrEmpty(sceneBGMName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(sceneBGMName);
        }
    }
}