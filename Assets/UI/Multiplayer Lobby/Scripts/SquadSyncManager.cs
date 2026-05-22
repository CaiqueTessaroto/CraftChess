using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

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
    public static string LocalSquadName;

    public static MatchSquadData LocalSquad =>
        LocalIsWhite ? WhiteSquad : BlackSquad;

    public static MatchSquadData OpponentSquad =>
        LocalIsWhite ? BlackSquad : WhiteSquad;

    public static void Log(string context = "")
    {
        string white = WhiteSquad?.Data?.Name ?? "null";
        string black = BlackSquad?.Data?.Name ?? "null";
        string local = LocalIsWhite ? "White" : "Black";

        Debug.Log($"[MultiplayerLobbyState] {context}\n" +
                $"  LocalIsWhite : {LocalIsWhite} ({local})\n" +
                $"  WhiteSquad   : {white}\n" +
                $"  BlackSquad   : {black}\n" +
                $"  LocalSquad   : {LocalSquad?.Data?.Name ?? "null"}\n" +
                $"  OpponentSquad: {OpponentSquad?.Data?.Name ?? "null"}");
    }

}


public class SquadSyncManager : NetworkBehaviour
{
    public static SquadSyncManager Instance { get; private set; }

    // ─── Eventos ───────────────────────────────────────────────────────────
    public event Action<bool> OnRemoteSquadReady; // true = White, false = Black

    // ─── Controle interno ──────────────────────────────────────────────────
    private FileManager fileManager;

    private Dictionary<string, Sprite> pendingSpritesWhite = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> pendingSpritesBlack = new Dictionary<string, Sprite>();

    private int  expectedSpriteCountWhite = 0, expectedSpriteCountBlack = 0;
    private int  receivedSpriteCountWhite = 0, receivedSpriteCountBlack = 0;
    private bool jsonReceivedWhite = false,     jsonReceivedBlack = false;

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
        jsonReceivedWhite        = jsonReceivedBlack        = false;

        //WhiteSquad = null;
        //BlackSquad = null;

        //WhiteSquad = MultiplayerLobbyState.WhiteSquad;
        //BlackSquad = MultiplayerLobbyState.BlackSquad;

