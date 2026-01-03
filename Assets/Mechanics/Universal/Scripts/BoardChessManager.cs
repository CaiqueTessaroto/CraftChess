using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



public class BoardChessManager : MonoBehaviour
{
    public CapturedPiecesManager capturedManager;

    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 1f;

    [Header("References")]
    public GameObject cellPrefab;
    public GameObject piecePrefab;
    public GameObject selectionPrefab;
    public GameObject IdentificationPrefab;

    [Header("Colors")]
    public Color colorA = Color.white;
    public Color colorB = Color.gray;
    public Color selectedColor = Color.blue;

    [Header("Options")]
    public bool inBlackView = false;
    public bool noRules = false;
    public bool freeMode = true;
    public bool localGame = true;

    [Header("Control")]
    public bool infoPiece = false;
    public bool WhiteHasMoves = true;
    public bool BlackHasMoves = true;
    private GameObject currentSelection;

    private GameObject currentOrigin;
    private GameObject currentTarget;

    private Vector2Int originInt;
    private Vector2Int targetInt;

    [Header("Data")]
    public List<MatchSquadData> Squads = new List<MatchSquadData>();
    public List<GameObject> AllPieces = new List<GameObject>();
    public List<GameObject> WhitePieces = new List<GameObject>();
    public List<GameObject> BlackPieces = new List<GameObject>();

    public List<House> BoardHouses = new List<House>();
    //public House[,] BoardHouses = new House[8, 8];
    // Armazena as células do grid
    public GameObject[,] gridCells;

    public ManagerPieceInfo managerPieceInfo;


    void Start()
    {

        infoPiece = false;
        
        if (managerPieceInfo == null)
            managerPieceInfo = FindObjectOfType<ManagerPieceInfo>();

        if (capturedManager == null)
            capturedManager = FindObjectOfType<CapturedPiecesManager>();

        //Debug.Log("Mapa: " + MatchData.Instance.mapName);
        //Debug.Log("Esquadrão do Jogador: " + MatchData.Instance.userSquadName);
        //Debug.Log("Dificuldade: " + MatchData.Instance.botDifficulty);
        //Debug.Log("Quem começa: " + MatchData.Instance.whoStarts);


        //var squad = MatchData.Instance.yourSquad;
        //var sprite = MatchData.Instance.yourPieceSprites["Rei"];
        //var pieceData = MatchData.Instance.yourPieces["Rei"];
        //var enemyData = MatchData.Instance.enemySquad;

        //managerPieceInfo.pieceSprites.AddRange(MatchData.Instance.Squads[0].Sprites);
        //managerPieceInfo.pieceSprites.AddRange(MatchData.Instance.Squads[1].Sprites);

        //Debug.Log(pieceData);

        if (cellPrefab == null)
        {
            Debug.LogError("Cell prefab não atribuído no GridManager!");
            return;
        }

        //Squad = MatchData.Instance.Squad;
        //BotSquad = MatchData.Instance.BotSquad;


        Squads = MatchData.Instance.Squads;

        if (Squads[0].Player.name == "Bot")
            GenerateGrid_reverse();
        else
            GenerateGrid();

        foreach (var squad in Squads)
        {
            managerPieceInfo.pieceSprites.AddRange(squad.Sprites);

            LoadSquadPieces(squad);
        }


        StartCoroutine(AfterStart());


        if (MatchData.Instance.botDifficulty == BotDifficulty.Easy)
        {
            IAn1 iA = FindObjectOfType<IAn1>();
            iA.enabled = true;
        }
        else if (MatchData.Instance.botDifficulty == BotDifficulty.Medium)
        {
            IAn2 iA = FindObjectOfType<IAn2>();
            iA.enabled = true;
        }
        else
        {
            IAn3 iA = FindObjectOfType<IAn3>();
            iA.enabled = true;
        }

    }

    public int GetBotId()
    {
        int id;


        Squads = MatchData.Instance.Squads;

        if (Squads[0].Player.name == "Bot")
        {
            id = 0;
        }
        else
        {
            id = 1;
        }

        return id;

    }

    IEnumerator AfterStart()
    {
        yield return null; // espera 1 frame
        UpdateBoardControl();
        UpdateMoves();
    }

