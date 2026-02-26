using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class GameVersiController : MonoBehaviour
{
    [Header("Gameplay Settings")]
    public MusicController musicController;
    public GameObject buttonPrefab;
    public TMP_Text scoreLabel;
    public float delayBeforeScoreScene = 2f;
    public string gameDataFileName;

    [Header("Config")]
    public bool loadDefaultData = true;

    [Header("Sync Settings")]
    [Tooltip("Positif: tombol muncul lebih cepat. Negatif: muncul lebih lambat. (ms)")]
    public float spawnLeadTimeMs = 0f;

    [Header("Timing")]
    [Tooltip("Jeda sebelum game dimulai setelah semua siap (detik).")]
    public float delayBeforeStart = 2f;

    [Tooltip("Lebar timing window tiap tombol (ms) → dikirim ke ButtonController.duration")]
    public float timingWindowMs = 800f;

    // =========================
    // GLOBAL SFX (klik tombol)
    // =========================
    [Header("Global SFX (Click)")]
    [Tooltip("AudioSource global untuk memutar SFX klik tombol.")]
    public AudioSource globalSfxSource;
    [Tooltip("Clip klik global. Akan diisi otomatis dari JSON sfxClickPath jika ada.")]
    public AudioClip globalClickClip;
    [Tooltip("Kalau true, Miss juga bunyi. Default: false (hanya saat berhasil tekan).")]
    public bool playClickOnMiss = false;

    private int currentScore = 0;
    private int roundedButtonCount;
    private SortedList<float, ButtonItem> gameButtons = new SortedList<float, ButtonItem>();

    private bool gameRunning = false;
    private bool endSequenceStarted = false;

    private VideoPlayer videoPlayer;

    // ==== PAUSE FLAG ====
    private bool isPaused = false;
    public bool IsPaused => isPaused;

    // ==== SFX (loaded from metadata) ====
    // tetap dipakai sebagai “buffer” saat load dari JSON, lalu diset ke globalClickClip.
    private AudioClip sfxClickClip;
    private AudioClip sfxDragPressClip;   // tidak dipakai di versi global ini
    private AudioClip sfxDragReleaseClip; // tidak dipakai di versi global ini

    void Start()
    {
        // Reset streak setiap kali scene gameplay (custom) dimulai
        ButtonController.ResetPerfectStreak();

        ButtonController.OnClicked += OnGameButtonClick;

        // Siapkan sumber audio global
        if (globalSfxSource == null)
        {
            globalSfxSource = gameObject.GetComponent<AudioSource>();
            if (globalSfxSource == null) globalSfxSource = gameObject.AddComponent<AudioSource>();
            globalSfxSource.playOnAwake = false;
            globalSfxSource.loop = false;
            globalSfxSource.spatialBlend = 0f;
            globalSfxSource.volume = 1f;
            globalSfxSource.mute = false;
            globalSfxSource.outputAudioMixerGroup = null;
        }

        videoPlayer = FindObjectOfType<VideoPlayer>();

        if (loadDefaultData)
        {
            StartCoroutine(LoadGameData());
        }
    }

    void Update()
    {
        if (!gameRunning) return;
        if (musicController == null || musicController.audio == null) return;

        if (isPaused) return;
        if (!musicController.audio.isPlaying) return;

        float currentTimeMs = (musicController.audio.time * 1000f) + spawnLeadTimeMs;

        if (videoPlayer != null && !videoPlayer.isPlaying)
            videoPlayer.Play();

        while (gameButtons.Count > 0 && currentTimeMs > gameButtons.Keys[0])
        {
            float keyTime = gameButtons.Keys[0];
            ButtonItem data = gameButtons[keyTime];

            int buttonNum = 4 - Mathf.Abs(roundedButtonCount) % 4;

            CreateButton(currentTimeMs, data, buttonNum);

            if (data.isDrag)
                roundedButtonCount--;

            gameButtons.RemoveAt(0);
            roundedButtonCount--;
        }

        if (!endSequenceStarted && gameButtons.Count == 0)
        {
            endSequenceStarted = true;
            StartCoroutine(HandleGameEnd());
        }
    }

    public void StartGame(List<ButtonItem> customButtons = null)
    {
        if (customButtons != null)
        {
            SetupButtons(customButtons);
        }

        ButtonController.ResetPerfectStreak();
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        endSequenceStarted = false;

        ButtonController.SetPaused(false);
        isPaused = false;

        if (videoPlayer == null) videoPlayer = FindObjectOfType<VideoPlayer>();

        yield return new WaitForSeconds(delayBeforeStart);

        ButtonController.ResetPerfectStreak();

        if (musicController != null && musicController.audio != null)
        {
            musicController.audio.time = 0f;
            musicController.audio.Play();

            while (!musicController.audio.isPlaying)
                yield return null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.time = 0f;
            videoPlayer.Play();
        }

        gameRunning = true;
        Debug.Log("🎵 Game (custom) started.");
    }

    private IEnumerator LoadGameData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, gameDataFileName);
        string json = "";

