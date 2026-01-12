using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System.Collections.Generic;


public class DynamicColorPaletteManager : MonoBehaviour
{
    public FileManager fileManager;
    public PaintingGridManager gridManager;
    public GameObject colorButtonPrefab;
    public Transform paletteContainer;
    public Image colorPreview; // Mostra a cor antes de adicionar
    private Color currentColor = Color.black;
    public GameObject panel;
    public GameObject selectedButtonColor;

    [Header("Buttons:")]
    public Button confirm;
    public Button cancel;
    public Button Add;
    public Button removeColor;
    public Button savePalletsButton;

    [Header("Control Pallets:")]
    public Button buttonPallet1;
    public Button buttonPallet2;
    public Button buttonPallet3;
    public Button buttonPallet4;
    public Button buttonPallet5;
    public Button buttonPallet6;
    public Button buttonPallet7;
    public Button buttonPallet8;
    public GameObject panelPallet1;
    public GameObject panelPallet2;
    public GameObject panelPallet3;
    public GameObject panelPallet4;
    public GameObject panelPallet5;
    public GameObject panelPallet6;
    public GameObject panelPallet7;
    public GameObject panelPallet8;
    //public List<GameObject> panelPallets;
    public GameObject selectPanelPallet;



    void Start()
    {

        savePalletsButton.onClick.AddListener(() =>
        {
            SavePalettes("Pallet2.json", panelPallet2.transform, "Pallets");
            SavePalettes("Pallet3.json", panelPallet3.transform, "Pallets");
            SavePalettes("Pallet4.json", panelPallet4.transform, "Pallets");

            SavePalettes("Pallet5.json", panelPallet5.transform, "Pallets");
            SavePalettes("Pallet6.json", panelPallet6.transform, "Pallets");
            SavePalettes("Pallet7.json", panelPallet7.transform, "Pallets");
            SavePalettes("Pallet8.json", panelPallet8.transform, "Pallets");
        });

        LoadPalette("Pallet2.json", panelPallet2.transform, "Pallets", colorButtonPrefab);
        LoadPalette("Pallet3.json", panelPallet3.transform, "Pallets", colorButtonPrefab);
        LoadPalette("Pallet4.json", panelPallet4.transform, "Pallets", colorButtonPrefab);

        LoadPalette("Pallet5.json", panelPallet5.transform, "Pallets", colorButtonPrefab);
        LoadPalette("Pallet6.json", panelPallet6.transform, "Pallets", colorButtonPrefab);
        LoadPalette("Pallet7.json", panelPallet7.transform, "Pallets", colorButtonPrefab);
        LoadPalette("Pallet8.json", panelPallet8.transform, "Pallets", colorButtonPrefab);
        // Conectar eventos dos botões
        buttonPallet1.onClick.AddListener(() =>
        {
            SelectPanel(panelPallet1);
        });
        buttonPallet2.onClick.AddListener(() =>
        {
            SelectPanel(panelPallet2);
        });
        buttonPallet3.onClick.AddListener(() =>
        {
            SelectPanel(panelPallet3);
        });
        buttonPallet4.onClick.AddListener(() =>
        {
            SelectPanel(panelPallet4);
        });

        buttonPallet5.onClick.AddListener(() =>
        {
            SelectPanel(panelPallet5);
        });

        buttonPallet6.onClick.AddListener(() =>
        {
            SelectPanel(panelPallet6);
        });

        buttonPallet7.onClick.AddListener(() =>
        {
            SelectPanel(panelPallet7);
        });

        buttonPallet8.onClick.AddListener(() =>
        {
            SelectPanel(panelPallet8);
        });

        // Define painel padrão ao iniciar (opcional)
        SelectPanel(panelPallet1);

        removeColor.onClick.AddListener(RemoveSelectedColor);


        if (confirm != null)
        {
            confirm.onClick.AddListener(() =>
            {
                gridManager.SetSelectedColor(currentColor);
                panel.SetActive(false);
            });
        }

        if (Add != null)
        {
            Add.onClick.AddListener(() => AddColorToPalette());
        }

        if (confirm != null)
        {
            cancel.onClick.AddListener(() => panel.SetActive(false));
        }



        sliderR.onValueChanged.AddListener(delegate { OnSliderChanged(); });
        sliderG.onValueChanged.AddListener(delegate { OnSliderChanged(); });
        sliderB.onValueChanged.AddListener(delegate { OnSliderChanged(); });

        OnSliderChanged(); // Atualiza já no início
    }

