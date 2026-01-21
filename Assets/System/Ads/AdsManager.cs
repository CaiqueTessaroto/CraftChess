using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour,
    IUnityAdsInitializationListener,
    IUnityAdsLoadListener,
    IUnityAdsShowListener
{

    public static AdsManager Instance;
    //4673340916623
    [SerializeField] string androidGameId = "6029556";
    [SerializeField] string iosGameId = "6029557";
    [SerializeField] bool testMode = true;


    [Header("Ad Units")]
    [SerializeField] string rewardedAdUnitAndroid = "Rewarded_Android";
    [SerializeField] string interstitialAdUnitAndroid = "Interstitial_Android";

    [Header("Interstitial Time Settings")]
    [SerializeField] float interstitialCooldown = 300f; // 5 minutos

    float lastInterstitialTime = -999f;


    string rewardedAdUnit;
    string interstitialAdUnit;
    bool rewardPending = false;

    // =========================
    // LIFECYCLE
    // =========================
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (Advertisement.isInitialized)
            return;

        string gameId = Application.platform == RuntimePlatform.IPhonePlayer
            ? iosGameId
            : androidGameId;

        rewardedAdUnit = rewardedAdUnitAndroid;
        interstitialAdUnit = interstitialAdUnitAndroid;

        Advertisement.Initialize(gameId, testMode, this);
    }

    public static void TryShowInterstitial()
    {
        if (Instance == null)
            return;

        if (Time.time - Instance.lastInterstitialTime < Instance.interstitialCooldown)
            return;

        Instance.lastInterstitialTime = Time.time;
        Advertisement.Show(Instance.interstitialAdUnit, Instance);
    }

    // =========================
    // INIT CALLBACKS
    // =========================
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads inicializados");

        LoadRewarded();
        LoadInterstitial();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Erro Ads Init: {error} - {message}");
    }

    // =========================
    // LOAD
    // =========================
    void LoadRewarded()
    {
        Advertisement.Load(rewardedAdUnit, this);
    }

    void LoadInterstitial()
    {
        Advertisement.Load(interstitialAdUnit, this);
    }

    // =========================
    // SHOW (API PÚBLICA)
    // =========================
    public static void ShowRewarded()
    {
        if (Instance == null) return;

        Advertisement.Show(Instance.rewardedAdUnit, Instance);
    }

    public static void ShowInterstitial()
    {
        if (Instance == null) return;

        Advertisement.Show(Instance.interstitialAdUnit, Instance);
    }

    // =========================
    // LOAD CALLBACKS
    // =========================
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log($"Ad carregado: {adUnitId}");
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"Falha load {adUnitId}: {error} - {message}");
    }

    // =========================
    // SHOW CALLBACKS
    // =========================
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState state)
    {
        if (adUnitId == rewardedAdUnit)
        {
            if (state == UnityAdsShowCompletionState.COMPLETED)
            {
                Instance.rewardPending = true;
                Debug.Log("Recompensa concedida");
                // 👉 APLIQUE A RECOMPENSA AQUI
                // Ex: moedas++, reviver peça, etc
            }

            LoadRewarded();
        }

        if (adUnitId == interstitialAdUnit)
        {
            LoadInterstitial();
        }
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Erro Show {adUnitId}: {error} - {message}");
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }
}
