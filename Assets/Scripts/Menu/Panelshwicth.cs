using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class GameMenuPanelController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject songPanel;

    [Header("Buttons")]
    public Button playButton;
    public Button backButton;
    public Button quitButton;

    [Header("Optional Video")]
    public VideoPlayer videoPlayer;

    private const string SongPanelKey = "Song_select"; // Konsisten

    void Start()
    {
        // Cek apakah ada permintaan buka langsung panel Song
        string targetPanel = PlayerPrefs.GetString("TargetPanel", "");

        if (targetPanel == SongPanelKey)
        {
            ShowSongPanel();
        }
        else
        {
            ShowMenuPanel();
        }

        PlayerPrefs.DeleteKey("TargetPanel");

        if (playButton != null)
            playButton.onClick.AddListener(ShowSongPanel);

        if (backButton != null)
            backButton.onClick.AddListener(ShowMenuPanel);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    void ShowSongPanel()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (songPanel != null) songPanel.SetActive(true);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.Play();
        }
    }

    void ShowMenuPanel()
    {
        if (songPanel != null) songPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game called!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Fungsi ganti scene biasa
    public void ChangeScene(string sceneName)
    {
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(sceneName);
    }

    // Fungsi ganti scene & minta tampilkan panel Song Select
    public void ChangeSceneAndOpenSongPanel(string sceneName)
    {
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString("TargetPanel", SongPanelKey);
        SceneManager.LoadScene(sceneName);
    }

    // Kembali ke scene sebelumnya
    public void RestartPreviousScene()
    {
        string previousScene = PlayerPrefs.GetString("PreviousScene", "");
        if (!string.IsNullOrEmpty(previousScene))
        {
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.LogWarning("Previous scene not set!");
        }
    }
}
