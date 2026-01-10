using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GridLobby : MonoBehaviour
{
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

        gridTransform = transform;
        thisObject = gameObject;
        Image imagem = thisObject.GetComponent<Image>();
        imagem.enabled = false; // Desativando o componente Image

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

                Button button = newCell.GetComponent<Button>();
                button.onClick.AddListener(() => ToggleCellSelection(cellPos));

            }
        }
    }

    void ToggleCellSelection(Vector2Int cellPos)
    {

        Debug.Log("cellPos: " + cellPos);

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

    public void ClearGrid(List<Vector2Int> pos)
    {
        foreach (var kvp in gridCells)
        {
            Vector2 cellPos = kvp.Key;

            // ignora células que estão na lista
            if (pos != null && pos.Contains(Vector2Int.RoundToInt(cellPos)))
                continue;

            GameObject cell = kvp.Value;
            if (cell == null) continue;

            Transform piece = cell.transform.Find("Piece");
            if (piece != null)
            {
                Destroy(piece.gameObject);
            }
        }
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
