using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;
using System.Collections;

public class FileNavigation : MonoBehaviour
{

    public FileManager fileManager;
    public UIHelperUtils uIHelperUtils;
    public FolderNavigation folderNavigation;

    [Header("Managers")]
    public NavigationManage_Create manageCreate;
    public NavigationManage_Painting managePainting;
    public NavigationManage_Squad manageSquad;

    [Header("Prefabs")]
    public GameObject fileButtonPrefab;

    [Header("Options")]
    public Button allBtw;
    public Button myBtw;
    public Button libraryBtw;
    public Button squadsBtw;

    [Header("Panel")]
    public GameObject panelFile;
    public GameObject navigationOptions;

    [Header("Buttons:")]
    public Button backBtw;
    public GameObject deleteObj;
    public Button deleteBtw;

    [Header("Control")]
    public string selectBasePath;
    public bool initiate = false;


    //private string selectRootPath;

    // Start is called before the first frame update
    void Start()
    {


        if (manageCreate == null)
        {
            manageCreate = FindObjectOfType<NavigationManage_Create>();
        }
        if (managePainting == null)
        {
            managePainting = FindObjectOfType<NavigationManage_Painting>();
        }


        allBtw.onClick.AddListener(() =>
        {
            if (uIHelperUtils.setAll())
                StartCoroutine(UpdateFilesButtons());

        });
        myBtw.onClick.AddListener(() =>
        {
            if (uIHelperUtils.setMy())
                StartCoroutine(UpdateFilesButtons());

        });
        libraryBtw.onClick.AddListener(() =>
        {
            if (uIHelperUtils.setLibrary())
                StartCoroutine(UpdateFilesButtons());

        });


        deleteBtw = deleteObj.GetComponent<Button>();


        deleteBtw.onClick.AddListener(() =>
        {
            uIHelperUtils.delete = true;
        });


        backBtw.onClick.AddListener(() =>
        {

            if (uIHelperUtils.back)
            {
                panelFile.SetActive(false);

                if (uIHelperUtils.change)
                {
                    folderNavigation.StartCreatingFolderButtons(selectBasePath, folderNavigation.panelFolders);
                }

                folderNavigation.panelFolders.SetActive(true);
                uIHelperUtils.back = false;
            }
            else
                panelFile.SetActive(false);

            uIHelperUtils.ResetAllControlBooleans();

            if (manageCreate)
                manageCreate.ResetAllControlBooleans();
        });



        squadsBtw.onClick.AddListener(() =>
        {
            uIHelperUtils.OnFolder = true;
            uIHelperUtils.OnFiles = false;

            uIHelperUtils.setAll();

            panelFile.SetActive(false);

            folderNavigation.panelFolders.SetActive(true);

            StartCoroutine(folderNavigation.UpdateFolderButtons());

            /*
            if (uIHelperUtils.change)
            {
                folderNavigation.UpdateFolderButtons();
                uIHelperUtils.change = false;
            }
            */
        });


    }

    public void StartCreatingFileButtons(string folder, string rootPath, string basePath)
    {
        initiate = true;
        selectBasePath = basePath;

        if (rootPath == Application.streamingAssetsPath)
            deleteObj.SetActive(false);
        else
            deleteObj.SetActive(true);

        Transform content = panelFile.transform.Find("Scroll View/Viewport/Content");

        foreach (Transform child in content)
            Destroy(child.gameObject);


        StartCoroutine(CreateFileButtons(folder, rootPath));
    }

    public IEnumerator UpdateFilesButtons()
    {
        Transform content = panelFile.transform.Find("Scroll View/Viewport/Content");

        // Remove botões antigos
        foreach (Transform child in content)
            Destroy(child.gameObject);

        string rootPath;

        if (uIHelperUtils.onMy)
        {
            rootPath = Application.persistentDataPath;

            yield return StartCoroutine(CreatePathFileButtons(content, rootPath));
        }

        if (uIHelperUtils.onLibrary)
        {
            rootPath = Application.streamingAssetsPath;

            yield return StartCoroutine(CreatePathFileButtons(content, rootPath));
        }

        UIHelperUtils.SetSizeScrollView(panelFile);
    }

    public IEnumerator CreatePathFileButtons(Transform content, string rootPath)
    {

        // Caminho base
        if (selectBasePath == fileManager.basePath_PieceData)
        {
            string caminhoBase = Path.Combine(rootPath, fileManager.basePath_PieceData);

            if (!Directory.Exists(caminhoBase))
            {
                Debug.LogWarning("Pasta base não encontrada: " + caminhoBase);
                yield break;
            }

            string[] arquivos = Directory.GetFiles(caminhoBase, "*.json", SearchOption.AllDirectories);

            foreach (string arquivo in arquivos)
            {
                string pasta = Path.GetFileName(Path.GetDirectoryName(arquivo));
                string json = File.ReadAllText(arquivo); // ainda síncrono
                PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

                if (wrapper == null || wrapper.piece == null)
                {
                    Debug.LogWarning("JSON inválido: " + arquivo);
                    continue;
                }

                PieceInfo piece = wrapper.piece;

                GameObject newButton = Instantiate(fileButtonPrefab, content);
                //Transform newButton = painelNewButton.transform.GetChild(0);


                string caminhoSprite;

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
                    Debug.LogWarning("Sprite não encontrado: " + caminhoSprite);

                    caminhoSprite = Path.Combine(Application.streamingAssetsPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art.Trim() + ".png");

                    if (!File.Exists(caminhoSprite))
                    {
                        Debug.LogWarning("Sprite não encontrado: " + caminhoSprite);
                        //    continue;
                    }
                }

                Sprite sprite = UIHelperUtils.GetSpriteFromPath(caminhoSprite);

                // Aplica no botão
                Image imgComp = newButton.GetComponent<Image>();
                if (imgComp != null)
                    imgComp.sprite = sprite;

                TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                    textComp.text = Path.GetFileNameWithoutExtension(piece.Name);

                string jsonPathCopy = arquivo; // evita closure
                string pastaCopy = pasta;
                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(jsonPathCopy);
                    if (manageCreate)
                        manageCreate.OnFileClick(newButton, fileName, jsonPathCopy, pastaCopy, sprite, rootPath);
                    else if (manageSquad)
                        manageSquad.OnClickFile(jsonPathCopy, rootPath);

                });

