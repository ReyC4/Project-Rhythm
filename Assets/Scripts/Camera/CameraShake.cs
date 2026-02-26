using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance;

    [Header("Default Shake")]
    [Tooltip("Durasi default bila pakai Shake() tanpa parameter.")]
    public float duration = 0.15f;
    [Tooltip("Kekuatan default bila pakai Shake() tanpa parameter.")]
    public float magnitude = 0.2f;

    [Header("Advanced")]
    [Tooltip("Kurangi getaran per frame (0 = tidak redam, 1 = lenyap seketika).")]
    [Range(0f, 1f)] public float damping = 0.0f;
    [Tooltip("Pakai unscaled time agar tetap halus walau Time.timeScale berubah.")]
    public bool useUnscaledTime = true;

    Transform camT;
    Vector3 originalLocalPos;
    Coroutine co;

    void Awake()
    {
        Instance = this;
        var cam = Camera.main; // pakai main camera
        if (cam == null)
        {
            Debug.LogWarning("[ScreenShake] Camera.main tidak ditemukan. Pastikan kamera ditag MainCamera.");
            return;
        }
        camT = cam.transform;
        originalLocalPos = camT.localPosition;
    }

    // Dipanggil tanpa parameter (pakai default di Inspector)
    public void Shake()
    {
        ShakeOnce(duration, magnitude);
    }

    // Dipanggil dengan parameter (ButtonController kirim via SendMessage("ShakeCustom", Vector2))
    public void ShakeCustom(Vector2 durMag)
    {
        ShakeOnce(Mathf.Max(0.01f, durMag.x), Mathf.Max(0f, durMag.y));
    }

    // API publik kalau mau panggil langsung skrip lain
    public void ShakeOnce(float dur, float mag)
    {
        if (camT == null) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoShake(dur, mag));
    }

    IEnumerator CoShake(float dur, float mag)
    {
        float elapsed = 0f;
        Vector3 start = originalLocalPos;

        while (elapsed < dur)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;

            // guncangan acak
            float x = Random.Range(-1f, 1f) * mag;
            float y = Random.Range(-1f, 1f) * mag;
            camT.localPosition = start + new Vector3(x, y, 0f);

            // redam kekuatan perlahan
            if (damping > 0f)
            {
                mag = Mathf.Lerp(mag, 0f, damping);
            }

            yield return null;
        }

        camT.localPosition = start;
        co = null;
    }
}
