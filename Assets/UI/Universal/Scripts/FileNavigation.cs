using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System;
using System.Collections;
using System.Linq;

public class FileNavigation : MonoBehaviour
{

    public FileManager fileManager;
    public UIHelperUtils uIHelperUtils;
    public FolderNavigation folderNavigation;

    [Header("Managers")]
    public NavigationManage_Create manageCreate;
    public NavigationManage_Painting managePainting;
    public NavigationManage_Squad manageSquad;
    public ProfileImageManager profileImageManager;

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
    public int count = 0;
    private int fileLimit = 72;



    private bool setCursor = false;



    //private string selectRootPath;

    // Start is called before the first frame update
    void Start()
    {


        if (manageCreate == null)
            manageCreate = FindFirstObjectByType<NavigationManage_Create>();
    
        if (managePainting == null)
            managePainting = FindFirstObjectByType<NavigationManage_Painting>();
        
        if (manageSquad == null)
            manageSquad = FindFirstObjectByType<NavigationManage_Squad>();

        if (profileImageManager == null)
            profileImageManager = FindFirstObjectByType<ProfileImageManager>();

        allBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setAll())
                StartCoroutine(UpdateFilesButtons());

        });
        myBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setMy())
                StartCoroutine(UpdateFilesButtons());

        });
        libraryBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setLibrary())
                StartCoroutine(UpdateFilesButtons());

        });


        deleteBtw = deleteObj.GetComponent<Button>();


        deleteBtw.onClick.AddListener(() =>
        {
            uIHelperUtils.delete = !uIHelperUtils.delete;
            setCursor = true;

            UIHelperUtils.SetCursor(uIHelperUtils.TrashIcon, CursorHotspot.Center);

        });


        backBtw.onClick.AddListener(() =>
        {

            if (uIHelperUtils.back)
            {
                panelFile.SetActive(false);

                if (uIHelperUtils.change)
                {
                    //    folderNavigation.StartCreatingFolderButtons(selectBasePath, folderNavigation.panelFolders);
                }

                //folderNavigation.RefreshFolderButton(folderNavigation.currentButtonFolder.name);

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
            if (initiate) return;

            uIHelperUtils.OnFolder = true;
            uIHelperUtils.OnFiles = false;

            uIHelperUtils.setAll();

            panelFile.SetActive(false);

            folderNavigation.panelFolders.SetActive(true);

            StartCoroutine(folderNavigation.UpdateFolderButtons(selectBasePath));

            /*
            if (uIHelperUtils.change)
            {
                folderNavigation.UpdateFolderButtons();
                uIHelperUtils.change = false;
            }
            */
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

    public void StartCreatingFileButtons(string folder, string rootPath, string basePath)
    {
        selectBasePath = basePath;

        if (rootPath == Application.streamingAssetsPath)
            deleteObj.SetActive(false);
        else
            deleteObj.SetActive(true);

        Transform content = panelFile.transform.Find("Scroll View/Viewport/Content");

        foreach (Transform child in content)
            Destroy(child.gameObject);

        count = 0;
        StartCoroutine(CreateFileButtons(folder, rootPath));
    }

    public IEnumerator UpdateFilesButtons()
    {
        initiate = true;

        try
        {
            Transform content = panelFile.transform.Find("Scroll View/Viewport/Content");

            // Remove botões antigos
            foreach (Transform child in content)
                Destroy(child.gameObject);

            string rootPath;

            count = 0;

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
        finally
        {
            initiate = false;
        }

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

            var arquivosOrdenados = arquivos
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => f.FullName)
                    .ToArray();

            foreach (string arquivo in arquivosOrdenados)
            {
                if (count >= fileLimit) break;
                count++;
                string pasta = Path.GetFileName(Path.GetDirectoryName(arquivo));
                string json = File.ReadAllText(arquivo); // ainda síncrono
                PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

                if (wrapper == null || wrapper.piece == null)
                {
                    Debug.LogWarning("JSON inválido: " + arquivo);
                    fileManager.HandleDeleteFile(pasta, arquivo, null);
                    continue;
                }

                PieceInfo piece = wrapper.piece;

                GameObject newButton = Instantiate(fileButtonPrefab, content);
                //Transform newButton = painelNewButton.transform.GetChild(0);

                bool translate = UIHelperUtils.CheckTranslationFile(rootPath, selectBasePath, piece.Squad);


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


                string name = piece.Name;

                if (translate)
                {
                    Debug.Log("translate: " + translate);
                    name = UIHelperUtils.T(piece.Name);
                    if (string.IsNullOrEmpty(name))
                        name = piece.Name;
                }

                TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    if (LocalizationManager.Instance)
                        textComp.font = LocalizationManager.Instance.currentFont;
                    textComp.text = name;
                }

                string jsonPathCopy = arquivo; // evita closure
                string pastaCopy = pasta;
                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(jsonPathCopy);
                    if (manageCreate)
                        manageCreate.OnFileClick(newButton, fileName, jsonPathCopy, pastaCopy, sprite, rootPath);
                    else if (manageSquad)
                        manageSquad.OnClickFile(jsonPathCopy, rootPath, pastaCopy);

                    panelFile.SetActive(false);

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

            var orderedfiles = files
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => f.FullName)
                    .ToArray();


            foreach (string file in orderedfiles)
            {
                if (count >= fileLimit) break;
                count++;
                GameObject newButton = Instantiate(fileButtonPrefab, content);
                //Transform newButton = painelNewButton.transform.GetChild(0);

                Sprite sprite = UIHelperUtils.GetSpriteFromPath(file);

                string pasta = Path.GetDirectoryName(file);
                bool translate = UIHelperUtils.CheckTranslationFile(rootPath, selectBasePath, pasta);

                Image imgComp = newButton.GetComponent<Image>();
                if (imgComp != null)
                    imgComp.sprite = sprite;


                string name = Path.GetFileNameWithoutExtension(file);

                if (translate)
                {
                    name = UIHelperUtils.T(name);
                    if (string.IsNullOrEmpty(name))
                        name = Path.GetFileNameWithoutExtension(file);
                }

                TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    if (LocalizationManager.Instance)
                        textComp.font = LocalizationManager.Instance.currentFont;
                    textComp.text = name;
                }

                Sprite spriteCopy = sprite;
                string fileCopy = file; // evita closure

                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    string nameFolder = Path.GetFileName(Path.GetDirectoryName(fileCopy));
                    string fileName = Path.GetFileNameWithoutExtension(fileCopy);
                    //deleteObj.SetActive(false);

                    if (manageCreate)
                        manageCreate.HandleSelectionArt(Path.GetFileNameWithoutExtension(fileCopy), nameFolder, spriteCopy, rootPath);
                    else if (managePainting)
                        managePainting.OnFileClick(newButton, fileName, nameFolder, rootPath);
                    else if (profileImageManager)
                        profileImageManager.OnImageSelected(spriteCopy.texture);

                    panelFile.SetActive(false);
                });

                yield return null;
            }
        }

        yield return new WaitForEndOfFrame();

        //initiate = false;
    }

    private IEnumerator CreateFileButtons(string folder, string rootPath)
    {

        int count = 0;

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

        bool translate = UIHelperUtils.CheckTranslationFile(rootPath, selectBasePath, folder);

        if (selectBasePath == fileManager.basePath_PieceData)
        {
            string[] arquivosJson = Directory.GetFiles(caminhoCompleto, "*.json", SearchOption.TopDirectoryOnly);

            foreach (string jsonPath in arquivosJson)
            {
                if (count >= 36) break;
                count++;
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

                    if (wrapper == null || wrapper.piece == null)
                    {
                        fileManager.HandleDeleteFile(jsonPath, jsonPath, null);
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

                    string name = piece.Name;

                    if (translate)
                    {
                        name = UIHelperUtils.T(piece.Name);
                        if (string.IsNullOrEmpty(name))
                            name = piece.Name;
                    }

                    TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        if (LocalizationManager.Instance)
                            textComp.font = LocalizationManager.Instance.currentFont;
                        textComp.text = name;
                    }

                    Sprite sprite = UIHelperUtils.GetSpriteFromPath(caminhoSprite);

                    Image imgComp = newButton.GetComponent<Image>();
                    if (imgComp != null)
                        imgComp.sprite = sprite;

                    newButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(jsonPath);
                        if (manageCreate)
                            manageCreate.OnFileClick(newButton, fileName, jsonPath, folder, sprite, rootPath, piece);

                        panelFile.SetActive(false);
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
            //string pathJsons = Path.Combine(rootPath, fileManager.basePath_PaintingData, folder);

            List<SpriteData> sprites = new List<SpriteData>();

            yield return StartCoroutine(
                uIHelperUtils.LoadSpritesFromPathCoroutine(
                    pathSprites,
                    sprites
                )
            );
            foreach (var spriteData in sprites)
            {
                if (count >= 36) break;
                count++;
                GameObject newButton = Instantiate(fileButtonPrefab, content);
                //Transform newButton = painelNewButton.transform.GetChild(0);

                Image img = newButton.GetComponent<Image>();
                img.sprite = spriteData.Sprite;
                img.rectTransform.sizeDelta = new Vector2(90, 90);


                string name = spriteData.Name;

                if (translate)
                {
                    name = UIHelperUtils.T(spriteData.Name);
                    if (string.IsNullOrEmpty(name))
                        name = spriteData.Name;
                }

                TextMeshProUGUI textComp = newButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    if (LocalizationManager.Instance)
                        textComp.font = LocalizationManager.Instance.currentFont;
                    textComp.text = name;
                }

                Sprite spriteCopy = spriteData.Sprite;
                string pathCopy = spriteData.PngPath;

                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    string pastaCopy = Path.GetFileName(Path.GetDirectoryName(pathCopy));
                    string fileName = Path.GetFileNameWithoutExtension(pathCopy);


                    if (manageCreate)
                        manageCreate.HandleSelectionArt(fileName, pastaCopy, spriteCopy, rootPath);
                    else if (managePainting)
                        managePainting.OnFileClick(newButton, fileName, pastaCopy, rootPath);
                    else if (profileImageManager)
                        profileImageManager.OnImageSelected(spriteCopy.texture);

                    panelFile.SetActive(false);

                });

                yield return null;
            }

            yield return null;

        }
    }























}
