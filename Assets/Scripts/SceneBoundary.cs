using UnityEngine;
using UnityEngine.SceneManagement;

// Zona en el borde del nivel que carga la escena vecina al tocarse.
// Patrón: Command — la transición está encapsulada; se ejecuta automáticamente
//         cuando el jugador entra al trigger (sin tecla de interacción).
[RequireComponent(typeof(BoxCollider2D))]
public class SceneBoundary : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string spawnId = "default";

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerSpawnManager.NextSpawnId = spawnId;

        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.0f, 1.0f, 0.5f, 0.35f);
        Vector3 c = transform.TransformPoint(col.offset);
        Gizmos.DrawCube(c, new Vector3(col.size.x, col.size.y, 0.1f));
        Gizmos.color = new Color(0.0f, 1.0f, 0.5f, 0.9f);
        Gizmos.DrawWireCube(c, new Vector3(col.size.x, col.size.y, 0.1f));
    }
#endif
}
