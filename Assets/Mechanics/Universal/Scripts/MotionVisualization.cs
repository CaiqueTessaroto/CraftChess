using System;
using System.Collections.Generic;
using UnityEngine;

public class MotionVisualization : MonoBehaviour
{
    [Header("Scripts")]
    public BoardChessManager gridManager;
    private MoveTracker moveTracker;

    [Header("Color")]
    public Color moveColor = Color.green;
    public Color captureColor = Color.red;

    private GameObject moveOverlayPrefab;
    private List<GameObject> activeOverlays = new List<GameObject>();

    private Movement moveData;
    private PersonalizedMove personalizedMoveData;
    private Special specialMoveData;

    // Start is called before the first frame update
    void Start()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<BoardChessManager>();

        moveOverlayPrefab = gridManager.selectionPrefab;

        moveTracker = FindObjectOfType<MoveTracker>();

        if (moveTracker == null)
            Debug.LogError("MoveTracker não encontrado na cena.");


    }

    public void VisualizeMoves(PieceComponent piece, PieceMovement movement)
    {
        ClearMoveOverlays();

        if (!gridManager || piece == null) return;

        if (movement == null || movement.configData == null) return;

        //Vector2Int origin = new Vector2Int((int)piece.gridPosition.x, (int)piece.gridPosition.y);


        // Direcional
        if (movement.configData.straight.Active)
        {
            moveData = movement.configData.straight;
            List<Vector2Int> rawMoves = movement.GetDirectionalMoves(moveData);
            List<Vector2Int> validMoves = movement.GetValidDirectionalMoves(rawMoves, moveData.Jump, moveData.Capture, moveData.Move);
            validMoves = movement.ControlOccupiedHouses(validMoves, moveData.Capture, false);
            validMoves = movement.GetValidKingMoves(validMoves);
            //List<Vector2Int> validMoves = movement.FilterValidMoves(rawMoves, moveData.Jump, moveData.Capture, moveData.Move);
            ShowSpritesAtMoves(validMoves, 1);
        }

        // Diagonal
        if (movement.configData.diagonal.Active)
        {
            moveData = movement.configData.diagonal;
            List<Vector2Int> rawMoves = movement.GetDiagonalMoves(moveData);
            List<Vector2Int> validMoves = movement.GetValidDiagonalMoves(rawMoves, moveData.Jump, moveData.Capture, moveData.Move);
            validMoves = movement.ControlOccupiedHouses(validMoves, moveData.Capture, false);
            validMoves = movement.GetValidKingMoves(validMoves);
            //List<Vector2Int> validMoves = movement.FilterValidMoves(rawMoves, moveData.Jump, moveData.Capture, moveData.Move);
            ShowSpritesAtMoves(validMoves, 1);
        }

        // Custom
        if (movement.configData.custom.Active)
        {
            personalizedMoveData = movement.configData.custom;
            List<Vector2Int> rawMoves = movement.GetCustomMovies();
            List<Vector2Int> validMoves = movement.FilterValidMoves(rawMoves, personalizedMoveData.Jump, personalizedMoveData.Capture, personalizedMoveData.Move);
            validMoves = movement.ControlOccupiedHouses(validMoves, personalizedMoveData.Capture, false);
            validMoves = movement.GetValidKingMoves(validMoves);
            ShowSpritesAtMoves(validMoves, 2);
        }

        // Especial
        if (!piece.HasMoved)
            if (movement.configData.special.Active)
            {
                specialMoveData = movement.configData.special;
                List<Vector2Int> rawMoves = movement.GetSpecialMovies();
                List<Vector2Int> validMoves = movement.FilterValidMoves(rawMoves, specialMoveData.Jump, specialMoveData.Capture, specialMoveData.Move);
                validMoves = movement.ControlOccupiedHouses(validMoves, specialMoveData.Capture, false);
                validMoves = movement.GetValidKingMoves(validMoves);
                ShowSpritesAtMoves(validMoves, 3);
            }


        if (moveTracker.GetLastMoved() != null)
        {
            Move lastMoved = moveTracker.GetLastMoved();
            if (lastMoved != null && lastMoved.PieceObject != null)
            {
                PieceComponent lastPieceMoved = lastMoved.PieceObject.GetComponent<PieceComponent>();
                if (lastPieceMoved.InitialMoved && piece.Player.id != lastPieceMoved.Player.id)
                {
                    List<Vector2Int> validMoves = movement.GetHouseBehindInitialMove(lastPieceMoved, lastMoved.TargetPosition);
                    validMoves = movement.GetValidKingMoves(validMoves);
                    ShowSpritesAtPassantMoves(validMoves);
                }
            }
        }


        if (!piece.HasMoved)
            if (piece.CastlingPieces.Count > 0 && piece.CastlingPieces != null)
            {
                List<Vector2Int> validMoves = movement.GetCastlingMove(piece.CastlingPieces);
                validMoves = movement.GetValidKingMoves(validMoves);
                ShowSpritesAtCastlingMoves(validMoves);
            }

    }


    private void ShowSpritesAtMoves(List<Vector2Int> moves, byte TypeMove)
    {

        foreach (var move in moves)
        {
            if (!gridManager.IsWithinBounds(move.x, move.y))
                continue;

            bool occupied = gridManager.IsHouseOccupied(move.x, move.y);
            bool canCapture = false;

            // Determina se o movimento atual pode capturar, conforme o tipo
            switch (TypeMove)
            {
                case 1:
                    canCapture = moveData.Capture;
                    break;
                case 2:
                    canCapture = personalizedMoveData.Capture;
                    break;
                case 3:
                    canCapture = specialMoveData.Capture;
                    break;
                default:
                    Debug.LogWarning("TypeMove inválido para sprite highlight");
                    break;
            }

            // Define a cor e cria o overlay na célula
            if (!occupied || (occupied && canCapture))
            {
                GameObject cell = gridManager.GetCellAtPosition(move.x, move.y);
                if (cell != null && moveOverlayPrefab != null)
                {
                    var highlight = Instantiate(moveOverlayPrefab, cell.transform);
                    highlight.transform.localPosition = Vector3.zero;
                    highlight.transform.localScale = Vector3.one;
                    //highlight.name = occupied ? "Overlay" : "Overlay";
                    highlight.name = "Overlay";

                    SpriteRenderer sr = highlight.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = 4;
                        sr.color = occupied ? captureColor : moveColor;
                    }

                    activeOverlays.Add(highlight);
                }
            }
        }
    }


    private void ShowSpritesAtCastlingMoves(List<Vector2Int> moves)
    {
        foreach (var move in moves)
        {
            GameObject cell = gridManager.GetCellAtPosition(move.x, move.y);
            if (cell == null) continue;

            OverlayInstantiateOn(cell.transform, moveColor);
        }

    }
    private void ShowSpritesAtPassantMoves(List<Vector2Int> moves)
    {
        foreach (var move in moves)
        {
            // Pega a célula correspondente
            GameObject cell = gridManager.GetCellAtPosition(move.x, move.y);
            if (cell == null) continue;

            OverlayInstantiateOn(cell.transform, captureColor);
        }
    }

    private void OverlayInstantiateOn(Transform transform, Color color)
    {
        // Instancia o overlay sobre a célula
        GameObject overlay = Instantiate(moveOverlayPrefab, transform);
        overlay.name = "Overlay";

        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localScale = Vector3.one;

        // Ajusta o sprite (cor, ordem, etc.)
        SpriteRenderer sr = overlay.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
            sr.sortingOrder = 4;
        }

        activeOverlays.Add(overlay);
    }

    public void ClearMoveOverlays()
    {
        foreach (var overlay in activeOverlays)
        {
            if (overlay != null)
                Destroy(overlay);
        }

        activeOverlays.Clear();
    }

}
