// WorldSpaceFix.cs - 월드 스페이스 렌더링 문제 해결
using UnityEngine;

public class WorldSpaceFix : MonoBehaviour
{
    public Camera targetCamera;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        Debug.Log("=== WORLD SPACE RENDERING FIX ===");
        DiagnoseWorldSpaceIssue();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            FixCameraAndTest();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            TestAtCameraOrigin();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetEverything();
        }
    }

    void DiagnoseWorldSpaceIssue()
    {
        if (targetCamera == null)
        {
            Debug.LogError("No camera found!");
            return;
        }

        Debug.Log("=== Camera Diagnosis ===");
        Debug.Log($"Camera Position: {targetCamera.transform.position}");
        Debug.Log($"Camera Rotation: {targetCamera.transform.eulerAngles}");
        Debug.Log($"Camera Forward: {targetCamera.transform.forward}");
        Debug.Log($"Camera Up: {targetCamera.transform.up}");
        Debug.Log($"Camera Right: {targetCamera.transform.right}");

        Debug.Log($"Orthographic: {targetCamera.orthographic}");
        if (targetCamera.orthographic)
        {
            Debug.Log($"Ortho Size: {targetCamera.orthographicSize}");
        }
        else
        {
            Debug.Log($"FOV: {targetCamera.fieldOfView}");
        }

        Debug.Log($"Near Clip: {targetCamera.nearClipPlane}");
        Debug.Log($"Far Clip: {targetCamera.farClipPlane}");
        Debug.Log($"Culling Mask: {targetCamera.cullingMask}");

        // 가능한 문제들 체크
        CheckCommonIssues();
    }

    void CheckCommonIssues()
    {
        Debug.Log("=== Common Issues Check ===");

        // 1. Culling Mask 문제
        bool canSeeDefaultLayer = (targetCamera.cullingMask & (1 << 0)) != 0;
        Debug.Log($"Can see Default Layer (0): {canSeeDefaultLayer}");

        if (!canSeeDefaultLayer)
        {
            Debug.LogError("CRITICAL: Camera cannot see Default layer!");
            Debug.Log("This is likely why 3D objects are not visible");
        }

        // 2. 카메라 위치 문제
        Vector3 camPos = targetCamera.transform.position;
        if (camPos.magnitude > 1000f)
        {
            Debug.LogWarning($"Camera is very far from origin: {camPos}");
        }

        // 3. Orthographic size 문제
        if (targetCamera.orthographic && targetCamera.orthographicSize < 0.1f)
        {
            Debug.LogWarning($"Orthographic size is very small: {targetCamera.orthographicSize}");
        }

        // 4. Clipping plane 문제
        if (targetCamera.nearClipPlane > 5f)
        {
            Debug.LogWarning($"Near clip plane is too far: {targetCamera.nearClipPlane}");
        }

        if (targetCamera.farClipPlane < 10f)
        {
            Debug.LogWarning($"Far clip plane is too close: {targetCamera.farClipPlane}");
        }
    }

    void FixCameraAndTest()
    {
        Debug.Log("=== FIXING CAMERA SETTINGS ===");

        if (targetCamera == null)
        {
            Debug.LogError("No camera to fix!");
            return;
        }

        // 카메라를 완전히 리셋하고 표준 2D 설정 적용
        targetCamera.transform.position = new Vector3(0, 0, -10);
        targetCamera.transform.rotation = Quaternion.identity;
        targetCamera.orthographic = true;
        targetCamera.orthographicSize = 5f;
        targetCamera.nearClipPlane = 0.1f;
        targetCamera.farClipPlane = 1000f;
        targetCamera.cullingMask = -1; // 모든 레이어 보기
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = Color.black;

        Debug.Log("Camera fixed with standard 2D settings");
        Debug.Log($"New Position: {targetCamera.transform.position}");
        Debug.Log($"New Ortho Size: {targetCamera.orthographicSize}");
        Debug.Log($"New Culling Mask: {targetCamera.cullingMask}");

        // 즉시 테스트
        CreateTestObjectsAtOrigin();
    }

    void TestAtCameraOrigin()
    {
        Debug.Log("=== TESTING AT CAMERA ORIGIN ===");

        // 카메라가 확실히 볼 수 있는 위치에 오브젝트 생성
        Vector3 testPos = Vector3.zero; // 원점에 생성

        CreateVisibleTestObjects(testPos);
    }

    void CreateTestObjectsAtOrigin()
    {
        Debug.Log("Creating test objects at origin (0,0,0)");

        // 1. 빨간 큐브 - 원점
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "ORIGIN_CUBE";
        cube.transform.position = Vector3.zero;
        cube.transform.localScale = Vector3.one;
        cube.GetComponent<Renderer>().material.color = Color.red;

        Debug.Log($"Red cube at origin: {cube.transform.position}");

        // 2. 초록 구체 - 우측
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "RIGHT_SPHERE";
        sphere.transform.position = new Vector3(2, 0, 0);
        sphere.transform.localScale = Vector3.one;
        sphere.GetComponent<Renderer>().material.color = Color.green;

        Debug.Log($"Green sphere at: {sphere.transform.position}");

        // 3. 파란 큰 큐브 - 좌측
        GameObject bigCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bigCube.name = "BIG_BLUE_CUBE";
        bigCube.transform.position = new Vector3(-3, 0, 0);
        bigCube.transform.localScale = Vector3.one * 2f; // 2배 크기
        bigCube.GetComponent<Renderer>().material.color = Color.blue;

        Debug.Log($"Big blue cube at: {bigCube.transform.position}");

        // 4. 노란 파티클 - 위쪽
        CreateBasicParticleAtPosition(new Vector3(0, 3, 0));

        // 5초 후 삭제
        Destroy(cube, 10f);
        Destroy(sphere, 10f);
        Destroy(bigCube, 10f);

        Debug.Log("All test objects created. They should be visible now!");
    }

    void CreateVisibleTestObjects(Vector3 basePosition)
    {
        // 매우 큰 오브젝트들로 테스트
        GameObject hugeCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hugeCube.name = "HUGE_TEST_CUBE";
        hugeCube.transform.position = basePosition;
        hugeCube.transform.localScale = Vector3.one * 5f; // 5배 크기

        // 매우 밝은 색상
        Material mat = hugeCube.GetComponent<Renderer>().material;
        mat.color = Color.red;

        // Unlit shader 사용 (조명 무관)
        mat.shader = Shader.Find("Unlit/Color");

        Debug.Log($"HUGE RED CUBE created at {basePosition} with scale {hugeCube.transform.localScale}");
        Debug.Log($"Using Unlit shader: {mat.shader.name}");

        Destroy(hugeCube, 8f);
    }

    void CreateBasicParticleAtPosition(Vector3 position)
    {
        GameObject particleObj = new GameObject("BASIC_PARTICLE");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 5f;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startColor = Color.yellow;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 5;

        // 렌더러 설정
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material particleMat = new Material(Shader.Find("Unlit/Color"));
        particleMat.color = Color.yellow;
        renderer.material = particleMat;
        renderer.sortingOrder = 100;

        ps.Play();

        Debug.Log($"Basic particle system created at {position}");

        Destroy(particleObj, 8f);
    }

    void ResetEverything()
    {
        Debug.Log("=== COMPLETE RESET ===");

        // 모든 테스트 오브젝트 삭제
        GameObject[] testObjects = {
            GameObject.Find("ORIGIN_CUBE"),
            GameObject.Find("RIGHT_SPHERE"),
            GameObject.Find("BIG_BLUE_CUBE"),
            GameObject.Find("HUGE_TEST_CUBE"),
            GameObject.Find("BASIC_PARTICLE"),
            GameObject.Find("TEST_CUBE"),
            GameObject.Find("TEST_SPHERE"),
            GameObject.Find("TEST_SPRITE")
        };

        foreach (GameObject obj in testObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log($"Destroyed: {obj.name}");
            }
        }

        Debug.Log("All test objects cleared");
        Debug.Log("Press F to fix camera and create new test objects");
    }
}