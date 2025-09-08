// InstanceBasedDebugger.cs - 인스턴스 기반 이펙트 진단
using UnityEngine;

public class InstanceBasedDebugger : MonoBehaviour
{
    public GameObject effectPrefab;
    public Camera targetCamera;

    [Header("Test Settings")]
    public bool createDebugMarker = true;
    public float testDuration = 5f;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        Debug.Log($"Unity Version: {Application.unityVersion}");
        CheckBasicSetup();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestEffectInstance();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            CreateBasicParticleTest();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            CheckCameraSetup();
        }
    }

    void CheckBasicSetup()
    {
        Debug.Log("=== BASIC SETUP CHECK ===");

        if (effectPrefab == null)
        {
            Debug.LogError("❌ Effect Prefab is not assigned!");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogError("❌ No camera found!");
            return;
        }

        Debug.Log($"✅ Effect Prefab: {effectPrefab.name}");
        Debug.Log($"✅ Target Camera: {targetCamera.name}");

        // 프리팹의 기본 정보만 확인 (sharedMaterial 사용)
        ParticleSystem[] prefabSystems = effectPrefab.GetComponentsInChildren<ParticleSystem>();
        Debug.Log($"Prefab has {prefabSystems.Length} ParticleSystem(s)");

        for (int i = 0; i < prefabSystems.Length; i++)
        {
            ParticleSystem ps = prefabSystems[i];
            Debug.Log($"  ParticleSystem {i}: {ps.name}");

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                Debug.Log($"    Renderer: ✅");
                Debug.Log($"    Shared Material: {(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "NULL")}");
            }
            else
            {
                Debug.LogError($"    Renderer: ❌ MISSING!");
            }
        }
    }

    void TestEffectInstance()
    {
        Debug.Log("=== EFFECT INSTANCE TEST ===");

        if (effectPrefab == null)
        {
            Debug.LogError("No effect prefab assigned!");
            return;
        }

        Vector3 testPos = transform.position;
        Debug.Log($"Creating effect instance at: {testPos}");

        // 디버그 마커 생성
        GameObject marker = null;
        if (createDebugMarker)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "DEBUG_MARKER";
            marker.transform.position = testPos;
            marker.transform.localScale = Vector3.one * 0.3f;

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            markerRenderer.material.color = Color.red;

            Debug.Log($"🔴 Red debug marker created at {testPos}");
        }

        // 이펙트 인스턴스 생성
        GameObject effectInstance = Instantiate(effectPrefab, testPos, Quaternion.identity);
        effectInstance.name = "EFFECT_INSTANCE_TEST";

        Debug.Log($"Effect instance created: {effectInstance.name}");

        // 인스턴스 분석 (이제 material 접근 가능)
        AnalyzeEffectInstance(effectInstance);

        // 파티클 강제 재생
        ForcePlayAllParticles(effectInstance);

        // 실시간 모니터링 시작
        StartCoroutine(MonitorEffectInstance(effectInstance));

        // 정리
        Destroy(effectInstance, testDuration);
        if (marker != null)
            Destroy(marker, testDuration);
    }

    void AnalyzeEffectInstance(GameObject effectInstance)
    {
        Debug.Log($"--- Analyzing Instance: {effectInstance.name} ---");

        ParticleSystem[] systems = effectInstance.GetComponentsInChildren<ParticleSystem>();
        Debug.Log($"Found {systems.Length} ParticleSystem(s) in instance");

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            Debug.Log($"\nParticleSystem {i}: {ps.name}");
            Debug.Log($"  Position: {ps.transform.position}");
            Debug.Log($"  Scale: {ps.transform.localScale}");
            Debug.Log($"  Active: {ps.gameObject.activeInHierarchy}");
            Debug.Log($"  Layer: {LayerMask.LayerToName(ps.gameObject.layer)} ({ps.gameObject.layer})");

            // Main module
            var main = ps.main;
            Debug.Log($"  Main Module:");
            Debug.Log($"    Duration: {main.duration}");
            Debug.Log($"    Looping: {main.loop}");
            Debug.Log($"    Play On Awake: {main.playOnAwake}");
            Debug.Log($"    Max Particles: {main.maxParticles}");
            Debug.Log($"    Start Lifetime: {main.startLifetime.constant}");
            Debug.Log($"    Start Speed: {main.startSpeed.constant}");
            Debug.Log($"    Start Size: {main.startSize.constant}");
            Debug.Log($"    Start Color: {main.startColor.color}");

            // Emission
            var emission = ps.emission;
            Debug.Log($"  Emission:");
            Debug.Log($"    Enabled: {emission.enabled}");
            Debug.Log($"    Rate Over Time: {emission.rateOverTime.constant}");
            Debug.Log($"    Burst Count: {emission.burstCount}");

            // Renderer (인스턴스에서는 material 접근 가능)
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                Debug.Log($"  Renderer:");
                Debug.Log($"    Enabled: {renderer.enabled}");
                Debug.Log($"    Render Mode: {renderer.renderMode}");
                Debug.Log($"    Material: {(renderer.material != null ? renderer.material.name : "NULL")}");
                Debug.Log($"    Shader: {(renderer.material != null ? renderer.material.shader.name : "N/A")}");
                Debug.Log($"    Sorting Layer: '{renderer.sortingLayerName}'");
                Debug.Log($"    Order in Layer: {renderer.sortingOrder}");
                Debug.Log($"    Bounds: {renderer.bounds}");

                // 카메라 가시성 체크
                int layer = ps.gameObject.layer;
                bool layerVisible = (targetCamera.cullingMask & (1 << layer)) != 0;
                Debug.Log($"    Camera can see this layer: {layerVisible}");

                if (!layerVisible)
                {
                    Debug.LogError($"    ❌ CAMERA CANNOT SEE LAYER {layer}!");
                }

                // Material 문제 체크
                if (renderer.material == null)
                {
                    Debug.LogError($"    ❌ MATERIAL IS NULL!");

                    // 자동 수정 시도
                    Debug.Log($"    🔧 Attempting to fix material...");
                    Material fixMat = new Material(Shader.Find("Sprites/Default"));
                    fixMat.color = Color.white;
                    renderer.material = fixMat;
                    Debug.Log($"    ✅ Applied default material");
                }
            }
            else
            {
                Debug.LogError($"  ❌ NO RENDERER FOUND!");
            }
        }
    }

    void ForcePlayAllParticles(GameObject effectInstance)
    {
        Debug.Log("--- Force Playing All Particles ---");

        ParticleSystem[] systems = effectInstance.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem ps in systems)
        {
            Debug.Log($"Force-playing: {ps.name}");

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();

            Debug.Log($"  After Play() - Playing: {ps.isPlaying}, Stopped: {ps.isStopped}");
        }
    }

    void CreateBasicParticleTest()
    {
        Debug.Log("=== BASIC PARTICLE TEST ===");

        GameObject testObj = new GameObject("BASIC_TEST_PARTICLE");
        testObj.transform.position = transform.position + Vector3.up * 2f; // 위쪽에 생성

        ParticleSystem ps = testObj.AddComponent<ParticleSystem>();

        // 확실히 보이도록 설정
        var main = ps.main;
        main.startLifetime = 5f;
        main.startSpeed = 1f;
        main.startSize = 1f; // 큰 크기
        main.startColor = Color.yellow;
        main.maxParticles = 100;

        var emission = ps.emission;
        emission.rateOverTime = 10;

        // 렌더러 설정
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 100; // 최상위 렌더링

        Debug.Log($"🟡 Basic yellow particle created at {testObj.transform.position}");
        Debug.Log($"If you can see this, your camera setup is working!");

        Destroy(testObj, 10f);
    }

    void CheckCameraSetup()
    {
        Debug.Log("=== CAMERA SETUP ===");

        if (targetCamera == null)
        {
            Debug.LogError("No camera assigned!");
            return;
        }

        Debug.Log($"Camera: {targetCamera.name}");
        Debug.Log($"Position: {targetCamera.transform.position}");
        Debug.Log($"Rotation: {targetCamera.transform.eulerAngles}");
        Debug.Log($"Orthographic: {targetCamera.orthographic}");

        if (targetCamera.orthographic)
        {
            Debug.Log($"Orthographic Size: {targetCamera.orthographicSize}");
        }
        else
        {
            Debug.Log($"Field of View: {targetCamera.fieldOfView}");
        }

        Debug.Log($"Near Clip: {targetCamera.nearClipPlane}");
        Debug.Log($"Far Clip: {targetCamera.farClipPlane}");
        Debug.Log($"Culling Mask: {targetCamera.cullingMask}");

        // 테스트 위치가 카메라 범위 안에 있는지 확인
        Vector3 testPos = transform.position;
        Vector3 viewportPos = targetCamera.WorldToViewportPoint(testPos);

        Debug.Log($"Test Position: {testPos}");
        Debug.Log($"Viewport Position: {viewportPos}");

        bool inView = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                      viewportPos.y >= 0 && viewportPos.y <= 1 &&
                      viewportPos.z > 0;

        Debug.Log($"Test position in camera view: {inView}");

        if (!inView)
        {
            Debug.LogWarning("⚠️  Test position is outside camera view!");
        }
    }

    System.Collections.IEnumerator MonitorEffectInstance(GameObject effectInstance)
    {
        Debug.Log("--- Starting Effect Monitoring ---");

        float elapsed = 0f;
        while (effectInstance != null && elapsed < testDuration)
        {
            ParticleSystem[] systems = effectInstance.GetComponentsInChildren<ParticleSystem>();

            bool anyParticlesActive = false;
            foreach (ParticleSystem ps in systems)
            {
                int particleCount = ps.particleCount;
                if (particleCount > 0)
                {
                    anyParticlesActive = true;
                    Debug.Log($"[{elapsed:F1}s] {ps.name}: {particleCount} particles, Playing: {ps.isPlaying}");
                }
            }

            if (!anyParticlesActive)
            {
                Debug.Log($"[{elapsed:F1}s] No particles active");
            }

            elapsed += 1f;
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("--- Effect monitoring completed ---");
    }
}