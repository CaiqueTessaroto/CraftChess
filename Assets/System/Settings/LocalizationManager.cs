using System.Collections.Generic;
using TMPro;
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

    private TextAsset loadedLanguageFile;
    
    [Header("Fontes por Idioma")]
    public TMP_FontAsset defaultFont; // LiberationSans ou similar

    [Header("Fontes Específicas")]
    public TMP_FontAsset japaneseFont;
    public TMP_FontAsset koreanFont;
    public TMP_FontAsset russianFont;
    public TMP_FontAsset chineseFont;
    public TMP_FontAsset hindiFont;
    public TMP_FontAsset arabicFont;

    public static LocalizationManager Instance { get; private set; }

    private Dictionary<string, string> localizedTexts = new();
    public string currentLanguageCode;

    public TMP_FontAsset currentFont;

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

        if (loadedLanguageFile != null)
            Resources.UnloadAsset(loadedLanguageFile);

        loadedLanguageFile = Resources.Load<TextAsset>($"Localization/{languageCode}");

        if (loadedLanguageFile == null)
        {
            Debug.LogError($"Arquivo de idioma não encontrado: {languageCode}");
            return;
        }

        LocalizationFile data = JsonUtility.FromJson<LocalizationFile>(loadedLanguageFile.text);

        foreach (var entry in data.entries)
            localizedTexts[entry.key] = entry.value;

        //Debug.Log($"Idioma carregado: {languageCode} ({localizedTexts.Count} textos)");
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

        //Debug.Log("ApplyLanguage");
        SettingsManager.Instance.Settings.language = lang;

        string code = LanguageHelper.ToCode(lang);
        LoadLanguage(code);
        //TMP_FontAsset font = GetFontForLanguage(lang);

        ApplyFontToAllTMP(lang);

        foreach (var txt in FindObjectsOfType<ThemeText>(true))
            txt.UpdateText();


        TutorialController tutorial = FindObjectOfType<TutorialController>();
        if (tutorial != null)
            tutorial.ShowPage(0);

        RewardFeed rewardFeed = FindObjectOfType<RewardFeed>();
        if (rewardFeed != null)
            rewardFeed.ShowReward(rewardFeed.currentIndex);

        SettingsManager.Instance.Save();
    }


    public void ApplyFontToAllTMP(Language lang)
    {
        TMP_FontAsset font = GetFontForLanguage(lang);

        if (font == null) return;

        currentFont = font;

        // Textos normais
        foreach (var tmp in FindObjectsOfType<TextMeshProUGUI>(true))
            tmp.font = font;

        // Textos 3D
        foreach (var tmp in FindObjectsOfType<TextMeshPro>(true))
            tmp.font = font;

        // TMP InputFields
        foreach (var input in FindObjectsOfType<TMP_InputField>(true))
        {
            if (input.textComponent != null)
                input.textComponent.font = font;

            if (input.placeholder is TextMeshProUGUI placeholderTMP)
                placeholderTMP.font = font;
        }

        //Debug.Log($"[TMPFontApplier] Fonte aplicada em {tmpUI.Length + tmp3D.Length} TextMeshPro.");
    }

    public TMP_FontAsset GetFontForLanguage(Language lang)
    {
        switch (lang)
        {
            case Language.JapaneseJP:
                return japaneseFont;
            case Language.KoreanKR:
                return koreanFont;
            case Language.ChineseSP:
                return chineseFont;
            case Language.RussoRU:
                return russianFont;
            case Language.HindiIN:
                return hindiFont;
            case Language.ArabicAR:
                return arabicFont;
            default:
                return defaultFont; // LiberationSans
        }
    }

    public static Language DetectSystemLanguage()
    {
        return Application.systemLanguage switch
        {
            SystemLanguage.Portuguese => Language.PortugueseBR,
            SystemLanguage.English => Language.EnglishUS,
            SystemLanguage.Spanish => Language.SpanishES,
            SystemLanguage.Russian => Language.RussoRU,
            SystemLanguage.German => Language.GermanDE,
            SystemLanguage.French => Language.FrenchFR,
            SystemLanguage.Japanese => Language.JapaneseJP,
            SystemLanguage.Korean => Language.KoreanKR,
            SystemLanguage.Chinese => Language.ChineseSP,
            SystemLanguage.Hindi => Language.HindiIN,
            SystemLanguage.Arabic => Language.ArabicAR,

            // fallback seguro
            _ => Language.EnglishUS
        };
    }

    public string CurrentLanguage => currentLanguageCode;
}
