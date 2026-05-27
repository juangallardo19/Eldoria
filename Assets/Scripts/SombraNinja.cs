using System.Collections;
using UnityEngine;

// Pattern: State Machine + Observer
// States: Idle → Patrol → Chase → Attack → Hurt → Dead
//
// Behaviour:
//   · Patrols between patrolLeft and patrolRight.
//   · Switches to Chase when the player enters detectRange.
//   · At attackRange, executes a wide-arc attack (BossAttackHitbox on hitboxActiveFrame).
//   · The hitbox is flipped automatically with _facingDir to always point forward.
//   · Receives damage from PlayerCombat via IDamageable.TakeDamage.
//   · 30 HP: 3 combo1 hits (10 dmg each) to die.
//   · Red flash on damage as visual feedback.
//
// Code-driven animation (Sprite[]): no Animator or AnimationClip required.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SombraNinja : MonoBehaviour, IDamageable
{
    public enum NinjaState { Idle, Patrol, Chase, Attack, Hurt, Dead }

    // ── Stats ──────────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private int   maxHP        = 30;   // 3 combo1 hits (10 dmg) = death
    [SerializeField] private float detectRange  = 15f;
    [SerializeField] private float loseRange    = 22f;
    [SerializeField] private float attackRange  = 5.5f;  // attacks from range (long weapon arc)
    [SerializeField] private float patrolSpeed  = 2f;
    [SerializeField] private float chaseSpeed   = 3.5f;
    [SerializeField] private float attackCooldown = 3.5f;

    [Header("Patrol — X bounds (world space)")]
    [SerializeField] private float patrolLeft  = -10f;
    [SerializeField] private float patrolRight = +10f;

    // ── Animation ─────────────────────────────────────────────────────────────
    [Header("Animation frames (assigned by SetupSombraNinja)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] deathFrames;

    [Header("FPS per animation")]
    [SerializeField] private float idleFps   = 8f;
    [SerializeField] private float attackFps = 5f;   // lowered so the attack is visible and slower
    [SerializeField] private float deathFps  = 8f;

    // ── Hitbox de ataque ───────────────────────────────────────────────────────
    [Header("Attack hitbox (child with BossAttackHitbox)")]
    [SerializeField] private BossAttackHitbox attackHitbox;
    [SerializeField] private int   attackDamage      = 1;
    [SerializeField] private int   hitboxActiveFrame = 3;  // 0-indexed frame that activates the hitbox (earlier = more reach)

    // ── Internal state ────────────────────────────────────────────────────────
    private NinjaState    _state;
    private int           _hp;
    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;
    private Transform      _player;

    // Animation
    private Sprite[] _curAnim;
    private float    _curFps;
    private int      _frameIdx;
    private float    _frameTimer;

    // Timers
    private float _attackCooldownTimer;
    private float _hurtTimer;
    private float _attackDuration;   // total attack state duration in seconds
    private float _attackElapsed;    // time elapsed in Attack state

    // Facing direction
    private float         _patrolDir = 1f;
    private float         _facingDir = 1f;
    private bool          _hitboxFiredThisAttack;
    private BoxCollider2D _attackHitboxCol;  // offset.x is flipped with _facingDir

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        _hp     = maxHP;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        _rb.gravityScale          = 3f;
        _rb.freezeRotation        = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // The player can pass through the ninja: ignore physics collision between
        // the ninja's body collider and all player colliders.
        // The ninja does NOT act as a platform or block movement.
        if (_player != null)
        {
            var bodyCol    = GetComponent<BoxCollider2D>();
            var playerCols = _player.GetComponents<Collider2D>();
            foreach (var pc in playerCols)
                Physics2D.IgnoreCollision(bodyCol, pc, true);
        }

        // Cache the BoxCollider2D of the attack hitbox so its offset.x can be flipped
        if (attackHitbox != null)
            _attackHitboxCol = attackHitbox.GetComponent<BoxCollider2D>();

        TransitionTo(NinjaState.Patrol);
    }

    void Update()
    {
        AdvanceAnimation();
        if (_state == NinjaState.Dead) return;

        TickTimers();
        DecideState();
        UpdateFacing();
    }

    void FixedUpdate()
    {
        if (_state == NinjaState.Dead)   return;
        if (_state == NinjaState.Attack) { _rb.velocity = new Vector2(0f, _rb.velocity.y); return; }
        if (_state == NinjaState.Hurt)   { _rb.velocity = new Vector2(0f, _rb.velocity.y); return; }

        float vx = 0f;

        if (_state == NinjaState.Patrol)
        {
            vx = _patrolDir * patrolSpeed;
            if (transform.position.x >= patrolRight && _patrolDir > 0f) _patrolDir = -1f;
            if (transform.position.x <= patrolLeft  && _patrolDir < 0f) _patrolDir =  1f;
        }
        else if (_state == NinjaState.Chase && _player != null)
        {
            vx = Mathf.Sign(_player.position.x - transform.position.x) * chaseSpeed;
        }

        _rb.velocity = new Vector2(vx, _rb.velocity.y);
    }

    // ── State machine logic ───────────────────────────────────────────────────

    private void TickTimers()
    {
        if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;

        if (_state == NinjaState.Hurt)
        {
            _hurtTimer -= Time.deltaTime;
        }
        else if (_state == NinjaState.Attack)
        {
            _attackElapsed += Time.deltaTime;

            // Activate hitbox on the correct frame
            if (!_hitboxFiredThisAttack && _frameIdx >= hitboxActiveFrame)
            {
                _hitboxFiredThisAttack = true;
                attackHitbox?.Activate(attackDamage);
            }
            // Deactivate hitbox 0.2s after activating it
            if (_hitboxFiredThisAttack && _attackElapsed > (hitboxActiveFrame / Mathf.Max(attackFps, 1f)) + 0.2f)
                attackHitbox?.Deactivate();
        }
    }

    private void DecideState()
    {
        // Hurt: locked until it expires
        if (_state == NinjaState.Hurt)
        {
            if (_hurtTimer <= 0f) TransitionTo(GetStateByDistance());
            return;
        }

        // Attack: locked until the animation finishes
        if (_state == NinjaState.Attack)
        {
            if (_attackElapsed >= _attackDuration)
            {
                attackHitbox?.Deactivate();
                TransitionTo(GetStateByDistance());
            }
            return;
        }

        TransitionTo(GetStateByDistance());
    }

    private NinjaState GetStateByDistance()
    {
        if (_player == null) return NinjaState.Patrol;
        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist <= attackRange && _attackCooldownTimer <= 0f)
            return NinjaState.Attack;
        if (dist <= detectRange)
            return NinjaState.Chase;
        return NinjaState.Patrol;
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    private void TransitionTo(NinjaState next)
    {
        if (_state == next) return;

        // Cleanup when leaving Attack
        if (_state == NinjaState.Attack) attackHitbox?.Deactivate();

        _state                 = next;
        _frameIdx              = 0;
        _frameTimer            = 0f;
        _hitboxFiredThisAttack = false;
        _attackElapsed         = 0f;

        switch (next)
        {
            case NinjaState.Idle:
            case NinjaState.Patrol:
            case NinjaState.Chase:
                PlayAnim(idleFrames, idleFps);
                break;

            case NinjaState.Attack:
                PlayAnim(attackFrames, attackFps);
                _attackDuration     = (attackFrames != null && attackFrames.Length > 0)
                                      ? attackFrames.Length / Mathf.Max(attackFps, 1f) : 1f;
                _attackCooldownTimer = attackCooldown;
                break;

            case NinjaState.Hurt:
                _hurtTimer = 0.35f;
                PlayAnim(idleFrames, idleFps);
                StartCoroutine(HurtFlash());
                break;

            case NinjaState.Dead:
                attackHitbox?.Deactivate();
                PlayAnim(deathFrames, deathFps);
                _rb.velocity    = new Vector2(0f, _rb.velocity.y);
                _rb.isKinematic = true;
                float destroyDelay = (deathFrames != null && deathFrames.Length > 0)
                                     ? deathFrames.Length / Mathf.Max(deathFps, 1f) + 0.4f : 1.5f;
                Destroy(gameObject, destroyDelay);
                break;
        }
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void PlayAnim(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;
        _curAnim  = frames;
        _curFps   = Mathf.Max(fps, 1f);
        _frameIdx = 0;
        _frameTimer = 0f;
        _sr.sprite  = frames[0];
    }

    private void AdvanceAnimation()
    {
        if (_curAnim == null || _curAnim.Length == 0) return;

        _frameTimer += Time.deltaTime;
        float dur    = 1f / _curFps;

        while (_frameTimer >= dur)
        {
            _frameTimer -= dur;
            if (_state == NinjaState.Dead)
                _frameIdx = Mathf.Min(_frameIdx + 1, _curAnim.Length - 1);
            else
                _frameIdx = (_frameIdx + 1) % _curAnim.Length;
        }

        if (_curAnim[_frameIdx] != null)
            _sr.sprite = _curAnim[_frameIdx];
    }

    // ── Facing direction ─────────────────────────────────────────────────────

    private void UpdateFacing()
    {
        if (_state == NinjaState.Patrol)
        {
            _facingDir = _patrolDir;
        }
        else if ((_state == NinjaState.Chase || _state == NinjaState.Attack) && _player != null)
        {
            float dx = _player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.1f) _facingDir = Mathf.Sign(dx);
        }

        _sr.flipX = _facingDir < 0f;

        // Flip the hitbox offset.x so it always points forward.
        // flipX only flips the sprite; the BoxCollider2D doesn't move with it.
        if (_attackHitboxCol != null)
        {
            var off = _attackHitboxCol.offset;
            off.x   = Mathf.Abs(off.x) * _facingDir;
            _attackHitboxCol.offset = off;
        }
    }

    // ── Visual effects ────────────────────────────────────────────────────────

    private IEnumerator HurtFlash()
    {
        Color original = _sr.color;
        Color redTint  = new Color(1f, 0.25f, 0.25f, 1f);  // red tint signals damage received
        for (int i = 0; i < 3; i++)
        {
            _sr.color = redTint;
            yield return new WaitForSeconds(0.07f);
            _sr.color = original;
            yield return new WaitForSeconds(0.07f);
        }
        _sr.color = original;
    }

    // ── IDamageable ───────────────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (_state == NinjaState.Dead) return;

        _hp = Mathf.Max(0, _hp - damage);
        if (_hp == 0)
            TransitionTo(NinjaState.Dead);
        else
            TransitionTo(NinjaState.Hurt);
    }

    // ── Debug gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Detection range (green)
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Attack range (red)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Patrol zone (yellow)
        Gizmos.color = Color.yellow;
        Vector3 floor = transform.position;
        Gizmos.DrawLine(new Vector3(patrolLeft,  floor.y, 0f),
                        new Vector3(patrolRight, floor.y, 0f));
        Gizmos.DrawWireSphere(new Vector3(patrolLeft,  floor.y, 0f), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(patrolRight, floor.y, 0f), 0.3f);
    }
#endif
}
