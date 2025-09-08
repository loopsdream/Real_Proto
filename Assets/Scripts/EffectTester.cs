// EffectTester.cs - ����Ʈ �׽�Ʈ�� �ӽ� ��ũ��Ʈ
using UnityEngine;

public class EffectTester : MonoBehaviour
{
    public GameObject effectPrefab;

    void Update()
    {
        // ���콺 Ŭ������ ����Ʈ �׽�Ʈ
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));

            TestEffect(worldPos);
        }

        // Ű���� TŰ�� �߾ӿ� ����Ʈ �׽�Ʈ
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestEffect(Vector3.zero);
        }
    }

    void TestEffect(Vector3 position)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);

            // ��ƼŬ �ý��� �ڵ� ����
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                float duration = particles.main.startLifetime.constantMax + particles.main.duration;
                Destroy(effect, duration + 2f);

                Debug.Log($"Effect played at {position}, will be destroyed in {duration + 2f} seconds");
            }
            else
            {
                Destroy(effect, 3f);
                Debug.Log($"Non-particle effect played at {position}");
            }
        }
        else
        {
            Debug.LogWarning("Effect prefab is not assigned!");
        }
    }
}