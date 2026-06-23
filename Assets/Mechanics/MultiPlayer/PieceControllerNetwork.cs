using Unity.Netcode;
using UnityEngine;
using WebSocketSharp;

public class PieceControllerNetwork : NetworkBehaviour
{
    public static PieceControllerNetwork Instance { get; private set; }

    private MultiplayerPieceController mp;
    private PieceController pc;

    public bool simulatePacketLoss = false;
    private const float STALL_TIMEOUT = 10f; // segundos sem movimento

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            authorativeTurnPlayer.Value = 0;
        }
    }

    public void ResetTurnPlayer()
    {
        if (!IsHost) return;

        authorativeTurnPlayer.Value = 0;
    }

    private void Update()
    {
        //if (!IsHost) return;

        EnsureMP();
        if (mp == null || mp.endGame) return;

        mp.turnStallTimer += Time.deltaTime;

        if (mp.turnStallTimer >= STALL_TIMEOUT)
        {
            mp.turnStallTimer = 0f;
            RequestResyncFromCurrentPlayer();
        }
    }

    private void RequestResyncFromCurrentPlayer()
    {
        int expectedTurn = mp.moveTracker.GetTurnPlayer();
        int expectedTurnNumber = mp.moveTracker.GetTurnNumber();

        if (IsHost)
        {
            bool isClientTurn = mp.GetLocalPlayerId() != expectedTurn;
            if (!isClientTurn) return;

            if (string.IsNullOrEmpty(MultiplayerLobbyState.PlayerClientId)) return;

            if (!ulong.TryParse(MultiplayerLobbyState.PlayerClientId, out ulong clientId)) return;

            ReportHostTurnClientRpc(expectedTurnNumber,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { clientId }
                    }
                });
        }
        else
        {
            bool isHostTurn = mp.GetLocalPlayerId() != expectedTurn;
            if (!isHostTurn) return;

            ReportClientTurnServerRpc(expectedTurnNumber);
        }

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

        ResetStallTimer();

        if (simulatePacketLoss)
        {
            Debug.Log("[TEST] Pacote descartado intencionalmente.");
            ConfirmMoveClientRpc(ox, oy, tx, ty);
            return; // simula perda
        }

        // 1. Avisa TODOS que há um movimento pendente
        AcknowledgeMoveClientRpc(ox, oy, tx, ty);

        // 2. Confirma execução para todos
        ConfirmMoveClientRpc(ox, oy, tx, ty);
    }

    // chamado pelo host para resetar
    public void ResetStallTimer()
    {
        EnsureMP();
        mp.turnStallTimer = 0f;
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
        ResetStallTimer();
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
        ResetStallTimer();
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

        if (senderId != NetworkManager.ServerClientId && senderId.ToString() != MultiplayerLobbyState.PlayerClientId)
            return;

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

    public int diff = 0;
    private bool player2Synced = true;
    private bool spectatorSynced = true;

    [ServerRpc(RequireOwnership = false)]
    public void ReportClientTurnServerRpc(int clientTurn, ServerRpcParams p = default)
    {
        EnsureMP();

        ulong senderId = p.Receive.SenderClientId;
        int hostTurn = mp.moveTracker.GetTurnNumber();

        diff = hostTurn - clientTurn;

        bool synced = diff <= 0;

        if (!MultiplayerLobbyState.SpectatorClientId.IsNullOrEmpty())
            if (senderId.ToString() == MultiplayerLobbyState.SpectatorClientId)
            {
                spectatorSynced = synced;
                Debug.Log($"Spectator synced = {synced} diff={diff}");
            }
            else
            {
                player2Synced = synced;
                Debug.Log($"Player synced = {synced} diff={diff}");
            }

        if (synced) return;

        if (diff == 1)
            mp.ResendLastMove();
        else if (diff > 1)
            Debug.LogError($"Gap de {diff} — host envia board state completo");

    }

    [ClientRpc]
    public void ReportHostTurnClientRpc(int hostTurn, ClientRpcParams p = default)
    {
        EnsureMP();
        int localTurn = mp.moveTracker.GetTurnNumber();
        diff = localTurn - hostTurn;

        if (diff <= 0) return;

        if (diff == 1)
        {
            Debug.LogWarning($"[Resync] Host no turno {hostTurn}, cliente no turno {localTurn}, diff={diff}");
            mp.ResendLastMove();
        }
        else
        {
            Debug.LogError($"Gap de {diff} turnos — envia board state completo");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanPlayServerRpc(ServerRpcParams p = default)
    {
        bool canPlay = true;

        if (!player2Synced)
            canPlay = false;

        if (!MultiplayerLobbyState.SpectatorClientId.IsNullOrEmpty())
            if (!spectatorSynced)
                canPlay = false;

        ClientRpcParams target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { p.Receive.SenderClientId }
            }
        };

        CanPlayClientRpc(canPlay, target);
    }

    public bool CanPlayResponse = true;

    [ClientRpc]
    private void CanPlayClientRpc(bool canPlay, ClientRpcParams p = default)
    {
        CanPlayResponse = canPlay;
    }

    // NetworkVariable — sincroniza automaticamente para todos
    private NetworkVariable<int> authorativeTurnPlayer = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public void UpdateTurnPlayer()
    {
        if (!IsHost) return;

        EnsureMP();

        if (mp == null || mp.moveTracker == null) return;

        authorativeTurnPlayer.Value = mp.moveTracker.GetTurnPlayer();
    }

    public void ReportTurnAfterMove()
    {
        int currentTurn = mp.moveTracker.GetTurnNumber();

        if (IsHost)
            ReportHostTurnClientRpc(currentTurn);
        else
            ReportClientTurnServerRpc(currentTurn);
    }

    public void ReportCanPlayAfterMove()
    {
        //if (!IsHost)
        CanPlayServerRpc();
    }


    public int GetAuthorativeTurnPlayer()
    {
        return authorativeTurnPlayer.Value;
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