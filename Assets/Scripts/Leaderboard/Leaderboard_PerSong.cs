using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

public class Leaderboard_PerSong : MonoBehaviour
{
    [Header("UI")]
    public GameObject entryPrefab;
    public Transform leaderboardContainer;
    public TMP_Text currentSongDisplay; // Tampilkan nama lagu saat ini (opsional)

    [Header("Navigation")]
    public Button buttonLeft;
    public Button buttonRight;

    private List<LeaderboardEntry> allEntries = new List<LeaderboardEntry>();
    private Dictionary<string, List<LeaderboardEntry>> songEntries = new Dictionary<string, List<LeaderboardEntry>>();
    private List<string> songList = new List<string>();
    private int currentSongIndex = -1; // -1 = All Songs

    private string FilePath => Path.Combine(Application.streamingAssetsPath, "Leaderboard", "leaderboard.json");

    void Start()
    {
        LoadLeaderboard();
        BuildSongDictionary();
        ShowLeaderboardBySongIndex(currentSongIndex); // Start with All Songs

        if (buttonLeft != null)
            buttonLeft.onClick.AddListener(ShowPreviousSong);
        if (buttonRight != null)
            buttonRight.onClick.AddListener(ShowNextSong);
    }

    void LoadLeaderboard()
    {
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);
            Wrapper data = JsonUtility.FromJson<Wrapper>(json);
            allEntries = data.entries ?? new List<LeaderboardEntry>();
        }
        else
        {
            allEntries = new List<LeaderboardEntry>();
        }
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

    void ShowLeaderboardBySongIndex(int index)
    {
        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }

        List<LeaderboardEntry> entriesToShow;

        if (index == -1)
        {
            entriesToShow = allEntries.OrderByDescending(e => e.score).ToList();
            if (currentSongDisplay != null) currentSongDisplay.text = "All Songs";
        }
        else
        {
            if (index < 0 || index >= songList.Count) return;
            string song = songList[index];
            entriesToShow = songEntries[song];
            if (currentSongDisplay != null) currentSongDisplay.text = song;
        }

        for (int i = 0; i < entriesToShow.Count; i++)
        {
            AddEntryToUI(entriesToShow[i], i);
        }
    }

    void AddEntryToUI(LeaderboardEntry entry, int index)
    {
        GameObject newItem = Instantiate(entryPrefab, leaderboardContainer);
        TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();

        if (texts.Length >= 5)
        {
            texts[0].text = (index + 1).ToString();       // Rank
            texts[1].text = entry.playerName;             // Name
            texts[2].text = entry.songName;               // Song Name
            texts[3].text = entry.institution;            // Institution
            texts[4].text = entry.score.ToString();       // Score
        }
    }

    public void ShowNextSong()
    {
        if (songList.Count == 0) return;

        if (currentSongIndex < songList.Count - 1)
        {
            currentSongIndex++;
        }
        else
        {
            currentSongIndex = -1; // wrap around to All Songs
        }

        ShowLeaderboardBySongIndex(currentSongIndex);
    }

    public void ShowPreviousSong()
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
            currentSongIndex = -1;
        }

        ShowLeaderboardBySongIndex(currentSongIndex);
    }

    public void RefreshLeaderboard()
    {
        LoadLeaderboard();
        BuildSongDictionary();
        ShowLeaderboardBySongIndex(currentSongIndex);
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<LeaderboardEntry> entries;
    }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public string songName;
        public string institution;
        public int score;
    }
}
