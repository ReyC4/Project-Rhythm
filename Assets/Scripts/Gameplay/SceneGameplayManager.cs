using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SceneGameplayManager : MonoBehaviour
{
    [System.Serializable]
    public class SongPanel
    {
        [Tooltip("Nama lagu default untuk panel ini (hanya dipakai untuk default songs)")]
        public string songName;
        [Tooltip("Panel UI default song (berisi AudioSource/Video bila perlu)")]
        public GameObject panel;
    }

    [Header("Default Song Panels (opsional)")]
    public SongPanel[] songPanels;

    [Header("Scene Names")]
    public string menuSceneName = "SelectionMenu";

    [Header("Pause UI")]
    public GameObject pausePanel;

    [Header("Key Bindings")]
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode restartKey = KeyCode.R;

    private bool isPaused = false;
    private readonly List<VideoPlayer> _videosToResume = new List<VideoPlayer>();
    private readonly List<AudioSource> _audiosToResume = new List<AudioSource>();

    void Start()
    {
        if (pausePanel) pausePanel.SetActive(false);

        int index = GameData.selectedSongIndex;
        if (index < 0) return;

        if (songPanels == null || index >= songPanels.Length) return;

        foreach (var s in songPanels)
            if (s?.panel) s.panel.SetActive(false);

        var selected = songPanels[index];
        if (selected?.panel)
        {
            selected.panel.SetActive(true);
            var audio = selected.panel.GetComponent<AudioSource>();
            if (audio) audio.Play();

            PlayerPrefs.SetString("SelectedSong", 
                string.IsNullOrEmpty(selected.songName) ? $"Default {index + 1}" : selected.songName);
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }
    }

    // ================== UI BUTTON ==================
    // Dipanggil dari tombol pause di UI
    public void OnPauseButtonClick()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    // Dipanggil dari tombol resume di UI (panel pause)
    public void OnResumeButtonClick()
    {
        ResumeGame();
    }

    // Dipanggil dari tombol restart di UI (panel pause)
    public void OnRestartButtonClick()
    {
        RestartGame();
    }

    // Dipanggil dari tombol exit di UI (panel pause)
    public void OnExitButtonClick()
    {
        ExitToMenu();
    }

    // ================== PAUSE / RESUME ==================
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;

        var gc = FindObjectOfType<GameVersiController>();
        if (gc) gc.PauseGame();

        PauseAllAudios();
        PauseAllVideos();

        if (pausePanel) pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        var gc = FindObjectOfType<GameVersiController>();
        if (gc) gc.ResumeGame();

        ResumeAudiosThatWerePlaying();
        ResumeVideosThatWerePlaying();

        Time.timeScale = 1f;

        if (pausePanel) pausePanel.SetActive(false);
    }

    // ================== RESTART / EXIT ==================
    public void RestartGame()
    {
        Time.timeScale = 1f;
        var gc = FindObjectOfType<GameVersiController>();
        if (gc) gc.ResumeGame();

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        var gc = FindObjectOfType<GameVersiController>();
        if (gc) gc.ResumeGame();

        PlayerPrefs.SetInt("GoToSongSelect", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(menuSceneName);
    }

    // ================== HELPERS: AUDIO / VIDEO ==================
    private void PauseAllVideos()
    {
        _videosToResume.Clear();
        var vids = FindObjectsOfType<VideoPlayer>(includeInactive: false);
        foreach (var vp in vids)
        {
            if (!vp) continue;
            if (vp.isPlaying)
            {
                _videosToResume.Add(vp);
                vp.Pause();
            }
        }
    }

    private void ResumeVideosThatWerePlaying()
    {
        foreach (var vp in _videosToResume)
            if (vp) vp.Play();

        _videosToResume.Clear();
    }

    private void PauseAllAudios()
    {
        _audiosToResume.Clear();
        var audios = FindObjectsOfType<AudioSource>(includeInactive: false);
        foreach (var a in audios)
        {
            if (!a) continue;
            if (a.isPlaying)
            {
                _audiosToResume.Add(a);
                a.Pause();
            }
        }
    }

    private void ResumeAudiosThatWerePlaying()
    {
        foreach (var a in _audiosToResume)
            if (a) a.UnPause();

        _audiosToResume.Clear();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
