using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Representa un elemento de colisión de sala (suelo, plataforma, pared, techo).
// Patrón: Value Object — contiene tipo + collider; RoomBuilderWindow lo crea/administra.
[RequireComponent(typeof(BoxCollider2D))]
public class RoomStructure : MonoBehaviour
{
    public enum StructureType { Ground, Platform, Wall, Ceiling, OneWay }

    [Header("Tipo")]
    public StructureType type = StructureType.Platform;

    [Header("Overlay (editor)")]
    [Range(0f, 1f)] public float overlayOpacity = 0.55f;

    [Header("Overlay en juego (debug)")]
    public bool showOverlayInGame = false;

    void Awake()
    {
        if (type == StructureType.OneWay)
            SetupOneWayEffector();

        if (showOverlayInGame)
            BuildRuntimeOverlay();
    }

    private void SetupOneWayEffector()
    {
        var col = GetComponent<BoxCollider2D>();
        var eff = gameObject.GetComponent<PlatformEffector2D>()
                  ?? gameObject.AddComponent<PlatformEffector2D>();
        eff.useOneWay       = true;
        eff.rotationalOffset = 0f;
        col.usedByEffector  = true;
    }

    private void BuildRuntimeOverlay()
    {
        var child = new GameObject("_overlay") { hideFlags = HideFlags.HideInHierarchy };
        child.transform.SetParent(transform, false);

        var sr = child.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite   = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size     = GetComponent<BoxCollider2D>().size;

        Color c = TypeColor();
        c.a        = overlayOpacity;
        sr.color   = c;
        sr.sortingOrder = -5;
    }

    // ── Gizmos (solo editor) ─────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        DrawGizmoBox(overlayOpacity, 0.9f);
        DrawLabel();
    }

    void OnDrawGizmosSelected()
    {
        DrawGizmoBox(0.85f, 1f);
    }

    private void DrawGizmoBox(float fillAlpha, float wireAlpha)
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Vector3 center = transform.TransformPoint(col.offset);
        Vector3 size   = new Vector3(
            col.size.x * Mathf.Abs(transform.lossyScale.x),
            col.size.y * Mathf.Abs(transform.lossyScale.y),
            0.05f);

        Color c = TypeColor();

        c.a = fillAlpha;
        Gizmos.color = c;
        Gizmos.DrawCube(center, size);

        c.a = wireAlpha;
        Gizmos.color = c;
        Gizmos.DrawWireCube(center, size);
    }

    private void DrawLabel()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Vector3 center = transform.TransformPoint(col.offset);
        float   halfH  = col.size.y * Mathf.Abs(transform.lossyScale.y) * 0.5f;

        var style = new GUIStyle
        {
            fontSize = 9,
            normal   = { textColor = Color.white }
        };
        Handles.Label(center + Vector3.up * (halfH + 0.1f), $"{name} [{type}]", style);
    }
#endif

    // ── Color por tipo ────────────────────────────────────────────────────────
    public Color TypeColor() => type switch
    {
        StructureType.Ground   => new Color(0.15f, 0.15f, 0.15f),
        StructureType.Platform => new Color(0.25f, 0.5f,  1.0f),
        StructureType.Wall     => new Color(1.0f,  0.45f, 0.1f),
        StructureType.Ceiling  => new Color(0.15f, 0.85f, 0.3f),
        StructureType.OneWay   => new Color(1.0f,  0.85f, 0.1f),
        _                      => Color.white
    };
}
