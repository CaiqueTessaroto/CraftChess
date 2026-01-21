using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour, IUnityAdsInitializationListener
{

    //4673340916623
    [SerializeField] string androidGameId = "6029556";
    [SerializeField] string iosGameId = "6029557";
    [SerializeField] bool testMode = true;

    void Start()
    {
        string gameId = Application.platform == RuntimePlatform.IPhonePlayer
            ? iosGameId
            : androidGameId;

        Advertisement.Initialize(gameId, testMode, this);
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Ads inicializados com sucesso");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Erro Ads: {error} - {message}");
    }
}
