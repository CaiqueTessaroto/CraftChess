using Unity.Netcode;
using UnityEngine;

public class PieceControllerNetwork : NetworkBehaviour
{
    public static PieceControllerNetwork Instance { get; private set; }

    private MultiplayerPieceController mp;
    private PieceController pc;

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
    private void ConfirmMoveClientRpc(int ox, int oy, int tx, int ty,
        ClientRpcParams clientRpcParams = default)
    {
        if (mp == null)
            mp = FindFirstObjectByType<MultiplayerPieceController>();

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
        if (pc == null)
            pc = FindFirstObjectByType<MultiplayerPieceController>();

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
    private void ConfirmPromotionClientRpc(int ox, int oy, int tx, int ty,
        string pieceName, int playerId,
        ClientRpcParams clientRpcParams = default)
    {
        if (mp == null)
            mp = FindFirstObjectByType<MultiplayerPieceController>();

        mp.ExecuteConfirmedPromotion(
            new Vector2Int(ox, oy), new Vector2Int(tx, ty), pieceName, playerId
        );
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

        if (pc == null)
            pc = FindFirstObjectByType<MultiplayerPieceController>();

        pc.SetEndGame(black: blackWins, white: whiteWins, draw: draw);
    }






    public void SendCastle(int kingOx, int kingOy, int kingTx, int kingTy,
                            int rookOx, int rookOy, int rookTx, int rookTy)
    {
        SendCastleServerRpc(kingOx, kingOy, kingTx, kingTy,
                            rookOx, rookOy, rookTx, rookTy);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendCastleServerRpc(int kingOx, int kingOy, int kingTx, int kingTy,
                                      int rookOx, int rookOy, int rookTx, int rookTy)
    {
        ConfirmCastleClientRpc(kingOx, kingOy, kingTx, kingTy,
                               rookOx, rookOy, rookTx, rookTy);
    }

    [ClientRpc]
    private void ConfirmCastleClientRpc(int kingOx, int kingOy, int kingTx, int kingTy,
                                         int rookOx, int rookOy, int rookTx, int rookTy,
                                         ClientRpcParams clientRpcParams = default)
    {
        if (mp == null)
            mp = FindFirstObjectByType<MultiplayerPieceController>();

        mp.ExecuteConfirmedCastle(
            new Vector2Int(kingOx, kingOy), new Vector2Int(kingTx, kingTy),
            new Vector2Int(rookOx, rookOy), new Vector2Int(rookTx, rookTy)
        );
    }





    [ClientRpc]
    public void TurnHeartbeatClientRpc(int hostTurn)
    {
        // O host recebe o próprio ClientRpc mas ignora
        if (NetworkManager.Singleton.IsHost) return;

        if (mp == null)
            mp = FindFirstObjectByType<MultiplayerPieceController>();

        int localTurn = mp.GetCurrentTurn();

        if (localTurn != hostTurn)
        {
            Debug.LogWarning($"[Heartbeat] Dessincronização detectada! Local: {localTurn} | Host: {hostTurn}. Solicitando reenvio...");
            RequestResyncServerRpc();
        }

    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestResyncServerRpc(ServerRpcParams rpcParams = default)
    {
        if (mp == null)
            mp = FindFirstObjectByType<MultiplayerPieceController>();

        // ✅ Bug 1 corrigido
        if (mp.lastMoveType == MultiplayerPieceController.LastMoveType.None)
        {
            Debug.LogWarning("[Heartbeat] Resync solicitado mas nenhum lance foi registrado.");
            return;
        }

        Debug.Log($"[Heartbeat] Reenviando último lance ({mp.lastMoveType}) para client desincronizado.");

        ClientRpcParams targetClient = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
            }
        };

        // ✅ Bug 2 corrigido — vai direto ao ClientRpc targeted, sem passar pelo ServerRpc de novo
        switch (mp.lastMoveType)
        {
            case MultiplayerPieceController.LastMoveType.Move:
                ConfirmMoveClientRpc(
                    mp.lastMoveOrigin.x, mp.lastMoveOrigin.y,
                    mp.lastMoveTarget.x, mp.lastMoveTarget.y,
                    targetClient
                );
                break;

            case MultiplayerPieceController.LastMoveType.Castle:
                ConfirmCastleClientRpc(
                    mp.lastCastleKingOrigin.x, mp.lastCastleKingOrigin.y,
                    mp.lastCastleKingTarget.x, mp.lastCastleKingTarget.y,
                    mp.lastCastleRookOrigin.x, mp.lastCastleRookOrigin.y,
                    mp.lastCastleRookTarget.x, mp.lastCastleRookTarget.y,
                    targetClient
                );
                break;

            case MultiplayerPieceController.LastMoveType.Promotion:
                ConfirmPromotionClientRpc(
                    mp.lastPromotionOrigin.x, mp.lastPromotionOrigin.y,
                    mp.lastPromotionTarget.x, mp.lastPromotionTarget.y,
                    mp.lastPromotionPieceName,
                    mp.lastPromotionPlayerId,
                    targetClient
                );
                break;
        }
    }

}