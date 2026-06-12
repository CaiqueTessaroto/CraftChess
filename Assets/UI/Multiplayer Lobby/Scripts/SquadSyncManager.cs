using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SquadSyncManager : NetworkBehaviour
{
    public static SquadSyncManager Instance { get; private set; }

    // ─── Eventos ───────────────────────────────────────────────────────────
    public event Action<bool> OnRemoteSquadReady; // true = White, false = Black

    // ─── Canais de mensagem ────────────────────────────────────────────────
    private const string MSG_SPRITE_HOST_TO_CLIENT = "SquadSprite_H2C";
    private const string MSG_SPRITE_CLIENT_TO_HOST = "SquadSprite_C2H";
    private const string MSG_SQUAD_JSON_HOST_TO_CLIENT = "SquadJson_H2C";
    private const string MSG_SQUAD_JSON_CLIENT_TO_HOST = "SquadJson_C2H";
    private const string MSG_PROFILE_HOST_TO_CLIENT = "ProfileImage_H2C";
    private const string MSG_PROFILE_CLIENT_TO_HOST = "ProfileImage_C2H";


    private const string MSG_HOST_PROFILE_TO_CLIENT = "HostProfile";
    private const string MSG_PLAYER_PROFILE_TO_CLIENT = "PlayerProfile";

    // ─── Estado interno ────────────────────────────────────────────────────
    private Dictionary<string, Sprite> pendingSpritesWhite = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> pendingSpritesBlack = new Dictionary<string, Sprite>();

    private int expectedSpriteCountWhite = 0, expectedSpriteCountBlack = 0;
    private int receivedSpriteCountWhite = 0, receivedSpriteCountBlack = 0;
    private bool jsonReceivedWhite = false, jsonReceivedBlack = false;

    // ───────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ───────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_SPRITE_CLIENT_TO_HOST, OnReceiveSpriteFromClient);
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_PROFILE_CLIENT_TO_HOST, OnReceiveProfileFromClient);
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_SQUAD_JSON_CLIENT_TO_HOST, OnReceiveSquadJsonFromClient);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_SPRITE_HOST_TO_CLIENT, OnReceiveSpriteFromHost);
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_PROFILE_HOST_TO_CLIENT, OnReceiveProfileFromHost);
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_SQUAD_JSON_HOST_TO_CLIENT, OnReceiveSquadJsonFromHost);

            if (!NetworkLobbyManager.Instance.isSpectator)
                StartCoroutine(SendProfileImageDelayed_Client());
        }

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
            MSG_HOST_PROFILE_TO_CLIENT,
            OnReceiveHostProfile);

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
            MSG_PLAYER_PROFILE_TO_CLIENT,
            OnReceivePlayerProfile);

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton?.CustomMessagingManager != null)
        {
            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_SPRITE_HOST_TO_CLIENT);
            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_SPRITE_CLIENT_TO_HOST);
            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_PROFILE_HOST_TO_CLIENT);
            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_PROFILE_CLIENT_TO_HOST);
            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_SQUAD_JSON_CLIENT_TO_HOST);
            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_SQUAD_JSON_HOST_TO_CLIENT);


            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_HOST_PROFILE_TO_CLIENT);

            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_PLAYER_PROFILE_TO_CLIENT);
        }

        if (IsHost)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    // ───────────────────────────────────────────────────────────────────────
    // API PÚBLICA
    // ───────────────────────────────────────────────────────────────────────

    public void SetLocalSquadAndSync(string rootPath, string folderName, string squadName, string jsonFile)
    {
        if (MultiplayerLobbyUI.Instance == null)
            return;

        bool isWhite = MultiplayerLobbyUI.Instance.isWhite;

        ResetSync();
        SetLocalSquad(rootPath, folderName, squadName, jsonFile, isWhite);

        MatchSquadData squad = isWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        if (squad == null) return;

        MultiplayerLobbyState.LocalIsWhite = isWhite;

        if (IsHost)
        {
            AssignSquadByColor(squad, isWhite, isLocal: true);
            SetIdByColor(NetworkManager.ServerClientId.ToString(), isWhite);

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.ServerClientId) continue;
                if (string.IsNullOrEmpty(MultiplayerLobbyState.PlayerClientId)) continue;
                if (clientId != ulong.Parse(MultiplayerLobbyState.PlayerClientId)) continue;

                SendSquadJson(BuildJsonPayloadRaw(isWhite), clientId, toHost: false);

                StartCoroutine(SendSpritesToClient(
                    NetworkManager.Singleton.ConnectedClientsIds, isWhite));
            }

            MultiplayerLobbyUI.Instance?.RefreshLocalUI();
        }
        else
        {
            SendSquadJson(BuildJsonPayloadRaw(isWhite), 0, toHost: true);
            StartCoroutine(SendSpritesToHost(isWhite));
        }
    }

    public void SetLocalSquad(string rootPath, string folderName,
                              string squadName, string jsonFile, bool isWhite)
    {
        if (!File.Exists(jsonFile))
        {
            Debug.LogWarning($"[SquadSync] Squad JSON não encontrado: {jsonFile}");
            return;
        }

        Squad data = JsonUtility.FromJson<Squad>(File.ReadAllText(jsonFile));
        var squad = new MatchSquadData { Data = data };

        string spriteSquadPath = Path.Combine(rootPath,
            FileManager.Instance.basePath_SquadData, folderName, squadName.Trim() + ".png");

        if (File.Exists(spriteSquadPath))
            squad.SquadImageRaw = File.ReadAllBytes(spriteSquadPath);

        foreach (SquadPieceData piece in data.Pieces)
        {
            string movPath = Path.Combine(rootPath,
                FileManager.Instance.basePath_PieceData, piece.Squad, piece.Name + ".json");

            if (!File.Exists(movPath))
            {
                Debug.LogWarning($"[SquadSync] Movement JSON não encontrado: {movPath}");
                continue;
            }

            squad.Pieces[piece.NameInSquad] =
                JsonUtility.FromJson<MovementConfigData>(File.ReadAllText(movPath));

            string spritePath = Path.Combine(rootPath,
                FileManager.Instance.basePath_Sprite, piece.SpriteSet, piece.Sprite.Trim() + ".png");

            if (!File.Exists(spritePath))
                Debug.LogWarning($"[SquadSync] Sprite não encontrada: {spritePath}");

            squad.Sprites[piece.NameInSquad] = UIHelperUtils.GetSpriteFromPath(spritePath);
        }

        if (isWhite) MultiplayerLobbyState.WhiteSquad = squad;
        else MultiplayerLobbyState.BlackSquad = squad;
    }

    // ───────────────────────────────────────────────────────────────────────
    // CONEXÃO — Client conecta / desconecta
    // ───────────────────────────────────────────────────────────────────────

    private void OnClientConnected(ulong clientId)
    {
        ResetSync();

        if (clientId == NetworkManager.ServerClientId) return;

        bool isPlayer = string.IsNullOrEmpty(MultiplayerLobbyState.PlayerClientId);

        if (isPlayer)
        {
            MultiplayerLobbyState.PlayerClientId = clientId.ToString();
            //    Debug.Log($"[SquadSync] PlayerClientId definido: {clientId}");

            string text = UIHelperUtils.T("lobby_entered");
            if (string.IsNullOrEmpty(text)) text = "A player has joined the lobby.";
            FileManager.Instance.SpawnMessage(text);
        }
        else
        {
            string text = UIHelperUtils.T("lobby_entered_spectator");
            if (string.IsNullOrEmpty(text)) text = "A player entered the lobby as a spectator.";
            FileManager.Instance.SpawnMessage(text);
        }

        Debug.Log($"[SquadSync] Client {clientId} conectou.");

        if (isPlayer)
            StartCoroutine(SendProfileImageDelayed(clientId));
        else
        {
            MultiplayerLobbyState.SendReadyStateToHost(false);
            StartCoroutine(SendExistingProfileImagesToSpectator(clientId));
            return;
        }

        if (MultiplayerLobbyState.WhiteSquad != null)
        {
            var payload = SquadJsonPayloadRawFromDisc(true);
            if (payload != null)
            {
                foreach (ulong u_clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (u_clientId == NetworkManager.ServerClientId) continue;
                    SendSquadJson(payload, u_clientId, toHost: false);
                }
                StartCoroutine(SendSpritesToClient(NetworkManager.Singleton.ConnectedClientsIds, true));
            }
        }

        if (MultiplayerLobbyState.BlackSquad != null)
        {
            var payload = SquadJsonPayloadRawFromDisc(false);
            if (payload != null)
            {
                foreach (ulong u_clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (u_clientId == NetworkManager.ServerClientId) continue;
                    SendSquadJson(payload, u_clientId, toHost: false);
                }
                StartCoroutine(SendSpritesToClient(NetworkManager.Singleton.ConnectedClientsIds, false));
            }
        }

    }

    private IEnumerator SendExistingProfileImagesToSpectator(ulong clientId)
    {
        yield return new WaitForSeconds(1f);

        SendStoredProfileImage(
            MultiplayerLobbyState.HostProfileImageRaw,
            clientId,
            MSG_HOST_PROFILE_TO_CLIENT);

        SendStoredProfileImage(
            MultiplayerLobbyState.ClientProfileImageRaw,
            clientId,
            MSG_PLAYER_PROFILE_TO_CLIENT);
    }

    private void SendStoredProfileImage(
    byte[] raw,
    ulong targetClientId,
    string messageName)
    {
        if (raw == null || raw.Length == 0)
            return;

        using var writer = new FastBufferWriter(
            4 + raw.Length,
            Allocator.Temp);

        writer.WriteValueSafe(raw.Length);
        writer.WriteBytesSafe(raw);

        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
            messageName,
            targetClientId,
            writer,
            NetworkDelivery.ReliableFragmentedSequenced);

        Debug.Log(
            $"[SquadSync] Stored profile enviada para {targetClientId} ({raw.Length / 1024f:F1} kb)");
    }

    public void BroadcastSquadsToSpectators()
    {
        if (!IsHost) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId) continue;

            // Espectador = qualquer client conectado que não seja "o player"
            if (clientId.ToString() == MultiplayerLobbyState.PlayerClientId) continue;

            var targets = new List<ulong> { clientId };

            if (MultiplayerLobbyState.WhiteSquad != null)
            {
                var payload = BuildJsonPayloadRaw(true);
                if (payload != null)
                {
                    SendSquadJson(payload, clientId, toHost: false);
                    StartCoroutine(SendSpritesToClient(targets, true));
                }
            }

            if (MultiplayerLobbyState.BlackSquad != null)
            {
                var payload = BuildJsonPayloadRaw(false);
                if (payload != null)
                {
                    SendSquadJson(payload, clientId, toHost: false);
                    StartCoroutine(SendSpritesToClient(targets, false));
                }
            }
        }

        Debug.Log("[SquadSync] Squads (re)enviados para espectadores após ready do client.");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        bool isMultiplayerScene = SceneManager.GetActiveScene().name == "Multiplayer";
        bool isLobbyScene = SceneManager.GetActiveScene().name == "Multiplayer Lobby";

        if (IsHost)
        {
            HandleHostDisconnect(clientId, isMultiplayerScene);
            return;
        }

        HandleClientDisconnect(clientId, isLobbyScene, isMultiplayerScene);
    }

    private void HandleHostDisconnect(ulong clientId, bool isMultiplayerScene)
    {
        bool isPlayer = clientId.ToString() == MultiplayerLobbyState.PlayerClientId;

        if (clientId != NetworkManager.ServerClientId)
        {
            if (isPlayer)
            {
                MultiplayerLobbyState.PlayerClientId = null;
                ShowPlayerLeftMessage();
            }
            else
            {
                ShowSpectatorLeftMessage();
            }
        }

        if (isMultiplayerScene)
        {
            if (isPlayer)
            {
                MultiplayerPieceController mp =
                    FindFirstObjectByType<MultiplayerPieceController>();

                if (mp != null)
                    mp.EndGameClientLoseConnection();

                MultiplayerLobbyState.ClientProfileImageRaw = null;
            }
        }

        if (MultiplayerLobbyUI.Instance)
        {
            if (isPlayer)
                MultiplayerLobbyUI.Instance.play2ProfileImage.sprite =
                    MultiplayerLobbyUI.Instance.defaultProfileSprite;
        }
    }

    private void HandleClientDisconnect(
        ulong clientId,
        bool isLobbyScene,
        bool isMultiplayerScene)
    {
        bool hostDisconnected =
            clientId == NetworkManager.ServerClientId;

        bool spectatorDisconnected =
            clientId.ToString() != MultiplayerLobbyState.PlayerClientId && clientId != NetworkManager.ServerClientId;

        if (isLobbyScene)
        {
            if (hostDisconnected)
            {
                NetworkLobbyManager.Instance.HandleDisconnect();
                return;
            }

            if (spectatorDisconnected)
                ShowSpectatorLeftMessage();

            return;
        }

        if (isMultiplayerScene)
        {
            if (hostDisconnected)
            {
                ShowPlayerLeftMessage();
                NetworkLobbyManager.Instance.currentLobby = null;

                MultiplayerPieceController mp =
                    FindFirstObjectByType<MultiplayerPieceController>();

                if (mp != null)
                    mp.EndGameHostLoseConnection();
            }
            else if (spectatorDisconnected)
            {
                ShowSpectatorLeftMessage();
            }
        }
    }

    private void ShowPlayerLeftMessage()
    {
        string text = UIHelperUtils.T("lobby_exited");

        if (string.IsNullOrEmpty(text))
            text = "A player has left the lobby.";

        FileManager.Instance.SpawnMessage(text);
    }

    private void ShowSpectatorLeftMessage()
    {
        string text = UIHelperUtils.T("lobby_exited_spectator");

        if (string.IsNullOrEmpty(text))
            text = "A player who was a spectator left the lobby.";

        FileManager.Instance.SpawnMessage(text);
    }

    // ───────────────────────────────────────────────────────────────────────
    // IMAGEM DE PERFIL — Envio e recebimento
    // ───────────────────────────────────────────────────────────────────────

    private IEnumerator SendProfileImageDelayed(ulong clientId)
    {
        yield return new WaitForSeconds(1f);
        SendProfileImage(clientId, toHost: false);
    }

    private IEnumerator SendProfileImageDelayed_Client()
    {
        yield return new WaitForSeconds(1f);
        SendProfileImage(NetworkManager.ServerClientId, toHost: true);
    }

    private void SendProfileImage(ulong targetClientId, bool toHost)
    {
        byte[] raw = ProfileImageManager.Instance?.CurrentTexture != null
            ? ProfileImageManager.Instance.CurrentTexture.EncodeToPNG()
            : null;

        if (raw == null || raw.Length == 0)
        {
            Debug.Log("[SquadSync] Sem foto de perfil para enviar.");
            return;
        }

        if (IsHost) MultiplayerLobbyState.HostProfileImageRaw = raw;
        else MultiplayerLobbyState.ClientProfileImageRaw = raw;

        MultiplayerLobbyUI.Instance?.ApplyProfileImages();

        var writer = new FastBufferWriter(4 + raw.Length + 8, Allocator.Temp);
        using (writer)
        {
            writer.WriteValueSafe(raw.Length);
            writer.WriteBytesSafe(raw);

            if (toHost)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    MSG_PROFILE_CLIENT_TO_HOST, NetworkManager.ServerClientId, writer,
                    NetworkDelivery.ReliableFragmentedSequenced);
            }
            else
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    MSG_PROFILE_HOST_TO_CLIENT, targetClientId, writer,
                    NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        Debug.Log($"[SquadSync] ProfileImage enviada | {raw.Length / 1024f:F1} kb | para: {(toHost ? "Host" : "Client")}");
    }

    private void OnReceiveProfileFromHost(ulong senderId, FastBufferReader reader)
        => ProcessReceivedProfile(reader, true);

    private void OnReceiveProfileFromClient(ulong senderId, FastBufferReader reader)
        => ProcessReceivedProfile(reader, false);

    private void OnReceiveHostProfile(ulong senderId, FastBufferReader reader)
        => ProcessReceivedProfile(reader, true);

    private void OnReceivePlayerProfile(ulong senderId, FastBufferReader reader)
        => ProcessReceivedProfile(reader, false);

    private void ProcessReceivedProfile(
        FastBufferReader reader,
        bool isHostProfile)
    {
        reader.ReadValueSafe(out int length);

        byte[] raw = new byte[length];
        reader.ReadBytesSafe(ref raw, length);

        if (isHostProfile)
        {
            MultiplayerLobbyState.HostProfileImageRaw = raw;
        }
        else
        {
            MultiplayerLobbyState.ClientProfileImageRaw = raw;
        }

        Debug.Log(
            $"[SquadSync] ProfileImage recebida ({(isHostProfile ? "Host" : "Player")}) | {length / 1024f:F1} kb");

        MultiplayerLobbyUI.Instance?.ApplyProfileImages();
    }

    // ───────────────────────────────────────────────────────────────────────
    // SQUAD JSON — Envio e recebimento
    // ───────────────────────────────────────────────────────────────────────

    private void SendSquadJson(SquadJsonPayloadRaw payload, ulong targetClientId, bool toHost)
    {
        string json = JsonUtility.ToJson(payload);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

        var writer = new FastBufferWriter(4 + data.Length + 8, Allocator.Temp, 4 + data.Length + 8);
        using (writer)
        {
            writer.WriteValueSafe(data.Length);
            writer.WriteBytesSafe(data);

            string channel = toHost ? MSG_SQUAD_JSON_CLIENT_TO_HOST : MSG_SQUAD_JSON_HOST_TO_CLIENT;
            ulong target = toHost ? NetworkManager.ServerClientId : targetClientId;

            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                channel, target, writer,
                NetworkDelivery.ReliableFragmentedSequenced);
        }

        Debug.Log($"[SquadSync] SquadJSON enviado | {data.Length / 1024f:F1} kb | toHost={toHost}");
    }

    private void OnReceiveSquadJsonFromHost(ulong senderId, FastBufferReader reader)
        => ProcessReceivedSquadJson(reader, fromHost: true);

    private void OnReceiveSquadJsonFromClient(ulong senderId, FastBufferReader reader)
        => ProcessReceivedSquadJson(reader, fromHost: false);

    private void ProcessReceivedSquadJson(FastBufferReader reader, bool fromHost)
    {
        reader.ReadValueSafe(out int length);
        byte[] data = new byte[length];
        reader.ReadBytesSafe(ref data, length);

        string json = System.Text.Encoding.UTF8.GetString(data);
        var payload = JsonUtility.FromJson<SquadJsonPayloadRaw>(json);

        bool isWhite = payload.IsWhite;

        var squad = new MatchSquadData
        {
            Data = JsonUtility.FromJson<Squad>(payload.SquadJson)
        };
        foreach (var p in payload.Pieces)
            squad.Pieces[p.Name] = JsonUtility.FromJson<MovementConfigData>(p.MovementJson);

        if (payload.SquadImageRaw != null && payload.SquadImageRaw.Length > 0)
            squad.SquadImageRaw = payload.SquadImageRaw;

        SetIdByColor(payload.SenderId, isWhite);
        AssignSquadByColor(squad, isWhite, isLocal: false);
        SetExpectedByColor(payload.PieceCount, isWhite);
        SetJsonReceivedByColor(true, isWhite);

        if (!fromHost)
            ConfirmSquadReceivedClientRpc(isWhite);

        Debug.Log($"[SquadSync] SquadJSON processado | {(isWhite ? "White" : "Black")} | {payload.PieceCount} peças");
    }

    [ClientRpc]
    private void ConfirmSquadReceivedClientRpc(bool isWhite)
    {
        if (IsHost) return;

        Debug.Log($"[SquadSync] Host confirmou recebimento do squad ({(isWhite ? "White" : "Black")}).");

        SetJsonReceivedByColor(true, isWhite);
        CheckIfComplete(isWhite);
        MultiplayerLobbyUI.Instance?.RefreshLocalUI();
    }

    // ───────────────────────────────────────────────────────────────────────
    // SQUAD — Pedido ao client (ClientRpc → client responde)
    // ───────────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RequestSquadFromClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        bool isWhite = MultiplayerLobbyState.LocalIsWhite;

        MatchSquadData localSquad = isWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        if (localSquad == null) return;

        Debug.Log("[SquadSync] Recebi pedido do host. Enviando meu squad...");

        SendSquadJson(BuildJsonPayloadRaw(isWhite), 0, toHost: true);
        StartCoroutine(SendSpritesToHost(isWhite));
    }

    // ───────────────────────────────────────────────────────────────────────
    // SPRITES — Envio e recebimento
    // ───────────────────────────────────────────────────────────────────────

    private IEnumerator SendSpritesToClient(IReadOnlyList<ulong> clientIds, bool senderIsWhite)
    {
        MatchSquadData squad = senderIsWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        if (squad == null) yield break;

        Dictionary<string, byte[]> spritesCache = senderIsWhite
            ? MultiplayerLobbyState.WhiteSpritesRaw
            : MultiplayerLobbyState.BlackSpritesRaw;

        foreach (SquadPieceData piece in squad.Data.Pieces)
        {
            byte[] pngBytes;

            // 1) squad recebido via rede → já temos os bytes em memória
            if (!spritesCache.TryGetValue(piece.NameInSquad, out pngBytes) || pngBytes == null)
            {
                // 2) squad local → lê do disco
                pngBytes = LoadPngBytes(piece);
            }

            if (pngBytes == null)
            {
                Debug.LogWarning($"[SquadSync] Sprite não encontrada (memória/disco): {piece.NameInSquad}");
                continue;
            }

            int nameLen = System.Text.Encoding.UTF8.GetByteCount(piece.NameInSquad);
            int bufferSize = 2 + nameLen + 4 + pngBytes.Length + 33;

            var writer = new FastBufferWriter(bufferSize, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(piece.NameInSquad);
                writer.WriteValueSafe(pngBytes.Length);
                writer.WriteBytesSafe(pngBytes);
                writer.WriteValueSafe(senderIsWhite);

                foreach (ulong clientId in clientIds)
                {
                    if (clientId == NetworkManager.ServerClientId) continue;
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                        MSG_SPRITE_HOST_TO_CLIENT, clientId, writer,
                        NetworkDelivery.ReliableFragmentedSequenced);
                }
            }

            yield return new WaitForSeconds(0.10f);
        }

        Debug.Log($"[SquadSync] Host terminou de enviar sprites ({(senderIsWhite ? "White" : "Black")}).");
    }

    private IEnumerator SendSpritesToHost(bool senderIsWhite)
    {
        MatchSquadData squad = senderIsWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        if (squad == null) yield break;

        bool ready = MultiplayerLobbyState.ClientIsReady;
        if (ready)
        {
            MultiplayerLobbyUI.Instance.UpdateReadyUI(false);
            MultiplayerLobbyState.SendReadyStateToHost(false);
        }

        Dictionary<string, byte[]> spritesCache = senderIsWhite
            ? MultiplayerLobbyState.WhiteSpritesRaw
            : MultiplayerLobbyState.BlackSpritesRaw;

        foreach (SquadPieceData piece in squad.Data.Pieces)
        {
            byte[] pngBytes;

            // 1) squad recebido via rede → já temos os bytes em memória
            if (!spritesCache.TryGetValue(piece.NameInSquad, out pngBytes) || pngBytes == null)
            {
                // 2) squad local → lê do disco
                pngBytes = LoadPngBytes(piece);
            }

            if (pngBytes == null)
            {
                Debug.LogWarning($"[SquadSync] Sprite não encontrada (memória/disco): {piece.NameInSquad}");
                continue;
            }

            int nameLen = System.Text.Encoding.UTF8.GetByteCount(piece.NameInSquad);
            int bufferSize = 2 + nameLen + 4 + pngBytes.Length + 33;

            var writer = new FastBufferWriter(bufferSize, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(piece.NameInSquad);
                writer.WriteValueSafe(pngBytes.Length);
                writer.WriteBytesSafe(pngBytes);
                writer.WriteValueSafe(senderIsWhite);

                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    MSG_SPRITE_CLIENT_TO_HOST, NetworkManager.ServerClientId, writer,
                    NetworkDelivery.ReliableFragmentedSequenced);
            }

            yield return new WaitForSeconds(0.10f);
        }

        Debug.Log($"[SquadSync] Client terminou de enviar sprites ({(senderIsWhite ? "White" : "Black")}).");
    }

    private void OnReceiveSpriteFromClient(ulong senderId, FastBufferReader reader)
        => ProcessReceivedSprite(reader);

    private void OnReceiveSpriteFromHost(ulong senderId, FastBufferReader reader)
        => ProcessReceivedSprite(reader);

    private void ProcessReceivedSprite(FastBufferReader reader)
    {
        bool ready = MultiplayerLobbyState.ClientIsReady;
        if (ready)
        {
            if (!NetworkLobbyManager.Instance.isSpectator)
            {
                MultiplayerLobbyUI.Instance.UpdateReadyUI(false);
                MultiplayerLobbyState.SendReadyStateToHost(false);
            }
        }

        reader.ReadValueSafe(out string pieceName);
        reader.ReadValueSafe(out int length);

        byte[] pngBytes = new byte[length];
        reader.ReadBytesSafe(ref pngBytes, length);
        reader.ReadValueSafe(out bool senderIsWhite);

        if (senderIsWhite) MultiplayerLobbyState.WhiteSpritesRaw[pieceName] = pngBytes;
        else MultiplayerLobbyState.BlackSpritesRaw[pieceName] = pngBytes;

        var tex = new Texture2D(408, 408, TextureFormat.RGBA32, false);
        tex.LoadImage(pngBytes);

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);

        MatchSquadData targetSquad = senderIsWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        Dictionary<string, Sprite> pending = senderIsWhite
            ? pendingSpritesWhite
            : pendingSpritesBlack;

        if (targetSquad != null) targetSquad.Sprites[pieceName] = sprite;
        else pending[pieceName] = sprite;

        if (senderIsWhite) receivedSpriteCountWhite++;
        else receivedSpriteCountBlack++;

        if (targetSquad != null)
            CheckIfComplete(senderIsWhite);
    }

    // ───────────────────────────────────────────────────────────────────────
    // VERIFICAÇÃO DE CONCLUSÃO
    // ───────────────────────────────────────────────────────────────────────

    private void CheckIfComplete(bool isWhite)
    {
        MatchSquadData targetSquad = isWhite ? MultiplayerLobbyState.WhiteSquad : MultiplayerLobbyState.BlackSquad;
        Dictionary<string, Sprite> pending = isWhite ? pendingSpritesWhite : pendingSpritesBlack;
        int expected = isWhite ? expectedSpriteCountWhite : expectedSpriteCountBlack;
        int received = isWhite ? receivedSpriteCountWhite : receivedSpriteCountBlack;
        bool jsonReady = isWhite ? jsonReceivedWhite : jsonReceivedBlack;

        if (targetSquad == null) return;

        if (pending.Count > 0)
        {
            foreach (var kv in pending) targetSquad.Sprites[kv.Key] = kv.Value;
            pending.Clear();
        }

        if (!jsonReady || received < expected) return;

        Debug.Log($"[SquadSync] Squad completo: {(isWhite ? "White" : "Black")}");
        MultiplayerLobbyState.Log("CheckIfComplete");
        OnRemoteSquadReady?.Invoke(isWhite);
    }

    // ───────────────────────────────────────────────────────────────────────
    // HELPERS
    // ───────────────────────────────────────────────────────────────────────

    private void ResetSync()
    {
        pendingSpritesWhite.Clear();
        pendingSpritesBlack.Clear();

        expectedSpriteCountWhite = expectedSpriteCountBlack = 0;
        receivedSpriteCountWhite = receivedSpriteCountBlack = 0;
        jsonReceivedWhite = jsonReceivedBlack = false;
    }

    private void SetIdByColor(string id, bool isWhite)
    {
        if (isWhite) MultiplayerLobbyState.WhiteSquadOwnerId = id;
        else MultiplayerLobbyState.BlackSquadOwnerId = id;
    }

    private void AssignSquadByColor(MatchSquadData squad, bool isWhite, bool isLocal = false)
    {
        if (isWhite) MultiplayerLobbyState.WhiteSquad = squad;
        else MultiplayerLobbyState.BlackSquad = squad;

        if (!isLocal)
            CheckIfComplete(isWhite);
    }

    private void SetExpectedByColor(int count, bool isWhite)
    {
        if (isWhite) expectedSpriteCountWhite = count;
        else expectedSpriteCountBlack = count;
    }

    private void SetJsonReceivedByColor(bool value, bool isWhite)
    {
        if (isWhite) jsonReceivedWhite = value;
        else jsonReceivedBlack = value;
    }

    private SquadJsonPayloadRaw SquadJsonPayloadRawFromDisc(bool isWhite, string senderId = null)
    {
        MatchSquadData squad = isWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        var pieceJsons = new List<SquadJsonPayloadRaw.PieceJsonDataRaw>();

        foreach (SquadPieceData piece in squad.Data.Pieces)
        {
            string movPath = Path.Combine(Application.persistentDataPath,
                FileManager.Instance.basePath_PieceData, piece.Squad, piece.Name + ".json");

            if (!File.Exists(movPath))
            {
                Debug.LogWarning($"[SquadSync] Movement JSON não encontrado: {movPath}");
                continue;
            }

            pieceJsons.Add(new SquadJsonPayloadRaw.PieceJsonDataRaw
            {
                Name = piece.NameInSquad,
                MovementJson = File.ReadAllText(movPath)
            });
        }

        return new SquadJsonPayloadRaw
        {
            SquadJson = JsonUtility.ToJson(squad.Data),
            PieceCount = pieceJsons.Count,
            Pieces = pieceJsons.ToArray(),
            IsWhite = isWhite,
            SenderId = senderId ?? NetworkManager.Singleton.LocalClientId.ToString(),
            SquadImageRaw = squad.SquadImageRaw
        };
    }

    private SquadJsonPayloadRaw BuildJsonPayloadRaw(bool isWhite, string senderId = null)
    {
        MatchSquadData squad = isWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        if (squad == null)
        {
            Debug.LogWarning($"[SquadSync] BuildJsonPayloadRaw: squad nulo ({(isWhite ? "White" : "Black")}).");
            return null;
        }

        var pieceJsons = new List<SquadJsonPayloadRaw.PieceJsonDataRaw>();

        foreach (var kv in squad.Pieces)
        {
            pieceJsons.Add(new SquadJsonPayloadRaw.PieceJsonDataRaw
            {
                Name = kv.Key,
                MovementJson = JsonUtility.ToJson(kv.Value)
            });
        }

        string ownerId = isWhite
            ? MultiplayerLobbyState.WhiteSquadOwnerId
            : MultiplayerLobbyState.BlackSquadOwnerId;

        return new SquadJsonPayloadRaw
        {
            SquadJson = JsonUtility.ToJson(squad.Data),
            PieceCount = pieceJsons.Count,
            Pieces = pieceJsons.ToArray(),
            IsWhite = isWhite,
            SenderId = senderId ?? ownerId ?? NetworkManager.Singleton.LocalClientId.ToString(),
            SquadImageRaw = squad.SquadImageRaw
        };
    }

    private MatchSquadData RebuildSquadFromJson(SquadJsonPayloadRaw payload)
    {
        var squadData = new MatchSquadData
        {
            Data = JsonUtility.FromJson<Squad>(payload.SquadJson)
        };

        foreach (SquadJsonPayloadRaw.PieceJsonDataRaw pieceJson in payload.Pieces)
            squadData.Pieces[pieceJson.Name] =
                JsonUtility.FromJson<MovementConfigData>(pieceJson.MovementJson);

        if (payload.SquadImageRaw != null && payload.SquadImageRaw.Length > 0)
        {
            squadData.SquadImageRaw = payload.SquadImageRaw;
            Debug.Log($"[SquadSync] SquadImage recebida | {payload.SquadImageRaw.Length / 1024f:F1} kb | Cor: {(payload.IsWhite ? "White" : "Black")}");
        }
        else
        {
            Debug.LogWarning($"[SquadSync] SquadImage NÃO recebida | Cor: {(payload.IsWhite ? "White" : "Black")}");
        }

        return squadData;
    }

    private byte[] LoadPngBytes(SquadPieceData piece)
    {
        string path = Path.Combine(Application.persistentDataPath,
            FileManager.Instance.basePath_Sprite, piece.SpriteSet, piece.Sprite.Trim() + ".png");

        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}

// ─── Structs de payload ────────────────────────────────────────────────────

[Serializable]
public class SquadJsonPayloadRaw
{
    public string SquadJson;
    public int PieceCount;
    public PieceJsonDataRaw[] Pieces;
    public bool IsWhite;
    public string SenderId;
    public byte[] SquadImageRaw;

    [Serializable]
    public class PieceJsonDataRaw
    {
        public string Name;
        public string MovementJson;
    }
}