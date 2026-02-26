using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToSongSelect : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Nama scene menu yang akan diload saat keluar dari gameplay")]
    public string menuSceneName = "SelectionMenu";

    [Header("Panels (drag dari scene MENU)")]
    [Tooltip("Panel Song Select yang akan diaktifkan otomatis saat kembali dari gameplay")]
    public GameObject songSelectPanel;
    [Tooltip("Panel lain (mis. Main Menu) yang ingin dimatikan saat Song Select dibuka")]
    public GameObject menuPanel;

    private const string GoToSongSelectKey = "GoToSongSelect";

    // === NOTE ===
    // Letakkan script ini di SCENE MENU.
    // Saat scene menu dibuka (mis. setelah Exit dari gameplay), script ini akan membaca flag
    // PlayerPrefs dan otomatis mengaktifkan Song Select + menonaktifkan panel lain.
    void Start()
    {
        // Jalankan hanya bila diminta dari gameplay
        if (PlayerPrefs.GetInt(GoToSongSelectKey, 0) == 1)
        {
            // Matikan panel lain (opsional)
            if (menuPanel != null) menuPanel.SetActive(false);

            // Aktifkan Song Select
            if (songSelectPanel != null)
            {
                songSelectPanel.SetActive(true);
                Debug.Log("✅ Song Select panel diaktifkan lewat ReturnToSongSelect.");
            }
            else
            {
                Debug.LogWarning("⚠️ songSelectPanel belum di-assign di Inspector (scene menu).");
            }

            // Reset flag supaya tidak kepanggil lagi berikutnya
            PlayerPrefs.SetInt(GoToSongSelectKey, 0);
            PlayerPrefs.Save();
        }
    }

    // === Panggil fungsi ini dari SCENE GAMEPLAY (mis. tombol Exit di pause menu) ===
    public void ExitToSongSelect()
    {
        // Set flag agar saat scene menu terbuka, panel Song Select otomatis aktif
        PlayerPrefs.SetInt(GoToSongSelectKey, 1);
        PlayerPrefs.Save();

        // Load scene menu
        SceneManager.LoadScene(menuSceneName);
    }
}
