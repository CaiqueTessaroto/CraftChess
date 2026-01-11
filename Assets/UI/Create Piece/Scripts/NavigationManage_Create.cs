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
    public TMP_InputField namePiece;
    //public TMP_Text namePiece;
    public TMP_Text nameArt;
    public TMP_Text squadPiece;

    [Header("Buttons:")]
    public Button saveBtn;
    public Button quickSaveBtw;
    public Button loadBtn;
    public Button newBtw;
    public Button changeSquadBtw;
    public Button uploadArtBtw;

    [Header("Control Actions:")]
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
            if (string.IsNullOrEmpty(namePiece.text))
            {
                fileManager.CreateAdvice("Precisa ter um nome para salvar.");
                return;
            }

            if (!string.IsNullOrEmpty(movementCreation.piece.Art))
            {
                uIHelperUtils.save = true;
                StartFolderNavigation();
            }
            else
            {
                fileManager.CreateAdvice("Precisa ter uma arte para salvar.");
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

                movementCreation.piece.Squad = pasta;
                squadPiece.text = movementCreation.piece.Squad;

            }

            //string texto = nameInput.text;
            //SavePiece(texto, pasta);
            /*
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
            */
            SavePiece(namePiece.text, pasta, rootPath);



            uIHelperUtils.save = false;
            //StartCoroutine(folderNavigation.UpdateFolderButtons());
        }
        else
        {
            if (OnSquad)
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

                    movementCreation.piece.Squad = pasta;
                    squadPiece.text = movementCreation.piece.Squad;

                    movementCreation.CalcularPoderTotal();

                }

                if (OnSquad)
                    OnSquad = false;

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

        if (rootPath == Application.streamingAssetsPath)
            movementCreation.piece.NativeSprite = true;
        else
            movementCreation.piece.NativeSprite = false;


        namePiece.text = movementCreation.piece.Name;
        nameArt.text = movementCreation.piece.Art;
        squadPiece.text = movementCreation.piece.Squad;


        string resourcePath = Path.Combine(rootPath, fileManager.basePath_PieceData, folder, fileName + ".json");
        StartCoroutine(movementCreation.LoadJson(resourcePath));

        movementCreation.resultPreview.sprite = sprite;

        if (!movementCreation.resultPreview.gameObject.activeSelf)
            movementCreation.resultPreview.gameObject.SetActive(true);

        gridViewManager.UpdateArtToGrid(sprite);

        folderNavigation.selectRootPath = rootPath;
    }


    public void HandleSelectionArt(string name, string folder, Sprite sprite, string rootPath)
    {

        movementCreation.piece.Art = name;
        nameArt.text = name;
        movementCreation.piece.FolderSprite = folder;

        if (string.IsNullOrEmpty(namePiece.text))
        {
            namePiece.text = nameArt.text;
        }

        if (rootPath == Application.streamingAssetsPath)
            movementCreation.piece.NativeSprite = true;
        else
            movementCreation.piece.NativeSprite = false;

        movementCreation.resultPreview.sprite = sprite;

        if (!movementCreation.resultPreview.gameObject.activeSelf)
            movementCreation.resultPreview.gameObject.SetActive(true);

        gridViewManager.UpdateArtToGrid(sprite);

        fileNavigation.deleteObj.SetActive(true);
        panelFile.SetActive(false);
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

        //StartCoroutine(folderNavigation.UpdateFolderButtons());
        panelFolder.SetActive(false);

        folderNavigation.RefreshFolderButton(subfolderName);

        Debug.Log($"SavePiece: Peça '{fileName}' salva com sucesso no SquadData.");
    }





}

