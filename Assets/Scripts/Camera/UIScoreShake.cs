using System.Collections;
using UnityEngine;

public class UIScoreShake : MonoBehaviour
{
    public RectTransform target;
    public float duration = 0.10f;
    public float magnitudePx = 14f;
    [Range(0f,1f)] public float damping = 0.0f;
    public bool useUnscaledTime = true;

    Coroutine co;
    Vector2 orig;

    void Awake()
    {
        if (!target) target = transform as RectTransform;
        if (target) orig = target.anchoredPosition;
    }

    public void ShakeOnce() => StartShake(duration, magnitudePx);

    public void StartShake(float dur, float magPx)
    {
        if (!target) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Co(dur, magPx));
    }

    IEnumerator Co(float dur, float mag)
    {
        float t = 0f;
        var start = orig;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float x = Random.Range(-1f, 1f) * mag;
            float y = Random.Range(-1f, 1f) * mag;
            target.anchoredPosition = start + new Vector2(x, y);
            if (damping > 0f) mag = Mathf.Lerp(mag, 0f, damping);
            yield return null;
        }
        target.anchoredPosition = start;
        co = null;
    }
}
