using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;
using System.Collections;
using Ookii.Dialogs;
using UnityEngine.SceneManagement;


public class NavigationManage_Create : MonoBehaviour
{
    public FileManager fileManager;
    public MovementCreation movementCreation;
    public FileNavigation fileNavigation;
    public FolderNavigation folderNavigation;
    public GridViewManager gridViewManager;
    public UIHelperUtils uIHelperUtils;
    private GameObject panelFolder;
    private GameObject panelFile;

    [Header("TMP_Text")]
    public TMP_Text namePiece;
    public TMP_Text nameArt;
    public TMP_Text squadPiece;

    [Header("Buttons:")]
    public Button saveBtn;
    public Button quickSaveBtw;
    public Button loadBtn;
    public Button newBtw;
    public Button changeSquadBtw;
    public Button uploadArtBtw;

    [Header("Add Pieces Swap and Promotion:")]
    public Button swapBtw;
    public Button promotionBtw;
    public Transform swapColunContent;
    public Transform promotionColunContent;
    public GameObject viewPiecePrefab;

    [Header("Control Actions:")]
    public bool onSwap = false;
    public bool onPromotion = false;
    public bool OnSquad = false;

    // Start is called before the first frame update
    void Start()
    {
        if (fileNavigation == null)
        {
            fileNavigation = FindObjectOfType<FileNavigation>();
        }

        if (gridViewManager == null)
        {
            gridViewManager = FindObjectOfType<GridViewManager>();
        }

        if (folderNavigation == null)
        {
            folderNavigation = FindObjectOfType<FolderNavigation>();
        }


        panelFolder = folderNavigation.panelFolders;
        panelFile = fileNavigation.panelFile;

        if (movementCreation == null)
        {
            movementCreation = FindObjectOfType<MovementCreation>();
        }
        if (uIHelperUtils == null)
        {
            uIHelperUtils = FindObjectOfType<UIHelperUtils>();
        }

        quickSaveBtw.onClick.AddListener(() =>
        {
            SavePiece(movementCreation.piece.Name, movementCreation.piece.Squad);
        });

        saveBtn.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(movementCreation.piece.Art))
            {
                uIHelperUtils.save = true;
                StartFolderNavigation();
            }
        });

        loadBtn.onClick.AddListener(() =>
        {
            uIHelperUtils.save = false;
            StartFolderNavigation();
        });


        newBtw.onClick.AddListener(() =>
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        });

        uploadArtBtw.onClick.AddListener(() =>
        {
            CreateSelectionArt();
        });


        swapBtw.onClick.AddListener(() =>
        {
            onSwap = true;
            onPromotion = false;

            if (squadPiece.text == "Squad") //|| !string.IsNullOrEmpty(nameText.text)
            {
                if (uIHelperUtils.change)
                {
                    folderNavigation.StartCreatingFolderButtons(fileManager.basePath_PieceData, panelFolder);
                }
                panelFolder.SetActive(true);
            }
            else
            {
                if (uIHelperUtils.change)
                {
                    fileNavigation.StartCreatingFileButtons(movementCreation.piece.Squad, folderNavigation.selectRootPath, fileManager.basePath_PieceData);
                }
                panelFile.SetActive(true);
            }
        });


        promotionBtw.onClick.AddListener(() =>
        {
            onPromotion = true;
            onSwap = false;

            if (squadPiece.text == "Squad") //|| !string.IsNullOrEmpty(nameText.text)
            {

                folderNavigation.StartCreatingFolderButtons(fileManager.basePath_PieceData, panelFolder);

                panelFolder.SetActive(true);
            }
            else
            {
                fileNavigation.StartCreatingFileButtons(movementCreation.piece.Squad, folderNavigation.selectRootPath, fileManager.basePath_PieceData);


                panelFile.SetActive(true);
            }
        });


        changeSquadBtw.onClick.AddListener(() =>
        {
            ResetAllControlBooleans();

            OnSquad = true;

            if (uIHelperUtils.change)
            {
                folderNavigation.StartCreatingFolderButtons(fileManager.basePath_PieceData, panelFolder);
            }
            panelFolder.SetActive(true);

        });




    }


    public void ResetAllControlBooleans()
    {
        onSwap = false;
        onPromotion = false;
        OnSquad = false;
    }

    public void StartFolderNavigation()
    {
        uIHelperUtils.setAll();
        panelFolder.SetActive(true);
        folderNavigation.StartCreatingFolderButtons(fileManager.basePath_PieceData, panelFolder);
    }

    public void CreateSelectionArt()
    {

        fileNavigation.navigationOptions.SetActive(true);
        fileNavigation.squadsBtw.gameObject.SetActive(false);

        panelFile.SetActive(true);

        fileNavigation.selectBasePath = fileManager.basePath_Sprite;

        Transform content = panelFile.transform.Find("Scroll View/Viewport/Content");

        // Limpa botões antigos
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        uIHelperUtils.setAll();

        StartCoroutine(fileNavigation.UpdateFilesButtons());
    }

    public void OnClickFolder(string pasta, GameObject newButton, string rootPath)
    {
        if (uIHelperUtils.delete)
        {
            // Se for StreamingAssets, não pode excluir
            if (rootPath == Application.streamingAssetsPath)
            {
                //Debug.LogWarning("Não é permitido excluir pastas de StreamingAssets!");
                fileManager.CreateAdvice("Deleting StreamingAssets folders is not allowed!");
                uIHelperUtils.delete = false;
                return;
            }


            string pathPiece = Path.Combine(rootPath, fileManager.basePath_PieceData, pasta);
            string pathSquad = Path.Combine(rootPath, fileManager.basePath_SquadData, pasta);

            fileManager.HandleDeleteFolder(pasta, pathPiece, newButton);
            fileManager.HandleDeleteFolder(pasta, pathSquad, newButton);

            uIHelperUtils.delete = false;
        }
        else if (uIHelperUtils.save)
        {
            //if (string.IsNullOrEmpty(nameInput.text))
            //    return;

            // Se for StreamingAssets, não pode salvar
            if (rootPath == Application.streamingAssetsPath)
            {
                //Debug.LogWarning("Não é permitido salvar pastas de StreamingAssets!");
                fileManager.CreateAdvice("Saving StreamingAssets folders is not allowed!");
                //save = false;
                return;
            }

            if (pasta != movementCreation.piece.Squad)
            {

                if (movementCreation.piece.Squad != "")
                    ClearSelectPieces();

                movementCreation.piece.Squad = pasta;
                squadPiece.text = movementCreation.piece.Squad;

                movementCreation.special.Pieces.Clear();
                movementCreation.promotion.Pieces.Clear();

            }

            //string texto = nameInput.text;
            //SavePiece(texto, pasta);

            string name = null;
            if (!string.IsNullOrEmpty(namePiece.text) && namePiece.text != "Name")
            {
                name = namePiece.text;
            }
            else if (!string.IsNullOrEmpty(nameArt.text) && nameArt.text != "Name")
            {
                name = nameArt.text;
            }

            fileManager.CreateInput("Salvar Arquivo", "Digite o nome...", (text) =>
            {
                SavePiece(text, pasta, rootPath);
            }, name);



            uIHelperUtils.save = false;
            //StartCoroutine(folderNavigation.UpdateFolderButtons());
        }
        else
        {
            if (onSwap || onPromotion || OnSquad)
            {
                if (rootPath == Application.streamingAssetsPath)
                {
                    fileManager.CreateAdvice("Changing StreamingAssets folder squads is not allowed!");
                    folderNavigation.panelFolders.SetActive(false);
                    OnSquad = false;
                    return;
                }

                if (pasta != movementCreation.piece.Squad)
                {

                    if (movementCreation.piece.Squad != "")
                        ClearSelectPieces();

                    movementCreation.piece.Squad = pasta;
                    squadPiece.text = movementCreation.piece.Squad;

                    movementCreation.special.Pieces.Clear();
                    movementCreation.promotion.Pieces.Clear();

                    movementCreation.CalcularPoderTotal();

                }

                if (OnSquad)
                    OnSquad = false;
                if (onPromotion)
                    onPromotion = false;
                if (onSwap)
                    onSwap = false;

                folderNavigation.selectRootPath = rootPath;

            }
            else
            {
                uIHelperUtils.back = true;
                uIHelperUtils.change = false;
                fileNavigation.StartCreatingFileButtons(pasta, rootPath, fileManager.basePath_PieceData);
                panelFile.SetActive(true);
            }

            panelFolder.SetActive(false);
        }
    }




















    public void OnFileClick(GameObject buttonObj, string fileName, string jsonPath, string folder, Sprite sprite, string rootPath, PieceInfo piece = null)
    {
        if (piece == null)
        {
            string json = File.ReadAllText(jsonPath);
            PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

            if (wrapper == null || wrapper.piece == null)
            {
                Debug.LogWarning("JSON inválido: " + jsonPath);
                return;
            }

            piece = wrapper.piece;
        }

        if (uIHelperUtils.delete)
        {
            fileManager.HandleDeleteFile(fileName, jsonPath, buttonObj);
            uIHelperUtils.change = true;
            uIHelperUtils.delete = false;
        }
        else if (onSwap)
        {
            HandleSwapPiece(piece, fileName, sprite);
            panelFile.SetActive(false);
            onSwap = false;
        }
        else if (onPromotion)
        {
            HandlePromotionPiece(piece, fileName, sprite);
            panelFile.SetActive(false);
            onPromotion = false;
        }
        else
        {
            HandleSelectPiece(piece, fileName, folder, sprite, rootPath);
            panelFile.SetActive(false);
        }
    }

    private void HandleSelectPiece(PieceInfo piece, string fileName, string folder, Sprite sprite, string rootPath)
    {
        movementCreation.piece.Name = fileName;
        movementCreation.piece.Art = piece.Art;
        movementCreation.piece.FolderSprite = piece.FolderSprite;
        movementCreation.piece.Squad = folder;

        namePiece.text = movementCreation.piece.Name;
        nameArt.text = movementCreation.piece.Art;
        squadPiece.text = movementCreation.piece.Squad;

        string resourcePath = Path.Combine(rootPath, fileManager.basePath_PieceData, folder, fileName + ".json");
        StartCoroutine(movementCreation.LoadJson(resourcePath));

        movementCreation.resultPreview.sprite = sprite;

        gridViewManager.UpdateArtToGrid(sprite);

        folderNavigation.selectRootPath = rootPath;
    }


    public void HandleSelectionArt(string name, string folder, Sprite sprite)
    {

        movementCreation.piece.Art = name;
        nameArt.text = name;
        movementCreation.piece.FolderSprite = folder;

        movementCreation.resultPreview.sprite = sprite;

        gridViewManager.UpdateArtToGrid(sprite);

        fileNavigation.deleteObj.SetActive(true);
        panelFile.SetActive(false);
    }









    private void HandleSwapPiece(PieceInfo piece, string fileName, Sprite sprite)
    {
        if (fileName == movementCreation.piece.Name)
        {
            fileManager.CreateAdvice("Adding the same selected Piece is not allowed.");
            return;
        }

        if (!movementCreation.AddPiece(fileName, movementCreation.special.Pieces))
            return;

        GameObject clone = Instantiate(viewPiecePrefab, swapColunContent);
        clone.name = "Preview_" + piece.Art;

        Image img = clone.GetComponentInChildren<Image>();
        if (img != null) img.sprite = sprite;

        Button btn = clone.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (movementCreation.RemovePiece(fileName, movementCreation.special.Pieces))
                {
                    Destroy(clone);
                    movementCreation.CalcularPoderTotal();
                }
            });
        }

        movementCreation.CalcularPoderTotal();
    }

    private void HandlePromotionPiece(PieceInfo piece, string fileName, Sprite sprite)
    {
        if (fileName == movementCreation.piece.Name)
        {
            fileManager.CreateAdvice("Adding the same selected Piece is not allowed.");
            return;
        }

        if (!movementCreation.AddPiece(fileName, movementCreation.promotion.Pieces))
            return;

        GameObject clone = Instantiate(viewPiecePrefab, promotionColunContent);
        clone.name = "Preview_" + piece.Art;

        Image img = clone.GetComponentInChildren<Image>();
        if (img != null) img.sprite = sprite;

        Button btn = clone.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (movementCreation.RemovePiece(fileName, movementCreation.promotion.Pieces))
                {
                    Destroy(clone);
                    movementCreation.CalcularPoderTotal();
                }
            });
        }

        movementCreation.CalcularPoderTotal();
    }






























    [System.Serializable]
    public class PendingRemove
    {
        public string fileName;
        public List<string> pieces;

        public PendingRemove(string fileName, List<string> pieces)
        {
            this.fileName = fileName;
            this.pieces = pieces;
        }
    }

    // Lista de pendentes
    private List<PendingRemove> pendingRemoves = new List<PendingRemove>();

    public void ProcessPendingRemoves()
    {

        if (pendingRemoves.Count > 0)
            fileManager.CreateAdvice("Pieces from other squads in Promotion and Castling have been removed.");

        foreach (var item in pendingRemoves)
        {
            movementCreation.RemovePiece(item.fileName, item.pieces);
        }

        movementCreation.CalcularPoderTotal();
        // se quiser limpar a lista depois
        pendingRemoves.Clear();
    }


    public void ClearSelectPieces()
    {
        foreach (Transform child in promotionColunContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in swapColunContent)
        {
            Destroy(child.gameObject);
        }

        //fileManager.CreateAdvice("Pieces from other squads in Promotion and Castling have been removed.");
    }

    public IEnumerator LoadPromotionsPiecesImage(string fileName, bool swap = false)
    {
        yield return null;

        Transform content = promotionColunContent;

        if (swap)
            content = swapColunContent;

        // Caminho do JSON específico
        string jsonPath = Path.Combine(folderNavigation.selectRootPath, fileManager.basePath_PieceData, movementCreation.piece.Squad, fileName + ".json");

        if (!File.Exists(jsonPath))
        {
            Debug.LogWarning("Arquivo JSON não encontrado: " + jsonPath);

            if (swap)
                pendingRemoves.Add(new PendingRemove(fileName, movementCreation.special.Pieces));
            else
                pendingRemoves.Add(new PendingRemove(fileName, movementCreation.promotion.Pieces));

            yield break;
        }

        // Lê o JSON
        string json = File.ReadAllText(jsonPath);
        PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

        if (wrapper == null || wrapper.piece == null)
        {
            Debug.LogWarning("JSON inválido: " + jsonPath);
            yield break;
        }

        PieceInfo piece = wrapper.piece;

        // Instancia o prefab do painel
        GameObject clone = Instantiate(viewPiecePrefab, content);

        // Define o nome do objeto (opcional)
        clone.name = "Preview_" + fileName;

        // Acha a imagem dentro do painel
        Image img = clone.GetComponentInChildren<Image>();

        Texture2D tex = fileManager.LoadTextureFromFile(piece.FolderSprite, piece.Art, fileManager.basePath_Sprite, folderNavigation.selectRootPath);
        Sprite sprite = fileManager.ConvertTextureToSprite(tex);

        if (img != null)
        {
            img.sprite = sprite;
        }

        // Acha o botão dentro do painel
        Button btn = clone.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners(); // limpa listeners antigos, se houver
            btn.onClick.AddListener(() =>
            {
                if (swap)
                {
                    if (movementCreation.RemovePiece(fileName, movementCreation.special.Pieces))
                    {
                        Destroy(clone);
                        movementCreation.CalcularPoderTotal();
                    }
                }
                else
                {
                    if (movementCreation.RemovePiece(fileName, movementCreation.promotion.Pieces))
                    {
                        Destroy(clone);
                        movementCreation.CalcularPoderTotal();
                    }
                }


            });
        }

        // Se quiser simular um carregamento assíncrono, pode colocar um yield
        yield return null;
    }































    public void SavePiece(string fileName, string subfolderName, string rootPath = null)
    {

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileManager.CreateAdvice("Nenhum arquivo selecionado");
            Debug.LogError("SavePiece: Nome do arquivo não pode ser vazio.");
            return;
        }

        if (string.IsNullOrWhiteSpace(subfolderName))
        {
            Debug.LogError("SavePiece: Subpasta não pode ser vazia.");
            return;
        }

        // Nome do JSON sempre normalizado
        string fileJson = fileName.Trim() + ".json";

        // salvando de StreamingAssets
        if (folderNavigation.selectRootPath == Application.streamingAssetsPath)
        {

            movementCreation.piece.NativeSprite = true;

            /*
            try
            {
                // Carrega dados originais do JSON
                string jsonData = fileManager.LoadJson(
                    folderNavigation.selectRootPath,
                    fileManager.basePathPaintingData,
                    subfolderName,
                    fileName
                );

                // Caminho completo para o sprite PNG
                string spritePath = Path.Combine(
                    folderNavigation.selectRootPath,
                    fileManager.basePathPng,
                    movementCreation.piece.FolderSprite,
                    movementCreation.piece.Art
                );

                if (!File.Exists(spritePath))
                {
                    Debug.LogError($"SavePiece: Arquivo de sprite não encontrado em {spritePath}");
                    return;
                }

                // Carrega textura
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(spritePath)))
                {
                    Debug.LogError($"SavePiece: Falha ao carregar sprite em Texture2D ({spritePath})");
                    return;
                }

                // Nome do JSON para peça (garante extensão correta)
                string jsonFileName = Path.ChangeExtension(movementCreation.piece.Art, ".json");

                // Salva cópias em PaintingData e PNG
                fileManager.SaveJson(movementCreation.piece.FolderSprite, jsonFileName, jsonData, fileManager.basePathPaintingData);
                fileManager.SavePng(movementCreation.piece.FolderSprite, movementCreation.piece.Art, tex, fileManager.basePathPng);

                Debug.Log($"SavePiece: Sprite + JSON salvos para peça '{movementCreation.piece.Art}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SavePiece: Erro ao salvar assets da peça. Detalhes: {ex.Message}");
                return;
            }
            */
        }

        // Caso o arquivo já exista
        if (fileManager.FileExists(subfolderName, fileJson, fileManager.basePath_PieceData))
        {
            string title = "Do you want to replace the file?";
            string text = "There is already a file with the same name in the folder. Do you want to replace it?";

            fileManager.CreateWarning(title, text, () =>
            {
                SaveSquadPiece(fileName, subfolderName, fileJson);

                if (!string.IsNullOrEmpty(rootPath))
                {
                    folderNavigation.selectRootPath = rootPath;
                }

            });

            return;
        }

        SaveSquadPiece(fileName, subfolderName, fileJson);

        if (!string.IsNullOrEmpty(rootPath))
        {
            folderNavigation.selectRootPath = rootPath;
        }

    }

    private void SaveSquadPiece(string fileName, string subfolderName, string fileJson)
    {

        //if (folderNavigation.selectRootPath == Application.streamingAssetsPath)
        uIHelperUtils.change = true;

        movementCreation.piece.Name = fileName;
        movementCreation.piece.Squad = subfolderName;

        namePiece.text = movementCreation.piece.Name;
        squadPiece.text = movementCreation.piece.Squad;

        string json = movementCreation.CreateJson();
        fileManager.SaveJson(subfolderName, fileJson, json, fileManager.basePath_PieceData);

        StartCoroutine(folderNavigation.UpdateFolderButtons());
        panelFolder.SetActive(false);

        Debug.Log($"SavePiece: Peça '{fileName}' salva com sucesso no SquadData.");
    }





}

