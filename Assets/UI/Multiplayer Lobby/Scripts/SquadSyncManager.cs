using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

/// <summary>
/// Gerencia a troca de esquadrões entre host e client via NGO.
/// JSONs trafegam via ServerRpc/ClientRpc.
/// Sprites trafegam via CustomMessagingManager (sem limite de tamanho).
/// </summary>

/*
// No MultiplayerLobbyUI ou onde for iniciar a partida:
MatchSquadData hostSquad   = SquadSyncManager.Instance.HostSquad;
MatchSquadData clientSquad = SquadSyncManager.Instance.ClientSquad;

// Brancas/Pretas dependem da escolha no lobby — ex:
MultiplayerLobbyState.WhiteSquad = hostSquad;
MultiplayerLobbyState.BlackSquad = clientSquad;
*/
public static class MultiplayerLobbyState
{
    public static MatchSquadData WhiteSquad;
    public static MatchSquadData BlackSquad;
    public static bool LocalIsWhite;
    public static string WhiteSquadOwnerId;  // clientId de quem enviou o White
    public static string BlackSquadOwnerId;  // clientId de quem enviou o Black
    public static Dictionary<string, byte[]> WhiteSpritesRaw = new Dictionary<string, byte[]>();
    public static Dictionary<string, byte[]> BlackSpritesRaw = new Dictionary<string, byte[]>();
    public static byte[] HostProfileImageRaw; 
    public static byte[] ClientProfileImageRaw;
    public static void Log(string context = "")
    {
        string white = WhiteSquad?.Data?.Name ?? "null";
        string black = BlackSquad?.Data?.Name ?? "null";
        string local = LocalIsWhite ? "White" : "Black";

        Debug.Log($"[MultiplayerLobbyState] {context}\n" +
                $"  LocalIsWhite : {LocalIsWhite} ({local})\n" +
                $"  WhiteSquad   : {white}\n" +
                $"  BlackSquad   : {black}\n");
    }


    public static class LobbyConstants
    {
        public const string ClientReady = "clientReady";
    }
    public static bool ClientIsReady
    {
        get
        {
            Lobby lobby = NetworkLobbyManager.Instance?.currentLobby;
            if (lobby == null) return false;

            // Procura o player que NÃO é o host
            foreach (var player in lobby.Players)
            {
                if (player.Id == lobby.HostId) continue;

                if (player.Data != null &&
                    player.Data.TryGetValue(LobbyConstants.ClientReady, out PlayerDataObject data))
                {
                    return data.Value == "true";
                }
            }

            return false;
        }
    }

