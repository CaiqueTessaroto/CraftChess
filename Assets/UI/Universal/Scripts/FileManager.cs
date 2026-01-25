using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Collections;
using UnityEngine.Networking;




public class FileManager : MonoBehaviour
{
    [Header("Directory Path:")]
    public string basePath_Sprite = "Sprites";
    public string basePath_PaintingData = "PaintingEditor";
    public string basePath_UserData = "User";
    public string basePath_PieceData = "Pieces";
    public string basePath_SquadData = "Squads";

    [Header("System:")]
    public GameObject warningPrefab;
    public GameObject advicePrefab;
    public GameObject inputPrefab;
    public Transform panel;
    public bool warning = false;

    void Start()
    {

    }

    public void CreateInput(string title, string placeholder, System.Action<string> onContinue, string defaultValue = null)
    {
        // Instancia o prefab como filho do panel
        GameObject newInput = Instantiate(inputPrefab, panel);

        newInput.transform.localScale = Vector3.one;

        TMP_Text titleText = newInput.transform.Find("Title")?.GetComponent<TMP_Text>();
        if (titleText != null)
            titleText.text = title;

        TMP_InputField inputField = newInput.GetComponentInChildren<TMP_InputField>();
        if (inputField != null)
        {
            // Preenche com valor padrão se houver
            if (!string.IsNullOrEmpty(defaultValue))
                inputField.text = defaultValue;

            if (inputField.placeholder != null)
            {
                TextMeshProUGUI placeholderText = inputField.placeholder.GetComponent<TextMeshProUGUI>();
                if (placeholderText != null)
                    placeholderText.text = placeholder;
            }

            inputField.characterLimit = 50;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

        }

        Button buttonCancel = newInput.transform
            .Find("PanelBtn/Cancel")
            .GetComponent<Button>();

        Button buttonContinue = newInput.transform
            .Find("PanelBtn/Continue")
            .GetComponent<Button>();

        if (buttonCancel != null)
        {
            buttonCancel.onClick.AddListener(() =>
            {
                Destroy(newInput);
            });
        }

        if (buttonContinue != null)
        {
            buttonContinue.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    onContinue?.Invoke(inputField.text); // retorna o texto digitado
                    Destroy(newInput);
                }
            });
        }
    }

    public void CreateWarning(string title, string text, System.Action onContinue)
    {
        warning = true;

        GameObject newWarning = Instantiate(warningPrefab, panel);

        // Acessa o TextMeshPro do head
        TMP_Text headText = newWarning.transform
            .Find("Head/Text (TMP)")
            .GetComponent<TMP_Text>();

        Image imageIcon = newWarning.transform
            .Find("Head/Image")
            .GetComponent<Image>();

        // Acessa o TextMeshPro do body
        TMP_Text bodyText = newWarning.transform
            .Find("Body/Text (TMP)")
            .GetComponent<TMP_Text>();

        Button buttonCancel = newWarning.transform
            .Find("Foot/ButtonCancel")
            .GetComponent<Button>();

        Button buttonContinue = newWarning.transform
            .Find("Foot/ButtonContinue")
            .GetComponent<Button>();


        buttonCancel.onClick.AddListener(() =>
        {
            Destroy(newWarning);
            warning = false;
        });

        buttonContinue.onClick.AddListener(() =>
        {
            onContinue?.Invoke(); // chama a ação passada
            Destroy(newWarning);
            warning = false;
        });

        // Alterando os textos
        headText.text = title;
        bodyText.text = text;
    }

    public void CreateAdvice(string text)
    {

        GameObject newAdvice = Instantiate(advicePrefab, panel);

        TMP_Text tmpText = newAdvice.transform.Find("Text (TMP)").GetComponent<TMP_Text>();

        Button button = newAdvice.transform.Find("Button").GetComponent<Button>();

        tmpText.text = text;

        button.onClick.AddListener(() =>
        {
            Destroy(newAdvice);
        });

    }



















    public void HandleDeleteFile(string fileName, string path, GameObject buttonObj)
    {
        string title = "File will be deleted";
        string text = "Do you really want to delete the file " + fileName + " ?";

        if (buttonObj)
        {
            if (warning) return;

            CreateWarning(title, text, () =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Destroy(buttonObj);
                    Debug.Log("Arquivo excluído: " + path);
                }
                else
                {
                    Debug.LogWarning("Arquivo não encontrado: " + path);
                }
            });
        }
        else
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("Arquivo excluído: " + path);
            }
            else
            {
                Debug.LogWarning("Arquivo não encontrado: " + path);
            }
        }

    }

    public void HandleDeleteFiles(string fileName, string path1, string path2, GameObject buttonObj = null)
    {
        if (warning) return;

        string title = "Files will be deleted";
        string text = "Do you really want to delete the file " + fileName + " ?";

        void DeleteFiles()
        {
            // Arquivo 1
            if (File.Exists(path1))
            {
                File.Delete(path1);
                Debug.Log("Arquivo excluído: " + path1);
            }
            else
            {
                Debug.LogWarning("Arquivo não encontrado: " + path1);
            }

            // Arquivo 2
            if (File.Exists(path2))
            {
                File.Delete(path2);
                Debug.Log("Arquivo excluído: " + path2);
            }
            else
            {
                Debug.LogWarning("Arquivo não encontrado: " + path2);
            }

            if (buttonObj)
                Destroy(buttonObj);
        }

        CreateWarning(title, text, DeleteFiles);
    }

    public void HandleDeleteFolders(string folderName, string path1, string path2, GameObject buttonObj = null)
    {
        if (warning) return;

        string title = "Folder will be deleted";
        string text = "Do you really want to delete the folder " + folderName + " ?";

        void DeleteFiles()
        {
            // Arquivo 1
            if (Directory.Exists(path1))
            {
                Directory.Delete(path1, true); ;
                Debug.Log("Pasta excluída: " + path1);
            }
            else
            {
                Debug.LogWarning("Pasta não encontrado: " + path1);
            }

            // Arquivo 2
            if (Directory.Exists(path2))
            {
                Directory.Delete(path2, true); ;
                Debug.Log("Pasta excluída: " + path2);
            }
            else
            {
                Debug.LogWarning("Pasta não encontrado: " + path2);
            }

            if (buttonObj)
                Destroy(buttonObj);
        }

        CreateWarning(title, text, DeleteFiles);
    }

    public void HandleDeleteFolder(string pasta, string caminhoPasta, GameObject newButton)
    {
        if (!Directory.Exists(caminhoPasta))
        {
            Debug.LogWarning("A pasta não existe: " + caminhoPasta);
            return;
        }

        if (Directory.GetFiles(caminhoPasta).Length > 0)
        {
            //CreateAdvice("The folder is not empty: " + pasta);
            //return;
        }

        if (newButton)
        {
            if (warning) return;

            string title = "Folder will be deleted";
            string text = "Do you really want to delete the folder " + pasta + " ?";

            //Debug.Log("Pasta: " + caminhoPasta);

            CreateWarning(title, text, () =>
            {
                Directory.Delete(caminhoPasta, true);
                Destroy(newButton);
                Debug.Log("Pasta excluída: " + caminhoPasta);
                warning = false;
            });
        }
        else
        {
            Directory.Delete(caminhoPasta, true);
            Debug.Log("Pasta excluída: " + caminhoPasta);
            warning = false;
        }


    }













    public void CleanUpEmptyFolder(string basePath, string folder)
    {
        string mainFolder = Path.Combine(Application.persistentDataPath, basePath, folder);

        if (!Directory.Exists(mainFolder))
            return;

        try
        {
            // Pega todas as subpastas dentro do squad
            string[] subfolders = Directory.GetDirectories(mainFolder);

            foreach (string subfolder in subfolders)
            {
                bool subfolderIsEmpty = Directory.GetFiles(subfolder).Length == 0 &&
                                        Directory.GetDirectories(subfolder).Length == 0;

                if (subfolderIsEmpty)
                {
                    Directory.Delete(subfolder, true);
                    Debug.Log($"Subpasta vazia removida: {subfolder}");
                }
            }

            // Depois de limpar as subpastas, verifica se o squad ficou vazio
            bool squadIsEmpty = Directory.GetFiles(mainFolder).Length == 0 &&
                                Directory.GetDirectories(mainFolder).Length == 0;

            if (squadIsEmpty)
            {
                Directory.Delete(mainFolder, true);
                Debug.Log($"Squad '{folder}' removido (ficou vazio).");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Erro ao limpar squad '{folder}': {ex.Message}");
        }
    }









    public void SaveJson(string folderName, string fileName, string json, string basePath)
    {
        // Caminho base -> persistentDataPath/Art
        string pieceFolderPath = Path.Combine(Application.persistentDataPath, basePath);

        // Garante que a pasta exista
        if (!Directory.Exists(pieceFolderPath))
            Directory.CreateDirectory(pieceFolderPath);

        // Caminho da subpasta desejada
        string targetFolderPath = Path.Combine(pieceFolderPath, folderName);

        // Garante que a subpasta exista
        if (!Directory.Exists(targetFolderPath))
            Directory.CreateDirectory(targetFolderPath);

        // Caminho completo do arquivo
        string filePath = Path.Combine(targetFolderPath, fileName);

        // Salva o JSON
        File.WriteAllText(filePath, json);

        Debug.Log($"Arquivo salvo em: {filePath}");
    }

    public void SavePng(string folderName, string fileName, Texture2D texture, string basePath)
    {
        string artFolderPath = Path.Combine(Application.persistentDataPath, basePath);

        if (!Directory.Exists(artFolderPath))
            Directory.CreateDirectory(artFolderPath);

        string targetFolderPath = Path.Combine(artFolderPath, folderName);

        if (!Directory.Exists(targetFolderPath))
            Directory.CreateDirectory(targetFolderPath);

        int pngCount = Directory.GetFiles(targetFolderPath, "*.png").Length;

        if (pngCount >= 16)
        {
            string text = UIHelperUtils.T("file.limit.txt", 16);

            if (string.IsNullOrEmpty(text))
                text = "The limit of 16 files in this folder has been reached.";

            CreateAdvice(text);
            return;
        }

        string filePath = Path.Combine(targetFolderPath, fileName);

        byte[] pngBytes = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngBytes);

        Debug.Log($"PNG salvo em: {filePath}");
    }

    public bool FileExists(string folderName, string fileName, string basePath)
    {
        string artFolderPath = Path.Combine(Application.persistentDataPath, basePath);
        string targetFolderPath = Path.Combine(artFolderPath, folderName);
        string filePath = Path.Combine(targetFolderPath, fileName);

        return File.Exists(filePath);
    }

    public string LoadJson(string rootPath, string basePath, string folderName, string fileName)
    {
        string filePath = Path.Combine(rootPath, basePath, folderName, fileName);

        if (File.Exists(filePath))
        {
            Debug.Log($"Arquivo carregado de: {filePath}");
            return File.ReadAllText(filePath);
        }
        else
        {
            Debug.LogWarning($"Arquivo não encontrado: {filePath}");
            return null;
        }
    }










    public Texture2D LoadTextureFromFile(string folderName, string fileName, string basePath, string rootPath)
    {
        string folderPath = Path.Combine(rootPath, basePath, folderName);
        string fullPath = Path.Combine(folderPath, fileName.Trim() + ".png");

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"Arquivo não encontrado: {fullPath}");
            return null;
        }

        byte[] fileData = File.ReadAllBytes(fullPath);
        //Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);

        tex.LoadImage(fileData, true);
        return tex;
    }

    public Sprite ConvertTextureToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                             new Vector2(0.5f, 0.5f), 100);
    }







    public int GetSubfolderCount(string basePath, string rootPath)
    {
        string fullPath = Path.Combine(rootPath, basePath);

        if (!Directory.Exists(fullPath))
            return 0;

        return Directory.GetDirectories(fullPath).Length;
    }




    public List<string> GetSubfoldersIn(string basePath, string rootPath, bool orderByModificationDate = true, bool descending = true)
    {
        List<string> subfolders = new List<string>();

        //#if UNITY_ANDROID && !UNITY_EDITOR
        // No Android não faz nada
        if (rootPath == Application.streamingAssetsPath)
            return subfolders;
        //#endif

        string fullPath = Path.Combine(rootPath, basePath);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            return subfolders;
        }

        // Obter informações das pastas
        var dirInfo = new DirectoryInfo(fullPath);
        var directories = dirInfo.GetDirectories();

        // Ordenar por data de modificação se solicitado
        if (orderByModificationDate)
        {
            if (descending)
                directories = directories.OrderByDescending(d => d.LastWriteTime).ToArray();
            else
                directories = directories.OrderBy(d => d.LastWriteTime).ToArray();
        }
        // Caso contrário, ordenar por nome (opcional)
        else
        {
            directories = directories.OrderBy(d => d.Name).ToArray();
        }

        foreach (var dir in directories)
        {
            subfolders.Add(dir.Name);
        }

        subfolders.Reverse();
        return subfolders;
    }



}
