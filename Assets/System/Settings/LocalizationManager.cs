using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocalizationFile
{
    public LocalizationEntry[] entries;
}

[System.Serializable]
public class LocalizationEntry
{
    public string key;
    public string value;
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private Dictionary<string, string> localizedTexts = new();
    private string currentLanguageCode;

    void Start()
    {
        ApplyLanguage(SettingsManager.Instance.Settings.language);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===============================
    // Carrega o idioma
    // ===============================
    public void LoadLanguage(string languageCode)
    {
        currentLanguageCode = languageCode;
        localizedTexts.Clear();

        TextAsset jsonFile = Resources.Load<TextAsset>($"Localization/{languageCode}");

        if (jsonFile == null)
        {
            Debug.LogError($"Arquivo de idioma não encontrado: {languageCode}");
            return;
        }

        LocalizationFile data = JsonUtility.FromJson<LocalizationFile>(jsonFile.text);

        foreach (var entry in data.entries)
        {
            localizedTexts[entry.key] = entry.value;
        }

        Debug.Log($"Idioma carregado: {languageCode} ({localizedTexts.Count} textos)");
    }

    // ===============================
    // Retorna texto traduzido
    // ===============================
    public string Get(string key)
    {
        if (localizedTexts.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"Chave de tradução não encontrada: {key}");
        return null;
    }

    public void ApplyLanguage(Language lang)
    {
        SettingsManager.Instance.Settings.language = lang;

        string code = LanguageHelper.ToCode(lang);
        LoadLanguage(code);

        foreach (var txt in FindObjectsOfType<ThemeText>(true))
            txt.UpdateText();


        TutorialController tutorial = FindObjectOfType<TutorialController>();
        if (tutorial != null)
            tutorial.ShowPage(0);

        SettingsManager.Instance.Save();
    }

    public static Language DetectSystemLanguage()
    {
        return Application.systemLanguage switch
        {
            SystemLanguage.Portuguese => Language.PortugueseBR,
            SystemLanguage.English => Language.EnglishUS,
            SystemLanguage.Spanish => Language.SpanishES,

            // fallback seguro
            _ => Language.EnglishUS
        };
    }

    public string CurrentLanguage => currentLanguageCode;
}
