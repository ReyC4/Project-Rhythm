using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("Dependencies")]
    public MusicController musicController;
    public GameObject buttonPrefab;
    public TMP_Text scoreLabel;

    [Header("Settings")]
    public string gameDataFileName;
    public float gameSpeed = 1f;
    public float delayBeforeScoreScene = 2f;
    public bool loadDefaultData = true;

    [Header("Timing Offset")]
    [Tooltip("Berapa milidetik tombol muncul lebih awal dari waktu audio. Contoh: 1000 = 1 detik lebih awal.")]
    public float spawnOffsetMs = 0f;

    private int gameScore = 0;
    private int roundedButtonCount;
    private SortedList<float, ButtonItem> gameButtons = new SortedList<float, ButtonItem>();

    private bool gameStarted = false;
    private bool isPaused = false;
    private long pauseStartTime = 0;
    private long totalPausedTime = 0;

    void Start()
    {
        // Reset perfect streak SETIAP kali scene gameplay mulai
        ButtonController.ResetPerfectStreak();

        ButtonController.OnClicked += OnGameButtonClick;

        if (loadDefaultData)
        {
            StartCoroutine(LoadGameData());
        }
    }

    void Update()
    {
        if (!gameStarted || isPaused) return;

        // Gunakan waktu audio untuk sinkronisasi
        float currentTime = musicController.audio.time * 1000f;
        float adjustedTime = currentTime + spawnOffsetMs;

        if (gameButtons.Count > 0 && adjustedTime > gameButtons.Keys[0])
        {
            float keyTime = gameButtons.Keys[0];
            int buttonNum = 4 - Mathf.Abs(roundedButtonCount) % 4;

            CreateButton(currentTime, gameButtons[keyTime].position,
                gameButtons[keyTime].isDrag, gameButtons[keyTime].endPosition, buttonNum);

            if (gameButtons[keyTime].isDrag)
                roundedButtonCount--;

            gameButtons.Remove(keyTime);
            roundedButtonCount--;
        }
        else if (gameStarted && gameButtons.Count == 0)
        {
            PlayerPrefs.SetInt("FinalScore", gameScore);
            PlayerPrefs.Save();
            StartCoroutine(GoToScoreSceneAfterFade(delayBeforeScoreScene));
        }
    }

    private IEnumerator LoadGameData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, gameDataFileName);
        string dataAsJson = "";

#if UNITY_WEBGL
        UnityWebRequest request = UnityWebRequest.Get(filePath);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError("Gagal memuat data game (WebGL): " + request.error);
            yield break;
        }

        dataAsJson = request.downloadHandler.text;
#else
        if (!File.Exists(filePath))
        {
            UnityEngine.Debug.LogError("File tidak ditemukan: " + filePath);
            yield break;
        }

        dataAsJson = File.ReadAllText(filePath);
#endif

        ButtonData buttonData = JsonUtility.FromJson<ButtonData>(dataAsJson);

        for (int i = 0; i < buttonData.buttons.Count; ++i)
        {
            gameButtons.Add(buttonData.buttons[i].time, buttonData.buttons[i]);
        }

        roundedButtonCount = ButtonCountInitializer();

#if UNITY_WEBGL || UNITY_EDITOR
        FindObjectOfType<VideoControl>()?.ShowVideo();
#endif

        StartCoroutine(PlayMusicOnDelay(2f));
        UnityEngine.Debug.Log("Jumlah tombol dimuat: " + buttonData.buttons.Count);
    }

    public void LoadCustomMapping(List<ButtonItem> buttons)
    {
        // Reset streak saat load chart custom juga, supaya perhitungan fair
        ButtonController.ResetPerfectStreak();

        gameButtons = new SortedList<float, ButtonItem>();
        foreach (var b in buttons)
        {
            gameButtons.Add(b.time, b);
        }

        roundedButtonCount = ButtonCountInitializer();

#if UNITY_WEBGL || UNITY_EDITOR
        FindObjectOfType<VideoControl>()?.ShowVideo();
#endif

        StartCoroutine(PlayMusicOnDelay(2f));
        UnityEngine.Debug.Log("✅ Custom mapping dimuat: " + gameButtons.Count + " tombol");
    }

    private IEnumerator PlayMusicOnDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        // Double-safety: reset streak persis sebelum gameplay dimulai/lagu diputar
        ButtonController.ResetPerfectStreak();

        musicController.PlayAudio();
        gameStarted = true;
    }

    private IEnumerator FadeOutMusic(float seconds)
    {
        AudioSource audio = musicController.audio;
        if (audio == null)
        {
            UnityEngine.Debug.LogError("AudioSource tidak ditemukan pada MusicController!");
            yield break;
        }

        float startVol = audio.volume;

        while (audio.volume > 0)
        {
            audio.volume -= startVol * (Time.deltaTime / seconds);
            yield return null;
        }

        audio.Stop();
    }

    private IEnumerator GoToScoreSceneAfterFade(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        yield return StartCoroutine(FadeOutMusic(1f));

        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        SceneManager.LoadScene("ScoreScene");
    }

    public void CreateButton(float startTime, float[] startPos, bool isDrag, float[] endPos, int buttonNum)
    {
        GameObject button = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity);
        button.transform.SetParent(GameObject.FindGameObjectWithTag("GameController").transform, false);
        ButtonController buttonController = button.GetComponent<ButtonController>();

        buttonController.startButtonText.text = buttonNum.ToString();
        if (isDrag)
        {
            buttonController.endButtonText.text = (buttonNum + 1).ToString();
        }

        buttonController.duration = gameSpeed; // (pastikan ini memang yang kamu maksud)
        buttonController.InitializeButton(startTime, startPos[0], startPos[1], isDrag, endPos[0], endPos[1]);
    }

    public void OnGameButtonClick(ButtonController button)
    {
        gameScore += (Mathf.RoundToInt((button.buttonScore * 1000) / 100) * 100);
        UpdateScoreLabel(gameScore);
    }

    private void UpdateScoreLabel(int scoreValue)
    {
        scoreLabel.text = scoreValue.ToString();
    }

    private int ButtonCountInitializer()
    {
        int count = gameButtons.Count;
        int nearestMultiple = Mathf.RoundToInt(count / 4f) * 4;
        return nearestMultiple - 1;
    }

    public void OnPauseButtonPressed()
    {
        if (isPaused) return;

        isPaused = true;
        pauseStartTime = (long)(musicController.audio.time * 1000f);
        Time.timeScale = 0f;
        musicController.audio.Pause();
    }

    public void OnResumeButtonPressed()
    {
        if (!isPaused) return;

        isPaused = false;
        long pauseDuration = (long)(musicController.audio.time * 1000f) - pauseStartTime;
        totalPausedTime += pauseDuration;
        Time.timeScale = 1f;
        musicController.audio.Play();
    }

    private void OnDestroy()
    {
        ButtonController.OnClicked -= OnGameButtonClick;
    }
}
