using UnityEngine;

// ScriptableObject de configuración del HUD del jugador.
// Colócalo en Assets/Resources/PlayerHUDConfig.asset para que sea cargable en builds.
// Ejecutar menú Eldoria/Setup Player HUD para crear y rellenar automáticamente.
[CreateAssetMenu(menuName = "Eldoria/Player HUD Config")]
public class PlayerHUDConfig : ScriptableObject
{
    [Header("Ara — animaciones de vida (4 frames c/u)")]
    public Sprite[] araIdle;
    public Sprite[] araDamage;
    public Sprite[] araLow;
    public Sprite[] araDeath;
    public Sprite[] araHeal;

    [Header("Kael — idle portrait (10 frames)")]
    public Sprite[] kaelIdle;

    [Header("HUD visuals")]
    public Sprite araContainer;  // ContainerHealth.png — fondo de la sección de vidas
}
