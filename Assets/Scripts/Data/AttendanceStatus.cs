using System;
using System.Collections.Generic;

// 출석 상태 (서버 계산값, 표시 전용 - UserData에 넣지 않고 런타임 보관)
public class AttendanceStatus
{
    public bool canClaimDaily;
    public bool claimedToday;
    public int todaySlot;                 // 오늘 받을 7일 슬롯 (1~7)
    public int consecutiveDays;
    public int totalAttendanceDays;
    public List<int> claimedMilestones = new List<int>();
    public List<int> availableMilestones = new List<int>();

    public static AttendanceStatus FromDict(Dictionary<object, object> d)
    {
        var s = new AttendanceStatus();
        if (d == null) return s;
        if (d.ContainsKey("canClaimDaily")) s.canClaimDaily = Convert.ToBoolean(d["canClaimDaily"]);
        if (d.ContainsKey("claimedToday")) s.claimedToday = Convert.ToBoolean(d["claimedToday"]);
        if (d.ContainsKey("todaySlot")) s.todaySlot = Convert.ToInt32(d["todaySlot"]);
        if (d.ContainsKey("consecutiveDays")) s.consecutiveDays = Convert.ToInt32(d["consecutiveDays"]);
        if (d.ContainsKey("totalAttendanceDays")) s.totalAttendanceDays = Convert.ToInt32(d["totalAttendanceDays"]);
        s.claimedMilestones = ToIntList(d.ContainsKey("claimedMilestones") ? d["claimedMilestones"] : null);
        s.availableMilestones = ToIntList(d.ContainsKey("availableMilestones") ? d["availableMilestones"] : null);
        return s;
    }

    static List<int> ToIntList(object raw)
    {
        var list = new List<int>();
        if (raw is List<object> l)
        {
            foreach (var o in l) list.Add(Convert.ToInt32(o));
        }
        return list;
    }
}

// getAccountData 응답
public class AccountDataResult
{
    public string userDataJson;
    public long serverTime;
    public int energy;
    public int maxEnergy;
    public int gameCoins;
    public int diamonds;
    public int hammerCount;
    public int tornadoCount;
    public int brushCount;
    public AttendanceStatus attendance = new AttendanceStatus();
}

// claimAttendance가 지급한 보상 1건
public class AttendanceReward
{
    public string type;   // "Coins", "Diamonds", "Energy", "Hammer", "Tornado", "Brush"
    public int amount;
}

// claimAttendance 응답
public class AttendanceClaimResult
{
    public int newCoins;
    public int newDiamonds;
    public List<AttendanceReward> grantedRewards = new List<AttendanceReward>();
    public AttendanceStatus attendance = new AttendanceStatus();

    public static List<AttendanceReward> ParseRewards(object raw)
    {
        var list = new List<AttendanceReward>();
        if (raw is List<object> l)
        {
            foreach (var o in l)
            {
                if (o is Dictionary<object, object> rd)
                {
                    var r = new AttendanceReward();
                    if (rd.ContainsKey("type")) r.type = rd["type"] as string;
                    if (rd.ContainsKey("amount")) r.amount = Convert.ToInt32(rd["amount"]);
                    list.Add(r);
                }
            }
        }
        return list;
    }
}