// AudioManager.cs - 게임 전체 사운드 관리 시스템
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    
    public bool loop = false;
    public AudioMixerGroup mixerGroup;
    
    [HideInInspector]
    public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    
    [Header("Background Music")]
    public Sound[] bgmSounds;
    
    [Header("Sound Effects")]
    public Sound[] sfxSounds;
    
    [Header("UI Sounds")]
    public Sound[] uiSounds;

    // 현재 재생 중인 BGM
    private string currentBGM = "";
    
    // 캐시된 사운드들
    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            LoadAudioSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeAudioSources()
    {
        // BGM 사운드 초기화
        InitializeSoundArray(bgmSounds);
        
        // SFX 사운드 초기화
        InitializeSoundArray(sfxSounds);
        
        // UI 사운드 초기화
        InitializeSoundArray(uiSounds);
    }

    void InitializeSoundArray(Sound[] sounds)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.clip == null)
            {
                Debug.LogWarning($"AudioClip이 없습니다: {sound.name}");
                continue;
            }

            // AudioSource 컴포넌트 추가
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            sound.source.outputAudioMixerGroup = sound.mixerGroup;

            // 딕셔너리에 추가
            if (!soundDictionary.ContainsKey(sound.name))
            {
                soundDictionary.Add(sound.name, sound);
            }
            else
            {
                Debug.LogWarning($"중복된 사운드 이름: {sound.name}");
            }
        }
    }

    #region Public Methods

    // BGM 재생
    public void PlayBGM(string name)
    {
        // 같은 BGM이 이미 재생 중이면 무시
        if (currentBGM == name) return;

        // 현재 BGM 정지
        StopBGM();

        Sound sound = GetSound(name);
        if (sound != null)
        {
            sound.source.Play();
            currentBGM = name;
            Debug.Log($"BGM 재생: {name}");
        }
    }

    // BGM 정지
    public void StopBGM()
    {
        if (!string.IsNullOrEmpty(currentBGM))
        {
            Sound sound = GetSound(currentBGM);
            if (sound != null && sound.source.isPlaying)
            {
                sound.source.Stop();
            }
            currentBGM = "";
        }
    }

    // SFX 재생
    public void PlaySFX(string name)
    {
        Sound sound = GetSound(name);
        if (sound != null)
        {
            sound.source.PlayOneShot(sound.clip, sound.volume);
        }
    }

    // UI 사운드 재생
    public void PlayUI(string name)
    {
        Sound sound = GetSound(name);
        if (sound != null)
        {
            sound.source.PlayOneShot(sound.clip, sound.volume);
        }
    }

    // 사운드 정지
    public void StopSound(string name)
    {
        Sound sound = GetSound(name);
        if (sound != null && sound.source.isPlaying)
        {
            sound.source.Stop();
        }
    }

    // 모든 사운드 정지
    public void StopAllSounds()
    {
        foreach (var sound in soundDictionary.Values)
        {
            if (sound.source.isPlaying)
            {
                sound.source.Stop();
            }
        }
        currentBGM = "";
    }

    #endregion

    #region Volume Control

    // BGM 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (audioMixer != null)
        {
            // -80dB ~ 0dB로 변환 (로그 스케일)
            float dbVolume = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
            Debug.Log($"BGM Volume: linear={volume}, dB={dbVolume}");
            audioMixer.SetFloat("BGMVolume", dbVolume);
        }
        
        PlayerPrefs.SetFloat("BGMVolume", volume);
        Debug.Log($"BGM 볼륨 설정: {volume}");
    }

    // SFX 볼륨 설정
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (audioMixer != null)
        {
            float dbVolume = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
            Debug.Log($"SFX Volume: linear={volume}, dB={dbVolume}");
            audioMixer.SetFloat("SFXVolume", dbVolume);
        }
        
        PlayerPrefs.SetFloat("SFXVolume", volume);
        Debug.Log($"SFX 볼륨 설정: {volume}");
    }

    // 음소거 설정
    public void SetMute(bool isMuted)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", isMuted ? -80f : 0f);
        }
        
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        Debug.Log($"음소거 설정: {isMuted}");
    }

    #endregion

    #region Settings

    // 오디오 설정 로드
    void LoadAudioSettings()
    {
        // PlayerPrefs 초기화 확인
        if (!PlayerPrefs.HasKey("BGMVolume"))
        {
            PlayerPrefs.SetFloat("BGMVolume", 0.75f);
            Debug.Log("Initializing BGMVolume to 0.75");
        }
        if (!PlayerPrefs.HasKey("SFXVolume"))
        {
            PlayerPrefs.SetFloat("SFXVolume", 1.0f);
            Debug.Log("Initializing SFXVolume to 1.0");
        }

        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;

        Debug.Log($"LoadAudioSettings - BGM: {bgmVolume}, SFX: {sfxVolume}, Muted: {isMuted}");

        // 0 값 방지
        if (bgmVolume <= 0.01f) bgmVolume = 0.75f;
        if (sfxVolume <= 0.01f) sfxVolume = 1.0f;

        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
        SetMute(isMuted);
    }

    // 설정 저장
    public void SaveAudioSettings()
    {
        PlayerPrefs.Save();
    }

    #endregion

    #region Helper Methods

    Sound GetSound(string name)
    {
        if (soundDictionary.TryGetValue(name, out Sound sound))
        {
            return sound;
        }
        
        Debug.LogWarning($"사운드를 찾을 수 없습니다: {name}");
        return null;
    }

    // 사운드가 재생 중인지 확인
    public bool IsPlaying(string name)
    {
        Sound sound = GetSound(name);
        return sound != null && sound.source.isPlaying;
    }

    // 현재 BGM 이름 반환
    public string GetCurrentBGM()
    {
        return currentBGM;
    }

    #endregion

    #region Scene-Specific BGM

    // 씬별 BGM 자동 재생
    public void PlaySceneBGM(string sceneName)
    {
        string bgmName = "";
        
        switch (sceneName)
        {
            case "LogoScene":
                // 로고 씬은 조용히
                StopBGM();
                return;
                
            case "TitleScene":
                bgmName = "TitleBGM";
                break;
                
            case "LobbyScene":
                bgmName = "LobbyBGM";
                break;
                
            case "StageModeScene":
                bgmName = "GameBGM";
                break;
                
            case "InfiniteModeScene":
                bgmName = "InfiniteBGM";
                break;
                
            case "LevelDesigner":
                bgmName = "LobbyBGM"; // 로비와 같은 BGM 사용
                break;
                
            default:
                Debug.Log($"알 수 없는 씬: {sceneName}");
                return;
        }
        
        if (!string.IsNullOrEmpty(bgmName))
        {
            PlayBGM(bgmName);
        }
    }

    #endregion
}