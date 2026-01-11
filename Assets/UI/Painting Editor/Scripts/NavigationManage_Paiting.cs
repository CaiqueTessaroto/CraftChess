using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;
public class NavigationManage_Painting : MonoBehaviour
{


    public FileManager fileManager;
    public FileNavigation fileNavigation;
    public FolderNavigation folderNavigation;
    public UIHelperUtils uIHelperUtils;
    public PaintingGridManager gridManager;
    private GameObject panelFolder;
    private GameObject panelFile;
    private string fileName = "";
    private string folderName = "";
    private string selectRootPath = "";


    [Header("TMP_Text")]
    public TMP_Text folderText;
    //public TMP_Text namePiece;
    public TMP_InputField namePiece;

    [Header("Buttons:")]
    public Button saveBtn;
    public Button loadBtn;
    public Button QuickSave;

    void Start()
    {

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<PaintingGridManager>();
        }

        if (fileManager == null)
        {
            fileManager = FindObjectOfType<FileManager>();
        }
        if (fileNavigation == null)
        {
            fileNavigation = FindObjectOfType<FileNavigation>();
        }

        if (folderNavigation == null)
        {
            folderNavigation = FindObjectOfType<FolderNavigation>();
        }

        saveBtn.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(namePiece.text))
            {
                //    fileManager.CreateAdvice("Precisa ter um nome para salvar.");
                //    return;
            }

            Debug.Log(namePiece.text);

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            uIHelperUtils.save = true;
            folderNavigation.panelFolders.SetActive(true);

            folderNavigation.StartCreatingFolderButtons(fileManager.basePath_Sprite, folderNavigation.panelFolders);
        });


        loadBtn.onClick.AddListener(() =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            uIHelperUtils.save = false;
            folderNavigation.panelFolders.SetActive(true);

            folderNavigation.StartCreatingFolderButtons(fileManager.basePath_Sprite, folderNavigation.panelFolders);
        });

        QuickSave.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(folderName) && string.IsNullOrEmpty(fileName))
            {
                fileManager.CreateAdvice("Nenhuma arte selecionada.");
                return;
            }
            else if (string.IsNullOrEmpty(namePiece.text))
            {
                //    fileManager.CreateAdvice("Precisa ter um nome para salvar.");
                //    return;
            }

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            QuickSaveArt();
        });

        panelFolder = folderNavigation.panelFolders;
        panelFile = fileNavigation.panelFile;


    }






    public void OnClickFolder(string pasta, GameObject buttonObj, string rootPath)
    {

        if (uIHelperUtils.delete)
        {

            if (rootPath == Application.streamingAssetsPath)
            {
                //Debug.LogWarning("Não é permitido excluir pastas de StreamingAssets!");
                fileManager.CreateAdvice("Não é permitido excluir pastas de StreamingAssets!");
                return;
            }

            string caminhoPasta = Path.Combine(rootPath, fileManager.basePath_Sprite, pasta);
            string caminhoPastaJson = Path.Combine(rootPath, fileManager.basePath_PaintingData, pasta);

            fileManager.HandleDeleteFolders(pasta, caminhoPasta, caminhoPastaJson, buttonObj);

            uIHelperUtils.delete = false;
        }
        else if (uIHelperUtils.save)
        {
            if (rootPath == Application.streamingAssetsPath)
            {
                //Debug.LogWarning("Não é permitido salvar pastas de StreamingAssets!");
                fileManager.CreateAdvice("Não é permitido salvar pastas de StreamingAssets!");
                return;
            }


            //string name = null;
            if (string.IsNullOrEmpty(namePiece.text))
            {
                string name = null;

                fileManager.CreateInput("Salvar Arquivo", "Digite o nome...", (text) =>
                {
                    SaveArt(text, pasta);
                }, name);

            }
            else
                SaveArt(namePiece.text, pasta);



        }
        else
        {

            uIHelperUtils.back = true;
            uIHelperUtils.change = false;

            fileNavigation.StartCreatingFileButtons(pasta, rootPath, fileManager.basePath_Sprite);
            panelFolder.SetActive(false);
            panelFile.SetActive(true);
        }
    }


    public void OnFileClick(GameObject buttonObj, string fileName, string folder, string rootPath)
    {
        Debug.Log($"Arquivo clicado: {fileName}");

        string caminhoPng = Path.Combine(rootPath, fileManager.basePath_Sprite, folder, fileName.Trim() + ".png");
        string caminhoJson = Path.Combine(rootPath, fileManager.basePath_PaintingData, folder, fileName.Trim() + ".json");

        if (uIHelperUtils.delete)
        {
            //uIHelperUtils.change = true;

            fileManager.HandleDeleteFiles(fileName, caminhoPng, caminhoJson, buttonObj);

            string pasta = Path.GetDirectoryName(caminhoJson);
            if (Directory.Exists(pasta) && Directory.GetFiles(pasta).Length == 0 && Directory.GetDirectories(pasta).Length == 0)
            {
                string pasta2 = Path.GetDirectoryName(caminhoPng);

                fileManager.HandleDeleteFolders(fileName, pasta, pasta2, null);
            }

            //if (Directory.Exists(caminhoJson) && Directory.GetFiles(caminhoJson).Length == 0)
            //    fileManager.HandleDeleteFolder(fileName, caminhoJson, null);

            uIHelperUtils.delete = false;
            return;
        }
        else
        {
            //Carrega informações da peça
            folderText.text = folder;
            namePiece.text = fileName;

            this.fileName = fileName;
            this.folderName = folder;
            selectRootPath = rootPath;

            gridManager.LoadPaintedCells(Path.GetFileName(caminhoJson), rootPath, folder);
            panelFile.SetActive(false);
        }

    }













    private void SaveArt(string fileName, string subfolderName)
    {
        string fileJson = fileName.Trim() + ".json";
        string filePng = fileName.Trim() + ".png";

        if (fileManager.FileExists(subfolderName, filePng, fileManager.basePath_Sprite) ||
            fileManager.FileExists(subfolderName, fileJson, fileManager.basePath_PaintingData))
        {
            string title = "Do you want to replace the file?";
            string text = "There is already a file with the same name in the folder, do you want to replace it?";

            fileManager.CreateWarning(title, text, () =>
            {
                gridManager.Save(fileJson, filePng, subfolderName);
                //uIHelperUtils.change = true;
                panelFolder.SetActive(false);

                folderText.text = subfolderName;
                namePiece.text = fileName;
                this.fileName = fileName;
                this.folderName = subfolderName;
            });

            return; // sai daqui e espera o clique do usuário
        }

        // Se não existir, salva direto
        gridManager.Save(fileJson, filePng, subfolderName);
        //uIHelperUtils.change = true;
        panelFolder.SetActive(false);

        //folderText.text = subfolderName;
        namePiece.text = fileName;
        this.fileName = fileName;
        this.folderName = subfolderName;
        folderText.text = subfolderName;

        folderNavigation.RefreshFolderButton(folderName, Application.persistentDataPath);
    }

    private void QuickSaveArt()
    {
        if (string.IsNullOrEmpty(this.fileName)) // || namePiece.text == "Name"
            return;

        if (string.IsNullOrEmpty(this.folderName)) // || folderText.text == "Squad"
            return;

        if (selectRootPath == Application.streamingAssetsPath)
        {
            fileManager.CreateAdvice("Não é permitido salvar pastas de StreamingAssets!");
            return;
        }


        string fileName = namePiece.text; //namePiece.text;
        string subfolderName = this.folderName;

        string fileJson = fileName.Trim() + ".json";
        string filePng = fileName.Trim() + ".png";

        if (this.fileName != namePiece.text)
        {
            string fileJson_ = this.fileName.Trim() + ".json";
            string filePng_ = this.fileName.Trim() + ".png";

            string caminhoPasta = Path.Combine(selectRootPath, fileManager.basePath_Sprite, this.folderName, filePng_);
            string caminhoPastaJson = Path.Combine(selectRootPath, fileManager.basePath_PaintingData, this.folderName, fileJson_);

            fileManager.HandleDeleteFiles(fileName, caminhoPasta, caminhoPastaJson, null);


            if (fileManager.FileExists(subfolderName, filePng, fileManager.basePath_Sprite) ||
                fileManager.FileExists(subfolderName, fileJson, fileManager.basePath_PaintingData))
            {
                string title = "Do you want to Save the file?";
                string text = "Já existe um artquivo chamado: " + fileName;

                fileManager.CreateWarning(title, text, () =>
                {
                    gridManager.Save(fileJson, filePng, subfolderName);
                    //uIHelperUtils.change = true;
                    panelFolder.SetActive(false);

                    namePiece.text = fileName;
                    this.fileName = fileName;
                    this.folderName = subfolderName;
                    folderText.text = subfolderName;

                    folderNavigation.RefreshFolderButton(folderName, Application.persistentDataPath);
                });

                return; // sai daqui e espera o clique do usuário
            }

        }


        if (fileManager.FileExists(subfolderName, filePng, fileManager.basePath_Sprite) ||
            fileManager.FileExists(subfolderName, fileJson, fileManager.basePath_PaintingData))
        {
            string title = "Do you want to Save the file?";
            string text = "Are you sure you want to save the file?";

            fileManager.CreateWarning(title, text, () =>
            {
                gridManager.Save(fileJson, filePng, subfolderName);
                //uIHelperUtils.change = true;
                panelFolder.SetActive(false);

                namePiece.text = fileName;
                this.fileName = fileName;
                this.folderName = subfolderName;
                folderText.text = subfolderName;

                folderNavigation.RefreshFolderButton(folderName, Application.persistentDataPath);
            });

            return; // sai daqui e espera o clique do usuário
        }

        // Se não existir, salva direto
        gridManager.Save(fileJson, filePng, subfolderName);
        //uIHelperUtils.change = true;
        panelFolder.SetActive(false);

        namePiece.text = fileName;
        this.fileName = fileName;
        this.folderName = subfolderName;
        folderText.text = subfolderName;


        folderNavigation.RefreshFolderButton(folderName, Application.persistentDataPath);
    }



}
