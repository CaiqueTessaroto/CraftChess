using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameInterfaceManager : MonoBehaviour
{
    public BoardChessManager boardChessManager;
    public PieceController pieceController;
    public Button MenuBtn;
    public Button giveUpBtn;
    public Button switchSide;
    public Button ViewInfoBtn;
    public Button BackLobbyBtn;

    [Header("Panel")]
    public GameObject panel;
    public TMP_Text tmpEnd;
    public Button ContinueBtn;

    [Header("Icons Mouse:")]
    public Sprite lupaIcon;
    // Start is called before the first frame update
    void Start()
    {

        if (boardChessManager == null)
            boardChessManager = FindObjectOfType<BoardChessManager>();

        if (pieceController == null)
            pieceController = FindObjectOfType<PieceController>();

        switchSide.onClick.AddListener(() =>
        {
            boardChessManager.SwitchSide();
            boardChessManager.UpdateBoardControl();
        });

        giveUpBtn.onClick.AddListener(() =>
        {

            if (pieceController.endGame)
                return;

            bool black = false;
            bool white = false;
            bool draw = false;

            if (pieceController.botPlayerId == 0)
                white = true;
            else
                black = true;



            pieceController.EndGame(black, white, draw);

        });

        MenuBtn.onClick.AddListener(() =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            SceneManager.LoadScene("Menu");
        });


        ContinueBtn.onClick.AddListener(() =>
        {
            panel.SetActive(false);
            //SceneManager.LoadScene("Single Lobby");
        });

        BackLobbyBtn.onClick.AddListener(() =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            SceneManager.LoadScene("Single Lobby");
        });

        ViewInfoBtn.onClick.AddListener(() =>
        {
            boardChessManager.infoPiece = !boardChessManager.infoPiece;

            boardChessManager.setCursor = !boardChessManager.setCursor;

            UIHelperUtils.SetCursor(lupaIcon, CursorHotspot.TopLeft);


        });

    }

    public void EndGame(string result)
    {
        tmpEnd.text = result;
        panel.SetActive(true);
        BackLobbyBtn.gameObject.SetActive(true);
        MenuBtn.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
