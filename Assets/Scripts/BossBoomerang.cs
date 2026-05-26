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
    [SerializeField] private float speed      = 24f;
    [SerializeField] private float travelDist = 18f;
    [SerializeField] private int   damage     = 1;

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

        // boomarang arms pivot = bottom (y=0). Con scale=1, frame 94px / PPU=16 = 5.875u alto.
        // Hitbox solo cubre la parte baja (donde puede golpear al jugador parado).
        // El jugador salta ~2u para esquivarlo.
        var col       = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(6f, 2.5f);
        col.offset    = new Vector2(0f, 1.25f);

        StartCoroutine(BoomerangRoutine(direction, onReturn));
    }

    // Carga los sub-sprites de boomarang arms.png directamente desde el proyecto (editor-only)
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
        return list.Count > 0 ? list.ToArray() : null;
#else
        return null;
#endif
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
        Vector3 target = _origin + new Vector3(dir * travelDist, 0f, 0f);

        // ── Ida ──────────────────────────────────────────────────────────────
        while (Vector3.Distance(transform.position, target) > 0.25f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, speed * Time.deltaTime);
            yield return null;
        }

        // ── Vuelta ───────────────────────────────────────────────────────────
        _hit = false;

        while (Vector3.Distance(transform.position, _origin) > 0.25f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, _origin, speed * Time.deltaTime);
            yield return null;
        }

        onReturn?.Invoke();
        Destroy(gameObject);
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
