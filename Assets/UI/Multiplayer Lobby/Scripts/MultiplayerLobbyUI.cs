using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

using Unity.Services.Lobbies.Models;

public class MultiplayerLobbyUI : MonoBehaviour
{

    [Header("Scripts")]
    public GameManager gameManager;
    public NavigationManage_Lobby navigationManage;
    public MultiplayerPieceInfo multiplayerPieceInfo;
    public GridLobby gridLobby;

    [Header("Fotos de Perfil")]
    public Image play1ProfileImage;
    public Image play2ProfileImage;

    [Header("Buttons")]
    public Button play;
    public Button ready;
    public Button Back;
    public Button blackBtn;
    public Button whiteBtn;

    [Header("Options")]
    public GameObject optionsPanel;
    public Button OpenOpt;
    public Button CloseOpt;

    [Header("Code")]
    public TMP_Text codeText;
    public Button copyCodeBtn;

    [Header("Painel Local")]
    public TMP_Text blackSquadName;
    public TMP_Text blackSquadName2;
    public Transform blackPiecesGrid;
    public Button blackDowloadBtn;
    public GameObject blackUnbalancedObj;

    [Header("Painel Oponente")]
    public TMP_Text whiteSquadName;
    public TMP_Text whiteSquadName2;
    public Transform whitePiecesGrid;
    public Button whiteDowloadBtn;
    public GameObject whiteUnbalancedObj;

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

        if (multiplayerPieceInfo == null)
            multiplayerPieceInfo = FindFirstObjectByType<MultiplayerPieceInfo>();

        if (navigationManage == null)
            navigationManage = FindFirstObjectByType<NavigationManage_Lobby>();

        if (gridLobby == null)
            gridLobby = FindFirstObjectByType<GridLobby>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        ready.onClick.AddListener(() =>
        {
            bool next = !MultiplayerLobbyState.ClientIsReady;
            MultiplayerLobbyState.SendReadyStateToHost(next);

            string readyTxt = UIHelperUtils.T("READY");

            if (string.IsNullOrEmpty(readyTxt))
                readyTxt = "Ready";

            string notReadyTxt = UIHelperUtils.T("Cancel_Ready");

            if (string.IsNullOrEmpty(notReadyTxt))
                notReadyTxt = "Cancel Ready";

            ready.GetComponentInChildren<TMP_Text>().text = next ? notReadyTxt : readyTxt;
        });


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

