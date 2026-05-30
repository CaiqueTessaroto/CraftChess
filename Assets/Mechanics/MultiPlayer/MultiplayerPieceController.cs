using UnityEngine;

public class MultiplayerPieceController : PieceController
{
    private bool isReceivingMove = false;

    private Vector2Int pendingOrigin;
    private Vector2Int pendingTarget;

    private Vector2Int pendingKingOrigin;
    private Vector2Int pendingKingTarget;
    private Vector2Int pendingRookOrigin;
    private Vector2Int pendingRookTarget;
    private bool hasPendingMove = false;
    private bool hasPendingCastle = false;

    public new void OnCellClicked(Vector2Int clickedPos, bool forceMove = false, bool IA = false)
    {

        if (boardManager.infoPiece && !IA)
        {
            GameObject piece = boardManager.GetPieceAtPosition(clickedPos.x, clickedPos.y);
            GetPieceInfo(piece);
            return;
        }

        if (!isReceivingMove && !IsMyTurnPublic())
        {
            //Debug.Log($"[Multiplayer] Bloqueado — não é o turno local. Turno: {moveTracker.GetTurnPlayer()}, Local: {GetLocalPlayerId()}");
            return;
        }

        //Debug.Log($"[Multiplayer] OnCellClicked em {clickedPos} | forceMove: {forceMove} | isReceivingMove: {isReceivingMove}");
        base.OnCellClicked(clickedPos, forceMove, IA);
    }

    public new void RegisterMove(Vector2Int origin, Vector2Int target)
    {
        if (isReceivingMove)
        {
            //Debug.Log("[Multiplayer] RegisterMove ignorado — veio da rede.");
            return;
        }

        //Debug.Log($"[Multiplayer] RegisterMove: {origin} → {target}");
        pendingOrigin = origin;
        pendingTarget = target;
        hasPendingMove = true;
    }

    public new void RegisterCastle(Vector2Int kingOrigin, Vector2Int kingTarget,
                                Vector2Int rookOrigin, Vector2Int rookTarget)
    {
        // Se o movimento veio da rede, não reagenda envio
        if (isReceivingMove) return;

        pendingKingOrigin = kingOrigin;
        pendingKingTarget = kingTarget;
        pendingRookOrigin = rookOrigin;
        pendingRookTarget = rookTarget;
        hasPendingCastle = true;
    }

    public override void BoardUpdate()
    {
        base.BoardUpdate();

        if (hasPendingCastle && !isReceivingMove)
        {
            PieceControllerNetwork.Instance?.SendCastle(
                pendingKingOrigin.x, pendingKingOrigin.y,
                pendingKingTarget.x, pendingKingTarget.y,
                pendingRookOrigin.x, pendingRookOrigin.y,
                pendingRookTarget.x, pendingRookTarget.y
            );
            hasPendingCastle = false;
        }

        if (hasPendingMove && !isReceivingMove)
        {
            PieceControllerNetwork.Instance?.SendMove(
                pendingOrigin.x, pendingOrigin.y,
                pendingTarget.x, pendingTarget.y
            );
            hasPendingMove = false;
        }
    }



    // Chamado para TODOS após confirmação do servidor
    public void ExecuteConfirmedMove(Vector2Int origin, Vector2Int target)
    {
        if (IsMyTurnPublic())
        {
            //Debug.Log("[Multiplayer] Movimento próprio confirmado — já executado.");
            return;
        }

        //Debug.Log($"[Multiplayer] Executando movimento do oponente: {origin} → {target}");

        GameObject pieceAtOrigin = boardManager.GetPieceAtPosition(origin.x, origin.y);

        if (pieceAtOrigin == null)
        {
            //Debug.LogWarning($"[Multiplayer] Nenhuma peça em {origin} para mover.");
            return;
        }

        PieceComponent comp = pieceAtOrigin.GetComponent<PieceComponent>();

        // ✅ Garante que PossibleMoves está atualizado ANTES de executar
        PieceMovement movement = pieceAtOrigin.GetComponent<PieceMovement>();
        if (movement != null)
        {
            movement.enabled = true;
            boardManager.UpdateBoardControl();          // atualiza controle do tabuleiro
            comp.PossibleMoves = movement.GetValidMoves(); // recalcula movimentos válidos
        }

        //Debug.Log($"[Multiplayer] PossibleMoves count: {comp.PossibleMoves?.Count ?? -1}");
        //Debug.Log($"[Multiplayer] Destino {target} está em PossibleMoves: {comp.PossibleMoves?.Contains(target)}");

        isReceivingMove = true;
        base.OnCellClicked(origin, forceMove: true, false);
        base.OnCellClicked(target, forceMove: true, false);
        isReceivingMove = false;
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


    public void ExecuteConfirmedPromotion(Vector2Int origin, Vector2Int target, string pieceName, int playerId)
    {
        if (IsMyTurnPublic())
        {
            //Debug.Log("[Multiplayer] Promoção própria confirmada — já executada.");
            return;
        }

        //Debug.Log($"[Multiplayer] Aplicando promoção do oponente | origem: {origin} destino: {target} peça: {pieceName} | playerId: {playerId}");

        // ✅ Busca o peão na origem, não no destino
        GameObject pawnObj = boardManager.GetPieceAtPosition(origin.x, origin.y);
        if (pawnObj == null)
        {
            //Debug.LogError($"[Multiplayer] Nenhuma peça encontrada em {origin} para promover.");
            return;
        }

        PieceComponent pawn = pawnObj.GetComponent<PieceComponent>();

        GameObject targetPiece = boardManager.GetPieceAtPosition(target.x, target.y);

        MatchSquadData squadData = (playerId == 0)
            ? boardManager.Squads[0]
            : boardManager.Squads[1];

        PromotionUI promotionUI = pawnObj.GetComponent<PromotionUI>();
        if (promotionUI == null)
            promotionUI = pawnObj.AddComponent<PromotionUI>();

        promotionUI.InitializeWithPiece(
            pawn,
            createPromotionUI.promotionCanvasPrefab,
            createPromotionUI.promotionButtonPrefab,
            squadData,
            target,   // pos de destino para PlacePiece
            pieceName,
            targetPiece
        );
    }

    public void ExecuteConfirmedCastle(Vector2Int kingOrigin, Vector2Int kingTarget,
                                        Vector2Int rookOrigin, Vector2Int rookTarget)
    {
        // Quem enviou já executou localmente
        if (IsMyTurnPublic()) return;

        GameObject kingObj = boardManager.GetPieceAtPosition(kingOrigin.x, kingOrigin.y);
        GameObject rookObj = boardManager.GetPieceAtPosition(rookOrigin.x, rookOrigin.y);

        if (kingObj == null || rookObj == null)
        {
            Debug.LogWarning("[Multiplayer] Castle: rei ou torre não encontrados.");
            return;
        }

        isReceivingMove = true;

        // Reutiliza o Move interno do base controller para não disparar nova rede
        base.Move(kingObj, kingTarget);
        base.Move(rookObj, rookTarget);

        PieceComponent kingComponent = kingObj.GetComponent<PieceComponent>();

        int distance = Mathf.Abs(kingOrigin.x - rookOrigin.x) + Mathf.Abs(kingOrigin.y - rookOrigin.y);

        moveTracker.AddMove(kingObj, kingComponent, kingOrigin, kingTarget);
        AddMove(false, distance);
        boardManager.HighlightLastMove(kingOrigin, kingTarget);
        boardManager.UpdateBoardControl();

        isReceivingMove = false;
    }

}