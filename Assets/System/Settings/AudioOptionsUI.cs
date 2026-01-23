using UnityEngine;
using UnityEngine.UI;

public class AudioOptionsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        var settings = SettingsManager.Instance.Settings;

        // Inicializa valores
        masterSlider.value = settings.masterVolume;
        musicSlider.value = settings.musicVolume;
        sfxSlider.value = settings.sfxVolume;

        // Listeners
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    } 

    void OnMasterChanged(float value)
    {
        SettingsManager.Instance.Settings.masterVolume = value;
        Apply();
    }

    void OnMusicChanged(float value)
    {
        SettingsManager.Instance.Settings.musicVolume = value;
        Apply();
    }

    void OnSfxChanged(float value)
    {
        SettingsManager.Instance.Settings.sfxVolume = value;
        Apply();
    }

    void Apply()
    {
        AudioManager.Instance.ApplyVolumes();
        SettingsManager.Instance.Save();
    }
}
