using UnityEngine;
using System.Collections.Generic; // Necessário para Listas

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource[] sfxSources;

    [Header("Playlist Settings")]
    private AudioClip[] currentPlaylist;
    private int currentTrackIndex = 0;
    private bool isPlaylistActive = false;

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

    void Update()
    {
        // Verifica se a música atual acabou para tocar a próxima da lista
        if (isPlaylistActive && !musicSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    void Start()
    {
        ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        var s = SettingsManager.Instance.Settings;
        AudioListener.volume = s.masterVolume;

        if (musicSource != null)
            musicSource.volume = s.musicVolume;

        foreach (AudioSource sfx in sfxSources)
        {
            if (sfx != null)
                sfx.volume = s.sfxVolume;
        }
    }

    // --- SISTEMA DE PLAYLIST ---

    public void PlayMusicPlaylist(AudioClip[] playlist)
    {
        // Se a lista for nula ou vazia, ignora e continua o que estava tocando
        if (playlist == null || playlist.Length == 0) return;

        // Se a playlist enviada for a mesma que já está tocando, não reinicia
        if (currentPlaylist == playlist) return;

        currentPlaylist = playlist;
        currentTrackIndex = 0;
        isPlaylistActive = true;

        PlayTrack(currentTrackIndex);
    }

    private void PlayTrack(int index)
    {
        if (currentPlaylist == null || index >= currentPlaylist.Length) return;

        musicSource.clip = currentPlaylist[index];
        musicSource.loop = false; // Loop falso para podermos detectar o fim da música no Update
        musicSource.Play();
    }

    private void PlayNextTrack()
    {
        currentTrackIndex++;

        // Se chegou ao fim da lista, volta para o começo
        if (currentTrackIndex >= currentPlaylist.Length)
        {
            currentTrackIndex = 0;
        }

        PlayTrack(currentTrackIndex);
    }

    // --- SFX ---
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource source = GetFreeSFXSource();
        source.clip = clip;
        source.Play();
    }

    AudioSource GetFreeSFXSource()
    {
        foreach (AudioSource sfx in sfxSources)
        {
            if (!sfx.isPlaying) return sfx;
        }
        return sfxSources[0];
    }
}