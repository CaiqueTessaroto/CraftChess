using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridPreview : MonoBehaviour
{
    public PaintingGridManager manager;
    public GameObject gridCell; // Prefab da célula
    public int rows = 17; // Número de linhas
    public int cols = 17; // Número de colunas
    public Dictionary<Vector2Int, GameObject> gridCells = new Dictionary<Vector2Int, GameObject>();
    private bool gridGenerated = false;





    public Transform parentContainer;
    private Vector2 originalCellSize;


    void Start()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<PaintingGridManager>();
        }

        GridLayoutGroup layout = parentContainer.GetComponent<GridLayoutGroup>();
        originalCellSize = layout.cellSize;

        GenerateGrid();
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

            }
        }
    }

    public void CopyGridFrom()
    {
        // Garante que o grid atual esteja gerado
        if (!gridGenerated)
            GenerateGrid();

        foreach (var kvp in manager.gridCells)
        {
            Vector2Int pos = kvp.Key;
            GameObject sourceCell = kvp.Value;

            if (!gridCells.ContainsKey(pos)) continue;

            GameObject targetCell = gridCells[pos];

            Image sourceImage = sourceCell.GetComponent<Image>();
            Image targetImage = targetCell.GetComponent<Image>();

            if (sourceImage != null && targetImage != null)
            {
                targetImage.color = sourceImage.color;
            }
        }
    }





    public void UpscaleGrid()
    {
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
        layout.constraintCount *= 2;

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
    }


    public void DownscaleGrid()
    {
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
        layout.constraintCount /= 2;

        GenerateGrid();

        foreach (var kvp in newColors)
        {
            if (gridCells.TryGetValue(kvp.Key, out GameObject cell))
            {
                Image img = cell.GetComponent<Image>();
                img.color = kvp.Value;
            }
        }
    }
}







