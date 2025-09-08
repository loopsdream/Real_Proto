// CROxCROBlockEffectSystem.cs - CROxCRO ���ӿ� ��� �ı� ����Ʈ
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CROxCROBlockEffectSystem : MonoBehaviour
{
    [Header("Canvas References")]
    public Canvas gameCanvas;           // ������ ���� Canvas
    public Canvas effectCanvas;         // ����Ʈ ���� Canvas (�ɼ�)

    [Header("Effect Settings")]
    public int particleCount = 8;       // ��ƼŬ ���� (8����)
    public float particleSpeed = 300f;  // ��ƼŬ �ӵ�
    public float particleLifetime = 1.5f; // ��ƼŬ ���� �ð�
    public float particleSize = 15f;    // ��ƼŬ ũ��

    [Header("Block Colors")]
    public Color[] blockColors = {      // ��� ���� ����Ʈ ����
        Color.white,    // �� ���
        Color.red,      // ���� ���
        Color.blue,     // �Ķ� ���  
        Color.yellow,   // ��� ���
        Color.green,    // �ʷ� ���
        Color.magenta   // ���� ���
    };

    [Header("GridManager Integration")]
    public BaseGridManager gridManager;     // GridManager ����
    public float gridCellSize = 1.0f;   // �׸��� �� ũ��

    void Start()
    {
        SetupEffectSystem();
    }

    void Update()
    {
        // �׽�Ʈ�� Ű�Է�
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
        // ���� Canvas ã��
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
        }

        // GridManager ã��
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
    /// ���� �޼���: ������ ��ġ���� ��� �ı� ����Ʈ ����
    /// </summary>
    /// <param name="gridX">�׸��� X ��ǥ</param>
    /// <param name="gridY">�׸��� Y ��ǥ</param>
    /// <param name="blockType">��� Ÿ�� (0=����, 1=����, 2=�Ķ�, 3=���, 4=�ʷ�, 5=����)</param>
    public void CreateBlockDestroyEffect(int gridX, int gridY, int blockType = 1)
    {
        if (gameCanvas == null)
        {
            Debug.LogWarning("No Canvas available for effect!");
            return;
        }

        // �׸��� ��ǥ�� UI ��ǥ�� ��ȯ
        Vector2 uiPosition = GridToUIPosition(gridX, gridY);

        // ��� ���� ����
        Color effectColor = GetBlockEffectColor(blockType);

        // UI ����Ʈ ����
        CreateUIExplosionEffect(uiPosition, effectColor, blockType);

        Debug.Log($"Block destroy effect created at grid({gridX},{gridY}) -> UI({uiPosition.x},{uiPosition.y})");
    }

    /// <summary>
    /// UI ��ǥ�� ���� ����Ʈ ����
    /// </summary>
    public void CreateBlockDestroyEffectAtUIPosition(Vector2 uiPosition, int blockType = 1)
    {
        if (gameCanvas == null) return;

        Color effectColor = GetBlockEffectColor(blockType);
        CreateUIExplosionEffect(uiPosition, effectColor, blockType);
    }

    /// <summary>
    /// �׸��� ��ǥ�� UI ��ǥ�� ��ȯ
    /// </summary>
    Vector2 GridToUIPosition(int gridX, int gridY)
    {
        if (gridManager != null)
        {
            // GridManager�� ������ ���� �׸��� ���� ���
            // �� �κ��� GridManager�� ���� ������ �°� ���� �ʿ�

            // ����: �׸��� �߾��� �������� ���
            float worldX = (gridX - gridManager.width / 2f) * gridCellSize;
            float worldY = (gridY - gridManager.height / 2f) * gridCellSize;

            // ���� ��ǥ�� UI ��ǥ�� ��ȯ (Canvas�� Screen Space�� ���)
            Vector3 worldPos = new Vector3(worldX, worldY, 0);
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            RectTransform canvasRect = gameCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, gameCanvas.worldCamera, out Vector2 localPoint);

            return localPoint;
        }
        else
        {
            // GridManager�� ������ ������ ���
            float uiX = (gridX - 3) * 80f; // �ӽð�
            float uiY = (3 - gridY) * 80f; // �ӽð�
            return new Vector2(uiX, uiY);
        }
    }

    /// <summary>
    /// ��� Ÿ�Կ� ���� ����Ʈ ���� ��ȯ
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
    /// UI ���� ����Ʈ ���� (�ٽ� �޼���)
    /// </summary>
    void CreateUIExplosionEffect(Vector2 uiPosition, Color effectColor, int blockType)
    {
        Canvas targetCanvas = effectCanvas != null ? effectCanvas : gameCanvas;

        // ���� ���� ���� ��ƼŬ ����
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject($"BlockEffect_Particle_{i}");
            particle.transform.SetParent(targetCanvas.transform, false);

            // Image ������Ʈ �߰�
            Image image = particle.AddComponent<Image>();
            image.color = effectColor;

            // RectTransform ����
            RectTransform rectTransform = particle.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(particleSize, particleSize);
            rectTransform.anchoredPosition = uiPosition;

            // ���� ��� (�������� �л�)
            float angle = i * (360f / particleCount) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // �ణ�� ������ �߰�
            direction += Random.insideUnitCircle * 0.3f;
            direction.Normalize();

            // �ִϸ��̼� ����
            StartCoroutine(AnimateBlockParticle(rectTransform, direction, effectColor));
        }

        // �߰� ȿ��: �߾ӿ��� Ȯ��Ǵ� ���� ȿ��
        CreateCentralExpandEffect(uiPosition, effectColor);
    }

    /// <summary>
    /// ���� ��ƼŬ �ִϸ��̼�
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

            // ��ġ ������Ʈ (����)
            float currentSpeed = randomSpeed * (1f - progress * 0.5f);
            Vector2 newPos = startPos + direction * currentSpeed * elapsed;
            rectTransform.anchoredPosition = newPos;

            // ���̵� �ƿ�
            if (image != null)
            {
                Color color = startColor;
                color.a = 1f - progress;
                image.color = color;
            }

            // ũ�� ��ȭ
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
    /// �߾� Ȯ�� ȿ��
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
    /// �߾� Ȯ�� �ִϸ��̼�
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

            // ũ�� Ȯ��
            float currentSize = maxSize * progress;
            rectTransform.sizeDelta = new Vector2(currentSize, currentSize);

            // ���̵� �ƿ�
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

    #region �׽�Ʈ �޼����

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