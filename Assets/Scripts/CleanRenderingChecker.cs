// CleanRenderingChecker.cs - 유니코드 완전 제거 버전
using UnityEngine;

public class CleanRenderingChecker : MonoBehaviour
{
    public Camera targetCamera;

    [Header("Test Objects")]
    public GameObject testCube;
    public GameObject testSphere;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        Debug.Log("=== FUNDAMENTAL RENDERING CHECK ===");
        CheckUnityEnvironment();
        CheckCameraBasics();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CreateBasicCube();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CreateBasicSphere();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            CreateSprite2D();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            CreateUIElement();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            CreateSimpleParticleSystem();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            MoveToOptimalCameraPosition();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            CheckLighting();
        }
    }

    void CheckUnityEnvironment()
    {
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Render Pipeline: {UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().Name ?? "Built-in"}");
        Debug.Log($"Graphics Device Type: {SystemInfo.graphicsDeviceType}");
        Debug.Log($"Graphics Device Name: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"Screen Resolution: {Screen.width}x{Screen.height}");
        Debug.Log($"Quality Level: {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
    }

    void CheckCameraBasics()
    {
        if (targetCamera == null)
        {
            Debug.LogError("NO CAMERA FOUND!");

            // 씬에서 모든 카메라 찾기
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Debug.Log($"Found {allCameras.Length} cameras in scene:");
            for (int i = 0; i < allCameras.Length; i++)
            {
                Debug.Log($"  Camera {i}: {allCameras[i].name} - Active: {allCameras[i].gameObject.activeInHierarchy}");
            }

            if (allCameras.Length > 0)
            {
                targetCamera = allCameras[0];
                Debug.Log($"Using first available camera: {targetCamera.name}");
            }
            else
            {
                Debug.LogError("NO CAMERAS IN SCENE!");
                return;
            }
        }

        Debug.Log($"Camera: {targetCamera.name}");
        Debug.Log($"Camera Active: {targetCamera.gameObject.activeInHierarchy}");
        Debug.Log($"Camera Enabled: {targetCamera.enabled}");
        Debug.Log($"Camera Position: {targetCamera.transform.position}");
        Debug.Log($"Camera Rotation: {targetCamera.transform.eulerAngles}");
        Debug.Log($"Camera Orthographic: {targetCamera.orthographic}");

        if (targetCamera.orthographic)
        {
            Debug.Log($"Orthographic Size: {targetCamera.orthographicSize}");
        }
        else
        {
            Debug.Log($"Field of View: {targetCamera.fieldOfView}");
        }

        Debug.Log($"Near Clip Plane: {targetCamera.nearClipPlane}");
        Debug.Log($"Far Clip Plane: {targetCamera.farClipPlane}");
        Debug.Log($"Culling Mask: {targetCamera.cullingMask} (Binary: {System.Convert.ToString(targetCamera.cullingMask, 2)})");
        Debug.Log($"Clear Flags: {targetCamera.clearFlags}");
        Debug.Log($"Background Color: {targetCamera.backgroundColor}");
        Debug.Log($"Target Display: {targetCamera.targetDisplay}");
        Debug.Log($"Target Texture: {(targetCamera.targetTexture != null ? targetCamera.targetTexture.name : "None")}");

        // 이 스크립트 위치가 카메라에 보이는지 확인
        Vector3 myPos = transform.position;
        Vector3 viewportPos = targetCamera.WorldToViewportPoint(myPos);
        Debug.Log($"Script Position: {myPos}");
        Debug.Log($"Viewport Position: {viewportPos}");

        bool inView = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                      viewportPos.y >= 0 && viewportPos.y <= 1 &&
                      viewportPos.z > 0;

        Debug.Log($"Script position visible to camera: {inView}");

        if (!inView)
        {
            Debug.LogWarning("WARNING: Script position is outside camera view!");
        }
    }

    void CreateBasicCube()
    {
        Debug.Log("=== Creating Basic Cube ===");

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "TEST_CUBE";
        cube.transform.position = GetTestPosition();
        cube.transform.localScale = Vector3.one;

        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material.color = Color.red;

        Debug.Log($"RED CUBE created at {cube.transform.position}");
        Debug.Log($"Cube layer: {LayerMask.LayerToName(cube.layer)} ({cube.layer})");
        Debug.Log($"Cube active: {cube.activeInHierarchy}");
        Debug.Log($"Renderer enabled: {renderer.enabled}");

        // 5초 후 삭제
        Destroy(cube, 5f);

        testCube = cube;
    }

    void CreateBasicSphere()
    {
        Debug.Log("=== Creating Basic Sphere ===");

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "TEST_SPHERE";
        sphere.transform.position = GetTestPosition() + Vector3.right * 2f;
        sphere.transform.localScale = Vector3.one;

        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material.color = Color.green;

        Debug.Log($"GREEN SPHERE created at {sphere.transform.position}");

        Destroy(sphere, 5f);
        testSphere = sphere;
    }

    void CreateSprite2D()
    {
        Debug.Log("=== Creating 2D Sprite ===");

        GameObject spriteObj = new GameObject("TEST_SPRITE");
        spriteObj.transform.position = GetTestPosition() + Vector3.left * 2f;

        SpriteRenderer spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();

        // 기본 스프라이트 생성
        Texture2D texture = new Texture2D(64, 64);
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                texture.SetPixel(x, y, Color.blue);
            }
        }
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 10;

        Debug.Log($"BLUE SPRITE created at {spriteObj.transform.position}");
        Debug.Log($"Sprite sorting order: {spriteRenderer.sortingOrder}");

        Destroy(spriteObj, 5f);
    }

    void CreateUIElement()
    {
        Debug.Log("=== Creating UI Element ===");

        // Canvas 찾기 또는 생성
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TEST_CANVAS");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            Debug.Log("Created new canvas for UI test");
        }

        // UI 이미지 생성
        GameObject uiObj = new GameObject("TEST_UI_IMAGE");
        uiObj.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Image image = uiObj.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.magenta;

        RectTransform rectTransform = uiObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);
        rectTransform.anchoredPosition = Vector2.zero;

        Debug.Log($"MAGENTA UI ELEMENT created in center of screen");

        Destroy(uiObj, 5f);
    }

    void CreateSimpleParticleSystem()
    {
        Debug.Log("=== Creating Simple Particle System ===");

        GameObject particleObj = new GameObject("SIMPLE_PARTICLE_TEST");
        particleObj.transform.position = GetTestPosition() + Vector3.up * 2f;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        // 매우 기본적이고 확실한 설정
        var main = ps.main;
        main.startLifetime = 3f;
        main.startSpeed = 0f; // 움직이지 않게
        main.startSize = 2f; // 큰 크기
        main.startColor = Color.yellow;
        main.maxParticles = 10;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 3;

        var shape = ps.shape;
        shape.enabled = false; // Shape 비활성화

        // 렌더러 설정
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();

        // 가장 기본적인 Material 사용
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.yellow;
        renderer.material = mat;

        renderer.sortingOrder = 1000; // 매우 높은 순서

        Debug.Log($"YELLOW PARTICLE SYSTEM created at {particleObj.transform.position}");
        Debug.Log($"Particle settings - Lifetime: {main.startLifetime.constant}, Size: {main.startSize.constant}");
        Debug.Log($"Emission rate: {emission.rateOverTime.constant}");
        Debug.Log($"Material shader: {renderer.material.shader.name}");

        // 강제 재생
        ps.Stop();
        ps.Play();

        Debug.Log($"Particle system playing: {ps.isPlaying}");

        Destroy(particleObj, 8f);
    }

    Vector3 GetTestPosition()
    {
        if (targetCamera == null) return transform.position;

        // 카메라 앞 적절한 거리에 배치
        Vector3 cameraForward = targetCamera.transform.forward;
        float distance = targetCamera.orthographic ? 5f : 10f;

        return targetCamera.transform.position + cameraForward * distance;
    }

    void MoveToOptimalCameraPosition()
    {
        Debug.Log("=== Moving to Optimal Camera Position ===");

        if (targetCamera == null) return;

        // 카메라 설정을 2D 게임에 최적화
        Vector3 newCameraPos = new Vector3(0, 0, -10f);
        targetCamera.transform.position = newCameraPos;
        targetCamera.transform.rotation = Quaternion.identity;

        if (!targetCamera.orthographic)
        {
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = 5f;
        }

        targetCamera.nearClipPlane = 0.1f;
        targetCamera.farClipPlane = 1000f;
        targetCamera.backgroundColor = Color.black;
        targetCamera.clearFlags = CameraClearFlags.SolidColor;

        Debug.Log($"Camera moved to: {newCameraPos}");
        Debug.Log($"Camera set to orthographic, size: {targetCamera.orthographicSize}");

        // 이 스크립트도 적절한 위치로 이동
        transform.position = Vector3.zero;

        Debug.Log("Press 1-5 to test different rendering types");
    }

    void CheckLighting()
    {
        Debug.Log("=== Lighting Check ===");

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        Debug.Log($"Found {lights.Length} lights in scene");

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            Debug.Log($"Light {i}: {light.name}");
            Debug.Log($"  Type: {light.type}");
            Debug.Log($"  Intensity: {light.intensity}");
            Debug.Log($"  Color: {light.color}");
            Debug.Log($"  Enabled: {light.enabled}");
            Debug.Log($"  Active: {light.gameObject.activeInHierarchy}");
        }

        // Ambient lighting
        Debug.Log($"Ambient Mode: {RenderSettings.ambientMode}");
        Debug.Log($"Ambient Color: {RenderSettings.ambientLight}");
        Debug.Log($"Ambient Intensity: {RenderSettings.ambientIntensity}");
    }
}