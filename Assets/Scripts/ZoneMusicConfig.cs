using UnityEngine;

// ScriptableObject que centraliza las referencias de clips de música por zona.
// Se carga en runtime via Resources.Load("ZoneMusicConfig").
// Ejecutar Eldoria/Setup Zone Music para crear y poblar el asset.
[CreateAssetMenu(menuName = "Eldoria/Zone Music Config")]
public class ZoneMusicConfig : ScriptableObject
{
    [Header("Menú principal / SlotsScreen")]
    public AudioClip menuMusic;

    [Header("Zonas HV")]
    public AudioClip hvMusic;

    [Header("Zona Montañas (MTN01–MTN09)")]
    public AudioClip mtnMusic;

    [Header("Pre-Boss (PreMTN10)")]
    public AudioClip caveAmbience;

    [Header("Sala del Boss (MTN10)")]
    public AudioClip bossMusic;
}
