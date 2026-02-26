using UnityEngine;

public class MapperNextButton : MonoBehaviour
{
    public GameObject panelMapper;
    public GameObject panelNaming;

    public void OnNext()
    {
        // Contoh kalau kamu mau simpan bitmap dulu (kalau belum otomatis)
        // GameData.TempBitmap = YourMappingScript.Instance.GetMappingData();

        // Matikan panel mapper
        panelMapper.SetActive(false);

        // Nyalakan panel naming
        panelNaming.SetActive(true);

        Debug.Log("✅ Pindah ke Panel Naming");
    }
}
