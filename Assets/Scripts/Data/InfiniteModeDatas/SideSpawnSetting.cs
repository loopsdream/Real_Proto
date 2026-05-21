using UnityEngine;

// 면별 블록 스폰 설정
[System.Serializable]
public class SideSpawnSetting
{
    [Tooltip("이 면에서 블록을 생성할지 여부")]
    public bool enabled = true;

    [Range(0f, 1f)]
    [Tooltip("최소 스폰 확률")]
    public float minSpawnChance = 0.1f;

    [Range(0f, 1f)]
    [Tooltip("최대 스폰 확률")]
    public float maxSpawnChance = 0.3f;

    [Tooltip("양쪽 끝에서 추가로 제외할 칸 수 (기본 1칸(꼭지점)은 항상 제외)")]
    public int extraExcludeCount = 0;

    public SideSpawnSetting() { }

    public SideSpawnSetting(bool enabled, float min, float max, int extraExclude = 0)
    {
        this.enabled = enabled;
        this.minSpawnChance = min;
        this.maxSpawnChance = max;
        this.extraExcludeCount = extraExclude;
    }
}
