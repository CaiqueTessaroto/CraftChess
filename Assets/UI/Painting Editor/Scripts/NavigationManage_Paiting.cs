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

            if (!string.IsNullOrEmpty(folderName))
                folderNavigation.RefreshFolderButton(folderName);

            folderNavigation.panelFolders.SetActive(true);

            folderNavigation.StartCreatingFolderButtons(fileManager.basePath_Sprite, folderNavigation.panelFolders);
        });


        loadBtn.onClick.AddListener(() =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            uIHelperUtils.save = false;

            if (!string.IsNullOrEmpty(folderName))
                folderNavigation.RefreshFolderButton(folderName);

            folderNavigation.panelFolders.SetActive(true);

            folderNavigation.StartCreatingFolderButtons(fileManager.basePath_Sprite, folderNavigation.panelFolders);
        });

        QuickSave.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(folderName) && string.IsNullOrEmpty(fileName))
            {
                string text = UIHelperUtils.T("file.none.art.txt");

                if (string.IsNullOrEmpty(text))
                    text = "No art selected.";

                fileManager.CreateAdvice(text);
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
                string text = UIHelperUtils.T("file.native.delete.txt");

                if (string.IsNullOrEmpty(text))
                    text = "Deleting the native library is not allowed.";

                fileManager.CreateAdvice(text);
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
                string text = UIHelperUtils.T("file.native.save.txt");

                if (string.IsNullOrEmpty(text))
                    text = "Saving to the native library is not allowed.";

                fileManager.CreateAdvice(text);
                return;
            }


            //string name = null;
            if (string.IsNullOrEmpty(namePiece.text))
            {
                string name = null;

                string title = UIHelperUtils.T("file.save");
                string inputText = UIHelperUtils.T("file.create.txt");

                if (string.IsNullOrEmpty(title))
                    title = "Create Set";

                if (string.IsNullOrEmpty(inputText))
                    inputText = "Enter the name...";

                fileManager.CreateInput(title, inputText, (text) =>
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
        //string caminhoJson = Path.Combine(rootPath, fileManager.basePath_PaintingData, folder, fileName.Trim() + ".json");

        if (uIHelperUtils.delete)
        {
            //uIHelperUtils.change = true;

            fileManager.HandleDeleteFile(fileName, caminhoPng, buttonObj);

            string pasta = Path.GetDirectoryName(caminhoPng);
            if (Directory.Exists(pasta) && Directory.GetFiles(pasta).Length == 0 && Directory.GetDirectories(pasta).Length == 0)
            {
                //string pasta2 = Path.GetDirectoryName(caminhoPng);

                fileManager.HandleDeleteFolder(fileName, pasta, null);
            }

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



            if (imageImporter == null)
                imageImporter = FindObjectOfType<ImageImporter>();


            imageImporter.ImportImage(caminhoPng, 34, 34);
            // gridManager.LoadPaintedCells(Path.GetFileName(caminhoJson), rootPath, folder);
            panelFile.SetActive(false);
        }

    }

    public ImageImporter imageImporter;












    private void SaveArt(string fileName, string subfolderName)
    {
        //string fileJson = fileName.Trim() + ".json";
        string filePng = fileName.Trim() + ".png";

        if (fileManager.FileExists(subfolderName, filePng, fileManager.basePath_Sprite))
        {

            string title = UIHelperUtils.T("file.replace.title");
            string text = UIHelperUtils.T("file.replace.txt");

            if (string.IsNullOrEmpty(title))
                title = "Do you want to replace the file?";
            if (string.IsNullOrEmpty(text))
                text = "There is already a file with the same name in the folder, do you want to replace it?";


            fileManager.CreateWarning(title, text, () =>
            {
                gridManager.Save(filePng, subfolderName);
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
        gridManager.Save(filePng, subfolderName);
        //uIHelperUtils.change = true;
        panelFolder.SetActive(false);

        //folderText.text = subfolderName;
        namePiece.text = fileName;
        this.fileName = fileName;
        this.folderName = subfolderName;
        folderText.text = subfolderName;

        folderNavigation.RefreshFolderButton(folderName);
    }

    private void QuickSaveArt()
    {
        // ===============================
        // Validações básicas
        // ===============================
        if (string.IsNullOrEmpty(fileName))
            return;

        if (string.IsNullOrEmpty(folderName))
            return;

        if (selectRootPath == Application.streamingAssetsPath)
        {
            string text = UIHelperUtils.T("file.native.save.txt");

            if (string.IsNullOrEmpty(text))
                text = "Saving to the native library is not allowed.";

            fileManager.CreateAdvice(text);
            return;
        }

        string name = namePiece.text.Trim();
        string subfolderName = folderName;
        string filePng = fileName + ".png";
        string newfilePng = name + ".png";

        // ===============================
        // Função local de salvar
        // ===============================
        void Save()
        {
            gridManager.Save(newfilePng, subfolderName);

            panelFolder.SetActive(false);

            namePiece.text = name;
            fileName = name;
            folderName = subfolderName;
            folderText.text = subfolderName;
        }

        // ===============================
        // Caminho completo
        // ===============================
        string fullPath = Path.Combine(
            selectRootPath,
            fileManager.basePath_Sprite,
            subfolderName,
            filePng
        );

        bool fileAlreadyExists =
            fileManager.FileExists(subfolderName, newfilePng, fileManager.basePath_Sprite);

        bool isRenaming =
            !string.IsNullOrEmpty(fileName) &&
            fileName != name;

        // ===============================
        // Caso: rename + conflito
        // ===============================
        if (isRenaming)
        {
            if (fileAlreadyExists)
            {
                string title = UIHelperUtils.T("file.replace.title");
                string text = UIHelperUtils.T("file.replace.txt");

                if (string.IsNullOrEmpty(title))
                    title = "Do you want to replace the file?";
                if (string.IsNullOrEmpty(text))
                    text = "There is already a file with the same name in the folder, do you want to replace it?";

                fileManager.CreateWarning(title, text,
                    () =>
                    {
                        fileManager.HandleDeleteFile(fileName, fullPath, null);
                        Save();
                    }
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

            string title = UIHelperUtils.T("file.overwrite.title");
            string text = UIHelperUtils.T("file.overwrite.txt");

            if (string.IsNullOrEmpty(title))
                title = "Do you want to Save the file?";
            if (string.IsNullOrEmpty(text))
                text = "You are about to overwrite an existing file. Please note that the original content will be permanently deleted and cannot be recovered.";

            fileManager.CreateWarning(title, text,
                Save
            );

            return;
        }

        // ===============================
        // Caso: novo arquivo
        // ===============================
        Save();
    }



}
