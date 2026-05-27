using System.Collections;
using UnityEngine;

// Pattern: State Machine + Strategy — Boss of the Obsession.
//
// States: Dormant → Waking → Phase1 → Phase2(buff) → Phase3 → Dead
//
// Strategy by distance:
//   Close  (≤ meleeRange)  : melee / super
//   Far    (> meleeRange)  : range / boomerang / spincharge
//   Melee/super chosen from far → boss approaches first (ApproachPlayer)
//
// Obsession mechanic: _repeatCount → repeats the same attack N times before switching.
// Arena: clamped to [arenaMinX, arenaMaxX] in LateUpdate.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BossObsesionAnimator))]
public class BossObsesion : MonoBehaviour, IDamageable
{
    public enum BossPhase { Dormant, Waking, Phase1, Phase2, Phase3, Defeated, Dead }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Stats")]
    [SerializeField] private int   maxHP       = 20;
    [SerializeField] private float moveSpeed   = 4f;
    [SerializeField] private float detectRange = 25f;

    [Header("Arena MTN10 — room bounds")]
    [SerializeField] private float arenaMinX  = -62f;
    [SerializeField] private float arenaMaxX  =  62f;

    [Header("Combat ranges")]
    [SerializeField] private float meleeRange = 5f;

    [Header("Attack hitboxes")]
    [SerializeField] private BossAttackHitbox meleeHitbox;
    [SerializeField] private BossAttackHitbox spinHitbox;

    [Header("Projectiles")]
    [SerializeField] private Transform  rangeSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject boomerangPrefab;
    [SerializeField] private Sprite     projectileSprite;
    [SerializeField] private Sprite[]   boomerangFrames;  // sub-sprites from boomarang arms.png (drag all in order)

    [Header("Visual (independent from hitbox)")]
    [SerializeField] private float spriteYOffset = 0f;
    [SerializeField] private float spriteXOffset = 0f;

    [Header("Boss audio")]
    [SerializeField] private AudioClip bossMusic;

    [Header("Wake — dramatic pause before animating")]
    [SerializeField] private float wakeStillDuration = 1.5f;

    [Header("Range / Super — hit position offset (when rangeSpawnPoint is null)")]
    [SerializeField] private float rangeSpawnOffsetX =  5f;  // units in front of the boss
    [SerializeField] private float rangeSpawnOffsetY =  0f;
    [SerializeField] private Vector2 superHitboxSize = new Vector2(5f, 6f);

    // ── State ─────────────────────────────────────────────────────────────────

    private int                  _hp;
    private BossPhase            _phase = BossPhase.Dormant;
    private BossObsesionAnimator _anim;
    private Rigidbody2D          _rb;
    private SpriteRenderer       _sr;
    private Transform            _player;
    private bool                 _flashing;
    private SpriteRenderer       _visualSR;   // "Visual" child — sprite positioned independently of the hitbox

    private string _lastAttack  = "";
    private int    _repeatCount = 0;

    // ── Events (BossHealthBar subscribes here) ────────────────────────────────

    public static event System.Action<int, int>  OnHealthChanged;
    public static event System.Action<BossPhase> OnPhaseChanged;
    public static event System.Action            OnBossDefeated;  // boss HP=0, before extraction
    public static event System.Action            OnBossDead;      // extraction complete

    // Blocks SceneBoundary during the fight — prevents the player escaping without disabling triggers
    public static bool IsArenaActive = false;

    public BossPhase Phase   => _phase;
    public float     HPRatio => (float)_hp / maxHP;

    // ── Init ──────────────────────────────────────────────────────────────────