                // Espera 1 frame antes de continuar (alivia a UI)
                yield return null;
            }
        }


        if (selectBasePath == fileManager.basePath_Sprite)
        {
            string pathBase = Path.Combine(rootPath, fileManager.basePath_Sprite);

            if (!Directory.Exists(pathBase))
            {
                Debug.LogWarning("Pasta base não encontrada: " + pathBase);
                yield break;
            }

            string[] files = Directory.GetFiles(pathBase, "*.png", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                GameObject newButton = Instantiate(fileButtonPrefab, content);
                //Transform newButton = painelNewButton.transform.GetChild(0);

                Sprite sprite = UIHelperUtils.GetSpriteFromPath(file);

                Image imgComp = newButton.GetComponent<Image>();
                if (imgComp != null)
                    imgComp.sprite = sprite;

                TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                    textComp.text = Path.GetFileNameWithoutExtension(file);

                string fileCopy = file; // evita closure
                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    string nameFolder = Path.GetFileName(Path.GetDirectoryName(fileCopy));
                    string fileName = Path.GetFileNameWithoutExtension(fileCopy);
                    deleteObj.SetActive(false);

                    if (manageCreate)
                        manageCreate.HandleSelectionArt(Path.GetFileNameWithoutExtension(fileCopy), nameFolder, sprite,rootPath);
                    else if (managePainting)
                        managePainting.OnFileClick(newButton, fileName, nameFolder, rootPath);
                });

                yield return null;
            }
        }

    }

    private IEnumerator CreateFileButtons(string folder, string rootPath)
    {
        Transform content = panelFile.transform.Find("Scroll View/Viewport/Content");
        if (content == null)
        {
            Debug.LogError("Não foi possível encontrar o Content do ScrollView!");
            yield break;
        }

        string caminhoCompleto = Path.Combine(rootPath, selectBasePath, folder);
        if (!Directory.Exists(caminhoCompleto))
        {
            Debug.LogWarning("Pasta não encontrada: " + caminhoCompleto);
            yield break;
        }

        if (selectBasePath == fileManager.basePath_PieceData)
        {
            string[] arquivosJson = Directory.GetFiles(caminhoCompleto, "*.json", SearchOption.TopDirectoryOnly);

            foreach (string jsonPath in arquivosJson)
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

                    if (wrapper == null || wrapper.piece == null)
                    {
                        Debug.LogWarning("JSON inválido: " + jsonPath);
                        continue;
                    }

                    PieceInfo piece = wrapper.piece;

                    string caminhoSprite = piece.NativeSprite
                        ? Path.Combine(Application.streamingAssetsPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art.Trim() + ".png")
                        : Path.Combine(rootPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art.Trim() + ".png");

                    if (!File.Exists(caminhoSprite))
                    {
                        Debug.LogWarning("Sprite não encontrado: " + caminhoSprite);
                        //continue;
                    }

                    GameObject newButton = Instantiate(fileButtonPrefab, content);
                    //Transform newButton = painelNewButton.transform.GetChild(0);

                    TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComp != null)
                        textComp.text = Path.GetFileNameWithoutExtension(piece.Name);

                    Sprite sprite = UIHelperUtils.GetSpriteFromPath(caminhoSprite);

                    Image imgComp = newButton.GetComponent<Image>();
                    if (imgComp != null)
                        imgComp.sprite = sprite;

                    newButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(jsonPath);
                        if (manageCreate)
                            manageCreate.OnFileClick(newButton, fileName, jsonPath, folder, sprite, rootPath, piece);
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Erro ao carregar JSON: {jsonPath} -> {e.Message}");
                }

                // 🔹 espera 1 frame antes de continuar
                yield return null;
            }
        }
        else if (selectBasePath == fileManager.basePath_Sprite)
        {
            string pathSprites = Path.Combine(rootPath, fileManager.basePath_Sprite, folder);
            string pathJsons = Path.Combine(rootPath, fileManager.basePath_PaintingData, folder);

            List<SpriteData> sprites = uIHelperUtils.LoadJsonSpritesFromPath(pathJsons, pathSprites);

            foreach (var spriteData in sprites)
            {
                GameObject newButton = Instantiate(fileButtonPrefab, content);
                //Transform newButton = painelNewButton.transform.GetChild(0);

                Image img = newButton.GetComponent<Image>();
                img.sprite = spriteData.Sprite;
                img.rectTransform.sizeDelta = new Vector2(90, 90);

                TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                    textComp.text = spriteData.Name;

                string jsonCopy = spriteData.JsonPath;
                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    string pastaCopy = Path.GetFileName(Path.GetDirectoryName(jsonCopy));
                    string fileName = Path.GetFileNameWithoutExtension(jsonCopy);

                    if (managePainting)
                        managePainting.OnFileClick(newButton, fileName, pastaCopy, rootPath);
                });

                yield return null;
            }

            yield return null;

        }
    }























}
