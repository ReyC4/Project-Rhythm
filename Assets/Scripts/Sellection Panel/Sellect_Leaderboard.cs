using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.IO;
using System.Collections.Generic;

public class SongSelectManager : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;
    public VideoClip[] videoClips;

    [Header("Buttons")]
    public Button buttonLeft;
    public Button buttonRight;
    public Button buttonStart;

    [Header("Gameplay Scene")]
    public string sceneGameplayName = "SceneGameplay";

    [Header("UI")]
    public TMPro.TMP_Text mainTitleText;
    public string[] defaultSongTitles;

    private int currentIndex = 0;

    private List<SongMetadata> customSongs = new List<SongMetadata>();
    private List<string> customVideoPaths = new List<string>();
    private List<string> customJsonPaths = new List<string>();

    private int totalSongCount => (videoClips != null ? videoClips.Length : 0) + customSongs.Count;

    void Start()
    {
        LoadCustomSongs();
        UpdateMenu();

        buttonLeft.onClick.AddListener(MoveLeft);
        buttonRight.onClick.AddListener(MoveRight);
        buttonStart.onClick.AddListener(StartGame);
    }

    void LoadCustomSongs()
    {
        string songsFolder = Path.Combine(Application.streamingAssetsPath, "Bitmap");

        if (!Directory.Exists(songsFolder))
        {
            Debug.LogWarning("❌ Folder tidak ditemukan, membuat folder baru: " + songsFolder);
            Directory.CreateDirectory(songsFolder);
            return;
        }

        string[] jsonFiles = Directory.GetFiles(songsFolder, "*.json");

        foreach (string file in jsonFiles)
        {
            string json = File.ReadAllText(file);
            SongMetadata metadata = JsonUtility.FromJson<SongMetadata>(json);
            customSongs.Add(metadata);
            customVideoPaths.Add(metadata.menuBackgroundPath);
            customJsonPaths.Add(file);
        }

        Debug.Log($"✅ Loaded {customSongs.Count} custom songs from StreamingAssets/Bitmap");
    }

    void UpdateMenu()
    {
        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (totalSongCount == 0)
        {
            Debug.LogWarning("⚠️ Tidak ada lagu yang tersedia.");
            if (mainTitleText != null)
                mainTitleText.text = "No Songs Available";
            return;
        }

        if (currentIndex < (videoClips?.Length ?? 0))
        {
            // Default song
            if (videoClips[currentIndex] != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClips[currentIndex];
                videoPlayer.Prepare();
                videoPlayer.Play();
            }

            if (mainTitleText != null)
            {
                mainTitleText.text = (defaultSongTitles != null && currentIndex < defaultSongTitles.Length)
                    ? defaultSongTitles[currentIndex]
                    : "Default Song " + (currentIndex + 1);
            }
        }
        else
        {
            // Custom song
            int customIndex = currentIndex - (videoClips?.Length ?? 0);
            if (customIndex >= 0 && customIndex < customSongs.Count)
            {
                string videoUrl = customVideoPaths[customIndex];
                if (!string.IsNullOrEmpty(videoUrl) && File.Exists(videoUrl))
                {
                    videoPlayer.source = VideoSource.Url;
                    videoPlayer.url = videoUrl;
                    videoPlayer.Prepare();
                    videoPlayer.Play();
                }
                else
                {
                    Debug.LogWarning("⚠️ File video custom tidak ditemukan: " + videoUrl);
                }

                if (mainTitleText != null)
                {
                    mainTitleText.text = customSongs[customIndex].title;
                }
            }
        }
    }

    void MoveLeft()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = totalSongCount - 1;

        UpdateMenu();
    }

    void MoveRight()
    {
        currentIndex++;
        if (currentIndex >= totalSongCount)
            currentIndex = 0;

        UpdateMenu();
    }

    void StartGame()
    {
        if (totalSongCount == 0)
        {
            Debug.LogWarning("⚠️ Tidak ada lagu yang bisa dimainkan.");
            return;
        }

        int defaultCount = (videoClips?.Length ?? 0);
        string selectedSongName;

        if (currentIndex < defaultCount)
        {
            // Default song
            GameData.selectedSongIndex = currentIndex;
            GameData.selectedSongJsonPath = "";

            selectedSongName = (defaultSongTitles != null && currentIndex < defaultSongTitles.Length)
                ? defaultSongTitles[currentIndex]
                : "Default Song " + (currentIndex + 1);

            Debug.Log("▶️ Mulai lagu default index: " + currentIndex);
        }
        else
        {
            // Custom song
            int customIndex = currentIndex - defaultCount;
            if (customIndex >= 0 && customIndex < customJsonPaths.Count)
            {
                GameData.selectedSongJsonPath = customJsonPaths[customIndex];
                GameData.selectedSongIndex = -1;

                selectedSongName = customSongs[customIndex].title;

                Debug.Log("▶️ Mulai lagu custom: " + selectedSongName);
                Debug.Log("📂 JSON path: " + GameData.selectedSongJsonPath);
            }
            else
            {
                Debug.LogError("❌ Indeks custom song tidak valid.");
                return;
            }
        }

        // Simpan nama lagu terpilih
        PlayerPrefs.SetString("SelectedSong", selectedSongName);

        // Simpan daftar semua lagu untuk leaderboard
        List<string> allSongTitles = new List<string>();

        if (defaultSongTitles != null && defaultSongTitles.Length > 0)
            allSongTitles.AddRange(defaultSongTitles);

        foreach (var custom in customSongs)
        {
            allSongTitles.Add(custom.title);
        }

        SongListWrapper wrapper = new SongListWrapper { songs = allSongTitles.ToArray() };
        string allSongsJson = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("AllSongs", allSongsJson);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneGameplayName);
    }

    [System.Serializable]
    public class SongMetadata
    {
        public string title;
        public string artist;
        public string audioPath;
        public string videoPath;
        public string menuBackgroundPath;
        public List<ButtonItem> bitmapData;
    }

    [System.Serializable]
    public class SongListWrapper
    {
        public string[] songs;
    }
}
