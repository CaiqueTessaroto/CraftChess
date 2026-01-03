using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSettings
{
    // 🎵 Áudio
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.8f;

    // 🖥️ Gráficos
    public int resolutionIndex = 0;
    public bool fullscreen = true;
    //public int qualityLevel = 0;
}


public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    public GameSettings Settings;

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
