using UnityEngine;

// Connection data between two map zones. Attached to each Line object.
// WorldMapController uses it to show/hide the line based on discovery state.
public class WorldMapLine : MonoBehaviour
{
    public string zoneIdA;
    public string zoneIdB;
}
