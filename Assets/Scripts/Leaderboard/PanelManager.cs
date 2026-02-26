using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    public GameObject inputPanel;
    public GameObject leaderboardPanel;

    public GameObject buttonLeft;    // Tambahkan di Inspector
    public GameObject buttonRight;   // Tambahkan di Inspector

    // Panggil ini saat ingin input data
    public void ShowInput()
    {
        inputPanel.SetActive(true);
        leaderboardPanel.SetActive(false);

        // Tombol panah nonaktif
        if (buttonLeft) buttonLeft.SetActive(false);
        if (buttonRight) buttonRight.SetActive(false);
    }

    // Panggil ini setelah submit
    public void ShowLeaderboard()
    {
        inputPanel.SetActive(false);
        leaderboardPanel.SetActive(true);

        // Tombol panah aktif
        if (buttonLeft) buttonLeft.SetActive(true);
        if (buttonRight) buttonRight.SetActive(true);
    }
}
