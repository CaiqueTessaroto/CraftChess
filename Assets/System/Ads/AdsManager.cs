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
    [SerializeField] float firstAdDelay = 60f;
    [SerializeField] float interstitialCooldown = 300f; // 5 minutos

    float lastInterstitialTime = 0;
    float gameStartTime;

    bool firstAd = false;


    string rewardedAdUnit;
    string interstitialAdUnit;
    bool rewardPending = false;

    [Header("Native / Banner")]
    [SerializeField] string bannerAdUnitAndroid = "Banner_Android";
    [SerializeField] string bannerAdUnitIOS = "Banner_iOS";

    string bannerAdUnit;
    bool bannerLoaded = false;

    bool rewardedLoaded = false;
    bool interstitialLoaded = false;

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

        gameStartTime = Time.time;

        if (Advertisement.isInitialized)
            return;

        string gameId = Application.platform == RuntimePlatform.IPhonePlayer
            ? iosGameId
            : androidGameId;

        rewardedAdUnit = rewardedAdUnitAndroid;
        interstitialAdUnit = interstitialAdUnitAndroid;

        bannerAdUnit = Application.platform == RuntimePlatform.IPhonePlayer
        ? bannerAdUnitIOS
        : bannerAdUnitAndroid;

        Advertisement.Initialize(gameId, testMode, this);
    }

    public static void TryShowInterstitial()
    {
        if (Instance == null)
            return;

        float elapsed = Time.time - Instance.gameStartTime;

        // Primeiro anúncio só após 1 minuto
        if (!Instance.firstAd)
        {
            if (elapsed < Instance.firstAdDelay)
                return;

            Instance.firstAd = true;
            Instance.lastInterstitialTime = Time.time;
            Advertisement.Show(Instance.interstitialAdUnit, Instance);
            return;
        }

        // Próximos anúncios
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
        LoadNative();
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

        if (!Instance.rewardedLoaded)
        {
            Debug.Log("⏳ Rewarded ainda não carregado");
            return;
        }

        Advertisement.Show(Instance.rewardedAdUnit, Instance);
    }

    public static void ShowInterstitial()
    {
        if (Instance == null) return;

        Advertisement.Show(Instance.interstitialAdUnit, Instance);
    }

    // =========================
    // LOAD CALLBACKS
    // ========================

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log($"Ad carregado: {adUnitId}");

        if (adUnitId == rewardedAdUnit)
            rewardedLoaded = true;

        if (adUnitId == interstitialAdUnit)
            interstitialLoaded = true;
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"Falha load {adUnitId}: {error} - {message}");

        if (adUnitId == rewardedAdUnit)
            rewardedLoaded = false;

        if (adUnitId == interstitialAdUnit)
            interstitialLoaded = false;
    }

    // =========================
    // SHOW CALLBACKS
    // =========================
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState state)
    {
        if (adUnitId == rewardedAdUnit)
        {
            rewardedLoaded = false;

            if (state == UnityAdsShowCompletionState.COMPLETED)
            {
                Instance.rewardPending = true;
                Debug.Log("Recompensa concedida");

                RewardData reward = null;
                int safety = 50;

                while (safety-- > 0)
                {
                    reward = RewardManager.Instance.GetRandomReward();

                    if (PlayerPrefs.GetInt("Reward_" + reward.id, 0) == 0)
                        break; // reward válido encontrado
                }

                if (reward == null || PlayerPrefs.GetInt("Reward_" + reward.id, 0) == 1)
                {
                    Debug.Log("⚠️ Nenhuma recompensa válida disponível");
                    return;
                }

                RewardManager.Instance.GrantReward(reward);
                // 👉 APLIQUE A RECOMPENSA AQUI
                // Ex: moedas++, reviver peça, etc
            }

            LoadRewarded();
        }

        if (adUnitId == interstitialAdUnit)
        {
            interstitialLoaded = false;
            LoadInterstitial();
        }
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Erro Show {adUnitId}: {error} - {message}");
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }





    // =========================
    // NATIVE / BANNER
    // =========================

    public void LoadNative()
    {
        if (!Advertisement.isInitialized)
            return;

        Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);

        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = () =>
            {
                bannerLoaded = true;
                Debug.Log("Native Banner carregado");
            },
            errorCallback = (error) =>
            {
                bannerLoaded = false;
                Debug.LogWarning("Erro ao carregar Native Banner: " + error);
            }
        };

        Advertisement.Banner.Load(bannerAdUnit, options);
    }

    public void ShowNative()
    {
        if (!bannerLoaded)
        {
            LoadNative();
            return;
        }

        Advertisement.Banner.Show(bannerAdUnit);
    }

    public void HideNative()
    {
        Advertisement.Banner.Hide();
    }

}