            if (!MultiplayerLobbyState.ClientIsReady)
            {
                string text = UIHelperUtils.T("lobby_client_not_ready")
                            ?? "The other player is not ready yet.";
                FileManager.Instance.CreateAdvice(text);
                return;
            }


        });

        copyCodeBtn.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(codeText.text))
                return;

            GUIUtility.systemCopyBuffer = codeText.text;

            string text = UIHelperUtils.T("lobby_code_copied");

            if (string.IsNullOrEmpty(text))
                text = "Lobby code copied to clipboard.";

            FileManager.Instance.SpawnMessage(text);
        });

        blackDowloadBtn.onClick.AddListener(() =>
        {


            string title = UIHelperUtils.T("downloading_title");

            if (string.IsNullOrEmpty(title))
                title = "Download Squad";

            string message = UIHelperUtils.T("downloading_message");

            if (string.IsNullOrEmpty(message))
                message = "This squad is not available on your device. Do you want to download it?";

            void DownloadSquad()
            {
                MultiplayerLobbyState.DownloadSquad(isWhite: false);
            }

            FileManager.Instance.CreateWarning(title, message, DownloadSquad);

            //MultiplayerLobbyState.DownloadSquad(isWhite: false);
        });

        whiteDowloadBtn.onClick.AddListener(() =>
        {
            string title = UIHelperUtils.T("downloading_title");

            if (string.IsNullOrEmpty(title))
                title = "Download Squad";

            string message = UIHelperUtils.T("downloading_message");

            if (string.IsNullOrEmpty(message))
                message = "This squad is not available on your device. Do you want to download it?";

            void DownloadSquad()
            {
                MultiplayerLobbyState.DownloadSquad(isWhite: true);
            }

            FileManager.Instance.CreateWarning(title, message, DownloadSquad);

            //MultiplayerLobbyState.DownloadSquad(isWhite: true);
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
            MultiplayerLobbyState.Reset();
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

        if (NetworkManager.Singleton.IsListening)
            SetupButtons();

    }

    private void SetupButtons()
    {
        bool isHost = NetworkManager.Singleton.IsHost;

        if (isHost)
            play1ProfileImage.sprite = ProfileImageManager.Instance.defaultSprite;
        else
            play2ProfileImage.sprite = ProfileImageManager.Instance.defaultSprite;

        play.gameObject.SetActive(isHost);
        ready.gameObject.SetActive(!isHost);
    }

    private void OnLobbyUpdated(Lobby lobby)
    {
        // Só o host precisa reagir ao ready do client
        if (!NetworkManager.Singleton.IsHost) return;

        play.interactable = MultiplayerLobbyState.ClientIsReady;
    }

    // ───────────────────────────────────────────────────────────────────────
    // Chamado automaticamente quando ambos os squads chegaram pela rede
    // ───────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        NetworkLobbyManager.Instance.OnLobbyPolled += OnLobbyUpdated;
        NetworkLobbyManager.Instance.StartPollingLobby();

        SquadSyncManager.Instance.OnRemoteSquadReady += OnSquadReady;
    }

    private void OnDisable()
    {
        NetworkLobbyManager.Instance.OnLobbyPolled -= OnLobbyUpdated;
        NetworkLobbyManager.Instance.StopPollingLobby();

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

        UpdateSquadInLobby(squadWhite, whitePiecesGrid, whiteSquadName, whiteSquadName2, false);
        UpdateSquadInLobby(squadBlack, blackPiecesGrid, blackSquadName, blackSquadName2, true);

        gridLobby.ClearGrid(gridLobby.posInGrid);

        //Debug.Log($"[MultiplayerLobbyUI] Painel {(isMySquad ? "local" : "oponente")} atualizado.");
        //Debug.Log($"[MultiplayerLobbyUI] Painel atualizado.");
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

        UpdateSquadInLobby(squadWhite, whitePiecesGrid, whiteSquadName, whiteSquadName2, false);
        UpdateSquadInLobby(squadBlack, blackPiecesGrid, blackSquadName, blackSquadName2, true);

        gridLobby.ClearGrid(gridLobby.posInGrid);

        //Debug.Log($"[MultiplayerLobbyUI] Painel {(isWhite ? "White" : "Black")} atualizado localmente.");
    }

    public void ApplyProfileImages()
    {
        ApplyProfileImage(MultiplayerLobbyState.HostProfileImageRaw, play1ProfileImage);
        ApplyProfileImage(MultiplayerLobbyState.ClientProfileImageRaw, play2ProfileImage);
    }

    private void ApplyProfileImage(byte[] raw, Image target)
    {
        if (target == null || raw == null || raw.Length == 0) return;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(raw)) return;

        Rect rect = new Rect(0, 0, tex.width, tex.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        target.sprite = Sprite.Create(tex, rect, pivot);
    }

    public void UpdateSquadInLobby(MatchSquadData squad, Transform gridPainel, TMP_Text squadName, TMP_Text squadName2, bool isBlack)
    {
        if (squad != null)
        {
            if (squad.Data.Translate)
            {
                string name = UIHelperUtils.T(squad.Data.Name);

                squadName.text = $"{name}\n{squad.Data.Power}";
                squadName2.text = name;
            }
            else
            {
                squadName.text = $"{squad.Data.Name}\n{squad.Data.Power}";
                squadName2.text = squad.Data.Name;
            }

            if (!squad.Data.Balanced)
            {
                if (isBlack)
                    blackUnbalancedObj.SetActive(true);
                else
                    whiteUnbalancedObj.SetActive(true);
            }
            else
            {
                if (isBlack)
                    blackUnbalancedObj.SetActive(false);
                else
                    whiteUnbalancedObj.SetActive(false);
            }

            if (isBlack)
            {
                if (MultiplayerLobbyState.BlackSquadOwnerId != NetworkManager.Singleton.LocalClientId.ToString())
                    blackDowloadBtn.gameObject.SetActive(true);
                else
                    blackDowloadBtn.gameObject.SetActive(false);
            }
            else
            {
                if (MultiplayerLobbyState.WhiteSquadOwnerId != NetworkManager.Singleton.LocalClientId.ToString())
                    whiteDowloadBtn.gameObject.SetActive(true);
                else
                    whiteDowloadBtn.gameObject.SetActive(false);

            }

            RenderPiecesGrid(gridPainel, squad, isBlack);
            gridLobby.LoadPiecesInGrid(squad.Data, squad.Sprites, isBlack);
        }
    }

    // ───────────────────────────────────────────────────────────────────────

    private void RenderPiecesGrid(Transform grid, MatchSquadData squad, bool isBlack)
    {

        if (grid == null) return;

        // Limpa grid anterior
        foreach (Transform child in grid)
            Destroy(child.gameObject);

        foreach (var piece in squad.Data.Pieces)
        {

            //if (squad.Sprites.ContainsKey(piece.NameInSquad))

            Sprite sprite = squad.Sprites[piece.NameInSquad];

            MovementConfigData wrapper = squad.Pieces[piece.NameInSquad];

            GameObject img = Instantiate(piece_ImgPrefab, grid);
            img.name = piece.NameInSquad;

            Image imgComp = img.GetComponent<Image>();
            if (imgComp != null)
                imgComp.sprite = sprite;

            TextMeshProUGUI text = img.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = piece.NameInSquad;

            bool IsKing = false;
            if (piece.NameInSquad == squad.Data.King.Name)
                IsKing = true;

            img.GetComponent<Button>().onClick.AddListener(() =>
            {

                multiplayerPieceInfo.SelectPiece(piece.NameInSquad, piece, wrapper, sprite, isBlack, IsKing);
            });

        }


    }

}