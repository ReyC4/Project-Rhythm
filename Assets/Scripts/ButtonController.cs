using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    // ========= GLOBAL PAUSE =========
    public static bool IsPaused { get; private set; } = false;

    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;

        foreach (var inst in _instances)
        {
            if (inst == null) continue;
            if (paused) inst.OnPaused();
            else        inst.OnResumed();
        }
    }
    private static readonly HashSet<ButtonController> _instances = new HashSet<ButtonController>();

    // ========= REFS =========
    [Header("Refs")]
    public Button startButton;
    public Button endButton;
    public Image dragRegion;
    public Text startButtonText;
    public Text endButtonText;
    public Image indicator;
    public IndicatorCollision indicatorCollision;

    // ========= TIMING & STATE =========
    [Header("Timing (ms)")]
    public float duration = 800f; // ms
    public float buttonScore = 0f;

    private float spawnTimeSec = 0f;
    private bool isDrag = false;
    private bool beginDragEvent = false;
    private bool wasClicked = false;

    [Header("Input")]
    public KeyCode inputKey = KeyCode.E;

    public delegate void ButtonClick(ButtonController button);
    public static event ButtonClick OnClicked;

    // ========= SCORING =========
    [Header("Scoring")]
    [Range(0f, 1f)] public float perfectThreshold = 0.85f;
    [Range(0f, 1f)] public float greatThreshold   = 0.60f;

    // ========= PRESS EFFECT =========
    [Header("Press Effect Settings")]
    public float pressScale = 0.9f;
    public float pressDuration = 0.08f;
    public AnimationCurve pressEase;

    // ========= FADE + ZOOM =========
    [Header("Fade Zoom Settings")]
    public float fadeDuration = 0.45f;
    public float zoomScale = 1.35f;
    public float textRise = 50f;
    public AnimationCurve fadeEase;

    // ========= AFTER IMAGE (DRAG ONLY) =========
    [Header("After Image (Drag Only)")]
    public bool enableAfterImage = true;
    public float afterImageInterval = 0.03f;
    public float afterImageLifetime = 0.6f;
    [Range(0f, 1f)] public float afterImageStartAlpha = 0.6f;
    public float afterImageEndScale = 1.2f;
    public Sprite afterImageOverrideSprite;

    // ========= RESULT TINTING =========
    [Header("Result Tinting")]
    public bool enableResultTint = true;
    public Color perfectTint = new Color(0.35f, 1f, 0.35f, 1f);
    public Color greatTint   = new Color(1f, 0.9f, 0.35f, 1f);
    public Color missTint    = new Color(1f, 0.4f, 0.4f, 1f);
    public bool tintStartButton = true;
    public bool tintEndButton = true;
    public bool tintDragRegion = false;
    public bool tintIndicator = false;
    public bool tintText = false;

    // ========= DRAG VISUAL =========
    [Header("Drag Visual (Slider Brightness)")]
    public bool controlDragBrightness = true;
    [Range(0f, 1f)] public float dragIdleAlpha    = 0.35f;
    [Range(0f, 1f)] public float dragPressedAlpha = 0.95f;
    public float dragAlphaLerpDuration = 0.12f;
    public bool affectDragRegionAlpha = true;
    public bool affectIndicatorAlpha = true;

    private Coroutine afterImageCo;
    private Coroutine moveIndicatorCo;
    private Coroutine dragAlphaCo;

    // ========= SFX =========
    [Header("SFX (In-Game)")]
    public AudioSource sfx;
    [Range(0f,1f)] public float sfxVolume = 1f;
    public Vector2 randomPitchRange = new Vector2(0.98f, 1.02f);
    public AudioClip clickClip;
    public AudioClip dragPressClip;
    public AudioClip dragReleaseClip;

    [Header("SFX Speed Control")]
    [Range(0.5f, 2f)] public float sfxSpeed = 1f;
    private bool releaseSoundPlayed = false;

    // ========= SCORE UI SHAKE REF =========
    [Header("Score UI Shake (assign dari scene)")]
    public UIScoreShake scoreShake;  // drag komponen UIScoreShake (Score HUD)

    private UIScoreShake GetScoreShake()
    {
        if (scoreShake != null) return scoreShake;
        scoreShake = FindObjectOfType<UIScoreShake>();
        return scoreShake;
    }

    // ========= SCREEN SHAKE (opsional) =========
    private enum ShakeType { Perfect, Great, Miss }

    [Header("Screen Shake")]
    public bool enableShake = true;
    public bool shakeOnPerfect = true;
    public float shakeDurationPerfect = 0.15f;
    public float shakeMagnitudePerfect = 0.22f;

    public bool shakeOnGreat = true;
    public float shakeDurationGreat = 0.10f;
    public float shakeMagnitudeGreat = 0.12f;

    public bool shakeOnMiss = true;
    public float shakeDurationMiss = 0.06f;
    public float shakeMagnitudeMiss = 0.06f;

    private Coroutine localShakeCo;
    private Coroutine uiRootShakeCo;

    // ========= PERFECT STREAK =========
    [Header("Perfect Streak → Score Shake")]
    [Tooltip("Berapa kali Perfect beruntun untuk memicu shake pada UI skor.")]
    public int perfectStreakTarget = 5;

    // disatukan lintas instance; WAJIB di-reset saat mulai level/song
    private static int _globalPerfectStreak = 0;

    public static void ResetPerfectStreak()
    {
        _globalPerfectStreak = 0;
#if UNITY_EDITOR
        Debug.Log("[ButtonController] Perfect streak reset.");
#endif
    }

    private void OnEnable()  { _instances.Add(this); }
    private void OnDisable() { _instances.Remove(this); }

    private void OnValidate()
    {
        if (perfectThreshold < greatThreshold) perfectThreshold = greatThreshold;
        if (afterImageInterval < 0.005f) afterImageInterval = 0.005f;
        if (afterImageLifetime < 0.05f) afterImageLifetime = 0.05f;
        if (afterImageEndScale < 1f)    afterImageEndScale = 1f;
        if (duration < 1f)              duration = 1f;

        sfxSpeed = Mathf.Clamp(sfxSpeed, 0.5f, 2f);

        if (shakeDurationPerfect < 0.01f) shakeDurationPerfect = 0.01f;
        if (shakeDurationGreat   < 0.01f) shakeDurationGreat   = 0.01f;
        if (shakeDurationMiss    < 0.01f) shakeDurationMiss    = 0.01f;

        if (perfectStreakTarget < 1) perfectStreakTarget = 1;
    }

    // =============================================================
    // INIT
    // =============================================================
    public void InitializeButton(float /*ignored*/ _, float startX, float startY, bool isDrag, float endX, float endY)
    {
        transform.SetAsFirstSibling();
        transform.position = new Vector3(startX, startY);
        if (startButton != null) startButton.transform.SetParent(transform, false);

        this.isDrag = isDrag;
        releaseSoundPlayed = false;
        beginDragEvent = false;
        wasClicked = false;

        if (this.isDrag)
        {
            SetupDragRegion(startX, endX, startY, endY);

            if (controlDragBrightness)
            {
                if (affectDragRegionAlpha && dragRegion != null)
                    SetAlpha(dragRegion, dragIdleAlpha);
                if (affectIndicatorAlpha && indicator != null)
                    SetAlpha(indicator, dragIdleAlpha);
            }
        }

        if (startButton != null) startButton.gameObject.SetActive(true);

        spawnTimeSec = Time.time;
        StartCoroutine(ScaleIndicator());
    }

    public void SetupDragRegion(float x1, float x2, float y1, float y2)
    {
        if (dragRegion == null || endButton == null) return;

        Vector3 centerPos = new Vector3(x1 + x2, y1 + y2) / 2f;
        float scaleX = Mathf.Abs(x2 - x1);
        float scaleY = Mathf.Abs(y2 - y1);

        dragRegion.transform.localScale = new Vector3((scaleX + scaleY) / 100f, 1f);
        dragRegion.transform.position = centerPos;

        float angle = Mathf.Atan2(y2 - y1, x2 - x1);
        dragRegion.transform.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);

        endButton.transform.SetParent(transform, false);
        endButton.transform.position = new Vector3(x2, y2);

        dragRegion.gameObject.SetActive(true);
        endButton.gameObject.SetActive(true);
    }

    // =============================================================
    // UPDATE
    // =============================================================
    void Update()
    {
        if (IsPaused) return;

        bool inputDown = Input.GetKeyDown(inputKey) || Input.GetMouseButtonDown(0);
        bool inputHeld = Input.GetKey(inputKey)      || Input.GetMouseButton(0);
        bool inputUp   = Input.GetKeyUp(inputKey)    || Input.GetMouseButtonUp(0);

        if (inputDown && indicatorCollision != null && indicatorCollision.isHit &&
            startButton != null && startButton.gameObject.activeSelf && !wasClicked)
        {
            ButtonClicked();
        }

        if (isDrag && beginDragEvent)
        {
            if (inputHeld && indicatorCollision != null && indicatorCollision.isHit)
            {
                if (moveIndicatorCo == null)
                    moveIndicatorCo = StartCoroutine(MoveIndicator());
                buttonScore += 0.05f;
            }

            if (inputUp)
            {
                StopAfterImageSpawner();
                if (moveIndicatorCo != null) { StopCoroutine(moveIndicatorCo); moveIndicatorCo = null; }

                if (!releaseSoundPlayed)
                {
                    PlayOneShot(dragReleaseClip);
                    releaseSoundPlayed = true;
                }

                wasClicked = true;
                OnClicked?.Invoke(this);
                StartCoroutine(FadeAway());
            }
        }

        float elapsedMs = (Time.time - spawnTimeSec) * 1000f;
        if (startButton != null && startButton.gameObject.activeSelf &&
            elapsedMs > duration && !wasClicked)
        {
            StopAfterImageSpawner();
            OnClicked?.Invoke(this);
            StartCoroutine(FadeAway());
        }
    }

    // =============================================================
    // CLICK
    // =============================================================
    public void ButtonClicked()
    {
        if (IsPaused) return;

        // Tidak memanggil score shake di sini (hanya saat streak Perfect tercapai)
        if (isDrag) PlayOneShot(dragPressClip);
        else        PlayOneShot(clickClip);

        if (isDrag) StartCoroutine(ClickSequenceDrag());
        else        StartCoroutine(ClickSequenceNonDrag());
    }

    private IEnumerator ClickSequenceNonDrag()
    {
        yield return StartCoroutine(PressEffect());

        float clickTimeMs = (Time.time - spawnTimeSec) * 1000f;
        buttonScore = CalcScore(clickTimeMs);
        wasClicked = true;
        OnClicked?.Invoke(this);

        StartCoroutine(FadeAway());
    }

    private IEnumerator ClickSequenceDrag()
    {
        yield return StartCoroutine(PressEffect());
        beginDragEvent = true;

        if (moveIndicatorCo == null)
            moveIndicatorCo = StartCoroutine(MoveIndicator());

        if (controlDragBrightness)
        {
            if (dragAlphaCo != null) StopCoroutine(dragAlphaCo);
            dragAlphaCo = StartCoroutine(LerpDragVisualAlpha(dragPressedAlpha, dragAlphaLerpDuration));
        }

        if (enableAfterImage && afterImageCo == null)
            afterImageCo = StartCoroutine(SpawnAfterImages());
    }

    // =============================================================
    // SCORING / ANIMS
    // =============================================================
    public float CalcPerfectTimeMs() => duration / 2f;
    public float CalcScore(float clickTimeMs)
    {
        float perfect = CalcPerfectTimeMs();
        return 1f - Mathf.Abs(clickTimeMs - perfect) / perfect; // 0..1
    }

    private IEnumerator ScaleIndicator()
    {
        if (indicator == null) yield break;

        Vector3 originalScale = indicator.transform.localScale;
        Vector3 destinationScale = new Vector3(0.6f, 0.6f, 0.6f);

        while (((Time.time - spawnTimeSec) * 1000f) < (duration / 2f))
        {
            if (IsPaused) { yield return null; continue; }
            float t = ((Time.time - spawnTimeSec) * 1000f) / (duration / 2f);
            indicator.transform.localScale = Vector3.Lerp(originalScale, destinationScale, t);
            yield return null;
        }
    }

    private IEnumerator MoveIndicator()
    {
        if (indicator == null || endButton == null) yield break;

        Vector3 originalLocation = indicator.transform.position;
        Vector3 destination = endButton.transform.position;

        while (((Time.time - spawnTimeSec) * 1000f) < duration)
        {
            if (IsPaused) { yield return null; continue; }
            float t = ((Time.time - spawnTimeSec) * 1000f) / duration;
            indicator.transform.position = Vector3.Lerp(originalLocation, destination, t);
            yield return null;
        }

        moveIndicatorCo = null;
    }

    private IEnumerator PressEffect()
    {
        Transform t = transform;
        Vector3 startScale = t.localScale;
        Vector3 pressedScale = startScale * Mathf.Clamp(pressScale, 0.5f, 1f);
        float dur = Mathf.Max(0.01f, pressDuration);
        AnimationCurve curve = pressEase ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);

        float elapsed = 0f;
        while (elapsed < dur)
        {
            if (IsPaused) { yield return null; continue; }
            elapsed += Time.deltaTime;
            float k = curve.Evaluate(Mathf.Clamp01(elapsed / dur));
            t.localScale = Vector3.LerpUnclamped(startScale, pressedScale, k);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < dur)
        {
            if (IsPaused) { yield return null; continue; }
            elapsed += Time.deltaTime;
            float k = curve.Evaluate(Mathf.Clamp01(elapsed / dur));
            t.localScale = Vector3.LerpUnclamped(pressedScale, startScale, k);
            yield return null;
        }

        t.localScale = startScale;
    }

    private IEnumerator SpawnAfterImages()
    {
        if (indicator == null) yield break;

        RectTransform indRT = indicator.rectTransform;
        Transform parent = indicator.transform.parent;

        while (beginDragEvent && (Input.GetKey(inputKey) || Input.GetMouseButton(0)))
        {
            if (IsPaused) { yield return null; continue; }

            GameObject ghost = new GameObject("AfterImage");
            ghost.transform.SetParent(parent, false);

            var ghostRT = ghost.AddComponent<RectTransform>();
            ghostRT.anchorMin = indRT.anchorMin;
            ghostRT.anchorMax = indRT.anchorMax;
            ghostRT.pivot     = indRT.pivot;
            ghostRT.sizeDelta = indRT.sizeDelta;
            ghostRT.rotation  = indRT.rotation;
            ghostRT.position  = indRT.position;
            ghostRT.localScale= indRT.localScale;

            var img = ghost.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = afterImageOverrideSprite != null ? afterImageOverrideSprite : indicator.sprite;

            Color baseCol = indicator.color;
            img.color = new Color(baseCol.r, baseCol.g, baseCol.b, afterImageStartAlpha);

            StartCoroutine(FadeAndScaleOut(img, afterImageLifetime, afterImageEndScale));

            yield return new WaitForSeconds(afterImageInterval);
        }

        afterImageCo = null;
    }

    private IEnumerator FadeAndScaleOut(Image img, float lifetime, float endScaleMul)
    {
        if (img == null) yield break;

        RectTransform rt = img.rectTransform;
        float t = 0f;
        float dur = Mathf.Max(0.05f, lifetime);

        Color c0 = img.color;
        Color c1 = new Color(c0.r, c0.g, c0.b, 0f);

        Vector3 s0 = rt.localScale;
        Vector3 s1 = s0 * Mathf.Max(1f, endScaleMul);

        while (t < dur)
        {
            if (IsPaused) { yield return null; continue; }
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            img.color = Color.Lerp(c0, c1, k);
            rt.localScale = Vector3.LerpUnclamped(s0, s1, k);
            yield return null;
        }

        if (img != null) Destroy(img.gameObject);
    }

    private void StopAfterImageSpawner()
    {
        if (afterImageCo != null)
        {
            StopCoroutine(afterImageCo);
            afterImageCo = null;
        }
    }

    private IEnumerator LerpDragVisualAlpha(float targetAlpha, float dur)
    {
        float d = Mathf.Max(0.01f, dur);
        float t = 0f;

        Color? drFrom = null, inFrom = null;
        if (affectDragRegionAlpha && dragRegion != null) drFrom = dragRegion.color;
        if (affectIndicatorAlpha && indicator != null)   inFrom = indicator.color;

        while (t < d)
        {
            if (IsPaused) { yield return null; continue; }
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / d);

            if (drFrom.HasValue && dragRegion != null)
            {
                var c = drFrom.Value;
                dragRegion.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, targetAlpha, k));
            }
            if (inFrom.HasValue && indicator != null)
            {
                var c = inFrom.Value;
                indicator.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, targetAlpha, k));
            }

            yield return null;
        }

        if (affectDragRegionAlpha && dragRegion != null)
            SetAlpha(dragRegion, targetAlpha);
        if (affectIndicatorAlpha && indicator != null)
            SetAlpha(indicator, targetAlpha);

        dragAlphaCo = null;
    }

    private void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        g.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
    }

    public IEnumerator FadeAway()
    {
        if (indicator != null)
        {
            var col = indicator.GetComponent<CircleCollider2D>();
            if (col != null) col.enabled = false;
        }

        StopAfterImageSpawner();

        string resultText;
        Color resultTint = Color.white;

        if (!wasClicked)
        {
            // MISS → reset streak
            if (_globalPerfectStreak != 0)
            {
#if UNITY_EDITOR
                Debug.Log("[ButtonController] Miss → streak reset.");
#endif
            }
            _globalPerfectStreak = 0;

            resultText = "Miss";
            resultTint = missTint;
            TriggerShake(ShakeType.Miss);
        }
        else
        {
            bool isPerfect = (buttonScore >= perfectThreshold);
            bool isGreat   = (!isPerfect && buttonScore >= greatThreshold);

            if (isPerfect)
            {
                _globalPerfectStreak++;
#if UNITY_EDITOR
                Debug.Log($"[ButtonController] Perfect! Streak = {_globalPerfectStreak}/{perfectStreakTarget}");
#endif
                resultText = "Perfect";
                resultTint = perfectTint;
                TriggerShake(ShakeType.Perfect);

                if (_globalPerfectStreak >= Mathf.Max(1, perfectStreakTarget))
                {
                    var ss = GetScoreShake();
                    if (ss != null) ss.ShakeOnce();
                    _globalPerfectStreak = 0; // reset setelah memicu
#if UNITY_EDITOR
                    Debug.Log("[ButtonController] Streak reached target → UI score shake! Streak reset to 0.");
#endif
                }
            }
            else if (isGreat)
            {
                if (_globalPerfectStreak != 0)
                {
#if UNITY_EDITOR
                    Debug.Log("[ButtonController] Great → streak reset.");
#endif
                }
                _globalPerfectStreak = 0;

                resultText = "Great";
                resultTint = greatTint;
                TriggerShake(ShakeType.Great);
            }
            else
            {
                if (_globalPerfectStreak != 0)
                {
#if UNITY_EDITOR
                    Debug.Log("[ButtonController] Miss-ish → streak reset.");
#endif
                }
                _globalPerfectStreak = 0;

                resultText = "Miss";
                resultTint = missTint;
                TriggerShake(ShakeType.Miss);
            }
        }

        if (enableResultTint)
        {
            if (!isDrag && tintStartButton && startButton != null)
                startButton.image.color = resultTint;

            if (isDrag && tintEndButton && endButton != null)
                endButton.image.color = resultTint;

            if (tintDragRegion && dragRegion != null)
                dragRegion.color = resultTint;

            if (tintIndicator && indicator != null)
                indicator.color = resultTint;

            if (tintText)
            {
                if (isDrag && endButtonText != null) endButtonText.color = resultTint;
                else if (!isDrag && startButtonText != null) startButtonText.color = resultTint;
            }
        }

        if (isDrag)
        {
            if (endButtonText != null) endButtonText.text = resultText;
        }
        else
        {
            if (startButtonText != null) startButtonText.text = resultText;
        }

        // ===== Fade & Zoom Out =====
        Color sbCol = startButton != null ? startButton.image.color : Color.white;
        Color ebCol = endButton   != null ? endButton.image.color   : Color.white;
        Color drCol = dragRegion  != null ? dragRegion.color        : Color.white;
        Color inCol = indicator   != null ? indicator.color         : Color.white;

        Color stCol = startButtonText != null ? startButtonText.color : Color.white;
        Color etCol = endButtonText   != null ? endButtonText.color   : Color.white;

        Color sbEnd = new Color(sbCol.r, sbCol.g, sbCol.b, 0f);
        Color ebEnd = new Color(ebCol.r, ebCol.g, ebCol.b, 0f);
        Color drEnd = new Color(drCol.r, drCol.g, drCol.b, 0f);
        Color inEnd = new Color(inCol.r, inCol.g, inCol.b, 0f);
        Color stEnd = new Color(stCol.r, stCol.g, stCol.b, 0f);
        Color etEnd = new Color(etCol.r, etCol.g, etCol.b, 0f);

        Transform t = transform;
        Vector3 scaleStart = t.localScale;
        Vector3 scaleEnd = scaleStart * Mathf.Max(1f, zoomScale);

        Vector3 txtStart, txtEnd;
        if (isDrag && endButtonText != null)
        {
            txtStart = endButtonText.transform.position;
            txtEnd   = txtStart + new Vector3(0f, textRise, 0f);
        }
        else
        {
            txtStart = startButtonText != null ? startButtonText.transform.position : Vector3.zero;
            txtEnd   = txtStart + new Vector3(0f, textRise, 0f);
        }

        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, fadeDuration);
        AnimationCurve curve = fadeEase ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        while (elapsed < dur)
        {
            if (IsPaused) { yield return null; continue; }

            elapsed += Time.deltaTime;
            float e = curve.Evaluate(Mathf.Clamp01(elapsed / dur));

            t.localScale = Vector3.LerpUnclamped(scaleStart, scaleEnd, e);

            if (startButton != null) startButton.image.color = Color.Lerp(sbCol, sbEnd, e);
            if (endButton   != null) endButton.image.color   = Color.Lerp(ebCol, ebEnd, e);
            if (dragRegion  != null) dragRegion.color        = Color.Lerp(drCol, drEnd, e);
            if (indicator   != null) indicator.color         = Color.Lerp(inCol, inEnd, e);

            if (startButtonText != null) startButtonText.color = Color.Lerp(stCol, stEnd, e);
            if (endButtonText   != null) endButtonText.color   = Color.Lerp(etCol, etEnd, e);

            if (isDrag && endButtonText != null)
                endButtonText.transform.position = Vector3.LerpUnclamped(txtStart, txtEnd, e);
            else if (!isDrag && startButtonText != null)
                startButtonText.transform.position = Vector3.LerpUnclamped(txtStart, txtEnd, e);

            yield return null;
        }

        transform.localScale = scaleEnd;
        Destroy(gameObject);
    }

    // ===== Pause hooks =====
    private void OnPaused()
    {
        StopAfterImageSpawner();
        if (sfx != null) sfx.Pause();
    }
    private void OnResumed()
    {
        if (sfx != null) sfx.UnPause();
    }

    // ===== SFX =====
    public void SetSfxSpeed(float speed)
    {
        sfxSpeed = Mathf.Clamp(speed, 0.5f, 2f);
        if (sfx != null) sfx.pitch = sfxSpeed;
    }

    private void PlayOneShot(AudioClip clip, float volumeMul = 1f)
    {
        if (clip == null) return;

        if (sfx == null)
        {
            sfx = gameObject.GetComponent<AudioSource>();
            if (sfx == null) sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.loop = false;
            sfx.spatialBlend = 0f;
        }
        if (!sfx.enabled) sfx.enabled = true;

        float randomMul = Random.Range(randomPitchRange.x, randomPitchRange.y);
        sfx.pitch = sfxSpeed * randomMul;
        sfx.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeMul));
    }

    // ===== SCREEN SHAKE Helpers =====
    private void TriggerShake(ShakeType type)
    {
        if (!enableShake) return;

        bool allowed = false;
        float dur = 0f, mag = 0f;

        switch (type)
        {
            case ShakeType.Perfect:
                allowed = shakeOnPerfect; dur = shakeDurationPerfect; mag = shakeMagnitudePerfect; break;
            case ShakeType.Great:
                allowed = shakeOnGreat;   dur = shakeDurationGreat;   mag = shakeMagnitudeGreat;   break;
            case ShakeType.Miss:
                allowed = shakeOnMiss;    dur = shakeDurationMiss;    mag = shakeMagnitudeMiss;    break;
        }
        if (!allowed) return;

        if (!TrySendMessageShake(dur, mag))
            StartLocalShake(dur, mag);
    }

    // Coba kamera (ScreenShake) & UI (UIShake). Fallback: root Canvas.
    private bool TrySendMessageShake(float duration, float magnitude)
    {
        bool sent = false;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.gameObject.SendMessage("ShakeCustom", new Vector2(duration, magnitude), SendMessageOptions.DontRequireReceiver);
            cam.gameObject.SendMessage("Shake", SendMessageOptions.DontRequireReceiver);
            sent = true;
        }

        var uiShake = FindObjectOfType<UIShake>();
        if (uiShake != null)
        {
            uiShake.gameObject.SendMessage("ShakeCustom", new Vector2(duration, magnitude * 100f), SendMessageOptions.DontRequireReceiver);
            uiShake.gameObject.SendMessage("Shake", SendMessageOptions.DontRequireReceiver);
            sent = true;
        }

        if (!sent)
        {
            var rootCanvas = GetRootCanvasRect(this.transform);
            if (rootCanvas != null)
            {
                StartUIRootShake(rootCanvas, duration, magnitude * 100f);
                sent = true;
            }
        }

        return sent;
    }

    private RectTransform GetRootCanvasRect(Transform t)
    {
        var canvas = t.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        var root = canvas.rootCanvas;
        return root != null ? root.transform as RectTransform : null;
    }

    private void StartUIRootShake(RectTransform target, float duration, float magnitudePx)
    {
        if (uiRootShakeCo != null) StopCoroutine(uiRootShakeCo);
        uiRootShakeCo = StartCoroutine(CoUIRootShake(target, duration, magnitudePx));
    }

    private IEnumerator CoUIRootShake(RectTransform rt, float duration, float magnitudePx)
    {
        if (rt == null) yield break;

        float dur = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        Vector2 start = rt.anchoredPosition;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;

            float x = Random.Range(-1f, 1f) * magnitudePx;
            float y = Random.Range(-1f, 1f) * magnitudePx;
            rt.anchoredPosition = start + new Vector2(x, y);

            yield return null;
        }

        rt.anchoredPosition = start;
        uiRootShakeCo = null;
    }

    private void StartLocalShake(float duration, float magnitude)
    {
        if (localShakeCo != null) StopCoroutine(localShakeCo);
        localShakeCo = StartCoroutine(LocalShake(duration, magnitude));
    }

    private IEnumerator LocalShake(float duration, float magnitude)
    {
        var cam = Camera.main;
        if (cam == null) yield break;

        Transform ct = cam.transform;
        Vector3 originalPos = ct.localPosition;

        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, duration);
        while (elapsed < dur)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            ct.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ct.localPosition = originalPos;
        localShakeCo = null;
    }
}
