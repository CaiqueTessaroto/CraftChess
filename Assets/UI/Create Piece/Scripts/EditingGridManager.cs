using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EditingGridManager : MonoBehaviour
{
    public GameObject gridCellWhite; // Prefab da célula
    public GameObject gridCellBlack; // Prefab da célula
    public GameObject gridCellPiece; // Prefab da célula
    public int rows = 5; // Número de linhas
    public int cols = 5; // Número de colunas
    private Dictionary<Vector2Int, GameObject> gridCells = new Dictionary<Vector2Int, GameObject>();
    private bool gridbool = false;
    private GameObject newCell;
    private GameObject editingGrid;
    public GridViewManager gridView;
    private bool gridGenerated = false;
    private int centerX = 2;
    private int centerY = 2;
    void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        if (gridGenerated) return; // Já foi gerado, não faz nada

        gridGenerated = true;

        editingGrid = gameObject;

        Image imagem = editingGrid.GetComponent<Image>();
        imagem.enabled = false; // Desativando o componente Image

        if (rows == 7 && cols == 7)
        {
            centerX = 3;
            centerY = 3;
        }


        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (gridbool == false)
                {
                    if (x == centerX && y == centerY)
                    {
                        newCell = Instantiate(gridCellPiece, transform);
                    }
                    else
                    {
                        newCell = Instantiate(gridCellWhite, transform);
                    }

                    gridbool = true;

                }
                else
                {
                    newCell = Instantiate(gridCellBlack, transform);
                    Image cellImage = newCell.GetComponent<Image>();
                    cellImage.color = Color.black;
                    gridbool = false;
                }



                gridCells[new Vector2Int(x, y)] = newCell;

                Button button = newCell.GetComponent<Button>();
                Vector2Int cellPos = new Vector2Int(x, y);

                if (editingGrid.name != "ViewGrid")
                    button.onClick.AddListener(() =>
                    {
                        ToggleCellSelection(cellPos);
                        gridView.HighlightValidMoves();
                    });

            }
        }
    }

    void ToggleCellSelection(Vector2Int cellPos)
    {
        GameObject cell = gridCells[cellPos];
        Image cellImage = cell.GetComponent<Image>();
        var cellName = cellImage.name;

        if (cellName != "CellPiece(Clone)")
        {

            // Alternar entre selecionado e não selecionado
            if (cellImage.color == Color.white || cellImage.color == Color.black)
            {
                cellImage.color = Color.green; // Selecionado
            }
            else if (cellName == "CellBlack(Clone)")
            {
                cellImage.color = Color.black;
            }
            else
            {
                cellImage.color = Color.white; // Desselecionado
            }
        }
    }

    public void DeselectAllCells()
    {
        foreach (var pair in gridCells)
        {
            GameObject cell = pair.Value;
            Image cellImage = cell.GetComponent<Image>();
            string cellName = cellImage.name;

            if (cellName == "CellWhite(Clone)")
            {
                cellImage.color = Color.white;
            }
            else if (cellName == "CellBlack(Clone)")
            {
                cellImage.color = Color.black;
            }
        }
    }

    public void ToggleCellLoadSelection(Vector2Int cellPos)
    {
        Vector2Int newPos = cellPos + new Vector2Int(centerX, centerY);
        //Vector2Int key = new Vector2Int(Mathf.RoundToInt(newPos.x), Mathf.RoundToInt(newPos.y));

        if (gridCells.TryGetValue(newPos, out GameObject cell))
        {
            Image cellImage = cell.GetComponent<Image>();
            var cellName = cellImage.name;

            if (cellName != "CellPiece(Clone)")
            {
                cellImage.color = Color.green; // Selecionado
            }
        }
        else
        {
            Debug.LogWarning("Posição não encontrada no grid: " + newPos);
        }
    }


    public List<Vector2Int> GetGreenCells()
    {
        List<Vector2Int> greenCells = new List<Vector2Int>();


        foreach (var cell in gridCells)
        {
            Image cellImage = cell.Value.GetComponent<Image>();
            if (cellImage.color == Color.green)
            {
                greenCells.Add(new Vector2Int(cell.Key.x - centerX, cell.Key.y - centerY));
            }
        }

        return greenCells;
    }



}
