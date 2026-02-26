using UnityEngine;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    [Header("Panel dan Tombol Navigasi")]
    public GameObject targetPanel;
    public Button openButton;
    public Button backButton;

    [Header("Tombol Reset Leaderboard")]
    public Button resetButton; // Tambahkan tombol reset di Inspector

    private const string LeaderboardKey = "LeaderboardData";

    void Start()
    {
        targetPanel.SetActive(false);          // Sembunyikan panel saat awal
        openButton.gameObject.SetActive(true); // Tampilkan tombol pembuka

        openButton.onClick.AddListener(ShowPanel);
        backButton.onClick.AddListener(HidePanel);

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetLeaderboard);
        }
    }

    void Update()
    {
        if (targetPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HidePanel();
        }
    }

    void ShowPanel()
    {
        targetPanel.SetActive(true);              // Tampilkan panel
        openButton.gameObject.SetActive(false);   // Sembunyikan tombol open
    }

    void HidePanel()
    {
        targetPanel.SetActive(false);             // Sembunyikan panel
        openButton.gameObject.SetActive(true);    // Tampilkan tombol open
    }

    void ResetLeaderboard()
    {
        PlayerPrefs.DeleteKey(LeaderboardKey);
        PlayerPrefs.Save();
        Debug.Log("Leaderboard telah direset!");
    }
}
