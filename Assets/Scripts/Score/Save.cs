using UnityEngine;

public class DetectActiveSongPanel : MonoBehaviour
{
    [System.Serializable]
    public class SongPanel
    {
        public string songName;
        public GameObject panel;
    }

    public SongPanel[] songPanels;

    void Start()
    {
        foreach (var song in songPanels)
        {
            if (song.panel.activeSelf)
            {
                PlayerPrefs.SetString("SelectedSong", song.songName);
                PlayerPrefs.Save(); // wajib untuk WebGL
                break;
            }
        }
    }
}
