using System.Collections;
using UnityEngine;

// Patrón: State Machine + Observer
// Estados: Idle → Patrol → Chase → Attack → Hurt → Dead
//
// Comportamiento:
//   · Patrulla entre patrolLeft y patrolRight.
//   · Detecta al jugador a detectRange; lo persigue pero SE DETIENE a attackRange.
//   · Lanza un SombraProyectil animado (proyectil que crece en vuelo) tras castDelay.
//   · 30 HP: 3 golpes de combo1 para morir.
//   · Flash rojo al recibir daño.
//   · El jugador puede atravesarlo (Physics2D.IgnoreCollision en Start).
//
// Animación por código (Sprite[]): no requiere Animator ni AnimationClip.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SombraMago : MonoBehaviour, IDamageable
{
    public enum MagoState { Idle, Patrol, Chase, Attack, Hurt, Dead }

    // ── Stats ──────────────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private int   maxHP         = 30;    // 3 hits de combo1 (10 dmg c/u)
    [SerializeField] private float detectRange   = 18f;
    [SerializeField] private float loseRange     = 24f;
    [SerializeField] private float attackRange   = 10f;   // dispara desde lejos
    [SerializeField] private float patrolSpeed   = 1.8f;
    [SerializeField] private float chaseSpeed    = 3f;
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private float castDelay     = 0.5f;  // tiempo de carga antes de disparar

    [Header("Patrulla — límites en X (world space)")]
    [SerializeField] private float patrolLeft  = -10f;
    [SerializeField] private float patrolRight = +10f;

    // ── Animación ──────────────────────────────────────────────────────────────
    [Header("Frames de animación (asignados por SetupSombraMago)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] attackFrames;  // atack sombra1-6: animación del cuerpo al lanzar
    [SerializeField] private Sprite[] hurtFrames;

    [Header("FPS de cada animación")]
    [SerializeField] private float idleFps   = 8f;
    [SerializeField] private float attackFps = 9f;   // 6 frames / 9fps ≈ 0.67s (castDelay=0.4s dispara en frame 4)
    [SerializeField] private float hurtFps   = 8f;

    // ── Proyectil ──────────────────────────────────────────────────────────────
    [Header("Proyectil")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int        projectileDamage = 1;

    // ── Estado interno ─────────────────────────────────────────────────────────
    private MagoState    _state;
    private int          _hp;
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
    private float _attackElapsed;
    private bool  _projectileFired;

    // Dirección
    private float _patrolDir = 1f;
    private float _facingDir = 1f;

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

        _rb.gravityScale           = 3f;
        _rb.freezeRotation         = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // El jugador puede atravesar al mago (no bloquea ni actúa como plataforma)
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

            // Disparar el proyectil tras el castDelay
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
            // Salir del estado Attack solo después de haber disparado + breve pausa
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

    // ── Transiciones ──────────────────────────────────────────────────────────

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
                // Frames del cuerpo del mago durante el lanzamiento (atack sombra1-6).
                // Si no están asignados, usa idle como fallback.
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

    // ── Proyectil ─────────────────────────────────────────────────────────────

    private void FireProjectile()
    {
        if (projectilePrefab == null || _player == null) return;

        // Disparar desde el centro del cuerpo (offset del BoxCollider en world space)
        // para que el proyectil no nazca en el suelo y se autodestruya de inmediato.
        var bodyCol = GetComponent<BoxCollider2D>();
        float ctrY  = bodyCol != null ? bodyCol.offset.y * transform.lossyScale.y : 1.3f;

        Vector2 origin = (Vector2)transform.position + new Vector2(0f, ctrY);
        // Apuntar al centro del jugador; si está demasiado cerca garantizar dirección horizontal
        float rawDx = _player.position.x - transform.position.x;
        float aimX  = Mathf.Abs(rawDx) < 0.5f ? _facingDir : rawDx;
        Vector2 target = new Vector2(transform.position.x + aimX, _player.position.y + ctrY);
        Vector2 dir    = (target - origin).normalized;

        var go   = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var proj = go.GetComponent<SombraProyectil>();
        proj?.Launch(dir, projectileDamage);
    }

    // ── Animación ─────────────────────────────────────────────────────────────

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

    // ── Dirección ─────────────────────────────────────────────────────────────

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

    // ── Efectos visuales ──────────────────────────────────────────────────────

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

        Gizmos.color = new Color(0.5f, 0.2f, 1f, 0.35f);  // púrpura = rango mágico
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
