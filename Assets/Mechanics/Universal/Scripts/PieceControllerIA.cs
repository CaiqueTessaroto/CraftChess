using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceControllerIA : MonoBehaviour
{

    [Header("Scripts:")]
    public BoardChessManager boardManager;
    public MotionVisualization motionVisualization;
    public MoveTracker moveTracker;
    public ChessMovesPanel chessMovesPanel;
    public CreatePromotionUI createPromotionUI;
    public GameInterfaceManager gameInterfaceManager;
    public GameObject selectedPiece;
    public ManagerPieceInfo managerPieceInfo;
    public PieceController pieceController;

    [Header("Control:")]
    public bool KingWhiteIsInCheck;
    public bool KingBlackIsInCheck;
    public int botPlayerId;
    public bool IA = false;
    public PieceComponent KingWhite;
    public PieceComponent KingBlack;

    private PieceComponent pieceComponent;
    private PieceMovement pieceMovement;
    // Start is called before the first frame update
    void Start()
    {
        if (pieceController == null)
            pieceController = FindObjectOfType<PieceController>();

        if (managerPieceInfo == null)
            managerPieceInfo = FindObjectOfType<ManagerPieceInfo>();

        if (chessMovesPanel == null)
            chessMovesPanel = FindObjectOfType<ChessMovesPanel>();

        if (motionVisualization == null)
            motionVisualization = FindObjectOfType<MotionVisualization>();

        if (moveTracker == null)
            moveTracker = FindObjectOfType<MoveTracker>();

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardChessManager>();

        if (createPromotionUI == null)
            createPromotionUI = FindObjectOfType<CreatePromotionUI>();

        if (gameInterfaceManager == null)
            gameInterfaceManager = FindObjectOfType<GameInterfaceManager>();


        //KingWhite = pieceController.KingWhite;
        //KingBlack = pieceController.KingBlack;

        botPlayerId = boardManager.GetBotId();
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void OnCellClicked(Vector2Int clickedPos, bool IA = false)
    {
        this.IA = IA;

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

            if (!boardManager.localGame && comp.Player.id == botPlayerId && IA == false)
                return;

            if (boardManager.noTurns || comp.Player.id == moveTracker.GetTurnPlayer())
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
    }

    public bool SelectPiece(GameObject piece)
    {


        PieceComponent component = piece.GetComponent<PieceComponent>();


        if (component != null) //component != null && component.player.id == GameManager.Instance.currentPlayerId
        {
            selectedPiece = piece;
            pieceComponent = piece.GetComponent<PieceComponent>();
            pieceMovement = piece.GetComponent<PieceMovement>();

            return true;
        }

        return false;
    }

    private bool AttemptMoveOrCapture(Vector2Int clickedPosition)
    {
        //List<Vector2Int> validMoves = pieceMovement.GetValidMoves();
        List<Vector2Int> validMoves = pieceComponent.PossibleMoves;

        //if (pieceComponent.CastlingPieces.Count > 0 && pieceComponent.CastlingPieces != null)
        //    validMoves.AddRange(pieceMovement.GetCastlingMove(pieceComponent.CastlingPieces));

        if (validMoves == null)
        {
            //boardManager.UpdateBoardControl();
            pieceController.BoardUpdate();
        }

        bool captured = false;

        if (validMoves.Contains(clickedPosition) && validMoves != null)
        {

            GameObject targetPiece = boardManager.GetPieceAtPosition(clickedPosition.x, clickedPosition.y);

            bool isEnPassant = false;
            PieceComponent enemyBehind = null;

            // Verifica se é captura en passant
            if (pieceComponent.Power <= 50 && targetPiece == null && moveTracker.GetLastMoved() != null)
            {
                Move lastMoved = moveTracker.GetLastMoved();
                if (lastMoved != null && lastMoved.PieceObject != null)
                {
                    PieceComponent lastPieceMoved = lastMoved.PieceObject.GetComponent<PieceComponent>();

                    if (lastPieceMoved.InitialMoved && lastPieceMoved.Player.id != pieceComponent.Player.id)
                    {
                        Vector2Int direction = (lastPieceMoved.Player.id == 0) ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
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

                    AudioManager.Instance?.PlaySFX(pieceController.moveSound);

                    DeselectPiece();

                    //boardManager.UpdateBoardControl();
                    pieceController.BoardUpdate();

                    return true;
                }
            }

            if (isEnPassant && enemyBehind != null)
            {
                //VoxelSplitter voxelSplitter = enemyBehind.gameObject.GetComponent<VoxelSplitter>();
                //voxelSplitter.Splitter();

                //PieceDestroyer destroyer = GetComponent<PieceDestroyer>();
                //destroyer.DestroyPiece(enemyBehind.gameObject);

                //enemyBehind.gameObject.SetActive(false);
                boardManager.AddCapturedPiece(enemyBehind.gameObject, pieceComponent.Player.id);
                boardManager.AllPieces.Remove(enemyBehind.gameObject);
                Destroy(enemyBehind.gameObject);

                captured = true;

            }

            if (targetPiece != null)
            {
                PieceComponent targetComponent = targetPiece.GetComponent<PieceComponent>();

                if (targetComponent != null && targetComponent.Player.id != pieceComponent.Player.id)
                {
                    //moveTracker.AddMove(selectedPiece, pieceComponent, pieceComponent.Position, clickedPosition);
                    // Captura normal
                    boardManager.HighlightLastMove(pieceComponent.Position, clickedPosition);
                    CaptureEnemyPiece(selectedPiece, targetPiece, clickedPosition);

                    AudioManager.Instance?.PlaySFX(pieceController.captureSound);

                    DeselectPiece();

                    //boardManager.UpdateBoardControl();
                    pieceController.BoardUpdate();
                    //StartCoroutine(DelayedBoardUpdate(selectedPiece));

                    return true;
                }
            }
            else
            {
                // Movimento normal

                //moveTracker.AddMove(selectedPiece, pieceComponent, pieceComponent.Position, clickedPosition);

                boardManager.HighlightLastMove(pieceComponent.Position, clickedPosition);
                MovePiece(selectedPiece, clickedPosition, captured);

                AudioManager.Instance?.PlaySFX(pieceController.moveSound);

                DeselectPiece();

                //boardManager.UpdateBoardControl();
                pieceController.BoardUpdate();

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

        //if(rook)
        //    house = 


        SpriteRenderer sr = move.PieceObject.GetComponent<SpriteRenderer>();

        chessMovesPanel.AddMove(house, sr.sprite);
    }

    private void CaptureEnemyPiece(GameObject selectedPiece, GameObject targetPiece, Vector2Int targetPosition)
    {

        PieceComponent component = selectedPiece.GetComponent<PieceComponent>();

        if (component.PromotionPieces.Count > 0 && component.PromotionPieces != null)
            if (PromotePiece(component, targetPosition, targetPiece))
                return;



        if (targetPiece != null && targetPiece.name != "Selection Overlay")
        {

            PieceComponent componentTarget = targetPiece.GetComponent<PieceComponent>();
            // Captura: remove a peça inimiga
            boardManager.AddCapturedPiece(targetPiece, component.Player.id);
            boardManager.AllPieces.Remove(targetPiece);
            Destroy(targetPiece);
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

        moveTracker.AddMove(selectedPiece, pieceComponent, pieceComponent.Position, targetPosition);

        if (component.InitialMoved)
            component.InitialMoved = false;

        if (!component.HasMoved)
            component.InitialMoved = movement.IsMoveOnlyInSpecial(targetPosition.x, targetPosition.y);

        AddMove(captured);
        Move(selectedPiece, targetPosition);

    }



    private void Move(GameObject selectedPiece, Vector2Int targetPosition)
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
        int promotionRank = (piece.Player.id == 0) ? boardManager.gridHeight - 1 : 0;

        // Verifica a posição Y no grid
        bool reachedPromotionRank = targetPosition.y == promotionRank;

        // Verifica se a casa está no tabuleiro
        bool isPositionValid = boardManager.IsWithinBounds(
            targetPosition.x,
            targetPosition.y
        );

        if (reachedPromotionRank && isPositionValid)
        {
            //promotionUI.promotionCanvas.SetActive(true);
            //promotionUI.ShowPromotionOptions(selectedPiece);

            PromotionUI newpromotionUI = piece.gameObject.AddComponent<PromotionUI>();

            MatchSquadData squadData;

            if (piece.Player.id == 0)
                squadData = boardManager.Squads[0];
            else
                squadData = boardManager.Squads[1];

            newpromotionUI.Initialize(piece, createPromotionUI.promotionCanvasPrefab, createPromotionUI.promotionButtonPrefab, squadData, targetPosition, IA, targetPiece);
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

        // Move o rei para a posição da torre
        Move(selectedPiece, rookOrigin);

        // Move a torre para a posição original do rei
        Move(castlePiece, kingOrigin);

        moveTracker.AddMove(selectedPiece, pieceComponent, kingOrigin, rookOrigin);
        boardManager.HighlightLastMove(kingOrigin, rookOrigin);

        DeselectPiece();
    }


}
