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

    // ── campos de resync ──────────────────────────────────────────────────

    public enum LastMoveType { None, Move, Castle, Promotion }
    public LastMoveType lastMoveType = LastMoveType.None;

    public float turnStallTimer = 0f;

    private float lastResyncReportTime = -999f;
    private const float RESYNC_REPORT_COOLDOWN = 1f;

    public void EndGameHostLoseConnection()
    {
        if (endGame) return;

        bool white = !MatchData.Instance.HostIsWhite;
        bool black = MatchData.Instance.HostIsWhite;
        bool draw = false;

        base.EndGameLocal(black, white, draw);
    }

    public void EndGameClientLoseConnection()
    {
        if (endGame) return;

        bool white = MatchData.Instance.HostIsWhite;
        bool black = !MatchData.Instance.HostIsWhite;
        bool draw = false;

        base.EndGameLocal(black, white, draw);
    }

    public new void OnCellClicked(Vector2Int clickedPos, bool forceMove = false, bool IA = false)
    {

        if (boardManager.infoPiece && !IA)
        {
            GameObject piece = boardManager.GetPieceAtPosition(clickedPos.x, clickedPos.y);
            GetPieceInfo(piece);
            return;
        }

        if (MultiplayerLobbyState.IsSpectator)
            return;

        if (!isReceivingMove && !boardManager.noTurns && !IsMyTurnPublic())
            return;

        if (boardManager.noTurns && PieceControllerNetwork.Instance?.diff != 0)
        {
            if (Time.time - lastResyncReportTime >= RESYNC_REPORT_COOLDOWN)
            {
                lastResyncReportTime = Time.time;
                PieceControllerNetwork.Instance?.ReportTurnAfterMove();
            }

            return;
        }

        //Debug.Log($"[Multiplayer] OnCellClicked em {clickedPos} | forceMove: {forceMove} | isReceivingMove: {isReceivingMove}");
        base.OnCellClicked(clickedPos, forceMove, IA);
    }

    // ── gravar último lance ───────────────────────────────────────────────
    public new void RegisterMove(Vector2Int origin, Vector2Int target)
    {
        if (isReceivingMove) return;

        pendingOrigin = origin;
        pendingTarget = target;
        hasPendingMove = true;

        lastMoveType = LastMoveType.Move;
    }

    public new void RegisterCastle(Vector2Int kingOrigin, Vector2Int kingTarget,
                                    Vector2Int rookOrigin, Vector2Int rookTarget)
    {
        if (isReceivingMove) return;

        pendingKingOrigin = kingOrigin;
        pendingKingTarget = kingTarget;
        pendingRookOrigin = rookOrigin;
        pendingRookTarget = rookTarget;
        hasPendingCastle = true;

        lastMoveType = LastMoveType.Castle;
    }

    // Chame esse método logo antes de chamar PieceControllerNetwork.Instance?.SendPromotion(...)
    public void RegisterPromotion(Vector2Int origin, Vector2Int target,
        string pieceName, int playerId)
    {
        if (isReceivingMove) return;
        pendingOrigin = origin;
        pendingTarget = target;
        pendingNetworkPieceName = pieceName;   // ← faltava
        pendingNetworkPlayerId = playerId;     // ← faltava
        lastMoveType = LastMoveType.Promotion;
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

    public enum PendingMoveType { None, Move, Castle, Promotion }

    private PendingMoveType pendingNetworkType = PendingMoveType.None;
    private Vector2Int pendingNetworkOrigin;
    private Vector2Int pendingNetworkTarget;
    private Vector2Int pendingNetworkKingOrigin, pendingNetworkKingTarget;
    private Vector2Int pendingNetworkRookOrigin, pendingNetworkRookTarget;
    private string pendingNetworkPieceName;
    private int pendingNetworkPlayerId;

    // Fase 1 — salvar o que chegou (cancelando qualquer pendente anterior)
    public void SetPendingNetworkMove(Vector2Int origin, Vector2Int target,
        PendingMoveType type)
    {
        if (pendingNetworkType != PendingMoveType.None)
            Debug.Log($"[TwoPhase] Movimento pendente ({pendingNetworkType}) cancelado por novo Move.");

        pendingNetworkOrigin = origin;
        pendingNetworkTarget = target;
        pendingNetworkType = type;
    }

    public void SetPendingNetworkCastle(Vector2Int kOrigin, Vector2Int kTarget,
                                        Vector2Int rOrigin, Vector2Int rTarget)
    {
        if (pendingNetworkType != PendingMoveType.None)
            Debug.Log($"[TwoPhase] Movimento pendente ({pendingNetworkType}) cancelado por novo Castle.");

        pendingNetworkKingOrigin = kOrigin; pendingNetworkKingTarget = kTarget;
        pendingNetworkRookOrigin = rOrigin; pendingNetworkRookTarget = rTarget;
        pendingNetworkType = PendingMoveType.Castle;
    }

    public void SetPendingNetworkPromotion(Vector2Int origin, Vector2Int target,
                                           string pieceName, int playerId)
    {
        if (pendingNetworkType != PendingMoveType.None)
            Debug.Log($"[TwoPhase] Movimento pendente ({pendingNetworkType}) cancelado por nova Promoção.");

        pendingNetworkOrigin = origin;
        pendingNetworkTarget = target;
        pendingNetworkPieceName = pieceName;
        pendingNetworkPlayerId = playerId;
        pendingNetworkType = PendingMoveType.Promotion;
    }

    // Fase 2 — ExecuteConfirmed só executa se o pendente bate com o confirmado

    public void ExecuteConfirmedMove(Vector2Int origin, Vector2Int target)
    {
        if (pendingNetworkType == PendingMoveType.None)
        {
            Debug.LogWarning("[TwoPhase] Confirmação de Move ignorada — nenhum movimento pendente.");
            return;
        }

        if (pendingNetworkType != PendingMoveType.Move)
        {
            Debug.LogWarning("[TwoPhase] Confirmação de Move ignorada — tipo pendente diferente.");
            pendingNetworkType = PendingMoveType.None;
            return;
        }

        if (pendingNetworkOrigin != origin || pendingNetworkTarget != target)
        {
            Debug.LogWarning("[TwoPhase] Confirmação de Move ignorada — posições não batem com pendente.");
            pendingNetworkType = PendingMoveType.None;
            return;
        }

        pendingNetworkType = PendingMoveType.None;
        //lastMoveType = LastMoveType.None;

        base.DeselectPiece();

        // --- lógica original ---
        GameObject pieceAtOrigin = boardManager.GetPieceAtPosition(origin.x, origin.y);
        if (pieceAtOrigin == null) return;

        PieceComponent comp = pieceAtOrigin.GetComponent<PieceComponent>();
        PieceMovement movement = pieceAtOrigin.GetComponent<PieceMovement>();
        if (movement != null)
        {
            movement.enabled = true;
            boardManager.UpdateBoardControl();
            comp.PossibleMoves = movement.GetValidMoves();
        }

        isReceivingMove = true;
        base.OnCellClicked(origin, forceMove: true, false);
        base.OnCellClicked(target, forceMove: true, false);
        isReceivingMove = false;

        if (NetworkLobbyManager.Instance.IsHost)
            PieceControllerNetwork.Instance?.UpdateTurnPlayer();

        PieceControllerNetwork.Instance?.ReportTurnAfterMove();
    }

    public void ExecuteConfirmedCastle(Vector2Int kingOrigin, Vector2Int kingTarget,
                                        Vector2Int rookOrigin, Vector2Int rookTarget)
    {

        if (pendingNetworkType == PendingMoveType.None)
        {
            Debug.LogWarning("[TwoPhase] Confirmação de Move ignorada — nenhum movimento pendente.");
            return;
        }

        if (pendingNetworkType != PendingMoveType.None
            && pendingNetworkType != PendingMoveType.Castle)
        {
            Debug.LogWarning("[TwoPhase] Castle ignorado — tipo pendente diferente.");
            pendingNetworkType = PendingMoveType.None;
            return;
        }

        pendingNetworkType = PendingMoveType.None;
        //lastMoveType = LastMoveType.None;

        base.DeselectPiece();

        // --- lógica original ---
        GameObject kingObj = boardManager.GetPieceAtPosition(kingOrigin.x, kingOrigin.y);
        GameObject rookObj = boardManager.GetPieceAtPosition(rookOrigin.x, rookOrigin.y);
        if (kingObj == null || rookObj == null)
        {
            Debug.LogWarning("[Multiplayer] Castle: rei ou torre não encontrados.");
            return;
        }

        isReceivingMove = true;
        base.Move(kingObj, kingTarget);
        base.Move(rookObj, rookTarget);

        PieceComponent kingComponent = kingObj.GetComponent<PieceComponent>();
        int distance = Mathf.Abs(kingOrigin.x - rookOrigin.x)
                     + Mathf.Abs(kingOrigin.y - rookOrigin.y);

        moveTracker.AddMove(kingObj, kingComponent, kingOrigin, kingTarget);
        AddMove(false, distance);
        boardManager.HighlightLastMove(kingOrigin, kingTarget);
        boardManager.UpdateBoardControl();
        isReceivingMove = false;

        if (NetworkLobbyManager.Instance.IsHost)
            PieceControllerNetwork.Instance?.UpdateTurnPlayer();

        PieceControllerNetwork.Instance?.ReportTurnAfterMove();
    }

    public void ExecuteConfirmedPromotion(Vector2Int origin, Vector2Int target,
                                           string pieceName, int playerId)
    {

        if (pendingNetworkType == PendingMoveType.None)
        {
            Debug.LogWarning("[TwoPhase] Confirmação de Move ignorada — nenhum movimento pendente.");
            return;
        }

        if (pendingNetworkType != PendingMoveType.None
            && pendingNetworkType != PendingMoveType.Promotion)
        {
            Debug.LogWarning("[TwoPhase] Promoção ignorada — tipo pendente diferente.");
            pendingNetworkType = PendingMoveType.None;
            return;
        }

        base.DeselectPiece();
        
        pendingNetworkType = PendingMoveType.None;
        //lastMoveType = LastMoveType.None;

        // --- lógica original ---
        GameObject pawnObj = boardManager.GetPieceAtPosition(origin.x, origin.y);
        if (pawnObj == null) return;

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
            target,
            pieceName,
            targetPiece
        );


        if (NetworkLobbyManager.Instance.IsHost)
            PieceControllerNetwork.Instance?.UpdateTurnPlayer();

        PieceControllerNetwork.Instance?.ReportTurnAfterMove();
    }


    public void ResendLastMove()
    {
        if (isReceivingMove) return;

        Debug.Log($"[Resync] Reenviando último lance: {lastMoveType}");

        switch (lastMoveType)
        {
            case LastMoveType.Move:
                PieceControllerNetwork.Instance?.SendMove(
                    pendingOrigin.x, pendingOrigin.y,
                    pendingTarget.x, pendingTarget.y);
                break;

            case LastMoveType.Castle:
                PieceControllerNetwork.Instance?.SendCastle(
                    pendingKingOrigin.x, pendingKingOrigin.y,
                    pendingKingTarget.x, pendingKingTarget.y,
                    pendingRookOrigin.x, pendingRookOrigin.y,
                    pendingRookTarget.x, pendingRookTarget.y);
                break;

            case LastMoveType.Promotion:
                // guarde pieceName e playerId em campos, como nos outros
                PieceControllerNetwork.Instance?.SendPromotion(
                    pendingOrigin.x, pendingOrigin.y,
                    pendingTarget.x, pendingTarget.y,
                    pendingNetworkPieceName, pendingNetworkPlayerId);
                break;
        }
    }

    public bool IsMyTurnPublic()
    {
        if (PieceControllerNetwork.Instance != null)
            return PieceControllerNetwork.Instance.GetAuthorativeTurnPlayer() == GetLocalPlayerId();

        return moveTracker.GetTurnPlayer() == GetLocalPlayerId(); // fallback
    }

    public int GetLocalPlayerId()
    {
        if (MultiplayerLobbyState.IsSpectator) return -1;

        return NetworkLobbyManager.Instance.IsHost
            ? (MatchData.Instance.HostIsWhite ? 0 : 1)
            : (MatchData.Instance.HostIsWhite ? 1 : 0);
    }


}