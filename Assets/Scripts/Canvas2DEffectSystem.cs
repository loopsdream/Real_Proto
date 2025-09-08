// Canvas2DEffectSystem.cs - Canvas ��� 2D ���ӿ� ����Ʈ �ý���
using UnityEngine;
using UnityEngine.UI;

public class Canvas2DEffectSystem : MonoBehaviour
{
    [Header("Canvas References")]
    public Canvas gameCanvas;           // ������ ���� Canvas
    public Canvas effectCanvas;         // ����Ʈ ���� Canvas (�ڵ� ���� ����)

    [Header("Effect Prefab")]
    public GameObject effectPrefab;     // 3D ��ƼŬ ����Ʈ ������

    [Header("Camera References")]
    public Camera mainCamera;           // ���� ī�޶�
    public Camera effectCamera;         // ����Ʈ ���� ī�޶� (�ڵ� ����)

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

        // ���� Canvas ã��
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

        // ���� ī�޶� ã��
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

        // Canvas�� �߾� ��ġ ���
        Vector3 canvasCenter = GetCanvasCenterWorldPosition();

        Debug.Log($"Canvas center world position: {canvasCenter}");

        // ����Ʈ ����
        GameObject effect = Instantiate(effectPrefab, canvasCenter, Quaternion.identity);
        effect.name = "CANVAS_TEST_EFFECT";

        // ��ƼŬ �ý��� ���� ����
        AdjustParticleForCanvas(effect);

        Destroy(effect, 5f);

        Debug.Log("Canvas effect test created");
    }

    void TestEffectAtCanvasCenter()
    {
        Debug.Log("=== Testing Effect at Canvas Center ===");

        if (gameCanvas == null || effectPrefab == null) return;

        // Canvas UI ��ǥ�� �߾� ���� ���
        RectTransform canvasRect = gameCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        Vector3 centerUIPos = new Vector3(canvasSize.x / 2, canvasSize.y / 2, 0);

        // UI ��ǥ�� ���� ��ǥ�� ��ȯ
        Vector3 worldPos = gameCanvas.transform.TransformPoint(centerUIPos);

        Debug.Log($"Canvas UI center: {centerUIPos}");
        Debug.Log($"World position: {worldPos}");

        // ����Ʈ ����
        GameObject effect = Instantiate(effectPrefab, worldPos, Quaternion.identity);
        effect.name = "CANVAS_CENTER_EFFECT";

        AdjustParticleForCanvas(effect);

        Destroy(effect, 5f);
    }

    void TestMethod1_WorldSpaceCanvas()
    {
        Debug.Log("=== Method 1: World Space Canvas ===");

        if (gameCanvas == null || effectPrefab == null) return;

        // ���� Canvas ���� Ȯ��
        Debug.Log($"Current Canvas Render Mode: {gameCanvas.renderMode}");

        // �ӽ÷� World Space�� �����ؼ� �׽�Ʈ
        RenderMode originalMode = gameCanvas.renderMode;
        gameCanvas.renderMode = RenderMode.WorldSpace;
        gameCanvas.worldCamera = mainCamera;

        // World Space���� ����Ʈ �׽�Ʈ
        Vector3 effectPos = gameCanvas.transform.position + Vector3.forward * 2f;
        GameObject effect = Instantiate(effectPrefab, effectPos, Quaternion.identity);
        effect.name = "WORLDSPACE_CANVAS_EFFECT";

        AdjustParticleForCanvas(effect);

        Debug.Log($"Effect created at {effectPos} with WorldSpace Canvas");
        Debug.Log("Effect should be visible in front of canvas");

        // 5�� �� ���� ���� ����
        StartCoroutine(RestoreCanvasModeAfterDelay(originalMode, 5f));

        Destroy(effect, 5f);
    }

    void TestMethod2_RenderTexture()
    {
        Debug.Log("=== Method 2: Render Texture ===");

        if (effectPrefab == null) return;

        // ����Ʈ ���� ī�޶� ����
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

        // Render Texture ����
        RenderTexture renderTexture = new RenderTexture(512, 512, 16);
        effectCamera.targetTexture = renderTexture;

        // Canvas�� RenderTexture�� ǥ���� UI Image ����
        GameObject imageObj = new GameObject("Effect_Image");
        imageObj.transform.SetParent(gameCanvas.transform, false);

        RawImage rawImage = imageObj.AddComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.rectTransform.sizeDelta = new Vector2(200, 200);
        rawImage.rectTransform.anchoredPosition = Vector2.zero;

        // ����Ʈ�� ī�޶� �տ� ����
        Vector3 effectPos = effectCamera.transform.position + Vector3.forward * 5f;
        GameObject effect = Instantiate(effectPrefab, effectPos, Quaternion.identity);
        effect.name = "RENDER_TEXTURE_EFFECT";

        AdjustParticleForCanvas(effect);

        Debug.Log("Render Texture method: Effect should appear in center UI image");

        // ����
        Destroy(effect, 5f);
        Destroy(imageObj, 5f);
        Destroy(renderTexture, 5f);
    }

    void TestMethod3_UIParticle()
    {
        Debug.Log("=== Method 3: UI Particle System ===");

        // Canvas�� ���� ��ƼŬ �ý��� ����
        GameObject uiParticleObj = new GameObject("UI_Particle");
        uiParticleObj.transform.SetParent(gameCanvas.transform, false);

        // RectTransform ����
        RectTransform rectTransform = uiParticleObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 200);
        rectTransform.anchoredPosition = Vector2.zero;

        // ��ƼŬ �ý��� �߰�
        ParticleSystem uiParticle = uiParticleObj.AddComponent<ParticleSystem>();

        // UI�� ��ƼŬ ����
        var main = uiParticle.main;
        main.startLifetime = 3f;
        main.startSpeed = 50f; // UI �����Ͽ� �°� ����
        main.startSize = 20f;  // UI �����Ͽ� �°� ����
        main.startColor = Color.yellow;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = uiParticle.emission;
        emission.rateOverTime = 10;

        // UI �������� ���� ����
        ParticleSystemRenderer renderer = uiParticle.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "UI";
        renderer.sortingOrder = 100;

        // UI�� Material ����
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

            // 2D ���ӿ� �����ϵ��� ũ�� ����
            if (main.startSize.constant < 0.1f)
            {
                main.startSize = 0.5f;
                Debug.Log($"Adjusted particle size to {main.startSize.constant}");
            }

            // ������ ���� ����
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = 10; // Canvas���� �տ� ������

                Debug.Log($"Adjusted particle renderer - Layer: {renderer.sortingLayerName}, Order: {renderer.sortingOrder}");
            }

            // ���� ���
            ps.Stop();
            ps.Play();
        }
    }

    Vector3 GetCanvasCenterWorldPosition()
    {
        if (gameCanvas == null) return Vector3.zero;

        RectTransform canvasRect = gameCanvas.GetComponent<RectTransform>();

        // Canvas �߾��� ���� ��ǥ
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