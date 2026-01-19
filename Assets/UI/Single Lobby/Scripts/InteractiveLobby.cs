using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class MatchSquadData
{
    public Squad Data;
    public Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    public Dictionary<string, MovementConfigData> Pieces = new Dictionary<string, MovementConfigData>();
    public Player Player;

    public void Clear()
    {
        Sprites.Clear();
        Pieces.Clear();
        Data = null;
    }

}

public class InteractiveLobby : MonoBehaviour
{
    //SingleLobbyManager
    public FileManager fileManager;
    public SingleLobbyManager managerLobby;
    public GridLobby gridLobby;
    //public MatchConfig currentMatch;

    [Header("Scripts")]
    public ManagerPieceInfo managerPieceInfo;
    public NavigationManage_SingleLobby navigationManage;

    [Header("Options")]
    public GameObject optionsPanel;
    public Button OpenOpt;
    public Button CloseOpt;
    public Toggle noRulesToggle;
    public Toggle noTurnsToggle;
    public Toggle localGameToggle;
    public Toggle AutoSwitchSideToggle;
    public Toggle IAvsIAToggle;

    [Header("Buttons")]
    public Button play;

    [Header("Map")]
    public Image map;

    [Header("BotView")]
    public Image ImageHuman;
    public Image ImageBot;
    public Sprite Human;
    public Sprite Robot;

    [Header("Crows")]
    public Image crowPlayer1;
    public Image crowPlayer2;

    [Header("Sprite Crows")]
    public Sprite crowWhite;
    public Sprite crowRandon;
    public Sprite crowBlack;

    [Header("ToggleGroup:")]
    public ToggleGroup difficultyOption;
    public ToggleGroup startOption;
    private Toggle[] difficulty_toggles;
    private Toggle[] startOption_toggles;

    [Header("BlackSquad")]
    //public GameObject userSelect;
    public Button userSquadView;
    public TMP_Text blackSquadTMP;
    public TMP_Text blackSquadTMP2;
    public Button blackBtn;
    public Transform BlackPiecesGrid;

    [Header("WhiteSquad")]
    //public GameObject enemySelect;
    public Button enemySquadView;
    public TMP_Text whiteSquadTMP;
    public TMP_Text whiteSquadTMP2;
    public Button whiteBtn;
    public Transform WhitePiecesGrid;

    [Header("Prefabs")]
    public GameObject piece_ImgPrefab;

    [Header("Match")]
    public SingleMatchConfig currentMatch;

    [Header("Control")]
    public bool OnWhite = false;
    public string currentWhiteRootPath;
    public string currentBlackRootPath;

    public List<MatchSquadData> Squads = new List<MatchSquadData>();

    private MatchSquadData BlackSquad = new MatchSquadData();
    private MatchSquadData WhiteSquad = new MatchSquadData();


