// MainMenuManager.cs - 메인 메뉴 관리 스크립트
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

    // 시작 시 기본 설정
    void Start()
    {
        // 패널 초기 상태 설정
        ShowMainMenu();

        // 저장된 설정값 불러오기
        LoadSettings();
    }

    // 게임 시작 버튼
    public void StartGame()
    {
        Debug.Log("Starting the game...");
        // 게임 씬 로드
        SceneManager.LoadScene("StageModeScene");
    }

    // 모드 선택 메뉴 열기
    public void OpenModeSelect()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    // 옵션 메뉴 열기
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
    }

    // 크레딧 메뉴 열기
    public void OpenCredits()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(true);
        modeSelectPanel.SetActive(false);
    }

    // 메인 메뉴로 돌아가기
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
    }

    // 게임 종료 버튼
    public void QuitGame()
    {
        Debug.Log("Quitting the game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // BGM 볼륨 변경
    public void SetBGMVolume(float volume)
    {
        PlayerPrefs.SetFloat("BGMVolume", volume);
        // 여기에 실제 BGM 볼륨 조절 코드 추가
        Debug.Log("BGM Volume set to: " + volume);
    }

    // 효과음 볼륨 변경
    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXVolume", volume);
        // 여기에 실제 효과음 볼륨 조절 코드 추가
        Debug.Log("SFX Volume set to: " + volume);
    }

    // 음소거 토글
    public void ToggleMute(bool isMuted)
    {
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        // 여기에 실제 음소거 코드 추가
        Debug.Log("Mute set to: " + isMuted);

        // 슬라이더 상호작용 활성화/비활성화
        bgmSlider.interactable = !isMuted;
        sfxSlider.interactable = !isMuted;
    }

    public void StartLevelDesign()
    {
        Debug.Log("Starting level design...");
        // 레벨 디자인 씬 로드
        SceneManager.LoadScene("LevelDesigner");
    }

    public void StartInfiniteMode()
    {
        Debug.Log("Starting Infinite Mode...");
        SceneManager.LoadScene("InfiniteModeScene");
    }

    // 설정 불러오기
    void LoadSettings()
    {
        // 저장된 값이 없으면 기본값 사용
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;

        // UI 요소에 값 적용
        if (bgmSlider != null) bgmSlider.value = bgmVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;
        if (muteToggle != null) muteToggle.isOn = isMuted;

        // 실제 오디오 설정에 적용
        ToggleMute(isMuted);
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
    }

    // 설정 저장하기
    public void SaveSettings()
    {
        PlayerPrefs.Save();
        ShowMainMenu();
    }
}