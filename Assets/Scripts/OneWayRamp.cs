using System.Collections;
using UnityEngine;

// Inclined one-way ramp.
// Pattern: Strategy — ShouldIgnore() decides each FixedUpdate whether the collision is active,
//         same as OneWayPlatform but adapted for rotated surfaces.
//
// The "surface" is defined by transform.up in world space (the ramp normal).
// If the player is on the positive side of that normal → can land.
// If on the negative side (below/behind) or beyond the endpoints → passes through.
//
// Key differences from OneWayPlatform:
//   · Instead of comparing AABB Y, projects onto transform.up (rotated normal).
//   · Instead of comparing AABB X, projects onto transform.right (rotated length).
//   · The "rising" check uses velocity projected onto the normal, not velocity.y.
[RequireComponent(typeof(BoxCollider2D))]
public class OneWayRamp : MonoBehaviour
{
    // LAND_TOLERANCE: threshold to activate collision (feet within 0.2 units of the surface).
    // LEAVE_TOLERANCE: hysteresis window — the player must descend (LAND+LEAVE) units
    //   below the surface before the collision deactivates.
    //   Prevents the rapid on/off toggle that creates a counter-force when walking slowly.
    private const float LAND_TOLERANCE  = 0.2f;
    private const float LEAVE_TOLERANCE = 1.0f;

    private BoxCollider2D  _box;
    private Collider2D[]   _allCols;
    private float          _halfLength;

    private Collider2D   _playerCol;
    private bool         _dropping;
    private bool         _rampColliding;  // hysteresis state

    void Awake()
    {
        _box        = GetComponent<BoxCollider2D>();
        _allCols    = GetComponents<Collider2D>();
        _halfLength = _box.size.x * 0.5f;

        // High friction to prevent sliding on a 45° slope (tan45°=1.0 → requires μ≥1.0)
        var mat = new PhysicsMaterial2D("RampFriction") { friction = 2f, bounciness = 0f };
        _box.sharedMaterial = mat;
    }

    void Start()    => FindPlayer();
    void OnEnable()
    {
        if (_box == null) { _box = GetComponent<BoxCollider2D>(); _allCols = GetComponents<Collider2D>(); }
        if (_playerCol == null) FindPlayer();
    }

    // Proactive FixedUpdate: runs BEFORE Unity resolves physics.
    void FixedUpdate()
    {
        if (_playerCol == null) { FindPlayer(); return; }

        bool ignore = ShouldIgnore();
        foreach (var col in _allCols)
            Physics2D.IgnoreCollision(_playerCol, col, ignore);
    }

    // ── Core decision ────────────────────────────────────────────────────────
    // Uses the player's FEET (AABB bottom) rather than the centre, and does NOT check velocity.
    // Benefits:
    //   · Eliminates the "auto-jump" bug: feet position doesn't change when releasing a key.
    //   · Eliminates "ejection" at the base: at ground level the feet are always
    //     below the ramp plane, so lateral collision never activates.
    //   · Jump-through from below: feet cross the surface in under one frame
    //     (jump speed >> transition zone) → no blocking.
    private bool ShouldIgnore()
    {
        if (_dropping) return true;

        Vector2 rampNormal = transform.up;
        Vector2 rampAlong  = transform.right;
        Vector2 rampCenter = _box.bounds.center;

        // Reference point: player centre-X, Y at the AABB bottom (feet).
        Vector2 playerFeet = new Vector2(_playerCol.bounds.center.x, _playerCol.bounds.min.y);
        Vector2 toFeet     = playerFeet - rampCenter;

        // ── Is the player within the ramp's length? ───────────────────────────
        float along = Vector2.Dot(toFeet, rampAlong);
        if (Mathf.Abs(along) > _halfLength)
        {
            _rampColliding = false;   // exited through the ends → reset hysteresis
            return true;
        }

        // ── Hysteresis: prevents rapid on/off toggle on the surface ───────────
        // Without hysteresis, physics penetration makes signedDist oscillate ±ε
        // across the threshold every frame, cancelling movement and creating counter-force.
        float signedDist = Vector2.Dot(toFeet, rampNormal);
        if (_rampColliding)
        {
            // Currently colliding: deactivate only if feet drop clearly below
            if (signedDist < -(LAND_TOLERANCE + LEAVE_TOLERANCE))
                _rampColliding = false;
        }
        else
        {
            // Currently ignoring: activate when feet reach the surface
            if (signedDist >= -LAND_TOLERANCE)
                _rampColliding = true;
        }

        return !_rampColliding;
    }

    // ── Drop-through (for future use by PlayerController) ────────────────────
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
            return;
        }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _playerCol = p.GetComponent<Collider2D>();
            return;
        }
        Debug.LogWarning($"[OneWayRamp] {name}: Player not found in the scene.");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_box == null) _box = GetComponent<BoxCollider2D>();
        float hl = _box != null ? _box.size.x * 0.5f : _halfLength;

        // Ramp surface line
        Vector3 leftEdge  = transform.position - transform.right * hl;
        Vector3 rightEdge = transform.position + transform.right * hl;
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 1f);
        Gizmos.DrawLine(leftEdge, rightEdge);

        // Arrow indicating the passable side (positive normal)
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 1.5f);
    }
#endif
}