    //SquadDataWrapper
    //PieceWrapper
    void Start()
    {

        if (managerPieceInfo == null)
            managerPieceInfo = FindObjectOfType<ManagerPieceInfo>();

        if (gridLobby == null)
            gridLobby = FindObjectOfType<GridLobby>();

        //userTMP.text = "Player20";

        OpenOpt.onClick.AddListener(() =>
        {
            optionsPanel.SetActive(true);
            currentMatch.options = true;
            OpenOpt.gameObject.SetActive(false);
        });

        CloseOpt.onClick.AddListener(() =>
        {
            optionsPanel.SetActive(false);
            currentMatch.options = false;
            OpenOpt.gameObject.SetActive(true);
        });

        noRulesToggle.onValueChanged.AddListener(OnNoRulesChanged);
        noTurnsToggle.onValueChanged.AddListener(OnNoTurnsChanged);
        localGameToggle.onValueChanged.AddListener(OnLocalGameChanged);

        AutoSwitchSideToggle.onValueChanged.AddListener(OnAutoSwitchSideChanged);

        IAvsIAToggle.onValueChanged.AddListener(OnIAvsIAChanged);

        //OnAutoSwitchSideChanged


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

            managerLobby.SaveMatchConfig(currentMatch);
            managerLobby.StartMatch(currentMatch, Squads);
        });

        blackBtn.onClick.AddListener(() =>
        {
            OnWhite = false;

            navigationManage.StartFormationsButtons();

        });

        userSquadView.onClick.AddListener(() =>
        {
            OnWhite = false;

            navigationManage.StartFormationsButtons();

        });

        whiteBtn.onClick.AddListener(() =>
        {
            OnWhite = true;

            navigationManage.StartFormationsButtons();
        });


        enemySquadView.onClick.AddListener(() =>
        {
            OnWhite = true;

            navigationManage.StartFormationsButtons();
        });

        difficulty_toggles = difficultyOption.GetComponentsInChildren<Toggle>();

        foreach (Toggle toggle in difficulty_toggles)
        {
            toggle.onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    //Debug.Log("Toggle ativado: " + toggle.name);

                    // remover "(Toggle)" se tiver no nome
                    string cleanName = toggle.name.Replace(" (Toggle)", "");

                    if (Enum.TryParse(cleanName, true, out BotDifficulty diff))
                    {
                        currentMatch.BotDifficulty = diff; // agora fica salvo no enum
                        //Debug.Log("Dificuldade selecionada: " + currentMatch.BotDifficulty);
                    }
                    else
                    {
                        Debug.LogWarning("Nome do toggle não corresponde ao enum: " + cleanName);
                    }
                }
            });
        }

        startOption_toggles = startOption.GetComponentsInChildren<Toggle>();

        foreach (Toggle toggle in startOption_toggles)
        {
            toggle.onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    //Debug.Log("Toggle ativado: " + toggle.name);

                    // remover "(Toggle)" se tiver no nome
                    string cleanName = toggle.name.Replace(" (Toggle)", "");

                    if (Enum.TryParse(cleanName, true, out StartOption diff))
                    {
                        currentMatch.StartOption = diff; // agora fica salvo no enum

                        switch (currentMatch.StartOption)
                        {
                            case StartOption.White:
                                crowPlayer1.sprite = crowWhite;
                                crowPlayer2.sprite = crowBlack;
                                return;

                            case StartOption.Random:
                                crowPlayer1.sprite = crowRandon;
                                crowPlayer2.sprite = crowRandon;
                                return;

                            case StartOption.Black:
                                crowPlayer1.sprite = crowBlack;
                                crowPlayer2.sprite = crowWhite;
                                return;

                            default:
                                crowPlayer1.sprite = crowWhite;
                                crowPlayer2.sprite = crowBlack;
                                return;
                        }
                        //Debug.Log("StartOption selecionada: " + currentMatch.StartOption);
                    }
                    else
                    {
                        Debug.LogWarning("Nome do toggle não corresponde ao enum: " + cleanName);
                    }
                }
            });
        }


        LoadMatchConfig();

    }

    void OnNoRulesChanged(bool value)
    {
        currentMatch.noRules = value;
    }

    void OnNoTurnsChanged(bool value)
    {
        currentMatch.noTurns = value;
    }

    void OnLocalGameChanged(bool value)
    {
        currentMatch.localGame = value;

        ImageHuman.sprite = Human;
        ImageBot.sprite = value ? Human : Robot;

        if (value)
        {
            IAvsIAToggle.SetIsOnWithoutNotify(false);
            currentMatch.IAvsIA = false;
        }
    }

    void OnAutoSwitchSideChanged(bool value)
    {
        currentMatch.switchSide = value;
    }

    void OnIAvsIAChanged(bool value)
    {
        currentMatch.IAvsIA = value;

        ImageBot.sprite = Robot;
        ImageHuman.sprite = value ? Robot : Human;

        if (value)
        {
            localGameToggle.SetIsOnWithoutNotify(false);
            currentMatch.localGame = false;
        }
    }

    public void SelectSquad(string rootPath, string folderName, string jsonFile)
    {
        managerPieceInfo.pieceSprites.Clear();

        if (OnWhite)
        {
            currentWhiteRootPath = rootPath;

            WhiteSquad.Clear();

            currentMatch.WhiteSquadName = folderName;

            CreatePiecesVisualization(jsonFile, WhitePiecesGrid);

            whiteSquadTMP.text = $"{folderName}\n{WhiteSquad.Data.Power}";
            whiteSquadTMP2.text = folderName;

            string squadFolder = Path.Combine(currentWhiteRootPath, fileManager.basePath_SquadData, currentMatch.WhiteSquadName);
            //string squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.WhiteSquadName);
            string jsonFileWhite = Path.Combine(squadFolder, currentMatch.WhiteSquadName + ".json");

            if (File.Exists(jsonFileWhite))
                CreatePiecesVisualization(jsonFileWhite, WhitePiecesGrid);
        }
        else
        {
            currentBlackRootPath = rootPath;
            BlackSquad.Clear();

            currentMatch.BlackSquadName = folderName;

            CreatePiecesVisualization(jsonFile, BlackPiecesGrid);

            blackSquadTMP.text = $"{folderName}\n{BlackSquad.Data.Power}";
            blackSquadTMP2.text = folderName;

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

            if (elementCount < 16)
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
                    managerPieceInfo.SelectPiece(piece.NameInSquad, piece, wrapper, sprite);
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
            }
            else
            {
                if (!BlackSquad.Sprites.ContainsKey(piece.NameInSquad))
                    BlackSquad.Sprites[piece.NameInSquad] = sprite;

                if (!BlackSquad.Pieces.ContainsKey(piece.NameInSquad))
                    BlackSquad.Pieces[piece.NameInSquad] = wrapper;
            }

            elementCount += 1;


            if (!managerPieceInfo.pieceSprites.ContainsKey(piece.NameInSquad + piece.Squad))
            {
                managerPieceInfo.pieceSprites[$"{piece.NameInSquad}{piece.Squad}"] = sprite;
            }

        }



        // --- 🔹 Ao final, guarda o Squad completo ---
        if (OnWhite)
            WhiteSquad.Data = data;
        else
            BlackSquad.Data = data;


        posInGrid.Clear();


        if (BlackSquad.Data != null)
            LoadPiecesInGrid(BlackSquad.Data, BlackSquad.Sprites, true);


        if (WhiteSquad.Data != null)
            LoadPiecesInGrid(WhiteSquad.Data, WhiteSquad.Sprites);


        gridLobby.ClearGrid(posInGrid);

    }


















    public void LoadMatchConfig()
    {
        SingleMatchConfig currentMatch = SingleLobbyManager.GetMatchConfig();

        if (currentMatch == null)
            return;

        if (!currentMatch.options)
        {
            optionsPanel.SetActive(false);
            OpenOpt.gameObject.SetActive(true);

        }


        noRulesToggle.isOn = currentMatch.noRules;

        localGameToggle.isOn = currentMatch.localGame;

        noTurnsToggle.isOn = currentMatch.noTurns;

        AutoSwitchSideToggle.isOn = currentMatch.switchSide;

        IAvsIAToggle.isOn = currentMatch.IAvsIA;

        string currentRootPath = Application.persistentDataPath;


        string squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.BlackSquadName);
        string jsonFile = Path.Combine(squadFolder, currentMatch.BlackSquadName + ".json");

        if (!File.Exists(jsonFile))
        {
            currentRootPath = Application.streamingAssetsPath;
            squadFolder = Path.Combine(Application.streamingAssetsPath, fileManager.basePath_SquadData, currentMatch.BlackSquadName);
            jsonFile = Path.Combine(squadFolder, currentMatch.BlackSquadName + ".json");
        }
        //string pngFile = Path.Combine(squadFolder, currentMatch.UserSquadName + ".png");

        //Sprite sprite = UIHelperUtils.GetSpriteFromPathForLobby(pngFile);

        if (File.Exists(jsonFile))
            SelectSquad(currentRootPath, currentMatch.BlackSquadName, jsonFile);


        currentRootPath = Application.persistentDataPath;

        OnWhite = true;
        squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.WhiteSquadName);
        jsonFile = Path.Combine(squadFolder, currentMatch.WhiteSquadName + ".json");
        //pngFile = Path.Combine(squadFolder, currentMatch.BotSquadName + ".png");

        if (!File.Exists(jsonFile))
        {
            currentRootPath = Application.streamingAssetsPath;
            squadFolder = Path.Combine(Application.streamingAssetsPath, fileManager.basePath_SquadData, currentMatch.WhiteSquadName);
            jsonFile = Path.Combine(squadFolder, currentMatch.WhiteSquadName + ".json");
        }
        //sprite = UIHelperUtils.GetSpriteFromPathForLobby(pngFile);

        if (File.Exists(jsonFile))
            SelectSquad(currentRootPath, currentMatch.WhiteSquadName, jsonFile);

        foreach (Toggle toggle in difficulty_toggles)
        {
            if (System.Enum.TryParse(toggle.name.Replace(" (Toggle)", ""), true, out BotDifficulty diff))
            {
                // ativa o toggle se ele for o mesmo que o enum atual
                toggle.isOn = (currentMatch.BotDifficulty == diff);
            }
        }


        foreach (Toggle toggle in startOption_toggles)
        {
            if (System.Enum.TryParse(toggle.name.Replace(" (Toggle)", ""), true, out StartOption diff))
            {
                // ativa o toggle se ele for o mesmo que o enum atual
                toggle.isOn = (currentMatch.StartOption == diff);
            }
        }


        //map.sprite = Resources.Load<Sprite>("Sprites/Default/Map_Default");

    }






























    public List<Vector2Int> posInGrid = new List<Vector2Int>();




    public void LoadPiecesInGrid(Squad squadData, Dictionary<string, Sprite> pieceSprites, bool IsBlack = false)
    {

        foreach (var piece in squadData.Units)
        {
            Vector2Int finalPosition = piece.Position;

            if (IsBlack)
            {
                finalPosition = MirrorPosition(piece.Position);
            }

            posInGrid.Add(finalPosition);

            GameObject cell = gridLobby.GetCellAtPosition(finalPosition);

            SetPieceToCellFromJson(cell, piece, pieceSprites);
        }

    }

    private Vector2Int MirrorPosition(Vector2Int original)
    {
        int boardSize = 8; // padrão do xadrez
        return new Vector2Int( //boardSize - 1 - 
            original.x,
            boardSize - 1 - original.y
        );
    }

    public void SetPieceToCellFromJson(GameObject cell, UnitPieceData piece, Dictionary<string, Sprite> pieceSprites)
    {
        // coloca o sprite na célula
        if (!pieceSprites.ContainsKey(piece.Name))
        {
            return;
        }

        SetSpriteFromJson(cell, piece, pieceSprites);
    }


    public void SetSpriteFromJson(GameObject cell, UnitPieceData piece, Dictionary<string, Sprite> pieceSprites)
    {
        // procura se já existe um filho chamado "Piece"
        Transform pieceTransform = cell.transform.Find("Piece");
        Image pieceImage;

        if (pieceTransform == null)
        {
            // cria um novo GameObject dentro da célula
            GameObject pieceGO = new GameObject("Piece", typeof(RectTransform), typeof(Image));

            // define como filho da célula
            pieceGO.transform.SetParent(cell.transform, false);

            float margin = 0f; // margem em pixels

            // ajusta o RectTransform para ocupar toda a célula
            RectTransform rt = pieceGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);       // distância da borda inferior/esquerda
            rt.offsetMax = new Vector2(-margin, -margin);     // distância da borda superior/direita

            // pega o componente Image recém-criado
            pieceImage = pieceGO.GetComponent<Image>();
        }
        else
        {
            // se já existe, só pega o Image
            pieceImage = pieceTransform.GetComponent<Image>();
        }
        if (pieceSprites.ContainsKey(piece.Name))
            pieceImage.sprite = pieceSprites[piece.Name];


        pieceImage.preserveAspect = true;
    }
















}