    public static async void SendReadyStateToHost(bool isReady)
    {
        try
        {
            Lobby lobby = NetworkLobbyManager.Instance?.currentLobby;
            if (lobby == null) return;

            string playerId = AuthenticationService.Instance.PlayerId;

            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {
                        LobbyConstants.ClientReady,
                        new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Member,
                            value: isReady ? "true" : "false"
                        )
                    }
                }
            };

            NetworkLobbyManager.Instance.currentLobby =
                await LobbyService.Instance.UpdatePlayerAsync(lobby.Id, playerId, options);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"[MultiplayerLobbyState] SendReadyStateToHost: {ex}");
        }
    }

    public static void DownloadSquad(bool isWhite)
    {
        MatchSquadData squad = isWhite ? WhiteSquad : BlackSquad;
        Dictionary<string, byte[]> spritesRaw = isWhite ? WhiteSpritesRaw : BlackSpritesRaw;

        if (squad?.Data == null)
        {
            Debug.LogWarning("[MultiplayerLobbyState] Squad nulo, download cancelado.");
            return;
        }

        // ─── Nome da pasta (evita colisão) ────────────────────────────────
        string baseName = squad.Data.Name;
        string folderName = baseName;
        int suffix = 1;

        string squadsRoot = Path.Combine(Application.persistentDataPath, FileManager.Instance.basePath_SquadData);

        while (Directory.Exists(Path.Combine(squadsRoot, folderName)))
            folderName = baseName + suffix++;

        // ─── Caminhos ─────────────────────────────────────────────────────
        string squadFolder = Path.Combine(squadsRoot, folderName);
        string piecesFolder = Path.Combine(Application.persistentDataPath, FileManager.Instance.basePath_PieceData);
        string spritesFolder = Path.Combine(Application.persistentDataPath, FileManager.Instance.basePath_Sprite);

        Directory.CreateDirectory(squadFolder);
        Directory.CreateDirectory(piecesFolder);
        Directory.CreateDirectory(spritesFolder);

        // ─── JSON do squad ────────────────────────────────────────────────
        string squadJson = JsonUtility.ToJson(squad.Data, true);
        string squadJsonPath = Path.Combine(squadFolder, squad.Data.Name + ".json");
        File.WriteAllText(squadJsonPath, squadJson);
        Debug.Log($"[Download] Squad JSON salvo: {squadJsonPath}");

        // ─── Imagem do squad ──────────────────────────────────────────────
        if (squad.SquadImageRaw != null)
        {
            string imagePath = Path.Combine(squadFolder, squad.Data.Name + ".png");
            File.WriteAllBytes(imagePath, squad.SquadImageRaw);
            Debug.Log($"[Download] Squad image salva: {imagePath}");
        }

        // ─── MovementConfigData (peças) ───────────────────────────────────
        foreach (var kv in squad.Pieces)
        {
            string pieceName = kv.Key;
            string pieceJson = JsonUtility.ToJson(kv.Value, true);

            // Pasta por squad dentro de Pieces  ex: Pieces/NomeDoSquad/
            string pieceSquadFolder = Path.Combine(piecesFolder, folderName);
            Directory.CreateDirectory(pieceSquadFolder);

            string piecePath = Path.Combine(pieceSquadFolder, pieceName + ".json");
            //squad.SquadImage
            File.WriteAllText(piecePath, pieceJson);
            Debug.Log($"[Download] Piece salva: {piecePath}");
        }

        // ─── Sprites ──────────────────────────────────────────────────────
        foreach (var kv in spritesRaw)
        {
            string pieceName = kv.Key;
            byte[] pngBytes = kv.Value;

            // Pasta por squad dentro de Sprites  ex: Sprites/NomeDoSquad/
            string spriteSquadFolder = Path.Combine(spritesFolder, folderName);
            Directory.CreateDirectory(spriteSquadFolder);

            string spritePath = Path.Combine(spriteSquadFolder, pieceName + ".png");
            File.WriteAllBytes(spritePath, pngBytes);
            Debug.Log($"[Download] Sprite salva: {spritePath}");
        }

        Debug.Log($"[MultiplayerLobbyState] Download completo → {squadFolder}");
    }

    public static void Reset()
    {
        WhiteSquad = null;
        BlackSquad = null;
        LocalIsWhite = false;
        WhiteSquadOwnerId = null;
        BlackSquadOwnerId = null;

        WhiteSpritesRaw.Clear();
        BlackSpritesRaw.Clear();

        HostProfileImageRaw   = null;
        ClientProfileImageRaw = null;
    }

}


public class SquadSyncManager : NetworkBehaviour
{
    public static SquadSyncManager Instance { get; private set; }

    private const string MSG_PROFILE_HOST_TO_CLIENT = "ProfileImage_H2C";
    private const string MSG_PROFILE_CLIENT_TO_HOST = "ProfileImage_C2H";
    // ─── Eventos ───────────────────────────────────────────────────────────
    public event Action<bool> OnRemoteSquadReady; // true = White, false = Black

    // ─── Controle interno ──────────────────────────────────────────────────
    private FileManager fileManager;

    private Dictionary<string, Sprite> pendingSpritesWhite = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> pendingSpritesBlack = new Dictionary<string, Sprite>();

    private int expectedSpriteCountWhite = 0, expectedSpriteCountBlack = 0;
    private int receivedSpriteCountWhite = 0, receivedSpriteCountBlack = 0;
    private bool jsonReceivedWhite = false, jsonReceivedBlack = false;

