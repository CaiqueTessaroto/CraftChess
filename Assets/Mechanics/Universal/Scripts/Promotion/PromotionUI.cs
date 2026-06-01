using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Timers;
using System;


public class PromotionUI : MonoBehaviour
{

    public ChessMovesPanel chessMovesPanel;
    public BoardChessManager boardManager;
    public PieceController pieceController;
    public MoveTracker moveTracker;

    [Header("Prefabs")]
    public GameObject promotionCanvasPrefab;
    public GameObject promotionButtonPrefab;

    [Header("References")]
    public PieceComponent currentPiece;
    public GameObject targetPiece;
    public Vector2Int pos;

    [Header("Posição:")]
    public GameObject currentPromotionCanvas;
    public Vector3 offset = new Vector3(0, -0.6f, 0); // ajuste de posição
    private Transform piecetransform;

    [Header("Data")]
    public MatchSquadData squad = new MatchSquadData();
    [HideInInspector]
    public bool isRemotePromotion = false;


    public void Initialize(PieceComponent piecePromotion, GameObject promotionCanvasPrefab, GameObject promotionButtonPrefab, MatchSquadData squadData, Vector2Int pos, bool forceMove = false, bool IA = false, GameObject targetPiece = null)
    {
        this.promotionCanvasPrefab = promotionCanvasPrefab;
        this.promotionButtonPrefab = promotionButtonPrefab;
        this.currentPiece = piecePromotion;
        this.targetPiece = targetPiece;
        this.pos = pos;
        this.squad = squadData;

        if (moveTracker == null)
            moveTracker = FindFirstObjectByType<MoveTracker>();

        if (chessMovesPanel == null)
            chessMovesPanel = FindFirstObjectByType<ChessMovesPanel>();

        if (boardManager == null)
            boardManager = FindFirstObjectByType<BoardChessManager>();

        if (pieceController == null)
            pieceController = FindFirstObjectByType<PieceController>();

        if (!forceMove) // piecePromotion.Player.id != pieceController.botPlayerId || boardManager.localGame
            CreateCanvas(currentPiece);

        if (IA)
        {
            SquadPieceData strongestPiece = null;
            float maxPower = 0;

            foreach (var pieceName in currentPiece.PromotionPieces)
            {
                if (squad.Pieces.ContainsKey(pieceName))
                {
                    SquadPieceData pieceData = squad.Data.Pieces.Find(p => p.Name == pieceName);

                    if (pieceData != null && pieceData.Power > maxPower)
                    {
                        maxPower = pieceData.Power;
                        strongestPiece = pieceData;
                    }
                }
            }

            string spriteName = strongestPiece.NameInSquad;
            Sprite sprite = squad.Sprites[spriteName];

            Promotion(strongestPiece.Name, sprite);
        }

        //StartCoroutine(DestroyObject());
    }

    public void InitializeWithPiece(PieceComponent piecePromotion,
        GameObject canvasPrefab, GameObject buttonPrefab,
        MatchSquadData squadData, Vector2Int targetPos,
        string chosenPieceName, GameObject targetPiece = null)
    {
        this.promotionCanvasPrefab = canvasPrefab;
        this.promotionButtonPrefab = buttonPrefab;
        this.currentPiece = piecePromotion;
        this.targetPiece = targetPiece;
        this.pos = targetPos;
        this.squad = squadData;

        if (moveTracker == null) moveTracker = FindFirstObjectByType<MoveTracker>();
        if (chessMovesPanel == null) chessMovesPanel = FindFirstObjectByType<ChessMovesPanel>();
        if (boardManager == null) boardManager = FindFirstObjectByType<BoardChessManager>();
        if (pieceController == null) pieceController = FindFirstObjectByType<PieceController>();

        isRemotePromotion = true; // ✅ marca como remota antes de tudo

        SquadPieceData pieceData = squadData.Data.Pieces.Find(p => p.Name == chosenPieceName);
        Sprite sprite = squadData.Sprites[pieceData.NameInSquad];

        Promotion(chosenPieceName, sprite);
    }

