using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class LeaderboardPanelManager : MonoBehaviour
{
    [Header("Leaderboard UI")]
    public GameObject entryPrefab;
    public Transform leaderboardContainer;

    [Header("Panel dan Tombol Navigasi")]
    public GameObject targetPanel; // Panel leaderboard
    public Button openButton;
    public Button backButton;

    [Header("Tombol Reset Leaderboard")]
    public Button resetButton;

    [Header("Panel Sebelumnya")]
    public GameObject previousPanel; // Panel yang ingin disembunyikan sementara (misal: panel utama dengan VideoPlayer)

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public string songName;
        public string institution;
        public int score;
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<LeaderboardEntry> entries;
    }

    private string FolderPath => Path.Combine(Application.streamingAssetsPath, "Leaderboard");
    private string FilePath => Path.Combine(FolderPath, "leaderboard.json");

    void Start()
    {
        targetPanel.SetActive(false);
        openButton.gameObject.SetActive(true);

        openButton.onClick.AddListener(ShowPanel);
        backButton.onClick.AddListener(HidePanel);

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetLeaderboard);
        }

        LoadAndDisplayLeaderboard();
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
        SetPanelVisible(previousPanel, false); // Sembunyikan visual panel sebelumnya tanpa menghentikan logic
        targetPanel.SetActive(true);
        openButton.gameObject.SetActive(false);
        LoadAndDisplayLeaderboard(); // Refresh leaderboard saat dibuka
    }

    void HidePanel()
    {
        targetPanel.SetActive(false);
        openButton.gameObject.SetActive(true);
        SetPanelVisible(previousPanel, true); // Tampilkan kembali visual panel sebelumnya
    }

    void LoadAndDisplayLeaderboard()
    {
        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }

        if (!File.Exists(FilePath))
        {
            Debug.LogWarning($"⚠️ Leaderboard file belum ada di: {FilePath}");
            return;
        }

        string json = File.ReadAllText(FilePath);
        Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);

        if (wrapper == null || wrapper.entries == null)
        {
            Debug.LogWarning("⚠️ Leaderboard data kosong atau error.");
            return;
        }

        int rank = 1;
        foreach (var entry in wrapper.entries)
        {
            GameObject newItem = Instantiate(entryPrefab, leaderboardContainer);
            TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 5)
            {
                texts[0].text = rank.ToString();
                texts[1].text = entry.playerName;
                texts[2].text = entry.songName;
                texts[3].text = entry.institution;
                texts[4].text = entry.score.ToString();
            }

            rank++;
        }

        Debug.Log($"✅ Leaderboard ditampilkan ({wrapper.entries.Count} entries).");
    }

    void ResetLeaderboard()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log("✅ Leaderboard file dihapus.");
        }

        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void SetPanelVisible(GameObject panel, bool visible)
    {
        if (panel == null) return;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = panel.AddComponent<CanvasGroup>();
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
