using UnityEngine;
using TMPro;

public class Namalagu : MonoBehaviour
{
    public TMP_Text songText;

    void Start()
    {
        string selectedSong = PlayerPrefs.GetString("SelectedSong", "Unknown Song");
        songText.text = selectedSong;
    }
}
