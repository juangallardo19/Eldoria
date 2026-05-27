using System;
using UnityEngine;

// State Machine — estados: Idle, Walking, Running, Jumping, Falling, Dashing, WallSliding, WallJumping
// PlayerAnimator observa las propiedades públicas de estado para actualizar el Animator.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float runSpeed  = 16f;

    [Header("Salto")]
    [SerializeField] private float jumpMinForce   = 13f;   // fuerza al tap rápido
    [SerializeField] private float jumpForce      = 18f;   // fuerza máxima al mantener Z
    [SerializeField] private float jumpHoldTime   = 0.3f;  // segundos máximos de hold
    [SerializeField] private float jumpHoldBoost  = 30f;   // aceleración extra mientras se mantiene Z
    [SerializeField] private float coyoteTime     = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float fallMultiplier = 2.0f;
    [SerializeField] private float lowJumpMult    = 1.5f;

    [Header("Caída acelerada")]
    [SerializeField] private float fastFallAccel  = 30f;   // aceleración extra al presionar ↓ mientras cae

    [Header("Dash")]
    [SerializeField] private float dashForce    = 45f;
    [SerializeField] private float dashDuration = 0.22f;
    [SerializeField] private float dashCooldown = 0.2f;

    [Header("Wall Slide / Jump")]
    [SerializeField] private float wallSlideSpeed  = 1.5f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(9f, 16f);

    [Header("Combate — ralentización al atacar")]
    [SerializeField] private float attackWalkMult = 0.45f; // fracción de walkSpeed cuando se ataca caminando

    [Header("Float (Bioma 4)")]
    [SerializeField] private float floatGravity = 0.3f;
    [SerializeField] private float floatMaxTime = 3f;

    [Header("Habilidades — se desbloquean por progreso")]
    public bool hasDoubleJump = false;
    public bool hasWallClimb  = false;
    public bool hasFloat      = false;
    public bool hasTeleport   = false;
    public bool hasDash       = false;

    [Header("Detección de suelo y pared")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheckL;
    [SerializeField] private Transform wallCheckR;
    [SerializeField] private float     checkRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    // ── Eventos estáticos (Observer — TutorialGate se suscribe) ──────────
    public static event Action OnPlayerMoved;
    public static event Action OnPlayerJumped;
    public static event Action OnPlayerHeldJump;
    public static event Action OnPlayerAttacked;
    public static event Action OnPlayerDropped;
    public static event Action OnPlayerRan;

    // ── Estado público (leído por PlayerAnimator) ─────────────────────────
    public bool  IsGrounded    { get; private set; }
    public bool  IsRunning     { get; private set; }
    public bool  IsJumping     { get; private set; }
    public bool  IsFalling     { get; private set; }
    public bool  IsDashing     { get; private set; }
    public bool  IsWallSliding { get; private set; }
    public float FacingDir     { get; private set; } = 1f;
    public float VelocityY     => rb.velocity.y;
    public float SpeedX        => Mathf.Abs(rb.velocity.x);

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private PlayerCombat _combat;
    private Vector3 _baseScale;
    private bool  _runningMode;

    // Salto variable + momentum aéreo
    private bool  _jumpHolding;       // Z está siendo mantenido tras el salto
    private float _jumpHoldTimer;     // tiempo restante de hold
    private float _airSpeed;          // velocidad horizontal bloqueada al saltar
    private bool  _isJumpAirborne;    // true si el aire es resultado de un salto (no caída libre)

    private bool  usedDoubleJump;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool  isDashingInternal;
    private bool  _airDashUsed;        // dash consumido en el aire; se resetea al tocar suelo
    private float dashTimer;
    private float dashCooldownTimer;
    private bool  isOnWallL, isOnWallR;
    private bool  isFloating;
    private float floatTimer;
    private bool  _isOnRamp;

    // Buffer de contactos reutilizable (sin GC por frame)
    private readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    [Header("Sonidos del jugador")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip runClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip hurtClip;

    private AudioSource _sfxSource;   // one-shots (salto, aterrizaje, ataque, daño, pasos)
    private bool        _wasGrounded;
    private float       _airtime;           // tiempo consecutivo en el aire
    [SerializeField] private float minAirtimeForLandSound = 0.25f;

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        boxCol  = GetComponent<BoxCollider2D>();
        _combat = GetComponent<PlayerCombat>();
        _baseScale = transform.localScale;
        groundLayer = 1 << 8;

        // Garantizar que el HUD de vidas exista en cualquier escena con jugador.
        // PlayerLivesHUD es DontDestroyOnLoad; si ya existe de una escena anterior, este Awake no hace nada.
        if (FindObjectOfType<PlayerLivesHUD>() == null)
            new GameObject("PlayerLivesHUD").AddComponent<PlayerLivesHUD>();

        // AudioSource para one-shots (salto, aterrizaje, ataque, daño, pasos)
        _sfxSource             = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        // Auto-cargar clips desde Resources/Audio/Player/ si no están asignados en Inspector.
        if (jumpClip   == null) jumpClip   = Resources.Load<AudioClip>("Audio/Player/jump");
        if (landClip   == null) landClip   = Resources.Load<AudioClip>("Audio/Player/land");
        if (walkClip   == null) walkClip   = Resources.Load<AudioClip>("Audio/Player/walk");
        if (runClip    == null) runClip    = Resources.Load<AudioClip>("Audio/Player/run");
        if (attackClip == null) attackClip = Resources.Load<AudioClip>("Audio/Player/attack");
        if (hurtClip   == null) hurtClip   = Resources.Load<AudioClip>("Audio/Player/hurt");
    }

    void Start()
    {
        // Restaurar dash si el boss ya fue derrotado en esta partida
        if (SaveManager.ActiveSlot >= 0 && SaveManager.Instance != null)
        {
            var saved = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            if (saved != null && (saved.bossDefeated || saved.hasDash))
                hasDash = true;
        }
    }

    void Update()
    {
        UpdateChecks();

        // Bloquear todo input del jugador mientras hay diálogo activo
        if (DialogueManager.IsActive) { ApplyBetterGravity(); return; }

        if (isDashingInternal)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) EndDash();
            IsDashing = true;
            return;
        }
        IsDashing = false;

        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        // Run toggle solo en suelo — no se puede cambiar velocidad en el aire
        if (IsGrounded && Input.GetKeyDown(GetKey("Run", KeyCode.LeftShift)))
        {
            _runningMode = !_runningMode;
            if (_runningMode) OnPlayerRan?.Invoke();
        }

        coyoteCounter     = IsGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
        jumpBufferCounter = Input.GetKeyDown(GetKey("Jump", KeyCode.Z))
            ? jumpBufferTime
            : jumpBufferCounter - Time.deltaTime;

        // Detectar aterrizaje: solo suena si el jugador estuvo suficiente tiempo en el aire.
        // Evita que el sonido se dispare por micro-contactos con plataformas o rampas.
        if (!IsGrounded) _airtime += Time.deltaTime;
        if (IsGrounded && !_wasGrounded)
        {
            if (landClip != null && _airtime >= minAirtimeForLandSound)
                _sfxSource.PlayOneShot(landClip);
            _airtime = 0f;
        }
        _wasGrounded = IsGrounded;

        HandleDropThrough();
        HandleMovement();
        HandleJump();
        HandleJumpHold();
        HandleFastFall();
        HandleDash();
        HandleWallSlide();
        HandleFloat();
        ApplyBetterGravity();
        UpdateStateFlags();
    }

    // ── Detección ──────────────────────────────────────────────────────────
    private void UpdateChecks()
    {
        // Contactos del rigidbody: normal.y > 0.5 = superficie pisable
        int contactCount = rb.GetContacts(_contacts);
        IsGrounded = false;
        _isOnRamp  = false;
        for (int i = 0; i < contactCount; i++)
        {
            if (_contacts[i].normal.y > 0.5f)
            {
                IsGrounded = true;
                // Detecta superficie inclinada (rampa) por la componente X de la normal
                if (Mathf.Abs(_contacts[i].normal.x) > 0.1f)
                    _isOnRamp = true;
            }
        }

        float r = checkRadius * Mathf.Abs(transform.lossyScale.y);
        isOnWallL = wallCheckL != null &&
            Physics2D.OverlapCircle(wallCheckL.position, r, groundLayer);
        isOnWallR = wallCheckR != null &&
            Physics2D.OverlapCircle(wallCheckR.position, r, groundLayer);

        if (IsGrounded)
        {
            usedDoubleJump   = false;
            isFloating       = false;
            _isJumpAirborne  = false;
            _jumpHolding     = false;
        }
    }

    // ── Movimiento horizontal ──────────────────────────────────────────────
    private void HandleMovement()
    {
        // Ataque en suelo: ralentiza el movimiento en vez de bloquearlo.
        // Corriendo → baja a walkSpeed; Caminando → baja a walkSpeed * attackWalkMult.
        if (_combat != null && _combat.IsAttacking && IsGrounded)
        {
            float hAtk = 0f;
            if (Input.GetKey(GetKey("MoveLeft",  KeyCode.A)) || Input.GetKey(KeyCode.LeftArrow))  hAtk = -1f;
            if (Input.GetKey(GetKey("MoveRight", KeyCode.D)) || Input.GetKey(KeyCode.RightArrow)) hAtk =  1f;

            float attackSpeed = _runningMode ? walkSpeed : walkSpeed * attackWalkMult;
            rb.velocity = new Vector2(hAtk * attackSpeed, rb.velocity.y);
            IsRunning = false;
            return;
        }

        float h = 0f;
        if (Input.GetKey(GetKey("MoveLeft",  KeyCode.A)) || Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(GetKey("MoveRight", KeyCode.D)) || Input.GetKey(KeyCode.RightArrow)) h =  1f;

        // Velocidad: en suelo usa modo actual; en aire usa la velocidad bloqueada al saltar
        float speed;
        if (IsGrounded)
            speed = _runningMode ? runSpeed : walkSpeed;
        else if (_isJumpAirborne)
            speed = _airSpeed;   // bloqueada al momento del salto
        else
            speed = _runningMode ? runSpeed : walkSpeed;  // caída libre desde plataforma: control normal

        if (!isFloating && !IsWallSliding)
        {
            // En rampa sin input: para en seco para evitar el "salto residual"
            if (_isOnRamp && h == 0f)
                rb.velocity = new Vector2(0f, 0f);
            else
                rb.velocity = new Vector2(h * speed, rb.velocity.y);
        }

        if (h != 0f)
        {
            FacingDir = Mathf.Sign(h);
            transform.localScale = new Vector3(FacingDir * Mathf.Abs(_baseScale.x), _baseScale.y, 1f);
            OnPlayerMoved?.Invoke();
        }

        IsRunning = IsGrounded && Mathf.Abs(h) > 0.05f && _runningMode;
    }

    // ── Salto (+ variable height + double jump + wall jump) ───────────────
    private void HandleJump()
    {
        bool wallPresent = (isOnWallL || isOnWallR) && hasWallClimb;

        // Wall jump
        if (jumpBufferCounter > 0f && wallPresent && !IsGrounded)
        {
            float dir = isOnWallR ? -1f : 1f;
            rb.velocity       = new Vector2(dir * wallJumpForce.x, wallJumpForce.y);
            jumpBufferCounter = 0f;
            _airSpeed         = wallJumpForce.x;
            _isJumpAirborne   = true;
            FacingDir = dir;
            transform.localScale = new Vector3(dir * Mathf.Abs(_baseScale.x), _baseScale.y, 1f);
            if (jumpClip != null) _sfxSource.PlayOneShot(jumpClip);
            return;
        }

        // Normal / coyote jump — bloquea velocidad horizontal al saltar
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.velocity       = new Vector2(rb.velocity.x, jumpMinForce);
            jumpBufferCounter = 0f;
            coyoteCounter     = 0f;
            _jumpHolding      = true;
            _jumpHoldTimer    = jumpHoldTime;
            _airSpeed         = _runningMode ? runSpeed : walkSpeed;
            _isJumpAirborne   = true;
            if (jumpClip != null) _sfxSource.PlayOneShot(jumpClip);
            OnPlayerJumped?.Invoke();
            return;
        }

        // Double jump
        if (jumpBufferCounter > 0f && hasDoubleJump && !usedDoubleJump && !IsGrounded)
        {
            rb.velocity       = new Vector2(rb.velocity.x, jumpMinForce);
            usedDoubleJump    = true;
            jumpBufferCounter = 0f;
            _jumpHolding      = true;
            _jumpHoldTimer    = jumpHoldTime;
            if (jumpClip != null) _sfxSource.PlayOneShot(jumpClip);
        }
    }

    // ── Salto variable: hold Z para mayor altura ───────────────────────────
    // Patrón: sin nombre formal — es un modificador de impulso basado en input continuo.
    private void HandleJumpHold()
    {
        if (_combat != null && _combat.IsAttacking)
        {
            _jumpHolding = false;
            return;
        }
        if (!_jumpHolding) return;

        bool holdingKey = Input.GetKey(GetKey("Jump", KeyCode.Z));
        if (holdingKey && _jumpHoldTimer > 0f && rb.velocity.y < jumpForce && !IsGrounded)
        {
            _jumpHoldTimer -= Time.deltaTime;
            rb.velocity = new Vector2(rb.velocity.x,
                Mathf.Min(rb.velocity.y + jumpHoldBoost * Time.deltaTime, jumpForce));
            OnPlayerHeldJump?.Invoke();
        }
        else
        {
            _jumpHolding = false;
        }
    }

    // ── Caída acelerada + cancel de salto con ↓ ───────────────────────────
    private void HandleFastFall()
    {
        if (IsGrounded || isDashingInternal || isFloating) return;

        bool downHeld = Input.GetKey(KeyCode.DownArrow)     || Input.GetKey(KeyCode.S);
        bool downDown = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);

        // ↓ mientras se sube → cancela el hold del salto y detiene la subida
        if (downDown && rb.velocity.y > 0f)
        {
            _jumpHolding = false;
            rb.velocity  = new Vector2(rb.velocity.x, 0f);
        }

        // Aceleración extra hacia abajo mientras se cae
        if (rb.velocity.y < 0f && downHeld)
            rb.velocity += Vector2.down * fastFallAccel * Time.deltaTime;
    }

    // ── Drop-through: ↓ mientras está en suelo sobre OneWayPlatform ─────────
    // Delega el drop al componente OneWayPlatform (sistema propio, sin PlatformEffector2D).
    private void HandleDropThrough()
    {
        if (!IsGrounded) return;
        if (!Input.GetKeyDown(KeyCode.DownArrow) && !Input.GetKeyDown(KeyCode.S)) return;

        int contactCount = rb.GetContacts(_contacts);
        for (int i = 0; i < contactCount; i++)
        {
            if (_contacts[i].normal.y > 0.5f && _contacts[i].collider != null)
            {
                var owp = _contacts[i].collider.GetComponent<OneWayPlatform>();
                if (owp != null)
                {
                    owp.TriggerDropThrough(0.3f);
                    rb.velocity = new Vector2(rb.velocity.x, -5f);
                    OnPlayerDropped?.Invoke();
                    return;
                }

                var owr = _contacts[i].collider.GetComponent<OneWayRamp>();
                if (owr != null)
                {
                    owr.TriggerDropThrough(0.3f);
                    rb.velocity = new Vector2(rb.velocity.x, -5f);
                    return;
                }
            }
        }
    }

    // ── Dash ───────────────────────────────────────────────────────────────
    private void HandleDash()
    {
        if (!hasDash) return;
        if (IsGrounded) _airDashUsed = false;   // aterrizó → puede volver a usar dash en el aire
        if (!Input.GetKeyDown(GetKey("Dash", KeyCode.C))) return;
        if (dashCooldownTimer > 0f || isDashingInternal) return;
        if (!IsGrounded && _airDashUsed) return;   // ya usó el dash en el aire

        isDashingInternal = true;
        dashTimer         = dashDuration;
        dashCooldownTimer = dashCooldown;
        isFloating        = false;
        if (!IsGrounded) _airDashUsed = true;   // marcar dash aéreo como usado
        rb.velocity       = new Vector2(FacingDir * dashForce, 0f);
        rb.gravityScale   = 0f;
    }

    private void EndDash()
    {
        isDashingInternal = false;
        rb.gravityScale   = 1f;
    }

    // ── Wall slide ─────────────────────────────────────────────────────────
    private void HandleWallSlide()
    {
        if (!hasWallClimb || IsGrounded) { IsWallSliding = false; return; }

        bool onWall = (isOnWallR && FacingDir > 0f) || (isOnWallL && FacingDir < 0f);
        IsWallSliding = onWall && rb.velocity.y < 0f;

        if (IsWallSliding)
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
    }

    // ── Float ──────────────────────────────────────────────────────────────
    private void HandleFloat()
    {
        if (!hasFloat) return;

        if (Input.GetKeyDown(GetKey("Float", KeyCode.F)) && !IsGrounded)
        {
            isFloating      = true;
            floatTimer      = floatMaxTime;
            rb.velocity     = new Vector2(rb.velocity.x, 0f);
            rb.gravityScale = floatGravity;
        }

        if (isFloating)
        {
            floatTimer -= Time.deltaTime;
            if (floatTimer <= 0f || IsGrounded || Input.GetKeyDown(GetKey("Float", KeyCode.F)))
            {
                isFloating      = false;
                rb.gravityScale = 1f;
            }
        }
    }

    // ── Gravedad mejorada ─────────────────────────────────────────────────
    private void ApplyBetterGravity()
    {
        // Cuando está pisando suelo (incluye rampas), no aplicar gravedad extra:
        // en rampa causaría contrafuerza vs la velocidad de subida.
        if (isDashingInternal || isFloating || IsGrounded) return;

        if (rb.velocity.y < 0f)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
        else if (rb.velocity.y > 0f && !Input.GetKey(GetKey("Jump", KeyCode.Z)))
            // lowJumpMult no aplica si Z está presionado (HandleJumpHold está activo)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMult - 1f) * Time.deltaTime;
    }

    // ── Flags de estado para el Animator ──────────────────────────────────
    private void UpdateStateFlags()
    {
        IsJumping = !IsGrounded && rb.velocity.y > 0.1f;
        IsFalling = !IsGrounded && rb.velocity.y < -0.1f && !IsWallSliding;
    }

    // ── Sonido de pasos — llamado por Animation Events en Walk.anim y Run.anim ─
    // Agregar eventos en Unity: Animation window → frame de impacto del pie → función "OnFootstep".
    public void OnFootstep()
    {
        if (!IsGrounded || isDashingInternal) return;
        if (_combat != null && _combat.IsAttacking) return;
        AudioClip clip = _runningMode ? (runClip != null ? runClip : walkClip) : walkClip;
        if (clip != null) _sfxSource.PlayOneShot(clip);
    }

    // ── Sonidos de ataque y daño (llamados desde PlayerCombat / CrystalRespawnManager) ──
    public void PlayAttackSound() { if (attackClip != null) _sfxSource.PlayOneShot(attackClip); OnPlayerAttacked?.Invoke(); }
    public void PlayHurtSound()   { if (hurtClip   != null) _sfxSource.PlayOneShot(hurtClip);   }

    // ── Running mode persistence across scenes ────────────────────────────
    public bool GetRunningMode() => _runningMode;
    public void SetRunningMode(bool v) { _runningMode = v; }

    // ── Helpers de teclas reasignables ────────────────────────────────────
    private static KeyCode GetKey(string id, KeyCode def) => KeyRebindUI.GetKey(id, def);

    public KeyCode GetAttackKey()   => GetKey("Attack",   KeyCode.J);
    public KeyCode GetInteractKey() => GetKey("Interact", KeyCode.E);
    public KeyCode GetTeleportKey() => GetKey("Teleport", KeyCode.V);
    public KeyCode GetMapKey()      => GetKey("MapOpen",  KeyCode.M);
}
