using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IA : MonoBehaviour
{
    public BoardChessManager boardManager;
    public PieceController pieceController;
    public MoveTracker moveTracker;

    public float thinkDelay = 0.6f;
    private int botPlayerId;

    private bool isThinking = false;

    void Start()
    {
        if (!boardManager)
            boardManager = FindObjectOfType<BoardChessManager>();

        if (!pieceController)
            pieceController = FindObjectOfType<PieceController>();

        if (!moveTracker)
            moveTracker = FindObjectOfType<MoveTracker>();

        botPlayerId = boardManager.GetBotId();

    }

    void Update()
    {
        //if (boardManager.localGame)
        //    return;

        if (isThinking)
            return;

        if (moveTracker.GetTurnPlayer() == botPlayerId)
        {
            StartCoroutine(ThinkAndPlay());
        }
    }

    IEnumerator ThinkAndPlay()
    {
        isThinking = true;
        yield return new WaitForSecondsRealtime(thinkDelay);

        List<BotMove> possibleMoves = GetAllPossibleMoves();

        if (possibleMoves.Count == 0)
        {
            isThinking = false;
            yield break;
        }

        // Prioriza capturas
        List<BotMove> captureMoves = possibleMoves.FindAll(m => m.isCapture);

        BotMove chosenMove;

        if (captureMoves.Count > 0)
            chosenMove = captureMoves[Random.Range(0, captureMoves.Count)];
        else
            chosenMove = possibleMoves[Random.Range(0, possibleMoves.Count)];

        // Simula clique na peça
        pieceController.OnCellClicked(chosenMove.from);

        yield return new WaitForSecondsRealtime(0.1f);

        // Simula clique na casa de destino
        pieceController.OnCellClicked(chosenMove.to);

        isThinking = false;
    }

    List<BotMove> GetAllPossibleMoves()
    {
        List<BotMove> moves = new List<BotMove>();

        foreach (GameObject piece in boardManager.AllPieces)
        {
            if (!piece) continue;

            PieceComponent comp = piece.GetComponent<PieceComponent>();
            if (comp == null) continue;

            if (comp.Player.id != botPlayerId)
                continue;

            if (comp.PossibleMoves == null)
                continue;

            Vector2Int from = comp.Position;

            foreach (Vector2Int target in comp.PossibleMoves)
            {
                GameObject targetPiece =
                    boardManager.GetPieceAtPosition(target.x, target.y);

                bool isCapture =
                    targetPiece != null &&
                    targetPiece.GetComponent<PieceComponent>()?.Player.id != botPlayerId;

                moves.Add(new BotMove(from, target, isCapture));
            }

            foreach (Vector2Int target in comp.CaptureMoves)
            {
                moves.Add(new BotMove(from, target, true));
            }



        }

        return moves;
    }

    struct BotMove
    {
        public Vector2Int from;
        public Vector2Int to;
        public bool isCapture;

        public BotMove(Vector2Int f, Vector2Int t, bool capture)
        {
            from = f;
            to = t;
            isCapture = capture;
        }
    }
}
