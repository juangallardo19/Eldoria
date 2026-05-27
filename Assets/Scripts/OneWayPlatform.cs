using System.Collections;
using UnityEngine;

// One-way platform — custom system inspired by Hollow Knight / Celeste.
// Pattern: Strategy — ShouldIgnore() decides each FixedUpdate whether the collision is active,
//         BEFORE Unity resolves physics (proactive, not reactive approach).
//
// Logic:
//   COLLIDES  → player bottom ABOVE surface Y AND player falling or still
//               AND player centre X within the platform width.
//   IGNORES   → player below, lateral (centre X outside width), rising, or dropping through.
//
// Manages ALL Collider2D on the same GameObject to avoid duplicate collision bugs
// when the object has more than one BoxCollider2D.
[RequireComponent(typeof(Collider2D))]
public class OneWayPlatform : MonoBehaviour
{
    // Tolerance: player bottom can be this many units "inside" the edge and still land.
    private const float LAND_TOLERANCE  = 0.1f;

    // Threshold: if velocity.y exceeds this the player is considered rising → ignore.
    private const float RISING_THRESHOLD = 0.4f;

    // Reference collider for bounds calculation (first one); all are ignored together.
    private Collider2D   _col;
    private Collider2D[] _allCols;   // all Collider2D on this GameObject

    private Collider2D  _playerCol;
    private Rigidbody2D _playerRb;
    private bool        _dropping;

    void Awake()
    {
        _col     = GetComponent<Collider2D>();
        _allCols = GetComponents<Collider2D>();
    }

    void Start()    => FindPlayer();
    void OnEnable()
    {
        if (_col == null)    { _col = GetComponent<Collider2D>(); _allCols = GetComponents<Collider2D>(); }
        if (_playerCol == null) FindPlayer();
    }

    // FixedUpdate: proactive — runs BEFORE Unity resolves physics.
    // Applies the decision to ALL colliders on the object, not just the first.
    void FixedUpdate()
    {
        if (_playerCol == null) { FindPlayer(); return; }

        bool ignore = ShouldIgnore();
        foreach (var col in _allCols)
            Physics2D.IgnoreCollision(_playerCol, col, ignore);
    }

    // ── Core decision ────────────────────────────────────────────────────────
    private bool ShouldIgnore()
    {
        if (_dropping) return true;

        float surfaceY      = _col.bounds.max.y;
        float playerBottomY = _playerCol.bounds.min.y;

        // Player below the surface → coming from underneath or rising along the side
        if (playerBottomY < surfaceY - LAND_TOLERANCE) return true;

        // Player at or above surface: verify they are ABOVE, not lateral.
        // If player centre X is outside the platform width → coming from the side.
        float playerCenterX = _playerCol.bounds.center.x;
        if (playerCenterX < _col.bounds.min.x || playerCenterX > _col.bounds.max.x) return true;

        // Player above but rising → can pass through upward
        if (_playerRb != null && _playerRb.velocity.y > RISING_THRESHOLD) return true;

        // Player above, horizontally aligned, falling or still → lands
        return false;
    }

    // ── Drop-through (called from PlayerController) ───────────────────────────
    public void TriggerDropThrough(float duration = 0.28f)
    {
        StopAllCoroutines();
        StartCoroutine(DropRoutine(duration));
    }

    private IEnumerator DropRoutine(float duration)
    {
        _dropping = true;
        yield return new WaitForSeconds(duration);
        _dropping = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void FindPlayer()
    {
        var ctrl = FindObjectOfType<PlayerController>();
        if (ctrl != null)
        {
            _playerCol = ctrl.GetComponent<Collider2D>();
            _playerRb  = ctrl.GetComponent<Rigidbody2D>();
            return;
        }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _playerCol = p.GetComponent<Collider2D>();
            _playerRb  = p.GetComponent<Rigidbody2D>();
            return;
        }
        Debug.LogWarning($"[OneWayPlatform] {name}: Player not found.");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_col == null) _col = GetComponent<Collider2D>();
        if (_col == null) return;
        var b = _col.bounds;
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 0.35f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 1f);
        Gizmos.DrawLine(new Vector3(b.min.x, b.max.y), new Vector3(b.max.x, b.max.y));
    }
#endif
}
