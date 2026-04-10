using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// 이미지 폰트로 숫자를 표시하는 컴포넌트
[RequireComponent(typeof(HorizontalLayoutGroup))]
public class ImageFontNumber : MonoBehaviour
{
    [Header("Font Asset")]
    public ImageFontData fontData;

    [Header("Digit Display Settings")]
    public Vector2 digitSize = new Vector2(32f, 48f);
    public float spacing = -4f; // 음수면 간격 좁아짐

    [Header("Digit Image Prefab")]
    public GameObject digitImagePrefab; // Image 컴포넌트만 있는 단순 프리팹

    // 재사용 이미지 풀
    private List<Image> imagePool = new List<Image>();
    private HorizontalLayoutGroup layoutGroup;

    void Awake()
    {
        layoutGroup = GetComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = spacing;
    }

    // 단순 숫자 표시 (예: 42)
    public void SetNumber(int value)
    {
        SetText(value.ToString());
    }

    // 분수 표시 (예: 3/5)
    public void SetFraction(int current, int max)
    {
        SetText($"{current}/{max}");
    }

    // 타이머 표시 (예: 01:30)
    public void SetTimer(int minutes, int seconds)
    {
        SetText($"{minutes:D2}:{seconds:D2}");
    }

    // 문자열 기반 표시 - 내부 공통 처리
    public void SetText(string text)
    {
        if (fontData == null)
        {
            Debug.LogWarning("[ImageFontNumber] fontData not assigned.");
            return;
        }

        // 필요한 이미지 수만큼 풀 확장
        EnsurePoolSize(text.Length);

        // 각 문자에 스프라이트 할당
        for (int i = 0; i < text.Length; i++)
        {
            Image img = imagePool[i];
            img.gameObject.SetActive(true);
            img.sprite = fontData.GetCharSprite(text[i]);
            img.SetNativeSize();

            // digitSize 비율 유지하면서 크기 고정
            RectTransform rt = img.rectTransform;
            rt.sizeDelta = digitSize;
        }

        // 초과 이미지 비활성화
        for (int i = text.Length; i < imagePool.Count; i++)
        {
            imagePool[i].gameObject.SetActive(false);
        }
    }

    // 풀에 이미지가 부족하면 새로 생성
    private void EnsurePoolSize(int requiredCount)
    {
        while (imagePool.Count < requiredCount)
        {
            GameObject go;
            if (digitImagePrefab != null)
            {
                go = Instantiate(digitImagePrefab, transform);
            }
            else
            {
                go = new GameObject($"Digit_{imagePool.Count}");
                go.transform.SetParent(transform, false);
                go.AddComponent<Image>();
            }

            Image img = go.GetComponent<Image>();
            img.preserveAspect = true;
            imagePool.Add(img);
        }
    }

    // HorizontalLayoutGroup spacing 갱신
    public void SetSpacing(float newSpacing)
    {
        spacing = newSpacing;
        if (layoutGroup != null) layoutGroup.spacing = newSpacing;
    }
}