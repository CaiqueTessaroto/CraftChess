using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class NetworkBootstrap : MonoBehaviour
{
    async void Awake()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Unity Services Inicializado");
        Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);

        DontDestroyOnLoad(gameObject);
    }
}