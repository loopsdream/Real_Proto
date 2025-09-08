// CanvasParticleSolutions.cs - Canvas에서 파티클을 보이게 하는 해결책들
using UnityEngine;
using UnityEngine.UI;

public class CanvasParticleSolutions : MonoBehaviour
{
    public Canvas targetCanvas;

    void Start()
    {
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        Debug.Log("=== CANVAS PARTICLE SOLUTIONS ===");
        Debug.Log("Press 1: Canvas Render Mode 변경");
        Debug.Log("Press 2: Separate Canvas 방법");
        Debug.Log("Press 3: UI Animation 대체");
        Debug.Log("Press 4: Image Sequence 애니메이션");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestCanvasRenderModeChange();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestSeparateCanvas();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestUIAnimation();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestImageSequenceAnimation();
        }
    }

    void TestCanvasRenderModeChange()
    {
        Debug.Log("=== Method 1: Canvas Render Mode 변경 ===");

        if (targetCanvas == null) return;

        // 원래 설정 저장
        RenderMode originalMode = targetCanvas.renderMode;
        Camera originalCamera = targetCanvas.worldCamera;

        // Screen Space - Camera 모드로 변경
        targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;

        // 메인 카메라 할당
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            targetCanvas.worldCamera = mainCam;
            Debug.Log($"Canvas render mode changed to Screen Space - Camera");
            Debug.Log($"Assigned camera: {mainCam.name}");

            // 이제 파티클 테스트
            CreateParticleInScreenSpaceCamera();

            // 5초 후 원래 설정으로 복원
            StartCoroutine(RestoreCanvasMode(originalMode, originalCamera, 5f));
        }
        else
        {
            Debug.LogError("No main camera found!");
        }
    }

    void CreateParticleInScreenSpaceCamera()
    {
        // Canvas에 파티클 오브젝트 생성
        GameObject particleObj = new GameObject("SCREEN_CAMERA_PARTICLE");
        particleObj.transform.SetParent(targetCanvas.transform, false);

        // RectTransform 설정
        RectTransform rectTransform = particleObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 300);
        rectTransform.anchoredPosition = Vector2.zero;

        // 파티클 시스템 추가
        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.startLifetime = 3f;
        main.startSpeed = 50f;
        main.startSize = 15f;
        main.startColor = Color.cyan;
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = particles.emission;
        emission.rateOverTime = 15;

        // 렌더러 설정 - Screen Space Camera용
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Material particleMat = new Material(Shader.Find("Sprites/Default"));
        particleMat.color = Color.cyan;
        renderer.material = particleMat;

        particles.Play();

        Debug.Log("Screen Space Camera 파티클 생성됨 - 시안색 파티클이 보여야 함");

        Destroy(particleObj, 4f);
    }

    void TestSeparateCanvas()
    {
        Debug.Log("=== Method 2: Separate Canvas 방법 ===");

        // 이펙트 전용 Canvas 생성
        GameObject effectCanvasObj = new GameObject("EFFECT_CANVAS");
        Canvas effectCanvas = effectCanvasObj.AddComponent<Canvas>();
        effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        effectCanvas.sortingOrder = 100; // 메인 Canvas보다 위에

        CanvasScaler scaler = effectCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        effectCanvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("이펙트 전용 Canvas 생성됨");

        // 이 Canvas에 파티클 생성
        CreateParticleInEffectCanvas(effectCanvas);

        // 8초 후 삭제
        Destroy(effectCanvasObj, 8f);
    }

    void CreateParticleInEffectCanvas(Canvas effectCanvas)
    {
        GameObject particleObj = new GameObject("EFFECT_CANVAS_PARTICLE");
        particleObj.transform.SetParent(effectCanvas.transform, false);

        RectTransform rectTransform = particleObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 400);
        rectTransform.anchoredPosition = Vector2.zero;

        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.startLifetime = 4f;
        main.startSpeed = 80f;
        main.startSize = 20f;
        main.startColor = Color.magenta;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = particles.emission;
        emission.rateOverTime = 20;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 100f;

        // 다양한 셰이더 시도
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();

        string[] shaderNames = {
            "UI/Default",
            "Sprites/Default",
            "Unlit/Transparent",
            "Legacy Shaders/Particles/Alpha Blended"
        };

        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = Color.magenta;
                renderer.material = mat;
                Debug.Log($"Using shader: {shaderName}");
                break;
            }
        }

        particles.Play();

        Debug.Log("Effect Canvas 파티클 생성됨 - 마젠타색 파티클이 보여야 함");
    }

    void TestUIAnimation()
    {
        Debug.Log("=== Method 3: UI Animation 대체 ===");

        // 파티클 대신 UI 애니메이션으로 이펙트 구현
        CreateUIExplosionEffect();
    }

    void CreateUIExplosionEffect()
    {
        // 여러 개의 작은 UI 이미지로 폭발 효과 구현
        for (int i = 0; i < 8; i++)
        {
            GameObject particle = new GameObject($"UI_PARTICLE_{i}");
            particle.transform.SetParent(targetCanvas.transform, false);

            // Image 컴포넌트
            Image image = particle.AddComponent<Image>();
            image.color = Color.yellow;

            // RectTransform 설정
            RectTransform rectTransform = particle.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(20, 20);
            rectTransform.anchoredPosition = Vector2.zero;

            // 방향 계산 (8방향)
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // 애니메이션 시작
            StartCoroutine(AnimateUIParticle(rectTransform, direction));
        }

        Debug.Log("UI 애니메이션 이펙트 생성됨 - 노란색 조각들이 8방향으로 날아가야 함");
    }

    System.Collections.IEnumerator AnimateUIParticle(RectTransform rectTransform, Vector2 direction)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        float speed = 200f;
        float lifetime = 2f;
        float elapsed = 0f;

        while (elapsed < lifetime && rectTransform != null)
        {
            elapsed += Time.deltaTime;

            // 위치 업데이트
            Vector2 newPos = startPos + direction * speed * elapsed;
            rectTransform.anchoredPosition = newPos;

            // 페이드 아웃
            Image image = rectTransform.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = 1f - (elapsed / lifetime);
                image.color = color;
            }

            // 크기 감소
            float scale = 1f - (elapsed / lifetime) * 0.5f;
            rectTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (rectTransform != null)
        {
            Destroy(rectTransform.gameObject);
        }
    }

    void TestImageSequenceAnimation()
    {
        Debug.Log("=== Method 4: Image Sequence 애니메이션 ===");

        // 연속 이미지로 애니메이션 효과 구현
        CreateImageSequenceEffect();
    }

    void CreateImageSequenceEffect()
    {
        GameObject animObj = new GameObject("IMAGE_SEQUENCE_EFFECT");
        animObj.transform.SetParent(targetCanvas.transform, false);

        Image image = animObj.AddComponent<Image>();
        RectTransform rectTransform = animObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150, 150);
        rectTransform.anchoredPosition = Vector2.zero;

        // 색상 변화 애니메이션
        StartCoroutine(AnimateImageSequence(image));

        Debug.Log("Image Sequence 애니메이션 생성됨 - 색상 변화하는 사각형이 보여야 함");

        Destroy(animObj, 3f);
    }

    System.Collections.IEnumerator AnimateImageSequence(Image image)
    {
        Color[] colors = { Color.red, Color.orange, Color.yellow, Color.white };
        float[] scales = { 0.5f, 1f, 1.5f, 1f, 0.5f };

        for (int frame = 0; frame < colors.Length; frame++)
        {
            if (image == null) yield break;

            image.color = colors[frame];

            if (frame < scales.Length)
            {
                image.transform.localScale = Vector3.one * scales[frame];
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    System.Collections.IEnumerator RestoreCanvasMode(RenderMode originalMode, Camera originalCamera, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (targetCanvas != null)
        {
            targetCanvas.renderMode = originalMode;
            targetCanvas.worldCamera = originalCamera;
            Debug.Log($"Canvas render mode restored to: {originalMode}");
        }
    }
}