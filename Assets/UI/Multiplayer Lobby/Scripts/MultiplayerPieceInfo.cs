using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerPieceInfo : MonoBehaviour
{
    public InfoGridView infoGridView;
    public GameObject crowView;
    public string currentPieceName;
    public string squadPiece;
    public TMP_Text nameTmp;
    public TMP_Text powerTmp;
    public GameObject Panel;
    public Image previewImage;
    public GameObject promotion;
    public GameObject casteling;
    public Transform promotionContent;
    public Transform castelingContent;
    public GameObject viewPiecePrefab;
    public Button closePiecebtn;

    private bool isWhite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        closePiecebtn.onClick.AddListener(() =>
        {
            Panel.SetActive(false);

        });

    }

    public void SelectPiece(string namePieceSquad, SquadPieceData pieceData, MovementConfigData config, Sprite sprite, bool isBlack, bool IsKing)
    {

        isWhite = !isBlack;

        if (config.piece.Name != currentPieceName || config.piece.Squad != squadPiece || Panel.activeSelf == false)
        {
            SetInfoPiece(namePieceSquad, pieceData, config, sprite, IsKing);
            StartCoroutine(SetPromotionsAndCastelingPieces(pieceData, config, sprite));
        }

    }


    public void SetInfoPiece(string namePieceSquad, SquadPieceData pieceData, MovementConfigData config, Sprite sprite, bool IsKing)
    {

        PieceInfo piece = config.piece;

        bool translate = UIHelperUtils.CheckTranslationFile(Application.persistentDataPath, "Pieces", pieceData.Squad);

        string currentTname = namePieceSquad;

        if (translate)
        {
            currentTname = UIHelperUtils.T(namePieceSquad);
            if (string.IsNullOrEmpty(currentTname))
                currentTname = namePieceSquad;
        }


        Panel.SetActive(true);

        currentPieceName = namePieceSquad;
        squadPiece = piece.Squad;

        //spritePiece = sprite;
        previewImage.sprite = sprite;

        if (LocalizationManager.Instance)
            nameTmp.font = LocalizationManager.Instance.currentFont;

        nameTmp.text = currentTname;

        crowView.SetActive(IsKing);

        if (LocalizationManager.Instance)
            powerTmp.font = LocalizationManager.Instance.currentFont;

        powerTmp.text = UIHelperUtils.SetPowerText(pieceData.Power);

    }

    public IEnumerator SetPromotionsAndCastelingPieces(SquadPieceData pieceData, MovementConfigData config, Sprite sprite)
    {

        //MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        casteling.SetActive(false);
        promotion.SetActive(false);

        foreach (Transform child in promotionContent.transform)
            Destroy(child.gameObject);

        foreach (Transform child in castelingContent.transform)
            Destroy(child.gameObject);

        if (pieceData.CastlingPieces != null)
        {
            if (pieceData.CastlingPieces.Count > 0)
                casteling.SetActive(true);

            foreach (string name in pieceData.CastlingPieces)
            {
                yield return StartCoroutine(LoadPiecesImage(name, pieceData.Squad, castelingContent));
            }
        }

        if (pieceData.PromotionPieces != null)
        {
            if (pieceData.PromotionPieces.Count > 0)
                promotion.SetActive(true);

            foreach (string name in pieceData.PromotionPieces)
            {
                yield return StartCoroutine(LoadPiecesImage(name, pieceData.Squad, promotionContent));
            }
        }

        yield return null;


        infoGridView.GenerateGridPiece(config, sprite);

        yield return null;

    }

    //string name = fileName.Replace(squad, "").Trim();
    //string name = fileName.Trim();
    //string name = fileName.Replace(" ", "").Trim();

    public IEnumerator LoadPiecesImage(string fileName, string squad, Transform content)
    {
        //Transform content = panel.transform;

        GameObject clone = Instantiate(viewPiecePrefab, content);

        // Define o nome do objeto (opcional)
        clone.name = "Preview_" + fileName;

        // Acha a imagem dentro do painel
        Image img = clone.GetComponentInChildren<Image>();

        Sprite sprite = null;

        if (isWhite)
        {
            if (MultiplayerLobbyState.WhiteSquad?.Sprites != null)
            {
                if (MultiplayerLobbyState.WhiteSquad.Sprites.ContainsKey($"{fileName}"))
                    sprite = MultiplayerLobbyState.WhiteSquad.Sprites[$"{fileName}"];
                else
                    Debug.LogWarning($"Sprite not found for piece: {fileName} in squad: {squad}");
            }
            else
            {
                Debug.LogWarning("MultiplayerLobbyState or squads not properly initialized.");
            }
        }

        if(!isWhite)
        {
            if (MultiplayerLobbyState.BlackSquad?.Sprites != null)
            {
                if (MultiplayerLobbyState.BlackSquad.Sprites.ContainsKey($"{fileName}"))
                    sprite = MultiplayerLobbyState.BlackSquad.Sprites[$"{fileName}"];
                else
                    Debug.LogWarning($"Sprite not found for piece: {fileName} in squad: {squad}");
            }
            else
            {
                Debug.LogWarning("MultiplayerLobbyState or squads not properly initialized.");
            }
        }

        if (img != null)
        {
            img.sprite = sprite;
        }

        // Se quiser simular um carregamento assíncrono, pode colocar um yield
        yield return null;
    }



}

