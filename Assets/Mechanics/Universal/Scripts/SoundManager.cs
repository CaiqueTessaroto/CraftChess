using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Start is called before the first frame update

    //public AudioClip selectSound;
    //public AudioClip deselectSound;
    public AudioClip moveSound;
    public AudioClip captureSound;
    public AudioSource audioSource;

    void Start()
    {

        //audioSource = FindObjectOfType<AudioSource>();

    }

    public void PlayCapture()
    {
        if (captureSound)
            PlaySound(captureSound);
    }

    public void PlayMove()
    {
        if (moveSound)
            PlaySound(moveSound);
    }


    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
