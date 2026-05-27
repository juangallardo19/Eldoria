using System.Collections;
using UnityEngine;

// Patrón: State Machine + Observer
// Estados: Idle → Patrol → Chase → Attack → Hurt → Dead
//
// Comportamiento:
//   · Patrulla entre patrolLeft y patrolRight.
//   · Al detectar al jugador (detectRange) cambia a Chase.
//   · Al entrar en attackRange lanza un ataque con arco amplio (BossAttackHitbox en frame hitboxActiveFrame).
//   · La hitbox se voltea automáticamente con _facingDir para apuntar siempre al frente.
//   · Recibe daño de PlayerCombat vía IDamageable.TakeDamage.
//   · 30 HP: 3 golpes de combo1 (10 dmg c/u) para matar.
//   · Flash rojo al recibir daño como retroalimentación visual.
//
// Animación por código (Sprite[]): no requiere Animator ni AnimationClip.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SombraNinja : MonoBehaviour, IDamageable
{
    public enum NinjaState { Idle, Patrol, Chase, Attack, Hurt, Dead }

    // ── Stats ──────────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private int   maxHP        = 30;   // 3 hits de combo1 (10 dmg) = muerte
    [SerializeField] private float detectRange  = 15f;
    [SerializeField] private float loseRange    = 22f;
    [SerializeField] private float attackRange  = 5.5f;  // ataca desde lejos (arco de arma largo)
    [SerializeField] private float patrolSpeed  = 2f;
    [SerializeField] private float chaseSpeed   = 3.5f;
    [SerializeField] private float attackCooldown = 3.5f;

    [Header("Patrulla — límites en X (world space)")]
    [SerializeField] private float patrolLeft  = -10f;
    [SerializeField] private float patrolRight = +10f;

    // ── Animación ──────────────────────────────────────────────────────────────
    [Header("Frames de animación (asignados por SetupSombraNinja)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] deathFrames;

    [Header("FPS de cada animación")]
    [SerializeField] private float idleFps   = 8f;
    [SerializeField] private float attackFps = 5f;   // reducido para que el ataque sea visible y más lento
    [SerializeField] private float deathFps  = 8f;

    // ── Hitbox de ataque ───────────────────────────────────────────────────────
    [Header("Hitbox de ataque (hijo con BossAttackHitbox)")]
    [SerializeField] private BossAttackHitbox attackHitbox;
    [SerializeField] private int   attackDamage      = 1;
    [SerializeField] private int   hitboxActiveFrame = 3;  // frame 0-indexado que activa el hitbox (más temprano = más alcance)

    // ── Estado interno ─────────────────────────────────────────────────────────
    private NinjaState    _state;
    private int           _hp;
    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;
    private Transform      _player;

    // Animación
    private Sprite[] _curAnim;
    private float    _curFps;
    private int      _frameIdx;
    private float    _frameTimer;

    // Temporizadores
    private float _attackCooldownTimer;
    private float _hurtTimer;
    private float _attackDuration;   // duración total del ataque en segundos
    private float _attackElapsed;    // tiempo transcurrido en estado Attack

    // Dirección
    private float         _patrolDir = 1f;
    private float         _facingDir = 1f;
    private bool          _hitboxFiredThisAttack;
    private BoxCollider2D _attackHitboxCol;  // offset.x se invierte con _facingDir

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

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

        // El jugador puede atravesar al ninja: ignorar colisión física entre
        // el collider de cuerpo del ninja y todos los colliders del jugador.
        // Así el ninja NO actúa como plataforma ni bloquea el movimiento.
        if (_player != null)
        {
            var bodyCol    = GetComponent<BoxCollider2D>();
            var playerCols = _player.GetComponents<Collider2D>();
            foreach (var pc in playerCols)
                Physics2D.IgnoreCollision(bodyCol, pc, true);
        }

        // Cachear el BoxCollider2D del hitbox de ataque para poder voltear su offset.x
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

    // ── Lógica del State Machine ───────────────────────────────────────────────

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

            // Activar hitbox en el frame correcto
            if (!_hitboxFiredThisAttack && _frameIdx >= hitboxActiveFrame)
            {
                _hitboxFiredThisAttack = true;
                attackHitbox?.Activate(attackDamage);
            }
            // Desactivar hitbox 0.2 s después de activarlo
            if (_hitboxFiredThisAttack && _attackElapsed > (hitboxActiveFrame / Mathf.Max(attackFps, 1f)) + 0.2f)
                attackHitbox?.Deactivate();
        }
    }

    private void DecideState()
    {
        // Hurt: bloqueado hasta que expire
        if (_state == NinjaState.Hurt)
        {
            if (_hurtTimer <= 0f) TransitionTo(GetStateByDistance());
            return;
        }

        // Attack: bloqueado hasta que termine la animación
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

    // ── Transiciones ──────────────────────────────────────────────────────────

    private void TransitionTo(NinjaState next)
    {
        if (_state == next) return;

        // Limpieza al salir del ataque
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

    // ── Animación ─────────────────────────────────────────────────────────────

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

    // ── Dirección ─────────────────────────────────────────────────────────────

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

        // Voltear el offset.x del hitbox para que siempre apunte al frente.
        // flipX solo voltea el sprite; el BoxCollider2D no se mueve solo.
        if (_attackHitboxCol != null)
        {
            var off = _attackHitboxCol.offset;
            off.x   = Mathf.Abs(off.x) * _facingDir;
            _attackHitboxCol.offset = off;
        }
    }

    // ── Efectos visuales ──────────────────────────────────────────────────────

    private IEnumerator HurtFlash()
    {
        Color original = _sr.color;
        Color redTint  = new Color(1f, 0.25f, 0.25f, 1f);  // filtro rojo indicador de daño recibido
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

    // ── Gizmos de debug ───────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Rango de detección (verde)
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Rango de ataque (rojo)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Zona de patrulla (amarillo)
        Gizmos.color = Color.yellow;
        Vector3 floor = transform.position;
        Gizmos.DrawLine(new Vector3(patrolLeft,  floor.y, 0f),
                        new Vector3(patrolRight, floor.y, 0f));
        Gizmos.DrawWireSphere(new Vector3(patrolLeft,  floor.y, 0f), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(patrolRight, floor.y, 0f), 0.3f);
    }
#endif
}
