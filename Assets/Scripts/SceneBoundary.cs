using UnityEngine;
using UnityEngine.SceneManagement;

// Zone at the level edge that loads the neighbouring scene on contact.
// Pattern: Command — the transition is encapsulated; it fires automatically
//         when the player enters the trigger (no interaction key needed).
[RequireComponent(typeof(BoxCollider2D))]
public class SceneBoundary : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string spawnId = "default";

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(targetScene)) return;
        if (!other.CompareTag("Player")) return;
        if (SceneFader.Instance != null && SceneFader.Instance.IsFading) return;
        if (BossObsesion.IsArenaActive) return;

        PlayerSpawnManager.NextSpawnId = spawnId;

        var pc = other.GetComponent<PlayerController>();
        if (pc != null) PlayerSpawnManager.SavedRunningMode = pc.GetRunningMode();

        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadScene(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }

    // OnTriggerStay handles the case where the player was already inside the trigger when it activated
    void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

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
