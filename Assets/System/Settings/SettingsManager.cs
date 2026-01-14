using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    //public int qualityLevel = 0;
}


public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    public GameSettings Settings;

    public GameObject UIsettingsPrefab;
    public GameObject settingsContent;
    private GameObject settingsPanel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Load();

            // 🔒 garantia absoluta
            if (Settings == null)
                Settings = new GameSettings();
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
            Settings = new GameSettings();
    }
}