    void Awake()
    {
        _anim = GetComponent<BossObsesionAnimator>();
        _rb   = GetComponent<Rigidbody2D>();
        _sr   = GetComponent<SpriteRenderer>();

        _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        _rb.gravityScale = 1f;

        // Create a "Visual" child with its own Y-offset SpriteRenderer.
        // The Animator continues updating _sr (root, visually disabled).
        // _visualSR copies sprite/flipX/color every frame → position is independent of the hitbox.
        _sr.enabled = false;
        var visualGO = new GameObject("Visual");
        // false = keep LOCAL transforms; using true would make Unity adjust localScale to
        // (1/bossScale) to preserve world scale, causing the sprite to appear microscopic.
        visualGO.transform.SetParent(transform, false);
        visualGO.transform.localPosition = new Vector3(spriteXOffset, spriteYOffset, 0f);
        _visualSR               = visualGO.AddComponent<SpriteRenderer>();
        _visualSR.sortingLayerName = _sr.sortingLayerName;
        _visualSR.sortingOrder     = _sr.sortingOrder;
        _visualSR.material         = _sr.material;
    }

    void Start()
    {
        // If the boss was already defeated in this save, it never respawns
        if (SaveManager.ActiveSlot >= 0 && SaveManager.Instance != null)
        {
            var saved = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            if (saved != null && saved.bossDefeated)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        _hp    = maxHP;
        _phase = BossPhase.Dormant;
        _anim.PlaySleep();
        // OnHealthChanged is not invoked here: the bar only appears when Phase1 starts

        // Pre-load boomerang frames in editor Play Mode if they were not assigned in the
        // Inspector (avoids the orange fallback box without needing the editor script).
        // In builds the frames must be serialised by WireBossBoomerangFrames or BuildPreProcess.
#if UNITY_EDITOR
        if (boomerangFrames == null || boomerangFrames.Length == 0 ||
            System.Array.TrueForAll(boomerangFrames, s => s == null))
        {
            const string BOOM_PATH = "Assets/Sprites/Boss1Obsesion/Sprite Sheets/boomarang arms.png";
            var all  = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(BOOM_PATH);
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (var a in all) if (a is Sprite s) list.Add(s);
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            if (list.Count > 0) boomerangFrames = list.ToArray();
        }
#endif

        var pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            _player = pc.transform;
            // The boss does NOT act as a platform — ignore physics collision between bodies
            var playerCol = pc.GetComponent<BoxCollider2D>();
            var bossBody  = GetComponent<BoxCollider2D>();
            if (playerCol != null && bossBody != null)
                Physics2D.IgnoreCollision(bossBody, playerCol, true);
        }
    }

    // ── Main loop ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (_phase == BossPhase.Dormant)
        {
            TryWake();
            return;
        }
        if (_phase == BossPhase.Waking || _phase == BossPhase.Dead || _phase == BossPhase.Defeated) return;

