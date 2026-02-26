using UnityEngine;
using UnityEngine.Video;

public class PauseMenuController : MonoBehaviour
{
    public GameObject pauseButton;
    public GameObject pauseMenuUI;
    public VideoPlayer videoPlayer; // Tambahkan ini
    private bool isPaused = false;

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        pauseButton.SetActive(true);
        pauseMenuUI.SetActive(false);
    }

    public void OnPauseButtonPressed()
    {
        pauseMenuUI.SetActive(true);
        pauseButton.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;

        if (videoPlayer != null)
            videoPlayer.Pause(); // Pause videonya
    }

    public void OnResumeButtonPressed()
    {
        pauseMenuUI.SetActive(false);
        pauseButton.SetActive(true);
        Time.timeScale = 1f;
        isPaused = false;

        if (videoPlayer != null)
            videoPlayer.Play(); // Lanjutkan video
    }

    public void OnBackButtonPressed()
    {
        Debug.Log("Keluar dari game");
        Application.Quit();
    }
}
