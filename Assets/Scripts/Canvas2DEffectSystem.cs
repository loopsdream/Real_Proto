// Canvas2DEffectSystem.cs - Canvas 기반 2D 게임용 이펙트 시스템
using UnityEngine;
using UnityEngine.UI;

public class Canvas2DEffectSystem : MonoBehaviour
{
    [Header("Canvas References")]
    public Canvas gameCanvas;           // 게임의 메인 Canvas
    public Canvas effectCanvas;         // 이펙트 전용 Canvas (자동 생성 가능)

    [Header("Effect Prefab")]
    public GameObject effectPrefab;     // 3D 파티클 이펙트 프리팹

    [Header("Camera References")]
    public Camera mainCamera;           // 메인 카메라
    public Camera effectCamera;         // 이펙트 전용 카메라 (자동 생성)

    void Start()
    {
        SetupCanvasEffectSystem();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestCanvasEffect();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestEffectAtCanvasCenter();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestMethod1_WorldSpaceCanvas();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestMethod2_RenderTexture();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestMethod3_UIParticle();
        }
    }

    void SetupCanvasEffectSystem()
    {
        Debug.Log("=== Setting up Canvas 2D Effect System ===");

        // 메인 Canvas 찾기
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
            if (gameCanvas != null)
            {
                Debug.Log($"Found main Canvas: {gameCanvas.name}");
                Debug.Log($"Canvas Render Mode: {gameCanvas.renderMode}");
                Debug.Log($"Canvas Scaler: {(gameCanvas.GetComponent<CanvasScaler>() != null ? "Present" : "Missing")}");
            }
            else
            {
                Debug.LogError("No Canvas found in scene!");
                return;
            }
        }

        // 메인 카메라 찾기
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Debug.Log($"Found main Camera: {mainCamera.name}");
            }
            else
            {
                Debug.LogError("No main camera found!");
                return;
            }
        }

        Debug.Log("Canvas 2D Effect System ready!");
        Debug.Log("Press T: Test basic effect");
        Debug.Log("Press Space: Test effect at canvas center");
        Debug.Log("Press 1: World Space Canvas method");
        Debug.Log("Press 2: Render Texture method");
        Debug.Log("Press 3: UI Particle method");
    }

    void TestCanvasEffect()
    {
        Debug.Log("=== Testing Canvas Effect ===");

        if (effectPrefab == null)
        {
            Debug.LogError("Effect Prefab not assigned!");
            return;
        }

        // Canvas의 중앙 위치 계산
        Vector3 canvasCenter = GetCanvasCenterWorldPosition();

        Debug.Log($"Canvas center world position: {canvasCenter}");

        // 이펙트 생성
        GameObject effect = Instantiate(effectPrefab, canvasCenter, Quaternion.identity);
        effect.name = "CANVAS_TEST_EFFECT";

        // 파티클 시스템 설정 조정
        AdjustParticleForCanvas(effect);

        Destroy(effect, 5f);

        Debug.Log("Canvas effect test created");
    }

    void TestEffectAtCanvasCenter()
    {
        Debug.Log("=== Testing Effect at Canvas Center ===");

        if (gameCanvas == null || effectPrefab == null) return;

        // Canvas UI 좌표로 중앙 지점 계산
        RectTransform canvasRect = gameCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        Vector3 centerUIPos = new Vector3(canvasSize.x / 2, canvasSize.y / 2, 0);

        // UI 좌표를 월드 좌표로 변환
        Vector3 worldPos = gameCanvas.transform.TransformPoint(centerUIPos);

        Debug.Log($"Canvas UI center: {centerUIPos}");
        Debug.Log($"World position: {worldPos}");

        // 이펙트 생성
        GameObject effect = Instantiate(effectPrefab, worldPos, Quaternion.identity);
        effect.name = "CANVAS_CENTER_EFFECT";

        AdjustParticleForCanvas(effect);

        Destroy(effect, 5f);
    }

    void TestMethod1_WorldSpaceCanvas()
    {
        Debug.Log("=== Method 1: World Space Canvas ===");

        if (gameCanvas == null || effectPrefab == null) return;

        // 기존 Canvas 설정 확인
        Debug.Log($"Current Canvas Render Mode: {gameCanvas.renderMode}");

        // 임시로 World Space로 변경해서 테스트
        RenderMode originalMode = gameCanvas.renderMode;
        gameCanvas.renderMode = RenderMode.WorldSpace;
        gameCanvas.worldCamera = mainCamera;

        // World Space에서 이펙트 테스트
        Vector3 effectPos = gameCanvas.transform.position + Vector3.forward * 2f;
        GameObject effect = Instantiate(effectPrefab, effectPos, Quaternion.identity);
        effect.name = "WORLDSPACE_CANVAS_EFFECT";

        AdjustParticleForCanvas(effect);

        Debug.Log($"Effect created at {effectPos} with WorldSpace Canvas");
        Debug.Log("Effect should be visible in front of canvas");

        // 5초 후 원래 모드로 복원
        StartCoroutine(RestoreCanvasModeAfterDelay(originalMode, 5f));

        Destroy(effect, 5f);
    }

    void TestMethod2_RenderTexture()
    {
        Debug.Log("=== Method 2: Render Texture ===");

        if (effectPrefab == null) return;

        // 이펙트 전용 카메라 생성
        if (effectCamera == null)
        {
            GameObject cameraObj = new GameObject("Effect_Camera");
            effectCamera = cameraObj.AddComponent<Camera>();
            effectCamera.transform.position = new Vector3(0, 0, -10);
            effectCamera.orthographic = true;
            effectCamera.orthographicSize = 5;
            effectCamera.backgroundColor = Color.clear;
            effectCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        // Render Texture 생성
        RenderTexture renderTexture = new RenderTexture(512, 512, 16);
        effectCamera.targetTexture = renderTexture;

        // Canvas에 RenderTexture를 표시할 UI Image 생성
        GameObject imageObj = new GameObject("Effect_Image");
        imageObj.transform.SetParent(gameCanvas.transform, false);

        RawImage rawImage = imageObj.AddComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.rectTransform.sizeDelta = new Vector2(200, 200);
        rawImage.rectTransform.anchoredPosition = Vector2.zero;

        // 이펙트를 카메라 앞에 생성
        Vector3 effectPos = effectCamera.transform.position + Vector3.forward * 5f;
        GameObject effect = Instantiate(effectPrefab, effectPos, Quaternion.identity);
        effect.name = "RENDER_TEXTURE_EFFECT";

        AdjustParticleForCanvas(effect);

        Debug.Log("Render Texture method: Effect should appear in center UI image");

        // 정리
        Destroy(effect, 5f);
        Destroy(imageObj, 5f);
        Destroy(renderTexture, 5f);
    }

    void TestMethod3_UIParticle()
    {
        Debug.Log("=== Method 3: UI Particle System ===");

        // Canvas에 직접 파티클 시스템 생성
        GameObject uiParticleObj = new GameObject("UI_Particle");
        uiParticleObj.transform.SetParent(gameCanvas.transform, false);

        // RectTransform 설정
        RectTransform rectTransform = uiParticleObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 200);
        rectTransform.anchoredPosition = Vector2.zero;

        // 파티클 시스템 추가
        ParticleSystem uiParticle = uiParticleObj.AddComponent<ParticleSystem>();

        // UI용 파티클 설정
        var main = uiParticle.main;
        main.startLifetime = 3f;
        main.startSpeed = 50f; // UI 스케일에 맞게 조정
        main.startSize = 20f;  // UI 스케일에 맞게 조정
        main.startColor = Color.yellow;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = uiParticle.emission;
        emission.rateOverTime = 10;

        // UI 렌더링을 위한 설정
        ParticleSystemRenderer renderer = uiParticle.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "UI";
        renderer.sortingOrder = 100;

        // UI용 Material 설정
        Material uiMaterial = new Material(Shader.Find("UI/Default"));
        uiMaterial.color = Color.yellow;
        renderer.material = uiMaterial;

        uiParticle.Play();

        Debug.Log("UI Particle System created directly in Canvas");
        Debug.Log("Particle should appear at canvas center");

        Destroy(uiParticleObj, 5f);
    }

    void AdjustParticleForCanvas(GameObject effectObject)
    {
        ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem ps in particleSystems)
        {
            var main = ps.main;

            // 2D 게임에 적합하도록 크기 조정
            if (main.startSize.constant < 0.1f)
            {
                main.startSize = 0.5f;
                Debug.Log($"Adjusted particle size to {main.startSize.constant}");
            }

            // 렌더링 순서 조정
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = 10; // Canvas보다 앞에 렌더링

                Debug.Log($"Adjusted particle renderer - Layer: {renderer.sortingLayerName}, Order: {renderer.sortingOrder}");
            }

            // 강제 재생
            ps.Stop();
            ps.Play();
        }
    }

    Vector3 GetCanvasCenterWorldPosition()
    {
        if (gameCanvas == null) return Vector3.zero;

        RectTransform canvasRect = gameCanvas.GetComponent<RectTransform>();

        // Canvas 중앙의 월드 좌표
        return gameCanvas.transform.position;
    }

    System.Collections.IEnumerator RestoreCanvasModeAfterDelay(RenderMode originalMode, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (gameCanvas != null)
        {
            gameCanvas.renderMode = originalMode;
            Debug.Log($"Canvas render mode restored to: {originalMode}");
        }
    }
}