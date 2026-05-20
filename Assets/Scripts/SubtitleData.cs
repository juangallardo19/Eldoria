using UnityEngine;

// ScriptableObject — rellena las entradas en el Inspector o vía JSON externo.
// Crea uno desde: Assets → Create → Eldoria → Subtitle Data
[CreateAssetMenu(fileName = "SubtitleData", menuName = "Eldoria/Subtitle Data")]
public class SubtitleData : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Segundo de inicio (en el video)")]
        public float startTime;
        [Tooltip("Segundo de fin")]
        public float endTime;
        [TextArea(2, 4)]
        public string text;
    }

    public Entry[] entries;
}
