using UnityEngine;

// Pattern: Command — boss damage hitbox, activated/deactivated by BossObsesion.
//
// Unity bug: OnTriggerEnter2D does NOT fire if the player is ALREADY INSIDE the area
// when the collider is enabled. Fix: immediate Physics2D.OverlapBox in Activate()
// + OnTriggerStay2D as backup, both using the same flag.
[RequireComponent(typeof(BoxCollider2D))]
public class BossAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private BoxCollider2D _col;
    private bool          _hitThisActivation;

    void Awake()
    {
        _col           = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;
        _col.enabled   = false;
    }

    public void Activate(int dmg)
    {
        damage             = dmg;
        _hitThisActivation = false;
        _col.enabled       = true;

        // Immediate check: hit at once if the player is already inside
        CheckOverlap();
    }

    public void Deactivate()
    {
        _col.enabled = false;
    }

    // Player enters the trigger while active
    void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    // Player stays inside (backup in case OnTriggerEnter2D didn't fire)
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void OnDisable() => _hitThisActivation = false;

    private void CheckOverlap()
    {
        if (_hitThisActivation) return;
        var bounds = _col.bounds;
        var hits   = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        foreach (var hit in hits)
        {
            if (hit == _col) continue;
            TryHit(hit);
            if (_hitThisActivation) break;
        }
    }

    private void TryHit(Collider2D other)
    {
        if (_hitThisActivation) return;
        bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;
        if (!isPlayer) return;
        if (CrystalRespawnManager.Instance == null) return;

        _hitThisActivation = true;
        _col.enabled       = false;
        CrystalRespawnManager.Instance.TakeBossDamage(damage);
    }
}
