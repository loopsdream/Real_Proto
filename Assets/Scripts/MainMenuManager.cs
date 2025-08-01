// MainMenuManager.cs - ���� �޴� ���� ��ũ��Ʈ
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

    // ���� �� �⺻ ����
    void Start()
    {
        // �г� �ʱ� ���� ����
        ShowMainMenu();

        // ����� ������ �ҷ�����
        LoadSettings();
    }

    // ���� ���� ��ư
    public void StartGame()
    {
        Debug.Log("Starting the game...");
        // ���� �� �ε�
        SceneManager.LoadScene("StageModeScene");
    }

    // ��� ���� �޴� ����
    public void OpenModeSelect()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    // �ɼ� �޴� ����
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
    }

    // ũ���� �޴� ����
    public void OpenCredits()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(true);
        modeSelectPanel.SetActive(false);
    }

    // ���� �޴��� ���ư���
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
    }

    // ���� ���� ��ư
    public void QuitGame()
    {
        Debug.Log("Quitting the game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // BGM ���� ����
    public void SetBGMVolume(float volume)
    {
        PlayerPrefs.SetFloat("BGMVolume", volume);
        // ���⿡ ���� BGM ���� ���� �ڵ� �߰�
        Debug.Log("BGM Volume set to: " + volume);
    }

    // ȿ���� ���� ����
    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXVolume", volume);
        // ���⿡ ���� ȿ���� ���� ���� �ڵ� �߰�
        Debug.Log("SFX Volume set to: " + volume);
    }

    // ���Ұ� ���
    public void ToggleMute(bool isMuted)
    {
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        // ���⿡ ���� ���Ұ� �ڵ� �߰�
        Debug.Log("Mute set to: " + isMuted);

        // �����̴� ��ȣ�ۿ� Ȱ��ȭ/��Ȱ��ȭ
        bgmSlider.interactable = !isMuted;
        sfxSlider.interactable = !isMuted;
    }

    public void StartLevelDesign()
    {
        Debug.Log("Starting level design...");
        // ���� ������ �� �ε�
        SceneManager.LoadScene("LevelDesigner");
    }

    public void StartInfiniteMode()
    {
        Debug.Log("Starting Infinite Mode...");
        SceneManager.LoadScene("InfiniteModeScene");
    }

    // ���� �ҷ�����
    void LoadSettings()
    {
        // ����� ���� ������ �⺻�� ���
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;

        // UI ��ҿ� �� ����
        if (bgmSlider != null) bgmSlider.value = bgmVolume;
        if (sfxSlider != null) sfxSlider.value = sfxVolume;
        if (muteToggle != null) muteToggle.isOn = isMuted;

        // ���� ����� ������ ����
        ToggleMute(isMuted);
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
    }

    // ���� �����ϱ�
    public void SaveSettings()
    {
        PlayerPrefs.Save();
        ShowMainMenu();
    }
}