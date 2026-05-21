using System;
using UnityEngine;
using GoogleMobileAds.Api;

// 광고 시스템 전체를 관리하는 싱글톤 매니저
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("Ad Config")]
    [SerializeField] private AdConfig adConfig;

    private RewardedAd rewardedDoubleAd;
    private RewardedAd rewardedEnergyAd;
    private BannerView bannerView;

    // 광고 로드 상태
    private bool isDoubleAdLoaded = false;
    private bool isEnergyAdLoaded = false;

    private bool isBannerVisible = false;

    // 광고 완료/실패 콜백
    private Action onDoubleAdSuccess;
    private Action onDoubleAdFailed;
    private Action onEnergyAdSuccess;
    private Action onEnergyAdFailed;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAds();
    }

    // AdMob 초기화
    private void InitializeAds()
    {
        if (adConfig == null)
        {
            Debug.LogError("[AdManager] AdConfig is not assigned.");
            return;
        }

        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdManager] MobileAds initialized.");
            LoadDoubleRewardedAd();
            LoadEnergyRewardedAd();
        });
    }

    // ───────────────────────────────────────────
    // 보상 2배 광고 로드
    // ───────────────────────────────────────────
    private void LoadDoubleRewardedAd()
    {
        // 기존 인스턴스 정리
        rewardedDoubleAd?.Destroy();
        rewardedDoubleAd = null;
        isDoubleAdLoaded = false;

        var request = new AdRequest();
        RewardedAd.Load(adConfig.RewardedDoubleId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdManager] Double rewarded ad failed to load: {error?.GetMessage()}");
                return;
            }

            Debug.Log("[AdManager] Double rewarded ad loaded.");
            rewardedDoubleAd = ad;
            isDoubleAdLoaded = true;

            // 광고 종료 후 자동 재로드 등록
            RegisterDoubleAdEvents(rewardedDoubleAd);
        });
    }

    // 에너지 리필 광고 로드
    private void LoadEnergyRewardedAd()
    {
        rewardedEnergyAd?.Destroy();
        rewardedEnergyAd = null;
        isEnergyAdLoaded = false;

        var request = new AdRequest();
        RewardedAd.Load(adConfig.RewardedEnergyId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdManager] Energy rewarded ad failed to load: {error?.GetMessage()}");
                return;
            }

            Debug.Log("[AdManager] Energy rewarded ad loaded.");
            rewardedEnergyAd = ad;
            isEnergyAdLoaded = true;

            RegisterEnergyAdEvents(rewardedEnergyAd);
        });
    }

    // ───────────────────────────────────────────
    // 광고 이벤트 등록
    // ───────────────────────────────────────────
    private void RegisterDoubleAdEvents(RewardedAd ad)
    {
        // 광고 시청 완료 (보상 지급)
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdManager] Double rewarded ad closed.");
            LoadDoubleRewardedAd(); // 다음 광고 미리 로드
        };

        ad.OnAdFullScreenContentFailed += (error) =>
        {
            Debug.LogError($"[AdManager] Double rewarded ad show failed: {error.GetMessage()}");
            onDoubleAdFailed?.Invoke();
            onDoubleAdFailed = null;
            onDoubleAdSuccess = null;
            LoadDoubleRewardedAd();
        };
    }

    private void RegisterEnergyAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdManager] Energy rewarded ad closed.");
            LoadEnergyRewardedAd();
        };

        ad.OnAdFullScreenContentFailed += (error) =>
        {
            Debug.LogError($"[AdManager] Energy rewarded ad show failed: {error.GetMessage()}");
            onEnergyAdFailed?.Invoke();
            onEnergyAdFailed = null;
            onEnergyAdSuccess = null;
            LoadEnergyRewardedAd();
        };
    }

    // ───────────────────────────────────────────
    // 외부 호출 인터페이스
    // ───────────────────────────────────────────

    // 보상 2배 광고 표시
    // onSuccess : 광고 시청 완료 시 호출 (보상 지급)
    // onFailed  : 광고 없거나 실패 시 호출
    public void ShowDoubleRewardedAd(Action onSuccess, Action onFailed = null)
    {
        if (!isDoubleAdLoaded || rewardedDoubleAd == null)
        {
            Debug.Log("[AdManager] Double rewarded ad not ready.");
            onFailed?.Invoke();
            return;
        }

        onDoubleAdSuccess = onSuccess;
        onDoubleAdFailed = onFailed;

        rewardedDoubleAd.Show(reward =>
        {
            Debug.Log($"[AdManager] Double reward earned: {reward.Amount} {reward.Type}");
            onDoubleAdSuccess?.Invoke();
            onDoubleAdSuccess = null;
            onDoubleAdFailed = null;
        });
    }

    // 에너지 리필 광고 표시
    public void ShowEnergyRewardedAd(Action onSuccess, Action onFailed = null)
    {
        if (!isEnergyAdLoaded || rewardedEnergyAd == null)
        {
            Debug.Log("[AdManager] Energy rewarded ad not ready.");
            onFailed?.Invoke();
            return;
        }

        onEnergyAdSuccess = onSuccess;
        onEnergyAdFailed = onFailed;

        rewardedEnergyAd.Show(reward =>
        {
            Debug.Log($"[AdManager] Energy reward earned: {reward.Amount} {reward.Type}");
            onEnergyAdSuccess?.Invoke();
            onEnergyAdSuccess = null;
            onEnergyAdFailed = null;
        });
    }

    // 배너 광고 생성 및 표시
    public void ShowBanner()
    {
        // 이미 표시 중이면 무시
        if (isBannerVisible && bannerView != null)
        {
            Debug.Log("[AdManager] Banner is already visible.");
            return;
        }

        // 기존 배너 정리
        DestroyBanner();

        // 배너 생성 - 화면 최하단, 가로폭 자동 맞춤
        bannerView = new BannerView(adConfig.BannerId, AdSize.Banner, AdPosition.Bottom);

        // 이벤트 등록
        RegisterBannerEvents(bannerView);

        // 광고 요청 및 로드
        var request = new AdRequest();
        bannerView.LoadAd(request);

        isBannerVisible = true;
        Debug.Log("[AdManager] Banner ad requested.");
    }

    // 배너 광고 숨김 (씬 전환 시 등)
    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
            isBannerVisible = false;
            Debug.Log("[AdManager] Banner hidden.");
        }
    }

    // 배너 광고 완전 제거
    public void DestroyBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
            isBannerVisible = false;
            Debug.Log("[AdManager] Banner destroyed.");
        }
    }

    // 배너 상태 확인
    public bool IsBannerVisible() => isBannerVisible;

    private void RegisterBannerEvents(BannerView banner)
    {
        banner.OnBannerAdLoaded += () =>
        {
            Debug.Log("[AdManager] Banner ad loaded successfully.");
        };

        banner.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError($"[AdManager] Banner ad failed to load: {error.GetMessage()}");
            isBannerVisible = false;
        };
    }

    // 광고 준비 상태 확인
    public bool IsDoubleRewardedAdReady() => isDoubleAdLoaded && rewardedDoubleAd != null;
    public bool IsEnergyRewardedAdReady() => isEnergyAdLoaded && rewardedEnergyAd != null;

    void OnDestroy()
    {
        rewardedDoubleAd?.Destroy();
        rewardedEnergyAd?.Destroy();
        DestroyBanner();
    }
}