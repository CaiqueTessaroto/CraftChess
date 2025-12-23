using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;


public class PromotionUI : MonoBehaviour
{

    public ChessMovesPanel chessMovesPanel;
    public BoardChessManager boardManager;

    [Header("Prefabs")]
    public GameObject promotionCanvasPrefab;
    public GameObject promotionButtonPrefab;

    [Header("References")]
    public PieceComponent currentPiece;
    public GameObject targetPiece;
    public Vector2Int pos;

    [Header("Posição:")]
    public GameObject currentPromotionCanvas;
    public Vector3 offset; // ajuste de posição
    private Transform piecetransform;

    [Header("Data")]
    public MatchSquadData squad = new MatchSquadData();


    public void Initialize(PieceComponent piecePromotion, GameObject promotionCanvasPrefab, GameObject promotionButtonPrefab, MatchSquadData squadData, Vector2Int pos, GameObject targetPiece = null)
    {
        this.promotionCanvasPrefab = promotionCanvasPrefab;
        this.promotionButtonPrefab = promotionButtonPrefab;
        this.currentPiece = piecePromotion;
        this.targetPiece = targetPiece;
        this.pos = pos;
        this.squad = squadData;


        if (chessMovesPanel == null)
            chessMovesPanel = FindObjectOfType<ChessMovesPanel>();

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardChessManager>();


        CreateCanvas(currentPiece);

        //StartCoroutine(DestroyObject());
    }


    void Start()
    {

        if (chessMovesPanel == null)
            chessMovesPanel = FindObjectOfType<ChessMovesPanel>();

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardChessManager>();

    }

    void Update()
    {

    }

    private float GetObjectHeight(Transform obj)
    {
        float height = 2.25f; // valor padrão caso não tenha renderer ou collider

        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            height = renderer.bounds.size.y;
        }
        else
        {
            Collider col = obj.GetComponentInChildren<Collider>();
            if (col != null)
            {
                height = col.bounds.size.y;
            }
        }

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardChessManager>();

        foreach (var squad in boardManager.Squads)
        {
            if (squad.Player.name != "Bot")
                if (currentPiece.Player.id != squad.Player.id)
                    return -height; // - 1

        }

        return height; //+ 1
    }



    public void CreateCanvas(PieceComponent currentPiece)
    {
        currentPromotionCanvas = Instantiate(promotionCanvasPrefab);

        GameObject cell = boardManager.GetCellAtPosition(pos.x, pos.y);

        //currentPromotionCanvas.transform.SetParent(cell.transform);

        Button btn = currentPromotionCanvas.GetComponentInChildren<Button>();
        btn.onClick.AddListener(() =>
        {
            Destroy(currentPromotionCanvas);
            Destroy(currentPiece.gameObject.GetComponent<PromotionUI>());
        });

        if (piecetransform == null)
            piecetransform = cell.transform;

        float height = GetObjectHeight(piecetransform);
        Vector3 heightOffset = new Vector3(0, -height, 0);

        Vector3 desiredWorldPos = piecetransform.position + heightOffset + offset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(desiredWorldPos);

        if (screenPos.y >= Screen.height * 0.8f)
        {
            currentPromotionCanvas.transform.position = piecetransform.position - heightOffset + offset;
        }
        else
        {
            currentPromotionCanvas.transform.position = desiredWorldPos;
        }

        CreateSpriteButtons(squad);
    }


    public void CreateSpriteButtons(MatchSquadData matchData)
    {
        Transform contentParent = currentPromotionCanvas.transform.Find("Panel/Grid");
        // Limpa o conteúdo anterior
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var pieceName in currentPiece.PromotionPieces)
        {
            if (squad.Pieces.ContainsKey(pieceName))
            {
                SquadPieceData pieceData = squad.Data.Pieces.Find(p => p.Name == pieceName);

                string spriteName = pieceData.NameInSquad;
                Sprite sprite = matchData.Sprites[spriteName];

                // Instancia o botão
                GameObject newButton = Instantiate(promotionButtonPrefab, contentParent);

                // Define a imagem
                Image img = newButton.GetComponent<Image>();
                if (img != null)
                    img.sprite = sprite;

                // Adiciona listener para capturar o clique e retornar o nome
                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        GameObject newPiece = boardManager.PlacePiece(pieceName, sprite, pos, squad);
                        PieceComponent component = newPiece.GetComponent<PieceComponent>();
                        component.IsPromoted = true;

                        Destroy(currentPromotionCanvas);

                        string letter = $"{(char)('a' + pos.x)}";
                        string number = $"{pos.y + 1}";
                        string house;

                        if (targetPiece)
                        {
                            house = $"x{letter}{number}=";
                            boardManager.AddCapturedPiece(targetPiece, currentPiece.Player.id);
                            boardManager.AllPieces.Remove(targetPiece);
                            Destroy(targetPiece);
                        }
                        else
                            house = $"{letter}{number}=";


                        boardManager.AllPieces.Remove(currentPiece.gameObject);
                        Destroy(currentPiece.gameObject);


                        SpriteRenderer sr = currentPiece.GetComponent<SpriteRenderer>();

                        chessMovesPanel.AddMove(house, sr.sprite, sprite);

                        boardManager.UpdateBoardControl();
                    });
                }

                // (Opcional) — renomeia o botão na hierarquia
                newButton.name = $"Btn_{spriteName}";
            }
        }
    }


}