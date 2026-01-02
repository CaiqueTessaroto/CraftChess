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
    //public MatchConfig currentMatch;

    [Header("Scripts")]
    public NavigationManage_SingleLobby navigationManage;

    [Header("Buttons")]
    public Button play;

    [Header("Map")]
    public Image map;

    [Header("ToggleGroup:")]
    public ToggleGroup difficultyOption;
    public ToggleGroup startOption;
    private Toggle[] difficulty_toggles;
    private Toggle[] startOption_toggles;

    [Header("Select Piece:")]
    public InfoGridView infoGridView;
    public string currentPieceName;
    public string squadPiece;
    public TMP_Text nameTmp;
    public TMP_Text powerTmp;
    public GameObject piecepanel;
    public Image previewImage;
    public GameObject promotion;
    public GameObject casteling;
    public Transform promotionContent;
    public Transform castelingContent;
    public GameObject viewPiecePrefab;
    public Button closePiecebtn;
    private Dictionary<string, Sprite> pieceSprites = new Dictionary<string, Sprite>();

    [Header("YourSquad")]
    public GameObject userSelect;
    public GameObject userView;
    public Button userSquadView;
    public Image userImage;
    public TMP_Text userSquadTMP;
    public TMP_Text userTMP;
    public GameObject userSelectTMP;
    public GameObject userBtn_Squad;
    private Button userBtn;
    public Transform userPiecesGrid;

    [Header("EnemySquad")]
    public GameObject enemySelect;
    public GameObject enemyView;
    public Button enemySquadView;
    public Image enemyImage;
    public TMP_Text enemySquadTMP;
    public TMP_Text enemyTMP;
    public GameObject enemySelectTMP;
    public GameObject enemyBtn_Squad;
    private Button enemyBtn;
    public Transform enemyPiecesGrid;

    [Header("Prefabs")]
    public GameObject piece_ImgPrefab;

    [Header("Match")]
    public SingleMatchConfig currentMatch;

    [Header("Control")]
    public bool OnEnemy = false;

    public List<MatchSquadData> Squads = new List<MatchSquadData>();

    private MatchSquadData Squad = new MatchSquadData();
    private MatchSquadData BotSquad = new MatchSquadData();


    //SquadDataWrapper
    //PieceWrapper
    void Start()
    {

        userTMP.text = "Player20";

        play.onClick.AddListener(() =>
        {

            if (string.IsNullOrEmpty(currentMatch.BotSquadName) || string.IsNullOrEmpty(currentMatch.UserSquadName))
                return;

            currentMatch.MapName = "Default";

            if (currentMatch.StartOption == StartOption.UserFirst)
            {
                Squad.Player = new Player("Jogador", 0, Color.white);
                BotSquad.Player = new Player("Bot", 1, Color.black);

                Squads.Add(Squad);
                Squads.Add(BotSquad);
            }
            else if (currentMatch.StartOption == StartOption.BotFirst)
            {
                BotSquad.Player = new Player("Bot", 0, Color.white);
                Squad.Player = new Player("Jogador", 1, Color.black);

                Squads.Add(BotSquad);
                Squads.Add(Squad);
            }
            else
            {
                bool userStarts = UnityEngine.Random.value > 0.5f;

                if (userStarts)
                {
                    Squad.Player = new Player("Jogador", 0, Color.white);
                    BotSquad.Player = new Player("Bot", 1, Color.black);

                    Squads.Add(Squad);
                    Squads.Add(BotSquad);
                }
                else
                {
                    Squad.Player = new Player("Jogador", 1, Color.black);
                    BotSquad.Player = new Player("Bot", 0, Color.white);

                    Squads.Add(BotSquad);
                    Squads.Add(Squad);
                }

                Debug.Log($"Começo aleatório → {(userStarts ? "Jogador começa" : "Bot começa")}");
            }

            managerLobby.SaveMatchConfig(currentMatch);
            managerLobby.StartMatch(currentMatch, Squads);
        });



        userBtn = userBtn_Squad.GetComponent<Button>();
        //userPiecesGrid = userBtn_Squad.transform;

        enemyBtn = enemyBtn_Squad.GetComponent<Button>();
        //enemyPiecesGrid = enemyBtn_Squad.transform;

        userBtn.onClick.AddListener(() =>
        {
            OnEnemy = false;

            navigationManage.StartFormationsButtons();

        });

        userSquadView.onClick.AddListener(() =>
        {
            OnEnemy = false;

            navigationManage.StartFormationsButtons();

        });

        enemyBtn.onClick.AddListener(() =>
        {
            OnEnemy = true;

            navigationManage.StartFormationsButtons();
        });


        enemySquadView.onClick.AddListener(() =>
        {
            OnEnemy = true;

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
                        //Debug.Log("StartOption selecionada: " + currentMatch.StartOption);
                    }
                    else
                    {
                        Debug.LogWarning("Nome do toggle não corresponde ao enum: " + cleanName);
                    }
                }
            });
        }


        closePiecebtn.onClick.AddListener(() =>
        {
            piecepanel.SetActive(false);
        });


        LoadMatchConfig();

    }


    public void SelectSquad(string folderName, string jsonFile, Sprite sprite)
    {

        pieceSprites.Clear();

        if (OnEnemy)
        {
            BotSquad.Clear();

            currentMatch.BotSquadName = folderName;
            enemySquadTMP.text = folderName;

            if (enemySelectTMP.activeSelf)
            {
                //enemySelectTMP.SetActive(false);
                enemySelect.SetActive(false);
                enemyView.SetActive(true);

                enemySquadView.GetComponent<Image>().sprite = sprite;
                //enemyBtn_Squad.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);
            }

            CreatePiecesVisualization(jsonFile, enemyPiecesGrid);

            string squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.UserSquadName);
            string jsonFileUser = Path.Combine(squadFolder, currentMatch.UserSquadName + ".json");

            OnEnemy = false;
            if (File.Exists(jsonFileUser))
                CreatePiecesVisualization(jsonFileUser, userPiecesGrid);

        }
        else
        {
            Squad.Clear();

            currentMatch.UserSquadName = folderName;
            userSquadTMP.text = folderName;


            if (userSelectTMP.activeSelf)
            {
                //userSelectTMP.SetActive(false);
                userSelect.SetActive(false);
                userView.SetActive(true);

                userSquadView.GetComponent<Image>().sprite = sprite;
                //userBtn_Squad.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);
            }

            CreatePiecesVisualization(jsonFile, userPiecesGrid);

            string squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.UserSquadName);
            string jsonFileEnemy = Path.Combine(squadFolder, currentMatch.UserSquadName + ".json");

            OnEnemy = true;
            if (File.Exists(jsonFileEnemy))
                CreatePiecesVisualization(jsonFileEnemy, enemyPiecesGrid);
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

            string loadRootPath = piece.NativePiece ? Application.streamingAssetsPath : Application.persistentDataPath;

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
            string caminhoSprite = Path.Combine(
                loadRootPath,
                fileManager.basePath_Sprite,
                wrapper.piece.FolderSprite,
                wrapper.piece.Art.Trim() + ".png"
            );

            if (!File.Exists(caminhoSprite))
            {
                Debug.LogWarning("Sprite não encontrada: " + caminhoSprite);
            }

            Sprite sprite = UIHelperUtils.GetSpriteFromPath(caminhoSprite);

            if (elementCount <= 16)
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
                    SelectPiece(piece.NameInSquad, piece, json, sprite);
                    //    squadManager.SelectPiece(nameInSquad, pieceData, File.ReadAllText(jsonPath), sprite, rootPath);
                });

            }

            // --- 🔹 Guarda dados em cache conforme o lado (usuário ou inimigo) ---
            if (OnEnemy)
            {
                // Inimigo
                if (!BotSquad.Sprites.ContainsKey(piece.NameInSquad))
                    BotSquad.Sprites[piece.NameInSquad] = sprite;

                if (!BotSquad.Pieces.ContainsKey(piece.NameInSquad))
                    BotSquad.Pieces[piece.NameInSquad] = wrapper;
            }
            else
            {
                // Jogador
                if (!Squad.Sprites.ContainsKey(piece.NameInSquad))
                    Squad.Sprites[piece.NameInSquad] = sprite;

                if (!Squad.Pieces.ContainsKey(piece.NameInSquad))
                    Squad.Pieces[piece.NameInSquad] = wrapper;
            }

            elementCount += 1;


            if (!pieceSprites.ContainsKey(piece.NameInSquad))
            {
                pieceSprites[piece.NameInSquad] = sprite;
            }

        }

        // --- 🔹 Ao final, guarda o Squad completo ---
        if (OnEnemy)
            BotSquad.Data = data;
        else
            Squad.Data = data;
    }



    public void SelectPiece(string namePieceSquad, SquadPieceData pieceData, string json, Sprite sprite)
    {
        MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        if (config.piece.Name != currentPieceName || config.piece.Squad != squadPiece)
        {
            SetInfoPiece(namePieceSquad, pieceData, config, sprite);
            StartCoroutine(SetPromotionsAndCastelingPieces(pieceData, json, sprite));
        }

    }




    public void SetInfoPiece(string namePieceSquad, SquadPieceData pieceData, MovementConfigData config, Sprite sprite)
    {
        //MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

        PieceInfo piece = config.piece;

        piecepanel.SetActive(true);

        currentPieceName = namePieceSquad;
        squadPiece = piece.Squad;

        //spritePiece = sprite;
        previewImage.sprite = sprite;

        nameTmp.text = namePieceSquad;
        powerTmp.text = $"Power: {pieceData.Power}";

    }

    public IEnumerator SetPromotionsAndCastelingPieces(SquadPieceData pieceData, string json, Sprite sprite)
    {

        MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(json);

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













    public void LoadMatchConfig()
    {
        SingleMatchConfig currentMatch = SingleLobbyManager.GetMatchConfig();

        if (currentMatch == null)
            return;

        string squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.UserSquadName);
        string jsonFile = Path.Combine(squadFolder, currentMatch.UserSquadName + ".json");
        string pngFile = Path.Combine(squadFolder, currentMatch.UserSquadName + ".png");

        Sprite sprite = UIHelperUtils.GetSpriteFromPathForLobby(pngFile);

        if (File.Exists(jsonFile))
            SelectSquad(currentMatch.UserSquadName, jsonFile, sprite);

        OnEnemy = true;
        squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, currentMatch.BotSquadName);
        jsonFile = Path.Combine(squadFolder, currentMatch.BotSquadName + ".json");
        pngFile = Path.Combine(squadFolder, currentMatch.BotSquadName + ".png");

        sprite = UIHelperUtils.GetSpriteFromPathForLobby(pngFile);

        if (File.Exists(jsonFile))
            SelectSquad(currentMatch.BotSquadName, jsonFile, sprite);

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







}
