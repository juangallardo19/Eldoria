using System;
using System.Collections;
using UnityEngine;

// Boomerang del boss — proyectil animado con boomarang arms.png en loop.
// Movimiento horizontal puro: ida al lado del jugador, vuelta al origen.
// Los frames se cargan de Assets (AssetDatabase en editor) por si boomerangFrames no está asignado.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BossBoomerang : MonoBehaviour
{
    [SerializeField] private float speed      = 30f;
    [SerializeField] private float travelDist = 18f;
    [SerializeField] private int   damage     = 1;
    [SerializeField] private float wobbleAmp  = 0.3f;   // amplitud oscilación Y (u)
    [SerializeField] private float wobbleFreq = 5f;     // frecuencia (rad/s) ≈ 0.8 ciclos/s

    private Vector3 _origin;
    private bool    _hit;

    // onReturn: callback que DoBoomerang espera para saber que los brazos volvieron
    public void Init(float direction, Sprite[] animFrames, Action onReturn = null)
    {
        // Si no vienen frames o el array tiene slots nulos (Inspector asignó 7 elementos vacíos)
        if (animFrames == null || animFrames.Length == 0 || animFrames[0] == null)
            animFrames = LoadFrames();

        _origin = transform.position;

        var sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder      = 5;
        sr.flipX             = direction < 0f;
        transform.localScale = new Vector3(2f, 2f, 1f);

        bool hasFrames = animFrames != null && animFrames.Length > 0;
        if (hasFrames)
        {
            sr.sprite = animFrames[0];
            StartCoroutine(AnimateFrames(sr, animFrames));
        }
        else
        {
            // Fallback naranja solo si absolutamente no hay sprites
            var tex = new Texture2D(16, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[128];
            for (int i = 0; i < 128; i++) pixels[i] = new Color(1f, 0.55f, 0.1f);
            tex.SetPixels(pixels);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 8), new Vector2(0.5f, 0f), 4f);
        }

        // Hitbox centrada en el punto de spawn (al ras del suelo).
        // scale=2 → world: ancho=10u, alto=3u. Cubre exactamente la silueta del brazo rasante.
        var col       = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(5f, 1.5f);
        col.offset    = new Vector2(0f, 1f);   // sube la hitbox para alinear con los brazos del sprite

        StartCoroutine(BoomerangRoutine(direction, onReturn));
    }

    // Carga los sub-sprites de boomarang arms.png directamente desde el proyecto.
    private static Sprite[] LoadFrames()
    {
#if UNITY_EDITOR
        const string PATH = "Assets/Sprites/Boss1Obsesion/Sprite Sheets/boomarang arms.png";
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(PATH);
        var list   = new System.Collections.Generic.List<Sprite>();
        foreach (var a in assets)
            if (a is Sprite s) list.Add(s);
        list.Sort((a, b) =>
            System.StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));
        if (list.Count > 0) return list.ToArray();

        // Fallback: spritesheet importada como Single (no Multiple) — usar la textura completa
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(PATH);
        if (tex != null)
            return new[] { Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) };
#else
        // En builds: cargar desde Resources/Boss/BossBoomerang (arrastra los frames allí en el Inspector si es necesario)
        var sprites = Resources.LoadAll<Sprite>("Boss/BossBoomerang");
        if (sprites != null && sprites.Length > 0) return sprites;
#endif
        return null;
    }

    private IEnumerator AnimateFrames(SpriteRenderer sr, Sprite[] frames)
    {
        int i = 0;
        while (sr != null)
        {
            sr.sprite = frames[i % frames.Length];
            i++;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator BoomerangRoutine(float dir, Action onReturn)
    {
        float targetX = _origin.x + dir * travelDist;
        float baseY   = _origin.y;
        float t       = 0f;

        // ── Ida ──────────────────────────────────────────────────────────────
        while (Mathf.Abs(transform.position.x - targetX) > 0.25f)
        {
            t += Time.deltaTime;
            float newX = Mathf.MoveTowards(transform.position.x, targetX, speed * Time.deltaTime);
            float newY = baseY + Mathf.Sin(t * wobbleFreq) * wobbleAmp;
            transform.position = new Vector3(newX, newY, _origin.z);
            ManualHitCheck();
            yield return null;
        }

        // ── Vuelta ───────────────────────────────────────────────────────────
        _hit = false;

        while (Mathf.Abs(transform.position.x - _origin.x) > 0.25f)
        {
            t += Time.deltaTime;
            float newX = Mathf.MoveTowards(transform.position.x, _origin.x, speed * Time.deltaTime);
            float newY = baseY + Mathf.Sin(t * wobbleFreq) * wobbleAmp;
            transform.position = new Vector3(newX, newY, _origin.z);
            ManualHitCheck();
            yield return null;
        }

        onReturn?.Invoke();
        Destroy(gameObject);
    }

    // Comprobación manual cada frame — OnTriggerEnter2D pierde colisiones a velocidades altas
    // cuando el objeto se mueve vía transform.position (sin Rigidbody continuo).
    private void ManualHitCheck()
    {
        if (_hit) return;
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Vector2 worldCenter = (Vector2)transform.position + col.offset;
        Vector2 worldSize   = col.size * new Vector2(
            Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        var hits = Physics2D.OverlapBoxAll(worldCenter, worldSize, 0f);
        foreach (var h in hits)
        {
            bool isPlayer = h.CompareTag("Player") || h.GetComponent<PlayerController>() != null;
            if (!isPlayer) continue;
            if (CrystalRespawnManager.Instance == null) break;
            _hit = true;
            CrystalRespawnManager.Instance.TakeBossDamage(damage);
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_hit) return;
        bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;
        if (!isPlayer) return;
        if (CrystalRespawnManager.Instance == null) return;

        _hit = true;
        CrystalRespawnManager.Instance.TakeBossDamage(damage);
    }
}