    public void SwitchSide()
    {
        if (inBlackView)
            GenerateGrid();
        else
            GenerateGrid_reverse();

        // Atualiza posições das peças de acordo com o novo grid
        foreach (GameObject piece in AllPieces)
        {
            PieceComponent component = piece.GetComponent<PieceComponent>();
            Vector2Int origin = component.Position;

            // Se estiver na visão preta, espelha as coordenadas
            //Vector2Int mirroredPos = inBlackView
            //    ? new Vector2Int(gridWidth - 1 - origin.x, gridHeight - 1 - origin.y)
            //    : origin;

            GameObject targetCell = GetCellAtPosition(origin.x, origin.y);

            if (targetCell != null)
            {
                piece.transform.SetParent(targetCell.transform);
                piece.transform.localPosition = Vector3.zero;
            }
        }

        HighlightLastMove(originInt, targetInt);

    }


    /// <summary>
    /// Gera o grid 2D com alternância de cores.
    /// </summary>
    private void GenerateGrid()
    {
        inBlackView = false;

        foreach (Transform child in transform)
            Destroy(child.gameObject);

        BoardHouses.Clear();

        transform.position = Vector3.zero;

        gridCells = new GameObject[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)        // Começa pelo eixo Y inferior
        {
            for (int y = 0; y < gridHeight; y++)    // Vai da esquerda para direita
            {
                GameObject newCell = Instantiate(cellPrefab, transform);
                //string cellName = $"{(char)('A' + x)}{y + 1}";
                newCell.name = $"Cell ({x},{y})";


                // Posição: X = horizontal, Y = vertical
                //newCell.transform.position = new Vector2(y * cellSize, x * cellSize);
                newCell.transform.position = new Vector2(x * cellSize, y * cellSize);

                // Alterna cores
                SpriteRenderer sr = newCell.GetComponent<SpriteRenderer>();

                bool isEven = (x + y) % 2 == 0;
                if (sr != null)
                {
                    sr.color = isEven ? colorA : colorB;
                    sr.sortingOrder = 1;
                }

                // Configura o script Cell
                Cell cellComp = newCell.GetComponent<Cell>();

                if (cellComp == null)
                    cellComp = newCell.AddComponent<Cell>();

                //cellComp.gridPosition = new Vector2Int(x, y);
                cellComp.gridManager = this;

                string letter = $"{(char)('a' + x)}";
                string number = $"{y + 1}";
                string house = $"{letter}{number}";

                cellComp.house = new House(house, new Vector2Int(x, y));
                BoardHouses.Add(cellComp.house);

                gridCells[x, y] = newCell;

                Color iColor = isEven ? colorB : colorA;
                CreateIdentification(newCell, new Vector2Int(x, y), iColor);
            }
        }

        // Centraliza o grid no meio da tela
        transform.position = new Vector3(-gridWidth * cellSize / 2f + cellSize / 2f,
                                         -gridHeight * cellSize / 2f + cellSize / 2f,
                                         0f);

        capturedManager.CreateReferenceAreas(gridWidth, gridHeight, cellSize, false);
    }

    private void GenerateGrid_reverse()
    {
        inBlackView = true;
        // Remove células antigas
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        BoardHouses.Clear();

        transform.position = Vector3.zero;

        gridCells = new GameObject[gridWidth, gridHeight];

        for (int x = 0; x < gridHeight; x++) // linhas (vertical)
        {
            for (int y = 0; y < gridWidth; y++) // colunas (horizontal)
            {
                GameObject newCell = Instantiate(cellPrefab, transform);
                newCell.name = $"Cell ({x},{y})";

                // Inversão total (X e Y)
                float invertedX = (gridWidth - 1 - x) * cellSize;
                float invertedY = (gridHeight - 1 - y) * cellSize;

                newCell.transform.position = new Vector2(invertedX, invertedY);

                // Alterna as cores
                SpriteRenderer sr = newCell.GetComponent<SpriteRenderer>();

                bool isEven = (x + y) % 2 == 0;
                if (sr != null)
                {
                    sr.color = isEven ? colorA : colorB;
                    sr.sortingOrder = 1;
                }

                // Configura o script Cell
                Cell cellComp = newCell.GetComponent<Cell>();
                if (cellComp == null)
                    cellComp = newCell.AddComponent<Cell>();

                // Guarda a posição lógica (não invertida)
                //cellComp.gridPosition = new Vector2Int(x, y);
                cellComp.gridManager = this;

                string letter = $"{(char)('a' + x)}";
                string number = $"{y + 1}";
                string house = $"{letter}{number}";

                cellComp.house = new House(house, new Vector2Int(x, y));
                BoardHouses.Add(cellComp.house);

                gridCells[x, y] = newCell;

                Color iColor = isEven ? colorB : colorA;
                CreateIdentification(newCell, new Vector2Int(x, y), iColor, true);
            }
        }

        // Centraliza o grid no meio da tela
        transform.position = new Vector3(
            -gridWidth * cellSize / 2f + cellSize / 2f,
            -gridHeight * cellSize / 2f + cellSize / 2f,
            0f
        );

        capturedManager.CreateReferenceAreas(gridWidth, gridHeight, cellSize, true);
    }

