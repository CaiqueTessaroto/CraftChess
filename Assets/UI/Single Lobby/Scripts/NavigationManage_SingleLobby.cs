using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NavigationManage_SingleLobby : MonoBehaviour
{
    public UIHelperUtils uIHelperUtils;
    public FileManager fileManager;

    [Header("Scripts")]
    public InteractiveLobby interactiveLobby;

    [Header("Options")]
    public Button allBtw;
    public Button myBtw;
    public Button libraryBtw;

    [Header("Panel")]
    public GameObject panelSquad;
    public Button panelbackBtn;
    public GameObject squad_BtnPrefab;

    [Header("Control")]
    public bool initiate = false;
    // Start is called before the first frame update
    void Start()
    {

        allBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setAll())
                StartFormationsButtons();

        });
        myBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setMy())
                StartFormationsButtons();

        });
        libraryBtw.onClick.AddListener(() =>
        {
            if (initiate) return;
            if (uIHelperUtils.setLibrary())
                StartFormationsButtons();

        });

        panelbackBtn.onClick.AddListener(() =>
        {
            uIHelperUtils.ResetAllControlBooleans();

            panelSquad.SetActive(false);
        });


        uIHelperUtils.setAll();

    }

    // Update is called once per frame
    void Update()
    {

    }


    //Navegação e seleção -------
    public void StartFormationsButtons()
    {
        initiate = true;

        panelSquad.SetActive(true);

        Transform content = panelSquad.transform.Find("Scroll View/Viewport/Content");

        if (content == null)
        {
            Debug.LogError("Não foi possível encontrar o Content do ScrollView!");
            return;
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }


        // Carrega pastas do persistentDataPath se estiver no "onMy"
        if (uIHelperUtils.onMy)
        {
            // Espera terminar a criação antes de continuar
            CreateFormationsButtons(Application.persistentDataPath, content);
        }

        // Carrega pastas do streamingAssetsPath se estiver no "onLibrary"
        if (uIHelperUtils.onLibrary)
        {
            CreateFormationsButtons(Application.streamingAssetsPath, content);
        }

        // Ajusta tamanho do ScrollView
        UIHelperUtils.SetSizeScrollView(panelSquad);


        initiate = false;

    }


    public void CreateFormationsButtons(string rootPath, Transform content = null)
    {
        if (content == null)
            content = panelSquad.transform.Find("Scroll View/Viewport/Content");

        if (content == null)
        {
            Debug.LogError("Não foi possível encontrar o Content do ScrollView!");
            return;
        }

        string squadsRoot = Path.Combine(rootPath, fileManager.basePath_SquadData);

        if (!Directory.Exists(squadsRoot))
        {
            Debug.LogWarning("Pasta não encontrada: " + squadsRoot);
            return;
        }

        // 🔹 Itera todas as pastas dentro de SquadData
        foreach (string squadFolder in Directory.GetDirectories(squadsRoot))
        {

            string folderName = Path.GetFileName(squadFolder);

            string pngFile = Path.Combine(squadFolder, folderName + ".png");
            string jsonFile = Path.Combine(squadFolder, folderName + ".json");

            if (!File.Exists(pngFile) && !File.Exists(jsonFile))
            {
                Debug.LogWarning("Faltando arquivos no squad: " + folderName);
                continue;
            }

            string squadName = Path.GetFileNameWithoutExtension(jsonFile);

            // Instancia o botão
            GameObject newButton = Instantiate(squad_BtnPrefab, content);

            // Nome no botão
            TMP_Text textComponent = newButton.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.text = squadName;

            // Imagem
            Image imageComponent = newButton.GetComponentInChildren<Image>();
            if (imageComponent != null)
            {
                byte[] bytes = File.ReadAllBytes(pngFile);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);

                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                              new Vector2(0.5f, 0.5f));
                imageComponent.sprite = sprite;
            }

            //Sprite spriteSquad = UIHelperUtils.GetSpriteFromPathForLobby(pngFile);

            // Clique do botão
            Button buttonComponent = newButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() =>
                {
                    //OnButtonClicked(folderName, newButton, piecesPanel, squadName, rootPath, jsonFile);
                    interactiveLobby.SelectSquad(folderName, jsonFile);
                });
            }
        }
    }

}
