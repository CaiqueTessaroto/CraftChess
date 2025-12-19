using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Move
{

    
    public GameObject PieceObject;
    public PieceComponent PieceComponet;
    public Vector2Int OriginPosition;
    public Vector2Int TargetPosition;

    public Move(GameObject pieceObject, PieceComponent pieceData, Vector2Int originPosition, Vector2Int targetPosition)
    {
        this.PieceObject = pieceObject;
        this.PieceComponet = pieceData;
        this.OriginPosition = originPosition;
        this.TargetPosition = targetPosition;
    }
}

public class MoveTracker : MonoBehaviour
{
    public List<Move> moveHistory = new List<Move>();

    public void AddMove(GameObject pieceObject, PieceComponent pieceData, Vector2Int originPosition, Vector2Int targetPosition)
    {
        Move move = new Move(pieceObject, pieceData, originPosition, targetPosition);
        moveHistory.Add(move);
    }

    public Move GetLastMoved()
    {
        if (moveHistory.Count == 0)
            return null;

        return moveHistory[moveHistory.Count - 1];
    }

    public List<Move> GetAllMoves()
    {
        return new List<Move>(moveHistory);
    }

    public void ClearHistory()
    {
        moveHistory.Clear();
    }

}

