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
    public BoardChessManager boardChess;
    public List<Move> moveHistory = new List<Move>();

    public void Start()
    {

        if (boardChess == null)
            boardChess = FindFirstObjectByType<BoardChessManager>();

    }

    public void AddMove(GameObject pieceObject, PieceComponent pieceData, Vector2Int originPosition, Vector2Int targetPosition)
    {
        Move move = new Move(pieceObject, pieceData, originPosition, targetPosition);
        moveHistory.Add(move);

        if (boardChess.autoSwitchSide)
            boardChess.SwitchSide();

        if (boardChess.isMultiplayer)
            if (boardChess.chessClock != null && NetworkLobbyManager.Instance.IsHost)
            {
                bool IsWhiteTurn = GetTurnPlayer() == 0;
                boardChess.chessClock.chessClockNetwork.SwitchTurn(IsWhiteTurn);
            }
            
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

    public int GetTurnPlayer()
    {
        return moveHistory.Count % 2;
    }

    public int GetTurnNumber()
    {
        return moveHistory.Count;
    }

    public void ClearHistory()
    {
        moveHistory.Clear();
    }

}

