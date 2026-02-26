using UnityEngine;
using UnityEngine.UI;
using SFB; // Namespace StandaloneFileBrowser

public class FilePickerTest : MonoBehaviour
{
    public Button pickFileButton;

    void Start()
    {
        pickFileButton.onClick.AddListener(PickFile);
    }

    void PickFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Select File", "", "", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            Debug.Log("File selected: " + paths[0]);
            // TODO: Load or process file here
        }
    }
}
