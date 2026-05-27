using UnityEngine;

// Marks a spawn point in the scene.
// PlayerSpawnManager looks for the SpawnPoint whose spawnId matches the entry used.
// Conventional IDs:
//   "default"  → main spawn (centre or start)
//   "left"     → player comes from the left  (e.g. HV01 → HV02)
//   "right"    → player comes from the right (e.g. HV03 → HV02)
//   "door_XXX" → player exits through the door whose targetScene is XXX
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
