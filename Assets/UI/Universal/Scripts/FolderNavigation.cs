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
    public string selectRootPath;
    private string selectBasePath;
    public bool initiate = false;



    void Start()
    {

        if (fileManager == null)
        {
            fileManager = FindObjectOfType<FileManager>();
        }
        if (fileNavigation == null)
        {
            fileNavigation = FindObjectOfType<FileNavigation>();
        }


        if (manageCreate == null)
        {
            manageCreate = FindObjectOfType<NavigationManage_Create>();
        }
        if (managePainting == null)
        {
            managePainting = FindObjectOfType<NavigationManage_Painting>();
        }
        if (manageSquad == null)
        {
            manageSquad = FindObjectOfType<NavigationManage_Squad>();

            if (manageSquad)
                piecesBtw.gameObject.SetActive(false);
        }


        allBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setAll())
                StartCoroutine(UpdateFolderButtons());

        });
        myBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setMy())
                StartCoroutine(UpdateFolderButtons());

        });
        libraryBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setLibrary())
                StartCoroutine(UpdateFolderButtons());

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
            uIHelperUtils.delete = true;
        });


        piecesBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            
            uIHelperUtils.OnFolder = false;
            uIHelperUtils.OnFiles = true;

            uIHelperUtils.setAll();

            if (managePainting)
                fileNavigation.selectBasePath = fileManager.basePath_Sprite;
            else
                fileNavigation.selectBasePath = fileManager.basePath_PieceData;

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

            fileManager.CreateInput("Criar Pasta", "Digite o nome...", (text) =>
            {
                CreateFolder(text);
            });

        });


    }


    private void OnClickFolder(string pasta, GameObject newButton, string rootPath)
    {
        fileNavigation.navigationOptions.SetActive(false);

        if (manageCreate)
            manageCreate.OnClickFolder(pasta, newButton, rootPath);
        else if (managePainting)
            managePainting.OnClickFolder(pasta, newButton, rootPath);
        else if (manageSquad)
            manageSquad.OnClickFolder(pasta, newButton, rootPath);

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

        }
        else
        {
            fileManager.CreateAdvice("A folder with this name already exists!");
        }

        StartCoroutine(UpdateFolderButtons());
    }

    public void StartCreatingFolderButtons(string basePath, GameObject panel)
    {
        uIHelperUtils.setAll();

        panelFolders = panel;
        selectBasePath = basePath;

        Transform content = panelFolders.transform.Find("Scroll View/Viewport/Content");

        foreach (Transform child in content)
            Destroy(child.gameObject);


        StartCoroutine(UpdateFolderButtons());
    }

    public IEnumerator UpdateFolderButtons()
    {
        initiate = true;

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

            // Carrega pastas do persistentDataPath se estiver no "onMy"
            if (uIHelperUtils.onMy)
            {
                pastas = fileManager.GetSubfoldersIn(selectBasePath, Application.persistentDataPath);
                // Espera terminar a criação antes de continuar
                yield return StartCoroutine(CreateFolderButtons(pastas, Application.persistentDataPath));
            }

            // Carrega pastas do streamingAssetsPath se estiver no "onLibrary"
            if (uIHelperUtils.onLibrary)
            {
                pastas = fileManager.GetSubfoldersIn(selectBasePath, Application.streamingAssetsPath);
                yield return StartCoroutine(CreateFolderButtons(pastas, Application.streamingAssetsPath));
            }

            // Ajusta tamanho do ScrollView

            UIHelperUtils.SetSizeScrollView(panelFolders);
        }
        finally
        {
            initiate = false;
        }

    }



    private IEnumerator CreateFolderButtons(List<string> pastas, string rootPath)
    {
        Transform content = panelFolders.transform.Find("Scroll View/Viewport/Content");

        foreach (string pasta in pastas)
        {
            // Instancia o prefab da pasta
            GameObject newButton = Instantiate(folderbuttonPrefab, content);

            // Define o nome da pasta no Text
            TextMeshProUGUI nomeTexto = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (nomeTexto != null)
                nomeTexto.text = pasta;

            Button button = newButton.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                OnClickFolder(pasta, newButton, rootPath);
            });

            // Carrega as imagens da pasta se for PieceData
            if (selectBasePath == fileManager.basePath_PieceData)
            {
                string caminhoPasta = Path.Combine(rootPath, selectBasePath, pasta);
                if (!Directory.Exists(caminhoPasta))
                {
                    Debug.LogWarning("Pasta não encontrada: " + caminhoPasta);
                    continue;
                }

                string[] arquivosJson = Directory.GetFiles(caminhoPasta, "*.json", SearchOption.TopDirectoryOnly);

                Transform panelImagens = newButton.transform.Find("Panel");

                foreach (string jsonPath in arquivosJson)
                {
                    try
                    {
                        string json = File.ReadAllText(jsonPath);
                        PieceWrapper wrapper = JsonUtility.FromJson<PieceWrapper>(json);

                        if (wrapper?.piece == null)
                        {
                            Debug.LogWarning("JSON inválido em: " + jsonPath);
                            continue;
                        }

                        PieceInfo piece = wrapper.piece;

                        string caminhoSprite = piece.NativeSprite
                            ? Path.Combine(Application.streamingAssetsPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art.Trim() + ".png")
                            : Path.Combine(rootPath, fileManager.basePath_Sprite, piece.FolderSprite, piece.Art.Trim() + ".png");

                        if (!File.Exists(caminhoSprite))
                        {
                            Debug.LogWarning("Sprite não encontrado: " + caminhoSprite);
                            //fileManager.CreateAdvice($"Sprite for piece {piece.Name} not found, add a sprite to use it in the game");
                            //continue;
                        }

                        // Cria objeto de imagem no painel
                        GameObject imgObj = new GameObject(piece.Name, typeof(Image));
                        imgObj.transform.SetParent(panelImagens, false);

                        Sprite sprite = UIHelperUtils.GetSpriteFromPath(caminhoSprite);

                        Image imgComp = imgObj.GetComponent<Image>();
                        if (imgComp != null)
                            imgComp.sprite = sprite;

                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Erro ao carregar JSON: " + jsonPath + " -> " + e.Message);
                    }

                    yield return null;
                }
            }
            else if (selectBasePath == fileManager.basePath_Sprite)
            {
                Transform panelImagens = newButton.transform.Find("Panel");
                if (panelImagens != null)
                {
                    string pathSprites = Path.Combine(rootPath, fileManager.basePath_Sprite, pasta);
                    string pathJsons = Path.Combine(rootPath, fileManager.basePath_PaintingData, pasta);

                    List<SpriteData> sprites = uIHelperUtils.LoadJsonSpritesFromPath(pathJsons, pathSprites);

                    foreach (var spriteData in sprites)
                    {
                        GameObject imgObj = new GameObject(spriteData.Name, typeof(Image));
                        imgObj.transform.SetParent(panelImagens, false);

                        Image img = imgObj.GetComponent<Image>();

                        if (img != null)
                            img.sprite = spriteData.Sprite;

                        yield return null;
                    }
                }

            }
            yield return null;
        }
    }

}


