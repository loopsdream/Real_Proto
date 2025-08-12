// AudioDiagnostics.cs - 상세한 오디오 진단 도구
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioDiagnostics : MonoBehaviour
{
    private AudioManager audioManager;
    private float checkInterval = 1f;
    
    void Start()
    {
        StartCoroutine(ContinuousCheck());
    }
    
    IEnumerator ContinuousCheck()
    {
        yield return new WaitForSeconds(0.5f); // 초기화 대기
        
        while (true)
        {
            CheckAudioSystem();
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    void CheckAudioSystem()
    {
        Debug.Log("=== Audio System Check ===");
        
        // AudioManager 확인
        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogError("❌ AudioManager.Instance is NULL!");
            return;
        }
        
        // AudioMixer 파라미터 확인
        if (audioManager.audioMixer != null)
        {
            float bgmVolume, sfxVolume, masterVolume;
            
            if (audioManager.audioMixer.GetFloat("BGMVolume", out bgmVolume))
            {
                Debug.Log($"📊 AudioMixer BGMVolume: {bgmVolume} dB (Linear: {DbToLinear(bgmVolume):F2})");
            }
            else
            {
                Debug.LogError("❌ Cannot get BGMVolume from AudioMixer!");
            }
            
            if (audioManager.audioMixer.GetFloat("SFXVolume", out sfxVolume))
            {
                Debug.Log($"📊 AudioMixer SFXVolume: {sfxVolume} dB (Linear: {DbToLinear(sfxVolume):F2})");
            }
            
            if (audioManager.audioMixer.GetFloat("MasterVolume", out masterVolume))
            {
                Debug.Log($"📊 AudioMixer MasterVolume: {masterVolume} dB (Linear: {DbToLinear(masterVolume):F2})");
            }
        }
        else
        {
            Debug.LogError("❌ AudioMixer is NULL!");
        }
        
        // PlayerPrefs 값 확인
        float savedBGM = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        bool savedMute = PlayerPrefs.GetInt("Muted", 0) == 1;
        Debug.Log($"💾 PlayerPrefs - BGM: {savedBGM}, SFX: {savedSFX}, Muted: {savedMute}");
        
        // 현재 재생 중인 BGM 확인
        string currentBGM = audioManager.GetCurrentBGM();
        Debug.Log($"🎵 Current BGM: {currentBGM}");
        
        // AudioSource 컴포넌트 확인
        AudioSource[] sources = audioManager.GetComponents<AudioSource>();
        Debug.Log($"🔊 AudioSource count: {sources.Length}");
        
        int playingCount = 0;
        foreach (var source in sources)
        {
            if (source.isPlaying)
            {
                playingCount++;
                Debug.Log($"  ▶️ Playing: Clip={source.clip?.name ?? "NULL"}, Volume={source.volume}, Mute={source.mute}, OutputGroup={source.outputAudioMixerGroup?.name ?? "NULL"}");
            }
        }
        
        if (playingCount == 0)
        {
            Debug.LogWarning("⚠️ No AudioSources are playing!");
        }
        
        // Sound 배열 확인
        if (audioManager.bgmSounds != null)
        {
            foreach (var sound in audioManager.bgmSounds)
            {
                if (sound.source != null && sound.source.isPlaying)
                {
                    Debug.Log($"🎵 BGM Playing: {sound.name}, Volume={sound.source.volume}, Pitch={sound.source.pitch}, Loop={sound.source.loop}");
                }
            }
        }
        
        Debug.Log("=== End Check ===");
    }
    
    float DbToLinear(float db)
    {
        return Mathf.Pow(10f, db / 20f);
    }
    
    // 테스트 메서드들
    [ContextMenu("Force Set Volume 0.75")]
    public void ForceSetVolume()
    {
        if (AudioManager.Instance != null)
        {
            Debug.Log("Forcing BGM/SFX volume to 0.75");
            AudioManager.Instance.SetBGMVolume(0.75f);
            AudioManager.Instance.SetSFXVolume(0.75f);
            AudioManager.Instance.SetMute(false);
        }
    }
    
    [ContextMenu("Test Play Sound")]
    public void TestPlaySound()
    {
        if (AudioManager.Instance != null)
        {
            // BGM 테스트
            AudioManager.Instance.PlayBGM("TitleBGM");
            Debug.Log("Attempted to play TitleBGM");
            
            // 1초 후 UI 사운드 테스트
            StartCoroutine(DelayedUISound());
        }
    }
    
    IEnumerator DelayedUISound()
    {
        yield return new WaitForSeconds(1f);
        AudioManager.Instance.PlayUI("ButtonClick");
        Debug.Log("Attempted to play ButtonClick");
    }
    
    [ContextMenu("Check Audio Listener")]
    public void CheckAudioListener()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        Debug.Log($"AudioListener count: {listeners.Length}");
        foreach (var listener in listeners)
        {
            Debug.Log($"  AudioListener on: {listener.gameObject.name}, Enabled: {listener.enabled}");
        }
        
        if (listeners.Length == 0)
        {
            Debug.LogError("❌ No AudioListener found! Audio won't play!");
        }
        else if (listeners.Length > 1)
        {
            Debug.LogWarning("⚠️ Multiple AudioListeners found! This can cause issues!");
        }
    }
}
