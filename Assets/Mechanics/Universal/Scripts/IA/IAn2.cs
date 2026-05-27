using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAn2 : MonoBehaviour
{
    public BoardChessManager boardManager;
    public PieceControllerIA pieceControllerIA;
    public MoveTracker moveTracker;
    public PieceController pieceController;

    public int botPlayerId = 0;
    public bool selectId = true;
    public float thinkDelay = 0.6f;
    private BotMove lastBestMove;
    private int sameMoveCount = 0;
    private bool isThinking;

    void Start()
    {
        boardManager = FindFirstObjectByType<BoardChessManager>();

        pieceController = FindFirstObjectByType<PieceController>();

        if (!pieceControllerIA)
            pieceControllerIA = GetComponent<PieceControllerIA>();

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
            StartCoroutine(ThinkAndPlay());
        }

    }

    IEnumerator ThinkAndPlay()
    {
        isThinking = true;
        yield return new WaitForSecondsRealtime(thinkDelay);

        BotMove bestMove = FindBestMove();

        if (bestMove.isValid)
        {

            if (bestMove.Equals(lastBestMove))
            {
                sameMoveCount++;
            }
            else
            {
                sameMoveCount = 0;
            }

            lastBestMove = bestMove;

            pieceControllerIA.OnCellClicked(bestMove.from, true);
            yield return new WaitForSecondsRealtime(0.2f);
            pieceControllerIA.OnCellClicked(bestMove.to, true);
        }

        isThinking = false;
    }

    BotMove FindBestMove()
    {
        BotMove bestMove = new BotMove();
        float bestScore = float.MinValue;

        foreach (GameObject piece in boardManager.AllPieces)
        {
            if (!piece) continue;

            PieceComponent comp = piece.GetComponent<PieceComponent>();
            if (comp == null) continue;
            if (comp.Player.id != botPlayerId) continue;
            if (comp.PossibleMoves == null) continue;


            List<Vector2Int> validMoves = comp.PossibleMoves;
            validMoves.AddRange(comp.CaptureMoves);


            foreach (Vector2Int target in validMoves)
            {
                float score = EvaluateMove(comp, target);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = new BotMove(comp.Position, target);
                }
            }

        }



        return bestMove;
    }

    float EvaluateMove(PieceComponent piece, Vector2Int target)
    {
        float score = 0;

        // 📌 Captura
        GameObject targetPiece =
            boardManager.GetPieceAtPosition(target.x, target.y);

        if (targetPiece != null)
        {
            PieceComponent enemy = targetPiece.GetComponent<PieceComponent>();
            if (enemy != null && enemy.Player.id != botPlayerId)
            {
                score += enemy.Power * 1.2f;

                if (enemy.IsKing)
                    score += 1000;
            }
        }

        // 📌 Evitar casas controladas pelo inimigo
        Cell cell = boardManager
            .GetCellAtPosition(target.x, target.y)
            .GetComponent<Cell>();

        bool isDanger =
            botPlayerId == 0
                ? cell.house.isControlledByBlack
                : cell.house.isControlledByWhite;

        if (isDanger)
        {
            score -= piece.Power;
        }

        // 📌 Promoção
        if (piece.PromotionPieces != null && piece.PromotionPieces.Count > 0)
        {
            int promotionRank =
                piece.Player.color == Color.white ? boardManager.gridHeight - 1 : 0;

            if (target.y == promotionRank)
                score += 200;
        }

        // 📌 Movimento neutro recebe pequeno bônus
        score += Random.Range(0f, 0.5f + (sameMoveCount / 10f));

        return score;
    }

    struct BotMove
    {
        public Vector2Int from;
        public Vector2Int to;
        public bool isValid;

        public BotMove(Vector2Int f, Vector2Int t)
        {
            from = f;
            to = t;
            isValid = true;
        }
    }

}
