using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WheelMenu : MonoBehaviour
{
    [Header("Target Wheel (lingkaran kiri)")]
    public RectTransform wheel;              // drag RectTransform lingkaran kiri

    [Header("Urutan Menu (atas -> bawah)")]
    public Button[] menuButtons;             // contoh: [Play, Add Song, Leaderboard]

    [Header("Rotation Settings")]
    public float degreesPerStep = 30f;       // besar rotasi setiap pindah 1 item
    public float rotateDuration = 0.25f;     // durasi rotasi
    public AnimationCurve ease = null;       // biarkan null -> EaseInOut default
    public bool clockwiseWhenMoveDown = true;// turun = searah jarum jam

    // state internal
    private int currentIndex = 0;
    private float currentAngle = 0f;
    private Coroutine rotateCo;

    void Awake()
    {
        if (ease == null) ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Pasang hook ke setiap button agar bisa memanggil OnItemSelected()
        for (int i = 0; i < menuButtons.Length; i++)
        {
            var btn = menuButtons[i];
            if (btn == null) continue;

            // tambahkan komponen hook ke GameObject tombol (inner class)
            var hook = btn.gameObject.AddComponent<ItemHook>();
            hook.owner = this;
            hook.index = i;
        }

        // Set index awal (ambil yang pertama aktif)
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null && menuButtons[i].gameObject.activeInHierarchy)
            {
                currentIndex = i;
                break;
            }
        }
    }

    /// <summary>
    /// Dipanggil oleh hook saat tombol di-hover/terpilih.
    /// </summary>
    public void OnItemSelected(int newIndex)
    {
        if (wheel == null || newIndex == currentIndex) return;

        int delta = newIndex - currentIndex;                   // + turun, - naik
        float dir = Mathf.Sign(delta);                         // +1 atau -1
        float steps = Mathf.Abs(delta);

        float signedDeg = steps * degreesPerStep * (clockwiseWhenMoveDown ? dir : -dir);

        RotateBy(signedDeg);
        currentIndex = newIndex;
    }

    private void RotateBy(float deltaAngle)
    {
        if (rotateCo != null) StopCoroutine(rotateCo);
        rotateCo = StartCoroutine(RotateRoutine(deltaAngle));
    }

    private IEnumerator RotateRoutine(float deltaAngle)
    {
        float start = currentAngle;
        float end = start + deltaAngle;

        float t = 0f;
        float dur = Mathf.Max(0.01f, rotateDuration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / dur));

            float a = Mathf.LerpUnclamped(start, end, k);
            wheel.localEulerAngles = new Vector3(0f, 0f, a);
            yield return null;
        }

        currentAngle = end;
        wheel.localEulerAngles = new Vector3(0f, 0f, currentAngle);
        rotateCo = null;
    }

    // ----------------------------------------------------------------------
    // Inner class (tetap dalam 1 file): hook untuk tombol
    // Memicu rotasi saat tombol di-hover (mouse) atau terpilih (keyboard/controller)
    // ----------------------------------------------------------------------
    private class ItemHook : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [HideInInspector] public WheelMenu owner;
        [HideInInspector] public int index;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner != null) owner.OnItemSelected(index);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (owner != null) owner.OnItemSelected(index);
        }
    }
}
