using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // Fungsi khusus agar bisa dipanggil dari tombol untuk buka "Game Menu" dan langsung ke panel SongPanel
    public void GoToSongSelectScene()
    {
        GoToMenuAndOpenPanel("Game Menu", "Song_select");
    }

    // Fungsi umum untuk menyimpan target panel yang ingin ditampilkan di scene tujuan
    public void GoToMenuAndOpenPanel(string sceneName, string panelToOpen)
    {
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString("TargetPanel", panelToOpen);
        SceneManager.LoadScene(sceneName);
    }

    // Pindah scene biasa dan simpan scene sebelumnya
    public void ChangeScene(string sceneName)
    {
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
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

    // Keluar dari aplikasi
    public void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
