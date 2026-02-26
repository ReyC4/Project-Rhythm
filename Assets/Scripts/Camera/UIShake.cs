using System.Collections;
using UnityEngine;

public class UIShake : MonoBehaviour
{
    public static UIShake Instance;

    [Header("Default Shake (Canvas Overlay)")]
    [Tooltip("Durasi default jika dipanggil tanpa parameter.")]
    public float duration = 0.15f;
    [Tooltip("Kekuatan shake dalam piksel (karena ini UI).")]
    public float magnitude = 24f;

    [Header("Advanced")]
    [Tooltip("0 = tidak meredam, 1 = langsung habis.")]
    [Range(0f,1f)] public float damping = 0f;
    [Tooltip("Gunakan unscaled time agar tetap terasa saat Time.timeScale berubah.")]
    public bool useUnscaledTime = true;

    RectTransform rt;
    Vector2 originalAnchoredPos;
    Coroutine co;

    void Awake()
    {
        Instance = this;

        rt = transform as RectTransform;
        if (rt == null)
        {
            Debug.LogWarning("[UIShake] Harus dipasang di GameObject UI (RectTransform).");
            return;
        }
        originalAnchoredPos = rt.anchoredPosition;
    }

    // API tanpa parameter
    public void Shake() => ShakeOnce(duration, magnitude);

    // Dipanggil oleh ButtonController via SendMessage("ShakeCustom", Vector2)
    public void ShakeCustom(Vector2 durMagPx)
    {
        float dur = Mathf.Max(0.01f, durMagPx.x);
        float mag = Mathf.Max(0f, durMagPx.y);
        ShakeOnce(dur, mag);
    }

    // API publik kalau mau dipanggil langsung
    public void ShakeOnce(float dur, float magPx)
    {
        if (rt == null) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoShake(dur, magPx));
    }

    IEnumerator CoShake(float dur, float magPx)
    {
        float elapsed = 0f;
        var start = originalAnchoredPos;

        while (elapsed < dur)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;

            float x = Random.Range(-1f, 1f) * magPx;
            float y = Random.Range(-1f, 1f) * magPx;
            rt.anchoredPosition = start + new Vector2(x, y);

            if (damping > 0f) magPx = Mathf.Lerp(magPx, 0f, damping);
            yield return null;
        }

        rt.anchoredPosition = start;
        co = null;
    }
}
