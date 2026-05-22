using UnityEngine;

// Marca un punto de aparición en la escena.
// PlayerSpawnManager busca el SpawnPoint cuyo spawnId coincida con la entrada usada.
// IDs convencionales:
//   "default"  → spawn principal (centro o inicio)
//   "left"     → jugador viene de la izquierda (ej: HV01 → HV02)
//   "right"    → jugador viene de la derecha (ej: HV03 → HV02)
//   "door_XXX" → jugador sale de la puerta cuyo targetScene es XXX
public class SpawnPoint : MonoBehaviour
{
    public string spawnId = "default";

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawSphere(transform.position, 0.7f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.7f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f,
            $"[{spawnId}]", new GUIStyle { normal = { textColor = Color.cyan }, fontSize = 11 });
    }
#endif
}
