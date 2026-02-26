using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;

public class GameplayLoaderCustom : MonoBehaviour
{
    [Header("Prefab Panel")]
    public GameObject defaultPanelPrefab;

    private AudioSource audioSource;
    private VideoPlayer videoPlayer;
    private GameVersiController gameController;
    private TMP_Text titleLabel;

    private bool audioReady = false;
    private bool videoReady = false;

    // Simpan metadata supaya bisa dipakai di TryStartGame
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

        Debug.Log($"📂 Metadata loaded: {metadata.title}, bitmap count: {metadata.bitmapData.Count}");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Tidak ditemukan Canvas di scene!");
            return;
        }

        GameObject panel = Instantiate(defaultPanelPrefab, canvas.transform);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = Vector3.one;
        panel.SetActive(true);

        audioSource = panel.GetComponentInChildren<AudioSource>();
        videoPlayer = panel.GetComponentInChildren<VideoPlayer>();
        gameController = panel.GetComponentInChildren<GameVersiController>();
        titleLabel = FindTitleLabel(panel);

        if (gameController == null)
        {
            Debug.LogError("❌ GameVersiController tidak ditemukan di prefab!");
            return;
        }

        gameController.loadDefaultData = false;

        StartCoroutine(LoadAudio(metadata.audioPath));

        if (!string.IsNullOrEmpty(metadata.videoPath))
        {
            if (File.Exists(metadata.videoPath))
            {
                videoPlayer.Stop();
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = metadata.videoPath;

                videoPlayer.Prepare();
                videoPlayer.prepareCompleted += (vp) =>
                {
                    videoReady = true;
                    Debug.Log("✅ Video siap diputar.");
                    TryStartGame();
                };
            }
            else
            {
                Debug.LogWarning("⚠️ File video tidak ditemukan: " + metadata.videoPath);
                videoReady = true;
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Path video kosong.");
            videoReady = true;
        }

        if (titleLabel != null)
        {
            titleLabel.text = $"{metadata.title} - {metadata.artist}";
        }

        Image bgImage = panel.GetComponentInChildren<Image>();
        if (bgImage != null && !string.IsNullOrEmpty(metadata.gameplayBackgroundPath))
        {
            StartCoroutine(LoadBackground(metadata.gameplayBackgroundPath, bgImage));
        }

        Debug.Log("✅ Panel gameplay custom sudah dimuat.");
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
        using (WWW www = new WWW("file://" + path))
        {
            yield return www;
            AudioClip clip = www.GetAudioClip(false, false);
            audioSource.clip = clip;

            if (gameController != null && gameController.musicController != null)
            {
                gameController.musicController.audio = audioSource;
                Debug.Log("✅ MusicController.Audio assigned from loader.");
            }

            audioReady = true;
            TryStartGame();
        }
    }

    void TryStartGame()
    {
        if (audioReady && videoReady)
        {
            Debug.Log("✅ Semua siap, memulai game.");
            gameController.StartGame(metadata.bitmapData);
        }
    }

    IEnumerator LoadBackground(string path, Image image)
    {
        using (WWW www = new WWW("file://" + path))
        {
            yield return www;
            Texture2D tex = www.texture;
            image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }
    }

    [System.Serializable]
    public class SongMetadata
    {
        public string title;
        public string artist;
        public string audioPath;
        public string videoPath;
        public string menuBackgroundPath;
        public string gameplayBackgroundPath;
        public List<ButtonItem> bitmapData;
    }
}
