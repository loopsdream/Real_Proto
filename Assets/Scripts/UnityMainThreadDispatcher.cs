using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Firebase 콜백 등 백그라운드 스레드에서 Unity 메인 스레드로 작업을 전달하는 헬퍼
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    public static UnityMainThreadDispatcher Instance;

    private readonly Queue<Action> queue = new Queue<Action>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (Application.isPlaying)
                Destroy(gameObject);
        }
    }

    void Update()
    {
        lock (queue)
        {
            while (queue.Count > 0)
            {
                queue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// 메인 스레드에서 실행할 액션을 큐에 추가
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (Instance == null)
        {
            Debug.LogError("[Dispatcher] Instance가 없습니다. Scene에 추가했는지 확인하세요.");
            return;
        }

        lock (Instance.queue)
        {
            Instance.queue.Enqueue(action);
        }
    }
}
