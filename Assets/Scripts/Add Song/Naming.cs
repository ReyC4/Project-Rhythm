using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SFB;
using System.IO;
using System.Collections.Generic;
using TMPro;
using System;

public class NamingPanel : MonoBehaviour
{
    public static NamingPanel Instance;

    [Header("Input Fields")]
    public TMP_InputField inputSongTitle;
    public TMP_InputField inputArtistName;

    [Header("Buttons")]
    public Button buttonPickAudio;
    public Button buttonPickVideo;
    public Button buttonPickMenuBackground;

    // === Baru: tombol pilih SFX ===
    public Button buttonPickSfxClick;
    public Button buttonPickSfxDragPress;
    public Button buttonPickSfxDragRelease;

    public Button buttonSave;

    [Header("Labels")]
    public TMP_Text labelAudioFile;
    public TMP_Text labelVideoFile;
    public TMP_Text labelMenuBackgroundFile;

    // === Baru: label SFX ===
    public TMP_Text labelSfxClickFile;
    public TMP_Text labelSfxDragPressFile;
    public TMP_Text labelSfxDragReleaseFile;

    // path sumber (dipilih user)
    private string audioSrcPath;
    private string videoSrcPath;
    private string menuBgSrcPath;

    // === Baru: path sumber SFX (opsional) ===
    private string sfxClickSrcPath;
    private string sfxDragPressSrcPath;
    private string sfxDragReleaseSrcPath;

    void Awake() { Instance = this; }

    void Start()
    {
        if (buttonPickAudio)          buttonPickAudio.onClick.AddListener(OnPickAudio);
        if (buttonPickVideo)          buttonPickVideo.onClick.AddListener(OnPickVideo);
        if (buttonPickMenuBackground) buttonPickMenuBackground.onClick.AddListener(OnPickMenuBackground);

        // === Baru: listener tombol SFX ===
        if (buttonPickSfxClick)       buttonPickSfxClick.onClick.AddListener(OnPickSfxClick);
        if (buttonPickSfxDragPress)   buttonPickSfxDragPress.onClick.AddListener(OnPickSfxDragPress);
        if (buttonPickSfxDragRelease) buttonPickSfxDragRelease.onClick.AddListener(OnPickSfxDragRelease);

        if (buttonSave)               buttonSave.onClick.AddListener(OnSave);
    }

    public void OnPickAudio()
    {
        var exts = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        string def = Application.streamingAssetsPath;
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", def, exts, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            audioSrcPath = paths[0];
            if (labelAudioFile) labelAudioFile.text = Path.GetFileName(audioSrcPath);
        }
    }

    public void OnPickVideo()
    {
        var exts = new[] { new ExtensionFilter("Video Files", "mp4", "avi", "mov") };
        string def = Application.streamingAssetsPath;
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Gameplay Background Video", def, exts, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            videoSrcPath = paths[0];
            if (labelVideoFile) labelVideoFile.text = Path.GetFileName(videoSrcPath);
        }
    }

    public void OnPickMenuBackground()
    {
        var exts = new[] { new ExtensionFilter("Video Files", "mp4", "avi", "mov") };
        string def = Application.streamingAssetsPath;
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Menu Background Video", def, exts, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            menuBgSrcPath = paths[0];
            if (labelMenuBackgroundFile) labelMenuBackgroundFile.text = Path.GetFileName(menuBgSrcPath);
        }
    }

    // ===========================
    // SFX PICKERS (opsional)
    // ===========================
    public void OnPickSfxClick()
    {
        var exts = new[] { new ExtensionFilter("SFX Audio", "wav", "ogg", "mp3") };
        string def = Application.streamingAssetsPath;
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Button Click SFX", def, exts, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            sfxClickSrcPath = paths[0];
            if (labelSfxClickFile) labelSfxClickFile.text = Path.GetFileName(sfxClickSrcPath);
        }
    }

    public void OnPickSfxDragPress()
    {
        var exts = new[] { new ExtensionFilter("SFX Audio", "wav", "ogg", "mp3") };
        string def = Application.streamingAssetsPath;
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Drag Press SFX", def, exts, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            sfxDragPressSrcPath = paths[0];
            if (labelSfxDragPressFile) labelSfxDragPressFile.text = Path.GetFileName(sfxDragPressSrcPath);
        }
    }

    public void OnPickSfxDragRelease()
    {
        var exts = new[] { new ExtensionFilter("SFX Audio", "wav", "ogg", "mp3") };
        string def = Application.streamingAssetsPath;
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Drag Release SFX", def, exts, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            sfxDragReleaseSrcPath = paths[0];
            if (labelSfxDragReleaseFile) labelSfxDragReleaseFile.text = Path.GetFileName(sfxDragReleaseSrcPath);
        }
    }

    public void OnSave()
    {
        string songTitle  = (inputSongTitle ? inputSongTitle.text.Trim() : "").Trim();
        string artistName = (inputArtistName ? inputArtistName.text.Trim() : "").Trim();

        if (string.IsNullOrEmpty(songTitle))       { Debug.LogWarning("❌ Judul lagu belum diisi!"); return; }
        if (string.IsNullOrEmpty(audioSrcPath))    { Debug.LogWarning("❌ File audio belum dipilih!"); return; }
        if (string.IsNullOrEmpty(menuBgSrcPath))   { Debug.LogWarning("❌ File menu background belum dipilih!"); return; }

        string saRoot    = Application.streamingAssetsPath;
        string bitmapDir = Path.Combine(saRoot, "Bitmap");
        if (!Directory.Exists(bitmapDir)) Directory.CreateDirectory(bitmapDir);

        // Salin ke StreamingAssets root; kalau gagal karena locked, pakai nama baru otomatis.
        string audioFileName = CopyToStreamingAssetsSmart(audioSrcPath, saRoot);
        string videoFileName = string.IsNullOrEmpty(videoSrcPath) ? "" : CopyToStreamingAssetsSmart(videoSrcPath, saRoot);
        string menuFileName  = CopyToStreamingAssetsSmart(menuBgSrcPath, saRoot);

        // === Baru: salin SFX opsional ===
        string sfxClickFileName       = string.IsNullOrEmpty(sfxClickSrcPath)       ? "" : CopyToStreamingAssetsSmart(sfxClickSrcPath, saRoot);
        string sfxDragPressFileName   = string.IsNullOrEmpty(sfxDragPressSrcPath)   ? "" : CopyToStreamingAssetsSmart(sfxDragPressSrcPath, saRoot);
        string sfxDragReleaseFileName = string.IsNullOrEmpty(sfxDragReleaseSrcPath) ? "" : CopyToStreamingAssetsSmart(sfxDragReleaseSrcPath, saRoot);

        SongMetadata metadata = new SongMetadata
        {
            title = songTitle,
            artist = artistName,
            audioPath = audioFileName,     // hanya NAMA FILE
            videoPath = videoFileName,     // hanya NAMA FILE
            menuBackgroundPath = menuFileName, // hanya NAMA FILE
            bitmapData = GameData.TempMapping,

            // === Baru: simpan nama file SFX (boleh kosong) ===
            sfxClickPath = sfxClickFileName,
            sfxDragPressPath = sfxDragPressFileName,
            sfxDragReleasePath = sfxDragReleaseFileName
        };

        string json = JsonUtility.ToJson(metadata, true);

        string safeBase = SafeFileName(songTitle);
        string defaultJsonPath = Path.Combine(bitmapDir, safeBase + ".json");

#if UNITY_EDITOR
        string savePath = StandaloneFileBrowser.SaveFilePanel(
            "Save Song Metadata (JSON)",
            bitmapDir,
            safeBase,
            "json"
        );
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogWarning("❌ Simpan dibatalkan.");
            return;
        }
