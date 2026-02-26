using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardPreviewManager : MonoBehaviour
{
    public GameObject entryPrefab;
    public Transform leaderboardContainer;

    private const string LeaderboardKey = "LeaderboardData";

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
        LoadAndDisplayLeaderboard();
    }

    void LoadAndDisplayLeaderboard()
    {
        string json = PlayerPrefs.GetString(LeaderboardKey, "");
        if (string.IsNullOrEmpty(json)) return;

        Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);
        if (wrapper.entries == null) return;

        int rank = 1;
        foreach (var entry in wrapper.entries)
        {
            GameObject newItem = Instantiate(entryPrefab, leaderboardContainer);
            TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();

            texts[0].text = rank.ToString();
            texts[1].text = entry.playerName;
            texts[2].text = entry.songName;
            texts[3].text = entry.institution;
            texts[4].text = entry.score.ToString();

            rank++;
        }
    }
}
