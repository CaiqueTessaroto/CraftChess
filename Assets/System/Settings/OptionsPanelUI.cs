using UnityEngine;
using UnityEngine.UI;

public class OptionsPanelUI : MonoBehaviour
{
    [Header("Painel")]
    public GameObject optionsPanel;

    public Button apply;
    public Button reset;
    public Button close;


    GameSettings workingSettings;


    void Start()
    {

        close.onClick.AddListener(() =>
        {
            CloseSettings();
        });

        apply.onClick.AddListener(() =>
        {
            OnApply();
        });

        reset.onClick.AddListener(() =>
        {
            OnReset();
        });


    }

    public void CloseSettings()
    {
        SettingsManager.Instance.SendMessage("ToggleSettingsPanel");
    }

    void OnEnable()
    {
        // cria uma cópia temporária
        workingSettings = JsonUtility.FromJson<GameSettings>(
            JsonUtility.ToJson(SettingsManager.Instance.Settings)
        );

        RefreshUI();
    }

    // =============================
    // BOTÕES
    // =============================

    // ▶ APPLY
    public void OnApply()
    {
        // copia temporário → real
        SettingsManager.Instance.Settings = workingSettings;

        ApplyAll();
        SettingsManager.Instance.Save();
    }

    // 🔄 RESET
    public void OnReset()
    {
        workingSettings = new GameSettings(); // valores padrão
        RefreshUI();
    }

    // =============================
    // APLICAÇÕES
    // =============================

    void ApplyAll()
    {
        ApplyGraphics();
        ApplyAudio();
    }

    void ApplyGraphics()
    {
        var s = SettingsManager.Instance.Settings;

        Screen.fullScreenMode = s.fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Resolution res = Screen.resolutions[s.resolutionIndex];
        Screen.SetResolution(res.width, res.height, s.fullscreen);

        //QualitySettings.SetQualityLevel(s.qualityLevel, true);
    }

    void ApplyAudio()
    {
        // Master
        AudioListener.volume = SettingsManager.Instance.Settings.masterVolume;

        // Se tiver AudioManager, use ele
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyVolumes();
    }

    // =============================
    // UI ↔ SETTINGS TEMPORÁRIOS
    // =============================

    void RefreshUI()
    {
        // Aqui você sincroniza sliders, dropdowns e toggles
        // Exemplo:
        // masterSlider.value = workingSettings.masterVolume;
        // fullscreenToggle.isOn = workingSettings.fullscreen;
        // resolutionDropdown.value = workingSettings.resolutionIndex;
    }

    // ====== CALLBACKS DA UI ======

    public void OnMasterVolumeChanged(float value)
        => workingSettings.masterVolume = value;

    public void OnMusicVolumeChanged(float value)
        => workingSettings.musicVolume = value;

    public void OnSfxVolumeChanged(float value)
        => workingSettings.sfxVolume = value;

    public void OnResolutionChanged(int index)
        => workingSettings.resolutionIndex = index;

    //public void OnQualityChanged(int value)
    //    => workingSettings.qualityLevel = value;

    public void OnFullscreenChanged(bool value)
        => workingSettings.fullscreen = value;
}
