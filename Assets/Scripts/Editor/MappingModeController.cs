#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MappingModeEditorHelper
{
    public static string SaveJsonFile(string defaultFileName)
    {
        return EditorUtility.SaveFilePanel("Save JSON File", Application.streamingAssetsPath, defaultFileName, "json");
    }

    public static string OpenAudioFile()
    {
        return EditorUtility.OpenFilePanel("Select MP3 File", Application.dataPath + "/Resources", "mp3");
    }
}
#endif