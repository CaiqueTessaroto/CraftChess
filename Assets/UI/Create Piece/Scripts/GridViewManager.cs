using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridViewManager : MonoBehaviour
{
    public GameObject gridCellWhite; // Prefab da célula branca
    public GameObject gridCellBlack; // Prefab da célula preta
    public Sprite spritePiece;
    public Movement movementConfig; // Configuração de movimento
    public int rows = 7; // Número de linhas
    public int cols = 7; // Número de colunas
    public MovementCreation movementCreation;

    private Dictionary<Vector2, GameObject> gridCells = new Dictionary<Vector2, GameObject>();

    private Dictionary<Vector2Int, CellMovementState> movementStates = new Dictionary<Vector2Int, CellMovementState>();
    public class CellMovementState
    {
        public bool Move;
        public bool Capture;
        public bool Jump;

        public CellChange changeMove = new CellChange();
        public CellChange changeCapture = new CellChange();
        public CellChange changeJump = new CellChange();
    }

    public class CellChange
    {
        public bool straight = false;
        public bool diagonal = false;
        public bool custom = false;
        public bool special = false;


        public void SetFlag(string type)
        {
            switch (type)
            {
                case "straight": straight = true; break;
                case "diagonal": diagonal = true; break;
                case "custom": custom = true; break;
                case "special": special = true; break;
            }
        }

        public void UnsetFlag(string type)
        {
            switch (type)
            {
                case "straight": straight = false; break;
                case "diagonal": diagonal = false; break;
                case "custom": custom = false; break;
                case "special": special = false; break;
            }
        }

        public bool IsFrom(string type)
        {
            return type switch
            {
                "straight" => straight,
                "diagonal" => diagonal,
                "custom" => custom,
                "special" => special,
                _ => false,
            };
        }

    }


    private bool gridbool = false;
    private GameObject newCell; // <- Adicionamos a declaração aqui
    private Vector2Int selectedPosition; // Posição da peça selecionada
    private GameObject thisObject;
    private Transform gridTransform;

    private bool wasStraightActive = false;
    private bool wasDiagonalActive = false;
    private bool wasCustomActive = false;
    private bool wasSpecialActive = false;

    private bool allowHighlightRefresh = false;


    [Header("Position Button:")]
    public Button upBtw;
    public Button downBtw;
    public Button rigthBtw;
    public Button leftBtw;
    public Button middleBtw;

    void Start()
    {

        upBtw.onClick.AddListener(() =>
        {
            selectedPosition = new Vector2Int(3, 6);
            RegenerateGrid();
            HighlightValidMoves();
        });
        downBtw.onClick.AddListener(() =>
        {
            selectedPosition = new Vector2Int(3, 0);
            RegenerateGrid();
            HighlightValidMoves();
        });
        rigthBtw.onClick.AddListener(() =>
        {
            selectedPosition = new Vector2Int(6, 3);
            RegenerateGrid();
            HighlightValidMoves();
        });
        leftBtw.onClick.AddListener(() =>
        {
            selectedPosition = new Vector2Int(0, 3);
            RegenerateGrid();
            HighlightValidMoves();
        });
        middleBtw.onClick.AddListener(() =>
        {
            selectedPosition = new Vector2Int(3, 3);
            RegenerateGrid();
            HighlightValidMoves();
        });




        gridTransform = transform;
        thisObject = gameObject;
        Image imagem = thisObject.GetComponent<Image>();
        imagem.enabled = false; // Desativando o componente Image

        var centerX = 3;
        var centerY = 3;
        selectedPosition = new Vector2Int(centerX, centerY);

        GenerateGrid();
    }

    void GenerateGrid()
    {

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                if (gridbool == false)
                {
                    //newCell = (x == selectedPosition.x && y == selectedPosition.y) ? Instantiate(gridCellPiece, gridTransform) : Instantiate(gridCellWhite, gridTransform);
                    newCell = Instantiate(gridCellWhite, gridTransform);
                    gridbool = true;
                }
                else
                {
                    //newCell = (x == selectedPosition.x && y == selectedPosition.y) ? Instantiate(gridCellPiece, gridTransform) : Instantiate(gridCellBlack, gridTransform);

                    newCell = Instantiate(gridCellBlack, gridTransform);
                    Image cellImage = newCell.GetComponent<Image>();
                    //Color newBlack = new Color32(50, 50, 50, 255);
                    cellImage.color = Color.black;

                    gridbool = false;
                }

                gridCells[cellPos] = newCell;

                if (x == selectedPosition.x && y == selectedPosition.y)
                {
                    //newCell = Instantiate(gridCellPiece, gridTransform);
                    SetSprite(newCell);

                }


                Button button = newCell.GetComponent<Button>();


                button.onClick.AddListener(() => ToggleCellSelection(cellPos));

            }
        }
    }

    public void SetSprite(GameObject cell)
    {
        // procura se já existe um filho chamado "Piece"
        Transform pieceTransform = cell.transform.Find("Piece");
        Image pieceImage;

        if (pieceTransform == null)
        {
            // cria um novo GameObject dentro da célula
            GameObject pieceGO = new GameObject("Piece", typeof(RectTransform), typeof(Image));

            // define como filho da célula
            pieceGO.transform.SetParent(cell.transform, false);

            float margin = 0f; // margem em pixels

            // ajusta o RectTransform para ocupar toda a célula
            RectTransform rt = pieceGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);       // distância da borda inferior/esquerda
            rt.offsetMax = new Vector2(-margin, -margin);     // distância da borda superior/direita

            // pega o componente Image recém-criado
            pieceImage = pieceGO.GetComponent<Image>();
        }
        else
        {
            // se já existe, só pega o Image
            pieceImage = pieceTransform.GetComponent<Image>();
        }

        // aplica o sprite da peça
        pieceImage.sprite = spritePiece;
        pieceImage.preserveAspect = true;


    }

    public void UpdateArtToGrid(Sprite sprite)
    {
        spritePiece = sprite;
        Vector2Int cellPos = new Vector2Int(selectedPosition.x, selectedPosition.y);
        SetSprite(gridCells[cellPos]);
    }

    public void RegenerateGrid()
    {
        // Remove todas as células antigas do grid
        foreach (Transform child in gridTransform)
        {
            Destroy(child.gameObject);
        }

        // Limpa o dicionário antes de recriar
        gridCells.Clear();

        // Reseta o bool para começar o padrão de cores novamente
        gridbool = false;

        // Chama a geração novamente
        GenerateGrid();
    }

    void ToggleCellSelection(Vector2Int cellPos)
    {
        //    Debug.Log("cellPos: " + cellPos);
        selectedPosition = new Vector2Int(cellPos.x, cellPos.y);
        RegenerateGrid();
        HighlightValidMoves();
    }


    public void HighlightValidMoves()
    {

        // Resetar as cores do grid
        foreach (var cell in gridCells)
        {
            Image cellImage = cell.Value.GetComponent<Image>();
            cellImage.color = cellImage.name.Contains("Black") ? Color.black : Color.white;
        }

        // Atribuições locais para facilitar
        var straight = movementCreation.straight;
        var diagonal = movementCreation.diagonal;
        var custom = movementCreation.custom;
        var special = movementCreation.special;

        List<Vector2Int> validMoves;


        // Custom
        if (custom.Active)
        {
            if (!allowHighlightRefresh)
                ClearMovementFlagsFromSource("custom");

            validMoves = movementCreation.GetCustomMoves(selectedPosition);
            HighlightCustomGrid(custom, validMoves, "custom");
            wasCustomActive = true;

        }
        else if (wasCustomActive && !allowHighlightRefresh)
        {
            ClearMovementFlagsFromSource("custom");
            wasCustomActive = false;
        }

        // Special
        if (special.Active)
        {
            if (!allowHighlightRefresh)
                ClearMovementFlagsFromSource("special");

            validMoves = movementCreation.GetspecialMoves(selectedPosition);
            HighlightSpecialGrid(special, validMoves, "special");
            wasSpecialActive = true;
        }
        else if (wasSpecialActive && !allowHighlightRefresh)
        {
            ClearMovementFlagsFromSource("special");
            wasSpecialActive = false;
        }






        // Straight
        if (straight.Active)
        {
            if (!allowHighlightRefresh)
                ClearMovementFlagsFromSource("straight");

            validMoves = movementCreation.GetStraightMoves(selectedPosition);
            HighlightGrid(straight, validMoves, "straight");
            wasStraightActive = true;
        }
        else if (wasStraightActive && !allowHighlightRefresh)
        {
            ClearMovementFlagsFromSource("straight");
            wasStraightActive = false;
        }

        // Diagonal
        if (diagonal.Active)
        {
            if (!allowHighlightRefresh)
                ClearMovementFlagsFromSource("diagonal");

            validMoves = movementCreation.GetDiagonalMoves(selectedPosition);
            HighlightGrid(diagonal, validMoves, "diagonal");
            wasDiagonalActive = true;
        }
        else if (wasDiagonalActive && !allowHighlightRefresh)
        {
            ClearMovementFlagsFromSource("diagonal");
            wasDiagonalActive = false;
        }


        movementCreation.CalcularPoderTotal();


    }

    public void ClearMovementFlagsFromSource(string sourceType)
    {
        List<Vector2Int> keysToRemove = new List<Vector2Int>();

        foreach (var kvp in movementStates)
        {
            var state = kvp.Value;

            // Move
            if (state.changeMove.IsFrom(sourceType))
            {
                state.Move = false;
                state.changeMove.UnsetFlag(sourceType);
            }

            // Capture
            if (state.changeCapture.IsFrom(sourceType))
            {
                state.Capture = false;
                state.changeCapture.UnsetFlag(sourceType);
            }

            // Jump
            if (state.changeJump.IsFrom(sourceType))
            {
                state.Jump = false;
                state.changeJump.UnsetFlag(sourceType);
            }

            // Limpa se nada mais estiver ativo
            if (!state.Move && !state.Capture && !state.Jump)
            {
                keysToRemove.Add(kvp.Key);
                if (gridCells.ContainsKey(kvp.Key))
                {
                    gridCells[kvp.Key].GetComponent<Image>().color = Color.white;
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            movementStates.Remove(key);
        }

        // ✅ Temporariamente desativa o refresh automático para evitar loop
        allowHighlightRefresh = true;
        HighlightValidMoves();
        allowHighlightRefresh = false;
    }


    public void ClearHighlights()
    {
        foreach (var cellPair in gridCells)
        {
            cellPair.Value.GetComponent<Image>().color = Color.white;
        }

        movementStates.Clear();
    }

    public void HighlightGrid(Movement movement, List<Vector2Int> validMoves, string sourceType)
    {
        foreach (Vector2Int move in validMoves)
        {
            if (!movementStates.ContainsKey(move))
            {
                movementStates[move] = new CellMovementState();
            }

            // Acumula os estados
            var state = movementStates[move];
            // Atualiza MOVE se a flag for da mesma origem ou ainda não setada
            if (!state.Move || state.changeMove.IsFrom(sourceType))
            {
                state.Move = movement.Move;

                if (movement.Move)
                    state.changeMove.SetFlag(sourceType);
            }

            // Atualiza CAPTURE
            if (!state.Capture || state.changeCapture.IsFrom(sourceType))
            {
                state.Capture = movement.Capture;

                if (movement.Capture)
                    state.changeCapture.SetFlag(sourceType);
            }

            // Atualiza JUMP
            if (!state.Jump || state.changeJump.IsFrom(sourceType))
            {
                state.Jump = movement.Jump;

                if (movement.Jump)
                    state.changeJump.SetFlag(sourceType);
            }

            // Atualiza a cor da célula
            if (gridCells.ContainsKey(move))
            {
                GameObject cell = gridCells[move];
                Image cellImage = cell.GetComponent<Image>();

                // Decidir a cor com base na combinação dos estados acumulados
                if (state.Jump && state.Capture)
                {
                    cellImage.color = Color.magenta;
                }
                else if (state.Move && state.Capture)
                {
                    cellImage.color = Color.yellow;
                }
                else if (state.Jump)
                {
                    cellImage.color = Color.blue;
                }
                else if (state.Capture)
                {
                    cellImage.color = Color.red;
                }
                else if (state.Move)
                {
                    cellImage.color = Color.green;
                }
            }
        }
    }


    public void HighlightCustomGrid(PersonalizedMove custom, List<Vector2Int> validMoves, string sourceType)
    {

        foreach (Vector2Int move in validMoves)
        {
            if (!movementStates.ContainsKey(move))
            {
                movementStates[move] = new CellMovementState();
            }

            // Acumula os estados
            var state = movementStates[move];
            // Atualiza MOVE se a flag for da mesma origem ou ainda não setada
            if (!state.Move || state.changeMove.IsFrom(sourceType))
            {
                state.Move = custom.Move;

                if (custom.Move)
                    state.changeMove.SetFlag(sourceType);
            }

            // Atualiza CAPTURE
            if (!state.Capture || state.changeCapture.IsFrom(sourceType))
            {
                state.Capture = custom.Capture;

                if (custom.Capture)
                    state.changeCapture.SetFlag(sourceType);
            }

            // Atualiza JUMP
            if (!state.Jump || state.changeJump.IsFrom(sourceType))
            {
                state.Jump = custom.Jump;

                if (custom.Jump)
                    state.changeJump.SetFlag(sourceType);
            }

            // Atualiza a cor da célula
            if (gridCells.ContainsKey(move))
            {
                GameObject cell = gridCells[move];
                Image cellImage = cell.GetComponent<Image>();

                // Decidir a cor com base na combinação dos estados acumulados
                if (state.Jump && state.Capture)
                {
                    cellImage.color = Color.magenta;
                }
                else if (state.Move && state.Capture)
                {
                    cellImage.color = Color.yellow;
                }
                else if (state.Jump)
                {
                    cellImage.color = Color.blue;
                }
                else if (state.Capture)
                {
                    cellImage.color = Color.red;
                }
                else if (state.Move)
                {
                    cellImage.color = Color.green;
                }
            }
        }
    }

    public void HighlightSpecialGrid(Special special, List<Vector2Int> validMoves, string sourceType)
    {
        foreach (Vector2Int move in validMoves)
        {
            if (!movementStates.ContainsKey(move))
            {
                movementStates[move] = new CellMovementState();
            }

            // Acumula os estados
            var state = movementStates[move];
            // Atualiza MOVE se a flag for da mesma origem ou ainda não setada
            if (!state.Move || state.changeMove.IsFrom(sourceType))
            {
                state.Move = special.Move;

                if (special.Move)
                    state.changeMove.SetFlag(sourceType);
            }

            // Atualiza CAPTURE
            if (!state.Capture || state.changeCapture.IsFrom(sourceType))
            {
                state.Capture = special.Capture;

                if (special.Capture)
                    state.changeCapture.SetFlag(sourceType);
            }

            // Atualiza JUMP
            if (!state.Jump || state.changeJump.IsFrom(sourceType))
            {
                state.Jump = special.Jump;

                if (special.Jump)
                    state.changeJump.SetFlag(sourceType);
            }

            // Atualiza a cor da célula
            if (gridCells.ContainsKey(move))
            {
                GameObject cell = gridCells[move];
                Image cellImage = cell.GetComponent<Image>();


                if (state.Jump && state.Capture)
                {
                    cellImage.color = new Color(1f, 0f, 1f, 0.3f); // magenta mais fraco
                }
                else if (state.Move && state.Capture)
                {
                    cellImage.color = new Color(1f, 1f, 0f, 0.3f); // amarelo mais fraco
                }
                else if (state.Jump)
                {
                    cellImage.color = new Color(0f, 0f, 1f, 0.3f); // azul mais fraco
                }
                else if (state.Capture)
                {
                    cellImage.color = new Color(1f, 0f, 0f, 0.3f); // vermelho mais fraco
                }
                else if (state.Move)
                {
                    cellImage.color = new Color(0f, 1f, 0f, 0.3f); // verde mais fraco
                }

                // Decidir a cor com base na combinação dos estados acumulados
                /*
                if (state.Jump && state.Capture)
                {
                    cellImage.color = Color.magenta;
                }
                else if (state.Move && state.Capture)
                {
                    cellImage.color = Color.yellow;
                }
                else if (state.Jump)
                {
                    cellImage.color = Color.blue;
                }
                else if (state.Capture)
                {
                    cellImage.color = Color.red;
                }
                else if (state.Move)
                {
                    cellImage.color = Color.green;
                }
                */
            }
        }
    }



}
