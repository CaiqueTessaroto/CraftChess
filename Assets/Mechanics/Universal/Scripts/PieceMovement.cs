using UnityEngine;
using System.Collections.Generic;

using System.Linq;

public class PieceMovement : MonoBehaviour
{
    [SerializeField]
    public MovementConfigData configData;
    private PieceComponent thisPiece;
    private BoardChessManager gridManager;
    private MoveTracker moveTracker;

    private PieceController pieceController;
    //private MoveTracker moveTracker;

    void Start()
    {
        thisPiece = GetComponent<PieceComponent>();

        gridManager = FindObjectOfType<BoardChessManager>();

        moveTracker = FindObjectOfType<MoveTracker>();

        pieceController = FindObjectOfType<PieceController>();

        if (gridManager == null)
            Debug.LogError("GridManager não encontrado.");

        if (moveTracker == null)
            Debug.LogError("MoveTracker não encontrado na cena.");

    }

    public void LoadConfigFromJson(MovementConfigData data)
    {
        configData = data;
    }


    public List<Vector2Int> GetValidMoves(bool control = false)
    {
        if (thisPiece == null) return null;

        List<Vector2Int> validMoves = new List<Vector2Int>();

        if (configData.straight.Active)
        {
            List<Vector2Int> rawMoves = GetDirectionalMoves(configData.straight);
            List<Vector2Int> moves = GetValidDirectionalMoves(rawMoves, false, configData.straight.Capture, configData.straight.Move);
            validMoves.AddRange(ControlOccupiedHouses(moves, configData.straight.Capture, false));

            //validMoves.AddRange(FilterValidMoves(rawMoves, configData.straight.Jump, configData.straight.Capture, configData.straight.Move));
        }
        if (configData.diagonal.Active)
        {
            List<Vector2Int> rawMoves = GetDiagonalMoves(configData.diagonal);
            List<Vector2Int> moves = GetValidDiagonalMoves(rawMoves, false, configData.diagonal.Capture, configData.diagonal.Move);
            validMoves.AddRange(ControlOccupiedHouses(moves, configData.diagonal.Capture, control));

            //validMoves.AddRange(FilterValidMoves(rawMoves, configData.diagonal.Jump, configData.diagonal.Capture, configData.diagonal.Move));
        }
        if (configData.custom.Active)
        {
            List<Vector2Int> rawMoves = GetCustomMovies();
            List<Vector2Int> moves = FilterValidMoves(rawMoves, configData.custom.Jump, configData.custom.Capture, configData.custom.Move);
            validMoves.AddRange(ControlOccupiedHouses(moves, configData.custom.Capture, control));

        }
        if (!thisPiece.HasMoved)
            if (configData.special.Active)
            {
                List<Vector2Int> rawMoves = GetSpecialMovies();
                List<Vector2Int> moves = FilterValidMoves(rawMoves, configData.special.Jump, configData.special.Capture, configData.special.Move);
                validMoves.AddRange(ControlOccupiedHouses(moves, configData.special.Capture, control));
            }


        if (moveTracker.GetLastMoved() != null)
        {
            Move lastMoved = moveTracker.GetLastMoved();
            if (lastMoved != null && lastMoved.PieceObject != null)
            {
                PieceComponent lastPieceMoved = lastMoved.PieceObject.GetComponent<PieceComponent>();
                if (lastPieceMoved.InitialMoved && thisPiece.Player.id != lastPieceMoved.Player.id)
                {
                    validMoves.AddRange(GetHouseBehindInitialMove(lastPieceMoved, lastMoved.TargetPosition));
                    thisPiece.CaptureMoves = GetHouseBehindInitialMove(lastPieceMoved, lastMoved.TargetPosition);
                }
            }
        }


        if (!gridManager.noRules)
        {
            List<Vector2Int> rawForMovesInCheck = new List<Vector2Int>();
            rawForMovesInCheck.AddRange(validMoves);

            if (thisPiece.IsKing)
                validMoves = GetValidKingMoves(rawForMovesInCheck);
            else
                validMoves = GetPiecePinnedMoves(rawForMovesInCheck);

            validMoves = GetLegalMovesWhileInCheck(thisPiece, validMoves);
        }

        if (!thisPiece.HasMoved)
            if (thisPiece.CastlingPieces.Count > 0 && thisPiece.CastlingPieces != null)
            {
                if (thisPiece.IsKing)
                {
                    if (thisPiece.Player.id == 0 && !pieceController.kingWhiteIsInCheck)
                        validMoves.AddRange(GetCastlingMove(thisPiece.CastlingPieces));
                    else if (thisPiece.Player.id == 1 && !pieceController.kingBlackIsInCheck)
                        validMoves.AddRange(GetCastlingMove(thisPiece.CastlingPieces));
                }
                else
                {
                    validMoves.AddRange(GetCastlingMove(thisPiece.CastlingPieces));
                }
            }



        return validMoves;
    }

