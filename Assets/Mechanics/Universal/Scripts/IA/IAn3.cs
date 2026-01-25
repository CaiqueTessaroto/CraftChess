using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAn3 : MonoBehaviour
{
    public BoardChessManager boardManager;
    public PieceControllerIA pieceControllerIA;
    public PieceController pieceController;
    public MoveTracker moveTracker;

    public int botPlayerId = 1;
    public bool selectId = true;
    public float thinkDelay = 0.6f;

    private bool isThinking;

    void Start()
    {
        boardManager = FindObjectOfType<BoardChessManager>();
        pieceController = FindObjectOfType<PieceController>();
        moveTracker = FindObjectOfType<MoveTracker>();

        if (!pieceControllerIA)
            pieceControllerIA = GetComponent<PieceControllerIA>();

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
            pieceControllerIA.OnCellClicked(bestMove.from, true);
            yield return new WaitForSecondsRealtime(0.1f);
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
    // Movimento: casa que não estão sobre ataque ou tem o mesmo numero de defensores
    // Prioriza casas no meio

    //Ataque: captura peças soltas ou peças com maior poder
    float EvaluateMove(PieceComponent piece, Vector2Int target)
    {
        float score = 0;

        if (piece.IsKing)
            score -= 10;

        //if (piece.Power < 50)
        //    score -= 0.5f;

        // 📌 CENTRO
        if (!piece.IsKing && moveTracker.GetAllMoves().Count < 10)
        {
            float centerX = (boardManager.gridWidth - 1) / 2f;
            float centerY = (boardManager.gridHeight - 1) / 2f;

            float dist =
                Mathf.Abs(target.x - centerX) +
                Mathf.Abs(target.y - centerY);

            score += Mathf.Max(0, 12 - dist);
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

        // 📌 Defesa
        if (IsUnderAttack(piece.Position))
            score += piece.Power;

        List<PieceComponent> attackers =
            botPlayerId == 0
                ? cell.house.BlackPiecesControl
                : cell.house.WhitePiecesControl;

        List<PieceComponent> defenders =
            botPlayerId == 0
                ? cell.house.WhitePiecesControl
                : cell.house.BlackPiecesControl;

        int atkCount = attackers.Count;
        int defCount = defenders.Count;

        int atkPower = 0;
        int defPower = 0;

        foreach (PieceComponent attacker in attackers)
            atkPower += attacker.Power;

        foreach (PieceComponent defender in defenders)
            defPower += defender.Power;

        GameObject targetPiece =
            boardManager.GetPieceAtPosition(target.x, target.y);


        // 📌 Captura
        if (targetPiece != null)
        {
            PieceComponent enemy = targetPiece.GetComponent<PieceComponent>();
            if (enemy != null)
            {
                if (enemy.Player.id != botPlayerId)
                {
                    //score += enemy.Power * 1.2f;

                    if (atkCount == 0)
                        score += enemy.Power * 2f;
                    else if (atkCount == 1)
                        score += enemy.Power - piece.Power;
                    else
                    {
                        score += defPower - atkPower;
                        score += enemy.Power - piece.Power;
                    }


                }
                else
                {
                    //Roque
                    score += enemy.Power;
                }

                if (enemy.IsKing)
                    score += 1000;

            }
        }

        // 📌 Peças Pinadas
        cell = boardManager
            .GetCellAtPosition(piece.Position.x, piece.Position.y)
            .GetComponent<Cell>();

        attackers =
            botPlayerId == 0
                ? cell.house.BlackPiecesControl
                : cell.house.WhitePiecesControl;

        foreach (PieceComponent attacker in attackers)
        {
            PieceMovement enemyMove = attacker.GetComponent<PieceMovement>();
            PieceComponent BlockPiece = GetBlockPieceInRay(attacker.Position, enemyMove, piece);
            if (BlockPiece)
                score -= BlockPiece.Power;
        }

        //Xeque Mate
        List<GameObject> enemyPieces =
            botPlayerId == 0
                ? boardManager.WhitePieces
                : boardManager.BlackPieces;

        PieceMovement pieceMove = piece.GetComponent<PieceMovement>();

        PieceComponent enemyKing =
            botPlayerId == 0
                ? pieceController.KingBlack
                : pieceController.KingWhite;

        if (GetKingInRay(target, pieceMove, enemyKing))
            score += 10;

        if (moveTracker.GetAllMoves().Count > 50)
        {
            if (piece.IsKing && piece.Power < 100)
                score += 10;
            if (piece.PromotionPieces.Count > 0)
                score += 10;
        }


        // 📌 Promoção
        if (piece.PromotionPieces != null && piece.PromotionPieces.Count > 0)
        {
            int promotionRank =
                piece.Player.id == 0 ? boardManager.gridHeight - 1 : 0;

            if (target.y == promotionRank)
                score += 200;
        }

        // 📌 Movimento neutro recebe pequeno bônus
        score += Random.Range(0f, 0.5f);

        return score;
    }

    public PieceComponent GetBlockPieceInRay(
    Vector2Int attackerPos,
    PieceMovement attackerMove,
    PieceComponent pieceToIgnore
)
    {
        List<Vector2Int> ray = attackerMove.GetRayBetweenWithRange(attackerPos, pieceToIgnore.Position, attackerMove);

        foreach (Vector2Int pos in ray)
        {
            GameObject pieceObj = boardManager.GetPieceAtPosition(pos.x, pos.y);
            if (pieceObj == null)
                continue;

            PieceComponent piece = pieceObj.GetComponent<PieceComponent>();

            // Ignora a peça passada
            if (piece == pieceToIgnore)
                continue;

            // Encontrou qualquer outra peça no raio
            return piece;
        }

        // Nenhuma peça bloqueando
        return null;
    }

    public bool GetKingInRay(
        Vector2Int piecePos,
        PieceMovement pieceMove,
        PieceComponent enemyKing
    )
    {
        if (enemyKing == null)
            return false;

        List<Vector2Int> ray = pieceMove.GetRayBetweenWithRange(piecePos, enemyKing.Position, pieceMove);

        foreach (Vector2Int pos in ray)
        {
            GameObject pieceObj = boardManager.GetPieceAtPosition(pos.x, pos.y);

            // Casa vazia → continua o raio
            if (pieceObj == null)
                continue;

            PieceComponent piece = pieceObj.GetComponent<PieceComponent>();

            // Achou o rei inimigo → sucesso
            if (piece == enemyKing)
                return true;

            // Achou qualquer outra peça → bloqueia o raio
            return false;
        }

        return false;
    }

    private bool IsUnderAttack(Vector2Int piecePos)
    {

        Cell cell = boardManager.GetCellAtPosition(piecePos.x, piecePos.y)?.GetComponent<Cell>();
        if (cell == null) return false;

        return botPlayerId == 0
            ? cell.house.isControlledByBlack
            : cell.house.isControlledByWhite;
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