    void RemoveSelectedColor()
    {
        if (gridManager.selectedButtonColor != null)
        {
            Destroy(gridManager.selectedButtonColor.gameObject);
            gridManager.selectedButtonColor = null;
        }
    }

    public void SetPreviewColor(Color color)
    {
        currentColor = color;
        colorPreview.color = color;
    }

    void SelectPanel(GameObject targetPanel)
    {
        // Desativa todos
        panelPallet1.SetActive(false);
        panelPallet2.SetActive(false);
        panelPallet3.SetActive(false);
        panelPallet4.SetActive(false);

        panelPallet5.SetActive(false);
        panelPallet6.SetActive(false);
        panelPallet7.SetActive(false);
        panelPallet8.SetActive(false);

        // Ativa apenas o selecionado
        targetPanel.SetActive(true);

        // Atualiza referência
        selectPanelPallet = targetPanel;
    }

    public void AddColorToPalette()
    {
        int childCount = selectPanelPallet.transform.childCount;

        if (childCount < 30)
        {
            //ColorPaletteUI
            GameObject newButton = Instantiate(colorButtonPrefab, selectPanelPallet.transform);
            Image buttonImage = newButton.GetComponent<Image>();
            buttonImage.color = gridManager.previewSelectColor.color;

            newButton.AddComponent<ColorPaletteUI>();

            Button button = newButton.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                gridManager.selectedColor = gridManager.previewSelectColor.color;

            });
        }
    }

    [Header("New Color Pallet:")]

    public Slider sliderR;
    public Slider sliderG;
    public Slider sliderB;

    public TextMeshProUGUI textR, textG, textB;

    public int decimalPlaces = 2;


    public void OnSliderChanged()
    {
        Color newColor = new Color(sliderR.value, sliderG.value, sliderB.value);

        textR.text = sliderR.value.ToString($"F{decimalPlaces}");
        textG.text = sliderG.value.ToString($"F{decimalPlaces}");
        textB.text = sliderB.value.ToString($"F{decimalPlaces}");

        SetPreviewColor(newColor);
    }


    public void SavePalettes(string fileName, Transform panelPallet, string subfolderName = "Pallets")
    {
        Palette wrapper = new Palette();

        foreach (Transform child in panelPallet.transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                wrapper.palette.Add(new ColorData
                {
                    r = img.color.r,
                    g = img.color.g,
                    b = img.color.b,
                    a = img.color.a
                });
            }
        }

        // Serializa
        string data = JsonUtility.ToJson(wrapper, true);
        fileManager.SaveJson(subfolderName, fileName, data, fileManager.basePath_UserData);

        Debug.Log("Paleta salva em " + fileName);
    }


    public void LoadPalette(string fileName, Transform panelPallet, string subfolderName, GameObject colorButtonPrefab)
    {
        string json = fileManager.LoadJson(Application.persistentDataPath, fileManager.basePath_UserData, subfolderName, fileName);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("Arquivo de paletas não encontrado: " + fileName);
            return;
        }

        Palette wrapper = JsonUtility.FromJson<Palette>(json);

        List<ColorData> colorsToLoad;
        colorsToLoad = wrapper.palette;

        for (int i = 0; i < colorsToLoad.Count; i++)
        {
            var c = colorsToLoad[i];
            // Limita a quantidade de botões adicionais
            if (panelPallet.childCount < 30)
            {
                GameObject newButton = Instantiate(colorButtonPrefab, panelPallet);
                Image buttonImage = newButton.GetComponent<Image>();
                buttonImage.color = new Color(c.r, c.g, c.b, c.a);

                newButton.AddComponent<ColorPaletteUI>();

                Button button = newButton.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    gridManager.selectedColor = new Color(c.r, c.g, c.b, c.a);
                });
            }
        }

        Debug.Log("Paletas carregadas de " + fileName);
    }

}
