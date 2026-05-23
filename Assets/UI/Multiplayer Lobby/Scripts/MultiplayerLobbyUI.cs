using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerLobbyUI : MonoBehaviour
{

    [Header("Scripts")]
    public GameManager gameManager;
    public NavigationManage_Lobby navigationManage;
    public GridLobby gridLobby;

    [Header("Buttons")]
    public Button play;
    public Button Back;
    public Button blackBtn;
    public Button whiteBtn;

    [Header("Options")]
    public GameObject optionsPanel;
    public Button OpenOpt;
    public Button CloseOpt;

    [Header("Code")]
    public TMP_Text codeText;

    [Header("Painel Local")]
    public TMP_Text blackSquadName;
    public TMP_Text blackSquadName2;
    public Transform blackPiecesGrid;

    [Header("Painel Oponente")]
    public TMP_Text whiteSquadName;
    public TMP_Text whiteSquadName2;
    public Transform whitePiecesGrid;

    [Header("Prefabs")]
    public GameObject piece_ImgPrefab;

    [Header("Control")]
    public bool isWhite = false;
    public static MultiplayerLobbyUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {

        if (navigationManage == null)
            navigationManage = FindFirstObjectByType<NavigationManage_Lobby>();

        if (gridLobby == null)
            gridLobby = FindFirstObjectByType<GridLobby>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        play.onClick.AddListener(() =>
        {

            if (MultiplayerLobbyState.WhiteSquad == null)
            {
                string text = UIHelperUtils.T("select_white");

                if (string.IsNullOrEmpty(text))
                    text = "Select the white pieces.";

                FileManager.Instance.CreateAdvice(text);

                return;
            }

            if (MultiplayerLobbyState.BlackSquad == null)
            {
                string text = UIHelperUtils.T("select_black");

                if (string.IsNullOrEmpty(text))
                    text = "Select the black pieces.";

                FileManager.Instance.CreateAdvice(text);
                return;
            }

            bool hasClient = NetworkLobbyManager.Instance.currentLobby.Players.Count > 1;

            if (!hasClient)
            {
                string text = UIHelperUtils.T("lobby_no_player");

                if (string.IsNullOrEmpty(text))
                    text = "There is no other player connected to the lobby.";

                FileManager.Instance.CreateAdvice(text);
                return;
            }


        });


        blackBtn.onClick.AddListener(() =>
        {
            isWhite = false;

            navigationManage.StartFormationsButtons();

        });


        whiteBtn.onClick.AddListener(() =>
        {
            isWhite = true;

            navigationManage.StartFormationsButtons();
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

    }


    // ───────────────────────────────────────────────────────────────────────
    // Chamado quando o jogador seleciona um squad localmente
    // Chame isso no botão de seleção junto com SelectSquad e SetLocalSquadAndSync
    // ───────────────────────────────────────────────────────────────────────

    public void UpdateLocalPanel(string squadName, MatchSquadData squad)
    {
        if (blackSquadName != null)
            blackSquadName.text = squadName;

        RenderPiecesGrid(blackPiecesGrid, squad.Sprites);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Chamado automaticamente quando ambos os squads chegaram pela rede
    // ───────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        SquadSyncManager.Instance.OnRemoteSquadReady += OnSquadReady;
    }

    private void OnDisable()
    {
        SquadSyncManager.Instance.OnRemoteSquadReady -= OnSquadReady;
    }

    private void OnSquadReady(bool isWhite)
    {
        MatchSquadData squadWhite = MultiplayerLobbyState.WhiteSquad;
        MatchSquadData squadBlack = MultiplayerLobbyState.BlackSquad;

        if (gridLobby == null)
        {
            Debug.LogWarning($"[MultiplayerLobbyUI] gridLobby não atribuído.");
            return;
        }


        gridLobby.posInGrid.Clear();

        if (squadWhite != null)
        {


            if (squadBlack.Data.Translate)
            {
                string name = UIHelperUtils.T(squadWhite.Data.Name);

                whiteSquadName.text = $"{name}\n{squadWhite.Data.Power}";
                whiteSquadName2.text = name;
            }
            else
            {
                whiteSquadName.text = $"{squadWhite.Data.Name}\n{squadWhite.Data.Power}";
                whiteSquadName2.text = squadWhite.Data.Name;
            }


            RenderPiecesGrid(whitePiecesGrid, squadWhite.Sprites);
            gridLobby.LoadPiecesInGrid(squadWhite.Data, squadWhite.Sprites, false);
        }
        if (squadBlack != null)
        {

            if (squadBlack.Data.Translate)
            {
                string name = UIHelperUtils.T(squadBlack.Data.Name);

                blackSquadName.text = $"{name}\n{squadBlack.Data.Power}";
                blackSquadName2.text = name;
            }
            else
            {
                blackSquadName.text = $"{squadBlack.Data.Name}\n{squadBlack.Data.Power}";
                blackSquadName2.text = squadBlack.Data.Name;
            }

            
            RenderPiecesGrid(blackPiecesGrid, squadBlack.Sprites);
            gridLobby.LoadPiecesInGrid(squadBlack.Data, squadBlack.Sprites, true);
        }

        gridLobby.ClearGrid(gridLobby.posInGrid);

        //Debug.Log($"[MultiplayerLobbyUI] Painel {(isMySquad ? "local" : "oponente")} atualizado.");
        Debug.Log($"[MultiplayerLobbyUI] Painel atualizado.");
    }

    public void RefreshLocalUI()
    {
        MultiplayerLobbyState.Log("RefreshLocalUI");

        if (this == null || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[MultiplayerLobbyUI] RefreshLocalUI chamado mas objeto inativo.");
            return;
        }

        RefreshSquadPanel(MultiplayerLobbyState.LocalIsWhite);
    }

    private void RefreshSquadPanel(bool isWhite)
    {

        if (gridLobby == null)
        {
            Debug.LogWarning($"[MultiplayerLobbyUI] gridLobby nulo em RefreshSquadPanel.");
            return;
        }

        MatchSquadData squadWhite = MultiplayerLobbyState.WhiteSquad;
        MatchSquadData squadBlack = MultiplayerLobbyState.BlackSquad;

        gridLobby.posInGrid.Clear();

        if (squadWhite != null)
        {
            if (whiteSquadName != null) whiteSquadName.text = squadWhite.Data.Name;
            RenderPiecesGrid(whitePiecesGrid, squadWhite.Sprites);
            gridLobby.LoadPiecesInGrid(squadWhite.Data, squadWhite.Sprites, false);
        }
        if (squadBlack != null)
        {
            if (blackSquadName != null) blackSquadName.text = squadBlack.Data.Name;
            RenderPiecesGrid(blackPiecesGrid, squadBlack.Sprites);
            gridLobby.LoadPiecesInGrid(squadBlack.Data, squadBlack.Sprites, true);
        }

        gridLobby.ClearGrid(gridLobby.posInGrid);

        Debug.Log($"[MultiplayerLobbyUI] Painel {(isWhite ? "White" : "Black")} atualizado localmente.");
    }

    // ───────────────────────────────────────────────────────────────────────

    private void RenderPiecesGrid(Transform grid, Dictionary<string, Sprite> sprites)
    {
        if (grid == null) return;

        // Limpa grid anterior
        foreach (Transform child in grid)
            Destroy(child.gameObject);

        foreach (var kv in sprites)
        {
            GameObject img = Instantiate(piece_ImgPrefab, grid);
            img.name = kv.Key;

            Image imgComp = img.GetComponent<Image>();
            if (imgComp != null)
                imgComp.sprite = kv.Value;

            TextMeshProUGUI text = img.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = kv.Key;
        }
    }

}