// EffectTester.cs - 이펙트 테스트용 임시 스크립트
using UnityEngine;

public class EffectTester : MonoBehaviour
{
    public GameObject effectPrefab;

    void Update()
    {
        // 마우스 클릭으로 이펙트 테스트
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));

            TestEffect(worldPos);
        }

        // 키보드 T키로 중앙에 이펙트 테스트
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

            // 파티클 시스템 자동 삭제
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