#else
        string savePath = defaultJsonPath;
#endif

        File.WriteAllText(savePath, json);
        Debug.Log($"✅ Metadata disimpan: {savePath}");
        SceneManager.LoadScene("Game Menu");
    }

    // ===== Helpers =====

    private static string SafeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "file";
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Replace("/", "_").Replace("\\", "_").Trim();
    }

    private static bool PathsEqualCI(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        try
        {
            string fa = Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fb = Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(fa, fb, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>
    /// Copy file ke StreamingAssets root. Kalau gagal overwrite (file in use),
    /// otomatis pakai nama baru: "nama__copy_<ticks>.ext". Return: NAMA FILE tujuan.
    /// Skip copy bila sumber sudah sama dengan tujuan.
    /// </summary>
    private static string CopyToStreamingAssetsSmart(string srcPath, string saRoot)
    {
#if UNITY_WEBGL
        // WebGL: StreamingAssets read-only saat runtime. Asumsikan file sudah ada.
        return Path.GetFileName(srcPath);
#else
        try
        {
            if (string.IsNullOrEmpty(srcPath)) return "";
            if (!File.Exists(srcPath))
            {
                Debug.LogWarning("⚠️ Sumber tidak ditemukan: " + srcPath);
                return Path.GetFileName(srcPath);
            }

            string fileName = SafeFileName(Path.GetFileName(srcPath));
            string dstPath  = Path.Combine(saRoot, fileName);

            // kalau sumber == tujuan (misal user pilih file yang sudah ada di SA) → skip
            if (PathsEqualCI(srcPath, dstPath))
            {
                Debug.Log($"↪️ Skip copy (sudah di StreamingAssets): {fileName}");
                return fileName;
            }

            // coba overwrite langsung
            try
            {
                Directory.CreateDirectory(saRoot);
                File.Copy(srcPath, dstPath, overwrite: true);
                Debug.Log($"📦 Copied: {fileName}");
                return fileName;
            }
            catch (IOException ioEx)
            {
                // kemungkinan locked → gunakan nama baru
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                string ext      = Path.GetExtension(fileName);
                string altName  = $"{baseName}__copy_{DateTime.Now.Ticks}{ext}";
                string altPath  = Path.Combine(saRoot, altName);

                try
                {
                    File.Copy(srcPath, altPath, overwrite: true);
                    Debug.Log($"📦 Copied with new name (locked original): {altName}\nReason: {ioEx.Message}");
                    return altName;
                }
                catch (Exception ex2)
                {
                    Debug.LogError("❌ Gagal menyalin file (bahkan dengan nama baru): " + ex2.Message);
                    return fileName; // tetap kembalikan nama awal agar JSON menyimpan sesuatu
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Gagal menyalin file: " + ex.Message);
            return Path.GetFileName(srcPath);
        }
#endif
    }

    [System.Serializable]
    public class SongMetadata
    {
        public string title;
        public string artist;
        public string audioPath;          // hanya nama file
        public string videoPath;          // hanya nama file (boleh kosong)
        public string menuBackgroundPath; // hanya nama file
        public List<ButtonItem> bitmapData;

        // === Baru: path SFX (opsional, hanya nama file) ===
        public string sfxClickPath;
        public string sfxDragPressPath;
        public string sfxDragReleasePath;
    }
}
