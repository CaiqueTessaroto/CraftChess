using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GridSquadManager : MonoBehaviour
{
    public SquadManager squadManager;
    public GameObject gridCell;
    public Color whiteHouse = Color.white;
    public Color blackHouse = Color.black;
    public int rows = 8; // Número de linhas
    public int cols = 8; // Número de colunas

    public Dictionary<Vector2, GameObject> gridCells = new Dictionary<Vector2, GameObject>();


    private GameObject newCell; // <- Adicionamos a declaração aqui
    private GameObject thisObject;
    private Transform gridTransform;





    void Start()
    {

        if (squadManager == null)
        {
            squadManager = FindObjectOfType<SquadManager>();
        }

        gridTransform = transform;
        thisObject = gameObject;
        Image imagem = thisObject.GetComponent<Image>();
        imagem.enabled = false; // Desativando o componente Image

        //var centerX = 4;
        //var centerY = 4;
        //selectedPosition = new Vector2Int(centerX, centerY);

        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < rows; x++) // começa de baixo
        {
            bool gridbool = (x % 2 == 0);

            for (int y = 0; y < cols; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y); // para manter a lógica de coordenadas 0,0 no canto inferior esquerdo

                newCell = Instantiate(gridCell, gridTransform);
                Image cellImage = newCell.GetComponent<Image>();

                cellImage.color = gridbool ? blackHouse : whiteHouse;

                gridbool = !gridbool;

                newCell.name = $"Cell ({x},{y})";
                gridCells[cellPos] = newCell;

                if (y <= 3)
                {
                    Button button = newCell.GetComponent<Button>();
                    button.onClick.AddListener(() => ToggleCellSelection(cellPos));
                }
            }
        }
    }

    void ToggleCellSelection(Vector2Int cellPos)
    {

        //Debug.Log("cellPos: " + cellPos);

        GameObject cell = gridCells[cellPos];

        if (squadManager.removePiece)
        {
            squadManager.RemovePieceFromCell(cell, cellPos);
            squadManager.CheckStrategicModeRules();
        }
        else if (squadManager.selectedPiece)
        {
            squadManager.SetPieceToCell(cell, cellPos);
            squadManager.CheckStrategicModeRules();
        }
        else if (squadManager.setKing)
        {
            squadManager.SetKing(cell, cellPos);
            squadManager.CheckStrategicModeRules();
        }
        else
        {
            squadManager.GetPieceOnCell(cellPos);
        }

    }



    public GameObject GetCellAtPosition(Vector2Int pos)
    {
        // se você já mantém as células em uma matriz ou dicionário, é só acessar direto
        // exemplo genérico:
        if (gridCells.TryGetValue(pos, out GameObject cell))
        {
            return cell;
        }

        Debug.LogWarning("Célula não encontrada na posição: " + pos);
        return null;
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

        // Chama a geração novamente
        GenerateGrid();
    }










}