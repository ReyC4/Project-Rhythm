using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class MappingButton : MonoBehaviour
{
    public Button firstButton;      // tombol bitmap biasa
    public Button fristdrag;        // tombol awal slider
    public Button lastButton;       // tombol akhir slider
    public Image dragRegion;
    public Text buttonText;

    public bool isDrag = false;
    public Stopwatch buttonTimer = new Stopwatch();
    public float duration = 1000f; // Default 1 detik

    private const float MinDrag = 75f;

    public void SetType(bool isSlider)
    {
        isDrag = isSlider;

        firstButton.gameObject.SetActive(!isSlider);
        fristdrag.gameObject.SetActive(isSlider);
        lastButton.gameObject.SetActive(false);
        dragRegion.gameObject.SetActive(false);
    }

    public void InitializeFirstButton(float x, float y)
    {
        this.transform.SetAsFirstSibling();
        this.transform.position = new Vector3(x, y);

        if (isDrag)
            fristdrag.gameObject.SetActive(true);
        else
            firstButton.gameObject.SetActive(true);

        buttonTimer.Restart();
    }

    public void InitializeLastButton(float x, float y)
    {
        if (Mathf.Abs(this.transform.position.x - x) > MinDrag || Mathf.Abs(this.transform.position.y - y) > MinDrag)
        {
            isDrag = true;

            fristdrag.gameObject.SetActive(true);
            firstButton.gameObject.SetActive(false);

            dragRegion.gameObject.SetActive(true);
            lastButton.transform.SetParent(this.transform, false);
            lastButton.transform.position = new Vector3(x, y);

            Vector3 diffPos = lastButton.transform.position - this.transform.position;
            float angle = Mathf.Atan2(diffPos.y, diffPos.x);
            dragRegion.transform.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);

            lastButton.gameObject.SetActive(true);
        }
        else
        {
            isDrag = false;

            firstButton.gameObject.SetActive(true);
            fristdrag.gameObject.SetActive(false);
            dragRegion.gameObject.SetActive(false);
            lastButton.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (buttonTimer.ElapsedMilliseconds > duration + 50f)
        {
            DestroyButton();
        }
    }

    public void DestroyButton()
    {
        Destroy(this.gameObject);
    }
}
