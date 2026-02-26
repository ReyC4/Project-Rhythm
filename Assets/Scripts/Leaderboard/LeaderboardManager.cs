using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

[System.Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public string songName;
    public string institution;
    public int score;
}

public class LeaderboardManager : MonoBehaviour
{
    [Header("Input UI")]
    public TMP_InputField nameInput;
    public TMP_InputField institutionInput;
    public TMP_Text scoreText;
    public TMP_Text songNameText;
    public TMP_Text finalScoreDisplay;
    public TMP_Text finalSongDisplay;

    [Header("Leaderboard UI")]
    public GameObject entryPrefab;
    public Transform leaderboardContainer;

    [Header("Panel Control")]
    public PanelSwitcher panelSwitcher;

    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    private int finalScore;

    private string FolderPath => Path.Combine(Application.streamingAssetsPath, "Leaderboard");
    private string FilePath => Path.Combine(FolderPath, "leaderboard.json");

    void Start()
    {
        if (PlayerPrefs.HasKey("FinalScore"))
        {
            SetFinalScore(PlayerPrefs.GetInt("FinalScore"));
        }

        if (PlayerPrefs.HasKey("SelectedSong"))
        {
            SetSongName(PlayerPrefs.GetString("SelectedSong"));
        }

        LoadLeaderboard();
        UpdateLeaderboardUI();
    }

    public void SetFinalScore(int score)
    {
        finalScore = score;

        if (scoreText != null)
            scoreText.text = score.ToString();

        if (finalScoreDisplay != null)
            finalScoreDisplay.text = "Final Score: " + score;
    }

    public void SetSongName(string songName)
    {
        if (songNameText != null)
            songNameText.text = songName;

        if (finalSongDisplay != null)
            finalSongDisplay.text = songName;
    }

    public void SubmitEntry()
    {
        if (string.IsNullOrEmpty(nameInput.text) || string.IsNullOrEmpty(institutionInput.text))
        {
            Debug.LogWarning("Nama dan instansi harus diisi!");
            return;
        }

        LoadLeaderboard();

        LeaderboardEntry newEntry = new LeaderboardEntry
        {
            playerName = nameInput.text,
            songName = songNameText.text,
            institution = institutionInput.text,
            score = finalScore
        };

        entries.Add(newEntry);
        entries.Sort((a, b) => b.score.CompareTo(a.score));

        SaveLeaderboard();
        UpdateLeaderboardUI();

        // 🔄 Refresh leaderboard per song jika script tersebut aktif di scene
        FindObjectOfType<Leaderboard_PerSong>()?.RefreshLeaderboard();

        if (panelSwitcher != null)
        {
            panelSwitcher.ShowLeaderboard();
        }
    }

    void AddEntryToUI(LeaderboardEntry entry, int index)
    {
        GameObject newItem = Instantiate(entryPrefab, leaderboardContainer);
        TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();

        if (texts.Length >= 5)
        {
            texts[0].text = (index + 1).ToString();
            texts[1].text = entry.playerName;
            texts[2].text = entry.songName;
            texts[3].text = entry.institution;
            texts[4].text = entry.score.ToString();
        }
        else
        {
            Debug.LogWarning("Prefab tidak memiliki cukup TMP_Text.");
        }
    }

    void UpdateLeaderboardUI()
    {
        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            AddEntryToUI(entries[i], i);
        }
    }

    void SaveLeaderboard()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }

        string json = JsonUtility.ToJson(new Wrapper { entries = this.entries }, true);
        File.WriteAllText(FilePath, json);
        Debug.Log($"✅ Leaderboard berhasil disimpan di {FilePath}");
    }

    void LoadLeaderboard()
    {
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);
            Wrapper data = JsonUtility.FromJson<Wrapper>(json);
            entries = data.entries ?? new List<LeaderboardEntry>();
        }
        else
        {
            entries = new List<LeaderboardEntry>();
        }
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<LeaderboardEntry> entries;
    }

    public void ResetLeaderboard()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log("✅ Leaderboard file dihapus.");
        }

        entries.Clear();
        UpdateLeaderboardUI();
    }
}
