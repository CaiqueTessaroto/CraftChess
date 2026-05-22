using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ManagerLobby : MonoBehaviour
{

    public GameManager gameManager;

    [Header("Buttons")]
    public Button play;
    public Button Back;

    [Header("Buttons Color")]
    public Button blackBtn;
    public Button whiteBtn;

    [Header("Options")]
    public GameObject optionsPanel;
    public Button OpenOpt;
    public Button CloseOpt;

    [Header("Code")]
    public TMP_Text codeText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();


        play.onClick.AddListener(() =>
        {

        });

        blackBtn.onClick.AddListener(() =>
        {
            //OnWhite = false;

            //navigationManage.StartFormationsButtons();

        });

        whiteBtn.onClick.AddListener(() =>
        {
            //OnWhite = true;

            //navigationManage.StartFormationsButtons();

        });


        Back.onClick.AddListener(() =>
        {
            //sair do lobby
            try
            {
                NetworkLobbyManager.Instance.LeaveLobby("Menu");
            }
            catch
            {
                gameManager.ChangeScene("Menu");
            }

        });

        OpenOpt.onClick.AddListener(() =>
        {
            optionsPanel.SetActive(true);
            //currentMatch.options = true;
            OpenOpt.gameObject.SetActive(false);
        });

        CloseOpt.onClick.AddListener(() =>
        {
            optionsPanel.SetActive(false);
            //currentMatch.options = false;
            OpenOpt.gameObject.SetActive(true);
        });

        codeText.text = "";

        var lobbyManager = NetworkLobbyManager.Instance;

        if (lobbyManager == null)
            return;

        if (lobbyManager.currentLobby == null)
            return;

        if (string.IsNullOrEmpty(lobbyManager.currentLobby.LobbyCode))
            return;

        codeText.text = lobbyManager.currentLobby.LobbyCode;

        //codeText.text = LobbyManager.Instance.LobbyCode.Value.ToString();

    }


}