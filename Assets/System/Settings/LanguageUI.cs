using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Importante: Use UnityEngine.UI para Buttons no Unity

public enum Language
{
    EnglishUS = 0,
    ChineseSP = 1,
    HindiIN = 2,
    SpanishES = 3,
    ArabicAR = 4,
    PortugueseBR = 5,
    FrenchFR = 6,
    RussoRU = 7,
    JapaneseJP = 8,
    GermanDE = 9,
    KoreanKR = 10
}

public class LanguageUI : MonoBehaviour
{
    public TMP_Dropdown dropdown;


    [Header("Configuração de Painel")]
    public GameObject panelButtons; // Arraste o objeto pai aqui no Inspetor

    private List<Button> languageButtons = new List<Button>();

    void Awake()
    {
        if (panelButtons != null)
        {
            languageButtons = panelButtons.GetComponentsInChildren<Button>(false).ToList();
        }

        PopulateDropdown();
        SetupButtons();
    }

    void PopulateDropdown()
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();

        var options = Enum.GetValues(typeof(Language))
            .Cast<Language>()
            .Select(LanguageHelper.ToDisplayName)
            .ToList();

        dropdown.AddOptions(options);

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(OnValueChanged);

        dropdown.value = (int)SettingsManager.Instance.Settings.language;
        dropdown.RefreshShownValue();
    }


    void SetupButtons()
    {
        for (int i = 0; i < languageButtons.Count; i++)
        {
            int index = i; // Captura local para o closure do listener
            if (languageButtons[i] != null)
            {
                languageButtons[i].onClick.AddListener(() => OnValueChanged(index));
            }
        }
    }

    // Centraliza a lógica de troca de idioma
    void OnValueChanged(int index)
    {
        //SettingsManager.Instance.Settings.language = (Language)index;

        Language selectedLanguage = (Language)index;
        LocalizationManager.Instance.ApplyLanguage(selectedLanguage);

        // Opcional: Sincroniza o dropdown se o clique veio de um botão
        if (dropdown != null) dropdown.SetValueWithoutNotify(index);

        //ApplyFontForCurrentLanguage();

        //Debug.Log($"Idioma alterado para: {selectedLanguage}");
    }
}