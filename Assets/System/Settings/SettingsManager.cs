using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

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
            _ => "pt-BR"
        };
    }

    public static string ToDisplayName(Language lang)
    {
        return lang switch
        {
            Language.PortugueseBR => "Português (Brasil)",
            Language.EnglishUS => "English (US)",
            Language.SpanishES => "Español",
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
        settingsContent.SetActive(newState);

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

        PlayerPrefs.SetInt("StreamingAssetsCopied", 1);
        PlayerPrefs.Save();

        Debug.Log("✔ StreamingAssets copiado para persistentDataPath");
    }


    IEnumerator CopyStreamingAssetsFolder(string folderName)
    {

        string extractPath = Path.Combine(Application.persistentDataPath, folderName);

        if (Directory.Exists(extractPath))
        {
            Debug.Log("Exists: " + extractPath);
            yield break;
        }

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
