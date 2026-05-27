using UnityEngine;

// ScriptableObject — fill entries in the Inspector or via external JSON.
// Create one from: Assets → Create → Eldoria → Subtitle Data
[CreateAssetMenu(fileName = "SubtitleData", menuName = "Eldoria/Subtitle Data")]
public class SubtitleData : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Start time in seconds (within the video)")]
        public float startTime;
        [Tooltip("End time in seconds")]
        public float endTime;
        [TextArea(2, 4)]
        public string text;
    }

    public Entry[] entries;
}
