using UnityEngine;

// Kael's melee combo system: press X → Combo1 → Combo2 → Combo3.
// Pattern: State Machine — 4 states (Idle=0, Combo1=1, Combo2=2, Combo3=3).
// · First X press starts Combo1.
// · If X is pressed DURING the chain window of the current hit, the next
//   combo is buffered and executes automatically when the animation ends.
// · After Combo3, or if the chain window expires, reverts to Idle.
//
// Animator integration (KaelAnimator):
//   Trigger "IsAttacking1" → Attack1 state
//   Trigger "IsAttacking2" → Attack2 state
//   Trigger "IsAttacking3" → Attack3 state
//   Each state returns to Idle by ExitTime (no extra condition).
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Duration of each hit")]
    [SerializeField] private float combo1Duration = 0.25f;
    [SerializeField] private float combo2Duration = 0.42f;
    [SerializeField] private float combo3Duration = 0.50f;

    [Header("Chain window (seconds before hit ends)")]
    [SerializeField] private float comboWindow = 0.18f;

    [Header("Idle state name in Animator (to skip attack-end animation)")]
    [SerializeField] private string idleStateName = "Idle";

    [Header("Hitbox — 'AttackHitbox' child (auto-created if missing)")]
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private Vector2 hitboxOffset = new Vector2(2.0f, 1.0f);
    [SerializeField] private Vector2 hitboxSize   = new Vector2(6f, 4f);

    [Header("Damage per hit")]
    [SerializeField] private int combo1Damage = 10;
    [SerializeField] private int combo2Damage = 15;
    [SerializeField] private int combo3Damage = 25;

    [Header("Enemy layer (for hit detection)")]
    [SerializeField] private LayerMask enemyLayer;

    private Animator         _anim;
    private PlayerController _ctrl;

    private int   _comboStep;    // 0=idle, 1/2/3 = active combo
    private float _comboTimer;   // remaining time in current hit
    private bool  _nextQueued;   // X pressed during chain window → execute next combo
    private bool  _hitDealt;     // damage already applied this swing

    public static event System.Action OnCombo3Started;

    private static readonly int _hashAtk1 = Animator.StringToHash("IsAttacking1");
    private static readonly int _hashAtk2 = Animator.StringToHash("IsAttacking2");
    private static readonly int _hashAtk3 = Animator.StringToHash("IsAttacking3");
    private int _hashIdle;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _ctrl      = GetComponent<PlayerController>();
        _anim      = GetComponent<Animator>();
        _hashIdle  = Animator.StringToHash(idleStateName);

        if (hitbox == null)
            hitbox = CreateHitbox();
    }

    void Update()
    {
        bool pressed = Input.GetKeyDown(KeyRebindUI.GetKey("Attack", KeyCode.X));

        // ── Idle: start combo ─────────────────────────────────────────────────
        if (_comboStep == 0)
        {
            if (pressed) StartCombo(1);
            return;
        }

        // ── Active hit ────────────────────────────────────────────────────────
        _comboTimer -= Time.deltaTime;

        // Chain window: last fraction of the hit accepts input for the next combo
        bool inWindow = _comboTimer <= comboWindow;
        if (pressed && inWindow && _comboStep < 3)
            _nextQueued = true;

        // Apply damage on hit-frame (first frame of the active hit)
        if (!_hitDealt)
            ApplyDamage();

        // End of hit
        if (_comboTimer <= 0f)
        {
            if (_nextQueued && _comboStep < 3)
                StartCombo(_comboStep + 1);
            else
                EndCombo();
        }
    }

    // ── State Machine ─────────────────────────────────────────────────────────
    private void StartCombo(int step)
    {
        _comboStep   = step;
        _comboTimer  = DurationOf(step);
        _nextQueued  = false;
        _hitDealt    = false;

        if (_anim != null)
        {
            switch (step)
            {
                case 1: _anim.SetTrigger(_hashAtk1); break;
                case 2: _anim.SetTrigger(_hashAtk2); break;
                case 3: _anim.SetTrigger(_hashAtk3); OnCombo3Started?.Invoke(); break;
            }
        }

        // Hitbox is a child of the player and inherits scale flip — do not multiply by FacingDir.
        if (hitbox != null)
        {
            hitbox.offset  = hitboxOffset;
            hitbox.size    = hitboxSize;
            hitbox.enabled = true;
        }

        _ctrl.PlayAttackSound();
    }

    private void EndCombo()
    {
        _comboStep  = 0;
        _nextQueued = false;
        if (hitbox != null) hitbox.enabled = false;
        if (_anim != null) _anim.Play(_hashIdle, 0, 0f);
    }

    // ── Damage ────────────────────────────────────────────────────────────────
    private void ApplyDamage()
    {
        _hitDealt = true;
        if (hitbox == null) return;

        int dmg = DamageOf(_comboStep);
        Vector2 center = hitbox.bounds.center;

        // If enemyLayer is not configured in Inspector (value 0 = Nothing),
        // fall back to DefaultRaycastLayers to detect any IDamageable in the scene.
        int mask = (int)enemyLayer != 0 ? (int)enemyLayer : Physics2D.DefaultRaycastLayers;
        var hits = Physics2D.OverlapBoxAll(center, hitbox.size, 0f, mask);

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;  // don't damage self
            var dmgable = col.GetComponent<IDamageable>();
            if (dmgable != null) dmgable.TakeDamage(dmg);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private BoxCollider2D CreateHitbox()
    {
        var go = new GameObject("AttackHitbox");
        go.transform.SetParent(transform, false);
        var col    = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.enabled   = false;
        col.offset    = hitboxOffset;
        col.size      = hitboxSize;
        return col;
    }

    private float DurationOf(int step)
    {
        switch (step)
        {
            case 1: return combo1Duration;
            case 2: return combo2Duration;
            case 3: return combo3Duration;
            default: return combo1Duration;
        }
    }

    private int DamageOf(int step)
    {
        switch (step)
        {
            case 1: return combo1Damage;
            case 2: return combo2Damage;
            case 3: return combo3Damage;
            default: return combo1Damage;
        }
    }

    // ── Public state ──────────────────────────────────────────────────────────
    public bool IsAttacking => _comboStep > 0;
    public int  ComboStep   => _comboStep;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_comboStep == 0 || hitbox == null) return;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawCube(hitbox.bounds.center, new Vector3(hitbox.size.x, hitbox.size.y, 0.1f));
    }
#endif
}
