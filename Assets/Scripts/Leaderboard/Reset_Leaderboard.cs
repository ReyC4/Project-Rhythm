using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

public class LeaderboardPerSongManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject entryPrefab;
    public Transform leaderboardContainer;
    public TMP_Text currentSongDisplay;

    [Header("Navigation Buttons")]
    public Button buttonLeft;
    public Button buttonRight;
    public Button resetButton;

    private List<LeaderboardEntry> allEntries = new List<LeaderboardEntry>();
    private Dictionary<string, List<LeaderboardEntry>> songEntries = new Dictionary<string, List<LeaderboardEntry>>();
    private List<string> songList = new List<string>();
    private int currentSongIndex = -1; // -1 = All Songs

    private string FilePath => Path.Combine(Application.streamingAssetsPath, "Leaderboard", "leaderboard.json");

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

    void Start()
    {
        LoadLeaderboardFile();
        BuildSongDictionary();
        ShowLeaderboard(currentSongIndex);

        if (buttonLeft != null)
            buttonLeft.onClick.AddListener(ShowPreviousSong);

        if (buttonRight != null)
            buttonRight.onClick.AddListener(ShowNextSong);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetCurrentSongLeaderboard);
    }

    void LoadLeaderboardFile()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogWarning("⚠️ File leaderboard tidak ditemukan.");
            allEntries = new List<LeaderboardEntry>();
            return;
        }

        string json = File.ReadAllText(FilePath);
        Wrapper data = JsonUtility.FromJson<Wrapper>(json);
        allEntries = data?.entries ?? new List<LeaderboardEntry>();
    }

    void BuildSongDictionary()
    {
        songEntries.Clear();
        songList = allEntries
            .Select(e => e.songName)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        foreach (string song in songList)
        {
            songEntries[song] = allEntries
                .Where(e => e.songName == song)
                .OrderByDescending(e => e.score)
                .ToList();
        }
    }

    void ShowLeaderboard(int index)
    {
        foreach (Transform child in leaderboardContainer)
            Destroy(child.gameObject);

        List<LeaderboardEntry> entriesToShow;

        if (index == -1)
        {
            if (currentSongDisplay != null)
                currentSongDisplay.text = "All Songs";

            entriesToShow = allEntries.OrderByDescending(e => e.score).ToList();
        }
        else
        {
            if (index < 0 || index >= songList.Count)
            {
                Debug.LogWarning("⚠️ Index lagu di luar jangkauan.");
                return;
            }

            string song = songList[index];

            if (currentSongDisplay != null)
                currentSongDisplay.text = song;

            entriesToShow = songEntries[song];
        }

        for (int i = 0; i < entriesToShow.Count; i++)
        {
            GameObject newItem = Instantiate(entryPrefab, leaderboardContainer);
            TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 5)
            {
                texts[0].text = (i + 1).ToString();
                texts[1].text = entriesToShow[i].playerName;
                texts[2].text = entriesToShow[i].songName;
                texts[3].text = entriesToShow[i].institution;
                texts[4].text = entriesToShow[i].score.ToString();
            }
        }
    }

    void ShowNextSong()
    {
        if (songList.Count == 0) return;

        if (currentSongIndex < songList.Count - 1)
        {
            currentSongIndex++;
        }
        else
        {
            currentSongIndex = -1; // Kembali ke All Songs
        }

        ShowLeaderboard(currentSongIndex);
    }

    void ShowPreviousSong()
    {
        if (songList.Count == 0) return;

        if (currentSongIndex == -1)
        {
            currentSongIndex = songList.Count - 1;
        }
        else if (currentSongIndex > 0)
        {
            currentSongIndex--;
        }
        else
        {
            currentSongIndex = -1; // Kembali ke All Songs
        }

        ShowLeaderboard(currentSongIndex);
    }

    void ResetCurrentSongLeaderboard()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogWarning("⚠️ File leaderboard tidak ditemukan.");
            return;
        }

        string currentSong = currentSongDisplay != null ? currentSongDisplay.text : null;

        string json = File.ReadAllText(FilePath);
        Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);

        if (wrapper == null || wrapper.entries == null)
        {
            Debug.LogWarning("⚠️ Gagal membaca data leaderboard.");
            return;
        }

        int before = wrapper.entries.Count;

        if (string.IsNullOrEmpty(currentSong) || currentSong == "All Songs")
        {
            wrapper.entries.Clear(); // Hapus semua lagu
            Debug.Log("🗑️ Semua entri dihapus.");
        }
        else
        {
            wrapper.entries = wrapper.entries
                .Where(e => e.songName != currentSong)
                .ToList();
            int removed = before - wrapper.entries.Count;
            Debug.Log($"🗑️ {removed} entri untuk lagu '{currentSong}' dihapus.");
        }

        File.WriteAllText(FilePath, JsonUtility.ToJson(wrapper, true));

        LoadLeaderboardFile();
        BuildSongDictionary();

        if (songList.Count == 0)
            currentSongIndex = -1;
        else if (currentSongIndex >= songList.Count)
            currentSongIndex = songList.Count - 1;

        ShowLeaderboard(currentSongIndex);
    }
}
