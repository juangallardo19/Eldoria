using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

// Defines a valid camera movement area.
// Pattern: Value Object + Observer — declares bounds and notifies CameraFollow when the player enters/exits.
//
// USAGE:
//   · Single zone per scene (classic mode): CameraFollow auto-detects it in Start.
//   · Multiple zones (irregular rooms): each zone notifies CameraFollow on enter/exit,
//     allowing different bounds per section without extra components.
//   In both cases: Menu Eldoria → Add Camera Bounds, then Edit Collider to adjust.
[RequireComponent(typeof(BoxCollider2D))]
public class CameraBoundsZone : MonoBehaviour
{
    private BoxCollider2D _col;

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;
    }

    // ── Observer: notifies CameraFollow when the player enters/exits ─────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var cf = Camera.main?.GetComponent<CameraFollow>();
        if (cf != null) cf.SetActiveBoundsZone(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var cf = Camera.main?.GetComponent<CameraFollow>();
        if (cf != null && cf.ActiveBoundsZone == this) cf.SetActiveBoundsZone(null);
    }

    // Returns world-space bounds of the area.
    public Bounds GetWorldBounds() => GetComponent<BoxCollider2D>().bounds;

#if UNITY_EDITOR
    // ── Gizmo: green rectangle visible in Scene View ──────────────────────
    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        var b = col.bounds;

        // Semi-transparent fill
        Gizmos.color = new Color(0.1f, 0.95f, 0.25f, 0.08f);
        Gizmos.DrawCube(b.center, b.size);

        // Solid border
        Gizmos.color = new Color(0.1f, 0.95f, 0.25f, 0.85f);
        Gizmos.DrawWireCube(b.center, b.size);

        // Labels on the four edges (min/max)
        var style = new GUIStyle { fontSize = 9, normal = { textColor = new Color(0.1f, 0.95f, 0.25f) } };
        Handles.Label(new Vector3(b.center.x, b.min.y, 0), $" floor  Y={b.min.y:F1}", style);
        Handles.Label(new Vector3(b.center.x, b.max.y, 0), $" ceil   Y={b.max.y:F1}", style);
        Handles.Label(new Vector3(b.min.x, b.center.y, 0), $"◄ X={b.min.x:F1}", style);
        Handles.Label(new Vector3(b.max.x, b.center.y, 0), $"X={b.max.x:F1} ►", style);
    }

    // ── Editor menu ───────────────────────────────────────────────────────
    [MenuItem("Eldoria/Add Camera Bounds")]
    static void CreateBoundsZone()
    {
        var go = new GameObject("CameraBounds");
        Undo.RegisterCreatedObjectUndo(go, "Add Camera Bounds");

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(280f, 60f);    // initial size — adjust in Scene View

        // Centred at Y=20: floor≈-10, ceiling≈50 (adjust per room)
        go.transform.position = new Vector3(0f, 20f, 0f);

        go.AddComponent<CameraBoundsZone>();

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[CameraBoundsZone] Created. Select 'CameraBounds' → Inspector → Edit Collider " +
                  "and drag the green rectangle edges to set the camera bounds.");
    }
#endif
}
