using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameData
{
    // Untuk selection lagu default
    public static int selectedSongIndex = 0;

    // Untuk mapping sementara dari Mapper
    public static List<ButtonItem> TempMapping;

    // Untuk custom song (path JSON)
    public static string selectedSongJsonPath = "";
}