#if UNITY_WEBGL
        UnityWebRequest req = UnityWebRequest.Get(filePath);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load game data: " + req.error);
            yield break;
        }
        json = req.downloadHandler.text;
#else
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            yield break;
        }
        json = File.ReadAllText(filePath);
#endif

        SongMetaMaybe meta = null;
        try { meta = JsonUtility.FromJson<SongMetaMaybe>(json); } catch { meta = null; }

        List<ButtonItem> buttons = null;

        if (meta != null && meta.bitmapData != null && meta.bitmapData.Count > 0)
        {
            buttons = meta.bitmapData;

            // Load SFX dari StreamingAssets sesuai JSON (kalau ada)
            yield return StartCoroutine(LoadAllSfx(meta.sfxClickPath, meta.sfxDragPressPath, meta.sfxDragReleasePath));

            // Set ke global click clip (prioritas dari JSON)
            if (sfxClickClip != null)
            {
                globalClickClip = sfxClickClip;
                Debug.Log($"[Global SFX] Using click clip from JSON: {globalClickClip.name}");
            }
            else
            {
                Debug.LogWarning("[Global SFX] No click clip from JSON. (globalClickClip masih bisa diisi via Inspector)");
            }
        }
        else
        {
            ButtonData data = JsonUtility.FromJson<ButtonData>(json);
            if (data == null || data.buttons == null)
            {
                Debug.LogError("❌ JSON tidak valid untuk ButtonData.");
                yield break;
            }
            buttons = data.buttons;
        }

        SetupButtons(buttons);
        StartGame();
    }

    private void SetupButtons(List<ButtonItem> buttons)
    {
        gameButtons.Clear();

        foreach (var b in buttons)
        {
            float t = b.time;
            while (gameButtons.ContainsKey(t)) t += 0.001f;
            gameButtons.Add(t, b);
        }

        roundedButtonCount = CalculateButtonCount();
        Debug.Log($"✅ Loaded {buttons.Count} custom buttons.");
    }

    private void CreateButton(float currentTimeMs, ButtonItem data, int buttonNum)
    {
        GameObject obj = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity);

        Transform parent = null;
        var holder = GameObject.FindGameObjectWithTag("GameController");
        parent = holder != null ? holder.transform : transform;

        obj.transform.SetParent(parent, false);

        var ctrl = obj.GetComponent<ButtonController>();
        if (ctrl == null)
        {
            Debug.LogError("❌ buttonPrefab tidak memiliki ButtonController!");
            Destroy(obj);
            return;
        }

        if (ctrl.startButtonText != null) ctrl.startButtonText.text = buttonNum.ToString();
        if (data.isDrag && ctrl.endButtonText != null) ctrl.endButtonText.text = (buttonNum + 1).ToString();

        ctrl.duration = timingWindowMs;

        // ❌ Tidak lagi set clip per button. Kita pakai globalSfxSource saja.
        // ❌ Tidak perlu membuat AudioSource per button untuk SFX.

        ctrl.InitializeButton(currentTimeMs, data.position[0], data.position[1], data.isDrag,
            data.endPosition[0], data.endPosition[1]);

        if (isPaused)
            ButtonController.SetPaused(true);
    }

    // Dipanggil setiap ButtonController memicu OnClicked (klik atau timeout).
    private void OnGameButtonClick(ButtonController btn)
    {
        // Score update (tetap sama)
        int scoreGain = Mathf.RoundToInt((btn.buttonScore * 1000) / 100) * 100;
        currentScore += scoreGain;
        if (scoreLabel != null) scoreLabel.text = currentScore.ToString();

        // 🔊 Mainkan SFX global hanya jika klik berhasil (buttonScore > 0) 
        //    atau kalau kamu ingin Miss juga bunyi, set playClickOnMiss=true.
        bool isSuccess = btn.buttonScore > 0.0001f;
        if ((isSuccess || playClickOnMiss) && globalSfxSource != null && globalClickClip != null)
        {
            globalSfxSource.PlayOneShot(globalClickClip, 1f);
        }
    }

    private int CalculateButtonCount()
    {
        int count = gameButtons.Count;
        int nearestMultiple = Mathf.RoundToInt(count / 4f) * 4;
        return nearestMultiple - 1;
    }

    private IEnumerator HandleGameEnd()
    {
        gameRunning = false;

        yield return new WaitForSeconds(delayBeforeScoreScene);
        yield return StartCoroutine(FadeOutMusic(1f));

        PlayerPrefs.SetInt("FinalScore", currentScore);
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        Debug.Log("🎮 Game end, loading score scene...");
        SceneManager.LoadScene("ScoreScene");
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        AudioSource audio = musicController != null ? musicController.audio : null;
        if (audio == null)
        {
            Debug.LogError("AudioSource not found!");
            yield break;
        }

        float startVolume = audio.volume;
        while (audio.volume > 0f)
        {
            audio.volume -= startVolume * (Time.deltaTime / duration);
            yield return null;
        }
        audio.Stop();
    }

    private void OnDestroy()
    {
        ButtonController.OnClicked -= OnGameButtonClick;
        ButtonController.SetPaused(false);
    }

    // ====== PAUSE CONTROL ======
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        if (musicController != null && musicController.audio != null)
            musicController.audio.Pause();

        if (videoPlayer == null) videoPlayer = FindObjectOfType<VideoPlayer>();
        if (videoPlayer != null)
            videoPlayer.Pause();

        ButtonController.SetPaused(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        if (musicController != null && musicController.audio != null)
            musicController.audio.UnPause();

        if (videoPlayer == null) videoPlayer = FindObjectOfType<VideoPlayer>();
        if (videoPlayer != null)
        {
            double target = (musicController != null && musicController.audio != null)
                ? musicController.audio.time
                : videoPlayer.time;
            videoPlayer.time = target;
            videoPlayer.Play();
        }

        ButtonController.SetPaused(false);
    }

    // =========================
    // SFX loading helpers
    // =========================

    private IEnumerator LoadAllSfx(string clickName, string pressName, string releaseName)
    {
        // hanya klik yang dipakai untuk versi global
        yield return StartCoroutine(LoadClipFromStreamingAssets(clickName,   c => { sfxClickClip = c; Debug.Log("SFX Click loaded: " + (c ? c.name : "null")); }));
        yield return StartCoroutine(LoadClipFromStreamingAssets(pressName,   c => { sfxDragPressClip = c; }));
        yield return StartCoroutine(LoadClipFromStreamingAssets(releaseName, c => { sfxDragReleaseClip = c; }));

        Debug.Log($"🎧 SFX ready: click={(sfxClickClip ? sfxClickClip.name : "none")}");
    }

    private IEnumerator LoadClipFromStreamingAssets(string fileName, System.Action<AudioClip> setClip)
    {
        if (string.IsNullOrEmpty(fileName)) { setClip?.Invoke(null); yield break; }

        string full = Path.Combine(Application.streamingAssetsPath, fileName);
        full = full.Replace("\\", "/"); // penting di Windows
        string url = (full.StartsWith("file://") || full.StartsWith("http")) ? full : "file:///" + full;

        Debug.Log($"[SFX Loader] Trying to load: {url}");

        var at = GetAudioTypeFromExt(Path.GetExtension(fileName));

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, at))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isHttpError || req.isNetworkError)
#endif
            {
                Debug.LogWarning($"⚠️ Gagal load SFX {fileName}: {req.error}");
                setClip?.Invoke(null);
            }
            else
            {
                var clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip == null)
                {
                    Debug.LogError($"❌ Clip {fileName} ter-load tapi null (format tidak didukung?)");
                }
                else
                {
                    clip.name = fileName;
                    Debug.Log($"✅ Clip loaded: {clip.name}, samples={clip.samples}, freq={clip.frequency}");
                }
                setClip?.Invoke(clip);
            }
        }
    }

    private AudioType GetAudioTypeFromExt(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return AudioType.WAV;
        ext = ext.ToLowerInvariant();
        switch (ext)
        {
            case ".wav": return AudioType.WAV;
            case ".ogg": return AudioType.OGGVORBIS;
            case ".mp3": return AudioType.MPEG;
            case ".aif":
            case ".aiff": return AudioType.AIFF;
            default: return AudioType.WAV;
        }
    }

    [System.Serializable]
    private class SongMetaMaybe
    {
        public string title;
        public string artist;
        public string audioPath;
        public string videoPath;
        public string menuBackgroundPath;
        public List<ButtonItem> bitmapData;

        public string sfxClickPath;        // ← dipakai untuk globalClickClip
        public string sfxDragPressPath;    // (tidak dipakai di versi global)
        public string sfxDragReleasePath;  // (tidak dipakai di versi global)
    }
}
