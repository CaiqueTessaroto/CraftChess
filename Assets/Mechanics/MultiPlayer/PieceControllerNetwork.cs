using Unity.Netcode;
using UnityEngine;

public class PieceControllerNetwork : NetworkBehaviour
{
    public static PieceControllerNetwork Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Chamado por qualquer jogador (Host ou Client)
    public void SendMove(int ox, int oy, int tx, int ty)
    {
        Debug.Log($"[Network] Enviando movimento para o servidor: ({ox},{oy}) → ({tx},{ty})");
        SendMoveServerRpc(ox, oy, tx, ty);
    }

    // Sempre chega no Host primeiro
    [ServerRpc(RequireOwnership = false)]
    private void SendMoveServerRpc(int ox, int oy, int tx, int ty)
    {
        Debug.Log($"[Network][Server] Movimento recebido e validado: ({ox},{oy}) → ({tx},{ty}) | Confirmando para todos...");

        // Host confirma e rebroadcast para todos (inclusive quem enviou)
        ConfirmMoveClientRpc(ox, oy, tx, ty);
    }

    // Todos recebem — cada um decide se executa ou ignora
    [ClientRpc]
    private void ConfirmMoveClientRpc(int ox, int oy, int tx, int ty)
    {
        Debug.Log($"[Network][Client] Movimento confirmado pelo servidor: ({ox},{oy}) → ({tx},{ty})");

        MultiplayerPieceController mp = FindFirstObjectByType<MultiplayerPieceController>();

        if (mp == null)
        {
            Debug.LogError("[Network][Client] MultiplayerPieceController não encontrado!");
            return;
        }

        mp.ExecuteConfirmedMove(new Vector2Int(ox, oy), new Vector2Int(tx, ty));
    }

    public void SendGiveUp()
    {
        GiveUpServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void GiveUpServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        bool senderIsHost = senderId == NetworkManager.ServerClientId;

        // Quem se rendeu perde — determina cores
        bool hostIsWhite = MatchData.Instance.HostIsWhite;

        bool blackWins, whiteWins;

        if (senderIsHost)
        {
            // Host se rendeu
            blackWins = hostIsWhite;   // se host é branco, preto vence
            whiteWins = !hostIsWhite;
        }
        else
        {
            // Client se rendeu
            blackWins = !hostIsWhite;  // se host é branco, client é preto → branco vence
            whiteWins = hostIsWhite;
        }

        GiveUpClientRpc(blackWins, whiteWins);
    }

    [ClientRpc]
    private void GiveUpClientRpc(bool blackWins, bool whiteWins)
    {
        PieceController pc = FindFirstObjectByType<MultiplayerPieceController>();
        if (pc == null) return;

        pc.SetEndGame(black: blackWins, white: whiteWins, draw: false);
    }


    public void SendPromotion(int ox, int oy, int tx, int ty, string pieceName, int playerId)
    {
        SendPromotionServerRpc(ox, oy, tx, ty, pieceName, playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPromotionServerRpc(int ox, int oy, int tx, int ty, string pieceName, int playerId)
    {
        ConfirmPromotionClientRpc(ox, oy, tx, ty, pieceName, playerId);
    }

    [ClientRpc]
    private void ConfirmPromotionClientRpc(int ox, int oy, int tx, int ty, string pieceName, int playerId)
    {
        MultiplayerPieceController mp = FindFirstObjectByType<MultiplayerPieceController>();
        if (mp == null) return;

        mp.ExecuteConfirmedPromotion(new Vector2Int(ox, oy), new Vector2Int(tx, ty), pieceName, playerId);
    }




    public void SendEndGame(bool blackWins, bool whiteWins, bool draw)
    {
        // Só o host deve chamar isso
        if (!NetworkManager.Singleton.IsHost) return;
        EndGameClientRpc(blackWins, whiteWins, draw);
    }

    [ClientRpc]
    private void EndGameClientRpc(bool blackWins, bool whiteWins, bool draw)
    {
        PieceController pc = FindFirstObjectByType<MultiplayerPieceController>();
        if (pc == null) return;

        pc.SetEndGame(black: blackWins, white: whiteWins, draw: draw);
    }
}