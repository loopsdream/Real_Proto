// CROxCROBlockEffectSystem.cs - CROxCRO 게임용 블록 파괴 이펙트
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CROxCROBlockEffectSystem : MonoBehaviour
{
    [Header("Canvas References")]
    public Canvas gameCanvas;           // 게임의 메인 Canvas
    public Canvas effectCanvas;         // 이펙트 전용 Canvas (옵션)

    [Header("Effect Settings")]
    public int particleCount = 8;       // 파티클 개수 (8방향)
    public float particleSpeed = 300f;  // 파티클 속도
    public float particleLifetime = 1.5f; // 파티클 지속 시간
    public float particleSize = 15f;    // 파티클 크기

    [Header("Block Colors")]
    public Color[] blockColors = {      // 블록 색상별 이펙트 색상
        Color.white,    // 빈 블록
        Color.red,      // 빨간 블록
        Color.blue,     // 파란 블록  
        Color.yellow,   // 노란 블록
        Color.green,    // 초록 블록
        Color.magenta   // 보라 블록
    };

    [Header("GridManager Integration")]
    public BaseGridManager gridManager;     // GridManager 참조
    public float gridCellSize = 1.0f;   // 그리드 셀 크기

    void Start()
    {
        SetupEffectSystem();
    }

    void Update()
    {
        // 테스트용 키입력
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestBlockEffectAtRandomPosition();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestAllBlockTypeEffects();
        }
    }

    void SetupEffectSystem()
    {
        // 게임 Canvas 찾기
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
        }

        // GridManager 찾기
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<BaseGridManager>();
        }

        if (gameCanvas != null)
        {
            Debug.Log($"CROxCRO Block Effect System initialized");
            Debug.Log($"Canvas: {gameCanvas.name}");
            Debug.Log($"GridManager: {(gridManager != null ? gridManager.name : "Not Found")}");
            Debug.Log("Press T: Test random block effect");
            Debug.Log("Press Space: Test all block types");
        }
        else
        {
            Debug.LogError("No Canvas found! Effect system cannot work.");
        }
    }

    /// <summary>
    /// 메인 메서드: 지정된 위치에서 블록 파괴 이펙트 생성
    /// </summary>
    /// <param name="gridX">그리드 X 좌표</param>
    /// <param name="gridY">그리드 Y 좌표</param>
    /// <param name="blockType">블록 타입 (0=빈블록, 1=빨강, 2=파랑, 3=노랑, 4=초록, 5=보라)</param>
    public void CreateBlockDestroyEffect(int gridX, int gridY, int blockType = 1)
    {
        if (gameCanvas == null)
        {
            Debug.LogWarning("No Canvas available for effect!");
            return;
        }

        // 그리드 좌표를 UI 좌표로 변환
        Vector2 uiPosition = GridToUIPosition(gridX, gridY);

        // 블록 색상 결정
        Color effectColor = GetBlockEffectColor(blockType);

        // UI 이펙트 생성
        CreateUIExplosionEffect(uiPosition, effectColor, blockType);

        Debug.Log($"Block destroy effect created at grid({gridX},{gridY}) -> UI({uiPosition.x},{uiPosition.y})");
    }

    /// <summary>
    /// UI 좌표로 직접 이펙트 생성
    /// </summary>
    public void CreateBlockDestroyEffectAtUIPosition(Vector2 uiPosition, int blockType = 1)
    {
        if (gameCanvas == null) return;

        Color effectColor = GetBlockEffectColor(blockType);
        CreateUIExplosionEffect(uiPosition, effectColor, blockType);
    }

    /// <summary>
    /// 그리드 좌표를 UI 좌표로 변환
    /// </summary>
    Vector2 GridToUIPosition(int gridX, int gridY)
    {
        if (gridManager != null)
        {
            // GridManager가 있으면 실제 그리드 설정 사용
            // 이 부분은 GridManager의 실제 구조에 맞게 수정 필요

            // 예시: 그리드 중앙을 기준으로 계산
            float worldX = (gridX - gridManager.width / 2f) * gridCellSize;
            float worldY = (gridY - gridManager.height / 2f) * gridCellSize;

            // 월드 좌표를 UI 좌표로 변환 (Canvas가 Screen Space인 경우)
            Vector3 worldPos = new Vector3(worldX, worldY, 0);
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            RectTransform canvasRect = gameCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, gameCanvas.worldCamera, out Vector2 localPoint);

            return localPoint;
        }
        else
        {
            // GridManager가 없으면 간단히 계산
            float uiX = (gridX - 3) * 80f; // 임시값
            float uiY = (3 - gridY) * 80f; // 임시값
            return new Vector2(uiX, uiY);
        }
    }

    /// <summary>
    /// 블록 타입에 따른 이펙트 색상 반환
    /// </summary>
    Color GetBlockEffectColor(int blockType)
    {
        if (blockType >= 0 && blockType < blockColors.Length)
        {
            return blockColors[blockType];
        }
        return Color.white;
    }

    /// <summary>
    /// UI 폭발 이펙트 생성 (핵심 메서드)
    /// </summary>
    void CreateUIExplosionEffect(Vector2 uiPosition, Color effectColor, int blockType)
    {
        Canvas targetCanvas = effectCanvas != null ? effectCanvas : gameCanvas;

        // 여러 개의 작은 파티클 생성
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject($"BlockEffect_Particle_{i}");
            particle.transform.SetParent(targetCanvas.transform, false);

            // Image 컴포넌트 추가
            Image image = particle.AddComponent<Image>();
            image.color = effectColor;

            // RectTransform 설정
            RectTransform rectTransform = particle.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(particleSize, particleSize);
            rectTransform.anchoredPosition = uiPosition;

            // 방향 계산 (원형으로 분산)
            float angle = i * (360f / particleCount) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // 약간의 랜덤성 추가
            direction += Random.insideUnitCircle * 0.3f;
            direction.Normalize();

            // 애니메이션 시작
            StartCoroutine(AnimateBlockParticle(rectTransform, direction, effectColor));
        }

        // 추가 효과: 중앙에서 확장되는 원형 효과
        CreateCentralExpandEffect(uiPosition, effectColor);
    }

    /// <summary>
    /// 개별 파티클 애니메이션
    /// </summary>
    System.Collections.IEnumerator AnimateBlockParticle(RectTransform rectTransform, Vector2 direction, Color startColor)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;
        float randomSpeed = particleSpeed * Random.Range(0.8f, 1.2f);
        float randomLifetime = particleLifetime * Random.Range(0.8f, 1.2f);

        Image image = rectTransform.GetComponent<Image>();

        while (elapsed < randomLifetime && rectTransform != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / randomLifetime;

            // 위치 업데이트 (감속)
            float currentSpeed = randomSpeed * (1f - progress * 0.5f);
            Vector2 newPos = startPos + direction * currentSpeed * elapsed;
            rectTransform.anchoredPosition = newPos;

            // 페이드 아웃
            if (image != null)
            {
                Color color = startColor;
                color.a = 1f - progress;
                image.color = color;
            }

            // 크기 변화
            float scale = 1f - progress * 0.3f;
            rectTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (rectTransform != null)
        {
            Destroy(rectTransform.gameObject);
        }
    }

    /// <summary>
    /// 중앙 확장 효과
    /// </summary>
    void CreateCentralExpandEffect(Vector2 uiPosition, Color effectColor)
    {
        GameObject centralEffect = new GameObject("BlockEffect_Central");
        centralEffect.transform.SetParent(gameCanvas.transform, false);

        Image image = centralEffect.AddComponent<Image>();
        image.color = effectColor;

        RectTransform rectTransform = centralEffect.GetComponent<RectTransform>();
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = uiPosition;

        StartCoroutine(AnimateCentralExpand(rectTransform, effectColor));
    }

    /// <summary>
    /// 중앙 확장 애니메이션
    /// </summary>
    System.Collections.IEnumerator AnimateCentralExpand(RectTransform rectTransform, Color startColor)
    {
        float duration = 0.3f;
        float maxSize = particleSize * 3f;
        float elapsed = 0f;

        Image image = rectTransform.GetComponent<Image>();

        while (elapsed < duration && rectTransform != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 크기 확장
            float currentSize = maxSize * progress;
            rectTransform.sizeDelta = new Vector2(currentSize, currentSize);

            // 페이드 아웃
            if (image != null)
            {
                Color color = startColor;
                color.a = (1f - progress) * 0.5f;
                image.color = color;
            }

            yield return null;
        }

        if (rectTransform != null)
        {
            Destroy(rectTransform.gameObject);
        }
    }

    #region 테스트 메서드들

    void TestBlockEffectAtRandomPosition()
    {
        Vector2 randomUIPos = new Vector2(
            Random.Range(-200f, 200f),
            Random.Range(-200f, 200f)
        );

        int randomBlockType = Random.Range(1, blockColors.Length);
        CreateBlockDestroyEffectAtUIPosition(randomUIPos, randomBlockType);

        Debug.Log($"Test effect created at UI position {randomUIPos} with block type {randomBlockType}");
    }

    void TestAllBlockTypeEffects()
    {
        Debug.Log("Testing all block type effects...");

        for (int blockType = 1; blockType < blockColors.Length; blockType++)
        {
            Vector2 testPos = new Vector2((blockType - 3) * 100f, 0);
            StartCoroutine(DelayedTestEffect(testPos, blockType, blockType * 0.2f));
        }
    }

    System.Collections.IEnumerator DelayedTestEffect(Vector2 position, int blockType, float delay)
    {
        yield return new WaitForSeconds(delay);
        CreateBlockDestroyEffectAtUIPosition(position, blockType);
    }

    #endregion
}