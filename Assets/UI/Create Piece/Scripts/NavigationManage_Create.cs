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
    public TMP_Text nameArt;
    public TMP_Text squadPiece;
    public GameObject selectArtText;

    [Header("Buttons:")]
    public Button saveBtn;
    public Button quickSaveBtw;
    public Button loadBtn;
    public Button newBtw;
    public Button changeSquadBtw;
    public Button uploadArtBtw;

    [Header("Control Actions:")]
    public string fileName = "";
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
            QuickSavePiece(namePiece.text);
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

        this.fileName = movementCreation.piece.Name;

        selectArtText.SetActive(false);


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

        selectArtText.SetActive(false);

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

















    private void QuickSavePiece(string pieceName)
    {
        // ===============================
        // Validações básicas
        // ===============================
        if (string.IsNullOrEmpty(fileName))
        {
            fileManager.CreateAdvice("Nenhum arquivo selecionado.");
            return;
        }
        else if (string.IsNullOrEmpty(pieceName))
        {
            fileManager.CreateAdvice("Nome do arquivo não pode ser vazio.");
            return;
        }

        if (string.IsNullOrEmpty(squadPiece.text))
        {
            fileManager.CreateAdvice("Nenhuma pasta selecionada.");
            return;
        }

        if (folderNavigation.selectRootPath == Application.streamingAssetsPath)
        {
            fileManager.CreateAdvice("Não é permitido salvar pastas de StreamingAssets!");
            return;
        }

        string finalName = pieceName.Trim();
        string subfolderName = squadPiece.text.Trim();
        string fileJson = finalName + ".json";

        // ===============================
        // Função local de salvar
        // ===============================
        void Save()
        {
            SavePieceInternal(finalName, subfolderName, fileJson);
        }

        string fullPath = Path.Combine(
            folderNavigation.selectRootPath,
            fileManager.basePath_PieceData,
            subfolderName,
            fileName + ".json"
        );

        bool fileAlreadyExists = fileManager.FileExists(
            subfolderName,
            fileJson,
            fileManager.basePath_PieceData
        );

        bool isRenaming = fileName != finalName;

        // ===============================
        // Caso: rename
        // ===============================
        if (isRenaming)
        {
            if (fileAlreadyExists)
            {
                fileManager.CreateWarning(
                    "Quick Save",
                    $"Já existe um arquivo chamado: {finalName}. Deseja substituir?",
                    Save
                );
                return;
            }

            fileManager.HandleDeleteFile(fileName, fullPath, null);
            Save();
            return;
        }

        // ===============================
        // Caso: overwrite normal
        // ===============================
        if (fileAlreadyExists)
        {
            fileManager.CreateWarning(
                "Quick Save",
                "Do you want to overwrite the current file?",
                Save
            );
            return;
        }

        // ===============================
        // Caso: novo arquivo
        // ===============================
        Save();
    }




    public void SavePiece(
        string pieceName,
        string subfolderName,
        string rootPath = null
    )
    {
        if (string.IsNullOrEmpty(pieceName))
        {
            fileManager.CreateAdvice("Nome do arquivo não pode ser vazio.");
            return;
        }

        if (string.IsNullOrEmpty(subfolderName))
        {
            fileManager.CreateAdvice("Nenhuma pasta selecionada.");
            return;
        }

        string fileJson = pieceName.Trim() + ".json";

        if (folderNavigation.selectRootPath == Application.streamingAssetsPath)
        {
            movementCreation.piece.NativeSprite = true;
        }

        bool exists = fileManager.FileExists(
            subfolderName,
            fileJson,
            fileManager.basePath_PieceData
        );

        void Save()
        {
            SavePieceInternal(pieceName, subfolderName, fileJson);

            if (!string.IsNullOrEmpty(rootPath))
                folderNavigation.selectRootPath = rootPath;
        }

        if (exists)
        {
            fileManager.CreateWarning(
                "Do you want to replace the file?",
                "There is already a file with the same name in the folder. Do you want to replace it?",
                Save
            );
            return;
        }

        Save();
    }


    private void SavePieceInternal(
        string pieceName,
        string subfolderName,
        string fileJson
    )
    {
        uIHelperUtils.change = true;

        movementCreation.piece.Name = pieceName;
        movementCreation.piece.Squad = subfolderName;

        namePiece.text = pieceName;
        squadPiece.text = subfolderName;

        fileName = pieceName;

        string json = movementCreation.CreateJson();
        fileManager.SaveJson(subfolderName, fileJson, json, fileManager.basePath_PieceData);

        panelFolder.SetActive(false);
        folderNavigation.RefreshFolderButton(subfolderName);

        Debug.Log($"SavePiece: Peça '{pieceName}' salva com sucesso.");
    }






}

