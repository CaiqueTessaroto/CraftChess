using Unity.Netcode;
using UnityEngine;

public class PieceControllerNetwork : NetworkBehaviour
{
    public static PieceControllerNetwork Instance { get; private set; }

    private MultiplayerPieceController mp;
    private PieceController pc;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ─── Move normal ──────────────────────────────────────────────────────

    public void SendMove(int ox, int oy, int tx, int ty)
    {
        Debug.Log($"[Network] Enviando movimento: ({ox},{oy}) → ({tx},{ty})");
        SendMoveServerRpc(ox, oy, tx, ty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMoveServerRpc(int ox, int oy, int tx, int ty,
        ServerRpcParams rpcParams = default)
    {
        // 1. Avisa TODOS que há um movimento pendente
        AcknowledgeMoveClientRpc(ox, oy, tx, ty);

        // 2. Confirma execução para todos
        ConfirmMoveClientRpc(ox, oy, tx, ty);
    }

    // Fase 1 — salva o movimento pendente em todos os clientes
    [ClientRpc]
    private void AcknowledgeMoveClientRpc(int ox, int oy, int tx, int ty)
    {
        EnsureMP();
        mp.SetPendingNetworkMove(
            new Vector2Int(ox, oy),
            new Vector2Int(tx, ty),
            MultiplayerPieceController.PendingMoveType.Move
        );
    }

    // Fase 2 — executa o movimento salvo (se ainda for o mesmo)
    [ClientRpc]
    private void ConfirmMoveClientRpc(int ox, int oy, int tx, int ty,
        ClientRpcParams clientRpcParams = default)
    {
        EnsureMP();
        mp.ExecuteConfirmedMove(new Vector2Int(ox, oy), new Vector2Int(tx, ty));
    }

    // ─── Castling ─────────────────────────────────────────────────────────

    public void SendCastle(int kox, int koy, int ktx, int kty,
                           int rox, int roy, int rtx, int rty)
    {
        SendCastleServerRpc(kox, koy, ktx, kty, rox, roy, rtx, rty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendCastleServerRpc(int kox, int koy, int ktx, int kty,
                                     int rox, int roy, int rtx, int rty)
    {
        AcknowledgeCastleClientRpc(kox, koy, ktx, kty, rox, roy, rtx, rty);
        ConfirmCastleClientRpc(kox, koy, ktx, kty, rox, roy, rtx, rty);
    }

    [ClientRpc]
    private void AcknowledgeCastleClientRpc(int kox, int koy, int ktx, int kty,
                                            int rox, int roy, int rtx, int rty)
    {
        EnsureMP();
        mp.SetPendingNetworkCastle(
            new Vector2Int(kox, koy), new Vector2Int(ktx, kty),
            new Vector2Int(rox, roy), new Vector2Int(rtx, rty)
        );
    }

    [ClientRpc]
    private void ConfirmCastleClientRpc(int kox, int koy, int ktx, int kty,
                                        int rox, int roy, int rtx, int rty,
                                        ClientRpcParams p = default)
    {
        EnsureMP();
        mp.ExecuteConfirmedCastle(
            new Vector2Int(kox, koy), new Vector2Int(ktx, kty),
            new Vector2Int(rox, roy), new Vector2Int(rtx, rty)
        );
    }

    // ─── Promoção ─────────────────────────────────────────────────────────

    public void SendPromotion(int ox, int oy, int tx, int ty,
                              string pieceName, int playerId)
    {
        SendPromotionServerRpc(ox, oy, tx, ty, pieceName, playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPromotionServerRpc(int ox, int oy, int tx, int ty,
                                        string pieceName, int playerId)
    {
        AcknowledgePromotionClientRpc(ox, oy, tx, ty, pieceName, playerId);
        ConfirmPromotionClientRpc(ox, oy, tx, ty, pieceName, playerId);
    }

    [ClientRpc]
    private void AcknowledgePromotionClientRpc(int ox, int oy, int tx, int ty,
                                               string pieceName, int playerId)
    {
        EnsureMP();
        mp.SetPendingNetworkPromotion(
            new Vector2Int(ox, oy), new Vector2Int(tx, ty),
            pieceName, playerId
        );
    }

    [ClientRpc]
    private void ConfirmPromotionClientRpc(int ox, int oy, int tx, int ty,
                                           string pieceName, int playerId,
                                           ClientRpcParams p = default)
    {
        EnsureMP();
        mp.ExecuteConfirmedPromotion(
            new Vector2Int(ox, oy), new Vector2Int(tx, ty),
            pieceName, playerId
        );
    }

    // ─── Give Up ──────────────────────────────────────────────────────────

    public void SendGiveUp() => GiveUpServerRpc();

    [ServerRpc(RequireOwnership = false)]
    private void GiveUpServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        bool senderIsHost = senderId == NetworkManager.ServerClientId;
        bool hostIsWhite = MatchData.Instance.HostIsWhite;

        bool blackWins = senderIsHost ? hostIsWhite : !hostIsWhite;
        bool whiteWins = !blackWins;

        GiveUpClientRpc(blackWins, whiteWins);
    }

    [ClientRpc]
    private void GiveUpClientRpc(bool blackWins, bool whiteWins)
    {
        EnsurePC();
        pc.SetEndGame(black: blackWins, white: whiteWins, draw: false);
    }

    // ─── End Game ─────────────────────────────────────────────────────────

    public void SendEndGame(bool blackWins, bool whiteWins, bool draw)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        EndGameClientRpc(blackWins, whiteWins, draw);
    }

    [ClientRpc]
    private void EndGameClientRpc(bool blackWins, bool whiteWins, bool draw)
    {
        EnsurePC();
        pc.SetEndGame(black: blackWins, white: whiteWins, draw: draw);
    }

    // ─── Heartbeat / Resync ───────────────────────────────────────────────

    [ClientRpc]
    public void TurnHeartbeatClientRpc(int hostTurn)
    {
        if (NetworkManager.Singleton.IsHost) return;

        EnsureMP();
        if (mp.GetCurrentTurn() != hostTurn)
        {
            Debug.LogWarning($"[Heartbeat] Dessync! Local: {mp.GetCurrentTurn()} | Host: {hostTurn}");
            RequestResyncServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestResyncServerRpc(ServerRpcParams rpcParams = default)
    {
        EnsureMP();

        if (mp.lastMoveType == MultiplayerPieceController.LastMoveType.None)
        {
            Debug.LogWarning("[Heartbeat] Resync solicitado mas nenhum lance registrado.");
            return;
        }

        ClientRpcParams target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
            }
        };

        switch (mp.lastMoveType)
        {
            case MultiplayerPieceController.LastMoveType.Move:
                ConfirmMoveClientRpc(
                    mp.lastMoveOrigin.x, mp.lastMoveOrigin.y,
                    mp.lastMoveTarget.x, mp.lastMoveTarget.y,
                    target);
                break;

            case MultiplayerPieceController.LastMoveType.Castle:
                ConfirmCastleClientRpc(
                    mp.lastCastleKingOrigin.x, mp.lastCastleKingOrigin.y,
                    mp.lastCastleKingTarget.x, mp.lastCastleKingTarget.y,
                    mp.lastCastleRookOrigin.x, mp.lastCastleRookOrigin.y,
                    mp.lastCastleRookTarget.x, mp.lastCastleRookTarget.y,
                    target);
                break;

            case MultiplayerPieceController.LastMoveType.Promotion:
                ConfirmPromotionClientRpc(
                    mp.lastPromotionOrigin.x, mp.lastPromotionOrigin.y,
                    mp.lastPromotionTarget.x, mp.lastPromotionTarget.y,
                    mp.lastPromotionPieceName,
                    mp.lastPromotionPlayerId,
                    target);
                break;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private void EnsureMP()
    {
        if (mp == null) mp = FindFirstObjectByType<MultiplayerPieceController>();
    }

    private void EnsurePC()
    {
        if (pc == null) pc = FindFirstObjectByType<MultiplayerPieceController>();
    }
}