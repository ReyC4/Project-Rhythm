using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoControl : MonoBehaviour
{
    [Header("Untuk Editor")]
    public RawImage editorVideoUI;
    public VideoPlayer editorVideoPlayer;

    public void ShowVideo()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    Debug.Log("Memanggil ShowVideo WebGL");
    Application.ExternalEval("ShowVideo();");
#else
        Debug.Log("Memanggil ShowVideo Editor");
        if (editorVideoUI != null) editorVideoUI.gameObject.SetActive(true);
        if (editorVideoPlayer != null) editorVideoPlayer.Play();
#endif
    }

    public void HideVideo()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval("HideVideo();");
#else
        if (editorVideoPlayer != null)
        {
            editorVideoPlayer.Stop();
        }
        if (editorVideoUI != null)
        {
            editorVideoUI.gameObject.SetActive(false);
        }
#endif
    }
}