    public List<Vector2Int> GetValidKingMoves(List<Vector2Int> validMoves)
    {
        if (!thisPiece.IsKing)
            return validMoves;

        List<Vector2Int> kingValidMoves = new List<Vector2Int>();

        foreach (Vector2Int move in validMoves)
        {
            GameObject gameObject_Cell = gridManager.GetCellAtPosition(move.x, move.y);
            Cell cell = gameObject_Cell.GetComponent<Cell>();

            if (thisPiece.Player.id == 0)
            {
                if (!cell.house.isControlledByBlack)
                    kingValidMoves.Add(move);
            }
            else
            {
                if (!cell.house.isControlledByWhite)
                    kingValidMoves.Add(move);
            }

        }

        Cell cellcomp = gridManager
            .GetCellAtPosition(thisPiece.Position.x, thisPiece.Position.y)
            .GetComponent<Cell>();

        List<PieceComponent> attackers =
            thisPiece.Player.id == 0
                ? cellcomp.house.BlackPiecesControl
                : cellcomp.house.WhitePiecesControl;

        foreach (var attacker in attackers)
        {
            PieceMovement attackerMove = attacker.GetComponent<PieceMovement>();
            if (!IsSlidingPiece(attackerMove))
                continue;

            List<Vector2Int> pinRay = GetRayBetweenWithRange(attacker.Position, thisPiece.Position, attackerMove);

            kingValidMoves = kingValidMoves.Where(move => !pinRay.Contains(move)).ToList();
        }

        return kingValidMoves;
    }

    public List<Vector2Int> GetRayBetweenWithRange(Vector2Int from, Vector2Int to, PieceMovement pieceMove)
    {
        List<Vector2Int> ray = new List<Vector2Int>();

        // Verifica se estão na mesma linha, coluna ou diagonal
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        // Se for o mesmo quadrado
        if (dx == 0 && dy == 0) return ray;

        // Verifica o tipo de movimento com base na direção
        bool isStraight = (dx == 0 || dy == 0);
        bool isDiagonal = (Mathf.Abs(dx) == Mathf.Abs(dy));

        // Se não for movimento válido
        if (!isStraight && !isDiagonal) return ray;

        // Verifica se a peça pode fazer este tipo de movimento
        if (isStraight && (!pieceMove.configData.straight.Active || pieceMove.configData.straight.Range <= 0))
            return ray;

        if (isDiagonal && (!pieceMove.configData.diagonal.Active || pieceMove.configData.diagonal.Range <= 0))
            return ray;

        // Determina a direção do passo
        int stepX = (dx != 0) ? (dx > 0 ? 1 : -1) : 0;
        int stepY = (dy != 0) ? (dy > 0 ? 1 : -1) : 0;

        // Calcula a distância até o destino
        int distanceToTarget = isStraight ? Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) : Mathf.Abs(dx);

        // Obtém o alcance máximo da peça
        int maxRange;
        if (isStraight)
        {
            maxRange = pieceMove.configData.straight.Range;
        }
        else // diagonal
        {
            maxRange = pieceMove.configData.diagonal.Range;
        }

        // Verifica se o destino está dentro do alcance
        if (distanceToTarget > maxRange)
            return ray;

        // Calcula quantos passos podemos dar (até o alcance máximo)
        int maxSteps = maxRange;
        int stepsTaken = 0;

        // Começa a partir da posição atual
        Vector2Int current = from;

        while (stepsTaken < maxSteps)
        {
            current += new Vector2Int(stepX, stepY);

            // Adiciona todas as posições incluindo o destino e além
            ray.Add(current);
            stepsTaken++;

            // Previne loop infinito
            if (stepsTaken > 16) break;
        }

