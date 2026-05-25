using UnityEngine;

// Patrón Command — zona de cristales que delega el respawn en CrystalRespawnManager.
// Usa PolygonCollider2D para que el diseñador pueda moldear la forma en Scene View
// (igual que el techo EdgeCollider2D). Genera automáticamente un glow radial muy sutil
// (más intenso en el centro, transparente en los bordes) para señalizar la zona.
[RequireComponent(typeof(PolygonCollider2D))]
public class CrystalHazard : MonoBehaviour
{
    [Header("Glow — color base (alpha muy bajo recomendado)")]
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

        // Inicializa con un rectángulo si el polígono está vacío (recién añadido)
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

    // Construye una textura de gradiente radial: opaco en el centro, transparente en los bordes.
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
                float dx   = (x - hr) / hr;            // -1..1 en X
                float dy   = (y - hr) / hr;            // -1..1 en Y
                float dist = Mathf.Sqrt(dx * dx + dy * dy); // 0 = centro, 1 = borde
                float a    = Mathf.Clamp01(1f - dist);
                a = a * a * a;                         // cúbico: cae rápido → centro muy concentrado
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        // PPU = res → sprite de 1×1 unidad; localScale ajusta al tamaño real de la zona
        var spr = Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f, res);

        // Tamaño y centro basados en el AABB del polígono (espacio local)
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
