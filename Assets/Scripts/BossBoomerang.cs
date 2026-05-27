using System;
using System.Collections;
using UnityEngine;

// Boss boomerang — animated projectile using boomarang arms.png in a loop.
// Pure horizontal movement: flies toward the player, then returns to origin.
// Frames are loaded from Assets (AssetDatabase in editor) if boomerangFrames is not assigned.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BossBoomerang : MonoBehaviour
{
    [SerializeField] private float speed      = 30f;
    [SerializeField] private float travelDist = 18f;
    [SerializeField] private int   damage     = 1;
    [SerializeField] private float wobbleAmp  = 0.3f;   // Y oscillation amplitude (units)
    [SerializeField] private float wobbleFreq = 5f;     // frequency (rad/s) ≈ 0.8 cycles/s

    private Vector3 _origin;
    private bool    _hit;

    // onReturn: callback that DoBoomerang waits on to know the arms have returned
    public void Init(float direction, Sprite[] animFrames, Action onReturn = null)
    {
        // If no frames or the array has null slots (Inspector assigned 7 empty elements)
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
            // Orange fallback only if there are absolutely no sprites
            var tex = new Texture2D(16, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[128];
            for (int i = 0; i < 128; i++) pixels[i] = new Color(1f, 0.55f, 0.1f);
            tex.SetPixels(pixels);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 8), new Vector2(0.5f, 0f), 4f);
        }

        // Hitbox centred at the spawn point (at floor level).
        // scale=2 → world: 10u wide, 3u tall. Covers the arm sprite silhouette exactly.
        var col       = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(5f, 1.5f);
        col.offset    = new Vector2(0f, 1f);   // raise hitbox to align with the arm sprite

        StartCoroutine(BoomerangRoutine(direction, onReturn));
    }

    // Loads sub-sprites from boomarang arms.png directly from the project.
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

        // Fallback: spritesheet imported as Single (not Multiple) — use the full texture
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(PATH);
        if (tex != null)
            return new[] { Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) };
#else
        // In builds: load from Resources/Boss/BossBoomerang (drag frames there in the Inspector if needed)
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

        // ── Outward ───────────────────────────────────────────────────────────
        while (Mathf.Abs(transform.position.x - targetX) > 0.25f)
        {
            t += Time.deltaTime;
            float newX = Mathf.MoveTowards(transform.position.x, targetX, speed * Time.deltaTime);
            float newY = baseY + Mathf.Sin(t * wobbleFreq) * wobbleAmp;
            transform.position = new Vector3(newX, newY, _origin.z);
            ManualHitCheck();
            yield return null;
        }

        // ── Return ────────────────────────────────────────────────────────────
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

    // Manual per-frame check — OnTriggerEnter2D misses collisions at high speeds
    // when the object moves via transform.position (no continuous Rigidbody).
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
