using UnityEngine;

public class MultiplayerPieceController : PieceController
{
    private bool isReceivingMove = false;

    private Vector2Int pendingOrigin;
    private Vector2Int pendingTarget;
    private bool hasPendingMove = false;

    public new void OnCellClicked(Vector2Int clickedPos, bool IA = false, bool forceMove = false)
    {

        if (boardManager.infoPiece && !IA)
        {
            GameObject piece = boardManager.GetPieceAtPosition(clickedPos.x, clickedPos.y);
            GetPieceInfo(piece);
            return;
        }

        if (!isReceivingMove && !IsMyTurnPublic())
        {
            Debug.Log($"[Multiplayer] Bloqueado — não é o turno local. Turno: {moveTracker.GetTurnPlayer()}, Local: {GetLocalPlayerId()}");
            return;
        }

        Debug.Log($"[Multiplayer] OnCellClicked em {clickedPos} | forceMove: {forceMove} | isReceivingMove: {isReceivingMove}");
        base.OnCellClicked(clickedPos, forceMove, IA);
    }

    public void RegisterMove(Vector2Int origin, Vector2Int target)
    {
        if (isReceivingMove)
        {
            Debug.Log("[Multiplayer] RegisterMove ignorado — veio da rede.");
            return;
        }

        Debug.Log($"[Multiplayer] RegisterMove: {origin} → {target}");
        pendingOrigin = origin;
        pendingTarget = target;
        hasPendingMove = true;
    }

    public override void BoardUpdate()
    {
        base.BoardUpdate();

        if (hasPendingMove && !isReceivingMove)
        {
            if (PieceControllerNetwork.Instance == null)
            {
                Debug.LogError("[Multiplayer] PieceControllerNetwork.Instance é null! GameObject de rede não encontrado.");
                return;
            }

            Debug.Log($"[Multiplayer] Enviando via PieceControllerNetwork: {pendingOrigin} → {pendingTarget}");
            PieceControllerNetwork.Instance.SendMove(pendingOrigin.x, pendingOrigin.y, pendingTarget.x, pendingTarget.y);
            hasPendingMove = false;
        }
    }

    // Chamado para TODOS após confirmação do servidor
    public void ExecuteConfirmedMove(Vector2Int origin, Vector2Int target)
    {
        if (IsMyTurnPublic())
        {
            Debug.Log("[Multiplayer] Movimento próprio confirmado — já executado.");
            return;
        }

        Debug.Log($"[Multiplayer] Executando movimento do oponente: {origin} → {target}");

        // Verifica o que tem na origem
        GameObject pieceAtOrigin = boardManager.GetPieceAtPosition(origin.x, origin.y);
        GameObject pieceAtTarget = boardManager.GetPieceAtPosition(target.x, target.y);

        Debug.Log($"[Multiplayer] Peça na origem {origin}: {(pieceAtOrigin != null ? pieceAtOrigin.name : "NENHUMA")}");
        Debug.Log($"[Multiplayer] Peça no destino {target}: {(pieceAtTarget != null ? pieceAtTarget.name : "NENHUMA")}");

        if (pieceAtOrigin != null)
        {
            PieceComponent comp = pieceAtOrigin.GetComponent<PieceComponent>();
            Debug.Log($"[Multiplayer] Peça origem — Player.id: {comp.Player.id} | PossibleMoves count: {comp.PossibleMoves?.Count ?? -1}");

            if (comp.PossibleMoves != null)
                Debug.Log($"[Multiplayer] PossibleMoves contém destino {target}: {comp.PossibleMoves.Contains(target)}");
        }

        isReceivingMove = true;
        base.OnCellClicked(origin, forceMove: true);
        base.OnCellClicked(target, forceMove: true);
        isReceivingMove = false;

        // Verifica resultado
        GameObject pieceAfter = boardManager.GetPieceAtPosition(target.x, target.y);
        Debug.Log($"[Multiplayer] Peça no destino após movimento: {(pieceAfter != null ? pieceAfter.name : "NENHUMA")}");
    }

    public bool IsMyTurnPublic()
    {
        return moveTracker.GetTurnPlayer() == GetLocalPlayerId();
    }

    private int GetLocalPlayerId()
    {
        return NetworkLobbyManager.Instance.IsHost
            ? (MatchData.Instance.HostIsWhite ? 0 : 1)
            : (MatchData.Instance.HostIsWhite ? 1 : 0);
    }
}