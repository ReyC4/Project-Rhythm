using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using TMPro;

public class MappingModeController : MonoBehaviour
{
    public TMP_Text positionLabel;
    public TMP_Text timerLabel;
    public TMP_Text playLabel;
    public InputField speedInput;
    public AudioSource audio;

    public GameObject buttonPrefab;
    public GameObject panelMapper;
    public GameObject panelNaming;
    public float scaleSensitivity;

    public KeyCode additionalKey = KeyCode.Q;

    private float curTime;
    private float[] curPosition = new float[2];
    private MappingButton curButton;
    private Vector3 lastPosition = new Vector3();

    private System.Diagnostics.Stopwatch mapTimer = new System.Diagnostics.Stopwatch();

    private SortedDictionary<float, ButtonItem> mappedButtons = new SortedDictionary<float, ButtonItem>();
    private SortedList<float, MappingButton> displayingButtons = new SortedList<float, MappingButton>();

    private void Start()
    {
        SetupAudioClip();
    }

    private void Awake()
    {
        speedInput.text = "1";
    }

    private void Update()
    {
        UpdatePositionLabel(Input.mousePosition.x, Input.mousePosition.y);
        UpdateTimerLabel();

        if (mapTimer.IsRunning && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(additionalKey)))
        {
            curPosition[0] = Input.mousePosition.x;
            curPosition[1] = Input.mousePosition.y;
            curTime = mapTimer.ElapsedMilliseconds;

            bool isSlider = true;
            curButton = CreateButton(curPosition[0], curPosition[1], isSlider);
            lastPosition = Input.mousePosition;
        }

        if ((Input.GetMouseButton(0) || Input.GetKey(additionalKey)) && curButton != null)
        {
            Vector3 diffPos = Input.mousePosition - lastPosition;
            curButton.dragRegion.transform.localScale += new Vector3(((Mathf.Abs(diffPos.x) + Mathf.Abs(diffPos.y)) * 1 / 60f * scaleSensitivity), 0f, 0f);
            curButton.dragRegion.transform.position += (diffPos * scaleSensitivity * 0.75f);

            curButton.InitializeLastButton(Input.mousePosition.x, Input.mousePosition.y);
            lastPosition = Input.mousePosition;
        }
        else if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(additionalKey)) && curButton != null)
        {
            ButtonItem button = new ButtonItem
            {
                time = curTime + GetSpeedInputVal(),
                position = new float[] { curPosition[0], curPosition[1] },
                endPosition = new float[] { lastPosition.x, lastPosition.y },
                isDrag = curButton.isDrag
            };

            mappedButtons.Add(button.time, button);
            curButton = null;
            lastPosition = Vector3.zero;
        }

        if (curButton != null && curButton.buttonTimer.ElapsedMilliseconds > curButton.duration)
        {
            ButtonItem button = new ButtonItem
            {
                time = curTime + GetSpeedInputVal(),
                position = new float[] { curPosition[0], curPosition[1] },
                endPosition = new float[] { lastPosition.x, lastPosition.y },
                isDrag = curButton.isDrag
            };

            mappedButtons.Add(button.time, button);
            curButton.DestroyButton();
            curButton = null;
            lastPosition = Vector3.zero;
        }
    }

    private MappingButton CreateButton(float x, float y, bool isSlider = false)
    {
        GameObject button = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity);
        button.transform.SetParent(this.transform, false);

        MappingButton mappingButton = button.GetComponent<MappingButton>();
        mappingButton.duration = GetSpeedInputVal();
        mappingButton.isDrag = isSlider;
        mappingButton.SetType(isSlider);
        mappingButton.InitializeFirstButton(x, y);

        displayingButtons.Add(mapTimer.ElapsedMilliseconds, mappingButton);
        return mappingButton;
    }

    private void UpdatePositionLabel(float x, float y)
    {
        positionLabel.text = $"x: {x}, y: {y}";
    }

    private void UpdateTimerLabel()
    {
        timerLabel.text = System.TimeSpan.FromMilliseconds(mapTimer.ElapsedMilliseconds).ToString();
    }

    public void OnPlayButtonPress()
    {
        if (mapTimer.IsRunning)
        {
            audio?.Pause();
            mapTimer.Stop();
            playLabel.text = "Start";
        }
        else
        {
            audio?.Play();
            speedInput.gameObject.SetActive(false);
            mapTimer.Start();
            playLabel.text = "Stop";
        }
    }

    public void RestartMapping()
    {
        if (audio.isPlaying)
            audio.Stop();
        audio.time = 0;

        mapTimer.Reset();
        playLabel.text = "Start";
        speedInput.gameObject.SetActive(true);

        foreach (var kvp in displayingButtons)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }

        displayingButtons.Clear();
        mappedButtons.Clear();
        curButton = null;
        lastPosition = Vector3.zero;
        timerLabel.text = "00:00:00";
        positionLabel.text = "x: 0, y: 0";

        UnityEngine.Debug.Log("🔁 Mapping di-reset ulang.");
    }

    public void OnFinishMapping()
    {
        ButtonData buttonData = new ButtonData();
        foreach (var pair in mappedButtons)
        {
            buttonData.buttons.Add(pair.Value);
        }

        GameData.TempMapping = buttonData.buttons;
        UnityEngine.Debug.Log("✅ Mapping disimpan ke memory.");

        panelMapper.SetActive(false);
        panelNaming.SetActive(true);
    }

    private float GetSpeedInputVal()
    {
        float speed = 1f;
        float.TryParse(speedInput.text, out speed);
        return speed * 1000;
    }

    private void SetupAudioClip()
    {
        string defaultFolder = Application.streamingAssetsPath;
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", defaultFolder, extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            StartCoroutine(LoadAudioFromFile(paths[0]));
        }
        else
        {
            UnityEngine.Debug.LogWarning("❌ Tidak ada file audio dipilih.");
        }
    }

    private IEnumerator LoadAudioFromFile(string path)
    {
        string url = "file://" + path;
        using (WWW www = new WWW(url))
        {
            yield return www;

            AudioClip clip = www.GetAudioClip(false, false);
            if (clip != null)
            {
                audio.clip = clip;
                UnityEngine.Debug.Log("✅ AudioClip berhasil dimuat: " + Path.GetFileName(path));
            }
            else
            {
                UnityEngine.Debug.LogError("❌ Gagal memuat AudioClip dari: " + path);
            }
        }
    }
}
