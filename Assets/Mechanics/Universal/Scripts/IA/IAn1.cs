using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAn1 : MonoBehaviour
{
    public BoardChessManager boardManager;
    public PieceControllerIA pieceControllerIA;
    public PieceController pieceController;
    public MoveTracker moveTracker;

    public float thinkDelay = 0.6f;
    public int botPlayerId = 0;
    public bool selectId = false;
    private bool isThinking = false;

    void Start()
    {
        if (!boardManager)
            boardManager = FindFirstObjectByType<BoardChessManager>();

        if (!pieceControllerIA)
            pieceControllerIA = GetComponent<PieceControllerIA>();

        pieceController = FindFirstObjectByType<PieceController>();

        if (!moveTracker)
            moveTracker = FindFirstObjectByType<MoveTracker>();

        if (!selectId)
            botPlayerId = boardManager.GetBotId();

    }

    void Update()
    {
        if (pieceController.endGame)
            return;

        if (isThinking)
            return;

        if (boardManager.noTurns)
            thinkDelay = Random.Range(1.5f, 3f);

        if (moveTracker.GetTurnPlayer() == botPlayerId || boardManager.noTurns)
        {
            isThinking = true;
            StartCoroutine(ThinkAndPlay());
        }
    }

    IEnumerator ThinkAndPlay()
    {
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
        pieceControllerIA.OnCellClicked(chosenMove.from, true);

        yield return new WaitForSecondsRealtime(0.2f);

        // Simula clique na casa de destino
        pieceControllerIA.OnCellClicked(chosenMove.to, true);

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
