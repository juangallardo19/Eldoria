using System.Collections;
using UnityEngine;

// Pattern: State Machine + Observer
// States: Idle → Patrol → Chase → Attack → Hurt → Dead
//
// Behaviour:
//   · Patrols between patrolLeft and patrolRight.
//   · Detects the player at detectRange; chases but STOPS at attackRange.
//   · Launches an animated SombraProyectil (projectile that grows in flight) after castDelay.
//   · 30 HP: 3 combo1 hits to die.
//   · Red flash on damage received.
//   · The player can pass through it (Physics2D.IgnoreCollision in Start).
//
// Code-driven animation (Sprite[]): no Animator or AnimationClip required.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SombraMago : MonoBehaviour, IDamageable
{
    public enum MagoState { Idle, Patrol, Chase, Attack, Hurt, Dead }

    // ── Stats ──────────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private int   maxHP         = 30;    // 3 combo1 hits (10 dmg each)
    [SerializeField] private float detectRange   = 18f;
    [SerializeField] private float loseRange     = 24f;
    [SerializeField] private float attackRange   = 10f;   // fires from range
    [SerializeField] private float patrolSpeed   = 1.8f;
    [SerializeField] private float chaseSpeed    = 3f;
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private float castDelay     = 0.5f;  // charge time before firing

    [Header("Patrol — X bounds (world space)")]
    [SerializeField] private float patrolLeft  = -10f;
    [SerializeField] private float patrolRight = +10f;

    // ── Animation ──────────────────────────────────────────────────────────────
    [Header("Animation frames (assigned by SetupSombraMago)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] attackFrames;  // atack sombra1-6: body animation while casting
    [SerializeField] private Sprite[] hurtFrames;

    [Header("FPS per animation")]
    [SerializeField] private float idleFps   = 8f;
    [SerializeField] private float attackFps = 9f;   // 6 frames / 9fps ≈ 0.67s (castDelay=0.4s fires on frame 4)
    [SerializeField] private float hurtFps   = 8f;

    // ── Projectile ─────────────────────────────────────────────────────────────
    [Header("Proyectil")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int        projectileDamage = 1;

    // ── Internal state ────────────────────────────────────────────────────────
    private MagoState    _state;
    private int          _hp;
    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;
    private Transform      _player;

    // Animation
    private Sprite[] _curAnim;
    private float    _curFps;
    private int      _frameIdx;
    private float    _frameTimer;

    // Temporizadores
    private float _attackCooldownTimer;
    private float _hurtTimer;
    private float _attackElapsed;
    private bool  _projectileFired;

    // Facing direction
    private float _patrolDir = 1f;
    private float _facingDir = 1f;

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

        _rb.gravityScale           = 3f;
        _rb.freezeRotation         = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // The player can pass through the mage (it doesn't block or act as a platform)
        if (_player != null)
        {
            var bodyCol    = GetComponent<BoxCollider2D>();
            var playerCols = _player.GetComponents<Collider2D>();
            foreach (var pc in playerCols)
                Physics2D.IgnoreCollision(bodyCol, pc, true);
        }

        TransitionTo(MagoState.Patrol);
    }

    void Update()
    {
        AdvanceAnimation();
        if (_state == MagoState.Dead) return;

        TickTimers();
        DecideState();
        UpdateFacing();
    }

    void FixedUpdate()
    {
        if (_state == MagoState.Dead)   return;
        if (_state == MagoState.Attack) { _rb.velocity = new Vector2(0f, _rb.velocity.y); return; }
        if (_state == MagoState.Hurt)   { _rb.velocity = new Vector2(0f, _rb.velocity.y); return; }

        float vx = 0f;

        if (_state == MagoState.Patrol)
        {
            vx = _patrolDir * patrolSpeed;
            if (transform.position.x >= patrolRight && _patrolDir > 0f) _patrolDir = -1f;
            if (transform.position.x <= patrolLeft  && _patrolDir < 0f) _patrolDir =  1f;
        }
        else if (_state == MagoState.Chase && _player != null)
        {
            float dx = Mathf.Abs(_player.position.x - transform.position.x);
            if (dx > attackRange + 0.5f)
                vx = Mathf.Sign(_player.position.x - transform.position.x) * chaseSpeed;
        }

        _rb.velocity = new Vector2(vx, _rb.velocity.y);
    }

    // ── State Machine ──────────────────────────────────────────────────────────

    private void TickTimers()
    {
        if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;

        if (_state == MagoState.Hurt)
        {
            _hurtTimer -= Time.deltaTime;
        }
        else if (_state == MagoState.Attack)
        {
            _attackElapsed += Time.deltaTime;

            // Fire the projectile after castDelay
            if (!_projectileFired && _attackElapsed >= castDelay)
            {
                _projectileFired = true;
                FireProjectile();
            }
        }
    }

    private void DecideState()
    {
        if (_state == MagoState.Hurt)
        {
            if (_hurtTimer <= 0f) TransitionTo(GetStateByDistance());
            return;
        }

        if (_state == MagoState.Attack)
        {
            // Leave Attack state only after firing + brief pause
            if (_projectileFired && _attackElapsed >= castDelay + 0.3f)
                TransitionTo(GetStateByDistance());
            return;
        }

        TransitionTo(GetStateByDistance());
    }

    private MagoState GetStateByDistance()
    {
        if (_player == null) return MagoState.Patrol;
        float dx   = Mathf.Abs(_player.position.x - transform.position.x);
        float dist = Vector2.Distance(transform.position, _player.position);

        if (dx > loseRange)  return MagoState.Patrol;
        if (dx <= attackRange && _attackCooldownTimer <= 0f) return MagoState.Attack;
        if (dx <= detectRange)  return MagoState.Chase;
        return MagoState.Patrol;
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    private void TransitionTo(MagoState next)
    {
        if (_state == next) return;

        _state           = next;
        _frameIdx        = 0;
        _frameTimer      = 0f;
        _attackElapsed   = 0f;
        _projectileFired = false;

        switch (next)
        {
            case MagoState.Idle:
            case MagoState.Patrol:
            case MagoState.Chase:
                PlayAnim(idleFrames, idleFps);
                break;

            case MagoState.Attack:
                // Mage body frames during casting (atack sombra1-6).
                // If not assigned, falls back to idle.
                var atkAnim = (attackFrames != null && attackFrames.Length > 0) ? attackFrames : idleFrames;
                PlayAnim(atkAnim, attackFps);
                _attackCooldownTimer = attackCooldown;
                break;

            case MagoState.Hurt:
                _hurtTimer = 0.35f;
                PlayAnim(hurtFrames != null && hurtFrames.Length > 0 ? hurtFrames : idleFrames, hurtFps);
                StartCoroutine(HurtFlash());
                break;

            case MagoState.Dead:
                PlayAnim(hurtFrames != null && hurtFrames.Length > 0 ? hurtFrames : idleFrames, hurtFps);
                _rb.velocity    = new Vector2(0f, _rb.velocity.y);
                _rb.isKinematic = true;
                float delay = (hurtFrames != null && hurtFrames.Length > 0)
                              ? hurtFrames.Length / Mathf.Max(hurtFps, 1f) + 0.4f : 1.5f;
                Destroy(gameObject, delay);
                break;
        }
    }

    // ── Projectile ────────────────────────────────────────────────────────────

    private void FireProjectile()
    {
        if (projectilePrefab == null || _player == null) return;

        // Fire from the body centre (BoxCollider offset in world space) so the projectile
        // doesn't spawn at the floor and immediately self-destruct.
        var bodyCol = GetComponent<BoxCollider2D>();
        float ctrY  = bodyCol != null ? bodyCol.offset.y * transform.lossyScale.y : 1.3f;

        Vector2 origin = (Vector2)transform.position + new Vector2(0f, ctrY);
        // Aim at the player's centre; if too close, guarantee a horizontal direction
        float rawDx = _player.position.x - transform.position.x;
        float aimX  = Mathf.Abs(rawDx) < 0.5f ? _facingDir : rawDx;
        Vector2 target = new Vector2(transform.position.x + aimX, _player.position.y + ctrY);
        Vector2 dir    = (target - origin).normalized;

        var go   = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var proj = go.GetComponent<SombraProyectil>();
        proj?.Launch(dir, projectileDamage);
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void PlayAnim(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;
        _curAnim    = frames;
        _curFps     = Mathf.Max(fps, 1f);
        _frameIdx   = 0;
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
            if (_state == MagoState.Dead)
                _frameIdx = Mathf.Min(_frameIdx + 1, _curAnim.Length - 1);
            else
                _frameIdx = (_frameIdx + 1) % _curAnim.Length;
        }

        if (_curAnim[_frameIdx] != null)
            _sr.sprite = _curAnim[_frameIdx];
    }

    // ── Facing direction ──────────────────────────────────────────────────────

    private void UpdateFacing()
    {
        if (_state == MagoState.Patrol)
        {
            _facingDir = _patrolDir;
        }
        else if ((_state == MagoState.Chase || _state == MagoState.Attack) && _player != null)
        {
            float dx = _player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.1f) _facingDir = Mathf.Sign(dx);
        }

        _sr.flipX = _facingDir < 0f;
    }

    // ── Visual effects ────────────────────────────────────────────────────────

    private IEnumerator HurtFlash()
    {
        Color original = _sr.color;
        Color redTint  = new Color(1f, 0.25f, 0.25f, 1f);
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
        if (_state == MagoState.Dead) return;

        _hp = Mathf.Max(0, _hp - damage);
        if (_hp == 0)
            TransitionTo(MagoState.Dead);
        else
            TransitionTo(MagoState.Hurt);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = new Color(0.5f, 0.2f, 1f, 0.35f);  // purple = magic range
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Vector3 floor = transform.position;
        Gizmos.DrawLine(new Vector3(patrolLeft,  floor.y, 0f),
                        new Vector3(patrolRight, floor.y, 0f));
        Gizmos.DrawWireSphere(new Vector3(patrolLeft,  floor.y, 0f), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(patrolRight, floor.y, 0f), 0.3f);
    }
#endif
}
