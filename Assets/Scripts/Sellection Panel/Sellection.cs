using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.IO;
using System.Collections.Generic;

public class SelectionMenuController : MonoBehaviour
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

    [Header("Optional Panels")]
    public GameObject mainMenuPanel;   // panel menu awal
    public GameObject songSelectPanel; // panel pilih lagu

    private int currentIndex = 0;

    private readonly List<SongMetadata> customSongs   = new List<SongMetadata>();
    private readonly List<string>       customJsonPaths = new List<string>();

    private bool _loadedOnce = false;

    // ========= LIFECYCLE =========
    void Start()
    {
        HookButtons();
        HandleInitialPanel();

        // tambahkan event auto-next
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;

        if (_loadedOnce) { ClampIndex(); UpdateMenu(); }
    }

    void OnEnable()
    {
        if (!_loadedOnce)
        {
            RefreshSongList();
            _loadedOnce = true;
        }
        else
        {
            ClampIndex();
            UpdateMenu();
        }
    }

    private void HookButtons()
    {
        if (buttonLeft)  buttonLeft.onClick.AddListener(MoveLeft);
        if (buttonRight) buttonRight.onClick.AddListener(MoveRight);
        if (buttonStart) buttonStart.onClick.AddListener(StartGame);
    }

    private void HandleInitialPanel()
    {
        bool goToSongSelect = PlayerPrefs.GetInt("GoToSongSelect", 0) == 1;
        if (goToSongSelect)
        {
            PlayerPrefs.SetInt("GoToSongSelect", 0);
            PlayerPrefs.Save();
            ShowSongSelect();
        }
        else
        {
            ShowMainMenu();
        }
    }

    // ========= PANELS =========
    public void ShowMainMenu()
    {
        if (mainMenuPanel)   mainMenuPanel.SetActive(true);
        if (songSelectPanel) songSelectPanel.SetActive(false);

        if (videoPlayer)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
            videoPlayer.url  = "";
        }
        if (mainTitleText) mainTitleText.text = "Main Menu";
    }

    public void ShowSongSelect()
    {
        if (mainMenuPanel)   mainMenuPanel.SetActive(false);
        if (songSelectPanel) songSelectPanel.SetActive(true);

        UpdateMenu();
    }

    // ========= LOAD CUSTOM SONGS =========
    public void RefreshSongList()
    {
        customSongs.Clear();
        customJsonPaths.Clear();
        currentIndex = 0;

        LoadCustomSongs();
        ClampIndex();
        UpdateMenu();

        Debug.Log("🔁 Lagu diperbarui setelah refresh.");
    }

    private void LoadCustomSongs()
    {
        string songsFolder = Path.Combine(Application.streamingAssetsPath, "Bitmap");
        if (!Directory.Exists(songsFolder))
        {
            Debug.LogWarning("❌ Folder tidak ditemukan, membuat folder baru: " + songsFolder);
            Directory.CreateDirectory(songsFolder);
            return;
        }

        string[] jsonFiles = Directory.GetFiles(songsFolder, "*.json");
        foreach (var jsonPath in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(jsonPath);
                var meta = JsonUtility.FromJson<SongMetadata>(json);
                if (meta == null) continue;

                customSongs.Add(meta);
                customJsonPaths.Add(jsonPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"⚠️ Gagal parse JSON: {jsonPath}\n{ex.Message}");
            }
        }
        Debug.Log($"✅ Loaded {customSongs.Count} custom songs from StreamingAssets/Bitmap");
    }

    // ========= MENU RENDER =========
    private void UpdateMenu()
    {
        if (videoPlayer && videoPlayer.isPlaying) videoPlayer.Stop();

        int total = TotalSongCount();
        if (total == 0)
        {
            if (mainTitleText) mainTitleText.text = "No Songs Available";
            return;
        }

        int validDefaultCount = GetValidDefaultCount();

        if (currentIndex < validDefaultCount)
        {
            // default song
            int srcIndex = MapToDefaultSourceIndex(currentIndex);
            VideoClip clip = (videoClips != null && srcIndex < videoClips.Length) ? videoClips[srcIndex] : null;

            if (videoPlayer && clip)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip   = clip;
                videoPlayer.Prepare();
                videoPlayer.Play();
            }

            if (mainTitleText)
            {
                string title = (defaultSongTitles != null && srcIndex < defaultSongTitles.Length && !string.IsNullOrEmpty(defaultSongTitles[srcIndex]))
                    ? defaultSongTitles[srcIndex]
                    : $"Default Song {srcIndex + 1}";
                mainTitleText.text = title;
            }
        }
        else
        {
            // custom song
            int customIndex = currentIndex - validDefaultCount;
            if (customIndex >= 0 && customIndex < customSongs.Count)
            {
                var meta = customSongs[customIndex];
                string fullMenuBg = ResolveAssetPath(meta.menuBackgroundPath);

                if (videoPlayer)
                {
                    if (!string.IsNullOrEmpty(fullMenuBg) && File.Exists(fullMenuBg))
                    {
                        videoPlayer.source = VideoSource.Url;
                        videoPlayer.url    = fullMenuBg;
                        videoPlayer.Prepare();
                        videoPlayer.Play();
                    }
                    else
                    {
                        videoPlayer.clip = null;
                        videoPlayer.url  = "";
                        Debug.LogWarning("⚠️ File video custom tidak ditemukan: " + fullMenuBg);
                    }
                }

                if (mainTitleText) mainTitleText.text = meta.title;
            }
        }
    }

    // ========= AUTOPLAY HANDLER =========
    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("✅ Video selesai → autoplay ke lagu berikutnya...");
        currentIndex++;
        ClampIndex();
        UpdateMenu();
    }

    // Cari file di StreamingAssets
    private static string ResolveAssetPath(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        if (Path.IsPathRooted(raw) && File.Exists(raw))
            return raw;

        string fileName = Path.GetFileName(raw);

        string p1 = Path.Combine(Application.streamingAssetsPath, fileName);
        if (File.Exists(p1)) return p1;

        string p2 = Path.Combine(Application.streamingAssetsPath, "Bitmap", fileName);
        if (File.Exists(p2)) return p2;

        return p1;
    }

    // ========= NAV =========
    private void MoveLeft()  { currentIndex--; ClampIndex(); UpdateMenu(); }
    private void MoveRight() { currentIndex++; ClampIndex(); UpdateMenu(); }

    private void ClampIndex()
    {
        int total = TotalSongCount();
        if (total <= 0) { currentIndex = 0; return; }
        if (currentIndex < 0) currentIndex = total - 1;
        if (currentIndex >= total) currentIndex = 0;
    }

    // ========= START GAME =========
    private void StartGame()
    {
        int total = TotalSongCount();
        if (total == 0) return;

        int validDefaultCount = GetValidDefaultCount();

        if (currentIndex < validDefaultCount)
        {
            int srcIndex = MapToDefaultSourceIndex(currentIndex);
            GameData.selectedSongIndex = srcIndex;
            GameData.selectedSongJsonPath = "";

            string title = (defaultSongTitles != null && srcIndex < defaultSongTitles.Length && !string.IsNullOrEmpty(defaultSongTitles[srcIndex]))
                ? defaultSongTitles[srcIndex]
                : $"Default Song {srcIndex + 1}";
            PlayerPrefs.SetString("SelectedSong", title);
        }
        else
        {
            int customIndex = currentIndex - validDefaultCount;
            if (customIndex >= 0 && customIndex < customJsonPaths.Count)
            {
                GameData.selectedSongJsonPath = customJsonPaths[customIndex];
                GameData.selectedSongIndex = -1;
                PlayerPrefs.SetString("SelectedSong", customSongs[customIndex].title);
            }
        }

        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneGameplayName);
    }

    // ========= UTILS =========
    private int TotalSongCount() => GetValidDefaultCount() + customSongs.Count;

    private int GetValidDefaultCount()
    {
        int count = 0;
        int n = Mathf.Max(videoClips != null ? videoClips.Length : 0,
                          defaultSongTitles != null ? defaultSongTitles.Length : 0);
        for (int i = 0; i < n; i++)
        {
            bool hasClip  = videoClips != null && i < videoClips.Length && videoClips[i] != null;
            bool hasTitle = defaultSongTitles != null && i < defaultSongTitles.Length && !string.IsNullOrEmpty(defaultSongTitles[i]);
            if (hasClip || hasTitle) count++;
        }
        return count;
    }

    private int MapToDefaultSourceIndex(int validIndex)
    {
        int n = Mathf.Max(videoClips != null ? videoClips.Length : 0,
                          defaultSongTitles != null ? defaultSongTitles.Length : 0);

        int count = 0;
        for (int i = 0; i < n; i++)
        {
            bool hasClip  = videoClips != null && i < videoClips.Length && videoClips[i] != null;
            bool hasTitle = defaultSongTitles != null && i < defaultSongTitles.Length && !string.IsNullOrEmpty(defaultSongTitles[i]);
            if (hasClip || hasTitle)
            {
                if (count == validIndex) return i;
                count++;
            }
        }
        return Mathf.Clamp(validIndex, 0, Mathf.Max(0, n - 1));
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
}
