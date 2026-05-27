using UnityEngine;

// ScriptableObject centralising music clip references per zone.
// Loaded at runtime via Resources.Load("ZoneMusicConfig").
// Run Eldoria/Setup Zone Music to create and populate the asset.
[CreateAssetMenu(menuName = "Eldoria/Zone Music Config")]
public class ZoneMusicConfig : ScriptableObject
{
    [Header("Main menu / SlotsScreen")]
    public AudioClip menuMusic;

    [Header("HV zones")]
    public AudioClip hvMusic;

    [Header("Mountains zone (MTN01–MTN09)")]
    public AudioClip mtnMusic;

    [Header("Pre-Boss (PreMTN10)")]
    public AudioClip caveAmbience;

    [Header("Boss arena (MTN10)")]
    public AudioClip bossMusic;
}
