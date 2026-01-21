using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;


public class PaintingGridManager : MonoBehaviour
{
    public GameObject gridCell; // Prefab da célula
    public int rows = 17; // Número de linhas
    public int cols = 17; // Número de colunas
    public Dictionary<Vector2Int, GameObject> gridCells = new Dictionary<Vector2Int, GameObject>();
    private bool gridGenerated = false;
    public int currentScale = 1;
    public TextMeshProUGUI scaleText;
    public bool isUpscaled = false;

    [Header("Scripts:")]
    public GridStateManager gridStateManager;
    public GridPreview gridPreview;
    public FileManager fileManager;
    public PaitingToolsManager paitingToolsManager;
    public ColorPickerManager colorPickerManager;

    [Header("Color Control:")]
    public Color baseGridColor;
    public Transform parentContainer;
    public Color selectedColor = Color.white;
    public GameObject selectedButtonColor;
    public Image previewSelectColor;

    [Header("Buttons:")]
    public Button UndoButton;

    [Header("Position Button:")]
    public Button upBtw;
    public Button downBtw;
    public Button rigthBtw;
    public Button leftBtw;
    public Button middleBtw;

    [Header("Tool Control:")]
    public bool eyedropperMode = false;
    public bool paintBucketMode = false;
    public bool eraserMode = false;
    public bool circleMode = false;
    public bool lineMode = false;
    public bool rectMode = false;
    public bool shadowMode = false;
    public bool eraseAll = false;
    public bool isPainting = false;

    [Header("Selection Control:")]
    public bool OnSelecting = false;
    public bool isSelecting = false;
    public bool isMoveSelection = false;
    public Vector2Int? selectionStart = null;
    public List<PixelCellPainter> selectedCells = new List<PixelCellPainter>();
    private Dictionary<PixelCellPainter, Color> originalColors = new Dictionary<PixelCellPainter, Color>();

    [Header("Geometry Control:")]
    public Vector2Int? circleStart = null;
    public Vector2Int? lineStart = null;
    public Vector2Int? rectStart = null;

    private Vector2 originalCellSize;
    private Vector2 originalSpacing;

    private bool isScalingLocked = false;
    void Start()
    {

        //float targetAlpha = 60f / 255f;
        //baseGridColor = new Color(1, 1, 1, targetAlpha);

        UndoButton.onClick.AddListener(() =>
        {

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            gridStateManager.Undo();

        });

        if (fileManager == null)
            fileManager = FindObjectOfType<FileManager>();


        if (gridPreview == null)
            gridPreview = FindObjectOfType<GridPreview>();

        if (colorPickerManager == null)
            colorPickerManager = FindObjectOfType<ColorPickerManager>();


        if (gridStateManager == null)
            gridStateManager = FindObjectOfType<GridStateManager>();


        if (paitingToolsManager == null)
            paitingToolsManager = FindObjectOfType<PaitingToolsManager>();


        upBtw.onClick.AddListener(() =>
        {
            if (!isMoveSelection)
                return;

            if (selectedCells.Count == 0)
                return;

            Vector2Int moveOffset = Vector2Int.zero;
            moveOffset = Vector2Int.up;
            MoveSelectionStep(moveOffset);

        });
        downBtw.onClick.AddListener(() =>
        {
            if (!isMoveSelection)
                return;

            if (selectedCells.Count == 0)
                return;

            Vector2Int moveOffset = Vector2Int.zero;
            moveOffset = Vector2Int.down;
            MoveSelectionStep(moveOffset);

        });
        rigthBtw.onClick.AddListener(() =>
        {
            if (!isMoveSelection)
                return;

            if (selectedCells.Count == 0)
                return;

            Vector2Int moveOffset = Vector2Int.zero;
            moveOffset = Vector2Int.right;
            MoveSelectionStep(moveOffset);
        });
        leftBtw.onClick.AddListener(() =>
        {
            if (!isMoveSelection)
                return;

            if (selectedCells.Count == 0)
                return;

            Vector2Int moveOffset = Vector2Int.zero;
            moveOffset = Vector2Int.left;
            MoveSelectionStep(moveOffset);

        });
        middleBtw.onClick.AddListener(() =>
        {
            DisableTools();
        });

        GridLayoutGroup layout = parentContainer.GetComponent<GridLayoutGroup>();

        originalCellSize = layout.cellSize;
        originalSpacing = layout.spacing;

        GenerateGrid();
    }

