using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class NetworkBootstrap : MonoBehaviour
{

    public static NetworkBootstrap Instance;
    async void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Unity Services Inicializado");
        Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


}