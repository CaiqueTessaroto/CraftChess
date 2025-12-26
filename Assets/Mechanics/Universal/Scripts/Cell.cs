using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


[System.Serializable]
public class House
{
    public string HouseName;
    public Vector2Int Position;
    public string Piece;
    //public string Type;          // "Pawn", "Rook", "Knight", "Bishop", "Queen", "King"
    public bool isOccupied = false;// => Piece != null;


    public bool isControlledByWhite;
    public bool isControlledByBlack;
    public List<PieceComponent> BlackPiecesControl = new List<PieceComponent>();
    public List<PieceComponent> WhitePiecesControl = new List<PieceComponent>();

    // Construtor para inicializar a casa
    public House(string name, Vector2Int position)
    {
        HouseName = name;
        Position = position;
        Piece = null;
    }
}


public class Cell : MonoBehaviour
{

    public BoardChessManager gridManager;
    public PieceController pieceController;
    public House house;

    // Start is called before the first frame update
    void Start()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<BoardChessManager>();

        if (pieceController == null)
            pieceController = FindObjectOfType<PieceController>();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (pieceController != null)
            pieceController.OnCellClicked(house.Position);
    }

}
