// AudioInitializer.cs - 씬 시작 시 AudioManager 초기화
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioInitializer : MonoBehaviour
{
    [Header("Audio Manager")]
    public GameObject audioManagerPrefab; // AudioManager 프리팹 참조
    
    void Awake()
    {
        // AudioManager가 이미 있는지 확인
        if (AudioManager.Instance == null)
        {
            // AudioManager가 없으면 프리팹에서 생성
            if (audioManagerPrefab != null)
            {
                Instantiate(audioManagerPrefab);
                Debug.Log("AudioManager created from prefab");
            }
            else
            {
                Debug.LogWarning("AudioManager prefab not assigned!");
            }
        }
        
        // 씬별 BGM 재생
        StartCoroutine(PlaySceneBGMWithDelay());
    }
    
    System.Collections.IEnumerator PlaySceneBGMWithDelay()
    {
        // AudioManager 초기화를 위해 잠깐 대기
        yield return new WaitForEndOfFrame();
        
        if (AudioManager.Instance != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            AudioManager.Instance.PlaySceneBGM(currentScene);
        }
    }
}