using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractiveMultiplayerLobby : MonoBehaviour
{
    //SingleLobbyManager
    public FileManager fileManager;
    public GridLobby gridLobby;
    //public MatchConfig currentMatch;

    [Header("Scripts")]
    public ManagerPieceInfo managerPieceInfo;
    public NavigationManage_Lobby navigationManage;

    [Header("Buttons")]
    public Button play;
    public Button blackBtn;
    public Button whiteBtn;

    [Header("BlackSquad")]
    //public GameObject userSelect;
    public TMP_Text blackSquadTMP;
    public TMP_Text blackSquadTMP2;
    public Transform BlackPiecesGrid;

    [Header("WhiteSquad")]
    //public GameObject enemySelect;
    public TMP_Text whiteSquadTMP;
    public TMP_Text whiteSquadTMP2;
    public Transform WhitePiecesGrid;

    [Header("Prefabs")]
    public GameObject piece_ImgPrefab;

    [Header("Match")]
    public SingleMatchConfig currentMatch;

    [Header("Control")]
    public bool OnWhite = false;
    public string currentWhiteRootPath;
    public string currentBlackRootPath;
    string currentWhiteTname;
    string currentBlackTname;
    int MaxVisiblePieces = 16;

    public List<MatchSquadData> Squads = new List<MatchSquadData>();

    private MatchSquadData BlackSquad = new MatchSquadData();
    private MatchSquadData WhiteSquad = new MatchSquadData();


    //SquadDataWrapper
    //PieceWrapper
    void Start()
    {

        if (managerPieceInfo == null)
            managerPieceInfo = FindFirstObjectByType<ManagerPieceInfo>();

        if (gridLobby == null)
            gridLobby = FindFirstObjectByType<GridLobby>();

        //userTMP.text = "Player20";


        play.onClick.AddListener(() =>
        {

            if (string.IsNullOrEmpty(currentMatch.WhiteSquadName))
            {
                string text = UIHelperUtils.T("select_white");

                if (string.IsNullOrEmpty(text))
                    text = "Select the white pieces.";

                fileManager.CreateAdvice(text);
                return;
            }
            if (string.IsNullOrEmpty(currentMatch.BlackSquadName))
            {
                string text = UIHelperUtils.T("select_black");

                if (string.IsNullOrEmpty(text))
                    text = "Select the black pieces.";

                fileManager.CreateAdvice(text);
                return;
            }

            currentMatch.MapName = "Default";

            if (currentMatch.StartOption == StartOption.Black)
            {
                BlackSquad.Player = new Player("Jogador", 1, Color.black);
                WhiteSquad.Player = new Player("Bot", 0, Color.white);

                Squads.Add(WhiteSquad);
                Squads.Add(BlackSquad);

            }
            else if (currentMatch.StartOption == StartOption.White)
            {
                WhiteSquad.Player = new Player("Jogador", 0, Color.white);
                BlackSquad.Player = new Player("Bot", 1, Color.black);

                Squads.Add(WhiteSquad);
                Squads.Add(BlackSquad);
            }
            else
            {
                bool userStarts = UnityEngine.Random.value > 0.5f;

                if (userStarts)
                {
                    BlackSquad.Player = new Player("Jogador", 1, Color.black);
                    WhiteSquad.Player = new Player("Bot", 0, Color.white);

                    Squads.Add(WhiteSquad);
                    Squads.Add(BlackSquad);

                }
                else
                {
                    WhiteSquad.Player = new Player("Jogador", 0, Color.white);
                    BlackSquad.Player = new Player("Bot", 1, Color.black);

                    Squads.Add(WhiteSquad);
                    Squads.Add(BlackSquad);
                }

                //Debug.Log($"Começo aleatório → {(userStarts ? "Jogador começa" : "Bot começa")}");
            }

        });

        blackBtn.onClick.AddListener(() =>
        {
            OnWhite = false;

            navigationManage.StartFormationsButtons();

        });


        whiteBtn.onClick.AddListener(() =>
        {
            OnWhite = true;

            navigationManage.StartFormationsButtons();
        });


    }

    public void SelectSquad(string rootPath, string folderName, string squadName, string jsonFile)
    {
        if (OnWhite)
        {
            managerPieceInfo.pieceSpritesWhite.Clear();

            currentWhiteRootPath = rootPath;

            WhiteSquad.Clear();

            currentMatch.WhiteSquadName = folderName;

            CreatePiecesVisualization(jsonFile, WhitePiecesGrid);

            whiteSquadTMP.text = $"{squadName}\n{WhiteSquad.Data.Power}";
            whiteSquadTMP2.text = squadName;

            currentWhiteTname = squadName;

            string squadFolder = Path.Combine(currentWhiteRootPath, fileManager.basePath_SquadData, currentMatch.WhiteSquadName);
            //string squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.WhiteSquadName);
            string jsonFileWhite = Path.Combine(squadFolder, currentMatch.WhiteSquadName + ".json");

            if (File.Exists(jsonFileWhite))
                CreatePiecesVisualization(jsonFileWhite, WhitePiecesGrid);
        }
        else
        {
            managerPieceInfo.pieceSpritesBlack.Clear();

            currentBlackRootPath = rootPath;
            BlackSquad.Clear();

            currentMatch.BlackSquadName = folderName;

            CreatePiecesVisualization(jsonFile, BlackPiecesGrid);

            blackSquadTMP.text = $"{squadName}\n{BlackSquad.Data.Power}";
            blackSquadTMP2.text = squadName;

            currentBlackTname = squadName;

            string squadFolder = Path.Combine(currentBlackRootPath, fileManager.basePath_SquadData, currentMatch.BlackSquadName);
            //string squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.BlackSquadName);
            string jsonFileBlack = Path.Combine(squadFolder, currentMatch.BlackSquadName + ".json");

            if (File.Exists(jsonFileBlack))
                CreatePiecesVisualization(jsonFileBlack, BlackPiecesGrid);
        }

        navigationManage.panelSquad.SetActive(false);

    }




    private void CreatePiecesVisualization(string jsonFile, Transform content)
    {
        // Lê e desserializa o JSON principal (formação)
        string jsonText = File.ReadAllText(jsonFile);
        Squad data = JsonUtility.FromJson<Squad>(jsonText);

        // Limpa UI antiga
        foreach (Transform child in content)
            Destroy(child.gameObject);

        int elementCount = 0;

        foreach (SquadPieceData piece in data.Pieces)
        {

            string loadRootPath = Application.persistentDataPath;//piece.NativePiece ? Application.streamingAssetsPath :

            string jsonPath = Path.Combine(
                loadRootPath,
                fileManager.basePath_PieceData,
                piece.Squad,
                piece.Name + ".json"
            );

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"[Formation Loader] Arquivo da peça não encontrado: {jsonPath}");
                continue;
            }

            // Lê o JSON da peça
            string json = File.ReadAllText(jsonPath);
            MovementConfigData wrapper = JsonUtility.FromJson<MovementConfigData>(json);

            // Caminho do sprite
            string caminhoSprite = "";

            if (wrapper.piece.NativeSprite)
            {
                caminhoSprite = Path.Combine(Application.streamingAssetsPath, fileManager.basePath_Sprite, wrapper.piece.FolderSprite, wrapper.piece.Art.Trim() + ".png");
            }
            else
            {
                caminhoSprite = Path.Combine(Application.persistentDataPath, fileManager.basePath_Sprite, wrapper.piece.FolderSprite, wrapper.piece.Art.Trim() + ".png");
            }

            if (!File.Exists(caminhoSprite))
            {
                Debug.LogWarning("Sprite não encontrada: " + caminhoSprite);
            }

            Sprite sprite = UIHelperUtils.GetSpriteFromPath(caminhoSprite);

            if (elementCount < MaxVisiblePieces)
            {
                // Instancia o botão/imagem da peça no painel
                GameObject newImage = Instantiate(piece_ImgPrefab, content);
                newImage.name = piece.NameInSquad;

                // Define sprite
                Image imgComp = newImage.GetComponent<Image>();
                if (imgComp != null)
                    imgComp.sprite = sprite;

                // Define texto
                TextMeshProUGUI textComp = newImage.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                    textComp.text = string.IsNullOrEmpty(piece.NameInSquad) ? wrapper.piece.Art : piece.NameInSquad;

                newImage.GetComponent<Button>().onClick.AddListener(() =>
                {
                    bool IsKing = false;
                    if (piece.NameInSquad == data.King.Name)
                        IsKing = true;

                    managerPieceInfo.SelectPiece(piece.NameInSquad, piece, wrapper, sprite, IsKing);
                    //    squadManager.SelectPiece(nameInSquad, pieceData, File.ReadAllText(jsonPath), sprite, rootPath);
                });

            }

            // --- 🔹 Guarda dados em cache conforme o lado (usuário ou inimigo) ---
            if (OnWhite)
            {
                if (!WhiteSquad.Sprites.ContainsKey(piece.NameInSquad))
                    WhiteSquad.Sprites[piece.NameInSquad] = sprite;

                if (!WhiteSquad.Pieces.ContainsKey(piece.NameInSquad))
                    WhiteSquad.Pieces[piece.NameInSquad] = wrapper;

                if (!managerPieceInfo.pieceSpritesWhite.ContainsKey($"{piece.NameInSquad}{piece.Squad}"))
                {
                    managerPieceInfo.pieceSpritesWhite[$"{piece.NameInSquad}{piece.Squad}"] = sprite;
                }

            }
            else
            {
                if (!BlackSquad.Sprites.ContainsKey(piece.NameInSquad))
                    BlackSquad.Sprites[piece.NameInSquad] = sprite;

                if (!BlackSquad.Pieces.ContainsKey(piece.NameInSquad))
                    BlackSquad.Pieces[piece.NameInSquad] = wrapper;

                if (!managerPieceInfo.pieceSpritesBlack.ContainsKey($"{piece.NameInSquad}{piece.Squad}"))
                {
                    managerPieceInfo.pieceSpritesBlack[$"{piece.NameInSquad}{piece.Squad}"] = sprite;
                }
            }

            elementCount += 1;

        }

        // --- 🔹 Ao final, guarda o Squad completo ---
        if (OnWhite)
            WhiteSquad.Data = data;
        else
            BlackSquad.Data = data;


        gridLobby.posInGrid.Clear();


        if (BlackSquad.Data != null)
            gridLobby.LoadPiecesInGrid(BlackSquad.Data, BlackSquad.Sprites, true);


        if (WhiteSquad.Data != null)
            gridLobby.LoadPiecesInGrid(WhiteSquad.Data, WhiteSquad.Sprites);


        gridLobby.ClearGrid(gridLobby.posInGrid);

    }













}