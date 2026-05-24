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

public class NetworkLobbyManager : MonoBehaviour
{
    public static NetworkLobbyManager Instance;

    public Lobby currentLobby;

    public NetworkVariable<FixedString32Bytes> LobbyCode =
        new NetworkVariable<FixedString32Bytes>();

    public bool startedHost = false;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

        if (NetworkManager.Singleton != null)
        {

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
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

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
                    {
                        "joinCode",
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: joinCode
                        )
                    }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                "Chess Lobby",
                2,
                options
            );

            //Debug.Log("Lobby criado");
            //Debug.Log("Código: " + currentLobby.LobbyCode);

            startedHost = NetworkManager.Singleton.StartHost();

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
            Lobby lobby =
                await LobbyService.Instance.JoinLobbyByCodeAsync(
                    lobbyCode
                );

            currentLobby = lobby;

            string relayJoinCode =
                lobby.Data["joinCode"].Value;

            JoinAllocation allocation =
                await RelayService.Instance.JoinAllocationAsync(
                    relayJoinCode
                );

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

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

            //Debug.Log("Entrou no lobby");
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
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

        if (!string.IsNullOrEmpty(scene))
            SceneManager.LoadScene(scene);
    }


    [ClientRpc]
    private void NotifyHostLeftClientRpc()
    {
        // Só executa nos clientes (não no host)
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host encerrou a partida.");
            HandleDisconnect();
        }
    }
    public void HandleDisconnect(string scene = "Menu")
    {
        currentLobby = null;
        NetworkManager.Singleton.Shutdown();

        if (!string.IsNullOrEmpty(scene))
            SceneManager.LoadScene(scene);
    }


}