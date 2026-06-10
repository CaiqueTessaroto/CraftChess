using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

using Unity.Collections;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;

public class NetworkLobbyManager : MonoBehaviour
{
    public static NetworkLobbyManager Instance;

    public Lobby currentLobby;

    public NetworkVariable<FixedString32Bytes> LobbyCode =
        new NetworkVariable<FixedString32Bytes>();

    public bool IsHost = false;

    [Header("Player")]
    public Sprite CurrentSprite;

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

    public bool IsConnected()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
    }
    public static void StartMultiplayerMatch(string scene)
    {
        // Só o host carrega a cena — o Netcode sincroniza automaticamente para o cliente
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }
    }
    public async void CreateLobby(string scene = null)
    {

        string text = UIHelperUtils.T("lobby_creating");
        if (string.IsNullOrEmpty(text))
            text = "Creating lobby...";

        FileManager.Instance.SpawnMessage(text);

        try
        {
            // Cria Relay
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            //Debug.Log(NetworkManager.Singleton);

            // Configura Unity Transport
            UnityTransport transport = NetworkManager.Singleton
                .GetComponent<UnityTransport>();

            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Cria Lobby
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                    { "maxPlayers", new DataObject(DataObject.VisibilityOptions.Public, "2") }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                "Chess Lobby",
                3,
                options
            );

            //Debug.Log("Lobby criado");
            //Debug.Log("Código: " + currentLobby.LobbyCode);

            IsHost = NetworkManager.Singleton.StartHost();

            StartCoroutine(LeaveRoutine(scene));
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    public async void JoinLobby(string lobbyCode, string scene = null)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            int maxPlayers = int.Parse(lobby.Data["maxPlayers"].Value);
            int realPlayers = CountRealPlayers(lobby); // veja abaixo

            bool isSpectator = realPlayers >= maxPlayers;
            MultiplayerLobbyState.IsSpectator = isSpectator;

            await SetupRelayAndStart(lobby, scene);
        }
        catch (LobbyServiceException ex)
        {
            // Jogador já está no lobby — tenta reconectar
            if (ex.ErrorCode == 409 || ex.Message.Contains("already a member"))
            {
                await HandleAlreadyInLobby(lobbyCode, scene);
            }
            else if (ex.Reason == LobbyExceptionReason.LobbyNotFound)
                FileManager.Instance.SpawnMessage(UIHelperUtils.T("lobby_not_found") ?? "Lobby not found.");
            else if (ex.Reason == LobbyExceptionReason.LobbyFull) // ← adicionar
            {
                FileManager.Instance.SpawnMessage(UIHelperUtils.T("lobby_full") ?? "Lobby is full.");
            }
            else
            {
                FileManager.Instance.SpawnMessage(
                    UIHelperUtils.T("lobby_error") ?? "Error while trying to enter the lobby.");
                Debug.LogError($"Lobby error: {ex}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private int CountRealPlayers(Lobby lobby)
    {
        string localId = AuthenticationService.Instance.PlayerId;
        int count = 0;
        foreach (var player in lobby.Players)
        {
            if (player.Id == localId) continue;

            bool spectator = player.Data != null
                && player.Data.ContainsKey("isSpectator")
                && player.Data["isSpectator"].Value == "true";
            if (!spectator) count++;
        }
        return count;
    }

    private async Task HandleAlreadyInLobby(string lobbyCode, string scene)
    {
        try
        {
            // Opção 1: Reconectar direto (mantém o player no lobby)
            Lobby lobby = await LobbyService.Instance.ReconnectToLobbyAsync(currentLobby?.Id);
            await SetupRelayAndStart(lobby, scene);
        }
        catch (LobbyServiceException)
        {
            // Opção 2: Se reconectar falhar, sair e entrar de novo
            try
            {
                if (currentLobby != null)
                {
                    await LobbyService.Instance.RemovePlayerAsync(
                        currentLobby.Id,
                        AuthenticationService.Instance.PlayerId
                    );
                }

                Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
                await SetupRelayAndStart(lobby, scene);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Falha ao re-entrar no lobby: {ex}");
            }
        }
    }

    private async Task SetupRelayAndStart(Lobby lobby, string scene)
    {

        await LobbyService.Instance.UpdatePlayerAsync(lobby.Id,
            AuthenticationService.Instance.PlayerId,
            new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                {
                    "isSpectator",
                    new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member,
                        MultiplayerLobbyState.IsSpectator ? "true" : "false"
                    )
                }
                }
            });

        currentLobby = lobby;

        string relayJoinCode = lobby.Data["joinCode"].Value;

        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData
        );

        NetworkManager.Singleton.StartClient();
        StartCoroutine(LeaveRoutine(scene));
    }


    public async void LeaveLobby(string scene = null)
    {
        try
        {
            // CLIENTE
            if (NetworkManager.Singleton.IsClient &&
                !NetworkManager.Singleton.IsHost)
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    currentLobby.Id,
                    Unity.Services.Authentication.AuthenticationService
                        .Instance.PlayerId
                );

                StartCoroutine(ShutdownAndLeave(scene));

                //CancelInvoke();
                //StopAllCoroutines();

                Debug.Log("Cliente saiu do lobby");
            }

            // HOST
            else if (NetworkManager.Singleton.IsHost)
            {
                NotifyHostLeftClientRpc(); // Avisa clientes primeiro

                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);

                StartCoroutine(ShutdownAndLeave(scene));

                Debug.Log("Host encerrou o lobby");
            }

            currentLobby = null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    public IEnumerator LeaveRoutine(string scene)
    {
        yield return null;

        if (!string.IsNullOrEmpty(scene))
            SceneManager.LoadScene(scene);
    }
    private IEnumerator ShutdownAndLeave(string scene)
    {
        yield return new WaitForSeconds(0.5f); // Aguarda clientes receberem o RPC

        NetworkManager.Singleton.Shutdown();

        yield return null;

        if (scene == "Menu")
            Destroy(NetworkManager.Singleton.gameObject);

        if (!string.IsNullOrEmpty(scene))
            SceneManager.LoadScene(scene);
    }


    [ClientRpc]
    private void NotifyHostLeftClientRpc()
    {
        if (NetworkManager.Singleton.IsHost) return;

        string scene = SceneManager.GetActiveScene().name;

        if (scene == "Multiplayer Lobby")
        {
            HandleDisconnect("Menu");
        }
        else if (scene == "Multiplayer")
        {
            currentLobby = null;
            if (PieceControllerNetwork.Instance != null)
                PieceControllerNetwork.Instance.SendGiveUp();
        }
    }

    /*
    public void HandleDisconnect(string scene = "Menu")
    {
        MultiplayerLobbyState.Reset();
        currentLobby = null;
        NetworkManager.Singleton.Shutdown();

        if (!string.IsNullOrEmpty(scene))
            SceneManager.LoadScene(scene);
    }
    */

    public void HandleDisconnect(string scene = "Menu")
    {
        MultiplayerLobbyState.Reset();
        currentLobby = null;

        if (NetworkManager.Singleton.IsHost)
        {
            StartCoroutine(HostShutdownSequence(scene));
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            if (!string.IsNullOrEmpty(scene))
                SceneManager.LoadScene(scene);
        }
    }

    private IEnumerator HostShutdownSequence(string scene)
    {
        NotifyHostLeftClientRpc();
        yield return new WaitForSeconds(0.3f);
        NetworkManager.Singleton.Shutdown();
        yield return null;
        if (!string.IsNullOrEmpty(scene))
            SceneManager.LoadScene(scene);
    }



    private Coroutine _pollCoroutine;

    public void StartPollingLobby()
    {
        if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);
        _pollCoroutine = StartCoroutine(PollLobbyRoutine());
    }

    public void StopPollingLobby()
    {
        if (_pollCoroutine != null)
        {
            StopCoroutine(_pollCoroutine);
            _pollCoroutine = null;
        }
    }

    // Lobby Service permite ~1 req/segundo
    private IEnumerator PollLobbyRoutine()
    {
        while (currentLobby != null)
        {
            yield return new WaitForSeconds(1.5f);

            if (currentLobby == null) yield break;

            Task<Lobby> task = LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                // Pega a mensagem interna sem acessar .Message direto no AggregateException
                string error = task.Exception?.InnerException?.Message ?? "Unknown error";
                //Debug.LogWarning($"[Poll] {error}");
                continue;
            }

            if (task.Result == null) continue;

            currentLobby = task.Result;
            OnLobbyPolled?.Invoke(currentLobby);
        }
    }

    public event Action<Lobby> OnLobbyPolled;

}