    void Update()
    {

        if (isPainting)
        {
            if (Input.GetMouseButtonUp(0))
            {
                isPainting = false;
            }
        }

        if (isMoveSelection)
        {

            if (selectedCells.Count == 0) return;

            Vector2Int moveOffset = Vector2Int.zero;

            // Detecta as teclas pressionadas
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                moveOffset = Vector2Int.up;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                moveOffset = Vector2Int.down;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                moveOffset = Vector2Int.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                moveOffset = Vector2Int.right;

            // Se alguma tecla foi pressionada, move
            if (moveOffset != Vector2Int.zero)
            {
                MoveSelectionStep(moveOffset);
            }
            //if (Input.GetMouseButtonUp(0))

            //if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            //    ClearSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            DisableTools();

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                gridStateManager.Undo();
            }
        }

    }

    public void DisableTools()
    {
        if (OnSelecting)
            ClearSelection();

        paitingToolsManager.DisableAllImages();

        if (eraseAll || eraserMode)
            selectedColor = colorPickerManager.currentColor;


        eyedropperMode = false;
        paintBucketMode = false;
        circleMode = false;
        lineMode = false;
        rectMode = false;
        shadowMode = false;
        eraserMode = false;
        eraseAll = false;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

    }


    public void SaveStateForUndo()
    {
        gridStateManager.SaveStateForUndo();
    }

    public void GenerateGrid()
    {
        if (gridGenerated) return; // Já foi gerado, não faz nada

        gridGenerated = true;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GameObject newCell = Instantiate(gridCell, parentContainer);

                Vector2Int cellPos = new(x, y);

                gridCells[cellPos] = newCell;

                PixelCellPainter painter = newCell.GetComponent<PixelCellPainter>();

                if (painter != null)
                {
                    painter.Init(cellPos, this);
                }

            }
        }
    }

    public void ClearGrid()
    {
        foreach (var cell in gridCells.Values)
        {
            Destroy(cell);
        }

        gridCells.Clear();
        gridGenerated = false;
        GenerateGrid();
    }

    public void DrawCircle(Vector2Int center, int radius)
    {
        int x = radius;
        int y = 0;
        int decisionOver2 = 1 - x; // critério de Bresenham

        while (y <= x)
        {
            PaintCell(center.x + x, center.y + y);
            PaintCell(center.x + y, center.y + x);
            PaintCell(center.x - x, center.y + y);
            PaintCell(center.x - y, center.y + x);
            PaintCell(center.x - x, center.y - y);
            PaintCell(center.x - y, center.y - x);
            PaintCell(center.x + x, center.y - y);
            PaintCell(center.x + y, center.y - x);

            y++;

            if (decisionOver2 <= 0)
            {
                decisionOver2 += 2 * y + 1;
            }
            else
            {
                x--;
                decisionOver2 += 2 * (y - x) + 1;
            }
        }
    }

    public void DrawLine(Vector2Int start, Vector2Int end)
    {
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            PaintCell(x0, y0); // pinta célula atual

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public void DrawRectangle(Vector2Int start, Vector2Int end, bool filled = false)
    {
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        if (filled)
        {
            // Preenchido
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    PaintCell(x, y);
                }
            }
        }
        else
        {
            // Apenas bordas
            for (int x = minX; x <= maxX; x++)
            {
                PaintCell(x, minY); // base
                PaintCell(x, maxY); // topo
            }
            for (int y = minY; y <= maxY; y++)
            {
                PaintCell(minX, y); // esquerda
                PaintCell(maxX, y); // direita
            }
        }
    }

    private void PaintCell(int x, int y)
    {
        if (gridCells.TryGetValue(new Vector2Int(x, y), out var cell))
        {
            cell.GetComponent<Image>().color = selectedColor;
        }
    }

    public void SetSelectedColor(Color color)
    {
        if (eraserMode)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        else if (eyedropperMode)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        selectedColor = color;
        previewSelectColor.color = color;
    }


    public void CreateSelection(Vector2Int start, Vector2Int end)
    {
        selectedCells.Clear();
        originalColors.Clear();

        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (gridCells.TryGetValue(new Vector2Int(x, y), out GameObject cell))
                {
                    var painter = cell.GetComponent<PixelCellPainter>();
                    Color currentColor = painter.GetComponent<Image>().color;

                    if (currentColor != baseGridColor)
                    {
                        selectedCells.Add(painter);
                        // Guarda a cor original
                        originalColors[painter] = currentColor;

                        // Marcar visualmente a seleção
                        painter.GetComponent<Image>().color = currentColor * 0.8f;
                    }
                }
            }
        }

        if (selectedCells.Count == 0)
        {
            isSelecting = false;
            paitingToolsManager.DisableAllImages();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            //selectionStart = null;
            return;


        }

        //    Debug.Log("Selecionadas " + selectedCells.Count + " células.");
        UIHelperUtils.SetCursor(paitingToolsManager.setaSprite, CursorHotspot.TopRight);

        isMoveSelection = true;
        isSelecting = true;
    }

    public void ClearSelection()
    {
        isMoveSelection = false;
        isSelecting = false;
        OnSelecting = false;

        paitingToolsManager.DisableAllImages();

        if (selectedCells.Count == 0) return;

        foreach (var cell in selectedCells)
        {
            Image img = cell.GetComponent<Image>();

            img.color /= 0.8f;
        }

        selectedCells.Clear();
        originalColors.Clear();

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        //Debug.Log("Seleção limpa.");
    }

    public void MoveSelectionStep(Vector2Int offset)
    {
        if (selectedCells.Count == 0) return;

        // Calcula limites da seleção
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var cell in selectedCells)
        {
            minX = Mathf.Min(minX, cell.position.x);
            minY = Mathf.Min(minY, cell.position.y);
            maxX = Mathf.Max(maxX, cell.position.x);
            maxY = Mathf.Max(maxY, cell.position.y);
        }

        // Verifica se o movimento sairia do grid
        int newMinX = minX + offset.x;
        int newMinY = minY + offset.y;
        int newMaxX = maxX + offset.x;
        int newMaxY = maxY + offset.y;

        if (newMinX < 0 || newMinY < 0 || newMaxX >= cols || newMaxY >= rows)
            return; // bloqueia movimento fora do grid

        // Copia cores e limpa origem
        List<(Vector2Int, Color)> oldColors = new List<(Vector2Int, Color)>();
        foreach (var cell in selectedCells)
        {
            oldColors.Add((cell.position, cell.GetComponent<Image>().color));
            cell.GetComponent<Image>().color = baseGridColor;
        }

        selectedCells.Clear();

        // Aplica movimento de 1 passo
        foreach (var data in oldColors)
        {
            Vector2Int newPos = data.Item1 + offset;
            if (gridCells.TryGetValue(newPos, out GameObject newCell))
            {
                var painter = newCell.GetComponent<PixelCellPainter>();
                newCell.GetComponent<Image>().color = data.Item2;
                selectedCells.Add(painter);
            }
        }

        UpdatePreview();
    }

    public void MoveSelectionTo(Vector2Int targetCell)
    {
        if (selectedCells.Count == 0) return;

        // Pega o canto superior-esquerdo e inferior-direito da seleção
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var cell in selectedCells)
        {
            minX = Mathf.Min(minX, cell.position.x);
            minY = Mathf.Min(minY, cell.position.y);
            maxX = Mathf.Max(maxX, cell.position.x);
            maxY = Mathf.Max(maxY, cell.position.y);
        }

        Vector2Int topLeft = new Vector2Int(minX, minY);
        Vector2Int bottomRight = new Vector2Int(maxX, maxY);

        int selectionWidth = (maxX - minX) + 1;
        int selectionHeight = (maxY - minY) + 1;

        // Calcula deslocamento desejado
        Vector2Int offset = targetCell - topLeft;

        // Corrige deslocamento para não sair do grid
        int newMinX = minX + offset.x;
        int newMinY = minY + offset.y;
        int newMaxX = newMinX + selectionWidth - 1;
        int newMaxY = newMinY + selectionHeight - 1;

        // Se sair do grid, ajusta offset
        if (newMinX < 0) offset.x -= newMinX; // empurra pra direita
        if (newMinY < 0) offset.y -= newMinY; // empurra pra cima
        if (newMaxX >= cols) offset.x -= (newMaxX - (cols - 1)); // empurra pra esquerda
        if (newMaxY >= rows) offset.y -= (newMaxY - (rows - 1)); // empurra pra baixo

        // Copia cores das selecionadas e limpa origem
        List<(Vector2Int, Color)> oldColors = new List<(Vector2Int, Color)>();

        foreach (var cell in selectedCells)
        {
            oldColors.Add((cell.position, cell.GetComponent<Image>().color));
            cell.GetComponent<Image>().color = baseGridColor; // limpa posição antiga
        }


        selectedCells.Clear();

        // Aplica na nova posição
        foreach (var data in oldColors)
        {
            Vector2Int newPos = data.Item1 + offset;
            if (gridCells.TryGetValue(newPos, out GameObject newCell))
            {
                var painter = newCell.GetComponent<PixelCellPainter>();
                newCell.GetComponent<Image>().color = data.Item2;
                selectedCells.Add(painter);
            }
        }

        //    Debug.Log($"Seleção movida para {targetCell} (ajustada para caber no grid)");
    }




    public void FloodFill(Vector2Int startPos)
    {
        if (!gridCells.ContainsKey(startPos)) return;

        GameObject startCell = gridCells[startPos];
        Color targetColor = startCell.GetComponent<Image>().color;

        if (targetColor == selectedColor) return; // Evita loop infinito

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startPos);
        visited.Add(startPos);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            GameObject cell = gridCells[current];
            Image cellImage = cell.GetComponent<Image>();

            if (cellImage.color == targetColor)
            {
                if (eraseAll)
                    cellImage.color = baseGridColor;
                else
                    cellImage.color = selectedColor;

                // 4 direções (vizinhos ortogonais)
                Vector2Int[] neighbors = new Vector2Int[]
                {
                current + Vector2Int.up,
                current + Vector2Int.down,
                current + Vector2Int.left,
                current + Vector2Int.right
                };

                foreach (Vector2Int neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor) && gridCells.ContainsKey(neighbor))
                    {
                        Image neighborImage = gridCells[neighbor].GetComponent<Image>();
                        if (neighborImage.color == targetColor)
                        {
                            queue.Enqueue(neighbor);
                            visited.Add(neighbor);
                        }
                    }
                }
            }
        }

        gridPreview.CopyGridFrom();
    }


    public void UpdatePreview()
    {
        gridPreview.CopyGridFrom();
    }

    public void LoadPaintedCells(string fileName, string selectRootPath, string folderName = "Default")
    {

        string json = fileManager.LoadJson(selectRootPath, fileManager.basePath_PaintingData, folderName, fileName);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("JSON não encontrado para exportar imagem.");
            return;
        }
        Drawing wrapper = JsonUtility.FromJson<Drawing>(json);

        UpdatePaitingGrid(wrapper.list, wrapper.scale);

    }

    public void UpdatePaitingGrid(List<PixelData> list, int scale)
    {

        ClearGrid();


        if (scale != currentScale)
        {
            UpdateScale();
        }

        foreach (PixelData pixel in list)
        {
            Vector2Int pos = new Vector2Int(pixel.x, pixel.y);
            if (gridCells.ContainsKey(pos))
            {
                Image img = gridCells[pos].GetComponent<Image>();
                img.color = new Color(pixel.r, pixel.g, pixel.b, pixel.a);
            }
        }

        UpdatePreview();
    }
    public void UpdateScale()
    {
        if (isScalingLocked)
            return;

        StartCoroutine(UpdateScaleDelayed(0.3f));
    }

    private IEnumerator UpdateScaleDelayed(float delay)
    {
        isScalingLocked = true;

        if (isUpscaled)
            DownscaleGrid();
        else
            UpscaleGrid();

        yield return new WaitForSeconds(delay);

        isScalingLocked = false;
    }


    public void UpscaleGrid()
    {
        if (isUpscaled)
            return;

        currentScale = 2;
        isUpscaled = true;
        scaleText.text = "2X";
        // Salva as cores atuais
        Dictionary<Vector2Int, Color> oldColors = new Dictionary<Vector2Int, Color>();
        foreach (var kvp in gridCells)
        {
            Image img = kvp.Value.GetComponent<Image>();
            oldColors[kvp.Key] = img.color;
        }

        // Dobra a resolução
        int newRows = rows * 2;
        int newCols = cols * 2;

        // Apaga grid antigo
        foreach (var cell in gridCells.Values)
        {
            Destroy(cell);
        }

        gridCells.Clear();
        gridGenerated = false;

        // Atualiza dimensões
        rows = newRows;
        cols = newCols;

        // Ajusta visual do layout
        GridLayoutGroup layout = parentContainer.GetComponent<GridLayoutGroup>();
        layout.cellSize /= 2f;
        layout.spacing *= 0.45f;
        layout.constraintCount *= 2;
        layout.padding.left = 4;
        layout.padding.top = 1;

        // Gera o novo grid
        GenerateGrid();

        // Reaplica as cores proporcionalmente
        foreach (var kvp in oldColors)
        {
            Vector2Int oldPos = kvp.Key;
            Color color = kvp.Value;

            // Cada célula antiga cobre 2x2 no novo grid
            Vector2Int basePos = oldPos * 2;
            Vector2Int[] newPositions = new Vector2Int[]
            {
            basePos,
            basePos + Vector2Int.right,
            basePos + Vector2Int.up,
            basePos + Vector2Int.right + Vector2Int.up
            };

            foreach (Vector2Int newPos in newPositions)
            {
                if (gridCells.TryGetValue(newPos, out GameObject cell))
                {
                    Image img = cell.GetComponent<Image>();
                    img.color = color;
                }
            }
        }


        gridPreview.UpscaleGrid();
    }


    public void DownscaleGrid()
    {

        if (!isUpscaled)
            return;

        isUpscaled = false;
        currentScale = 1;
        scaleText.text = "1X";
        // Salva cores reduzidas
        Dictionary<Vector2Int, Color> newColors = new Dictionary<Vector2Int, Color>();
        for (int x = 0; x < cols; x += 2)
        {
            for (int y = 0; y < rows; y += 2)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (gridCells.TryGetValue(pos, out GameObject cell))
                {
                    Image img = cell.GetComponent<Image>();
                    Vector2Int downPos = new Vector2Int(x / 2, y / 2);
                    newColors[downPos] = img.color;
                }
            }
        }

        foreach (var cell in gridCells.Values)
        {
            Destroy(cell);
        }

        gridCells.Clear();
        gridGenerated = false;

        rows = 17;
        cols = 17;

        GridLayoutGroup layout = parentContainer.GetComponent<GridLayoutGroup>();
        layout.cellSize = originalCellSize;
        layout.spacing = originalSpacing;
        layout.constraintCount /= 2;
        layout.padding.left = 1;
        layout.padding.top = 0;

        GenerateGrid();

        foreach (var kvp in newColors)
        {
            if (gridCells.TryGetValue(kvp.Key, out GameObject cell))
            {
                Image img = cell.GetComponent<Image>();
                img.color = kvp.Value;
            }
        }
        gridPreview.DownscaleGrid();
    }




    public void Save(string filePng, string subfolderName, bool Ads = true)
    {
        bool hasData = SavePaintedCells("fileJson", subfolderName);

        if (!hasData)
            return;

        if (Ads)
            AdsManager.TryShowInterstitial();

        ExportGridAsTextureFromJson(filePng, Application.persistentDataPath, 1000, subfolderName);
    }


    public bool SavePaintedCells(string fileName, string subfolderName = "Default")
    {
        List<PixelData> paintedPixels = new List<PixelData>();
        Color defaultColor = baseGridColor;

        foreach (var kvp in gridCells)
        {
            Image img = kvp.Value.GetComponent<Image>();
            if (img.color != defaultColor)
            {
                paintedPixels.Add(new PixelData
                {
                    x = kvp.Key.x,
                    y = kvp.Key.y,
                    r = img.color.r,
                    g = img.color.g,
                    b = img.color.b,
                    a = img.color.a
                });
            }
        }

        // 🚫 Nenhuma célula pintada → cancela
        if (paintedPixels.Count == 0)
        {
            string text = UIHelperUtils.T("none.cell.txt");

            if (string.IsNullOrEmpty(text))
                text = "No painted cells to save.";

            fileManager.CreateAdvice(text);
            return false;
        }

        wrapper = new Drawing
        {
            list = paintedPixels,
            scale = currentScale
        };

        string data = JsonUtility.ToJson(wrapper, true);
        //fileManager.SaveJson(subfolderName, fileName, data, fileManager.basePath_PaintingData);

        return true;
    }


    private Drawing wrapper = new Drawing();

    public void ExportGridAsTextureFromJson(string fileName, string selectRootPath, int textureSize = 1020, string folderName = "Default")
    {
        //string json = fileManager.LoadJson(selectRootPath, fileManager.basePath_PaintingData, folderName, fileName.Replace(".png", ".json"));

        //fileName.Replace(".png", ".json"));

        //if (string.IsNullOrEmpty(json))
        //{
        //    Debug.LogWarning("JSON não encontrado para exportar imagem.");
        //    return;
        //}

        //Drawing wrapper = JsonUtility.FromJson<Drawing>(json);

        int gridWidth = 17;
        int gridHeight = 17;

        if (wrapper.scale == 2)
        {
            gridWidth = 34;
            gridHeight = 34;
        }

        int cellPixelSize = textureSize / Mathf.Max(gridWidth, gridHeight);
        int finalWidth = gridWidth * cellPixelSize;
        int finalHeight = gridHeight * cellPixelSize;

        finalWidth = Mathf.CeilToInt(finalWidth / 4f) * 4;
        finalHeight = Mathf.CeilToInt(finalHeight / 4f) * 4;

        Texture2D texture = new Texture2D(finalWidth, finalHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);

        // Preenche toda a textura com transparente inicialmente
        Color[] clearColors = new Color[finalWidth * finalHeight];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = transparent;
        texture.SetPixels(clearColors);

        // Cria um dicionário rápido para acesso das cores do JSON por posição
        Dictionary<Vector2Int, Color> pixelDict = new Dictionary<Vector2Int, Color>();
        foreach (var pixel in wrapper.list)
        {
            Vector2Int pos = new Vector2Int(pixel.x, pixel.y);
            Color c = new Color(pixel.r, pixel.g, pixel.b, pixel.a);
            pixelDict[pos] = c;
        }

        // Para cada pixel na resolução do grid, pinta o bloco correspondente na textura
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Color colorToUse = transparent;

                Vector2Int pos = new Vector2Int(x, y);
                if (pixelDict.ContainsKey(pos))
                {
                    colorToUse = pixelDict[pos];
                }

                for (int px = 0; px < cellPixelSize; px++)
                {
                    for (int py = 0; py < cellPixelSize; py++)
                    {
                        int pixelX = x * cellPixelSize + px;
                        int pixelY = y * cellPixelSize + py;

                        texture.SetPixel(pixelX, pixelY, colorToUse);
                    }
                }
            }
        }

        texture.Apply();

        // Salva a imagem
        fileManager.SavePng(folderName, fileName, texture, fileManager.basePath_Sprite);

        // Atualiza preview se tiver
        //if (resultPreview != null)
        //{
        //    Texture2D tex = LoadTextureFromFile(folderName, fileName, "Sprites");
        //    resultPreview.sprite = ConvertTextureToSprite(tex);
        //}
    }



}







