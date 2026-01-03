using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManagerPieceInfo : MonoBehaviour
{

    public InfoGridView infoGridView;
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

    public Dictionary<string, Sprite> pieceSprites = new Dictionary<string, Sprite>();

    // Start is called before the first frame update
    void Start()
    {

        closePiecebtn.onClick.AddListener(() =>
        {
            Panel.SetActive(false);
        });

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SelectPiece(string namePieceSquad, SquadPieceData pieceData, MovementConfigData config, Sprite sprite)
    {

        //MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        if (config.piece.Name != currentPieceName || config.piece.Squad != squadPiece || Panel.activeSelf == false)
        {
            SetInfoPiece(namePieceSquad, pieceData, config, sprite);
            StartCoroutine(SetPromotionsAndCastelingPieces(pieceData, config, sprite));
        }

    }


    public void SetInfoPiece(string namePieceSquad, SquadPieceData pieceData, MovementConfigData config, Sprite sprite)
    {
        //MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        PieceInfo piece = config.piece;

        Panel.SetActive(true);

        currentPieceName = namePieceSquad;
        squadPiece = piece.Squad;

        //spritePiece = sprite;
        previewImage.sprite = sprite;

        nameTmp.text = namePieceSquad;
        powerTmp.text = $"Power: {pieceData.Power}";

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
                yield return StartCoroutine(LoadPiecesImage(name, castelingContent));
            }
        }

        if (pieceData.PromotionPieces != null)
        {
            if (pieceData.PromotionPieces.Count > 0)
                promotion.SetActive(true);

            foreach (string name in pieceData.PromotionPieces)
            {
                yield return StartCoroutine(LoadPiecesImage(name, promotionContent));
            }
        }

        yield return null;


        infoGridView.GenerateGridPiece(config, sprite);

        yield return null;

    }

    public IEnumerator LoadPiecesImage(string fileName, Transform content)
    {
        //Transform content = panel.transform;

        GameObject clone = Instantiate(viewPiecePrefab, content);

        // Define o nome do objeto (opcional)
        clone.name = "Preview_" + fileName;

        // Acha a imagem dentro do painel
        Image img = clone.GetComponentInChildren<Image>();

        Sprite sprite = null;

        if (pieceSprites.ContainsKey(fileName))
            sprite = pieceSprites[fileName];

        if (img != null)
        {
            img.sprite = sprite;
        }

        // Se quiser simular um carregamento assíncrono, pode colocar um yield
        yield return null;
    }



}