    private const string MSG_SPRITE_HOST_TO_CLIENT = "SquadSprite_H2C";
    private const string MSG_SPRITE_CLIENT_TO_HOST = "SquadSprite_C2H";

    // ───────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ───────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        fileManager = FindFirstObjectByType<FileManager>();
    }

    private void ResetSync()
    {
        pendingSpritesWhite.Clear();
        pendingSpritesBlack.Clear();

        expectedSpriteCountWhite = expectedSpriteCountBlack = 0;
        receivedSpriteCountWhite = receivedSpriteCountBlack = 0;
        jsonReceivedWhite = jsonReceivedBlack = false;
    }
    // ───────────────────────────────────────────────────────────────────────
    // API PÚBLICA
    // ───────────────────────────────────────────────────────────────────────

    public void SetLocalSquadAndSync(string rootPath, string folderName, string squadName, string jsonFile)
    {

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
            // Aplica localmente sem notificar UI
            AssignSquadByColor(squad, isWhite, isLocal: true);

            SetIdByColor(NetworkManager.ServerClientId.ToString(), isWhite);

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.ServerClientId) continue;
                ReceiveSquadJsonClientRpc(BuildJsonPayload(isWhite));
                StartCoroutine(SendSpritesToClient(
                    NetworkManager.Singleton.ConnectedClientsIds, isWhite));
            }

            MultiplayerLobbyUI.Instance?.RefreshLocalUI();
        }
        else
        {
            SendSquadJsonToHostServerRpc(BuildJsonPayload(isWhite));
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
            fileManager.basePath_SquadData, folderName, squadName + ".png");

        if (File.Exists(spriteSquadPath))
        {
            squad.SquadImageRaw = File.ReadAllBytes(spriteSquadPath);
            //squad.SquadImage    = UIHelperUtils.GetSpriteFromPath(spriteSquadPath);
        }

        foreach (SquadPieceData piece in data.Pieces)
        {
            // ─── Movement JSON ─────────────────────────────────────────────
            string movPath = Path.Combine(rootPath,
                fileManager.basePath_PieceData, piece.Squad, piece.Name + ".json");

            if (!File.Exists(movPath))
                movPath = Path.Combine(Application.streamingAssetsPath,
                    fileManager.basePath_PieceData, piece.Squad, piece.Name + ".json");

            if (!File.Exists(movPath))
            {
                Debug.LogWarning($"[SquadSync] Movement JSON não encontrado: {movPath}");
                continue;
            }

            squad.Pieces[piece.NameInSquad] =
                JsonUtility.FromJson<MovementConfigData>(File.ReadAllText(movPath));

            //if (piece.NativePiece) piece.SpriteSet = piece.Squad;

            // ─── Sprite ────────────────────────────────────────────────────
            string spritePath = Path.Combine(rootPath,
                fileManager.basePath_Sprite, piece.SpriteSet, piece.Sprite.Trim() + ".png");

            if (!File.Exists(spritePath))
            {
                Debug.LogWarning($"[SquadSync] Sprite não encontrada: {spritePath}");
                continue;
            }

            squad.Sprites[piece.NameInSquad] = UIHelperUtils.GetSpriteFromPath(spritePath);
        }

        if (isWhite) MultiplayerLobbyState.WhiteSquad = squad;
        else MultiplayerLobbyState.BlackSquad = squad;

        //Debug.Log($"[SquadSync] Squad {(isWhite ? "White" : "Black")} definido: {squadName}");
    }

    // ───────────────────────────────────────────────────────────────────────
    // NETWORK SPAWN
    // ───────────────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_SPRITE_CLIENT_TO_HOST, OnReceiveSpriteFromClient);
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_PROFILE_CLIENT_TO_HOST, OnReceiveProfileFromClient); 
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
        else
        {
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_SPRITE_HOST_TO_CLIENT, OnReceiveSpriteFromHost);
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_PROFILE_HOST_TO_CLIENT, OnReceiveProfileFromHost);
        }
    }

    public override void OnNetworkDespawn()
    {
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
        }

        if (IsHost && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 1 — Host detecta client conectado
    // ───────────────────────────────────────────────────────────────────────

    private void SendProfileImage(ulong targetClientId, bool toHost)
    {
        // Texture 'King' is not readabl
        byte[] raw = ProfileImageManager.Instance?.CurrentTexture != null
            ? ProfileImageManager.Instance.CurrentTexture.EncodeToPNG()
            : null;

        if (raw == null || raw.Length == 0)
        {
            Debug.Log("[SquadSync] Sem foto de perfil para enviar.");
            return;
        }

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

    private void OnReceiveProfileFromClient(ulong senderId, FastBufferReader reader)
        => ProcessReceivedProfile(reader);

    private void OnReceiveProfileFromHost(ulong senderId, FastBufferReader reader)
        => ProcessReceivedProfile(reader);

    private void ProcessReceivedProfile(FastBufferReader reader)
    {
        reader.ReadValueSafe(out int length);
        byte[] raw = new byte[length];
        reader.ReadBytesSafe(ref raw, length);

        if (IsHost) MultiplayerLobbyState.ClientProfileImageRaw = raw;
        else         MultiplayerLobbyState.HostProfileImageRaw = raw;

        Debug.Log($"[SquadSync] ProfileImage recebida | {length / 1024f:F1} kb");

        MultiplayerLobbyUI.Instance?.ApplyProfileImages();
    }

    private void OnClientDisconnect(ulong clientId)
    {

        if (IsHost)
        {
            // Um cliente saiu
            if (clientId != NetworkManager.ServerClientId)
            {
                string text = UIHelperUtils.T("lobby_exited");
                if (string.IsNullOrEmpty(text))
                    text = "A player has left the lobby.";

                FileManager.Instance.SpawnMessage(text);
            }

            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_PROFILE_CLIENT_TO_HOST);
        }
        else
        {
            NetworkManager.Singleton.CustomMessagingManager
                .UnregisterNamedMessageHandler(MSG_PROFILE_HOST_TO_CLIENT);

            // Cliente perdeu conexão com o host
            NetworkLobbyManager.Instance.HandleDisconnect();
        }

    }
    
    private void OnClientConnected(ulong clientId)
    {

        if (clientId == NetworkManager.ServerClientId) return;
        else
        {
            string text = UIHelperUtils.T("lobby_entered");

            if (string.IsNullOrEmpty(text))
                text = "A player has joined the lobby.";

            FileManager.Instance.SpawnMessage(text);
        }

        Debug.Log($"[SquadSync] Client {clientId} conectou.");

        // Envia squads já definidos pelo host
        if (MultiplayerLobbyState.WhiteSquad != null)
        {
            ReceiveSquadJsonClientRpc(BuildJsonPayload(true));
            StartCoroutine(SendSpritesToClient(NetworkManager.Singleton.ConnectedClientsIds, true));
        }

        if (MultiplayerLobbyState.BlackSquad != null)
        {
            ReceiveSquadJsonClientRpc(BuildJsonPayload(false));
            StartCoroutine(SendSpritesToClient(NetworkManager.Singleton.ConnectedClientsIds, false));
        }

        //host envia a dele pro client
        StartCoroutine(SendProfileImageDelayed(clientId));
        // Pede o squad do client
        // RequestSquadFromClientRpc(clientId);
    }

    private IEnumerator SendProfileImageDelayed(ulong clientId)
    {
        yield return new WaitForSeconds(1f); // aguarda o client registrar o handler
        SendProfileImage(clientId, toHost: false);
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 2 — Host pede o squad ao client
    // ───────────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RequestSquadFromClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        //client envia a dele pro host
        StartCoroutine(SendProfileImageDelayed_Client());

        bool isWhite = MultiplayerLobbyState.LocalIsWhite;

        MatchSquadData localSquad = isWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        if (localSquad == null) return; // client ainda não definiu squad

        Debug.Log("[SquadSync] Recebi pedido do host. Enviando meu squad...");

        SendSquadJsonToHostServerRpc(BuildJsonPayload(isWhite));

        StartCoroutine(SendSpritesToHost(isWhite));
    }

    private IEnumerator SendProfileImageDelayed_Client()
    {
        yield return new WaitForSeconds(1.5f);
        SendProfileImage(NetworkManager.ServerClientId, toHost: true);
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 3a — Client → Host (ServerRpc)
    // ───────────────────────────────────────────────────────────────────────

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendSquadJsonToHostServerRpc(SquadJsonPayload payload)
    {
        bool isWhite = payload.IsWhite;

        MatchSquadData rebuilt = RebuildSquadFromJson(payload);
        SetIdByColor(payload.SenderId.ToString(), isWhite);
        AssignSquadByColor(rebuilt, isWhite, isLocal: false);
        SetExpectedByColor(payload.PieceCount, isWhite);
        SetJsonReceivedByColor(true, isWhite);

        // Sempre reenvia o squad recebido de volta para o client confirmar
        ReceiveSquadJsonClientRpc(BuildJsonPayload(isWhite, payload.SenderId.ToString()));
        StartCoroutine(SendSpritesToClient(NetworkManager.Singleton.ConnectedClientsIds, isWhite));
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 3b — Host → Client (ClientRpc)
    // ───────────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void ReceiveSquadJsonClientRpc(SquadJsonPayload payload)
    {
        if (IsHost) return;

        bool isWhite = payload.IsWhite;

        Debug.Log($"[SquadSync] Client recebeu JSON do host. Cor: {(isWhite ? "White" : "Black")} | {payload.PieceCount} peças.");

        MatchSquadData rebuilt = RebuildSquadFromJson(payload);

        SetIdByColor(payload.SenderId.ToString(), isWhite);
        AssignSquadByColor(rebuilt, isWhite, isLocal: false);
        SetExpectedByColor(payload.PieceCount, isWhite);
        SetJsonReceivedByColor(true, isWhite);
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 4 — Envio de sprites
    // ───────────────────────────────────────────────────────────────────────

    private IEnumerator SendSpritesToClient(IReadOnlyList<ulong> clientIds, bool senderIsWhite)
    {
        MatchSquadData squad = senderIsWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        if (squad == null) yield break;

        foreach (SquadPieceData piece in squad.Data.Pieces)
        {
            byte[] pngBytes = LoadPngBytes(piece);
            if (pngBytes == null) { Debug.LogWarning($"[SquadSync] Sprite não encontrada: {piece.Name}"); continue; }

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

        foreach (SquadPieceData piece in squad.Data.Pieces)
        {
            byte[] pngBytes = LoadPngBytes(piece);
            if (pngBytes == null)
            {
                Debug.LogWarning($"[SquadSync] Sprite não encontrada: {piece.Name}"); continue;
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

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 5 — Recebimento de sprites
    // ───────────────────────────────────────────────────────────────────────

    private void OnReceiveSpriteFromClient(ulong senderId, FastBufferReader reader)
        => ProcessReceivedSprite(reader);

    private void OnReceiveSpriteFromHost(ulong senderId, FastBufferReader reader)
        => ProcessReceivedSprite(reader);

    private void ProcessReceivedSprite(FastBufferReader reader)
    {
        reader.ReadValueSafe(out string pieceName);
        reader.ReadValueSafe(out int length);

        byte[] pngBytes = new byte[length];
        reader.ReadBytesSafe(ref pngBytes, length);
        reader.ReadValueSafe(out bool senderIsWhite);

        // logo após reader.ReadValueSafe(out bool senderIsWhite);
        if (senderIsWhite) MultiplayerLobbyState.WhiteSpritesRaw[pieceName] = pngBytes;
        else MultiplayerLobbyState.BlackSpritesRaw[pieceName] = pngBytes;

        var tex = new Texture2D(408, 408, TextureFormat.RGBA32, false);
        tex.LoadImage(pngBytes);

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);

        MatchSquadData targetSquad = senderIsWhite ? MultiplayerLobbyState.WhiteSquad : MultiplayerLobbyState.BlackSquad;
        Dictionary<string, Sprite> pending = senderIsWhite
            ? pendingSpritesWhite
            : pendingSpritesBlack;

        if (targetSquad != null)
            targetSquad.Sprites[pieceName] = sprite;
        else
            pending[pieceName] = sprite;

        if (senderIsWhite) receivedSpriteCountWhite++;
        else receivedSpriteCountBlack++;

        //Debug.Log($"[SquadSync] Sprite recebida ({(senderIsWhite ? "White" : "Black")}): {pieceName}");

        if (targetSquad != null)
            CheckIfComplete(senderIsWhite);
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 6 — Verificação de conclusão
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

    private SquadJsonPayload BuildJsonPayload(bool isWhite, string senderId = null)
    {
        MatchSquadData squad = isWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        var pieceJsons = new List<PieceJsonData>();

        foreach (SquadPieceData piece in squad.Data.Pieces)
        {
            string movPath = Path.Combine(Application.persistentDataPath,
                fileManager.basePath_PieceData, piece.Squad, piece.Name + ".json");

            if (!File.Exists(movPath))
            {
                Debug.LogWarning($"[SquadSync] Movement JSON não encontrado: {movPath}");
                continue;
            }

            pieceJsons.Add(new PieceJsonData
            {
                Name = piece.NameInSquad,
                MovementJson = File.ReadAllText(movPath)
            });
        }

        return new SquadJsonPayload
        {
            SquadJson     = JsonUtility.ToJson(squad.Data),
            PieceCount    = pieceJsons.Count,
            Pieces        = pieceJsons.ToArray(),
            IsWhite       = isWhite,
            SenderId      = senderId ?? NetworkManager.Singleton.LocalClientId.ToString(),
            SquadImageRaw = squad.SquadImageRaw
        };
    }

    private MatchSquadData RebuildSquadFromJson(SquadJsonPayload payload)
    {
        var squadData = new MatchSquadData
        {
            Data = JsonUtility.FromJson<Squad>(payload.SquadJson.Value)
        };

        foreach (PieceJsonData pieceJson in payload.Pieces)
            squadData.Pieces[pieceJson.Name.Value] =
                JsonUtility.FromJson<MovementConfigData>(pieceJson.MovementJson.Value);

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
            fileManager.basePath_Sprite, piece.Squad, piece.Sprite.Trim() + ".png");

        if (!File.Exists(path))
            path = Path.Combine(Application.streamingAssetsPath,
                fileManager.basePath_Sprite, piece.Squad, piece.Sprite.Trim() + ".png");

        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}

// ─── Structs de payload ────────────────────────────────────────────────────

[Serializable]
public struct PieceJsonData : INetworkSerializable
{
    public FixedString128Bytes Name;
    public FixedString4096Bytes MovementJson;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref MovementJson);
    }
}

[Serializable]
public struct SquadJsonPayload : INetworkSerializable
{
    public FixedString4096Bytes SquadJson;
    public int PieceCount;
    public PieceJsonData[] Pieces;
    public bool IsWhite;
    public FixedString64Bytes SenderId;
    public byte[] SquadImageRaw;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SquadJson);
        serializer.SerializeValue(ref PieceCount);
        serializer.SerializeValue(ref IsWhite);
        serializer.SerializeValue(ref SenderId);

        // SquadImageRaw
        int imageLen = SquadImageRaw?.Length ?? 0;
        serializer.SerializeValue(ref imageLen);
        if (serializer.IsReader) SquadImageRaw = imageLen > 0 ? new byte[imageLen] : null;
        for (int i = 0; i < imageLen; i++)
            serializer.SerializeValue(ref SquadImageRaw[i]);

        if (serializer.IsReader)
            Pieces = new PieceJsonData[PieceCount];

        for (int i = 0; i < PieceCount; i++)
            serializer.SerializeValue(ref Pieces[i]);
    }




}