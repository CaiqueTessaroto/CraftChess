using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChessMovesPanel : MonoBehaviour
{

    public GameObject sidePanel;
    public Button OpenBtn;
    public Button CloseBtn;

    [Header("Moves Panel")]
    public GameObject movesPanel;
    public Color color1 = new Color(95, 95, 95, 255);
    public Color color2 = new Color(90, 90, 90, 255);
    public GameObject turnPrefab;
    public GameObject movePrefab;


    private RectTransform content;
    private GameObject currentTurn;
    private int moveCountInCurrentTurn = 0;
    private int totalTurns = 0;

    // Start is called before the first frame update
    void Start()
    {

        content = movesPanel.transform.Find("Scroll View/Viewport/Content").GetComponent<RectTransform>(); ;

        OpenBtn.onClick.AddListener(() =>
        {
            sidePanel.SetActive(true);
        });

        CloseBtn.onClick.AddListener(() =>
        {
            sidePanel.SetActive(false);
        });


    }


    public void AddMove(string moveText, Sprite moveSprite, Sprite promotionSprite = null)
    {
        // Se não há turno atual ou o atual já tem 2 movimentos, cria um novo turno
        if (currentTurn == null || moveCountInCurrentTurn >= 2)
        {
            totalTurns++;

            currentTurn = Instantiate(turnPrefab, content);
            currentTurn.name = "Turn_" + (totalTurns);
            moveCountInCurrentTurn = 0;

            // Alterna cor entre color1 e color2
            Image bg = currentTurn.GetComponent<Image>();
            if (bg != null)
                bg.color = (totalTurns % 2 == 0) ? color2 : color1;

            TMP_Text text = currentTurn.GetComponentInChildren<TMP_Text>();
            text.text = $"{totalTurns}.";

            UIHelperUtils.SetSizeScrollView(movesPanel);
        }

        // Cria o movimento dentro do turno atual
        GameObject moveObj = Instantiate(movePrefab, currentTurn.transform);
        moveObj.name = "Move_" + (moveCountInCurrentTurn + 1);

        // Preenche os dados visuais se existirem componentes
        Image img = moveObj.GetComponentInChildren<Image>();
        TMP_Text txt = moveObj.GetComponentInChildren<TMP_Text>();

        if (img != null && moveSprite != null)
            img.sprite = moveSprite;

        if (txt != null)
            txt.text = moveText;

        if (promotionSprite)
            UIHelperUtils.CreateImage(moveObj.transform, 50, 50, promotionSprite);


        // Atualiza o contador de movimentos
        moveCountInCurrentTurn++;

    }



}
