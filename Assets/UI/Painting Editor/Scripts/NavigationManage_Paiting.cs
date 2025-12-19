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

    [Header("TMP_Text")]
    public TMP_Text folderText;
    public TMP_Text namePiece;

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

            fileManager.HandleDeleteFolder(pasta, caminhoPasta, buttonObj);
            fileManager.HandleDeleteFolder(pasta, caminhoPastaJson, null);

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


            string name = null;
            if (!string.IsNullOrEmpty(namePiece.text) && namePiece.text != "Name")
            {
                name = namePiece.text;
            }

            fileManager.CreateInput("Salvar Arquivo", "Digite o nome...", (text) =>
            {
                SaveArt(text, pasta);
            }, name);
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

        string caminhoPng = Path.Combine(rootPath, fileManager.basePath_Sprite, folder, Path.ChangeExtension(fileName, ".png"));
        string caminhoJson = Path.Combine(rootPath, fileManager.basePath_PaintingData, folder, Path.ChangeExtension(fileName, ".json"));

        if (uIHelperUtils.delete)
        {
            uIHelperUtils.change = true;

            fileManager.HandleDeleteFile(fileName, caminhoPng, buttonObj);
            fileManager.HandleDeleteFile(fileName, caminhoJson, null);

            string pasta = Path.GetDirectoryName(caminhoJson);
            if (Directory.Exists(pasta) && Directory.GetFiles(pasta).Length == 0 && Directory.GetDirectories(pasta).Length == 0)
            {
                fileManager.HandleDeleteFolder(fileName, pasta, null);
                pasta = Path.GetDirectoryName(caminhoPng);
                fileManager.HandleDeleteFolder(fileName, pasta, null);
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
                uIHelperUtils.change = true;
                panelFolder.SetActive(false);


                folderText.text = subfolderName;
                namePiece.text = fileName;
            });

            return; // sai daqui e espera o clique do usuário
        }

        // Se não existir, salva direto
        gridManager.Save(fileJson, filePng, subfolderName);
        uIHelperUtils.change = true;
        panelFolder.SetActive(false);

        folderText.text = subfolderName;
        namePiece.text = fileName;
    }

    private void QuickSaveArt()
    {
        if (string.IsNullOrEmpty(namePiece.text) || namePiece.text == "Name")
            return;

        if (string.IsNullOrEmpty(folderText.text) || folderText.text == "Squad")
            return;

        string fileName = namePiece.text;
        string subfolderName = folderText.text;

        string fileJson = fileName.Trim() + ".json";
        string filePng = fileName.Trim() + ".png";

        if (fileManager.FileExists(subfolderName, filePng, fileManager.basePath_Sprite) ||
            fileManager.FileExists(subfolderName, fileJson, fileManager.basePath_PaintingData))
        {
            string title = "Do you want to Save the file?";
            string text = "Are you sure you want to save the file?";

            fileManager.CreateWarning(title, text, () =>
            {
                gridManager.Save(fileJson, filePng, subfolderName);
                uIHelperUtils.change = true;
                panelFolder.SetActive(false);
            });

            return; // sai daqui e espera o clique do usuário
        }

        // Se não existir, salva direto
        gridManager.Save(fileJson, filePng, subfolderName);
        uIHelperUtils.change = true;
        panelFolder.SetActive(false);
    }



}