    void Start()
    {

        if (chessMovesPanel == null)
            chessMovesPanel = FindFirstObjectByType<ChessMovesPanel>();

        if (boardManager == null)
            boardManager = FindFirstObjectByType<BoardChessManager>();

    }

    void Update()
    {

    }

    private float GetObjectHeight(Transform obj)
    {
        float height = 4.8f; // valor padrão caso não tenha renderer ou collider

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
            boardManager = FindFirstObjectByType<BoardChessManager>();

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

        
        bool isBlack = currentPiece.Player.color == Color.black;

        float fixedHeight = 2.60f;
        float heightDirection;
        if(isBlack)
            heightDirection = boardManager.inBlackView ? 1f : -1f;
        else
            heightDirection = boardManager.inBlackView ? -1f : 1f;

        currentPromotionCanvas.transform.position = new Vector3(
            piecetransform.position.x,
            fixedHeight * heightDirection,
            piecetransform.position.z
        );

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
                SquadPieceData pieceData = squad.Data.Pieces.Find(p => p.NameInSquad == pieceName);

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
                        Promotion(pieceName, sprite);
                    });
                }

                newButton.name = $"Btn_{spriteName}";
            }
        }
    }


    public void Promotion(string pieceName, Sprite sprite)
    {
        GameObject newPiece = boardManager.PlacePiece(pieceName, sprite, pos, squad);

        if (newPiece == null)
        {
            Debug.LogError("Erro ao criar peça promovida");
            return;
        }

        PieceComponent component = newPiece.GetComponent<PieceComponent>();
        PieceMovement newMovement = newPiece.GetComponent<PieceMovement>();

        component.IsPromoted = true;

        if (currentPiece.IsKing)
        {
            if (currentPiece.Player.color == Color.white)
                pieceController.KingWhite = component;
            else
                pieceController.KingBlack = component;

            component.IsKing = true;
        }

        // ✅ Apenas registra move e envia pela rede se for local
        if (!isRemotePromotion)
        {
            if (boardManager.isMultiplayer && PieceControllerNetwork.Instance != null)
            {
                MultiplayerPieceController mp = FindFirstObjectByType<MultiplayerPieceController>();
                if (mp != null && mp.IsMyTurnPublic())
                    // ✅ currentPiece.Position já é a origem pois o peão ainda não foi destruído
                    PieceControllerNetwork.Instance.SendPromotion(
                        currentPiece.Position.x, currentPiece.Position.y,
                        pos.x, pos.y,
                        pieceName,
                        currentPiece.Player.id
                    );
            }
        }

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

        boardManager.UpdatePiecePosition(currentPiece.Position, pos, component.Name);
        pieceController.DeselectPiece();

        if (newMovement != null)
            newMovement.enabled = true;

        boardManager.AllPieces.Remove(currentPiece.gameObject);

        SpriteRenderer sr = currentPiece.GetComponent<SpriteRenderer>();

        StartCoroutine(BoardUpdate(house, sr, sprite));
    }


    public IEnumerator BoardUpdate(string house, SpriteRenderer sr, Sprite sprite)
    {
        GameObject pawnToDestroy = currentPiece.gameObject;

        // ✅ Remoto só precisa destruir o peão e atualizar o painel
        if (!isRemotePromotion)
            yield return StartCoroutine(pieceController.DelayedBoardUpdate());
        else
            yield return null; // aguarda um frame para garantir que PlacePiece finalizou

        moveTracker.AddMove(pawnToDestroy, currentPiece, currentPiece.Position, pos);
        chessMovesPanel.AddMove(house, sr.sprite, sprite);

        if (pawnToDestroy != null)
            Destroy(pawnToDestroy);

        if (currentPromotionCanvas)
            Destroy(currentPromotionCanvas);
    }


}