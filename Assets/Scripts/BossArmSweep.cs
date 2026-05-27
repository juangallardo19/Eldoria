using System.Collections;
using UnityEngine;

// Boss arm as a floor-skimming sweep — flies horizontally at ground level, out and back.
// Mechanic: the player MUST JUMP to dodge it. No homing: purely horizontal path.
// Can hit once on the way out and once on the way back.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BossArmSweep : MonoBehaviour
{
    [SerializeField] private float speed  = 11f;
    [SerializeField] private int   damage = 1;

    private float _originX;
    private float _wallX;
    private float _fixedY;
    private bool  _hit;

    // direction: +1 = right, -1 = left
    // wallX: arena boundary to fly toward
    // Y must be set in transform.position before calling Init
    public void Init(float direction, float wallX, Sprite sprite)
    {
        _originX = transform.position.x;
        _wallX   = wallX;
        _fixedY  = transform.position.y;

        var sr = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        else
        {
            var tex = new Texture2D(16, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[64];
            var c = new Color(1f, 0.55f, 0.1f);
            for (int i = 0; i < 64; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 4), new Vector2(0.5f, 0.5f), 2f);
        }
        sr.flipX        = direction < 0f;
        sr.sortingOrder = 5;

        var col       = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        // 8u wide × 1.8u tall (arm scale=1): covers the arm's central body.
        // Y=-11.5: hits standing player at y=[-12,-10]; safe if jumping ≥1.5u (centre ≥-9.5).
        col.size      = new Vector2(8.0f, 1.8f);

        StartCoroutine(SweepRoutine(direction));
    }

    private IEnumerator SweepRoutine(float dir)
    {
        // ── Outward: fly toward the arena boundary ────────────────────────────
        while (Mathf.Abs(transform.position.x - _wallX) > 0.4f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(_wallX, _fixedY, 0f),
                speed * Time.deltaTime);
            transform.Rotate(0f, 0f, dir * 540f * Time.deltaTime);
            yield return null;
        }

        // ── Return: fly back to origin ────────────────────────────────────────
        _hit = false;   // can hit again on the return trip

        while (Mathf.Abs(transform.position.x - _originX) > 0.4f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(_originX, _fixedY, 0f),
                speed * Time.deltaTime);
            transform.Rotate(0f, 0f, -dir * 540f * Time.deltaTime);
            yield return null;
        }

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