        //Debug.Log("[SquadSync] Estado de sync resetado.");
    }

    // ───────────────────────────────────────────────────────────────────────
    // API PÚBLICA
    // ───────────────────────────────────────────────────────────────────────

    public void SetLocalSquadAndSync(string rootPath, string folderName,
                                     string squadName, string jsonFile, bool isWhite)
    {
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

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.ServerClientId) continue;
                ReceiveSquadJsonClientRpc(BuildJsonPayload(isWhite));
                StartCoroutine(SendSpritesToClient(
                    NetworkManager.Singleton.ConnectedClientsIds, isWhite));
            }
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
        var squad  = new MatchSquadData { Data = data };

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

            if (piece.NativePiece) piece.SpriteSet = piece.Squad;

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
        else         MultiplayerLobbyState.BlackSquad  = squad;

        Debug.Log($"[SquadSync] Squad {(isWhite ? "White" : "Black")} definido: {squadName}");
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
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            NetworkManager.Singleton.CustomMessagingManager
                .RegisterNamedMessageHandler(MSG_SPRITE_HOST_TO_CLIENT, OnReceiveSpriteFromHost);
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
        }

        if (IsHost && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 1 — Host detecta client conectado
    // ───────────────────────────────────────────────────────────────────────

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId) return;
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

        // Pede o squad do client
        RequestSquadFromClientRpc(clientId);
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 2 — Host pede o squad ao client
    // ───────────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RequestSquadFromClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        bool isWhite = MultiplayerLobbyState.LocalIsWhite;

        MatchSquadData localSquad = isWhite
            ? MultiplayerLobbyState.WhiteSquad
            : MultiplayerLobbyState.BlackSquad;

        if (localSquad == null) return; // client ainda não definiu squad

        Debug.Log("[SquadSync] Recebi pedido do host. Enviando meu squad...");

        SendSquadJsonToHostServerRpc(BuildJsonPayload(isWhite));
        StartCoroutine(SendSpritesToHost(isWhite));
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 3a — Client → Host (ServerRpc)
    // ───────────────────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void SendSquadJsonToHostServerRpc(SquadJsonPayload payload)
    {
        bool isWhite = payload.IsWhite;

        MatchSquadData rebuilt = RebuildSquadFromJson(payload);
        AssignSquadByColor(rebuilt, isWhite, isLocal: false);
        SetExpectedByColor(payload.PieceCount, isWhite);
        SetJsonReceivedByColor(true, isWhite);

        // Sempre reenvia o squad recebido de volta para o client confirmar
        ReceiveSquadJsonClientRpc(BuildJsonPayload(isWhite));
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

            int nameLen    = System.Text.Encoding.UTF8.GetByteCount(piece.NameInSquad);
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
        MultiplayerLobbyUI.Instance?.RefreshLocalUI();
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
            if (pngBytes == null) { 
                Debug.LogWarning($"[SquadSync] Sprite não encontrada: {piece.Name}"); continue; 
                }

            int nameLen    = System.Text.Encoding.UTF8.GetByteCount(piece.NameInSquad);
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
        else               receivedSpriteCountBlack++;

        //Debug.Log($"[SquadSync] Sprite recebida ({(senderIsWhite ? "White" : "Black")}): {pieceName}");

        if (targetSquad != null)
            CheckIfComplete(senderIsWhite);
    }

    // ───────────────────────────────────────────────────────────────────────
    // PASSO 6 — Verificação de conclusão
    // ───────────────────────────────────────────────────────────────────────

    private void CheckIfComplete(bool isWhite)
    {
        MatchSquadData targetSquad  = isWhite ? MultiplayerLobbyState.WhiteSquad : MultiplayerLobbyState.BlackSquad;
        Dictionary<string, Sprite> pending = isWhite ? pendingSpritesWhite : pendingSpritesBlack;
        int  expected  = isWhite ? expectedSpriteCountWhite : expectedSpriteCountBlack;
        int  received  = isWhite ? receivedSpriteCountWhite  : receivedSpriteCountBlack;
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

    private void AssignSquadByColor(MatchSquadData squad, bool isWhite, bool isLocal = false)
    {
        if (isWhite) MultiplayerLobbyState.WhiteSquad = squad;
        else         MultiplayerLobbyState.BlackSquad = squad;

        if (!isLocal)
            CheckIfComplete(isWhite);
    }

    private void SetExpectedByColor(int count, bool isWhite)
    {
        if (isWhite) expectedSpriteCountWhite = count;
        else         expectedSpriteCountBlack = count;
    }

    private void SetJsonReceivedByColor(bool value, bool isWhite)
    {
        if (isWhite) jsonReceivedWhite = value;
        else         jsonReceivedBlack = value;
    }

    private SquadJsonPayload BuildJsonPayload(bool isWhite)
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
                Name         = piece.NameInSquad,
                MovementJson = File.ReadAllText(movPath)
            });
        }

        return new SquadJsonPayload
        {
            SquadJson  = JsonUtility.ToJson(squad.Data),
            PieceCount = pieceJsons.Count,
            Pieces     = pieceJsons.ToArray(),
            IsWhite    = isWhite
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
    public FixedString128Bytes  Name;
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
    public int                  PieceCount;
    public PieceJsonData[]      Pieces;
    public bool                 IsWhite;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SquadJson);
        serializer.SerializeValue(ref PieceCount);
        serializer.SerializeValue(ref IsWhite);

        if (serializer.IsReader)
            Pieces = new PieceJsonData[PieceCount];

        for (int i = 0; i < PieceCount; i++)
            serializer.SerializeValue(ref Pieces[i]);
    }




}