// CanvasParticleSolutions.cs - Canvas���� ��ƼŬ�� ���̰� �ϴ� �ذ�å��
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
        Debug.Log("Press 1: Canvas Render Mode ����");
        Debug.Log("Press 2: Separate Canvas ���");
        Debug.Log("Press 3: UI Animation ��ü");
        Debug.Log("Press 4: Image Sequence �ִϸ��̼�");
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
        Debug.Log("=== Method 1: Canvas Render Mode ���� ===");

        if (targetCanvas == null) return;

        // ���� ���� ����
        RenderMode originalMode = targetCanvas.renderMode;
        Camera originalCamera = targetCanvas.worldCamera;

        // Screen Space - Camera ���� ����
        targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;

        // ���� ī�޶� �Ҵ�
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            targetCanvas.worldCamera = mainCam;
            Debug.Log($"Canvas render mode changed to Screen Space - Camera");
            Debug.Log($"Assigned camera: {mainCam.name}");

            // ���� ��ƼŬ �׽�Ʈ
            CreateParticleInScreenSpaceCamera();

            // 5�� �� ���� �������� ����
            StartCoroutine(RestoreCanvasMode(originalMode, originalCamera, 5f));
        }
        else
        {
            Debug.LogError("No main camera found!");
        }
    }

    void CreateParticleInScreenSpaceCamera()
    {
        // Canvas�� ��ƼŬ ������Ʈ ����
        GameObject particleObj = new GameObject("SCREEN_CAMERA_PARTICLE");
        particleObj.transform.SetParent(targetCanvas.transform, false);

        // RectTransform ����
        RectTransform rectTransform = particleObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 300);
        rectTransform.anchoredPosition = Vector2.zero;

        // ��ƼŬ �ý��� �߰�
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

        // ������ ���� - Screen Space Camera��
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Material particleMat = new Material(Shader.Find("Sprites/Default"));
        particleMat.color = Color.cyan;
        renderer.material = particleMat;

        particles.Play();

        Debug.Log("Screen Space Camera ��ƼŬ ������ - �þȻ� ��ƼŬ�� ������ ��");

        Destroy(particleObj, 4f);
    }

    void TestSeparateCanvas()
    {
        Debug.Log("=== Method 2: Separate Canvas ��� ===");

        // ����Ʈ ���� Canvas ����
        GameObject effectCanvasObj = new GameObject("EFFECT_CANVAS");
        Canvas effectCanvas = effectCanvasObj.AddComponent<Canvas>();
        effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        effectCanvas.sortingOrder = 100; // ���� Canvas���� ����

        CanvasScaler scaler = effectCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        effectCanvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("����Ʈ ���� Canvas ������");

        // �� Canvas�� ��ƼŬ ����
        CreateParticleInEffectCanvas(effectCanvas);

        // 8�� �� ����
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

        // �پ��� ���̴� �õ�
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

        Debug.Log("Effect Canvas ��ƼŬ ������ - ����Ÿ�� ��ƼŬ�� ������ ��");
    }

    void TestUIAnimation()
    {
        Debug.Log("=== Method 3: UI Animation ��ü ===");

        // ��ƼŬ ��� UI �ִϸ��̼����� ����Ʈ ����
        CreateUIExplosionEffect();
    }

    void CreateUIExplosionEffect()
    {
        // ���� ���� ���� UI �̹����� ���� ȿ�� ����
        for (int i = 0; i < 8; i++)
        {
            GameObject particle = new GameObject($"UI_PARTICLE_{i}");
            particle.transform.SetParent(targetCanvas.transform, false);

            // Image ������Ʈ
            Image image = particle.AddComponent<Image>();
            image.color = Color.yellow;

            // RectTransform ����
            RectTransform rectTransform = particle.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(20, 20);
            rectTransform.anchoredPosition = Vector2.zero;

            // ���� ��� (8����)
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // �ִϸ��̼� ����
            StartCoroutine(AnimateUIParticle(rectTransform, direction));
        }

        Debug.Log("UI �ִϸ��̼� ����Ʈ ������ - ����� �������� 8�������� ���ư��� ��");
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

            // ��ġ ������Ʈ
            Vector2 newPos = startPos + direction * speed * elapsed;
            rectTransform.anchoredPosition = newPos;

            // ���̵� �ƿ�
            Image image = rectTransform.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = 1f - (elapsed / lifetime);
                image.color = color;
            }

            // ũ�� ����
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
        Debug.Log("=== Method 4: Image Sequence �ִϸ��̼� ===");

        // ���� �̹����� �ִϸ��̼� ȿ�� ����
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

        // ���� ��ȭ �ִϸ��̼�
        StartCoroutine(AnimateImageSequence(image));

        Debug.Log("Image Sequence �ִϸ��̼� ������ - ���� ��ȭ�ϴ� �簢���� ������ ��");

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