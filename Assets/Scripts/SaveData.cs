[System.Serializable]
public class SaveData
{
    public bool   isEmpty         = true;
    public string slotName        = "Partida";
    public string zoneName        = "Inicio";
    public string sceneName       = "";        // Unity scene name to reload on Continue
    public float  playTimeSeconds = 0f;
    public int    level           = 1;
    public int    health          = 5;
    public float  posX            = 0f;
    public float  posY            = 0f;
    public bool   bossDefeated    = false;     // La Obsesión defeated — never respawns
    public bool   hasDash         = false;     // Dash unlocked after defeating the boss
    public string sanctuaryScene  = "";        // Scene of the last rested sanctuary
    public float  sanctuaryX      = 0f;
    public float  sanctuaryY      = 0f;
    public bool   tutorialDone    = false;     // HV01_Interior tutorial completed
    public int    tutorialPhase   = 0;         // TutorialManager.Phase cast to int; 0 = Inactive
}
