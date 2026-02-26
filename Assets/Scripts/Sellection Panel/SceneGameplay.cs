using UnityEngine;

public class SceneGameplay : MonoBehaviour
{
    public GameObject[] songPanels; // Panel yang masing-masing sudah ada AudioSource-nya

    void Start()
    {
        int index = GameData.selectedSongIndex;

        // Validasi index
        if (index < 0 || index >= songPanels.Length)
        {
            Debug.LogError("Index lagu tidak valid: " + index);
            return;
        }

        // Nonaktifkan semua panel dulu
        foreach (GameObject panel in songPanels)
        {
            panel.SetActive(false);
        }

        // Aktifkan panel sesuai lagu yang dipilih
        GameObject selectedPanel = songPanels[index];
        selectedPanel.SetActive(true);

        // Ambil AudioSource dari panel dan mainkan
        AudioSource panelAudio = selectedPanel.GetComponent<AudioSource>();
        if (panelAudio != null)
        {
            panelAudio.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource tidak ditemukan di panel: " + selectedPanel.name);
        }
    }
}
