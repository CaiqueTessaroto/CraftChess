using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;
using System.Collections;
using System.Linq;


[System.Serializable]
public class SquadDataWrapper
{
    public King King;
    public List<SquadPieceData> Pieces = new List<SquadPieceData>();
}

public class NavigationManage_Squad : MonoBehaviour
{
    public FileManager fileManager;
    public SquadManager squadManager;
    public FileNavigation fileNavigation;
    public FolderNavigation folderNavigation;
    public UIHelperUtils uIHelperUtils;
    private GameObject panelFolder;
    private GameObject panelFile;

    [Header("Options")]
    public Button allBtw;
    public Button myBtw;
    public Button libraryBtw;

    [Header("Buttons:")]
    public Button saveBtn;
    public Button quickSaveBtn;
    public Button loadBtn;
    public Button AddBtn;
    public Button RemoveBtn;

    [Header("Prefabs")]
    public GameObject piece_BtnPrefab;
    public GameObject squad_BtnPrefab;

    [Header("Panel Squads")]
    public GameObject panelSquad;
    public Button backBtw;
    public Button deleteBtw;

    [Header("Panels")]
    public Transform piecesPanel;

    [Header("Control")]
    public bool initiate = false;
    private bool setCursor = false;
    //private string selectRootPath;



    void Start()
    {

        if (fileNavigation == null)
        {
            fileNavigation = FindObjectOfType<FileNavigation>();
        }

        if (folderNavigation == null)
        {
            folderNavigation = FindObjectOfType<FolderNavigation>();
        }

        panelFolder = folderNavigation.panelFolders;
        panelFile = fileNavigation.panelFile;

        if (fileManager == null)
        {
            fileManager = FindObjectOfType<FileManager>();
        }

        if (squadManager == null)
        {
            squadManager = FindObjectOfType<SquadManager>();
        }


        allBtw.onClick.AddListener(() =>
{
    if (initiate) return;
    if (uIHelperUtils.setAll())
        StartFormationsButtons();

});
        myBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setMy())
                StartFormationsButtons();

        });
        libraryBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setLibrary())
                StartFormationsButtons();

        });


        uIHelperUtils.setAll();

        backBtw.onClick.AddListener(() =>
        {
            uIHelperUtils.ResetAllControlBooleans();

            panelSquad.SetActive(false);
        });

        AddBtn.onClick.AddListener(() =>
        {
            fileNavigation.selectBasePath = fileManager.basePath_PieceData;

            uIHelperUtils.save = false;

            if (uIHelperUtils.OnFiles)
            {
                fileNavigation.panelFile.SetActive(true);
                if (!fileNavigation.initiate)
                {
                    uIHelperUtils.setAll();
                    fileNavigation.UpdateFilesButtons();
                }
            }
            else
            {
                folderNavigation.panelFolders.SetActive(true);
                if (!folderNavigation.initiate)
                    folderNavigation.StartCreatingFolderButtons(fileManager.basePath_PieceData, folderNavigation.panelFolders);
            }



        });

        deleteBtw.onClick.AddListener(() =>
        {
            uIHelperUtils.delete = !uIHelperUtils.delete;
            setCursor = true;

            UIHelperUtils.SetCursor(uIHelperUtils.TrashIcon, CursorHotspot.Center);

        });


        RemoveBtn.onClick.AddListener(() =>
        {

            if (squadManager.currentPieceName != "")
            {
                squadManager.squadData.Pieces.RemoveAll(p => p.NameInSquad == squadManager.currentPieceName);
                squadManager.squadData.Units.RemoveAll(p => p.Name == squadManager.currentPieceName);
                squadManager.placedPieces.RemoveAll(p => p.Name == squadManager.currentPieceName);
                squadManager.pieceSprites.Remove(squadManager.currentPieceName);

                Transform buttonTransform = piecesPanel.Find(squadManager.currentPieceName);

                if (buttonTransform != null)
                {
                    Destroy(buttonTransform.gameObject);
                }

                squadManager.squadData.Units = new List<UnitPieceData>(squadManager.placedPieces);

                squadManager.LoadPiecesInGrid();
            }

        });


        loadBtn.onClick.AddListener(() =>
        {
            folderNavigation.selectBasePath = fileManager.basePath_PieceData;

            panelSquad.SetActive(true);

            StartFormationsButtons();

        });


        saveBtn.onClick.AddListener(() =>
        {
            folderNavigation.selectBasePath = fileManager.basePath_PieceData;

            if (squadManager.squadData.Power == 0)
            {
                string text = UIHelperUtils.T("file.empty_squad.txt");

                if (string.IsNullOrEmpty(text))
                    text = "The squad is empty.";

                fileManager.CreateAdvice(text);
                return;
            }

            uIHelperUtils.save = true;
            panelSquad.SetActive(true);

            StartFormationsButtons();

            string name = null;
            if (!string.IsNullOrEmpty(squadManager.squad))
            {
                name = squadManager.squad;
            }
            string title = UIHelperUtils.T("file.save");
            string inputText = UIHelperUtils.T("file.create.txt");

            if (string.IsNullOrEmpty(title))
                title = "Create Set";

            if (string.IsNullOrEmpty(inputText))
                inputText = "Enter the name...";

            fileManager.CreateInput(title, inputText, (text) =>
            {
                string titleSave = UIHelperUtils.T("file.replace.title");
                string textSave = UIHelperUtils.T("file.replace.txt");

                if (string.IsNullOrEmpty(titleSave))
                    titleSave = "Do you want to replace the file?";
                if (string.IsNullOrEmpty(textSave))
                    textSave = "There is already a file with the same name in the folder, do you want to replace it?";

                SaveSquad(text, titleSave, textSave);
            }, name);

            if (string.IsNullOrEmpty(squadManager.squadData.King?.Name))
            {
                string text = UIHelperUtils.T("file.no_king.txt");

                if (string.IsNullOrEmpty(text))
                    text = "There is no king.";

                fileManager.CreateAdvice(text);
                //return;
            }


        });


        quickSaveBtn.onClick.AddListener(() =>
        {
            folderNavigation.selectBasePath = fileManager.basePath_PieceData;

            if (squadManager.squadData.Power == 0)
            {
                string text = UIHelperUtils.T("file.empty_squad.txt");

                if (string.IsNullOrEmpty(text))
                    text = "The squad is empty.";

                fileManager.CreateAdvice(text);
                return;
            }

            string titleSave = UIHelperUtils.T("file.replace.title");
            string textSave = UIHelperUtils.T("file.replace.txt");

            if (string.IsNullOrEmpty(titleSave))
                titleSave = "Do you want to replace the file?";
            if (string.IsNullOrEmpty(textSave))
                textSave = "There is already a file with the same name in the folder, do you want to replace it?";

            SaveSquad(squadManager.squad, titleSave, textSave);


            if (string.IsNullOrEmpty(squadManager.squadData.King?.Name))
            {
                string text = UIHelperUtils.T("file.no_king.txt");

                if (string.IsNullOrEmpty(text))
                    text = "There is no king.";

                fileManager.CreateAdvice(text);
                //return;
            }

            //if (!squadManager.enabledMode)
            //    fileManager.CreateAdvice("The squad cannot be used in Strategic mode.");

        });







    }

    void Update()
    {
        if (!uIHelperUtils.delete && setCursor)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            setCursor = false;
        }

    }

    public void SaveSquad(string name, string title, string text)
    {

        string title2 = UIHelperUtils.T("mode.alert.title");
        string text2 = UIHelperUtils.T("mode.alert.txt");

        if (string.IsNullOrEmpty(title2))
            title2 = "Strategic mode";
        if (string.IsNullOrEmpty(text2))
            text2 = "The squad cannot be used in Strategic mode.";


        squadManager.squad = name;
        squadManager.squadnameTmp.text = name;
        folderNavigation.selectRootPath = Application.persistentDataPath;

        string fileName = name + ".json";

        string fullPath = Path.Combine(
            Application.persistentDataPath,
            fileManager.basePath_SquadData,
            name,
            fileName
        );

        // Se já existe, pede confirmação
        if (File.Exists(fullPath))
        {
            fileManager.CreateWarning(title, text, () =>
            {
                if (!squadManager.enabledMode)
                {
                    fileManager.CreateWarning(title2, text2, () =>
                    {
                        Save(name, fileName);
                        panelSquad.SetActive(false);
                    });

                    return;
                }

                Save(name, fileName);
                panelSquad.SetActive(false);
            });

            return;
        }

        if (!squadManager.enabledMode)
        {
            fileManager.CreateWarning(title2, text2, () =>
            {
                Save(name, fileName);
                panelSquad.SetActive(false);
            });

            return;
        }


        Save(name, fileName);
        panelSquad.SetActive(false);
    }

    private void Save(string name, string fileName)
    {
        if (string.IsNullOrEmpty(name) || name == "Squad")
            return;

        name = name.Trim(); // garante que não tenha espaços no início/fim

        // Atualiza dados do squad
        squadManager.squadData.Units = squadManager.placedPieces;
        squadManager.squadData.Name = name;

        // Converte para JSON formatado
        string json = JsonUtility.ToJson(squadManager.squadData, true);

        // Salva o JSON na pasta correta
        fileManager.SaveJson(
            name,       // subpasta dentro de basePath_SquadData
            fileName,   // nome do arquivo
            json,
            fileManager.basePath_SquadData
        );

        // Captura imagem do painel
        StartCoroutine(CapturePanel(name));
    }

    public IEnumerator CapturePanel(string name)
    {
        yield return null;
        yield return null;

        Texture2D screenTex = ScreenCapture.CaptureScreenshotAsTexture();

        // pega os limites do painel em coordenadas de tela
        Vector3[] corners = new Vector3[4];
        squadManager.gridPanel.GetWorldCorners(corners);

        int x = (int)corners[0].x;
        int y = (int)corners[0].y;
        int width = (int)(corners[2].x - corners[0].x);
        int height = (int)(corners[2].y - corners[0].y);

        Texture2D cropped = new Texture2D(width, height);
        cropped.SetPixels(screenTex.GetPixels(x, y, width, height));
        cropped.Apply();

        // caminho para salvar dentro da pasta do squad
        string folder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, name);

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);


        string filePath = Path.Combine(folder, name + ".png");
        File.WriteAllBytes(filePath, cropped.EncodeToPNG());

        //Debug.Log("Print do painel salvo em: " + filePath);

        Destroy(screenTex);
        yield break;
    }


    public void OnClickFile(string jsonPath, string rootPath)
    {

        if (squadManager.squadData.Pieces.Count < 16)
        {
            if (AddPieceToSquad(jsonPath, rootPath, piecesPanel))
            {
                squadManager.CheckStrategicModeRules();
                panelFile.SetActive(false);

            }
        }
        else
        {
            panelFile.SetActive(false);

            string text = UIHelperUtils.T("file.fulls_squad.txt");

            if (string.IsNullOrEmpty(text))
                text = "The squad is full.";

            fileManager.CreateAdvice(text);
        }



    }





    public void OnClickFolder(string pasta, GameObject newButton, string rootPath)
    {
        if (uIHelperUtils.delete)
        {
            // Se for StreamingAssets, não pode excluir
            if (rootPath == Application.streamingAssetsPath)
            {
                string text2 = UIHelperUtils.T("file.native.delete.txt");

                if (string.IsNullOrEmpty(text2))
                    text2 = "Deleting the native library is not allowed.";

                //Debug.LogWarning("Não é permitido excluir pastas de StreamingAssets!");
                fileManager.CreateAdvice(text2);
                uIHelperUtils.delete = false;
                return;
            }

            string pathPiece = Path.Combine(rootPath, fileManager.basePath_PieceData, pasta);
            string pathSquad = Path.Combine(rootPath, fileManager.basePath_SquadData, pasta);

            fileManager.HandleDeleteFolder(pasta, pathPiece, newButton);
            fileManager.HandleDeleteFolder(pasta, pathSquad, newButton);

            uIHelperUtils.delete = false;
        }
        else
        {
            squadManager.squadData.Units = new List<UnitPieceData>(squadManager.placedPieces);

            squadManager.placedPieces.Clear();
            squadManager.pieceSprites.Clear();
            squadManager.squadData.Pieces.Clear();
            squadManager.squadData.Units.Clear();

            if (squadManager.squad != pasta)
            {
                RenamePieces(pasta);

                squadManager.squad = pasta;
                squadManager.squadnameTmp.text = pasta;

            }

            CreatePiecesButtons(rootPath, piecesPanel);

            squadManager.LoadPiecesInGrid();

            folderNavigation.selectRootPath = rootPath;

            panelFolder.SetActive(false);

        }
    }

    public void RenamePieces(string squadName)
    {
        // Cria uma lista temporária para armazenar as peças que serão removidas
        List<UnitPieceData> piecesToRemove = new List<UnitPieceData>();

        bool DoTo = false;

        foreach (var piece in squadManager.squadData.Units)
        {
            if (piece.Name.Contains(squadName))
            {
                piece.Name = piece.Name.Replace(squadName, "").Trim();
                DoTo = true;
            }
            else
            {
                piecesToRemove.Add(piece);
            }
        }

        // Remove as peças marcadas
        if (DoTo)
            foreach (var piece in piecesToRemove)
            {
                squadManager.squadData.Units.Remove(piece);

                if (piece.Position == squadManager.squadData.King.Position)
                {
                    squadManager.squadData.King.Name = "";
                    squadManager.squadData.King.Position = new Vector2Int();

                    squadManager.kingView.sprite = Resources.Load<Sprite>("Sprites/Default/Piece_Default");
                }
            }


        if (!squadManager.squadData.Units.Any(u => u.Name == squadManager.squadData.King.Name))
        {
            squadManager.squadData.King.Name = "";
            squadManager.squadData.King.Position = new Vector2Int();

            squadManager.kingView.sprite = Resources.Load<Sprite>("Sprites/Default/Piece_Default");
        }

    }
    public void StartFormationsButtons()
    {
        initiate = true;

        Transform content = panelSquad.transform.Find("Scroll View/Viewport/Content");

        if (content == null)
        {
            Debug.LogError("Não foi possível encontrar o Content do ScrollView!");
            return;
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        if (uIHelperUtils.onMy)
        {
            // Espera terminar a criação antes de continuar
            CreateFormationsButtons(Application.persistentDataPath, content);
        }

        if (uIHelperUtils.onLibrary)
        {
            CreateFormationsButtons(Application.streamingAssetsPath, content);
        }

        // Ajusta tamanho do ScrollView
        UIHelperUtils.SetSizeScrollView(panelSquad);

        initiate = false;

    }

    public void CreateFormationsButtons(string rootPath, Transform content = null)
    {
        if (content == null)
            content = panelSquad.transform.Find("Scroll View/Viewport/Content");

        if (content == null)
        {
            Debug.LogError("Não foi possível encontrar o Content do ScrollView!");
            return;
        }

        string squadsRoot = Path.Combine(rootPath, fileManager.basePath_SquadData);

        if (!Directory.Exists(squadsRoot))
        {
            Debug.LogWarning("Pasta não encontrada: " + squadsRoot);
            return;
        }

        // 🔹 Itera todas as pastas dentro de SquadData
        foreach (string squadFolder in Directory.GetDirectories(squadsRoot))
        {

            string folderName = Path.GetFileName(squadFolder);

            string pngFile = Path.Combine(squadFolder, folderName + ".png");
            string jsonFile = Path.Combine(squadFolder, folderName + ".json");

            if (!File.Exists(pngFile) && !File.Exists(jsonFile))
            {
                Debug.LogWarning("Faltando arquivos no squad: " + folderName);
                continue;
            }

            string squadName = Path.GetFileNameWithoutExtension(jsonFile);

            // Instancia o botão
            GameObject newButton = Instantiate(squad_BtnPrefab, content);

            // Nome no botão
            TMP_Text textComponent = newButton.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.text = squadName;

            // Imagem
            Image imageComponent = newButton.GetComponentInChildren<Image>();
            if (imageComponent != null)
            {
                byte[] bytes = File.ReadAllBytes(pngFile);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);

                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                              new Vector2(0.5f, 0.5f));
                imageComponent.sprite = sprite;
            }

            // Clique do botão
            Button buttonComponent = newButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() =>
                {
                    OnButtonClicked(folderName, newButton, piecesPanel, squadName, rootPath, jsonFile);
                });
            }
        }
    }


    public void OnButtonClicked(string folderName, GameObject newButton, Transform piecesPanel, string squadName, string rootPath, string jsonFile)
    {
        if (uIHelperUtils.delete)
        {

            if (rootPath == Application.streamingAssetsPath)
            {
                string text2 = UIHelperUtils.T("file.native.save.txt");

                if (string.IsNullOrEmpty(text2))
                    text2 = "Saving to the native library is not allowed.";

                fileManager.CreateAdvice(text2);
                uIHelperUtils.delete = false;
                return;
            }

            string squadFolder = Path.Combine(Application.persistentDataPath, fileManager.basePath_SquadData, folderName);

            string jsonPath = Path.Combine(squadFolder, folderName.TrimEnd() + ".json");
            string pngPath = Path.Combine(squadFolder, folderName + ".png");

            string title = UIHelperUtils.T("folder.delete.title");
            string text = UIHelperUtils.T("folder.delete.txt", folderName);

            if (string.IsNullOrEmpty(title))
                title = "Folder will be deleted";
            if (string.IsNullOrEmpty(text))
                text = "Are you sure you want to delete " + folderName + " and all its contents? This action is permanent and cannot be undone.";

            Debug.Log("Pasta: " + squadFolder);

            fileManager.CreateWarning(title, text, () =>
            {
                //fileManager.HandleDeleteFile(folderName, jsonPath, null);
                //fileManager.HandleDeleteFile(folderName, pngPath, null);
                fileManager.warning = false;
                fileManager.HandleDeleteFolder(folderName, squadFolder, null);

                Destroy(newButton);

            });

            fileManager.CleanUpEmptyFolder(fileManager.basePath_SquadData, folderName);

            uIHelperUtils.delete = false;
        }
        else
        {
            // Limpa dados antigos
            squadManager.placedPieces.Clear();
            squadManager.pieceSprites.Clear();
            squadManager.squadData.Pieces.Clear();

            squadManager.squad = folderName;
            squadManager.squadnameTmp.text = folderName;
            folderNavigation.selectRootPath = rootPath;

            // Remove botões antigos
            foreach (Transform child in piecesPanel)
                Destroy(child.gameObject);

            // Carrega o squad
            squadManager.LoadSquadData(squadName, rootPath);

            // 🔹 Carrega o JSON
            string jsonText = File.ReadAllText(jsonFile);
            SquadDataWrapper data = JsonUtility.FromJson<SquadDataWrapper>(jsonText);

            // 🔹 Cria botões das peças
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

                string json = File.ReadAllText(jsonPath);
                PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

                CreatePieceButton(wrapper.piece, piece.NameInSquad, jsonPath, loadRootPath, piecesPanel);
            }

            squadManager.LoadPiecesInGrid();
            panelSquad.SetActive(false);
        }
    }

    private void CreatePiecesButtons(string rootPath, Transform content)
    {
        if (content == null)
        {
            Debug.LogError("Não foi possível encontrar o Content do ScrollView!");
            return;
        }

        foreach (Transform child in content)
            Destroy(child.gameObject);

        string fullPath = Path.Combine(rootPath, fileManager.basePath_PieceData, squadManager.squad);
        string[] fileJson = Directory.GetFiles(fullPath, "*.json", SearchOption.TopDirectoryOnly);

        foreach (string jsonPath in fileJson)
        {
            if (squadManager.squadData.Pieces.Count < 16)
            {
                AddPieceToSquad(jsonPath, rootPath, content);
            }
            else
            {
                panelFile.SetActive(false);

                string text = UIHelperUtils.T("file.fulls_squad.txt");

                if (string.IsNullOrEmpty(text))
                    text = "The squad is full.";


                fileManager.CreateAdvice(text);
                return;
            }

        }
    }


    private bool AddPieceToSquad(string jsonPath, string rootPath, Transform content)
    {
        string nameInSquad = string.Empty;
        bool nativePiece = rootPath == Application.streamingAssetsPath;

        try
        {
            string json = File.ReadAllText(jsonPath);
            PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

            if (wrapper == null || wrapper.piece == null)
            {
                Debug.LogWarning("JSON inválido: " + jsonPath);
                return false;
            }

            PieceInfo piece = wrapper.piece;
            nameInSquad = piece.Name;
            piece.NativeSprite = nativePiece;

            // Adiciona ao squad
            if (!squadManager.squadData.Pieces.Any(p => p.NameInSquad == nameInSquad))
            {
                squadManager.squadData.Pieces.Add(new SquadPieceData
                {
                    NameInSquad = nameInSquad,
                    Name = piece.Name,
                    Squad = piece.Squad,
                    Power = piece.Power,
                    Sprite = piece.Art,
                    SpriteSet = piece.FolderSprite,
                    NativePiece = nativePiece
                });
            }
            else
            {
                if (piece.Squad == squadManager.squad)
                    return false;

                nameInSquad = piece.Squad + " " + piece.Name;

                if (!squadManager.squadData.Pieces.Any(p => p.NameInSquad == nameInSquad))
                {
                    squadManager.squadData.Pieces.Add(new SquadPieceData
                    {
                        NameInSquad = nameInSquad,
                        Name = piece.Name,
                        Squad = piece.Squad,
                        Power = piece.Power,
                        Sprite = piece.Art,
                        SpriteSet = piece.FolderSprite,
                        NativePiece = nativePiece
                    });
                }
                else
                    return false;
            }

            CreatePieceButton(piece, nameInSquad, jsonPath, rootPath, content);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao carregar JSON: " + jsonPath + " -> " + e.Message);
            return false;
        }
    }


    private void CreatePieceButton(PieceInfo piece, string nameInSquad, string jsonPath, string rootPath, Transform content)
    {
        // Caminho da sprite
        string caminhoSprite = "";

        if (piece.NativeSprite)
        {
            caminhoSprite = Path.Combine(Application.streamingAssetsPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art.Trim() + ".png");
        }
        else
        {
            caminhoSprite = Path.Combine(rootPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art.Trim() + ".png");
        }

        if (!File.Exists(caminhoSprite))
        {
            Debug.LogWarning("Sprite não encontrada: " + caminhoSprite);
        }

        // Instancia o botão
        GameObject newButton = Instantiate(piece_BtnPrefab, content);
        newButton.name = nameInSquad;


        // Define sprite
        Sprite sprite = UIHelperUtils.GetSpriteFromPath(caminhoSprite);

        Image imgComp = newButton.GetComponent<Image>();
        if (imgComp != null)
            imgComp.sprite = sprite;

        // Define texto
        TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
            textComp.text = string.IsNullOrEmpty(nameInSquad) ? piece.Art : nameInSquad;

        // Guarda sprite em cache
        if (!squadManager.pieceSprites.ContainsKey(nameInSquad))
        {
            squadManager.pieceSprites[nameInSquad] = sprite;
        }

        UIDragItem uIDragItem = newButton.AddComponent<UIDragItem>();

        uIDragItem.GetPiece(nameInSquad, File.ReadAllText(jsonPath), sprite, rootPath);

        SquadPieceData pieceData = squadManager.squadData.Pieces.Find(p => p.NameInSquad == nameInSquad);
        // Configura evento do botão
        newButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            squadManager.SelectPiece(nameInSquad, pieceData, File.ReadAllText(jsonPath), sprite, rootPath);
        });


    }



}
