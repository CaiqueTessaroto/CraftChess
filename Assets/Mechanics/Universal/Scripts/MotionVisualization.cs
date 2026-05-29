using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MotionVisualization : MonoBehaviour
{
    [Header("Scripts")]
    public BoardChessManager gridManager;
    private MoveTracker moveTracker;
    private PieceController pieceController;

    [Header("Color")]
    public Color moveColor = Color.green;
    public Color captureColor = Color.red;

    private GameObject moveOverlayPrefab;
    private List<GameObject> activeOverlays = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<BoardChessManager>();

        moveOverlayPrefab = gridManager.selectionPrefab;

        moveTracker = FindFirstObjectByType<MoveTracker>();

        pieceController = FindFirstObjectByType<PieceController>();


        if (moveTracker == null)
            Debug.LogError("MoveTracker não encontrado na cena.");


    }

    public void VisualizeMoves(PieceComponent piece, PieceMovement movement)
    {
        ClearMoveOverlays();

        if (!gridManager || piece == null) return;

        if (movement == null || movement.configData == null) return;

        ShowSpritesAtMoves(piece);

    }

    private void ShowSpritesAtMoves(PieceComponent thisPiece)
    {

        if (thisPiece == null)
        {
            Debug.LogError("PieceComponent is null in ShowSpritesAtMoves");
            return;
        }

        if (thisPiece.PossibleMoves == null)
        {
            Debug.LogWarning("PossibleMoves is null for piece: " + thisPiece.name);
            //thisPiece.PossibleMoves = new List<Vector2Int>();
            return;
        }

        List<Vector2Int> moves = thisPiece.PossibleMoves;
        moves = moves.Where(move => !thisPiece.CaptureMoves.Contains(move)).ToList();

        foreach (var move in moves)
        {
            if (!gridManager.IsWithinBounds(move.x, move.y))
                continue;

            bool occupied = gridManager.IsHouseOccupied(move.x, move.y);

            if (occupied)
            {
                GameObject piece = gridManager.GetPieceAtPosition(move.x, move.y);
                PieceComponent pieceComponent = piece.GetComponent<PieceComponent>();
                if (pieceComponent.Player.id == thisPiece.Player.id)
                    occupied = false;

            }

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

        moves = thisPiece.CaptureMoves;
        foreach (var move in moves)
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
                    sr.color = captureColor;
                }

                activeOverlays.Add(highlight);

            }
        }
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
