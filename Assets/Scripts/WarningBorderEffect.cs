// WarningBorderEffect.cs - �׵θ� ȿ���� ���� ������Ʈ
using UnityEngine;

public class WarningBorderEffect : MonoBehaviour
{
    [Header("Border Settings")]
    public SpriteRenderer originalBlockRenderer;
    public float borderThickness = 0.1f; // �׵θ� �β�

    private SpriteRenderer borderRenderer;
    private Material borderMaterial;

    void Start()
    {
        borderRenderer = GetComponent<SpriteRenderer>();
        SetupBorderMaterial();
    }

    void SetupBorderMaterial()
    {
        if (borderRenderer == null) return;

        // �׵θ� ���� ��Ƽ���� ���� (�⺻ Sprite-Default ��Ƽ���� ����)
        borderMaterial = new Material(Shader.Find("Sprites/Default"));
        borderRenderer.material = borderMaterial;

        // ��Ϻ��� �ణ ũ�� ������ �����Ͽ� �׵θ� ȿ��
        transform.localScale = Vector3.one * (1f + borderThickness);
    }

    void OnDestroy()
    {
        if (borderMaterial != null)
        {
            Destroy(borderMaterial);
        }
    }
}