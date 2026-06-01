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
    public GameObject messagePrefab;
    public Transform panel;
    public bool warning = false;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float lifetime = 1f;

    public static FileManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {

    }


    public void SpawnMessage(string text)
    {
        GameObject instance = Instantiate(messagePrefab, panel);

        TextMeshProUGUI tmp = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            Destroy(instance);
            return;
        }

        tmp.text = text;
        tmp.font = LocalizationManager.Instance?.currentFont;

        StartCoroutine(FadeRoutine(tmp, instance));
    }


    private IEnumerator FadeRoutine(TextMeshProUGUI tmp, GameObject instance)
    {
        Color color = tmp.color;

        // Começa invisível
        //color.a = 0f;
        //tmp.color = color;

        // Fade In
        float t = 0f;
        //while (t < fadeDuration)
        //{
        //    t += Time.deltaTime;
        //    color.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
        //    tmp.color = color;
        //    yield return null;
        //}

        // Tempo visível
        yield return new WaitForSeconds(lifetime);

        // Fade Out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            tmp.color = color;
            yield return null;
        }

        Destroy(instance);
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
            if (LocalizationManager.Instance)
            {
                if (inputField.textComponent != null)
                    inputField.textComponent.font = LocalizationManager.Instance.currentFont;

                if (inputField.placeholder is TextMeshProUGUI placeholderTMP)
                    placeholderTMP.font = LocalizationManager.Instance.currentFont;
            }

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
        if (LocalizationManager.Instance)
        {
            bodyText.font = LocalizationManager.Instance.currentFont;
            headText.font = LocalizationManager.Instance.currentFont;
        }
    }

    public void CreateAdvice(string text)
    {

        GameObject newAdvice = Instantiate(advicePrefab, panel);

        TMP_Text tmpText = newAdvice.transform.Find("Text (TMP)").GetComponent<TMP_Text>();

        Button button = newAdvice.transform.Find("Button").GetComponent<Button>();

        tmpText.text = text;
        tmpText.font = LocalizationManager.Instance?.currentFont;

        button.onClick.AddListener(() =>
        {
            Destroy(newAdvice);
        });

    }



















    public void HandleDeleteFile(string fileName, string path, GameObject buttonObj)
    {

        string relativePath = path
    .Replace(Application.persistentDataPath, "")
    .TrimStart('/', '\\');

        string rootFolder = relativePath.Split('/', '\\')[0];

        string directoryPath = Path.GetDirectoryName(path);
        string fileFolder = Path.GetFileName(directoryPath);

        bool translate = UIHelperUtils.CheckTranslationFile(Application.persistentDataPath, rootFolder, fileFolder);

        string name = fileName;

        if (translate)
        {
            name = UIHelperUtils.T(fileName);
            if (string.IsNullOrEmpty(name))
                name = fileName;
        }

        string title = UIHelperUtils.T("file.delete.title");
        string text = UIHelperUtils.T("file.delete.txt", name);

        if (string.IsNullOrEmpty(title))
            title = "The file will be deleted";
        if (string.IsNullOrEmpty(text))
            text = "Are you sure you want to delete " + name + "? This action is permanent and cannot be undone.";

        if (buttonObj)
        {
            if (warning) return;

            CreateWarning(title, text, () =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Destroy(buttonObj);
                    //    Debug.Log("Arquivo excluído: " + path);
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

        string relativePath = path1
        .Replace(Application.persistentDataPath, "")
        .TrimStart('/', '\\');

        string rootFolder = relativePath.Split('/', '\\')[0];

        string directoryPath = Path.GetDirectoryName(path1);
        string fileFolder = Path.GetFileName(directoryPath);

        bool translate = UIHelperUtils.CheckTranslationFile(Application.persistentDataPath, rootFolder, fileFolder);

        string name = fileName;

        if (translate)
        {
            name = UIHelperUtils.T(fileName);
            if (string.IsNullOrEmpty(name))
                name = fileName;
        }

        string title = UIHelperUtils.T("file.delete.title");
        string text = UIHelperUtils.T("file.delete.txt", name);

        if (string.IsNullOrEmpty(title))
            title = "The file will be deleted";
        if (string.IsNullOrEmpty(text))
            text = "Are you sure you want to delete " + name + "? This action is permanent and cannot be undone.";

        void DeleteFiles()
        {
            // Arquivo 1
            if (File.Exists(path1))
            {
                File.Delete(path1);
            }
            else
            {
                Debug.LogWarning("Arquivo não encontrado: " + path1);
            }

            // Arquivo 2
            if (File.Exists(path2))
            {
                File.Delete(path2);
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

        string title = UIHelperUtils.T("folder.delete.title");
        string text = UIHelperUtils.T("folder.delete.txt", folderName);

        if (string.IsNullOrEmpty(title))
            title = "The folder will be deleted";
        if (string.IsNullOrEmpty(text))
            text = "Are you sure you want to delete " + folderName + " and all its contents? This action is permanent and cannot be undone.";

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

    public void HandleDeleteFolder(string folderName, string path, GameObject buttonObj)
    {
        if (warning) return;

        string relativePath = path
        .Replace(Application.persistentDataPath, "")
        .TrimStart('/', '\\');

        string rootFolder = relativePath.Split('/', '\\')[0];

        bool translate = UIHelperUtils.CheckTranslationFile(Application.persistentDataPath, rootFolder, folderName);

        string name = folderName;

        if (translate)
        {
            name = UIHelperUtils.T(folderName);
            if (string.IsNullOrEmpty(name))
                name = folderName;
        }

        string title = UIHelperUtils.T("folder.delete.title");
        string text = UIHelperUtils.T("folder.delete.txt", name);

        if (string.IsNullOrEmpty(title))
            title = "The folder will be deleted";
        if (string.IsNullOrEmpty(text))
            text = "Are you sure you want to delete " + name + " and all its contents? This action is permanent and cannot be undone.";

        void DeleteFiles()
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true); ;
                Debug.Log("Pasta excluída: " + path);
            }
            else
            {
                Debug.LogWarning("Pasta não encontrado: " + path);
            }

            if (buttonObj)
                Destroy(buttonObj);
        }

        CreateWarning(title, text, DeleteFiles);
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









    public void SaveJson(string folderName, string fileName, string json, string basePath, bool message = true)
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

        //Debug.Log($"Arquivo salvo em: {filePath}");

        string pasta = Path.GetFileName(Path.GetDirectoryName(filePath));

        if (pasta == "Pallets" || message == false) //|| pasta == Path.GetFileNameWithoutExtension(fileName)
            return;

        bool translate = UIHelperUtils.CheckTranslationFile(Application.persistentDataPath, basePath, pasta);

        string name = pasta;

        if (translate)
        {
            name = UIHelperUtils.T(name);
            if (string.IsNullOrEmpty(name))
                name = pasta;
        }

        string textMessage = UIHelperUtils.T("saved.in", name);
        if (string.IsNullOrEmpty(textMessage))
            textMessage = $"Saved in " + name;

        SpawnMessage(textMessage);
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

        //Debug.Log($"PNG salvo em: {filePath}");

        string pasta = Path.GetFileName(Path.GetDirectoryName(filePath));

        bool translate = UIHelperUtils.CheckTranslationFile(Application.persistentDataPath, basePath, pasta);

        string name = pasta;

        if (translate)
        {
            name = UIHelperUtils.T(name);
            if (string.IsNullOrEmpty(name))
                name = pasta;
        }

        string textMessage = UIHelperUtils.T("saved.in", name);
        if (string.IsNullOrEmpty(textMessage))
            textMessage = $"Saved in " + name;

        SpawnMessage(textMessage);
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
            //Debug.Log($"Arquivo carregado de: {filePath}");
            return File.ReadAllText(filePath);
        }
        else
        {
            //Debug.LogWarning($"Arquivo não encontrado: {filePath}");
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
