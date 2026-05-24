using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

// Define un área válida de movimiento de la cámara.
// Patrón: Value Object + Observer — declara límites y notifica a CameraFollow cuando el jugador entra/sale.
//
// USO:
//   · Una zona por escena (modo clásico): CameraFollow la detecta en Start automáticamente.
//   · Varias zonas (salas irregulares): cada zona notifica al CameraFollow al entrar/salir
//     el jugador, permitiendo bounds diferentes por sección sin componentes extra.
//   En ambos casos: Menú Eldoria → Add Camera Bounds, luego Edit Collider para ajustar.
[RequireComponent(typeof(BoxCollider2D))]
public class CameraBoundsZone : MonoBehaviour
{
    private BoxCollider2D _col;

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;
    }

    // ── Observer: notifica al CameraFollow cuando el jugador entra/sale ───────
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

    // Devuelve los límites del área en espacio mundo.
    public Bounds GetWorldBounds() => GetComponent<BoxCollider2D>().bounds;

#if UNITY_EDITOR
    // ── Gizmo: rectángulo verde visible en Scene View ─────────────────────
    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        var b = col.bounds;

        // Relleno semitransparente
        Gizmos.color = new Color(0.1f, 0.95f, 0.25f, 0.08f);
        Gizmos.DrawCube(b.center, b.size);

        // Borde sólido
        Gizmos.color = new Color(0.1f, 0.95f, 0.25f, 0.85f);
        Gizmos.DrawWireCube(b.center, b.size);

        // Etiqueta de las cuatro aristas (min/max)
        var style = new GUIStyle { fontSize = 9, normal = { textColor = new Color(0.1f, 0.95f, 0.25f) } };
        Handles.Label(new Vector3(b.center.x, b.min.y, 0), $" suelo  Y={b.min.y:F1}", style);
        Handles.Label(new Vector3(b.center.x, b.max.y, 0), $" techo  Y={b.max.y:F1}", style);
        Handles.Label(new Vector3(b.min.x, b.center.y, 0), $"◄ X={b.min.x:F1}", style);
        Handles.Label(new Vector3(b.max.x, b.center.y, 0), $"X={b.max.x:F1} ►", style);
    }

    // ── Menú editor ───────────────────────────────────────────────────────
    [MenuItem("Eldoria/Add Camera Bounds")]
    static void CreateBoundsZone()
    {
        var go = new GameObject("CameraBounds");
        Undo.RegisterCreatedObjectUndo(go, "Add Camera Bounds");

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(280f, 60f);    // tamaño inicial — ajustar en Scene View

        // Centro en Y=20: suelo≈-10, techo≈50 (reajustar según la sala)
        go.transform.position = new Vector3(0f, 20f, 0f);

        go.AddComponent<CameraBoundsZone>();

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[CameraBoundsZone] Creado. Selecciona 'CameraBounds' → Inspector → Edit Collider " +
                  "y arrastra los bordes del rectángulo verde para ajustar los límites de la cámara.");
    }
#endif
}
