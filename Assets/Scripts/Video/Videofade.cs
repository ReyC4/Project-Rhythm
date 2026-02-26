using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VideoDelayPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;        // Drag komponen VideoPlayer ke sini
    public float delayBeforePlay = 2f;     // Waktu jeda sebelum video diputar (dalam detik)

    void Start()
    {
        StartCoroutine(PlayVideoAfterDelay());
    }

    private IEnumerator PlayVideoAfterDelay()
    {
        // Tunggu jeda sesuai yang diatur di Inspector
        yield return new WaitForSeconds(delayBeforePlay);

        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("VideoPlayer tidak di-assign!");
        }
    }
}
