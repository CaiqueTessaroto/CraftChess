using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphicsOptionsUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;

    List<Resolution> resolutions = new List<Resolution>();

    void Start()
    {
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
        fullscreenToggle.isOn =
            SettingsManager.Instance.Settings.fullscreen;

        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
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
        SettingsManager.Instance.Settings.fullscreen = value;
        ApplyGraphics();
    }

    #endregion

    #region Apply

    void ApplyGraphics()
    {
        var s = SettingsManager.Instance.Settings;

        // 🖥️ Fullscreen / Janela
        Screen.fullScreenMode = s.fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        // 📺 Resolução
        Resolution res = resolutions[s.resolutionIndex];
        Screen.SetResolution(
            res.width,
            res.height,
            s.fullscreen
        );

        SettingsManager.Instance.Save();
    }

    #endregion
}
