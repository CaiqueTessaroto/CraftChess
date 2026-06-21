using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameInterfaceManager : MonoBehaviour
{
    public BoardChessManager boardChessManager;
    public PieceController pieceController;
    public MoveTracker moveTracker;
    public GameManager gameManager;
    public Button MenuBtn;
    public Button giveUpBtn;
    public Button switchSide;
    public Button ViewInfoBtn;
    public Button BackLobbyBtn;

    [Header("End Panel")]
    public GameObject panel;
    public TMP_Text tmpEnd;
    public Button returnBtn;
    public Button continueBtn;

    [Header("EndLocal Panel")]
    public GameObject panelLocal;
    public TMP_Text tmpEndLocal;
    public Image imageAvatar;
    public TMP_Text tmpSquad;
    public Button returnBtn2;
    public Button continueBtn2;

    [Header("Icons Mouse:")]
    public Sprite lupaIcon;
    public Image buttonImage;
    // Start is called before the first frame update
    void Start()
    {

        buttonImage = ViewInfoBtn.GetComponent<Image>();

        if (boardChessManager == null)
            boardChessManager = FindFirstObjectByType<BoardChessManager>();

        if (pieceController == null)
            pieceController = FindFirstObjectByType<PieceController>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (moveTracker == null)
            moveTracker = FindFirstObjectByType<MoveTracker>();

        switchSide.onClick.AddListener(() =>
        {
            boardChessManager.SwitchSide();
            boardChessManager.UpdateBoardControl();
        });

        giveUpBtn.onClick.AddListener(() =>
        {
            if (pieceController.endGame)
                return;

            if (boardChessManager.isMultiplayer)
            {
                if (NetworkLobbyManager.Instance.IsConnected())
                    PieceControllerNetwork.Instance.SendGiveUp();
                return; // resultado vem pelo ClientRpc
            }

            // lógica local/bot permanece igual
            bool black = false, white = false, draw = false;

            if (boardChessManager.localGame)
            {
                if (moveTracker.GetTurnPlayer() == 0)
                    black = true;
                else
                    white = true;
            }
            else
            {
                if (pieceController.botPlayerId == 0)
                    white = true;
                else
                    black = true;
            }

            pieceController.SetEndGame(black, white, draw);
        });


        MenuBtn.onClick.AddListener(() =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            if (boardChessManager.isMultiplayer)
            {
                LeaveToMenu();
            }
            else
            {
                gameManager.ChangeScene("Menu");
            }
        });


        returnBtn.onClick.AddListener(() =>
        {
            panel.SetActive(false);
        });

        continueBtn.onClick.AddListener(() =>
        {
            if (boardChessManager.isMultiplayer)
                if (NetworkLobbyManager.Instance.IsConnected())
                    gameManager.ChangeScene("Multiplayer Lobby");
                else
                    LeaveToMenu();
            else
                gameManager.ChangeScene("Single Lobby");
        });

        returnBtn2.onClick.AddListener(() =>
        {
            panelLocal.SetActive(false);
        });

        continueBtn2.onClick.AddListener(() =>
        {
            if (boardChessManager.isMultiplayer)
                if (NetworkLobbyManager.Instance.IsConnected())
                    gameManager.ChangeScene("Multiplayer Lobby");
                else
                    LeaveToMenu();
            else
                gameManager.ChangeScene("Single Lobby");
        });

        BackLobbyBtn.onClick.AddListener(() =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            if (boardChessManager.isMultiplayer)
                if (NetworkLobbyManager.Instance.IsConnected())
                    gameManager.ChangeScene("Multiplayer Lobby");
                else
                    LeaveToMenu();
            else
                gameManager.ChangeScene("Single Lobby");
        });

        ViewInfoBtn.onClick.AddListener(() =>
        {
            boardChessManager.infoPiece = !boardChessManager.infoPiece;

            boardChessManager.setCursor = true;

            //buttonImage.color = new Color32(118, 130, 162, 255);
            buttonImage.color = new Color32(240, 75, 79, 255);

            UIHelperUtils.SetCursor(lupaIcon, CursorHotspot.TopLeft);


        });


    }


    public void LeaveToMenu()
    {
        MultiplayerLobbyState.Reset();
        try
        {
            if (NetworkLobbyManager.Instance.IsConnected())
            {
                NetworkLobbyManager.Instance.HandleDisconnect();
            }
            else
            {
                NetworkLobbyManager.Instance.currentLobby = null;
                gameManager.ChangeScene("Menu");
            }
        }
        catch
        {
            gameManager.ChangeScene("Menu");
        }
    }

    public void EndGame(string result)
    {
        string endTxt = UIHelperUtils.T(result);

        if (string.IsNullOrEmpty(endTxt))
            endTxt = result;

        tmpEnd.text = endTxt;
        panel.SetActive(true);

        BackLobbyBtn.gameObject.SetActive(true);
        MenuBtn.gameObject.SetActive(true);
    }

    public void EndGameLocal(string squadName, Sprite avatar)
    {
        string endTxt = UIHelperUtils.T("Victory");

        if (string.IsNullOrEmpty(endTxt))
            endTxt = "Victory";

        tmpEndLocal.text = endTxt;

        panelLocal.SetActive(true);
        imageAvatar.sprite = avatar;
        tmpSquad.text = squadName;

        BackLobbyBtn.gameObject.SetActive(true);
        MenuBtn.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

        if (!boardChessManager.infoPiece && boardChessManager.setCursor)
        {
            buttonImage.color = new Color32(240, 75, 79, 0);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            boardChessManager.setCursor = false;
        }
    }

}
