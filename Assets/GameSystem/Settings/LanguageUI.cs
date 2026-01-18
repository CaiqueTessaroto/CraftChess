using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Importante: Use UnityEngine.UI para Buttons no Unity

public enum Language
{
    PortugueseBR = 0,
    EnglishUS = 1,
    SpanishES = 2
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

        var options = Enum.GetNames(typeof(Language)).ToList();
        // Se tiver o Helper, use: Enum.GetValues(typeof(Language)).Cast<Language>().Select(LanguageHelper.ToDisplayName).ToList();

        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(OnValueChanged);

        // Define o valor inicial baseado nas configurações salvas
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
        Language selectedLanguage = (Language)index;
        LocalizationManager.Instance.ApplyLanguage(selectedLanguage);

        // Opcional: Sincroniza o dropdown se o clique veio de um botão
        if (dropdown != null) dropdown.SetValueWithoutNotify(index);

        Debug.Log($"Idioma alterado para: {selectedLanguage}");
    }
}