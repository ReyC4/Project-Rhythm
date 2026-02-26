using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class GenerateJson : MonoBehaviour
{
    [MenuItem("Tools/Generate Media JSON")]
    public static void Generate()
    {
        string rootPath = Application.dataPath + "/StreamingAssets/Resources/";
        string videoPath = rootPath + "Video/";
        string audioPath = rootPath + "Song/";

        List<MediaItem> mediaList = new List<MediaItem>();

        // Video
        foreach (var file in Directory.GetFiles(videoPath))
        {
            if (file.EndsWith(".mp4") || file.EndsWith(".mov") || file.EndsWith(".avi"))
            {
                mediaList.Add(new MediaItem
                {
                    type = "video",
                    name = Path.GetFileNameWithoutExtension(file),
                    file = "Resources/Video/" + Path.GetFileName(file)
                });
            }
        }

        // Audio
        foreach (var file in Directory.GetFiles(audioPath))
        {
            if (file.EndsWith(".mp3") || file.EndsWith(".ogg") || file.EndsWith(".wav"))
            {
                mediaList.Add(new MediaItem
                {
                    type = "audio",
                    name = Path.GetFileNameWithoutExtension(file),
                    file = "Resources/Song/" + Path.GetFileName(file)
                });
            }
        }

        MediaWrapper wrapper = new MediaWrapper { media = mediaList.ToArray() };
        string json = JsonUtility.ToJson(wrapper, true);

        File.WriteAllText(rootPath + "list.json", json);
        Debug.Log("✅ list.json berhasil dibuat di: " + rootPath);
    }

    [System.Serializable]
    public class MediaItem
    {
        public string type;
        public string name;
        public string file;
    }

    [System.Serializable]
    public class MediaWrapper
    {
        public MediaItem[] media;
    }
}
