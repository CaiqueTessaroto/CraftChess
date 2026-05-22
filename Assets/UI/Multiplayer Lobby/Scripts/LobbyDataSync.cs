using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LobbyDataSync : MonoBehaviour
{
    public static LobbyDataSync Instance;

    public event Action<Dictionary<string, DataObject>> OnLobbyDataUpdated;

    private float timer;
    private bool isRefreshing = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (NetworkLobbyManager.Instance.currentLobby == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = 1.5f;
            if (!isRefreshing)
                _ = RefreshLobby();
        }
    }

    private async Task RefreshLobby()
    {
        isRefreshing = true;
        try
        {
            NetworkLobbyManager.Instance.currentLobby =
                await LobbyService.Instance.GetLobbyAsync(
                    NetworkLobbyManager.Instance.currentLobby.Id);

            OnLobbyDataUpdated?.Invoke(NetworkLobbyManager.Instance.currentLobby.Data);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Falha ao atualizar lobby: {e.Message}");
        }
        finally
        {
            isRefreshing = false;
        }
    }
}