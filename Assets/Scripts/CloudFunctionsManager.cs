using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Functions;

/// <summary>
/// Cloud Functions 호출 매니저
/// 골드/젬의 소비 및 획득을 서버에서 검증합니다.
/// </summary>
public class CloudFunctionsManager : MonoBehaviour
{
    public static CloudFunctionsManager Instance;

    private FirebaseFunctions functions;
    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            InitializeFunctions();
        }
        else
        {
            if (Application.isPlaying)
                Destroy(gameObject);
        }
    }

    void InitializeFunctions()
    {
        try
        {
            // Cloud Functions 서울 리전 설정
            functions = FirebaseFunctions.GetInstance("asia-northeast3");
            isInitialized = true;
            Debug.Log("[CloudFunctions] 초기화 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CloudFunctions] 초기화 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 재화 소비 요청 (서버 검증)
    /// </summary>
    /// <param name="currencyType">"gameCoins" 또는 "diamonds"</param>
    /// <param name="amount">소비할 수량</param>
    /// <param name="reason">소비 이유 (예: "item_purchase", "energy_refill")</param>
    /// <param name="onSuccess">성공 콜백 - 서버에서 반환된 새 잔액</param>
    /// <param name="onFail">실패 콜백 - 에러 메시지</param>
    public void SpendCurrency(string currencyType, int amount, string reason,
        Action<int> onSuccess, Action<string> onFail)
    {
        if (!isInitialized)
        {
            onFail?.Invoke("CloudFunctions not initialized");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "currencyType", currencyType },
            { "amount", amount },
            { "reason", reason }
        };

        Debug.Log($"[CloudFunctions] SpendCurrency 요청: {currencyType} x{amount} ({reason})");

        functions.GetHttpsCallable("spendCurrency")
            .CallAsync(data)
            .ContinueWith(task =>
            {
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        string error = task.Exception?.InnerException?.Message ?? "Unknown error";
                        Debug.LogError($"[CloudFunctions] SpendCurrency 실패: {error}");
                        onFail?.Invoke(error);
                        return;
                    }

                    // [추가] 응답 디버깅용 로그
                    var rawData = task.Result.Data;
                    Debug.Log($"[CloudFunctions] Response type: {rawData?.GetType()?.FullName ?? "null"}");
                    Debug.Log($"[CloudFunctions] Response value: {rawData}");

                    var result = task.Result.Data as Dictionary<object, object>;
                    if (result != null && result.ContainsKey("newBalance"))
                    {
                        int newBalance = Convert.ToInt32(result["newBalance"]);
                        Debug.Log($"[CloudFunctions] SpendCurrency 성공 - {currencyType} 새 잔액: {newBalance}");
                        onSuccess?.Invoke(newBalance);
                    }
                    else
                    {
                        onFail?.Invoke("Invalid response from server");
                    }
                });
            });
    }

    /// <summary>
    /// 재화 획득 요청 (서버 검증)
    /// </summary>
    /// <param name="currencyType">"gameCoins" 또는 "diamonds"</param>
    /// <param name="amount">획득할 수량</param>
    /// <param name="reason">획득 이유 (예: "stage_clear", "ad_reward")</param>
    /// <param name="onSuccess">성공 콜백 - 서버에서 반환된 새 잔액</param>
    /// <param name="onFail">실패 콜백 - 에러 메시지</param>
    public void AddCurrency(string currencyType, int amount, string reason,
        Action<int> onSuccess, Action<string> onFail)
    {
        if (!isInitialized)
        {
            onFail?.Invoke("CloudFunctions not initialized");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "currencyType", currencyType },
            { "amount", amount },
            { "reason", reason }
        };

        Debug.Log($"[CloudFunctions] AddCurrency 요청: {currencyType} x{amount} ({reason})");

        functions.GetHttpsCallable("addCurrency")
            .CallAsync(data)
            .ContinueWith(task =>
            {
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        string error = task.Exception?.InnerException?.Message ?? "Unknown error";
                        Debug.LogError($"[CloudFunctions] AddCurrency 실패: {error}");
                        onFail?.Invoke(error);
                        return;
                    }

                    // [추가] 응답 디버깅용 로그
                    var rawData = task.Result.Data;
                    Debug.Log($"[CloudFunctions] Response type: {rawData?.GetType()?.FullName ?? "null"}");
                    Debug.Log($"[CloudFunctions] Response value: {rawData}");

                    var result = task.Result.Data as Dictionary<object, object>;
                    if (result != null && result.ContainsKey("newBalance"))
                    {
                        int newBalance = Convert.ToInt32(result["newBalance"]);
                        Debug.Log($"[CloudFunctions] AddCurrency 성공 - {currencyType} 새 잔액: {newBalance}");
                        onSuccess?.Invoke(newBalance);
                    }
                    else
                    {
                        onFail?.Invoke("Invalid response from server");
                    }
                });
            });
    }

    /// <summary>
    /// 에너지 소비 요청 (서버 검증)
    /// </summary>
    public void SpendEnergy(int amount, string reason,
        Action<int, int, long> onSuccess, Action<string> onFail)
    {
        if (!isInitialized)
        {
            onFail?.Invoke("CloudFunctions not initialized");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "amount", amount },
            { "reason", reason }
        };

        Debug.Log($"[CloudFunctions] SpendEnergy request: x{amount} ({reason})");

        functions.GetHttpsCallable("spendEnergy")
            .CallAsync(data)
            .ContinueWith(task =>
            {
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        string error = task.Exception?.InnerException?.Message ?? "Unknown error";
                        Debug.LogError($"[CloudFunctions] SpendEnergy failed: {error}");
                        onFail?.Invoke(error);
                        return;
                    }

                    var result = task.Result.Data as Dictionary<object, object>;
                    if (result != null && result.ContainsKey("newEnergy"))
                    {
                        int newEnergy = Convert.ToInt32(result["newEnergy"]);
                        int maxEnergy = Convert.ToInt32(result["maxEnergy"]);
                        long serverTime = Convert.ToInt64(result["serverTime"]);
                        Debug.Log($"[CloudFunctions] SpendEnergy success - energy: {newEnergy}/{maxEnergy}");
                        onSuccess?.Invoke(newEnergy, maxEnergy, serverTime);
                    }
                    else
                    {
                        onFail?.Invoke("Invalid response from server");
                    }
                });
            });
    }

    /// <summary>
    /// 에너지 획득 요청 (광고 보상 등)
    /// </summary>
    public void AddEnergy(int amount, string reason,
        Action<int, int, long> onSuccess, Action<string> onFail)
    {
        if (!isInitialized)
        {
            onFail?.Invoke("CloudFunctions not initialized");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "amount", amount },
            { "reason", reason }
        };

        Debug.Log($"[CloudFunctions] AddEnergy request: x{amount} ({reason})");

        functions.GetHttpsCallable("addEnergy")
            .CallAsync(data)
            .ContinueWith(task =>
            {
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        string error = task.Exception?.InnerException?.Message ?? "Unknown error";
                        Debug.LogError($"[CloudFunctions] AddEnergy failed: {error}");
                        onFail?.Invoke(error);
                        return;
                    }

                    var result = task.Result.Data as Dictionary<object, object>;
                    if (result != null && result.ContainsKey("newEnergy"))
                    {
                        int newEnergy = Convert.ToInt32(result["newEnergy"]);
                        int maxEnergy = Convert.ToInt32(result["maxEnergy"]);
                        long serverTime = Convert.ToInt64(result["serverTime"]);
                        Debug.Log($"[CloudFunctions] AddEnergy success - energy: {newEnergy}/{maxEnergy}");
                        onSuccess?.Invoke(newEnergy, maxEnergy, serverTime);
                    }
                    else
                    {
                        onFail?.Invoke("Invalid response from server");
                    }
                });
            });
    }
}
