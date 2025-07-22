// SafeAreaManager.cs - 노치 및 상태바 대응
using UnityEngine;

public class SafeAreaManager : MonoBehaviour
{
    [Header("Safe Area Settings")]
    public bool applyTop = true;
    public bool applyBottom = true;
    public bool applyLeft = true;
    public bool applyRight = true;

    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2 lastScreenSize = Vector2.zero;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);

        if (lastSafeArea != Screen.safeArea || lastScreenSize != currentScreenSize)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // 선택적으로 안전 영역 적용
        if (!applyLeft) anchorMin.x = 0f;
        if (!applyRight) anchorMax.x = 1f;
        if (!applyBottom) anchorMin.y = 0f;
        if (!applyTop) anchorMax.y = 1f;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2(Screen.width, Screen.height);

        Debug.Log($"Safe area applied: {safeArea}, Anchors: {anchorMin} - {anchorMax}");
    }
}