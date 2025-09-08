// EffectDebugger.cs - ����Ʈ ���� ���ܿ� ��ũ��Ʈ
using UnityEngine;

public class EffectDebugger : MonoBehaviour
{
    public GameObject effectPrefab;

    void Start()
    {
        if (effectPrefab != null)
        {
            AnalyzeEffectPrefab();
        }
        else
        {
            Debug.LogError("Effect Prefab is not assigned!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestEffectWithFullDebug();
        }
    }

    void AnalyzeEffectPrefab()
    {
        Debug.Log("=== Effect Prefab Analysis ===");
        Debug.Log($"Prefab Name: {effectPrefab.name}");
        Debug.Log($"Prefab Active: {effectPrefab.activeInHierarchy}");

        // ��� ������Ʈ Ȯ��
        Component[] components = effectPrefab.GetComponents<Component>();
        Debug.Log($"Components found: {components.Length}");

        foreach (Component comp in components)
        {
            Debug.Log($"- {comp.GetType().Name}");

            // ��ƼŬ �ý��� �� ����
            if (comp is ParticleSystem ps)
            {
                Debug.Log($"  Particle System:");
                Debug.Log($"    Play On Awake: {ps.main.playOnAwake}");
                Debug.Log($"    Start Lifetime: {ps.main.startLifetime.constant}");
                Debug.Log($"    Start Speed: {ps.main.startSpeed.constant}");
                Debug.Log($"    Max Particles: {ps.main.maxParticles}");
                Debug.Log($"    Emission Rate: {ps.emission.rateOverTime.constant}");
                Debug.Log($"    Duration: {ps.main.duration}");
                Debug.Log($"    Looping: {ps.main.loop}");
            }

            // ������ ����
            if (comp is Renderer renderer)
            {
                Debug.Log($"  Renderer:");
                Debug.Log($"    Enabled: {renderer.enabled}");
                Debug.Log($"    Sorting Layer: {renderer.sortingLayerName}");
                Debug.Log($"    Order in Layer: {renderer.sortingOrder}");
                Debug.Log($"    Shared Materials: {renderer.sharedMaterials.Length}");

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] != null)
                    {
                        Debug.Log($"    Material {i}: {renderer.sharedMaterials[i].name}");
                        Debug.Log($"    Shader: {renderer.sharedMaterials[i].shader.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"    Material {i}: NULL!");
                    }
                }
            }
        }

        // �ڽ� ������Ʈ Ȯ��
        Debug.Log($"Child Objects: {effectPrefab.transform.childCount}");
        for (int i = 0; i < effectPrefab.transform.childCount; i++)
        {
            Transform child = effectPrefab.transform.GetChild(i);
            Debug.Log($"- Child {i}: {child.name} (Active: {child.gameObject.activeInHierarchy})");

            ParticleSystem childPS = child.GetComponent<ParticleSystem>();
            if (childPS != null)
            {
                Debug.Log($"    Child has ParticleSystem - Play On Awake: {childPS.main.playOnAwake}");
            }
        }
    }

    void TestEffectWithFullDebug()
    {
        if (effectPrefab == null)
        {
            Debug.LogError("Effect Prefab is null!");
            return;
        }

        Vector3 spawnPos = transform.position;
        Debug.Log($"Creating effect at position: {spawnPos}");

        GameObject effect = Instantiate(effectPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"Effect instantiated: {effect.name}");
        Debug.Log($"Effect active: {effect.activeInHierarchy}");

        // ��ƼŬ �ý��� ���� ���
        ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
        Debug.Log($"Found {particleSystems.Length} particle systems");

        foreach (ParticleSystem ps in particleSystems)
        {
            Debug.Log($"Particle System: {ps.name}");
            Debug.Log($"  Is Playing: {ps.isPlaying}");
            Debug.Log($"  Is Paused: {ps.isPaused}");
            Debug.Log($"  Is Stopped: {ps.isStopped}");
            Debug.Log($"  Particle Count: {ps.particleCount}");

            // ���� ���
            ps.Stop();
            ps.Play();

            Debug.Log($"  After Play() - Is Playing: {ps.isPlaying}");
            Debug.Log($"  After Play() - Particle Count: {ps.particleCount}");
        }

        // 3�� �� �����ϸ鼭 ���� ���� Ȯ��
        StartCoroutine(CheckEffectAfterDelay(effect, 1f));

        Destroy(effect, 5f);
    }

    System.Collections.IEnumerator CheckEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null)
        {
            Debug.Log("=== Effect Status After 1 Second ===");
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem ps in particleSystems)
            {
                Debug.Log($"{ps.name} - Playing: {ps.isPlaying}, Particles: {ps.particleCount}");
            }
        }
    }
}