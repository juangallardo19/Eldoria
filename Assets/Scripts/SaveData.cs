[System.Serializable]
public class SaveData
{
    public bool   isEmpty         = true;
    public string slotName        = "Partida";
    public string zoneName        = "Inicio";
    public string sceneName       = "";        // nombre de escena Unity para recargar al continuar
    public float  playTimeSeconds = 0f;
    public int    level           = 1;
    public int    health          = 5;
    public float  posX            = 0f;
    public float  posY            = 0f;
    public bool   bossDefeated    = false;     // La Obsesión derrotada — no reaparece nunca
    public bool   hasDash         = false;     // Dash desbloqueado al derrotar al boss
    public string sanctuaryScene  = "";        // Escena del último santuario descansado
    public float  sanctuaryX      = 0f;
    public float  sanctuaryY      = 0f;
    public bool   tutorialDone    = false;     // Tutorial de HV01_Interior completado
}
