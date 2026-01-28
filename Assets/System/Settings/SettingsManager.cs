using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GameSettings
{
    // 🎵 Áudio
    public float masterVolume = 0.5f;
    public float musicVolume = 0.5f;
    public float sfxVolume = 0.5f;

    // 🖥️ Gráficos
    public int resolutionIndex = 0;
    public bool fullscreen = true;
    public Language language = Language.PortugueseBR;
    //public int qualityLevel = 0;
}

public static class LanguageHelper
{
    public static string ToCode(Language lang)
    {
        return lang switch
        {
            Language.PortugueseBR => "pt-BR",
            Language.EnglishUS => "en-US",
            Language.SpanishES => "es-ES",
            Language.RussoRU => "ru-RU",
            Language.GermanDE => "de-De",
            Language.FrenchFR => "fr-FR",
            Language.JapaneseJP => "ja-JP",
            Language.KoreanKR => "ko-KR",
            Language.ChineseSP => "zh-CH",
            Language.HindiIN => "hi-IN",
            Language.ArabicAR => "ar-AR",
            _ => "pt-BR"
        };
    }

    public static string ToDisplayName(Language lang)
    {
        return lang switch
        {
            Language.PortugueseBR => "Português",
            Language.EnglishUS => "English",
            Language.SpanishES => "Español",
            Language.RussoRU => "Русский",
            Language.GermanDE => "Deutsch",
            Language.FrenchFR => "Français",
            Language.JapaneseJP => "日本語",
            Language.KoreanKR => "한국인",
            Language.ChineseSP => "中文",
            Language.HindiIN => "हिंदी",
            Language.ArabicAR => "عربي",
            _ => lang.ToString()
        };
    }
}


public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    public GameSettings Settings;

    public GameObject UIsettingsPrefab;
    public GameObject settingsContent;
    private GameObject settingsPanel;

    const string FIRST_RUN_KEY = "StreamingAssetsCopied";


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            //#if UNITY_ANDROID && !UNITY_EDITOR
            //if (!PlayerPrefs.HasKey("StreamingAssetsCopied"))
            //{
            StartCoroutine(CopyInitialNativeData());
            //}
            //#endif

            Load();

            if (Settings == null)
                Settings = new GameSettings();

            LocalizationManager.Instance.ApplyLanguage(Settings.language);
        }
        else Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsPanel();
        }
    }

    void ToggleSettingsPanel()
    {
        // 🔍 Busca o SettingsPanel apenas UMA vez
        if (settingsPanel == null)
        {
            settingsPanel = GameObject.Find("SettingsPanel");

            // Se encontrou, pega o painel interno
            if (settingsPanel != null)
            {
                settingsContent = settingsPanel.transform
                    .Find("SettingsContent")
                    ?.gameObject;
            }
        }

        // ❌ Não existe ainda → instancia
        if (settingsPanel == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            settingsPanel = Instantiate(UIsettingsPrefab, canvas.transform);
            settingsPanel.name = "SettingsPanel";

            settingsContent = settingsPanel.transform
                .Find("SettingsContent")
                ?.gameObject;
        }

        // 🔐 Segurança
        if (settingsContent == null)
        {
            Debug.LogError("SettingsContent não encontrado dentro de SettingsPanel");
            return;
        }

        // ✅ Toggle SOMENTE do painel interno
        bool newState = !settingsContent.activeSelf;
        settingsContent?.SetActive(newState);

        LanguageUI languageUI = settingsContent.GetComponent<LanguageUI>();

        if (SceneManager.GetActiveScene().name != "Menu")
        {
            languageUI.panelButtons.SetActive(false);
            languageUI.dropdown.gameObject.SetActive(false);
        }


        Time.timeScale = newState ? 0f : 1f; // opcional
    }

    public void Save()
    {
        PlayerPrefs.SetString("Settings", JsonUtility.ToJson(Settings));
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (PlayerPrefs.HasKey("Settings"))
            Settings = JsonUtility.FromJson<GameSettings>(
                PlayerPrefs.GetString("Settings")
            );
        else
        {
            Settings = new GameSettings();

            // 🌍 PRIMEIRA EXECUÇÃO → detectar idioma
            Settings.language = LocalizationManager.DetectSystemLanguage();

            Save();
        }
    }

    IEnumerator CopyInitialNativeData()
    {
        // 🔁 Adicione aqui TODAS as pastas que você precisa listar depois
        yield return StartCoroutine(CopyStreamingAssetsFolder("Pieces"));
        yield return StartCoroutine(CopyStreamingAssetsFolder("Sprites"));
        yield return StartCoroutine(CopyStreamingAssetsFolder("Squads"));

        //PlayerPrefs.SetInt("StreamingAssetsCopied", 1);
        //PlayerPrefs.Save();

        //Debug.Log("✔ StreamingAssets copiado para persistentDataPath");
    }


    IEnumerator CopyStreamingAssetsFolder(string folderName)
    {

        string extractPath = Path.Combine(Application.persistentDataPath, folderName);

        if (Directory.Exists(extractPath))
        {
            Debug.Log("Exists: " + extractPath);
            yield break;
        }

        if (RewardManager.Instance != null)
            RewardManager.Instance.ResetRewards();

        Debug.Log("✔ StreamingAssets copiado para persistentDataPath");

        string zipPath = Path.Combine(Application.streamingAssetsPath, folderName + ".zip");
        string targetZip = Path.Combine(Application.persistentDataPath, folderName + ".zip");

        using (UnityWebRequest www = UnityWebRequest.Get(zipPath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }

            File.WriteAllBytes(targetZip, www.downloadHandler.data);
        }

        System.IO.Compression.ZipFile.ExtractToDirectory(targetZip, extractPath);
        File.Delete(targetZip);
    }


}
