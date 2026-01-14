using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ColorPickerManager : MonoBehaviour
{

    public PaintingGridManager gridManager;
    public DynamicColorPaletteManager paletteManager;
    public Image colorPreview;
    public Color currentColor = Color.white;

    [Header("Picker Panel")]
    public GameObject panel;

    [Header("Buttons")]
    public Button confirmBtn;
    public Button cancelBtn;

    [Header("Buttons Panels")]
    public Button buttonRGB;
    public Button buttonHEX;
    public Button buttonSCP;

    [Header("Panels")]
    public GameObject panelRGB;
    public GameObject panelHEX;
    public GameObject panelSCP;

    [Header("New Color HEX:")]
    public TMP_InputField inputHex;

    [Header("New Color RGB:")]
    public Slider sliderR;
    public Slider sliderG;
    public Slider sliderB;

    public TextMeshProUGUI textR, textG, textB;

    public int decimalPlaces = 2;

    // Start is called before the first frame update
    void Start()
    {

        if (paletteManager == null)
            paletteManager = FindObjectOfType<DynamicColorPaletteManager>();

        if (gridManager == null)
            gridManager = FindObjectOfType<PaintingGridManager>();


        buttonRGB.onClick.AddListener(() =>
        {
            SelectPanel(panelRGB);
        });
        buttonHEX.onClick.AddListener(() =>
        {
            SelectPanel(panelHEX);
        });
        buttonSCP.onClick.AddListener(() =>
        {
            SelectPanel(panelSCP);
        });






        confirmBtn.onClick.AddListener(() =>
        {
            gridManager.SetSelectedColor(currentColor);
            panel.SetActive(false);
        });

        cancelBtn.onClick.AddListener(() => panel.SetActive(false));




        sliderR.onValueChanged.AddListener(delegate { OnSliderChanged(); });
        sliderG.onValueChanged.AddListener(delegate { OnSliderChanged(); });
        sliderB.onValueChanged.AddListener(delegate { OnSliderChanged(); });


        inputHex.characterLimit = 8;
        inputHex.text = inputHex.text.ToUpper();
        inputHex.onValueChanged.AddListener(delegate { OnHexInputChanged(); });

        OnSliderChanged();

    }

    private void SelectPanel(GameObject targetPanel)
    {
        // Desativa todos
        panelHEX.SetActive(false);
        panelRGB.SetActive(false);
        panelSCP.SetActive(false);

        // Ativa apenas o selecionado
        targetPanel.SetActive(true);
    }

    public void SetPreviewColor(Color color)
    {
        currentColor = color;
        colorPreview.color = color;
    }


    public void OnHexInputChanged()
    {
        string hex = inputHex.text;

        // garante que começa com #
        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            UpdateSlidersFromColor(color);
            SetPreviewColor(color);
        }
    }


    public void UpdateSlidersFromColor(Color color)
    {
        // evita loop de eventos se necessário
        sliderR.SetValueWithoutNotify(color.r);
        sliderG.SetValueWithoutNotify(color.g);
        sliderB.SetValueWithoutNotify(color.b);

        textR.text = color.r.ToString($"F{decimalPlaces}");
        textG.text = color.g.ToString($"F{decimalPlaces}");
        textB.text = color.b.ToString($"F{decimalPlaces}");
    }

    public void OnSliderChanged()
    {
        Color newColor = new Color(sliderR.value, sliderG.value, sliderB.value);

        textR.text = sliderR.value.ToString($"F{decimalPlaces}");
        textG.text = sliderG.value.ToString($"F{decimalPlaces}");
        textB.text = sliderB.value.ToString($"F{decimalPlaces}");

        UpdateHexFromColor(newColor);
        SetPreviewColor(newColor);
    }


    public void UpdateHexFromColor(Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGB(color);
        inputHex.SetTextWithoutNotify("#" + hex);
    }




}
