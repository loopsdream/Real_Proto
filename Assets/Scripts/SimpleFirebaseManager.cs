// SimpleFirebaseManager.cs - 기본 Firebase 관리자
using UnityEngine;

public class SimpleFirebaseManager : MonoBehaviour
{
    public static SimpleFirebaseManager Instance { get; private set; }
    
    [Header("Firebase Status")]
    public bool isInitialized = false;
    public bool isAuthenticated = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SimpleFirebaseManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Firebase 초기화는 나중에 구현
        InitializeFirebase();
    }
    
    void InitializeFirebase()
    {
        Debug.Log("Firebase 초기화 준비 중...");
        // TODO: Firebase SDK 초기화 로직 추가
        isInitialized = true;
    }
}