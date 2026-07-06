using System;
using System.Collections.Generic;
using UnityEngine;

// attendance.json 매핑 (표시 전용, 서버와 동일 json 공유)
[Serializable]
public class AttendanceRewardEntry
{
    public string type;     // Coins, Diamonds, Energy, Hammer, Tornado, Brush
    public int amount;
}

[Serializable]
public class AttendanceDailyRow
{
    public int slot;        // 1~7
    public List<AttendanceRewardEntry> rewards = new List<AttendanceRewardEntry>();
}

[Serializable]
public class AttendanceMilestoneRow
{
    public int index;
    public int days;
    public List<AttendanceRewardEntry> rewards = new List<AttendanceRewardEntry>();
}

[Serializable]
public class AttendanceTable
{
    public List<AttendanceDailyRow> dailyRewards = new List<AttendanceDailyRow>();
    public List<AttendanceMilestoneRow> milestones = new List<AttendanceMilestoneRow>();
}

// 런타임 로더 (Resources/Data/attendance.json) - 1회 로드 후 캐시
public static class AttendanceTableLoader
{
    private const string RESOURCE_PATH = "Data/attendance";
    private static AttendanceTable _cached;

    public static AttendanceTable Get()
    {
        if (_cached != null) return _cached;

        TextAsset ta = Resources.Load<TextAsset>(RESOURCE_PATH);
        if (ta == null)
        {
            Debug.LogError("[AttendanceTable] Resources/Data/attendance.json not found");
            _cached = new AttendanceTable();
            return _cached;
        }

        _cached = JsonUtility.FromJson<AttendanceTable>(ta.text);
        if (_cached == null)
        {
            Debug.LogError("[AttendanceTable] Parse failed");
            _cached = new AttendanceTable();
        }
        return _cached;
    }

    // 7일 슬롯(1~7) 보상
    public static List<AttendanceRewardEntry> GetDailyRewards(int slot)
    {
        var row = Get().dailyRewards.Find(r => r.slot == slot);
        return row != null ? row.rewards : new List<AttendanceRewardEntry>();
    }

    // 누적 마일스톤(index)
    public static AttendanceMilestoneRow GetMilestone(int index)
    {
        return Get().milestones.Find(m => m.index == index);
    }
}