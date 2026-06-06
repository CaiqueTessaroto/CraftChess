using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class PieceController : MonoBehaviour
{

    [Header("Scripts:")]
    public BoardChessManager boardManager;
    public MotionVisualization motionVisualization;
    public MoveTracker moveTracker;
    public ChessMovesPanel chessMovesPanel;
    public CreatePromotionUI createPromotionUI;
    public GameInterfaceManager gameInterfaceManager;
    public ManagerPieceInfo managerPieceInfo;

    [Header("AudioClip:")]
    public AudioClip moveSound;
    public AudioClip captureSound;
    public AudioClip checkSound;

    [Header("Control:")]
    public GameObject selectedPiece;
    public bool kingWhiteIsInCheck;
    public bool kingBlackIsInCheck;
    public int botPlayerId;
    public bool IA = false;
    public bool endGame = false;
    public bool haskingWhite = false;
    public bool haskingBlack = false;
    public bool forceMove = false;
    public PieceComponent KingWhite;
    public PieceComponent KingBlack;

    private PieceComponent pieceComponent;
    private PieceMovement pieceMovement;
    // Start is called before the first frame update
    void Start()
    {
        if (managerPieceInfo == null)
            managerPieceInfo = FindFirstObjectByType<ManagerPieceInfo>();

        if (chessMovesPanel == null)
            chessMovesPanel = FindFirstObjectByType<ChessMovesPanel>();

        if (motionVisualization == null)
            motionVisualization = FindFirstObjectByType<MotionVisualization>();

        if (moveTracker == null)
            moveTracker = FindFirstObjectByType<MoveTracker>();

        if (boardManager == null)
            boardManager = FindFirstObjectByType<BoardChessManager>();

        if (createPromotionUI == null)
            createPromotionUI = FindFirstObjectByType<CreatePromotionUI>();

        if (gameInterfaceManager == null)
            gameInterfaceManager = FindFirstObjectByType<GameInterfaceManager>();


        botPlayerId = boardManager.GetOpponentId();
    }


    public void OnCellClicked(Vector2Int clickedPos, bool forceMove = false, bool IA = false)
    {
        this.IA = IA;
        this.forceMove = forceMove;

        GameObject cell = boardManager.gridCells[clickedPos.x, clickedPos.y];
        boardManager.HighlightSelect(cell); // muda a cor da célula selecionada

        GameObject piece = boardManager.GetPieceAtPosition(clickedPos.x, clickedPos.y);

        if (piece != null)
        {
            PieceComponent comp = piece.GetComponent<PieceComponent>();

            if (selectedPiece != null)
            {
                if (AttemptMoveOrCapture(clickedPos))
                    return;
            }

            if (boardManager.infoPiece && !IA)
            {
                GetPieceInfo(piece);
                return;
            }

            if (!forceMove)
                if ((!boardManager.localGame && comp.Player.id == botPlayerId && IA == false) || boardManager.IAvsIA)
                    return;

            if (boardManager.noTurns || (comp.Player.id == moveTracker.GetTurnPlayer()) || forceMove) //erro ao mover o rei em check
                SelectPiece(piece);
            //Debug.Log($"Selecionou peça {piece.name} em {clickedPos}");

        }
        else if (selectedPiece != null)
        {
            AttemptMoveOrCapture(clickedPos);
        }
        else
        {
            DeselectPiece();
            //Debug.Log("Célula vazia clicada, nenhuma peça selecionada.");
        }

    }

    /*
    if (selectedPiece == null)
    {
        SelectPiece(piece);
        Debug.Log($"Selecionou peça {piece.name} em {clickedPos}");
    }
    else
    {
        AttemptMoveOrCapture(clickedPos);
    }
    */

    public void DeselectPiece()
    {
        selectedPiece = null;
        pieceComponent = null;
        pieceMovement = null;

        if (!forceMove)
            motionVisualization.ClearMoveOverlays();
    }

    public bool SelectPiece(GameObject piece)
    {


        PieceComponent component = piece.GetComponent<PieceComponent>();


        if (component != null) //component != null && component.player.id == GameManager.Instance.currentPlayerId
        {
            selectedPiece = piece;
            pieceComponent = piece.GetComponent<PieceComponent>();
            pieceMovement = piece.GetComponent<PieceMovement>();
            pieceMovement.enabled = true;


            if (!forceMove) // pieceComponent.Player.id != botPlayerId
                motionVisualization.VisualizeMoves(pieceComponent, pieceMovement);

            return true;
        }

        return false;
    }

    public void GetPieceInfo(GameObject piece)
    {
        if (piece == null)
            return;

        PieceComponent component = piece.GetComponent<PieceComponent>();
        PieceMovement movement = piece.GetComponent<PieceMovement>();

        MatchSquadData matchSquad;

        bool isWhite = component.Player.color == Color.white;

        if (isWhite)
            matchSquad = boardManager.Squads[0];
        else
            matchSquad = boardManager.Squads[1];

        string spriteName = component.name;
        Sprite sprite = matchSquad.Sprites[spriteName];

        Squad squad = matchSquad.Data;
        SquadPieceData pieceData = squad.Pieces.Find(p => p.NameInSquad == component.name);

        managerPieceInfo.SelectPiece(component.name, pieceData, movement.configData, sprite, !isWhite, component.IsKing);

    }

    // PieceController.cs
    public virtual void BoardUpdate()
    {
        StartCoroutine(DelayedBoardUpdate());
    }

    public IEnumerator DelayedBoardUpdate()
    {
        yield return new WaitForEndOfFrame();

        if (boardManager != null)
        {
            boardManager.UpdateBoardControl();
            GetCheck();
            boardManager.UpdateMoves();
        }

        if (selectedPiece)
            SelectPiece(selectedPiece.gameObject);
        else
            DeselectPiece();

        // Em multiplayer, só o host avalia e propaga fim de jogo
        if (boardManager.isMultiplayer)
        {
            if (NetworkLobbyManager.Instance.IsHost)
                CheckAndSendEndGame();
            // cliente não faz nada — aguarda o ClientRpc
            yield break;
        }

        // Lógica local/IA mantida igual
        bool black = false, white = false, draw = false;
        EvaluateEndGame(ref black, ref white, ref draw);
        SetEndGame(black, white, draw);
    }

    // Detecta e envia pelo host
    private void CheckAndSendEndGame()
    {
        bool black = false, white = false, draw = false;
        EvaluateEndGame(ref black, ref white, ref draw);

        if (black || white || draw)
            PieceControllerNetwork.Instance.SendEndGame(black, white, draw);
    }

    // Extrai a lógica de detecção — usada por ambos os caminhos
    private void EvaluateEndGame(ref bool black, ref bool white, ref bool draw)
    {
        if (!boardManager.WhiteHasMoves)
        {
            if (kingWhiteIsInCheck || boardManager.WhitePieces.Count == 0)
                black = true;
            else if (moveTracker.GetTurnPlayer() == 0)
                draw = true;
        }
        else if (!boardManager.BlackHasMoves)
        {
            if (kingBlackIsInCheck || boardManager.BlackPieces.Count == 0)
                white = true;
            else if (moveTracker.GetTurnPlayer() == 1)
                draw = true;
        }
        else if (boardManager.AllPieces.Count == 2 && haskingBlack && haskingWhite)
        {
            draw = true;
        }

        if (KingWhite == null && haskingWhite) black = true;
        if (KingBlack == null && haskingBlack) white = true;
    }


    public void SetEndGame(bool black = false, bool white = false, bool draw = false)
    {
        if (boardManager.localGame || boardManager.IAvsIA || boardManager.isMultiplayer)
            EndGameLocal(black, white, draw);
        else
            EndGame(black, white, draw);
    }

    public void EndGameLocal(bool black = false, bool white = false, bool draw = false)
    {
        if (draw)
        {
            gameInterfaceManager.EndGame("Draw");
            return;
        }

        Sprite winnerSprite = null;
        string winnerName = null;

        if (black) // pretas venceram
        {
            winnerName = MatchData.Instance.blackSquadName;

            if (MatchData.Instance.isMultiplayer)
            {

                bool blackIsHost = !MatchData.Instance.HostIsWhite;
                winnerSprite = blackIsHost
                    ? MatchData.Instance.HostProfileSprite
                    : MatchData.Instance.ClientProfileSprite;

                if (GetLocalPlayerId() == 0)
                {
                    EndGame(black, white, draw);
                    endGame = true;
                    return;
                }
            }
            else
            {
                winnerSprite = managerPieceInfo.pieceSpritesBlack[$"{KingBlack.Name}"];
            }

            gameInterfaceManager.EndGameLocal(winnerName, winnerSprite);
            endGame = true;
        }
        else if (white) // brancas venceram
        {
            winnerName = MatchData.Instance.whiteSquadName;

            if (MatchData.Instance.isMultiplayer)
            {
                bool whiteIsHost = MatchData.Instance.HostIsWhite;
                winnerSprite = whiteIsHost
                    ? MatchData.Instance.HostProfileSprite
                    : MatchData.Instance.ClientProfileSprite;

                if (GetLocalPlayerId() == 1)
                {
                    EndGame(black, white, draw);
                    endGame = true;
                    return;
                }
            }
            else
            {
                winnerSprite = managerPieceInfo.pieceSpritesWhite[$"{KingWhite.Name}"];
            }

            gameInterfaceManager.EndGameLocal(winnerName, winnerSprite);
            endGame = true;
        }
    }

    private int GetLocalPlayerId()
    {
        return NetworkLobbyManager.Instance.IsHost
            ? (MatchData.Instance.HostIsWhite ? 0 : 1)
            : (MatchData.Instance.HostIsWhite ? 1 : 0);
    }

    public void EndGame(bool black = false, bool white = false, bool draw = false)
    {
        if (draw)
            gameInterfaceManager.EndGame("Draw");

        if (black && botPlayerId == 0)
            gameInterfaceManager.EndGame("Victory");
        else if (white && botPlayerId == 1)
            gameInterfaceManager.EndGame("Victory");
        else if (black || white)
            gameInterfaceManager.EndGame("Defeat");

        if (black || white || draw)
            endGame = true;
    }

    private Coroutine whiteCheckBlink;
    private Coroutine blackCheckBlink;
    Cell whiteCellCheck;
    Cell blackCellCheck;
    public void GetCheck()
    {

        if (haskingWhite)
        {
            if (KingWhite == null)
            {
                kingWhiteIsInCheck = false;
                return;
            }
            // Verificar se o rei branco está em xeque
            Vector2Int kingWhitePos = KingWhite.Position;
            Cell cellWhite = boardManager
                .GetCellAtPosition(kingWhitePos.x, kingWhitePos.y)
                .GetComponent<Cell>();

            kingWhiteIsInCheck = cellWhite.house.isControlledByBlack;

            if (boardManager.localGame || (KingWhite.Player.id != botPlayerId && !boardManager.IAvsIA && !boardManager.noRules))
                if (kingWhiteIsInCheck)
                {
                    AudioManager.Instance.PlaySFX(checkSound);
                    whiteCellCheck = cellWhite;
                    boardManager.StartCheckBlink(cellWhite, ref whiteCheckBlink);
                }
                else
                {
                    boardManager.StopCheckBlink(whiteCellCheck, ref whiteCheckBlink);
                }

        }

        if (haskingBlack)
        {
            if (KingBlack == null)
            {
                kingBlackIsInCheck = false;
                return;
            }
            // Verificar se o rei preto está em xeque
            Vector2Int kingBlackPos = KingBlack.Position;
            Cell cellBlack = boardManager
                .GetCellAtPosition(kingBlackPos.x, kingBlackPos.y)
                .GetComponent<Cell>();

            kingBlackIsInCheck = cellBlack.house.isControlledByWhite;

            if (boardManager.localGame || (KingBlack.Player.id != botPlayerId && !boardManager.IAvsIA && !boardManager.noRules))
                if (kingBlackIsInCheck)
                {
                    AudioManager.Instance.PlaySFX(checkSound);
                    blackCellCheck = cellBlack;
                    boardManager.StartCheckBlink(cellBlack, ref blackCheckBlink);
                }
                else
                {
                    boardManager.StopCheckBlink(blackCellCheck, ref blackCheckBlink);
                }

        }

    }

    private PieceComponent enemyBehind = null;
    bool isEnPassant = false;
    private bool AttemptMoveOrCapture(Vector2Int clickedPosition)
    {

        if (endGame)
        {
            DeselectPiece();
            return false;
        }
        //List<Vector2Int> validMoves = pieceMovement.GetValidMoves();
        List<Vector2Int> validMoves = pieceComponent.PossibleMoves;

        if (validMoves == null && !forceMove)
        {
            BoardUpdate();
        }

        bool captured = false;

        if (validMoves.Contains(clickedPosition) && validMoves != null && !forceMove)
        {

            GameObject targetPiece = boardManager.GetPieceAtPosition(clickedPosition.x, clickedPosition.y);

            isEnPassant = false;
            enemyBehind = null;

            // Verifica se é captura en passant
            if (pieceComponent.Power <= 50 && targetPiece == null && moveTracker.GetLastMoved() != null)
            {
                Move lastMoved = moveTracker.GetLastMoved();
                if (lastMoved != null && lastMoved.PieceObject != null)
                {
                    PieceComponent lastPieceMoved = lastMoved.PieceObject.GetComponent<PieceComponent>();

                    if (lastPieceMoved.InitialMoved && lastPieceMoved.Player.id != pieceComponent.Player.id)
                    {
                        Vector2Int direction = (lastPieceMoved.Player.color == Color.white) ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
                        Vector2Int behind = lastMoved.TargetPosition - direction;

                        if (clickedPosition == behind)
                        {
                            isEnPassant = true;
                            enemyBehind = lastPieceMoved;
                        }
                    }
                }
            }

            // Verifica se é o movimento de Castling
            if (targetPiece != null)
            {
                PieceComponent targetComponent = targetPiece.GetComponent<PieceComponent>();

                if (targetComponent != null && targetComponent.Player.id == pieceComponent.Player.id)
                {
                    PerformCastle(targetPiece, clickedPosition);

                    DeselectPiece();

                    //boardManager.UpdateBoardControl();
                    BoardUpdate();

                    return true;
                }
            }

            if (isEnPassant && enemyBehind != null)
            {
                captured = true;
            }

            if (targetPiece != null)
            {
                PieceComponent targetComponent = targetPiece.GetComponent<PieceComponent>();

                if (targetComponent != null && targetComponent.Player.id != pieceComponent.Player.id)
                {
                    //moveTracker.AddMove(selectedPiece, pieceComponent, pieceComponent.Position, clickedPosition);
                    // Captura normal
                    CaptureEnemyPiece(selectedPiece, targetPiece, clickedPosition);

                    DeselectPiece();

                    //boardManager.UpdateBoardControl();
                    BoardUpdate();
                    //StartCoroutine(DelayedBoardUpdate(selectedPiece));

                    return true;
                }
            }
            else
            {
                MovePiece(selectedPiece, clickedPosition, captured);

                DeselectPiece();

                //boardManager.UpdateBoardControl();
                BoardUpdate();

                return true;
            }

        }
        else
        {
            DeselectPiece();
        }

        return false;
    }

    public void AddMove(bool captured, int distanceRook = 0)
    {

        Move move = moveTracker.GetLastMoved();

        string letter = $"{(char)('a' + move.TargetPosition.x)}";
        string number = $"{move.TargetPosition.y + 1}";

        string house;

        if (distanceRook != 0)
        {
            house = "O";
            for (int i = 1; i < distanceRook - 1; i++)
                house = $"{house}-O";
        }
        else if (captured)
            house = $"x{letter}{number}";
        else
            house = $"{letter}{number}";


        SpriteRenderer sr = move.PieceObject.GetComponent<SpriteRenderer>();

        chessMovesPanel.AddMove(house, sr.sprite);
    }

    private void CaptureEnemyPiece(GameObject selectedPiece, GameObject targetPiece, Vector2Int targetPosition)
    {

        PieceComponent component = selectedPiece.GetComponent<PieceComponent>();

        if (component.PromotionPieces.Count > 0 && component.PromotionPieces != null)
            if (PromotePiece(component, targetPosition, targetPiece))
                return;

        if (boardManager.isMultiplayer && !forceMove)
        {
            RegisterMove(component.Position, targetPosition);
            return;
        }

        if (targetPiece != null && targetPiece.name != "Selection Overlay")
        {

            //PieceComponent componentTarget = targetPiece.GetComponent<PieceComponent>();

            //boardManager.HighlightLastMove(component.Position, targetPosition);
            //RegisterMove(component.Position, targetPosition);

            // Captura: remove a peça inimiga
            boardManager.AddCapturedPiece(targetPiece, component.Player.id);
            boardManager.AllPieces.Remove(targetPiece);
            Destroy(targetPiece);
            AudioManager.Instance?.PlaySFX(captureSound);
            //targetPiece.SetActive(false);
            //Debug.Log($"Peça {targetPiece.name} capturada em {targetPosition}");
        }

        MovePiece(selectedPiece, targetPosition, true);

    }


    private void MovePiece(GameObject selectedPiece, Vector2Int targetPosition, bool captured = false)
    {
        PieceComponent component = selectedPiece.GetComponent<PieceComponent>();
        PieceMovement movement = selectedPiece.GetComponent<PieceMovement>();

        if (component.PromotionPieces.Count > 0 && component.PromotionPieces != null)
            if (PromotePiece(component, targetPosition))
                return;

        if (boardManager.isMultiplayer && !forceMove)
        {
            RegisterMove(component.Position, targetPosition);
            return;
        }

        if (captured && isEnPassant)
        {
            boardManager.AddCapturedPiece(enemyBehind.gameObject, pieceComponent.Player.id);
            boardManager.AllPieces.Remove(enemyBehind.gameObject);
            Destroy(enemyBehind.gameObject);
        }

        if (captured)
            AudioManager.Instance?.PlaySFX(captureSound);
        else
            AudioManager.Instance?.PlaySFX(moveSound);

        boardManager.HighlightLastMove(component.Position, targetPosition);
        moveTracker.AddMove(selectedPiece, component, component.Position, targetPosition);

        if (component.InitialMoved)
            component.InitialMoved = false;

        if (!component.HasMoved)
            component.InitialMoved = movement.IsMoveOnlyInSpecial(targetPosition.x, targetPosition.y);

        AddMove(captured);
        Move(selectedPiece, targetPosition);

    }



    public void Move(GameObject selectedPiece, Vector2Int targetPosition)
    {
        PieceComponent component = selectedPiece.GetComponent<PieceComponent>();
        PieceMovement movement = selectedPiece.GetComponent<PieceMovement>();

        Vector2Int origin = new Vector2Int(component.Position.x, component.Position.y);
        // Obtém a célula de origem e destino
        GameObject originCell = boardManager.GetCellAtPosition(origin.x, origin.y);
        GameObject targetCell = boardManager.GetCellAtPosition(targetPosition.x, targetPosition.y);

        if (originCell == null || targetCell == null)
        {
            Debug.LogWarning("Célula inválida ao tentar mover a peça!");
            return;
        }

        // Move a peça para o novo pai (a célula de destino)
        selectedPiece.transform.SetParent(targetCell.transform);
        selectedPiece.transform.localPosition = Vector3.zero;

        // Atualiza a posição no componente da peça (se existir)
        component.Position = targetPosition;

        if (!component.HasMoved)
            component.HasMoved = true;

        // (Opcional) Atualiza referências internas do GridManager se ele mantiver controle de peças
        boardManager.UpdatePiecePosition(origin, targetPosition, component.Name);

        //Debug.Log($"Peça {component.name} movida de {origin} para {targetPosition}");

    }

    private bool PromotePiece(PieceComponent piece, Vector2Int targetPosition, GameObject targetPiece = null)
    {

        // Verifica se já está promovido (se aplicável)
        if (!piece) return false;
        if (piece.IsPromoted) return false;

        // Determina a linha de promoção
        int promotionRank = (piece.Player.color == Color.white) ? boardManager.gridHeight - 1 : 0;

        // Verifica a posição Y no grid
        bool reachedPromotionRank = targetPosition.y == promotionRank;

        // Verifica se a casa está no tabuleiro
        bool isPositionValid = boardManager.IsWithinBounds(
            targetPosition.x,
            targetPosition.y
        );

        if (reachedPromotionRank && isPositionValid)
        {
            PromotionUI newpromotionUI = piece.gameObject.AddComponent<PromotionUI>();

            MatchSquadData squadData;

            if (piece.Player.color == Color.white)
                squadData = boardManager.Squads[0];
            else
                squadData = boardManager.Squads[1];

            newpromotionUI.Initialize(piece, createPromotionUI.promotionCanvasPrefab, createPromotionUI.promotionButtonPrefab, squadData, targetPosition, forceMove, IA, targetPiece);
        }
        else
            return false;

        DeselectPiece();
        return true;
    }


    private void PerformCastle(GameObject castlePiece, Vector2Int targetPosition)
    {
        Vector2Int origin = new Vector2Int(pieceComponent.Position.x, pieceComponent.Position.y);

        Vector2Int direction = new Vector2Int(
            targetPosition.x > origin.x ? 1 : (targetPosition.x < origin.x ? -1 : 0),
            targetPosition.y > origin.y ? 1 : (targetPosition.y < origin.y ? -1 : 0)
        );

        int distanceX = Mathf.Abs(targetPosition.x - origin.x);
        int distanceY = Mathf.Abs(targetPosition.y - origin.y);

        // Divide por 2 e arredonda pra cima
        int halfX = Mathf.CeilToInt(distanceX / 2f);
        int halfY = Mathf.CeilToInt(distanceY / 2f);

        Vector2Int middlePosition = origin + new Vector2Int(direction.x * halfX, direction.y * halfY);

        Vector2Int oppositeDirection = new Vector2Int(-direction.x, -direction.y);
        Vector2Int oneBackFromMiddle = middlePosition + oppositeDirection;

        int distance = Mathf.Abs(origin.x - targetPosition.x) + Mathf.Abs(origin.y - targetPosition.y);

        if (distance != 1)
        {

            if (boardManager.isMultiplayer && !forceMove)
            {
                RegisterCastle(origin, middlePosition,
                    castlePiece.GetComponent<PieceComponent>().Position, oneBackFromMiddle);
                return;
            }

            AudioManager.Instance?.PlaySFX(moveSound);

            Move(selectedPiece, middlePosition);

            Move(castlePiece, oneBackFromMiddle);

            moveTracker.AddMove(selectedPiece, pieceComponent, origin, middlePosition);
            boardManager.HighlightLastMove(origin, middlePosition);

            // Limpa as partículas e desseleciona a peça
            DeselectPiece();
        }
        else
        {
            PerformSwap(castlePiece);
        }

        AddMove(false, distance);

    }


    private void PerformSwap(GameObject castlePiece)
    {
        PieceComponent swapPiece = castlePiece.GetComponent<PieceComponent>();

        Vector2Int kingOrigin = pieceComponent.Position;
        Vector2Int rookOrigin = swapPiece.Position;

        if (boardManager.isMultiplayer && !forceMove)
        {
            RegisterCastle(kingOrigin, rookOrigin, rookOrigin, kingOrigin);
            return;
        }

        AudioManager.Instance?.PlaySFX(moveSound);

        // Move o rei para a posição da torre
        Move(selectedPiece, rookOrigin);

        // Move a torre para a posição original do rei
        Move(castlePiece, kingOrigin);

        moveTracker.AddMove(selectedPiece, pieceComponent, kingOrigin, rookOrigin);
        boardManager.HighlightLastMove(kingOrigin, rookOrigin);

        DeselectPiece();
    }
    public void RegisterCastle(Vector2Int kingOrigin, Vector2Int kingTarget, Vector2Int rookOrigin, Vector2Int rookTarget)
    {
        if (MatchData.Instance.isMultiplayer)
        {
            MultiplayerPieceController mp = this as MultiplayerPieceController;
            if (mp != null)
                mp.RegisterCastle(kingOrigin, kingTarget, rookOrigin, rookTarget);
        }
    }

    public void RegisterMove(Vector2Int origin, Vector2Int target)
    {
        if (MatchData.Instance.isMultiplayer)
        {
            MultiplayerPieceController mp = this as MultiplayerPieceController;
            if (mp != null)
                mp.RegisterMove(origin, target);
        }
    }

}
