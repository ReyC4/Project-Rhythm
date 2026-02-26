using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class SongManager : MonoBehaviour
{
    [Header("UI")]
    public Transform songListContainer;
    public GameObject songEntryPrefab;
    public GameObject panelSongManager;
    public Button openSongManagerButton;
    public Button closeSongManagerButton;
    public Button addSongButton;

    [Header("Scene")]
    public string mapperSceneName = "MapperScene";

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

    private string FolderPath => Path.Combine(Application.streamingAssetsPath, "Bitmap");

    void Start()
    {
        // Pastikan panel mati dulu
        panelSongManager.SetActive(false);

        openSongManagerButton.onClick.AddListener(() =>
        {
            RefreshSongList();
            panelSongManager.SetActive(true);
        });

        closeSongManagerButton.onClick.AddListener(() =>
        {
            panelSongManager.SetActive(false);
        });

        addSongButton.onClick.AddListener(() =>
        {
            Debug.Log("▶️ Pindah ke Mapper...");
            SceneManager.LoadScene(mapperSceneName);
        });
    }

    public void RefreshSongList()
    {
        // Bersihkan list lama
        foreach (Transform child in songListContainer)
        {
            Destroy(child.gameObject);
        }

        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }

        string[] jsonFiles = Directory.GetFiles(FolderPath, "*.json");

        Debug.Log("---- Refresh Song List ----");
        if (jsonFiles.Length == 0)
        {
            Debug.Log("⚠️ Tidak ada file JSON di " + FolderPath);
            return;
        }

        foreach (string filePath in jsonFiles)
        {
            Debug.Log($"File ditemukan: {filePath}");

            string json = File.ReadAllText(filePath);
            SongMetadata meta = JsonUtility.FromJson<SongMetadata>(json);

            GameObject entryObj = Instantiate(songEntryPrefab, songListContainer);

            TMP_Text label = entryObj.GetComponentInChildren<TMP_Text>();
            label.text = !string.IsNullOrEmpty(meta.title)
                ? meta.title
                : Path.GetFileNameWithoutExtension(filePath);

            // Cari tombol delete
            Button deleteButton = null;
            foreach (Button b in entryObj.GetComponentsInChildren<Button>())
            {
                if (b.name.ToLower().Contains("delete"))
                {
                    deleteButton = b;
                    break;
                }
            }

            if (deleteButton != null)
            {
                Debug.Log($"✅ Tombol Delete ditemukan untuk {meta.title}");

                // Simpan path per entry
                string capturedPath = filePath;

                deleteButton.onClick.AddListener(() =>
                {
                    Debug.Log($"[DELETE] Coba hapus: {capturedPath}");

                    if (File.Exists(capturedPath))
                    {
                        File.Delete(capturedPath);
                        Debug.Log($"✅ File berhasil dihapus: {capturedPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ File tidak ditemukan saat akan dihapus: {capturedPath}");
                    }

                    RefreshSongList();
                });
            }
            else
            {
                Debug.LogWarning("⚠️ Tidak ditemukan tombol Delete di prefab.");
            }
        }
    }
}
