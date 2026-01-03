using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource[] sfxSources;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyVolumes(); // aplica volumes ao iniciar
    }

    public void ApplyVolumes()
    {
        var s = SettingsManager.Instance.Settings;

        // Master
        AudioListener.volume = s.masterVolume;

        // Música
        if (musicSource != null)
            musicSource.volume = s.musicVolume;

        // SFX
        foreach (AudioSource sfx in sfxSources)
        {
            if (sfx != null)
                sfx.volume = s.sfxVolume;
        }
    }


    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        AudioSource source = GetFreeSFXSource();
        source.clip = clip;
        source.Play();
    }


    AudioSource GetFreeSFXSource()
    {
        foreach (AudioSource sfx in sfxSources)
        {
            if (!sfx.isPlaying)
                return sfx;
        }

        // Se todos estiverem ocupados, usa o primeiro
        return sfxSources[0];
    }

}
