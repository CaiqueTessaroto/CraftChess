using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphicsOptionsUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    List<Resolution> resolutions = new List<Resolution>();

    void Start()
    {
        Load();
        SetupResolutions();
        SetupFullscreen();
        ApplyGraphics(); // aplica ao abrir
    }

    #region Setup

    void SetupResolutions()
    {
        resolutions.Clear();
        resolutionDropdown.ClearOptions();

        foreach (Resolution res in Screen.resolutions)
        {
            // evita resoluções duplicadas (refresh rate diferente)
            if (!resolutions.Exists(r =>
                r.width == res.width && r.height == res.height))
            {
                // opcional: limita resolução mínima
                if (res.width >= 1280 && res.height >= 720)
                    resolutions.Add(res);
            }
        }

        List<string> options = new List<string>();
        foreach (Resolution res in resolutions)
            options.Add($"{res.width} x {res.height}");

        resolutionDropdown.AddOptions(options);

        resolutionDropdown.value =
            Mathf.Clamp(
                SettingsManager.Instance.Settings.resolutionIndex,
                0,
                resolutions.Count - 1
            );

        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    void SetupFullscreen()
    {
        fullscreenToggle.SetIsOnWithoutNotify(
            SettingsManager.Instance.Settings.fullscreen
        );

        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }


    public void Load()
    {
        if (PlayerPrefs.HasKey("Settings"))
        {
            SettingsManager.Instance.Settings = JsonUtility.FromJson<GameSettings>(
                PlayerPrefs.GetString("Settings")
            );
        }
        else
        {
            // 🔰 Setup inicial padrão
            SettingsManager.Instance.Settings = new GameSettings();

            SettingsManager.Instance.Settings.fullscreen = false;
            SettingsManager.Instance.Settings.resolutionIndex = 0; // menor resolução
            //SettingsManager.Instance.Settings.qualityLevel = QualitySettings.GetQualityLevel();

            SettingsManager.Instance.Save();
        }
    }

    #endregion

    #region Callbacks UI

    void OnResolutionChanged(int index)
    {
        SettingsManager.Instance.Settings.resolutionIndex = index;
        ApplyGraphics();
    }

    void OnFullscreenChanged(bool value)
    {
        var settings = SettingsManager.Instance.Settings;

        settings.fullscreen = value;

        if (value) // ligou fullscreen
        {
            settings.resolutionIndex = GetMaxResolutionIndex();

            // atualiza o dropdown visualmente (sem disparar evento)
            resolutionDropdown.SetValueWithoutNotify(
                settings.resolutionIndex
            );
        }
        else
        {

            settings.resolutionIndex = 0;

            // atualiza o dropdown visualmente (sem disparar evento)
            resolutionDropdown.SetValueWithoutNotify(
                settings.resolutionIndex
            );
        }

        ApplyGraphics();
    }

    #endregion

    #region Apply

    void ApplyGraphics()
    {
        var s = SettingsManager.Instance.Settings;

        if (resolutions == null || resolutions.Count == 0)
            return;

        int index = Mathf.Clamp(
            s.resolutionIndex,
            0,
            resolutions.Count - 1
        );

        // 🖥️ Fullscreen / Janela
        Screen.fullScreenMode = s.fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        // 📺 Resolução
        Resolution res = resolutions[index];
        Screen.SetResolution(
            res.width,
            res.height,
            s.fullscreen
        );

        SettingsManager.Instance.Save();
    }


    #endregion




    int GetMaxResolutionIndex()
    {
        int maxIndex = 0;
        int maxPixels = 0;

        for (int i = 0; i < resolutions.Count; i++)
        {
            int pixels = resolutions[i].width * resolutions[i].height;

            if (pixels > maxPixels)
            {
                maxPixels = pixels;
                maxIndex = i;
            }
        }

        return maxIndex;
    }
}
