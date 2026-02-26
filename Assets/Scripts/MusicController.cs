using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using UnityEngine;

public class MusicController : MonoBehaviour
{
    public AudioSource audio;
    public string clipName;

    void Start ()
    {
        // Pastikan audio terisi dari komponen di GameObject
        this.audio = GetComponent<AudioSource>();
        
        if (this.audio == null)
        {
            this.audio = gameObject.AddComponent<AudioSource>();
            UnityEngine.Debug.LogWarning("AudioSource tidak ditemukan, ditambahkan secara otomatis.");
        }

        LoadMusic();
    }

    public void PlayAudio()
    {
        if (audio != null && audio.clip != null)
        {
            audio.Play();
        }
    }

    private void LoadMusic()
    {
        AudioClip clip = Resources.Load<AudioClip>(clipName);
        if (clip != null)
        {
            this.audio.clip = clip;
        }
        else
        {
            UnityEngine.Debug.LogWarning("AudioClip tidak ditemukan di Resources: " + clipName);
        }
    }
}