    public void AddCapturedPiece(GameObject capturedPieceObject, int capturedBy)
    {
        capturedManager.AddCapturedPiece(capturedPieceObject, capturedBy);
    }

    public void CreateIdentification(GameObject newCell, Vector2Int posCell, Color color, bool isReverse = false)
    {
        int pos = 0;
        if (isReverse)
            pos = 7;

        if (posCell.x == pos)
        {
            GameObject newIdentification = Instantiate(IdentificationPrefab, newCell.transform);

            newIdentification.transform.localPosition = new Vector3(-0.3f, 0.3f, 0f);
            //newIdentification.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            string number = $"{posCell.y + 1}";

            Sprite sprite = Resources.Load<Sprite>("Sprites/Houses/Numbers/" + number);
            SpriteRenderer sr = newIdentification.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.sortingOrder = 2;
                sr.color = color;
            }
        }

        if (posCell.y == pos)
        {
            GameObject newIdentification = Instantiate(IdentificationPrefab, newCell.transform);

            newIdentification.transform.localPosition = new Vector3(0.3f, -0.3f, 0f);
            //newIdentification.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            string letter = $"{(char)('A' + posCell.x)}";

            Sprite sprite = Resources.Load<Sprite>("Sprites/Houses/Letters/" + letter);
            SpriteRenderer sr = newIdentification.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.sortingOrder = 2;
                sr.color = color;
            }
        }

    }

    public void HighlightSelect(GameObject newCell)
    {
        DestroyIfExists(currentSelection);
        currentSelection = CreateOverlay(newCell, selectedColor);
        //lastSelectedCell = newCell;
    }

    public void HighlightLastMove(Vector2Int origin, Vector2Int target)
    {
        DestroyIfExists(currentSelection);
        DestroyIfExists(currentOrigin);
        DestroyIfExists(currentTarget);

        originInt = origin;
        targetInt = target;

        GameObject originCell = GetCellAtPosition(origin.x, origin.y);
        GameObject targetCell = GetCellAtPosition(target.x, target.y);

        currentOrigin = CreateOverlay(originCell, selectedColor);
        currentTarget = CreateOverlay(targetCell, selectedColor);
    }

    private GameObject CreateOverlay(GameObject parent, Color color)
    {
        if (selectionPrefab == null)
        {
            Debug.LogWarning("selectionPrefab não atribuído no GridManager!");
            return null;
        }

        GameObject overlay = Instantiate(selectionPrefab, parent.transform);
        overlay.name = "Overlay";
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localScale = Vector3.one;

        SpriteRenderer sr = overlay.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 3;
            sr.color = color;
        }

        return overlay;
    }

    private void DestroyIfExists(GameObject obj)
    {
        if (obj != null)
            Destroy(obj);
    }

    /// Retorna o GameObject da célula na posição especificada.
    public GameObject GetCellAtPosition(int x, int y)
    {
        if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight)
            return null;

        return gridCells[x, y];
    }

    public bool IsHouseOccupied(int x, int y)
    {
        if (IsWithinBounds(x, y))
        {
            //if (GetPieceAtPosition(x, y) == null)
            //    return false;
            GameObject GameObject_Cell = GetCellAtPosition(x, y);
            Cell cell = GameObject_Cell.GetComponent<Cell>();
            if (!cell.house.isOccupied)
                return false;

            return true;
        }
        return true; // Fora dos limites é considerado ocupado
    }

    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    public GameObject GetPieceAtPosition(int x, int y)
    {
        // Garante que está dentro dos limites
        if (!IsWithinBounds(x, y))
            return null;

        GameObject cell = GetCellAtPosition(x, y);
        if (cell == null)
            return null;

        // Verifica todos os filhos da célula
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            Transform child = cell.transform.GetChild(i);

            // Ignora o overlay de seleção
            if (child.name == "Overlay" || child.name == "Identification(Clone)")
                continue;

            // Retorna o primeiro filho válido (presumindo que só há uma peça)
            return child.gameObject;
        }

        // Nenhuma peça encontrada
        return null;
    }



    // ---------- INSTANCIA AS PEÇAS ----------
    private void LoadSquadPieces(MatchSquadData matchSquad)
    {
        Squad squad = matchSquad.Data;
        Dictionary<string, Sprite> sprites = matchSquad.Sprites;

        foreach (var piece in squad.Units)
        {
            Vector2Int pos = piece.Position;

            if (matchSquad.Player.id == 1)
            {
                // Inverte as coordenadas para o outro time
                pos = new Vector2Int(pos.x, gridHeight - 1 - pos.y);
            }

            if (sprites.TryGetValue(piece.Name, out Sprite sprite))
            {
                PlacePiece(piece.Name, sprite, pos, matchSquad);
            }
            else
            {
                Debug.LogWarning($"Sprite não encontrado para a peça: {piece.Name}");
            }
        }
    }

    // ---------- POSICIONA UMA PEÇA ----------
    public GameObject PlacePiece(string name, Sprite sprite, Vector2Int pos, MatchSquadData matchSquad)
    {
        GameObject cell = GetCellAtPosition(pos.x, pos.y);
        if (cell == null)
        {
            Debug.LogWarning($"Célula inválida para a peça {name} na posição {pos}");
            return null;
        }

        // Instancia a peça dentro da célula
        GameObject pieceObj = Instantiate(piecePrefab, cell.transform);
        pieceObj.name = name;
        // Centraliza dentro da célula
        pieceObj.transform.localPosition = Vector3.zero;

        // 🔹 Zera a rotação herdada
        //pieceObj.transform.localRotation = Quaternion.identity;
        pieceObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);//-90f

        LoadPieceData(name, pieceObj, pos, matchSquad);

        // Opcional: garante escala 1
        pieceObj.transform.localScale = Vector3.one;

        SpriteRenderer cellSR = cell.GetComponent<SpriteRenderer>();
        SpriteRenderer pieceSR = pieceObj.GetComponent<SpriteRenderer>();

        if (pieceSR != null)
        {
            pieceSR.sprite = sprite;
            pieceSR.sortingOrder = 5;

            // 🔥 Ajusta automaticamente o tamanho da peça para caber na célula
            if (cellSR != null && cellSR.sprite != null)
            {
                // Tamanho físico do sprite (em unidades)
                Vector2 cellSize = cellSR.sprite.bounds.size;
                Vector2 pieceSize = pieceSR.sprite.bounds.size;

                // Calcula a escala necessária
                float scaleX = cellSize.x / pieceSize.x;
                float scaleY = cellSize.y / pieceSize.y;

                // Mantém proporção (usa o menor fator)
                float scale = Mathf.Min(scaleX, scaleY);

                pieceObj.transform.localScale = new Vector3(scale, scale, 1);
            }
            else
            {
                // fallback caso a célula não tenha SpriteRenderer
                pieceObj.transform.localScale = Vector3.one * 0.8f;
            }
        }

        Cell cellComp = cell.GetComponent<Cell>();
        cellComp.house.Piece = name;
        cellComp.house.isOccupied = true;

        AllPieces.Add(pieceObj);

        return pieceObj;
    }

    private void LoadPieceData(string name, GameObject pieceObj, Vector2Int pos, MatchSquadData matchSquad)
    {

        Dictionary<string, MovementConfigData> yourPieces = matchSquad.Pieces;
        //Dictionary<string, MovementConfigData> enemyPieces = MatchData.Instance.enemyPieces;

        PieceMovement movementScript = pieceObj.AddComponent<PieceMovement>();
        if (yourPieces.TryGetValue(name, out MovementConfigData data))
        {
            if (movementScript != null)
                movementScript.LoadConfigFromJson(data);
        }

        Squad squad = matchSquad.Data;
        SquadPieceData pieceData = squad.Pieces.Find(p => p.NameInSquad == name);

        PieceComponent pieceComponent = pieceObj.AddComponent<PieceComponent>();

        Player player = matchSquad.Player;

        if (pieceData.NameInSquad == squad.King.Name)
        {
            pieceComponent.Initialize(pieceData.Squad, name, pieceData.Power, pieceData.PromotionPieces, pieceData.CastlingPieces, player, pos, true);

            PieceController pieceController = FindObjectOfType<PieceController>();

            if (player.id == 0)
                pieceController.KingWhite = pieceComponent;
            else
                pieceController.KingBlack = pieceComponent;

        }
        else
            pieceComponent.Initialize(pieceData.Squad, name, pieceData.Power, pieceData.PromotionPieces, pieceData.CastlingPieces, player, pos, false);

    }



    public void UpdatePiecePosition(Vector2Int origin, Vector2Int targetPosition, string name)
    {
        GameObject cell = GetCellAtPosition(origin.x, origin.y);
        Cell cellComp = cell.GetComponent<Cell>();
        cellComp.house.isOccupied = false;
        cellComp.house.Piece = null;

        cell = GetCellAtPosition(targetPosition.x, targetPosition.y);
        cellComp = cell.GetComponent<Cell>();
        cellComp.house.isOccupied = true;
        cellComp.house.Piece = name;

    }







    public void UpdateBoardControl()
    {
        // 1️⃣ Limpa todos os controles antigos
        foreach (var house in BoardHouses)
        {
            house.isControlledByWhite = false;
            house.isControlledByBlack = false;
            house.WhitePiecesControl.Clear();
            house.BlackPiecesControl.Clear();
            house.isOccupied = false;
        }

        foreach (var house in BoardHouses)
        {
            Cell cellComp = gridCells[house.Position.x, house.Position.y].GetComponent<Cell>();
            if (GetPieceAtPosition(house.Position.x, house.Position.y))
                cellComp.house.isOccupied = true;
        }

        // 2️⃣ Atualiza o controle com base nas peças
        foreach (GameObject piece in AllPieces)
        {
            if (piece == null)
                continue;

            PieceComponent component = piece.GetComponent<PieceComponent>();
            PieceMovement movement = piece.GetComponent<PieceMovement>();

            if (component == null || movement == null)
                continue;

            List<Vector2Int> controlledMoves = movement.GetValidCaptureMoves(true);
            //List<Vector2Int> controlledMoves = movement.GetValidMoves();
            //component.PossibleMoves = movement.GetValidMoves(false, false);

            if (controlledMoves == null)
                continue;

            foreach (Vector2Int move in controlledMoves)
            {
                if (!IsWithinBounds(move.x, move.y))
                    continue;

                Cell cellComp = gridCells[move.x, move.y].GetComponent<Cell>();

                //if (GetPieceAtPosition(move.x, move.y))
                //    cellComp.house.isOccupied = true;

                if (component.Player.id == 0)
                {
                    cellComp.house.isControlledByWhite = true;
                    cellComp.house.WhitePiecesControl.Add(component);
                }
                else
                {
                    cellComp.house.isControlledByBlack = true;
                    cellComp.house.BlackPiecesControl.Add(component);
                }
            }

        }

    }

    public void UpdateMoves()
    {
        WhiteHasMoves = false;
        BlackHasMoves = false;

        WhitePieces.Clear();
        BlackPieces.Clear();

        foreach (GameObject piece in AllPieces)
        {
            if (piece == null)
                continue;

            PieceComponent component = piece.GetComponent<PieceComponent>();
            PieceMovement movement = piece.GetComponent<PieceMovement>();

            if (component == null || movement == null)
                continue;

            component.PossibleMoves = movement.GetValidMoves();

            if (!WhiteHasMoves || !BlackHasMoves)
                if (component.Player.id == 0)
                {
                    if (component.PossibleMoves.Count != 0)
                        WhiteHasMoves = true;
                }
                else
                {
                    if (component.PossibleMoves.Count != 0)
                        BlackHasMoves = true;
                }

            if (component.Player.id == 0)
                WhitePieces.Add(piece);
            else
                BlackPieces.Add(piece);

        }
    }







}








