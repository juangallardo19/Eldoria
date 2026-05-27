using UnityEngine;

// Pattern: Command — crystal hazard zone that delegates respawn to CrystalRespawnManager.
// Uses PolygonCollider2D so designers can shape it in Scene View (same as the EdgeCollider2D ceiling).
// Automatically generates a subtle radial glow (more intense at centre, transparent at edges)
// to visually mark the hazard zone.
[RequireComponent(typeof(PolygonCollider2D))]
public class CrystalHazard : MonoBehaviour
{
    [Header("Glow — base colour (very low alpha recommended)")]
    [SerializeField] private Color glowColor      = new Color(1f, 0.45f, 0.1f, 0.18f);
    [SerializeField] private float glowAlphaMin   = 0.04f;
    [SerializeField] private float glowAlphaMax   = 0.18f;
    [SerializeField] private float glowPulseSpeed = 1.0f;
    [SerializeField] private int   glowSortOrder  = -8;

    private SpriteRenderer _glowSR;

    void Awake()
    {
        var poly = GetComponent<PolygonCollider2D>();
        poly.isTrigger = true;

        // Initialise with a default rectangle if the polygon is empty (freshly added)
        if (poly.pathCount == 0 || poly.GetPath(0).Length == 0)
        {
            poly.SetPath(0, new Vector2[]
            {
                new Vector2(-5f, -2f),
                new Vector2( 5f, -2f),
                new Vector2( 5f,  2f),
                new Vector2(-5f,  2f),
            });
        }

        BuildGlowOverlay(poly);
    }

    void Update()
    {
        if (_glowSR == null) return;
        float t = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;
        var c = _glowSR.color;
        c.a = Mathf.Lerp(glowAlphaMin, glowAlphaMax, t);
        _glowSR.color = c;
    }

    // Builds a radial gradient texture: opaque at centre, transparent at edges.
    private void BuildGlowOverlay(PolygonCollider2D poly)
    {
        const int res  = 128;
        const float hr = res * 0.5f;

        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx   = (x - hr) / hr;            // -1..1 on X
                float dy   = (y - hr) / hr;            // -1..1 on Y
                float dist = Mathf.Sqrt(dx * dx + dy * dy); // 0 = centre, 1 = edge
                float a    = Mathf.Clamp01(1f - dist);
                a = a * a * a;                         // cubic: drops fast → highly concentrated at centre
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        // PPU = res → 1×1 unit sprite; localScale scales it to the zone's actual size
        var spr = Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f, res);

        // Size and centre derived from the polygon's AABB (local space)
        var bounds = poly.bounds;                                    // world-space
        var localCenter = transform.InverseTransformPoint(bounds.center);

        var go = new GameObject("GlowOverlay");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(localCenter.x, localCenter.y, 0f);
        go.transform.localScale    = new Vector3(bounds.size.x, bounds.size.y, 1f);

        _glowSR              = go.AddComponent<SpriteRenderer>();
        _glowSR.sprite       = spr;
        _glowSR.color        = glowColor;
        _glowSR.sortingOrder = glowSortOrder;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        CrystalRespawnManager.Instance?.TriggerHazard();
    }
}
