using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

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
        string squadJsonPath = Path.Combine(squadFolder, folderName  + ".json");
        File.WriteAllText(squadJsonPath, squadJson);
        Debug.Log($"[Download] Squad JSON salvo: {squadJsonPath}");

        // ─── Imagem do squad ──────────────────────────────────────────────
        if (squad.SquadImageRaw != null)
        {
            string imagePath = Path.Combine(squadFolder, folderName  + ".png");
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

        string text = UIHelperUtils.T("downloaded_successfully");

        if (string.IsNullOrEmpty(text))
            text = "The squad has been downloaded successfully.";

        FileManager.Instance.SpawnMessage(text);

        //Debug.Log($"[MultiplayerLobbyState] Download completo → {squadFolder}");
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

        HostProfileImageRaw = null;
        ClientProfileImageRaw = null;
    }

}