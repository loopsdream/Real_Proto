// AudioDebugger.cs - AudioManager 디버그 도구
using UnityEngine;
using UnityEngine.Audio;

public class AudioDebugger : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== AudioDebugger Started ===");
        
        // AudioManager 존재 확인
        if (AudioManager.Instance == null)
        {
            Debug.LogError("❌ AudioManager.Instance is NULL!");
            return;
        }
        
        Debug.Log("✅ AudioManager.Instance found!");
        
        // AudioManager GameObject 확인
        GameObject audioManagerGO = AudioManager.Instance.gameObject;
        Debug.Log($"AudioManager GameObject: {audioManagerGO.name}");
        
        // AudioSource 컴포넌트 확인
        AudioSource[] audioSources = audioManagerGO.GetComponents<AudioSource>();
        Debug.Log($"AudioSource count: {audioSources.Length}");
        
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            Debug.Log($"AudioSource [{i}]: Clip={source.clip?.name ?? "NULL"}, Volume={source.volume}, Mute={source.mute}, Playing={source.isPlaying}");
        }
        
        // AudioMixer 확인
        AudioMixer mixer = AudioManager.Instance.audioMixer;
        if (mixer == null)
        {
            Debug.LogError("❌ AudioMixer is NULL!");
        }
        else
        {
            Debug.Log($"✅ AudioMixer found: {mixer.name}");
            
            // AudioMixer 파라미터 확인
            float value;
            if (mixer.GetFloat("BGMVolume", out value))
            {
                Debug.Log($"BGMVolume: {value} dB");
            }
            if (mixer.GetFloat("SFXVolume", out value))
            {
                Debug.Log($"SFXVolume: {value} dB");
            }
            if (mixer.GetFloat("MasterVolume", out value))
            {
                Debug.Log($"MasterVolume: {value} dB");
            }
        }
        
        // Sound 배열 확인
        Debug.Log($"BGM Sounds count: {AudioManager.Instance.bgmSounds?.Length ?? 0}");
        Debug.Log($"SFX Sounds count: {AudioManager.Instance.sfxSounds?.Length ?? 0}");
        Debug.Log($"UI Sounds count: {AudioManager.Instance.uiSounds?.Length ?? 0}");
        
        // 사운드 상세 정보
        if (AudioManager.Instance.bgmSounds != null)
        {
            foreach (var sound in AudioManager.Instance.bgmSounds)
            {
                Debug.Log($"BGM: {sound.name} - Clip={sound.clip?.name ?? "NULL"}, Volume={sound.volume}, Loop={sound.loop}");
            }
        }
        
        if (AudioManager.Instance.sfxSounds != null)
        {
            foreach (var sound in AudioManager.Instance.sfxSounds)
            {
                Debug.Log($"SFX: {sound.name} - Clip={sound.clip?.name ?? "NULL"}, Volume={sound.volume}");
            }
        }
        
        if (AudioManager.Instance.uiSounds != null)
        {
            foreach (var sound in AudioManager.Instance.uiSounds)
            {
                Debug.Log($"UI: {sound.name} - Clip={sound.clip?.name ?? "NULL"}, Volume={sound.volume}");
            }
        }
        
        Debug.Log("=== AudioDebugger End ===");
    }
    
    // 테스트 메서드들
    [ContextMenu("Test Play BGM")]
    public void TestPlayBGM()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("LobbyBGM");
            Debug.Log("Attempted to play LobbyBGM");
        }
    }
    
    [ContextMenu("Test Play UI Sound")]
    public void TestPlayUISound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI("ButtonClick");
            Debug.Log("Attempted to play ButtonClick");
        }
    }
    
    [ContextMenu("Test Volume Controls")]
    public void TestVolumeControls()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(0.5f);
            AudioManager.Instance.SetSFXVolume(0.5f);
            Debug.Log("Set volumes to 50%");
        }
    }
}
