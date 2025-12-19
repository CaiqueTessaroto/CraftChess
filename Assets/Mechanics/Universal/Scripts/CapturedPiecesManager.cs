using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class CapturedData
{
    public string Name;
    public int Power;

    public CapturedData(string name, int power)
    {
        Name = name;
        Power = power;
    }
}

public class CapturedPiecesManager : MonoBehaviour
{

    public GameObject referenceSpritePrefab;
    private List<CapturedData> capturedByWhite = new List<CapturedData>();
    private List<CapturedData> capturedByBlack = new List<CapturedData>();

    private Transform bottomReference;
    private Transform topReference;

    private List<MatchSquadData> Squads = new List<MatchSquadData>();

    private bool OnReverse = false;

    // Start is called before the first frame update
    void Start()
    {
        Squads = MatchData.Instance.Squads;

    }

    public void CreateReferenceAreas(int gridWidth, int gridHeight, float cellSize, bool reverse = false)
    {
        if (referenceSpritePrefab == null)
        {
            Debug.LogWarning("referenceSpritePrefab não atribuído!");
            return;
        }

        if (reverse && !OnReverse)
        {
            SwapReferenceAreas();
            OnReverse = true;
        }
        else if (!reverse && OnReverse)
        {
            SwapReferenceAreas();
            OnReverse = false;
        }


        // Remove referências antigas se existirem
        if (bottomReference != null) RebuildCapturedArea(bottomReference, 0);
        if (topReference != null) RebuildCapturedArea(topReference, 1);

        if (bottomReference != null && topReference != null) return;

        float offsetX = -gridWidth * cellSize / 2f;   // canto esquerdo do tabuleiro
        float offsetY = -gridHeight * cellSize / 2f;  // canto inferior do tabuleiro

        // === Área inferior esquerda ===
        GameObject bottom = Instantiate(referenceSpritePrefab, transform.parent);
        bottom.name = "BottomReference";
        bottom.transform.localScale = new Vector3(4f, 0.5f, 1f);
        bottom.transform.position = new Vector3(
            offsetX + 2f,    // um pouco à esquerda do tabuleiro
            offsetY - 0.25f,    // abaixo do tabuleiro
            0f
        );
        bottomReference = bottom.transform;

        SpriteRenderer srBottom = bottom.GetComponent<SpriteRenderer>();
        if (srBottom != null)
            srBottom.sortingOrder = 1;

        // === Área superior esquerda ===
        GameObject top = Instantiate(referenceSpritePrefab, transform.parent);
        top.name = "TopReference";
        top.transform.localScale = new Vector3(4f, 0.5f, 1f);
        top.transform.position = new Vector3(
            offsetX + 2f,    // mesmo X da inferior
            offsetY + (gridHeight * cellSize) + 0.25f,  // acima do tabuleiro
            0f
        );
        topReference = top.transform;

        SpriteRenderer srTop = top.GetComponent<SpriteRenderer>();
        if (srTop != null)
            srTop.sortingOrder = 1;


        if (reverse)
        {
            SwapReferenceAreas();
            OnReverse = true;
        }

    }

    private void SwapReferenceAreas()
    {
        if (topReference == null || bottomReference == null)
            return;

        Transform temp = topReference;
        topReference = bottomReference;
        bottomReference = temp;
    }

    public void AddCapturedPiece(GameObject capturedPieceObject, int capturedBy)
    {
        // 0 = capturado por branco (vai pra área superior)
        // 1 = capturado por preto (vai pra área inferior)
        Transform targetArea = (capturedBy == 0) ? bottomReference : topReference;

        Dictionary<string, Sprite> whiteSprites = Squads[0].Sprites;
        Dictionary<string, Sprite> blackSprites = Squads[1].Sprites;

        if (capturedPieceObject == null)
        {
            Debug.LogWarning("Objeto da peça capturada é nulo!");
            return;
        }

        // --- Determina o nome da peça base ---
        string pieceName = capturedPieceObject.name.Replace("(Clone)", "").Trim();
        if (pieceName.Contains("_"))
            pieceName = pieceName.Split('_')[1];

        PieceComponent p = capturedPieceObject.GetComponent<PieceComponent>();
        int powerValue = (p != null) ? p.Power : 0;

        // --- Adiciona à lista persistente ---
        if (capturedBy == 0)
        {
            capturedByWhite.Add(new CapturedData(pieceName, powerValue));
            // ordena por poder (menor primeiro)
            capturedByWhite = capturedByWhite.OrderBy(c => c.Power).ToList();
        }
        else
        {
            capturedByBlack.Add(new CapturedData(pieceName, powerValue));
            capturedByBlack = capturedByBlack.OrderBy(c => c.Power).ToList();
        }

        // --- Reconstrói as áreas ---
        RebuildCapturedArea(targetArea, capturedBy);
    }

    private void RebuildCapturedArea(Transform targetArea, int capturedBy)
    {
        // Limpa os objetos anteriores
        foreach (Transform child in targetArea)
            Destroy(child.gameObject);

        // Pega a lista correta
        List<CapturedData> capturedList = (capturedBy == 0) ? capturedByWhite : capturedByBlack;
        Dictionary<string, Sprite> sourceSprites = (capturedBy == 0) ? Squads[1].Sprites : Squads[0].Sprites;

        // Agrupa por tipo de peça
        Dictionary<string, int> groupedPieces = new Dictionary<string, int>();
        foreach (var data in capturedList)
        {
            if (groupedPieces.ContainsKey(data.Name))
                groupedPieces[data.Name]++;
            else
                groupedPieces[data.Name] = 1;
        }

        // Reconstrói os sprites agrupados
        int typeIndex = 0;
        int index = 0;
        foreach (var kvp in groupedPieces)
        {
            string pieceName = kvp.Key;
            int count = kvp.Value;

            for (int i = 0; i < count; i++)
            {
                GameObject capturedSprite = new GameObject($"Captured_{pieceName}");
                capturedSprite.transform.SetParent(targetArea);
                SpriteRenderer sr = capturedSprite.AddComponent<SpriteRenderer>();

                // Escolhe o sprite correto
                if (sourceSprites.ContainsKey(pieceName))
                    sr.sprite = sourceSprites[pieceName];

                sr.sortingOrder = 2;
                capturedSprite.transform.localScale = new Vector3(0.01f, 0.1f, 1f);

                // === Posicionamento ===
                float xOffset = (typeIndex * 0.05f) + (index * 0.05f) - 0.45f;
                capturedSprite.transform.localPosition = new Vector3(xOffset, 0, 0);
                index++;
            }

            typeIndex++;
        }
    }






}
