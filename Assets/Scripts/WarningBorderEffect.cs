// WarningBorderEffect.cs - 테두리 효과를 위한 컴포넌트
using UnityEngine;

public class WarningBorderEffect : MonoBehaviour
{
    [Header("Border Settings")]
    public SpriteRenderer originalBlockRenderer;
    public float borderThickness = 0.1f; // 테두리 두께

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

        // 테두리 전용 머티리얼 생성 (기본 Sprite-Default 머티리얼 복사)
        borderMaterial = new Material(Shader.Find("Sprites/Default"));
        borderRenderer.material = borderMaterial;

        // 블록보다 약간 크게 스케일 조정하여 테두리 효과
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