        return ray;
    }

    public List<Vector2Int> GetPiecePinnedMoves(List<Vector2Int> validMoves)
    {
        if (thisPiece.IsKing)
            return validMoves;

        Vector2Int piecePos = thisPiece.Position;

        Cell cell = gridManager
            .GetCellAtPosition(piecePos.x, piecePos.y)
            .GetComponent<Cell>();

        List<PieceComponent> attackers =
            thisPiece.Player.id == 0
                ? cell.house.BlackPiecesControl
                : cell.house.WhitePiecesControl;

        if (attackers.Count == 0)
            return validMoves;

        Vector2Int kingPos = thisPiece.Player.id == 0
            ? pieceController.KingWhite.Position
            : pieceController.KingBlack.Position;

        foreach (var attacker in attackers)
        {
            PieceMovement attackerMove = attacker.GetComponent<PieceMovement>();
            if (!IsSlidingPiece(attackerMove))
                continue;

            List<Vector2Int> pinRay = GetRayBetween(attacker.Position, kingPos, attackerMove);

            // Verifica se há exatamente duas peças no raio
            int pieceCountInRay = 0;
            Vector2Int? ourPieceInRay = null;

            foreach (Vector2Int pos in pinRay)
            {
                GameObject gameObjectpieceAtpos = gridManager.GetPieceAtPosition(pos.x, pos.y);
                if (gameObjectpieceAtpos != null)
                {
                    PieceComponent pieceAtPos = gameObjectpieceAtpos.GetComponent<PieceComponent>();
                    pieceCountInRay++;
                    if (pieceAtPos == thisPiece)
                    {
                        ourPieceInRay = pos;
                    }
                }
            }

            // Para ser um pin válido, deve ter exatamente 2 peças no raio:
            // 1. A peça atual (thisPiece)
            // 2. O rei (que está na posição 'to' do raio)
            // E não pode haver outras peças no caminho
            if (pieceCountInRay != 1 || !ourPieceInRay.HasValue)
                continue;

            // Se chegou aqui, a peça está pinned
            List<Vector2Int> filtered = new();

            foreach (Vector2Int move in validMoves)
            {
                // A peça pinned só pode se mover:
                // 1. Para capturar o atacante
                // 2. Para permanecer no raio de pin (entre o atacante e o rei)
                if (move == attacker.Position || pinRay.Contains(move))
                    filtered.Add(move);
            }

            return filtered;
        }

        return validMoves;
    }

    public List<Vector2Int> GetRayBetween(Vector2Int from, Vector2Int to, PieceMovement pieceMove)
    {
        List<Vector2Int> ray = new List<Vector2Int>();

        // Verifica se estão na mesma linha, coluna ou diagonal
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        // Se for o mesmo quadrado
        if (dx == 0 && dy == 0) return ray;

        // Verifica o tipo de movimento com base na direção
        bool isStraight = (dx == 0 || dy == 0);
        bool isDiagonal = (Mathf.Abs(dx) == Mathf.Abs(dy));

        // Se não for movimento válido
        if (!isStraight && !isDiagonal) return ray;

        // Verifica se a peça pode fazer este tipo de movimento
        if (isStraight && (!pieceMove.configData.straight.Active || pieceMove.configData.straight.Range <= 0))
            return ray;

        if (isDiagonal && (!pieceMove.configData.diagonal.Active || pieceMove.configData.diagonal.Range <= 0))
            return ray;

        // Determina a direção do passo
        int stepX = (dx != 0) ? (dx > 0 ? 1 : -1) : 0;
        int stepY = (dy != 0) ? (dy > 0 ? 1 : -1) : 0;

        // Calcula a distância total
        int distance = isStraight ? Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) : Mathf.Abs(dx);

        // Verifica se está dentro do alcance da peça
        if (isStraight && distance > pieceMove.configData.straight.Range)
            return ray;

        if (isDiagonal && distance > pieceMove.configData.diagonal.Range)
            return ray;

        // Gera os quadrados intermediários
        Vector2Int current = from;
        int stepsTaken = 0;

        while (stepsTaken < distance)
        {
            current += new Vector2Int(stepX, stepY);

            // Para antes de chegar no quadrado de destino
            if (current == to)
                break;

            ray.Add(current);
            stepsTaken++;

            // Previne loop infinito
            if (stepsTaken > 16) return new List<Vector2Int>();
        }

        return ray;
    }

    bool IsSlidingPiece(PieceMovement pieceMove)
    {
        //return piece.Type == PieceType.Rook ||
        //       piece.Type == PieceType.Bishop ||
        //       piece.Type == PieceType.Queen;

        if (pieceMove.configData.straight.Active && pieceMove.configData.straight.Range > 1)
            return true;
        else if (pieceMove.configData.diagonal.Active && pieceMove.configData.diagonal.Range > 1)
            return true;
        else
            return false;
    }

    public List<Vector2Int> GetLegalMovesWhileInCheck(PieceComponent piece, List<Vector2Int> validMoves)
    {
        List<Vector2Int> legalMoves = new List<Vector2Int>();

        if (thisPiece.IsKing)
            return validMoves;

        bool check =
            piece.Player.id == 0
                ? pieceController.kingWhiteIsInCheck
                : pieceController.kingBlackIsInCheck;

        if (!check)
            return validMoves;

        // Rei
        Vector2Int kingPos = piece.Player.id == 0
            ? pieceController.KingWhite.Position
            : pieceController.KingBlack.Position;

        Cell kingCell = gridManager
            .GetCellAtPosition(kingPos.x, kingPos.y)
            .GetComponent<Cell>();

        // Atacantes
        List<PieceComponent> attackers =
            piece.Player.id == 0
                ? kingCell.house.BlackPiecesControl
                : kingCell.house.WhitePiecesControl;

        // Xeque duplo → só o rei pode jogar
        if (attackers.Count >= 2 && !thisPiece.IsKing)
            return legalMoves;

        // Xeque simples
        if (attackers.Count == 0)
            return legalMoves;
            
        PieceComponent attacker = attackers[0];
        PieceMovement attackerMove = attacker.gameObject.GetComponent<PieceMovement>();

        // Captura do atacante
        foreach (Vector2Int move in validMoves)
        {
            if (move == attacker.Position)
                legalMoves.Add(move);
        }

        // Bloqueio do ataque (se for peça de raio)
        if (IsSlidingPiece(attackerMove))
        {
            List<Vector2Int> blockRay =
                GetRayBetween(attacker.Position, kingPos, attackerMove);

            foreach (Vector2Int move in validMoves)
            {
                if (blockRay.Contains(move))
                    legalMoves.Add(move);
            }
        }

        return legalMoves;
    }


    public List<Vector2Int> GetValidCaptureMoves(bool control = false)
    {
        if (thisPiece == null) return null;

        List<Vector2Int> validMoves = new List<Vector2Int>();

        if (configData.straight.Active)
        {
            if (configData.straight.Capture)
            {
                List<Vector2Int> rawMoves = GetDirectionalMoves(configData.straight);
                rawMoves = GetValidDirectionalMoves(rawMoves, configData.straight.Jump, configData.straight.Capture, configData.straight.Move, control);
                rawMoves = ControlOccupiedHouses(rawMoves, configData.straight.Capture, control);
                validMoves.AddRange(rawMoves);
                //validMoves.AddRange(FilterValidMoves(rawMoves, configData.straight.Jump, configData.straight.Capture, configData.straight.Move));
            }
        }
        if (configData.diagonal.Active)
        {
            if (configData.diagonal.Capture)
            {
                List<Vector2Int> rawMoves = GetDiagonalMoves(configData.diagonal);
                rawMoves = GetValidDiagonalMoves(rawMoves, configData.diagonal.Jump, configData.diagonal.Capture, configData.diagonal.Move, control);
                rawMoves = ControlOccupiedHouses(rawMoves, configData.diagonal.Capture, control);
                validMoves.AddRange(rawMoves);
                //validMoves.AddRange(FilterValidMoves(rawMoves, configData.diagonal.Jump, configData.diagonal.Capture, configData.diagonal.Move));
            }
        }
        if (configData.custom.Active)
        {
            if (configData.custom.Capture)
            {
                List<Vector2Int> rawMoves = GetCustomMovies();
                rawMoves = FilterValidMoves(rawMoves, configData.custom.Jump, configData.custom.Capture, configData.custom.Move, control);
                rawMoves = ControlOccupiedHouses(rawMoves, configData.custom.Capture, control);
                validMoves.AddRange(rawMoves);
            }
        }
        if (!thisPiece.HasMoved)
            if (configData.special.Active)
            {
                if (configData.special.Capture)
                {
                    List<Vector2Int> rawMoves = GetSpecialMovies();
                    rawMoves = FilterValidMoves(rawMoves, configData.special.Jump, configData.special.Capture, configData.special.Move, control);
                    rawMoves = ControlOccupiedHouses(rawMoves, configData.special.Capture, control);
                    validMoves.AddRange(rawMoves);
                }
            }


        if (moveTracker.GetLastMoved() != null)
        {
            Move lastMoved = moveTracker.GetLastMoved();
            if (lastMoved != null && lastMoved.PieceObject != null)
            {
                PieceComponent lastPieceMoved = lastMoved.PieceObject.GetComponent<PieceComponent>();
                if (lastPieceMoved.InitialMoved && thisPiece.Player.id != lastPieceMoved.Player.id)
                {
                    validMoves.AddRange(GetHouseBehindInitialMove(lastPieceMoved, lastMoved.TargetPosition));
                }
            }
        }


        return validMoves;
    }

    public List<Vector2Int> GetCastlingMove(List<string> nameFragments)
    {
        List<PieceComponent> matchingPieces = new List<PieceComponent>();
        PieceComponent[] allPieces = GameObject.FindObjectsOfType<PieceComponent>();

        foreach (PieceComponent piece in allPieces)
        {
            foreach (string namePart in nameFragments)
            {
                if (piece.name.Contains(namePart) && piece.Player.id == thisPiece.Player.id)
                {
                    if (!piece.HasMoved)
                        if (IsPathClear(thisPiece, piece))
                        {
                            matchingPieces.Add(piece);
                            break; // Evita adicionar a mesma peça mais de uma vez
                        }
                }
            }
        }

        List<Vector2Int> positions = new List<Vector2Int>();

        foreach (PieceComponent piece in matchingPieces)
        {
            positions.Add(piece.Position);
            //Debug.Log(piece.gridPosition);
        }

        return positions;
    }

    public bool IsPathClear(PieceComponent fromPiece, PieceComponent toPiece)
    {
        Vector2Int start = fromPiece.Position;
        Vector2Int end = toPiece.Position;

        Vector2Int direction = new Vector2Int(
            end.x > start.x ? 1 : (end.x < start.x ? -1 : 0),
            end.y > start.y ? 1 : (end.y < start.y ? -1 : 0)
        );

        Vector2Int current = start + direction;

        while (current != end)
        {
            if (gridManager.IsHouseOccupied(current.x, current.y))
            {
                return false;
            }

            GameObject gameObjectPiece = gridManager.GetCellAtPosition(current.x, current.y);
            Cell cell = gameObjectPiece.GetComponent<Cell>();

            if (thisPiece.Player.id == 0)
            {
                if (cell.house.isControlledByBlack)
                    return false;
            }
            else
            {
                if (cell.house.isControlledByWhite)
                    return false;
            }

            current += direction;
        }

        return true;
    }

    public List<Vector2Int> GetHouseBehindInitialMove(PieceComponent piece, Vector2Int initialMove)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        Vector2Int direction = (piece.Player.id == 0) ? Vector2Int.up : Vector2Int.down;
        //Vector2Int direction = (piece.Player.id == 0) ? new Vector2Int(1, 0) : new Vector2Int(-1, 0);
        Vector2Int behind = initialMove - direction;

        bool passant = false;

        // Checar se está em outros movimentos
        if (configData.special.Active && configData.special.Capture && !passant)
        {
            List<Vector2Int> rawMoves = GetSpecialMovies();
            if (rawMoves.Contains(behind))
                passant = true;
        }

        if (configData.custom.Active && configData.custom.Capture && !passant)
        {
            List<Vector2Int> rawMovesCustom = GetCustomMovies();
            if (rawMovesCustom.Contains(behind))
                passant = true;
        }

        if (configData.straight.Active && configData.straight.Capture && !passant)
            if (GetDirectionalMoves(configData.straight).Contains(behind))
                passant = true;

        if (configData.diagonal.Active && configData.diagonal.Capture && !passant)
            if (GetDiagonalMoves(configData.diagonal).Contains(behind))
                passant = true;

        if (passant)
            if (configData.piece.Power <= 40)
                validMoves = new List<Vector2Int> { behind };

        return validMoves;
    }

    public List<Vector2Int> GetCustomMovies()
    {
        if (!configData.custom.Active || configData.custom.Moves == null)
            return new List<Vector2Int>();

        List<Vector2Int> moveList = new List<Vector2Int>();

        foreach (var m in configData.custom.Moves)
        {
            Vector2Int move = new Vector2Int(m.x, m.y);
            move = ConvertCoordinates(move);

            moveList.Add(move);
        }

        return GetRotatedMoves(moveList);


    }

    public Vector2Int ConvertCoordinates(Vector2Int m)
    {
        //return new Vector2Int(m.y, m.x);

        return m;
    }


    public List<Vector2Int> GetSpecialMovies()
    {
        if (!configData.special.Active || configData.special.Moves == null)
            return new List<Vector2Int>();

        List<Vector2Int> moveList = new List<Vector2Int>();

        foreach (var m in configData.special.Moves)
        {
            Vector2Int move = new Vector2Int(m.x, m.y);
            move = ConvertCoordinates(move);

            moveList.Add(move);
        }


        return GetRotatedMoves(moveList);

        //return GetRotatedMoves(configData.special.Moves
        //    .Select(m => new Vector2Int(m.x, m.y))
        //    .ToList());
    }

    public List<Vector2Int> GetDirectionalMoves(Movement moveData)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        if (moveData.All)
        {
            moveData.Front = moveData.Back = moveData.Left = moveData.Right = true;
        }

        // Obtém a posição atual da peça
        //Vector2Int origin = new Vector2Int((int)piece.gridPosition.x, (int)piece.gridPosition.y);

        Vector2Int move = new Vector2Int();

        for (int i = 1; i <= moveData.Range; i++)
        {

            if (moveData.Front)
            {
                move = new Vector2Int(0, i);

                move = ConvertCoordinates(move);
                validMoves.Add(move);
            }

            if (moveData.Back)
            {
                move = new Vector2Int(0, -i);

                move = ConvertCoordinates(move);
                validMoves.Add(move);
            }

            if (moveData.Right)
            {
                move = new Vector2Int(i, 0);

                move = ConvertCoordinates(move);
                validMoves.Add(move);
            }

            if (moveData.Left)
            {
                move = new Vector2Int(-i, 0);

                move = ConvertCoordinates(move);
                validMoves.Add(move);
            }


        }

        return GetRotatedMoves(validMoves);
    }





    public List<Vector2Int> GetDiagonalMoves(Movement moveData)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        // Se All estiver ativo, todas as direções são permitidas
        if (moveData.All)
        {
            moveData.Front = moveData.Back = moveData.Left = moveData.Right = true;
        }

        // Diagonais puras: (x, y)
        Vector2Int[] directions = new Vector2Int[]
        {
        new Vector2Int(1, 1),    // Frente-Direita
        new Vector2Int(-1, 1),   // Frente-Esquerda
        new Vector2Int(1, -1),   // Trás-Direita
        new Vector2Int(-1, -1)   // Trás-Esquerda
        };

        foreach (Vector2Int dir in directions)
        {
            for (int i = 1; i <= moveData.Range; i++)
            {
                int dx = dir.x;
                int dy = dir.y;

                if (moveData.Front && !moveData.Right && !moveData.Left)
                {
                    if (dy < 0 && !moveData.Back) continue;
                }
                else if (moveData.Back && !moveData.Right && !moveData.Left)
                {
                    if (dy > 0 && !moveData.Front) continue;
                }
                else if (moveData.Right && !moveData.Front && !moveData.Back)
                {
                    if (dx < 0 && !moveData.Left) continue;
                }
                else if (moveData.Left && !moveData.Front && !moveData.Back)
                {
                    if (dx > 0 && !moveData.Right) continue;
                }
                else
                {
                    // Filtrando direções específicas
                    if (dy > 0 && !moveData.Front) continue;
                    if (dy < 0 && !moveData.Back) continue;
                    if (dx > 0 && !moveData.Right) continue;
                    if (dx < 0 && !moveData.Left) continue;
                }

                Vector2Int target = new Vector2Int(dx * i, dy * i);

                Vector2Int move = new Vector2Int(target.x, target.y);
                move = ConvertCoordinates(move);

                validMoves.Add(move);
            }
        }

        return GetRotatedMoves(validMoves);
    }


    private List<Vector2Int> GetRotatedMoves(List<Vector2Int> moves)
    {

        List<Vector2Int> rotatedMoves = new List<Vector2Int>();

        if (moves == null || moves.Count == 0)
            return rotatedMoves;

        Vector2Int origin = new Vector2Int(thisPiece.Position.x, thisPiece.Position.y);

        // Se a peça for do jogador 1, inverte os movimentos (anda pra trás)

        foreach (Vector2Int move in moves)
        {
            Vector2Int adjustedMove = move;

            if (thisPiece.Player.id == 1)
                adjustedMove = new Vector2Int(-move.x, -move.y);

            rotatedMoves.Add(new Vector2Int(
                origin.x + adjustedMove.x,
                origin.y + adjustedMove.y
            ));

        }

        return rotatedMoves;

    }




    public List<Vector2Int> GetValidDiagonalMoves(List<Vector2Int> DiagonalMoves, bool canJumpOverPieces, bool captureMovement, bool canMove, bool control = false)
    {

        if (!canMove && !captureMovement && !canJumpOverPieces)
            canMove = true;

        List<Vector2Int> validMoves = new List<Vector2Int>();
        Vector2Int origin = new Vector2Int(thisPiece.Position.x, thisPiece.Position.y);

        // Group moves by diagonal direction
        Dictionary<string, List<Vector2Int>> diagonalDirections = new Dictionary<string, List<Vector2Int>>()
    {
        { "frontRight", DiagonalMoves.Where(m => m.x > origin.x && m.y > origin.y)
                              .OrderBy(m => m.x).ToList() },
        { "frontLeft", DiagonalMoves.Where(m => m.x < origin.x && m.y > origin.y)
                             .OrderByDescending(m => m.x).ToList() },
        { "backRight", DiagonalMoves.Where(m => m.x > origin.x && m.y < origin.y)
                             .OrderBy(m => m.x).ToList() },
        { "backLeft", DiagonalMoves.Where(m => m.x < origin.x && m.y < origin.y)
                            .OrderByDescending(m => m.x).ToList() }
    };

        foreach (var direction in diagonalDirections)
        {
            bool blocked = false;

            foreach (var move in direction.Value)
            {
                if (blocked) break;

                // Check if move is within grid bounds
                if (!gridManager.IsWithinBounds(move.x, move.y))
                    continue;

                // Calculate direction step (1 or -1 for each axis)
                Vector2Int step = new Vector2Int(
                    move.x > origin.x ? 1 : -1,
                    move.y > origin.y ? 1 : -1
                );

                // Check all cells along the diagonal path
                bool pathBlocked = false;
                Vector2Int current = origin + step;

                while (current != move)
                {
                    if (!gridManager.IsWithinBounds(current.x, current.y))
                    {
                        pathBlocked = true;
                        break;
                    }

                    if (gridManager.IsHouseOccupied(current.x, current.y))
                    {
                        pathBlocked = true;
                        break;
                    }

                    current += step;
                }

                if (pathBlocked)
                {
                    blocked = true;
                    continue;
                }

                // Check the target cell
                if (gridManager.IsHouseOccupied(move.x, move.y))
                {
                    validMoves.Add(move);

                    blocked = !canJumpOverPieces;
                }
                else if (captureMovement && control)
                {
                    validMoves.Add(move);
                }
                else if (canMove)
                {
                    validMoves.Add(move);
                }
            }
        }

        return validMoves;
    }

    public List<Vector2Int> GetValidDirectionalMoves(List<Vector2Int> DirectionalMoves, bool canJumpOverPieces, bool captureMovement, bool canMove, bool control = false)
    {

        if (!canMove && !captureMovement && !canJumpOverPieces)
            canMove = true;

        List<Vector2Int> validMoves = new List<Vector2Int>();
        Vector2Int origin = new Vector2Int(thisPiece.Position.x, thisPiece.Position.y);

        // Group moves by direction (front, back, left, right)
        Dictionary<string, List<Vector2Int>> directionalMoves = new Dictionary<string, List<Vector2Int>>()
    {
        { "front", DirectionalMoves.Where(m => m.x == origin.x && m.y > origin.y).OrderBy(m => m.y).ToList() },
        { "back", DirectionalMoves.Where(m => m.x == origin.x && m.y < origin.y).OrderByDescending(m => m.y).ToList() },
        { "right", DirectionalMoves.Where(m => m.y == origin.y && m.x > origin.x).OrderBy(m => m.x).ToList() },
        { "left", DirectionalMoves.Where(m => m.y == origin.y && m.x < origin.x).OrderByDescending(m => m.x).ToList() }
    };

        foreach (var direction in directionalMoves)
        {
            bool blocked = false;

            foreach (var move in direction.Value)
            {
                if (blocked) break;

                // Check if move is within grid bounds
                if (!gridManager.IsWithinBounds(move.x, move.y))
                    continue;

                // Calculate direction vector
                Vector2Int dirVector = new Vector2Int(
                    move.x != origin.x ? (move.x > origin.x ? 1 : -1) : 0,
                    move.y != origin.y ? (move.y > origin.y ? 1 : -1) : 0
                );

                // Check all cells along the path to the target move
                bool pathBlocked = false;
                Vector2Int currentPos = origin + dirVector;

                while (currentPos != move)
                {
                    if (!gridManager.IsWithinBounds(currentPos.x, currentPos.y))
                    {
                        pathBlocked = true;
                        break;
                    }

                    if (gridManager.IsHouseOccupied(currentPos.x, currentPos.y))
                    {
                        pathBlocked = true;
                        break;
                    }

                    currentPos += dirVector;
                }

                if (pathBlocked)
                {
                    blocked = true;
                    continue;
                }

                // Check the target cell
                if (gridManager.IsHouseOccupied(move.x, move.y))
                {
                    validMoves.Add(move);

                    blocked = true; //!canJumpOverPieces
                }
                else if (captureMovement && control)
                {
                    validMoves.Add(move);
                }
                else if (canMove)
                {
                    validMoves.Add(move);
                }

            }
        }

        return validMoves;
    }



    public List<Vector2Int> ControlOccupiedHouses(List<Vector2Int> moves, bool captureMovement, bool control = false)
    {
        // Se não está em modo de captura nem de controle, não há nada para filtrar

        //if (!control)
        //    return moves;

        List<Vector2Int> filteredMoves = new List<Vector2Int>();

        foreach (var move in moves)
        {
            // Verifica se a casa está ocupada
            if (gridManager.IsHouseOccupied(move.x, move.y))
            {
                if (!captureMovement) continue;

                GameObject pieceObject = gridManager.GetPieceAtPosition(move.x, move.y);
                if (pieceObject == null) continue;

                PieceComponent targetComponent = pieceObject.GetComponent<PieceComponent>();
                if (targetComponent == null) continue;

                bool isAlly = targetComponent.Player.id == thisPiece.Player.id;
                bool isEnemy = !isAlly;

                // 🛡️ Peças aliadas entram só no modo controle
                if (isAlly && control)
                    filteredMoves.Add(move);

                // ⚔️ Peças inimigas entram se captura for permitida ou controle ativo
                if (isEnemy)
                    filteredMoves.Add(move);
            }
            else
            {
                // Casas vazias sempre permanecem válidas
                filteredMoves.Add(move);
            }
        }

        return filteredMoves;
    }













    public List<Vector2Int> FilterDirectionalMoves(List<Vector2Int> rawMoves)
    {
        List<Vector2Int> orthogonalMoves = new List<Vector2Int>();
        Vector2Int currentPosition = new Vector2Int(thisPiece.Position.x, thisPiece.Position.y);

        foreach (Vector2Int move in rawMoves)
        {
            Vector2Int relativeMove = move - currentPosition;

            // Check for orthogonal movement (either x or y is zero, but not both)
            if (IsOrthogonalMove(relativeMove))
            {
                orthogonalMoves.Add(move);
            }
        }

        return orthogonalMoves;
    }

    private bool IsOrthogonalMove(Vector2Int move)
    {
        // True if movement is purely horizontal or vertical
        return (move.x == 0 && move.y != 0) || (move.y == 0 && move.x != 0);
    }

    public List<Vector2Int> FilterDiagonalMoves(List<Vector2Int> rawMoves)
    {
        List<Vector2Int> diagonalMoves = new List<Vector2Int>();
        Vector2Int currentPosition = new Vector2Int(thisPiece.Position.x, thisPiece.Position.y);

        foreach (Vector2Int move in rawMoves)
        {
            Vector2Int relativeMove = move - currentPosition;

            if (IsDiagonalMove(relativeMove))
            {
                diagonalMoves.Add(move);
            }
        }

        return diagonalMoves;
    }

    private bool IsDiagonalMove(Vector2Int move)
    {
        int absX = Mathf.Abs(move.x);
        int absY = Mathf.Abs(move.y);

        // True if movement is perfectly diagonal (equal x/y magnitude and not zero)
        return absX == absY && absX > 0;
    }


    public List<Vector2Int> FilterUnclassifiedMoves(List<Vector2Int> rawMoves, List<Vector2Int> directionalMoves, List<Vector2Int> diagonalMoves)
    {
        List<Vector2Int> filterMoves = new List<Vector2Int>();

        //List<Vector2Int> directionalMoves = FilterDirectionalMoves(rawMoves);
        //List<Vector2Int> diagonalMoves = FilterDiagonalMoves(rawMoves);

        // Cria um HashSet para verificação rápida
        HashSet<Vector2Int> classifiedMoves = new HashSet<Vector2Int>(directionalMoves);
        classifiedMoves.UnionWith(diagonalMoves);

        // Filtra apenas os movimentos que NÃO estão nos classificados
        foreach (Vector2Int move in rawMoves)
        {
            if (!classifiedMoves.Contains(move))
            {
                filterMoves.Add(move);
            }
        }

        return filterMoves;
    }

    public List<Vector2Int> FilterValidMoves(List<Vector2Int> rawMoves, bool canJumpOverPieces, bool captureMovement, bool canMove, bool control = false)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        if (!canMove && !captureMovement && !canJumpOverPieces)
            canMove = true;

        if (!canJumpOverPieces)
        {
            List<Vector2Int> filteDirectional = FilterDirectionalMoves(rawMoves);
            List<Vector2Int> validDirectional = GetValidDirectionalMoves(filteDirectional, canJumpOverPieces, captureMovement, canMove);
            validMoves.AddRange(validDirectional);

            List<Vector2Int> filterDiagonal = FilterDiagonalMoves(rawMoves);
            List<Vector2Int> validDiagonal = GetValidDiagonalMoves(filterDiagonal, canJumpOverPieces, captureMovement, canMove);
            validMoves.AddRange(validDiagonal);

            List<Vector2Int> filterUnclassified = FilterUnclassifiedMoves(rawMoves, filteDirectional, filterDiagonal);

            foreach (Vector2Int move in filterUnclassified)
            {
                // Inicialização
                AStarPathfinder pathfinder = new AStarPathfinder(gridManager);

                // Uso
                Vector2Int start = thisPiece.Position;
                Vector2Int end = move;
                int maxMovement = GetValueMovement(start, end);

                List<Vector2Int> path = pathfinder.FindPath(start, end, maxMovement);

                if (path.Count > 0)
                {
                    if (gridManager.IsHouseOccupied(move.x, move.y))
                    {
                        validMoves.Add(move);
                    }
                    else if (captureMovement && control)
                    {
                        validMoves.Add(move);
                    }
                    else if (canMove)
                    {
                        validMoves.Add(move);
                    }
                }
            }
        }
        else
        {

            foreach (Vector2Int move in rawMoves)
            {
                if (!gridManager.IsWithinBounds(move.x, move.y))
                    continue;

                if (gridManager.IsHouseOccupied(move.x, move.y))
                {
                    validMoves.Add(move);
                }
                else if (captureMovement && control)
                {
                    validMoves.Add(move);
                }
                else if (canMove)
                {
                    validMoves.Add(move);
                }

            }
        }





        return validMoves;
    }

    public int GetValueMovement(Vector2Int start, Vector2Int end)
    {
        int dx = Mathf.Abs(end.x - start.x); // Valor absoluto de dx
        int dy = Mathf.Abs(end.y - start.y); // Valor absoluto de dy

        return dx + dy; // Soma dos valores absolutos
    }

    //Find Paht A*
    public class AStarPathfinder
    {
        private BoardChessManager gridManager;

        public AStarPathfinder(BoardChessManager gridManager)
        {
            this.gridManager = gridManager;
        }

        private class Node
        {
            public Vector2Int position;
            public Node parent;
            public int gCost; // Custo do caminho desde o início
            public int hCost; // Heurística (distância até o destino)
            public int FCost => gCost + hCost;

            public Node(Vector2Int pos, Node parentNode, int g, int h)
            {
                position = pos;
                parent = parentNode;
                gCost = g;
                hCost = h;
            }
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, int maxMovement)
        {
            List<Node> openSet = new List<Node>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

            Node startNode = new Node(start, null, 0, CalculateH(start, end));
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = GetLowestFCostNode(openSet);

                // Verifica se atingiu o destino
                if (currentNode.position == end)
                {
                    return RetracePath(currentNode);
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode.position);

                // Verifica vizinhos
                foreach (Vector2Int neighbor in GetNeighbors(currentNode.position))
                {
                    if (closedSet.Contains(neighbor) ||
                        !gridManager.IsWithinBounds(neighbor.x, neighbor.y))
                        continue;

                    // Verifica se a casa está ocupada (exceto o destino final)
                    if (gridManager.IsHouseOccupied(neighbor.x, neighbor.y) && neighbor != end)
                        continue;

                    int newGCost = currentNode.gCost + 1; // Custo de cada movimento = 1

                    // Verifica se excede o movimento máximo permitido
                    if (newGCost > maxMovement)
                        continue;

                    Node neighborNode = openSet.Find(n => n.position == neighbor);

                    if (neighborNode == null)
                    {
                        neighborNode = new Node(neighbor, currentNode, newGCost, CalculateH(neighbor, end));
                        openSet.Add(neighborNode);
                    }
                    else if (newGCost < neighborNode.gCost)
                    {
                        neighborNode.gCost = newGCost;
                        neighborNode.parent = currentNode;
                    }
                }
            }

            return new List<Vector2Int>(); // Retorna lista vazia se não encontrar caminho
        }

        private Node GetLowestFCostNode(List<Node> nodes)
        {
            Node lowestNode = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].FCost < lowestNode.FCost ||
                   (nodes[i].FCost == lowestNode.FCost && nodes[i].hCost < lowestNode.hCost))
                {
                    lowestNode = nodes[i];
                }
            }
            return lowestNode;
        }

        private List<Vector2Int> RetracePath(Node endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Node currentNode = endNode;

            while (currentNode != null)
            {
                path.Add(currentNode.position);
                currentNode = currentNode.parent;
            }

            path.Reverse();
            return path;
        }

        private int CalculateH(Vector2Int a, Vector2Int b)
        {
            // Distância de Manhattan (para 4 direções)
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private List<Vector2Int> GetNeighbors(Vector2Int position)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            // Vizinhos em 4 direções (ortogonal)
            Vector2Int[] directions = {
            new Vector2Int(0, 1),   // Cima
            new Vector2Int(1, 0),   // Direita
            new Vector2Int(0, -1),  // Baixo
            new Vector2Int(-1, 0)   // Esquerda
        };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = position + dir;
                if (gridManager.IsWithinBounds(neighbor.x, neighbor.y))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }
    }
    //Find Paht A*


    public bool IsMoveOnlyInSpecial(int x, int y)
    {
        Vector2Int targetMove = new Vector2Int(x, y);

        List<Vector2Int> rawMoves = GetSpecialMovies();
        List<Vector2Int> rawMovesCustom = GetCustomMovies();

        // Se não estiver ativo ou não contiver o movimento, já retorna falso
        if (!configData.special.Active || !rawMoves.Contains(targetMove))
            return false;

        // Checar se está em outros movimentos
        if (configData.straight.Active && GetDirectionalMoves(configData.straight).Contains(targetMove))
            return false;

        if (configData.diagonal.Active && GetDiagonalMoves(configData.diagonal).Contains(targetMove))
            return false;

        if (configData.custom.Active && rawMovesCustom.Contains(targetMove))
            return false;

        // Está somente em special
        return true;
    }

}

