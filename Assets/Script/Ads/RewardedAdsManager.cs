using System;
using UnityEngine;
using GoogleMobileAds.Api;

/// <summary>
/// Quan ly rewarded ad cua AdMob.
/// Tu dong tao khi game chay (khong can keo vao scene), song qua moi scene.
/// Mac dinh dung ad unit TEST cua Google nen chay duoc ngay ma khong can tai khoan AdMob.
/// </summary>
public class RewardedAdsManager : MonoBehaviour
{
    // Ad unit test chinh chu cua Google - luon tra ve quang cao that de test.
    public const string TestRewardedAdUnitAndroid = "ca-app-pub-3940256099942544/5224354917";
    public const string TestRewardedAdUnitIOS = "ca-app-pub-3940256099942544/1712485313";

    public static RewardedAdsManager Instance { get; private set; }

    /// <summary>Bao cho UI biet quang cao da san sang hay chua.</summary>
    public static event Action<bool> OnAdReadyChanged;

    [Header("Ad Unit")]
    [Tooltip("Bat = dung ad unit test cua Google. Tat = dung id that ben duoi.")]
    [SerializeField] private bool useTestAdUnits = true;
    [SerializeField] private string androidAdUnitId = TestRewardedAdUnitAndroid;
    [SerializeField] private string iosAdUnitId = TestRewardedAdUnitIOS;

    [Header("Load Retry")]
    [SerializeField] private int maxLoadRetries = 4;

    private RewardedAd rewardedAd;
    private bool sdkReady;
    private bool loading;
    private int retryCount;
    private bool lastReportedReady;

    /// <summary>Quang cao da tai xong va co the show ngay.</summary>
    public bool IsAdReady => rewardedAd != null && rewardedAd.CanShowAd();

    /// <summary>SDK da initialize xong chua.</summary>
    public bool IsSdkReady => sdkReady;

    private string AdUnitId
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return useTestAdUnits ? TestRewardedAdUnitIOS : iosAdUnitId;
#else
            return useTestAdUnits ? TestRewardedAdUnitAndroid : androidAdUnitId;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject(nameof(RewardedAdsManager));
        go.AddComponent<RewardedAdsManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Tu v11.3 tro di SDK yeu cau Initialize() phai goi tren main thread.
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize(_ =>
        {
            sdkReady = true;
            LoadAd();
        });
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        DestroyCurrentAd();
        Instance = null;
    }

    /// <summary>Tai truoc mot rewarded ad. Goi lai bao nhieu lan cung an toan.</summary>
    public void LoadAd()
    {
        if (!sdkReady || loading || IsAdReady) return;

        loading = true;
        DestroyCurrentAd();

        RewardedAd.Load(AdUnitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            loading = false;

            if (error != null || ad == null)
            {
                retryCount++;
                Debug.LogWarning($"[Ads] Tai rewarded ad that bai ({retryCount}/{maxLoadRetries}): {error?.GetMessage()}");
                NotifyReadyChanged();

                if (retryCount <= maxLoadRetries)
                    Invoke(nameof(LoadAd), Mathf.Min(30f, Mathf.Pow(2f, retryCount)));
                return;
            }

            retryCount = 0;
            rewardedAd = ad;
            HookAdEvents(ad);
            NotifyReadyChanged();
        });
    }

    /// <summary>
    /// Show rewarded ad. <paramref name="onRewardEarned"/> chi chay khi nguoi choi xem du dieu kien nhan thuong.
    /// </summary>
    public void ShowRewardedAd(Action onRewardEarned, Action<string> onUnavailable = null)
    {
        if (!IsAdReady)
        {
            LoadAd();
            onUnavailable?.Invoke(sdkReady
                ? "Quang cao chua san sang, thu lai sau vai giay."
                : "Ads SDK dang khoi tao.");
            return;
        }

        bool rewardGranted = false;
        rewardedAd.Show(_ =>
        {
            if (rewardGranted) return;
            rewardGranted = true;
            onRewardEarned?.Invoke();
        });

        // Ad da duoc dung -> khong con san sang nua, bao UI doi trang thai.
        NotifyReadyChanged();
    }

    private void HookAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            NotifyReadyChanged();
            LoadAd();
        };

        ad.OnAdFullScreenContentFailed += (AdError adError) =>
        {
            Debug.LogWarning($"[Ads] Show rewarded ad that bai: {adError?.GetMessage()}");
            NotifyReadyChanged();
            LoadAd();
        };
    }

    private void DestroyCurrentAd()
    {
        if (rewardedAd == null) return;

        rewardedAd.Destroy();
        rewardedAd = null;
    }

    private void NotifyReadyChanged()
    {
        bool ready = IsAdReady;
        if (ready == lastReportedReady) return;

        lastReportedReady = ready;
        OnAdReadyChanged?.Invoke(ready);
    }
}