        FacePlayer();
    }

    void LateUpdate()
    {
        // Visual proxy: copy sprite/flip/color to the "Visual" child every frame.
        // The Animator updates _sr even when disabled; propagate to the visible renderer here.
        if (_visualSR != null)
        {
            _visualSR.sprite = _sr.sprite;
            _visualSR.flipX  = _sr.flipX;
            _visualSR.color  = _sr.color;
        }

        // Arena clamp — only during active combat phases
        if (_phase == BossPhase.Dead || _phase == BossPhase.Defeated) return;

        float bossX = _rb.position.x;
        if (bossX < arenaMinX)
        {
            _rb.position = new Vector2(arenaMinX, _rb.position.y);
            if (_rb.velocity.x < 0f) _rb.velocity = new Vector2(0f, _rb.velocity.y);
        }
        else if (bossX > arenaMaxX)
        {
            _rb.position = new Vector2(arenaMaxX, _rb.position.y);
            if (_rb.velocity.x > 0f) _rb.velocity = new Vector2(0f, _rb.velocity.y);
        }
    }

    // ── Wake-up ───────────────────────────────────────────────────────────────

    private void TryWake()
    {
        if (_player == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) _player = pc.transform;
        }
        if (_player == null) return;

        float dist = Mathf.Abs(_player.position.x - transform.position.x);
        if (dist <= detectRange)
        {
            _phase = BossPhase.Waking;  // prevents Update from re-entering TryWake
            StartCoroutine(WakeUpSequence());
        }
    }

    private IEnumerator WakeUpSequence()
    {
        // Enable arena physics barriers and block scene transitions
        foreach (var ab in FindObjectsOfType<ArenaBarrier>(true))
            ab.gameObject.SetActive(true);
        IsArenaActive = true;

        // Boss music starts immediately
        if (bossMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(bossMusic);

        // Redirect camera to boss so the player sees it wake up
        var camFollow      = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        Transform origTarget = camFollow != null ? camFollow.target : null;
        if (camFollow != null)
        {
            camFollow.target = transform;
            // Immediate snap — camera focuses on the boss without waiting for SmoothDamp
            if (Camera.main != null)
                Camera.main.transform.position = new Vector3(
                    transform.position.x, transform.position.y, Camera.main.transform.position.z);
        }

        // Shake while camera travels to boss and boss stays still
        StartCoroutine(ShakeCamera(wakeStillDuration, 0.12f));
        yield return new WaitForSeconds(wakeStillDuration);

        _anim.PlayWake();
        yield return new WaitForSeconds(_anim.WakeDuration);

        // Restore camera to player (CameraFollow.SmoothDamp handles the smooth transition)
        if (camFollow != null) camFollow.target = origTarget;
        yield return new WaitForSeconds(0.35f);  // brief wait for camera to reach the player

        _phase = BossPhase.Phase1;
        OnPhaseChanged?.Invoke(_phase);
        OnHealthChanged?.Invoke(_hp, maxHP);
        StartCoroutine(AttackLoop());
    }

    // ── Attack loop ───────────────────────────────────────────────────────────

    private IEnumerator AttackLoop()
    {
        while (_phase != BossPhase.Dead)
        {
            if (_player == null) { yield return null; continue; }

            // Idle pause between attacks
            _anim.PlayIdle();
            yield return new WaitForSeconds(GetIdleDelay());
            if (_phase == BossPhase.Dead) break;

            FacePlayer();

            // Choose attack based on current distance
            string attack = ChooseAttack();

            // Melee attacks: approach first if the player is far away
            if ((attack == "melee" || attack == "super") && DistToPlayer() > meleeRange)
                yield return StartCoroutine(ApproachPlayer());

            if (_phase == BossPhase.Dead) break;

            FacePlayer();
            yield return StartCoroutine(ExecuteAttack(attack));
        }
    }

    // Moves the boss toward the player until within melee range (5s timeout)
    private IEnumerator ApproachPlayer()
    {
        _anim.PlayMove();
        float elapsed = 0f;

        while (elapsed < 5f && _phase != BossPhase.Dead)
        {
            if (_player == null) break;
            if (DistToPlayer() <= meleeRange * 0.85f) break;

            _rb.velocity = new Vector2(DirToPlayer() * GetMoveSpeed(), _rb.velocity.y);
            FacePlayer();
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rb.velocity = new Vector2(0f, _rb.velocity.y);
    }

    // ── Strategy: attack selection ────────────────────────────────────────────

    private string ChooseAttack()
    {
        // Obsession mechanic: repeat the same attack up to maxRepeat times
        int maxRepeat = _phase switch
        {
            BossPhase.Phase1 => 2,
            BossPhase.Phase2 => 3,
            _                => 4,  // Phase3: obsesión total
        };

        if (!string.IsNullOrEmpty(_lastAttack) && _repeatCount < maxRepeat && Random.value < 0.55f)
        {
            _repeatCount++;
            return _lastAttack;
        }

        // Pool based on current distance and phase
        // "armsweep" = floor-skimming arms the player must jump (works at any distance)
        string[] pool;
        if (DistToPlayer() <= meleeRange)
        {
            pool = _phase switch
            {
                BossPhase.Phase1 => new[] { "melee", "melee", "range" },
                BossPhase.Phase2 => new[] { "melee", "melee", "range", "super" },
                _                => new[] { "melee", "melee", "melee", "range", "super" },
            };
        }
        else
        {
            pool = _phase switch
            {
                BossPhase.Phase1 => new[] { "range", "boomerang", "melee" },
                BossPhase.Phase2 => new[] { "range", "boomerang", "boomerang", "melee" },
                _                => new[] { "range", "boomerang", "boomerang", "range", "super", "melee" },
            };
        }

        string chosen = pool[Random.Range(0, pool.Length)];
        if (chosen != _lastAttack) { _lastAttack = chosen; _repeatCount = 1; }
        return chosen;
    }

    private IEnumerator ExecuteAttack(string attack)
    {
        switch (attack)
        {
            case "melee":      yield return StartCoroutine(DoMelee());      break;
            case "range":      yield return StartCoroutine(DoRange());      break;
            case "boomerang":  yield return StartCoroutine(DoBoomerang());  break;
            case "spincharge": yield return StartCoroutine(DoSpinCharge()); break;
            case "super":      yield return StartCoroutine(DoSuper());      break;
            case "armsweep":   yield return StartCoroutine(DoArmSweep());   break;
            default:           yield return StartCoroutine(DoMelee());      break;
        }
    }

    // ── Individual attacks ────────────────────────────────────────────────────

    private IEnumerator DoMelee()
    {
        _anim.PlayMelee();
        yield return new WaitForSeconds(0.9f);   // windup: frames 0-10 (frame 11 @ 12 fps)

        if (meleeHitbox != null) meleeHitbox.Activate(1);
        yield return new WaitForSeconds(0.45f);  // active: frames 11-16
        if (meleeHitbox != null) meleeHitbox.Deactivate();

        float rest = _anim.MeleeDuration - 1.35f;
        if (rest > 0f) yield return new WaitForSeconds(rest);
    }

    private IEnumerator DoRange()
    {
        _anim.PlayRange();
        yield return new WaitForSeconds(1.1f);   // windup: frame 11 @ 10 fps

        // Hitbox active only frames 11-12 = 2 frames @ 10 fps = 0.2s
        SpawnProjectile(lifetimeOverride: 0.2f);

        float rest = _anim.RangeDuration - 1.1f;
        if (rest > 0f) yield return new WaitForSeconds(rest);
    }

    // Boomerang flow: SpinCharge → launch arms → boss Idle/Move while arms are out → SpinEnd
    private IEnumerator DoBoomerang()
    {
        _anim.PlaySpinCharge();
        yield return new WaitForSeconds(_anim.SpinChargeDuration);

        bool returned = false;
        SpawnBoomerang(() => returned = true);

        // While arms are out: boss Idle/Move only, does NOT attack
        while (!returned && _phase != BossPhase.Dead)
        {
            FacePlayer();

            float dist = DistToPlayer();
            if (dist > meleeRange + 1f)
            {
                // Walk toward the player
                _rb.velocity = new Vector2(DirToPlayer() * GetMoveSpeed(), _rb.velocity.y);
                _anim.PlayMove();
            }
            else
            {
                // Already close: stay still in Idle
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
                _anim.PlayIdle();
            }

            yield return null;
        }

        _rb.velocity = new Vector2(0f, _rb.velocity.y);
        _anim.PlaySpinEnd();
        yield return new WaitForSeconds(_anim.SpinEndDuration);
    }

    // Dash toward the player with spin hitbox active
    private IEnumerator DoSpinCharge()
    {
        _anim.PlaySpinCharge();
        yield return new WaitForSeconds(0.3f);  // mini windup visual

        if (spinHitbox != null) spinHitbox.Activate(1);

        float dir     = DirToPlayer();
        float elapsed = 0f;

        while (elapsed < 0.45f && _phase != BossPhase.Dead)
        {
            _rb.velocity = new Vector2(dir * GetMoveSpeed() * 3.5f, _rb.velocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rb.velocity = new Vector2(0f, _rb.velocity.y);
        if (spinHitbox != null) spinHitbox.Deactivate();

        _anim.PlaySpinEnd();
        yield return new WaitForSeconds(_anim.SpinEndDuration);
    }

    private IEnumerator DoSuper()
    {
        yield return StartCoroutine(ObsessiveTwitch());  // temblor de carga

        _anim.PlaySuper();
        yield return new WaitForSeconds(1.0f);   // windup: frames 0-9

        // Super uses the same position as the range attack (in front of the boss)
        DamageAtRangePosition(2);

        yield return new WaitForSeconds(0.45f);

        float rest = _anim.SuperDuration - 1.45f;
        if (rest > 0f) yield return new WaitForSeconds(rest);
    }

    // Floor-skimming arm sweep — arms fly out in both directions at ground level.
    // The player must jump ~1.5u to dodge.
    // Same flow as DoBoomerang: SpinCharge → SpinEnd → spawn → Boomerang body-only
    private IEnumerator DoArmSweep()
    {
        yield return StartCoroutine(ObsessiveTwitch());   // charge visual

        _anim.PlaySpinCharge();
        yield return new WaitForSeconds(_anim.SpinChargeDuration);

        _anim.PlaySpinEnd();                            // transition before launch
        yield return new WaitForSeconds(_anim.SpinEndDuration);

        // Both arms launch simultaneously in opposite directions
        SpawnArmSweep(-1f, arenaMinX);
        SpawnArmSweep( 1f, arenaMaxX);

        _anim.PlayBoomerang();                          // body-only while arms sweep the room
        yield return new WaitForSeconds(_anim.BoomerangDuration);
    }

    // sweepY = -11.5f: standing Kael occupies y=[-12,-10]; jumping ≥1.5u clears the arm.
    private void SpawnArmSweep(float direction, float wallX)
    {
        var go = new GameObject("BossArmSweep");
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoxCollider2D>();
        var sweep = go.AddComponent<BossArmSweep>();

        const float sweepY = -11.5f;
        go.transform.position = new Vector3(transform.position.x, sweepY, 0f);

        sweep.Init(direction, wallX, projectileSprite);
    }

    // ── IDamageable ───────────────────────────────────────────────────────────


    public void TakeDamage(int amount)
    {
        if (_phase == BossPhase.Dead || _phase == BossPhase.Defeated ||
            _phase == BossPhase.Dormant || _phase == BossPhase.Waking)
            return;

        _hp = Mathf.Max(0, _hp - amount);
        OnHealthChanged?.Invoke(_hp, maxHP);

        if (!_flashing)
            StartCoroutine(DamageFlash());

        CheckPhaseTransition();

        if (_hp <= 0)
        {
            StopAllCoroutines();
            StartCoroutine(DefeatedSequence());
        }
    }

    // ── Phase transitions ─────────────────────────────────────────────────────

    private void CheckPhaseTransition()
    {
        if (_phase == BossPhase.Phase1 && HPRatio <= 0.5f)
        {
            _phase = BossPhase.Phase2;
            OnPhaseChanged?.Invoke(_phase);
            StopAllCoroutines();
            StartCoroutine(BuffTransition());
        }
        else if (_phase == BossPhase.Phase2 && HPRatio <= 0.25f)
        {
            _phase = BossPhase.Phase3;
            OnPhaseChanged?.Invoke(_phase);
            moveSpeed *= 1.5f;   // frantic speed in phase 3
        }
    }

    private IEnumerator BuffTransition()
    {
        _rb.velocity = Vector2.zero;
        _anim.PlayBuff();

        Vector3 origin  = transform.position;
        float   elapsed = 0f;

        while (elapsed < _anim.BuffDuration)
        {
            float intensity = elapsed / _anim.BuffDuration;
            transform.position = origin + new Vector3(
                Random.Range(-0.15f, 0.15f) * intensity,
                Random.Range(-0.08f, 0.08f) * intensity, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = origin;

        moveSpeed *= 1.25f;   // speed increases with the buff
        StartCoroutine(AttackLoop());
    }

    // ── Defeat sequence: static sleep → player holds E → flash + death + dash ──────────────────

    private IEnumerator DefeatedSequence()
    {
        _phase          = BossPhase.Defeated;
        _rb.velocity    = Vector2.zero;
        _rb.isKinematic = true;

        OnBossDefeated?.Invoke();   // health bar disappears immediately

        // Camera focuses on the boss as it falls
        var camFollow       = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        Transform origCamTarget = camFollow != null ? camFollow.target : null;
        if (camFollow != null) camFollow.target = transform;

        // Music fades out smoothly
        AudioManager.Instance?.FadeOutMusic(2f);

        // Boss stays in Idle — death animation fires when extraction is complete
        _anim.PlayIdle();

        // On-screen prompt
        var prompt = BuildExtractionPrompt();

        // ── Wait for the player to hold E with progressive shake ──────────────
        const float HOLD_REQUIRED = 2f;
        const float INTERACT_DIST = 6f;
        float holdTimer = 0f;

        var cam       = Camera.main;
        var camOrigin = cam != null ? cam.transform.localPosition : Vector3.zero;

        while (holdTimer < HOLD_REQUIRED)
        {
            bool inRange = _player != null && DistToPlayer() <= INTERACT_DIST;
            if (prompt != null) prompt.SetActive(inRange);

            if (inRange && Input.GetKey(KeyCode.E))
                holdTimer += Time.deltaTime;
            else
                holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 2f);

            // Progressive shake: intensity and frequency increase with progress
            if (cam != null && holdTimer > 0.1f)
            {
                float progress  = holdTimer / HOLD_REQUIRED;
                float intensity = progress * 0.35f;
                float hz        = 8f + progress * 20f;  // 8–28 Hz

                if (Time.frameCount % Mathf.Max(1, Mathf.RoundToInt(60f / hz)) == 0)
                {
                    cam.transform.localPosition = camOrigin + new Vector3(
                        Random.Range(-intensity, intensity),
                        Random.Range(-intensity, intensity), 0f);
                }
            }
            else if (cam != null)
            {
                cam.transform.localPosition = camOrigin;
            }

            yield return null;
        }

        if (cam != null) cam.transform.localPosition = camOrigin;
        if (prompt != null) UnityEngine.Object.Destroy(prompt);

        // ── On extraction complete: boss and Kael die simultaneously ─────────
        StartCoroutine(ShakeCamera(0.5f, 0.28f));
        _anim.PlayDeath();   // boss death animation

        PlayerController pc  = _player != null ? _player.GetComponent<PlayerController>() : null;
        var kaelAnim         = _player != null ? _player.GetComponent<PlayerAnimator>()   : null;
        var kaelRb           = _player != null ? _player.GetComponent<Rigidbody2D>()      : null;

        if (pc != null)     pc.enabled = false;
        if (kaelRb != null) { kaelRb.velocity = Vector2.zero; kaelRb.isKinematic = true; }
        kaelAnim?.TriggerDie();   // Kael collapses while absorbing the fragment

        yield return new WaitForSeconds(0.8f);   // let animations start

        // Slow fade out — screen darkens
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FastFadeOutAsync(1.5f);

        // ── In black: grant dash and reset Kael to Idle ───────────────────────
        if (pc != null) { pc.hasDash = true; pc.enabled = true; }
        if (kaelRb != null) { kaelRb.isKinematic = false; kaelRb.velocity = Vector2.zero; }

        // Restore full lives after defeating the boss
        CrystalRespawnManager.Instance?.RestoreLives();

        // Return to Idle so Kael doesn't appear stuck on the last death frame
        kaelAnim?.ResetToIdle();

        // Restore camera to player
        if (camFollow != null) camFollow.target = origCamTarget;

        // Unlock arena exits
        foreach (var ab in FindObjectsOfType<ArenaBarrier>(true))
            ab.gameObject.SetActive(false);
        IsArenaActive = false;

        // Dash tutorial starts before fade-in
        if (SceneFader.Instance != null) SceneFader.Instance.StartDashTutorial();

        // Slow fade in — Kael appears in Idle, tutorial visible
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FastFadeInAsync(1.5f);

        // Persist boss defeat — it won't respawn on returning to MTN10
        if (SaveManager.ActiveSlot >= 0 && SaveManager.Instance != null)
        {
            var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            if (data != null)
            {
                data.bossDefeated = true;
                data.hasDash      = true;
                SaveManager.Instance.Save(SaveManager.ActiveSlot, data);
            }
        }

        _phase = BossPhase.Dead;
        OnBossDead?.Invoke();
        gameObject.SetActive(false);
    }

    private GameObject BuildExtractionPrompt()
    {
        var canvasGO = new GameObject("ExtractionPrompt");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Semi-transparent black box at the bottom
        var boxGO  = new GameObject("PromptBox");
        boxGO.transform.SetParent(canvasGO.transform, false);
        var boxImg = boxGO.AddComponent<UnityEngine.UI.Image>();
        boxImg.color = new Color(0f, 0f, 0f, 0.78f);
        var boxRt = boxGO.GetComponent<UnityEngine.UI.Image>().rectTransform;
        boxRt.anchorMin        = new Vector2(0.5f, 0f);
        boxRt.anchorMax        = new Vector2(0.5f, 0f);
        boxRt.pivot            = new Vector2(0.5f, 0f);
        boxRt.anchoredPosition = new Vector2(0f, 60f);
        boxRt.sizeDelta        = new Vector2(780f, 110f);

        // Text above the box
        var textGO = new GameObject("PromptText");
        textGO.transform.SetParent(boxGO.transform, false);
        var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = "[ E ] Mantener para extraer fragmento";
        tmp.fontSize  = 42;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color     = Color.white;

#if UNITY_EDITOR
        var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
            "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
        var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
        if (font != null) tmp.font = font;

        var textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16f, 8f);
        textRt.offsetMax = new Vector2(-16f, -8f);

        return canvasGO;
    }

    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        var cam = Camera.main;
        if (cam == null) yield break;

        var origin  = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cam.transform.localPosition = new Vector3(origin.x + x, origin.y + y, origin.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.localPosition = origin;
    }

    private static Sprite LoadStaticSleepSprite()
    {
#if UNITY_EDITOR
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
            "Assets/Sprites/Boss1Obsesion/Sprite Sheets/static sleep .png");
        foreach (var a in assets)
            if (a is Sprite s) return s;
#endif
        return null;
    }

    private IEnumerator DieDeath()
    {
        _phase           = BossPhase.Dead;
        _rb.velocity     = Vector2.zero;
        _rb.isKinematic  = true;

        _anim.PlayDeath();
        yield return new WaitForSeconds(_anim.DeathDuration);

        OnBossDead?.Invoke();
        gameObject.SetActive(false);
    }

    // ── Movement helpers ──────────────────────────────────────────────────────

    private void FacePlayer()
    {
        if (_player == null) return;
        float   dir = DirToPlayer();
        Vector3 s   = transform.localScale;

        // Horizontal flip: negative scale.x = facing left
        // Children (hitboxes) move automatically with the scale flip
        if (dir > 0f && s.x < 0f) transform.localScale = new Vector3(-s.x,  s.y, s.z);
        if (dir < 0f && s.x > 0f) transform.localScale = new Vector3(-s.x,  s.y, s.z);
    }

    private float DistToPlayer() =>
        _player != null ? Mathf.Abs(_player.position.x - transform.position.x) : 999f;

    private float DirToPlayer() =>
        _player != null ? Mathf.Sign(_player.position.x - transform.position.x) : 1f;

    private float GetMoveSpeed() => _phase switch
    {
        BossPhase.Phase2 => moveSpeed * 1.2f,
        BossPhase.Phase3 => moveSpeed * 1.5f,
        _                => moveSpeed,
    };

    private float GetIdleDelay() => _phase switch
    {
        BossPhase.Phase1 => Random.Range(1.2f, 2.0f),
        BossPhase.Phase2 => Random.Range(0.6f, 1.2f),
        _                => Random.Range(0.1f, 0.4f),   // Phase3: almost no pause
    };

    // ── Obsession effects ─────────────────────────────────────────────────────

    // Scaled shake before heavy attacks — visual charge signal
    private IEnumerator ObsessiveTwitch()
    {
        Vector3 origin   = transform.position;
        int     twitches = _phase == BossPhase.Phase3 ? 8 : 4;

        for (int i = 0; i < twitches; i++)
        {
            float intensity = (i + 1f) / twitches * 0.4f;
            transform.position = origin + new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity * 0.4f, intensity * 0.4f), 0f);
            yield return new WaitForSeconds(0.045f);
        }
        transform.position = origin;
    }

    private IEnumerator DamageFlash()
    {
        _flashing = true;
        // Very subtle tint on damage — barely noticeable, doesn't distract from the sprite
        _sr.color = _phase switch
        {
            BossPhase.Phase2 => new Color(1f, 0.82f, 0.82f),   // very soft pink
            BossPhase.Phase3 => new Color(1f, 0.78f, 0.78f),   // slightly stronger pink
            _                => new Color(1f, 0.80f, 0.80f),   // phase 1: equally subtle
        };
        yield return new WaitForSeconds(0.12f);
        _sr.color = Color.white;
        _flashing = false;
    }

    // ── Projectiles / ranged hitboxes ─────────────────────────────────────────

    // Ranged hit position — in front of the boss in world space (independent of scale).
    private Vector3 GetRangeSpawnPosition() =>
        rangeSpawnPoint != null
            ? rangeSpawnPoint.position
            : transform.position + new Vector3(DirToPlayer() * rangeSpawnOffsetX, rangeSpawnOffsetY, 0f);

    private void SpawnProjectile(float lifetimeOverride = -1f)
    {
        if (projectilePrefab == null) return;
        var go   = Instantiate(projectilePrefab, GetRangeSpawnPosition(), Quaternion.identity);
        var proj = go.GetComponent<BossProjectile>();
        if (proj != null) proj.Init(DirToPlayer(), projectileSprite, lifetimeOverride);
    }

    // Instant damage at the range attack position — used by DoSuper.
    private void DamageAtRangePosition(int dmg)
    {
        if (CrystalRespawnManager.Instance == null) return;
        Vector2 center = GetRangeSpawnPosition();
        var hits = Physics2D.OverlapBoxAll(center, superHitboxSize, 0f);
        foreach (var hit in hits)
        {
            bool isPlayer = hit.CompareTag("Player") || hit.GetComponent<PlayerController>() != null;
            if (!isPlayer) continue;
            CrystalRespawnManager.Instance.TakeBossDamage(dmg);
            break;
        }
    }

    private void SpawnBoomerang(System.Action onReturn = null)
    {
        // +0.4u above the boss's feet → boomerang skims close to the floor.
        Vector3 pos = new Vector3(transform.position.x, transform.position.y + 0.4f, 0f);

        GameObject go;
        if (boomerangPrefab != null)
        {
            go = Instantiate(boomerangPrefab, pos, Quaternion.identity);
        }
        else
        {
            go = new GameObject("BossBoomerang");
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<BossBoomerang>();
            go.transform.position = pos;
        }

        var boom = go.GetComponent<BossBoomerang>();
        Sprite[] frames = (boomerangFrames != null && boomerangFrames.Length > 0) ? boomerangFrames : null;
        if (boom != null) boom.Init(DirToPlayer(), frames, onReturn);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Melee range
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Arena bounds
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawLine(new Vector3(arenaMinX, -30f, 0), new Vector3(arenaMinX, 30f, 0));
        Gizmos.DrawLine(new Vector3(arenaMaxX, -30f, 0), new Vector3(arenaMaxX, 30f, 0));
    }
}
