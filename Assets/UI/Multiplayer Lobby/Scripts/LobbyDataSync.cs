using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class LobbyDataSync : MonoBehaviour
{
    public static LobbyDataSync Instance;

    public event Action<Dictionary<string, DataObject>> OnLobbyDataUpdated;

    private float timer;
    private bool isRefreshing = false;

    private int _failCount = 0;
    private const int MAX_FAILS = 3;

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

            _failCount = 0;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Falha ao atualizar lobby: {e.Message}");

            if (e.Reason == LobbyExceptionReason.LobbyNotFound ||
                e.Reason == LobbyExceptionReason.Forbidden)
            {
                NetworkLobbyManager.Instance.HandleDisconnect("Menu");
                return;
            }

            // Erros temporários: só desconecta após N falhas seguidas
            _failCount++;
            if (_failCount >= MAX_FAILS)
                NetworkLobbyManager.Instance.HandleDisconnect("Menu");
        }
        finally
        {
            isRefreshing = false;
        }
    }

    public void LoseConnectionDuringMatch()
    {
        bool multiplayerScene = SceneManager.GetActiveScene().name == "Multiplayer";
        if (!multiplayerScene)
            return;

        string text = UIHelperUtils.T("lobby_exited");
        if (string.IsNullOrEmpty(text))
            text = "A player has left the lobby.";
        FileManager.Instance.SpawnMessage(text);

        NetworkLobbyManager.Instance.currentLobby = null;

        MultiplayerPieceController mp = FindFirstObjectByType<MultiplayerPieceController>();
        if (mp != null)
            if (!NetworkLobbyManager.Instance.IsHost)
                mp.EndGameHostLoseConnection();
    }

}