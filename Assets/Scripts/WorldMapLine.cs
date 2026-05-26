using UnityEngine;

// Datos de conexión entre dos zonas del mapa. Adjuntado a cada objeto Line.
// WorldMapController lo usa para mostrar/ocultar la línea según descubrimiento.
public class WorldMapLine : MonoBehaviour
{
    public string zoneIdA;
    public string zoneIdB;
}
