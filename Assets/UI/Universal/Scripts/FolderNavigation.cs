using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;
using System.Collections;

public class FolderNavigation : MonoBehaviour
{
    public FileManager fileManager;
    public UIHelperUtils uIHelperUtils;
    public FileNavigation fileNavigation;

    [Header("Managers")]
    public NavigationManage_Create manageCreate;
    public NavigationManage_Painting managePainting;
    public NavigationManage_Squad manageSquad;
    public ProfileImageManager profileImageManager;

    [Header("Prefabs")]
    public GameObject folderbuttonPrefab;

    [Header("Panels")]
    public GameObject panelFolders;

    [Header("Options")]
    public Button allBtw;
    public Button myBtw;
    public Button libraryBtw;
    public Button piecesBtw;

    [Header("Buttons:")]
    public Button backButton;
    public Button newButton;
    public Button deleteButton;

    [Header("Control")]
    public GameObject currentButtonFolder;
    public string selectRootPath;
    public string selectBasePath;
    public bool initiate = false;


    private bool setCursor = false;



    void Start()
    {

        if (fileManager == null)
        {
            fileManager = FindFirstObjectByType<FileManager>();
        }
        if (fileNavigation == null)
        {
            fileNavigation = FindFirstObjectByType<FileNavigation>();
        }


        if (manageCreate == null)
        {
            manageCreate = FindFirstObjectByType<NavigationManage_Create>();
        }
        if (managePainting == null)
        {
            managePainting = FindFirstObjectByType<NavigationManage_Painting>();
        }
        if (manageSquad == null)
        {
            manageSquad = FindFirstObjectByType<NavigationManage_Squad>();

            if (manageSquad)
                piecesBtw.gameObject.SetActive(false);
        }
        if (profileImageManager == null)
        {
            profileImageManager = FindFirstObjectByType<ProfileImageManager>();
        }

        allBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setAll())
                StartCoroutine(UpdateFolderButtons(selectBasePath));

        });
        myBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setMy())
                StartCoroutine(UpdateFolderButtons(selectBasePath));

        });
        libraryBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setLibrary())
                StartCoroutine(UpdateFolderButtons(selectBasePath));

        });



        backButton.onClick.AddListener(() =>
        {
            uIHelperUtils.ResetAllControlBooleans();

            if (manageCreate)
                manageCreate.ResetAllControlBooleans();

            panelFolders.SetActive(false);
        });

        deleteButton.onClick.AddListener(() =>
        {
            uIHelperUtils.delete = !uIHelperUtils.delete;
            setCursor = true;

            UIHelperUtils.SetCursor(uIHelperUtils.TrashIcon, CursorHotspot.Center);
        });


        piecesBtw.onClick.AddListener(() =>
        {
            if (initiate) return;

            uIHelperUtils.OnFolder = false;
            uIHelperUtils.OnFiles = true;

            uIHelperUtils.setAll();

            //if (managePainting)
            //    fileNavigation.selectBasePath = fileManager.basePath_Sprite;
            //else
            //    fileNavigation.selectBasePath = fileManager.basePath_PieceData;

            panelFolders.SetActive(false);

            fileNavigation.navigationOptions.SetActive(true);
            fileNavigation.squadsBtw.gameObject.SetActive(true);

            fileNavigation.panelFile.SetActive(true);

            StartCoroutine(fileNavigation.UpdateFilesButtons());

            /*
            if (uIHelperUtils.change)
            {
                 StartCoroutine(fileNavigation.UpdateFilesButtons());
                uIHelperUtils.change = false;
            }
            */

        });

        newButton.onClick.AddListener(() =>
        {

            string title = UIHelperUtils.T("file.create.title");
            string inputText = UIHelperUtils.T("file.create.txt");

            if (string.IsNullOrEmpty(title))
                title = "Create Set";

            if (string.IsNullOrEmpty(inputText))
                inputText = "Enter the name...";

            fileManager.CreateInput(title, inputText, (text) =>
            {
                CreateFolder(text);
            });

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


    private void OnClickFolder(string pasta, GameObject newButton, string rootPath)
    {
        fileNavigation.navigationOptions.SetActive(false);
        currentButtonFolder = newButton;
        selectRootPath = rootPath;

        if (manageCreate)
            manageCreate.OnClickFolder(pasta, newButton, rootPath);
        else if (managePainting)
            managePainting.OnClickFolder(pasta, newButton, rootPath);
        else if (manageSquad)
            manageSquad.OnClickFolder(pasta, newButton, rootPath);
        else if (profileImageManager)
        {
            uIHelperUtils.back = true;
            uIHelperUtils.change = false;

            fileNavigation.StartCreatingFileButtons(pasta, rootPath, fileManager.basePath_Sprite);
            panelFolders.SetActive(false);
            fileNavigation.panelFile.SetActive(true);
        }

    }


    public void CreateFolder(string text)
    {
        string folderName = text.Trim();

        if (string.IsNullOrEmpty(folderName))
        {
            Debug.LogWarning("O nome da pasta não pode estar vazio!");
            return;
        }

        string squadFullPath = Path.Combine(Application.persistentDataPath, selectBasePath, folderName);

        if (!Directory.Exists(squadFullPath))
        {
            Directory.CreateDirectory(squadFullPath);
            Debug.Log("Pasta criada em: " + squadFullPath);
            StartCoroutine(CreateSingleFolderButton(folderName, Application.persistentDataPath));
        }
        else
        {
            string text2 = UIHelperUtils.T("folder.exist");

            if (string.IsNullOrEmpty(text2))
                text2 = "A folder with this name already exists!";

            fileManager.CreateAdvice(text2);
        }

        //StartCoroutine(UpdateFolderButtons());
    }

    public void RefreshFolderButton(string folderName)
    {

        if (selectRootPath == Application.streamingAssetsPath)
            return;

        Transform content = panelFolders.transform.Find("Scroll View/Viewport/Content");

        // 🔍 procura o botão pelo nome
        Transform existingButton = content.Find(folderName);

        if (existingButton != null)
        {
            Destroy(existingButton.gameObject);
        }
        else
        {
            Debug.LogWarning($"Botão da pasta '{folderName}' não encontrado para atualização.");
        }

        // 🔄 recria o botão atualizado
        StartCoroutine(CreateSingleFolderButton(folderName, Application.persistentDataPath));
    }

    public void StartCreatingFolderButtons(string basePath, GameObject panel)
    {
        if (!uIHelperUtils.change)
            return;

        uIHelperUtils.setAll();

        panelFolders = panel;
        selectBasePath = basePath;

        Transform content = panelFolders.transform.Find("Scroll View/Viewport/Content");

        foreach (Transform child in content)
            Destroy(child.gameObject);


        StartCoroutine(UpdateFolderButtons(selectBasePath));
    }

    public IEnumerator UpdateFolderButtons(string basePath)
    {
        initiate = true;

        //Debug.Log("basePath: " + basePath);

        try
        {
            Transform content = panelFolders.transform.Find("Scroll View/Viewport/Content");

            // Remove todos os filhos, exceto o Head
            foreach (Transform child in content)
            {
                if (child.name != "Head")
                {
                    Destroy(child.gameObject);
                }
            }

            List<string> pastas = new List<string>();

            // Carrega pastas do streamingAssetsPath se estiver no "onLibrary"
            if (uIHelperUtils.onLibrary && !uIHelperUtils.save)
            {
                pastas = fileManager.GetSubfoldersIn(basePath, Application.streamingAssetsPath);
                yield return StartCoroutine(CreateFolderButtons(pastas, Application.streamingAssetsPath));
            }

            // Carrega pastas do persistentDataPath se estiver no "onMy"
            if (uIHelperUtils.onMy)
            {
                pastas = fileManager.GetSubfoldersIn(basePath, Application.persistentDataPath);
                // Espera terminar a criação antes de continuar
                yield return StartCoroutine(CreateFolderButtons(pastas, Application.persistentDataPath));
            }

            // Ajusta tamanho do ScrollView

            UIHelperUtils.SetSizeScrollView(panelFolders);
        }
        finally
        {
            initiate = false;
            uIHelperUtils.change = false;
        }

    }



    private IEnumerator CreateFolderButtons(List<string> pastas, string rootPath)
    {
        foreach (string pasta in pastas)
        {
            yield return StartCoroutine(CreateSingleFolderButton(pasta, rootPath));
        }
    }



    private IEnumerator CreateSingleFolderButton(string pasta, string rootPath)
    {
        Transform content = panelFolders.transform.Find("Scroll View/Viewport/Content");

        GameObject newButton = Instantiate(folderbuttonPrefab, content);

        newButton.name = $"{pasta}";

        newButton.transform.SetSiblingIndex(0);

        // Texto
        bool translate = UIHelperUtils.CheckTranslationFile(rootPath, selectBasePath, pasta);

        string name = pasta;

        if (translate)
        {
            name = UIHelperUtils.T(pasta);
            if (string.IsNullOrEmpty(name))
                name = pasta;
        }

        TextMeshProUGUI nomeTexto = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (nomeTexto != null)
        {
            if (LocalizationManager.Instance)
                nomeTexto.font = LocalizationManager.Instance.currentFont;
            nomeTexto.text = name;
        }

        // Click
        Button button = newButton.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            OnClickFolder(pasta, newButton, rootPath);
        });

        // ===============================
        // PIECE DATA
        // ===============================
        /*
        if (selectBasePath == fileManager.basePath_PieceData)
        {
            string caminhoPasta = Path.Combine(rootPath, selectBasePath, pasta);
            if (!Directory.Exists(caminhoPasta))
                yield break;

            string[] arquivosJson = Directory.GetFiles(caminhoPasta, "*.json");

            Transform panelImagens = newButton.transform.Find("Panel");

            foreach (string jsonPath in arquivosJson)
            {
                string json = File.ReadAllText(jsonPath);
                PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

                if (wrapper?.piece == null)
                    continue;

                PieceInfo piece = wrapper.piece;

                string caminhoSprite = piece.NativeSprite
                    ? Path.Combine(Application.streamingAssetsPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art + ".png")
                    : Path.Combine(rootPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art + ".png");

                GameObject imgObj = new GameObject(piece.Name, typeof(Image));
                imgObj.transform.SetParent(panelImagens, false);

                Image img = imgObj.GetComponent<Image>();
                img.sprite = UIHelperUtils.GetSpriteFromPath(caminhoSprite);

                yield return null;
            }
        }
        // ===============================
        // SPRITE
        // ===============================
        else if (selectBasePath == fileManager.basePath_Sprite)
        {
            Transform panelImagens = newButton.transform.Find("Panel");

            string pathSprites = Path.Combine(rootPath, fileManager.basePath_Sprite, pasta);
            string pathJsons = Path.Combine(rootPath, fileManager.basePath_PaintingData, pasta);

            List<SpriteData> sprites = new List<SpriteData>();

            yield return StartCoroutine(
                uIHelperUtils.LoadSpritesFromPathCoroutine(
                    pathSprites,
                    sprites
                )
            );

            foreach (var spriteData in sprites)
            {
                GameObject imgObj = new GameObject(spriteData.Name, typeof(Image));
                imgObj.transform.SetParent(panelImagens, false);

                Image img = imgObj.GetComponent<Image>();
                img.sprite = spriteData.Sprite;

                yield return null;
            }
        }
        */

        yield return null;
    }

}


