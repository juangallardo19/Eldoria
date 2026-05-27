using System;
using UnityEngine;

// State Machine — states: Idle, Walking, Running, Jumping, Falling, Dashing, WallSliding, WallJumping
// PlayerAnimator observes public state properties each frame to drive the Animator.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float runSpeed  = 16f;

    [Header("Jump")]
    [SerializeField] private float jumpMinForce   = 13f;   // force on quick tap
    [SerializeField] private float jumpForce      = 18f;   // max force when holding Z
    [SerializeField] private float jumpHoldTime   = 0.3f;  // max hold duration in seconds
    [SerializeField] private float jumpHoldBoost  = 30f;   // extra acceleration while holding Z
    [SerializeField] private float coyoteTime     = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float fallMultiplier = 2.0f;
    [SerializeField] private float lowJumpMult    = 1.5f;

    [Header("Fast Fall")]
    [SerializeField] private float fastFallAccel  = 30f;   // extra downward acceleration when pressing ↓ while falling

    [Header("Dash")]
    [SerializeField] private float dashForce    = 45f;
    [SerializeField] private float dashDuration = 0.22f;
    [SerializeField] private float dashCooldown = 0.2f;

    [Header("Wall Slide / Jump")]
    [SerializeField] private float wallSlideSpeed  = 1.5f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(9f, 16f);

    [Header("Combat — slow on attack")]
    [SerializeField] private float attackWalkMult = 0.45f; // fraction of walkSpeed while attacking on ground

    [Header("Float (Biome 4)")]
    [SerializeField] private float floatGravity = 0.3f;
    [SerializeField] private float floatMaxTime = 3f;

    [Header("Abilities — unlocked through progression")]
    public bool hasDoubleJump = false;
    public bool hasWallClimb  = false;
    public bool hasFloat      = false;
    public bool hasTeleport   = false;
    public bool hasDash       = false;

    [Header("Ground and Wall Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheckL;
    [SerializeField] private Transform wallCheckR;
    [SerializeField] private float     checkRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    // ── Observer events (TutorialGate subscribes to these) ───────────────────
    public static event Action OnPlayerMoved;
    public static event Action OnPlayerJumped;
    public static event Action OnPlayerHeldJump;
    public static event Action OnPlayerAttacked;
    public static event Action OnPlayerDropped;
    public static event Action OnPlayerRan;

    // ── Public state (read by PlayerAnimator) ─────────────────────────────────
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

    // Variable jump + air momentum
    private bool  _jumpHolding;       // Z is held after the initial jump press
    private float _jumpHoldTimer;     // remaining hold time
    private float _airSpeed;          // horizontal speed locked at jump moment
    private bool  _isJumpAirborne;    // true when airborne from a jump (not free fall)

    private bool  usedDoubleJump;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool  isDashingInternal;
    private bool  _airDashUsed;        // dash consumed in air; resets on landing
    private float dashTimer;
    private float dashCooldownTimer;
    private bool  isOnWallL, isOnWallR;
    private bool  isFloating;
    private float floatTimer;
    private bool  _isOnRamp;

    // Reusable contact buffer — avoids GC allocation per frame
    private readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    [Header("Player Sounds")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip runClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip hurtClip;

    private AudioSource _sfxSource;   // one-shots (jump, land, attack, hurt, footsteps)
    private bool        _wasGrounded;
    private float       _airtime;           // consecutive time spent airborne
    [SerializeField] private float minAirtimeForLandSound = 0.25f;

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        boxCol  = GetComponent<BoxCollider2D>();
        _combat = GetComponent<PlayerCombat>();
        _baseScale = transform.localScale;
        groundLayer = 1 << 8;

        // Ensure a PlayerLivesHUD exists in any scene that has a player.
        // PlayerLivesHUD is DontDestroyOnLoad; if it already exists from a previous scene this is a no-op.
        if (FindObjectOfType<PlayerLivesHUD>() == null)
            new GameObject("PlayerLivesHUD").AddComponent<PlayerLivesHUD>();

        _sfxSource             = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        // Auto-load clips from Resources/Audio/Player/ if not assigned in Inspector.
        if (jumpClip   == null) jumpClip   = Resources.Load<AudioClip>("Audio/Player/jump");
        if (landClip   == null) landClip   = Resources.Load<AudioClip>("Audio/Player/land");
        if (walkClip   == null) walkClip   = Resources.Load<AudioClip>("Audio/Player/walk");
        if (runClip    == null) runClip    = Resources.Load<AudioClip>("Audio/Player/run");
        if (attackClip == null) attackClip = Resources.Load<AudioClip>("Audio/Player/attack");
        if (hurtClip   == null) hurtClip   = Resources.Load<AudioClip>("Audio/Player/hurt");
    }

    void Start()
    {
        // Restore dash ability if the boss was already defeated in this save
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

        // Block all player input while dialogue is active
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

        // Run toggle only on ground — cannot change speed mid-air
        if (IsGrounded && Input.GetKeyDown(GetKey("Run", KeyCode.LeftShift)))
        {
            _runningMode = !_runningMode;
            if (_runningMode) OnPlayerRan?.Invoke();
        }

        coyoteCounter     = IsGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
        jumpBufferCounter = Input.GetKeyDown(GetKey("Jump", KeyCode.Z))
            ? jumpBufferTime
            : jumpBufferCounter - Time.deltaTime;

        // Land sound: only plays after sufficient airtime to avoid triggering on micro-contacts.
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

    // ── Detection ─────────────────────────────────────────────────────────────
    private void UpdateChecks()
    {
        // Rigidbody contacts: normal.y > 0.5 = walkable surface
        int contactCount = rb.GetContacts(_contacts);
        IsGrounded = false;
        _isOnRamp  = false;
        for (int i = 0; i < contactCount; i++)
        {
            if (_contacts[i].normal.y > 0.5f)
            {
                IsGrounded = true;
                // Detect inclined surface (ramp) by the X component of the contact normal
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

    // ── Horizontal movement ───────────────────────────────────────────────────
    private void HandleMovement()
    {
        // Attack on ground: slows movement instead of blocking it entirely.
        // Running → drops to walkSpeed; Walking → drops to walkSpeed * attackWalkMult.
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

        // Speed: on ground use current mode; in air use speed locked at jump moment
        float speed;
        if (IsGrounded)
            speed = _runningMode ? runSpeed : walkSpeed;
        else if (_isJumpAirborne)
            speed = _airSpeed;   // locked at jump moment
        else
            speed = _runningMode ? runSpeed : walkSpeed;  // free fall from platform: normal control

        if (!isFloating && !IsWallSliding)
        {
            // On ramp with no input: stop immediately to avoid residual bounce
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

    // ── Jump (+ variable height + double jump + wall jump) ────────────────────
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

        // Normal / coyote jump — locks horizontal speed at jump moment
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

    // ── Variable jump: hold Z for greater height ──────────────────────────────
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

    // ── Fast fall + cancel jump hold with ↓ ──────────────────────────────────
    private void HandleFastFall()
    {
        if (IsGrounded || isDashingInternal || isFloating) return;

        bool downHeld = Input.GetKey(KeyCode.DownArrow)     || Input.GetKey(KeyCode.S);
        bool downDown = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);

        // ↓ while rising → cancels jump hold and stops upward velocity
        if (downDown && rb.velocity.y > 0f)
        {
            _jumpHolding = false;
            rb.velocity  = new Vector2(rb.velocity.x, 0f);
        }

        // Extra downward acceleration while falling with ↓ held
        if (rb.velocity.y < 0f && downHeld)
            rb.velocity += Vector2.down * fastFallAccel * Time.deltaTime;
    }

    // ── Drop-through: ↓ while grounded on a OneWayPlatform ───────────────────
    // Delegates the drop to the OneWayPlatform component (custom system, no PlatformEffector2D).
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

    // ── Dash ──────────────────────────────────────────────────────────────────
    private void HandleDash()
    {
        if (!hasDash) return;
        if (IsGrounded) _airDashUsed = false;   // landed → air dash is available again
        if (!Input.GetKeyDown(GetKey("Dash", KeyCode.C))) return;
        if (dashCooldownTimer > 0f || isDashingInternal) return;
        if (!IsGrounded && _airDashUsed) return;   // air dash already consumed

        isDashingInternal = true;
        dashTimer         = dashDuration;
        dashCooldownTimer = dashCooldown;
        isFloating        = false;
        if (!IsGrounded) _airDashUsed = true;
        rb.velocity       = new Vector2(FacingDir * dashForce, 0f);
        rb.gravityScale   = 0f;
    }

    private void EndDash()
    {
        isDashingInternal = false;
        rb.gravityScale   = 1f;
    }

    // ── Wall slide ────────────────────────────────────────────────────────────
    private void HandleWallSlide()
    {
        if (!hasWallClimb || IsGrounded) { IsWallSliding = false; return; }

        bool onWall = (isOnWallR && FacingDir > 0f) || (isOnWallL && FacingDir < 0f);
        IsWallSliding = onWall && rb.velocity.y < 0f;

        if (IsWallSliding)
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
    }

    // ── Float ─────────────────────────────────────────────────────────────────
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

    // ── Enhanced gravity ──────────────────────────────────────────────────────
    private void ApplyBetterGravity()
    {
        // When grounded (including ramps), skip extra gravity to avoid fighting the slope force.
        if (isDashingInternal || isFloating || IsGrounded) return;

        if (rb.velocity.y < 0f)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
        else if (rb.velocity.y > 0f && !Input.GetKey(GetKey("Jump", KeyCode.Z)))
            // lowJumpMult does not apply while Z is held (HandleJumpHold handles that case)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMult - 1f) * Time.deltaTime;
    }

    // ── Animator state flags ──────────────────────────────────────────────────
    private void UpdateStateFlags()
    {
        IsJumping = !IsGrounded && rb.velocity.y > 0.1f;
        IsFalling = !IsGrounded && rb.velocity.y < -0.1f && !IsWallSliding;
    }

    // ── Footstep sound — called by Animation Events in Walk.anim and Run.anim ─
    // Add events in Unity: Animation window → foot-impact frame → function "OnFootstep".
    public void OnFootstep()
    {
        if (!IsGrounded || isDashingInternal) return;
        if (_combat != null && _combat.IsAttacking) return;
        AudioClip clip = _runningMode ? (runClip != null ? runClip : walkClip) : walkClip;
        if (clip != null) _sfxSource.PlayOneShot(clip);
    }

    // ── Attack and hurt sounds (called from PlayerCombat / CrystalRespawnManager) ──
    public void PlayAttackSound() { if (attackClip != null) _sfxSource.PlayOneShot(attackClip); OnPlayerAttacked?.Invoke(); }
    public void PlayHurtSound()   { if (hurtClip   != null) _sfxSource.PlayOneShot(hurtClip);   }

    // ── Running mode persistence across scenes ────────────────────────────────
    public bool GetRunningMode() => _runningMode;
    public void SetRunningMode(bool v) { _runningMode = v; }

    // ── Rebindable key helpers ────────────────────────────────────────────────
    private static KeyCode GetKey(string id, KeyCode def) => KeyRebindUI.GetKey(id, def);

    public KeyCode GetAttackKey()   => GetKey("Attack",   KeyCode.J);
    public KeyCode GetInteractKey() => GetKey("Interact", KeyCode.E);
    public KeyCode GetTeleportKey() => GetKey("Teleport", KeyCode.V);
    public KeyCode GetMapKey()      => GetKey("MapOpen",  KeyCode.M);
}
