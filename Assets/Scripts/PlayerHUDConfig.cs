using UnityEngine;

// ScriptableObject with Player HUD configuration.
// Place at Assets/Resources/PlayerHUDConfig.asset so it can be loaded in builds.
// Run menu Eldoria/Setup Player HUD to create and populate it automatically.
[CreateAssetMenu(menuName = "Eldoria/Player HUD Config")]
public class PlayerHUDConfig : ScriptableObject
{
    [Header("Ara — life animations (4 frames each)")]
    public Sprite[] araIdle;
    public Sprite[] araDamage;
    public Sprite[] araLow;
    public Sprite[] araDeath;
    public Sprite[] araHeal;

    [Header("Kael — idle portrait (10 frames)")]
    public Sprite[] kaelIdle;

    [Header("HUD visuals")]
    public Sprite araContainer;  // ContainerHealth.png — background of the lives section
}
