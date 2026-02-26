using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

public class CustomGameLoader : MonoBehaviour
{
    [Header("Prefab Panel")]
    public GameObject defaultPanelPrefab;

    [Header("UI References")]
    public GameObject pausePanel; // assign lewat Inspector

    private AudioSource audioSource;
    private VideoPlayer videoPlayer;
    private GameVersiController gameController;
    private TMP_Text titleLabel;

    private bool audioReady = false;
    private bool videoReady = false;

    private SongMetadata metadata;

    void Start()
    {
        string jsonPath = GameData.selectedSongJsonPath;
        if (string.IsNullOrEmpty(jsonPath))
        {
            Debug.LogError("❌ Tidak ada path JSON lagu custom.");
            return;
        }

        string jsonContent = File.ReadAllText(jsonPath);
        metadata = JsonUtility.FromJson<SongMetadata>(jsonContent);

        Debug.Log($"📂 Metadata loaded: {metadata.title}, {metadata.artist}, {metadata.bitmapData.Count} notes");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Tidak ditemukan Canvas di scene!");
            return;
        }

        // Spawn panel clone
        GameObject panel = Instantiate(defaultPanelPrefab, canvas.transform);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = Vector3.one;
        panel.SetActive(true);

        // Taruh clone di paling bawah
        panel.transform.SetAsFirstSibling();

        // Pastikan pause panel selalu di atas
        if (pausePanel != null)
            pausePanel.transform.SetAsLastSibling();

        audioSource  = panel.GetComponentInChildren<AudioSource>(true);
        videoPlayer  = panel.GetComponentInChildren<VideoPlayer>(true);
        gameController = panel.GetComponentInChildren<GameVersiController>(true);
        titleLabel   = FindTitleLabel(panel);

        if (gameController == null)
        {
            Debug.LogError("❌ GameVersiController tidak ditemukan di prefab!");
            return;
        }

        gameController.loadDefaultData = false;

        if (audioSource != null) audioSource.playOnAwake = false;
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping   = false;
        }

        StartCoroutine(LoadVideo(metadata.videoPath));
        StartCoroutine(LoadAudio(metadata.audioPath));
        StartCoroutine(LoadBackground(metadata.gameplayBackgroundPath, panel.GetComponentInChildren<Image>()));

        if (titleLabel != null)
            titleLabel.text = $"{metadata.title} - {metadata.artist}";
    }

    TMP_Text FindTitleLabel(GameObject panel)
    {
        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in texts)
        {
            if (t.gameObject.name.ToLower().Contains("title"))
                return t;
        }
        Debug.LogWarning("⚠️ TMP_Text untuk judul tidak ditemukan.");
        return null;
    }

    IEnumerator LoadAudio(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("❌ Audio path kosong.");
            yield break;
        }

        string fullPath = Path.Combine(Application.streamingAssetsPath, path);
        using (WWW www = new WWW("file://" + fullPath))
        {
            yield return www;
            AudioClip clip = www.GetAudioClip(false, false);
            if (clip == null)
            {
                Debug.LogError("❌ Gagal load AudioClip: " + path);
                yield break;
            }

            if (audioSource == null)
            {
                Debug.LogError("❌ AudioSource tidak ditemukan di prefab panel.");
                yield break;
            }

            audioSource.clip = clip;

            if (gameController != null && gameController.musicController != null)
            {
                gameController.musicController.audio = audioSource;
                Debug.Log("✅ MusicController.Audio assigned.");
            }

            audioReady = true;

            while (!videoReady) yield return null;

            TryStartGame();
        }
    }

    IEnumerator LoadVideo(string path)
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("ℹ️ VideoPlayer tidak ditemukan, skip video.");
            videoReady = true;
            yield break;
        }

        if (string.IsNullOrEmpty(path))
        {
            videoReady = true;
            yield break;
        }

        string fullPath = Path.Combine(Application.streamingAssetsPath, path);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("⚠️ Video file tidak ditemukan: " + fullPath);
            videoReady = true;
            yield break;
        }

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = fullPath;

        Debug.Log("⏳ Preparing video...");
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        Debug.Log("✅ Video siap (prepared).");
        videoReady = true;
    }

    IEnumerator LoadBackground(string path, Image image)
    {
        if (string.IsNullOrEmpty(path) || image == null)
            yield break;

        string fullPath = Path.Combine(Application.streamingAssetsPath, path);
        using (WWW www = new WWW("file://" + fullPath))
        {
            yield return www;
            Texture2D tex = www.texture;
            if (tex != null)
                image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }

    void TryStartGame()
    {
        Debug.Log($"[TryStartGame] audioReady={audioReady}, videoReady={videoReady}, audioClip={(audioSource != null && audioSource.clip != null)}");
        if (audioReady && videoReady)
        {
            gameController.StartGame(metadata.bitmapData);
        }
    }

    [System.Serializable]
    public class SongMetadata
    {
        public string title;
        public string artist;
        public string audioPath;
        public string videoPath;
        public string gameplayBackgroundPath;
        public List<ButtonItem> bitmapData;
    }
}
