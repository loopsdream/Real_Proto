// MainMenuManager.cs - 메인 메뉴 관리 스크립트 (AudioManager 연동 버전)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject modeSelectPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("Sound Settings")]
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Toggle muteToggle;

    [Header("Animation")]
    public float transitionSpeed = 0.5f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 게임 시작 시 기본 설정
    void Start()
    {
        // 패널 초기 상태 설정
        ShowMainMenu();

        // 사운드 설정을 불러오기
        LoadSettings();
        
        // 로비 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySceneBGM("LobbyScene");
        }
    }

    // 스테이지 모드 시작 버튼
    public void StartGame()
    {
        PlayUISound("ButtonClick");
        AudioManager.Instance.StopBGM();
        Debug.Log("Starting Stage Mode...");
        SceneManager.LoadScene("StageModeScene");
    }

    // 게임 모드 메뉴 열기
    public void OpenModeSelect()
    {
        PlayUISound("ButtonClick");
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    // 옵션 메뉴 열기
    public void OpenOptions()
    {
        PlayUISound("ButtonClick");
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
    }

    // 크레딧 메뉴 열기
    public void OpenCredits()
    {
        PlayUISound("ButtonClick");
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(true);
        modeSelectPanel.SetActive(false);
    }

    // 메인 메뉴로 돌아가기
    public void ShowMainMenu()
    {
        PlayUISound("ButtonClick");
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
    }

    // 게임 종료 버튼
    public void QuitGame()
    {
        PlayUISound("ButtonClick");
        Debug.Log("Quitting the game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // BGM 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(volume);
        }
        else
        {
            // 폴백: PlayerPrefs에만 저장
            PlayerPrefs.SetFloat("BGMVolume", volume);
            Debug.Log("BGM Volume set to: " + volume);
        }
    }

    // 효과음 볼륨 설정
    public void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
        }
        else
        {
            // 폴백: PlayerPrefs에만 저장
            PlayerPrefs.SetFloat("SFXVolume", volume);
            Debug.Log("SFX Volume set to: " + volume);
        }
    }

    // 음소거 토글
    public void ToggleMute(bool isMuted)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMute(isMuted);
        }
        else
        {
            // 폴백: PlayerPrefs에만 저장
            PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
            Debug.Log("Mute set to: " + isMuted);
        }

        // 슬라이더 비활성화/활성화
        if (bgmSlider != null) bgmSlider.interactable = !isMuted;
        if (sfxSlider != null) sfxSlider.interactable = !isMuted;
    }

    // 레벨 디자이너 시작
    public void StartLevelDesign()
    {
        PlayUISound("ButtonClick");
        Debug.Log("Starting level design...");
        SceneManager.LoadScene("LevelDesigner");
    }

    // 무한 모드 시작
    public void StartInfiniteMode()
    {
        PlayUISound("ButtonClick");
        AudioManager.Instance.StopBGM();
        Debug.Log("Starting Infinite Mode...");
        SceneManager.LoadScene("InfiniteModeScene");
    }

    // 타이틀 씬으로 돌아가기
    public void GoToTitleScene()
    {
        PlayUISound("ButtonClick");
        Debug.Log("Going to Title Scene...");
        SceneManager.LoadScene("TitleScene");
    }

    // 로고 씬으로 돌아가기
    public void GoToLogoScene()
    {
        PlayUISound("ButtonClick");
        Debug.Log("Going to Logo Scene...");
        SceneManager.LoadScene("LogoScene");
    }

    // UI 사운드 재생 헬퍼 메서드
    void PlayUISound(string soundName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(soundName);
        }
    }

    // 설정 불러오기
    void LoadSettings()
    {
        // 기본값 설정 (없으면 기본값 사용)
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;

        // UI 요소에 값 적용
        if (bgmSlider != null) 
        {
            bgmSlider.value = bgmVolume;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }
        
        if (sfxSlider != null) 
        {
            sfxSlider.value = sfxVolume;
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        
        if (muteToggle != null) 
        {
            muteToggle.isOn = isMuted;
            muteToggle.onValueChanged.RemoveAllListeners();
            muteToggle.onValueChanged.AddListener(ToggleMute);
        }

        // AudioManager가 있으면 실제 사운드 설정에 적용
        if (AudioManager.Instance != null)
        {
            // AudioManager가 자체적으로 LoadAudioSettings()를 호출하므로 
            // 여기서는 UI만 동기화
        }
        else
        {
            // 폴백: AudioManager가 없으면 기본 동작
            ToggleMute(isMuted);
        }
    }

    // 설정 저장하기
    public void SaveSettings()
    {
        PlayUISound("ButtonClick");
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SaveAudioSettings();
        }
        else
        {
            PlayerPrefs.Save();
        }
        
        ShowMainMenu();
    }
}