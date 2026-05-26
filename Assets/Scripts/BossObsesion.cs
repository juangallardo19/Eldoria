using System.Collections;
using UnityEngine;

// State Machine + Strategy — Boss de la Obsesión.
//
// Estados: Dormant → Waking → Phase1 → Phase2(buff) → Phase3 → Dead
//
// Strategy de ataques por distancia:
//   Cerca  (≤ meleeRange)  : melee / super
//   Lejos  (> meleeRange)  : range / boomerang / spincharge
//   Melee/super elegido de lejos → boss se acerca primero (ApproachPlayer)
//
// Mecánica de Obsesión: _repeatCount → repite el mismo ataque N veces antes de cambiar.
// Arena: clamp a [arenaMinX, arenaMaxX] en LateUpdate.
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

    [Header("Arena MTN10 — límites de sala")]
    [SerializeField] private float arenaMinX  = -62f;
    [SerializeField] private float arenaMaxX  =  62f;

    [Header("Distancias de combate")]
    [SerializeField] private float meleeRange = 5f;

    [Header("Hitboxes de ataque")]
    [SerializeField] private BossAttackHitbox meleeHitbox;
    [SerializeField] private BossAttackHitbox spinHitbox;

    [Header("Proyectiles")]
    [SerializeField] private Transform  rangeSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject boomerangPrefab;
    [SerializeField] private Sprite     projectileSprite;
    [SerializeField] private Sprite[]   boomerangFrames;  // sub-sprites de boomarang arms.png (arrastra todos en orden)

    [Header("Visual (independiente de hitbox)")]
    [SerializeField] private float spriteYOffset = 0f;
    [SerializeField] private float spriteXOffset = 0f;

    [Header("Audio del Boss")]
    [SerializeField] private AudioClip bossMusic;

    [Header("Despertar — pausa dramática antes de animar")]
    [SerializeField] private float wakeStillDuration = 1.5f;

    [Header("Range / Super — posición relativa del impacto (si rangeSpawnPoint es null)")]
    [SerializeField] private float rangeSpawnOffsetX =  5f;  // unidades delante del boss
    [SerializeField] private float rangeSpawnOffsetY =  0f;
    [SerializeField] private Vector2 superHitboxSize = new Vector2(5f, 6f);

    // ── Estado ────────────────────────────────────────────────────────────────

    private int                  _hp;
    private BossPhase            _phase = BossPhase.Dormant;
    private BossObsesionAnimator _anim;
    private Rigidbody2D          _rb;
    private SpriteRenderer       _sr;
    private Transform            _player;
    private bool                 _flashing;
    private SpriteRenderer       _visualSR;   // hijo "Visual" — sprite posicionado independiente del hitbox

    private string _lastAttack  = "";
    private int    _repeatCount = 0;

    // ── Eventos (BossHealthBar se suscribe aquí) ──────────────────────────────

    public static event System.Action<int, int>  OnHealthChanged;
    public static event System.Action<BossPhase> OnPhaseChanged;
    public static event System.Action            OnBossDead;

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

        // Crear hijo "Visual" con su propio SpriteRenderer desplazado en Y.
        // El Animator sigue actualizando _sr (root, deshabilitado visualmente).
        // _visualSR copia sprite/flipX/color cada frame → posición independiente del hitbox.
        _sr.enabled = false;
        var visualGO = new GameObject("Visual");
        // false = mantener transforms LOCALES; si usáramos true Unity ajustaría localScale
        // a (1/bossScale) para conservar world scale, haciendo el sprite aparecer microscópico.
        visualGO.transform.SetParent(transform, false);
        visualGO.transform.localPosition = new Vector3(spriteXOffset, spriteYOffset, 0f);
        _visualSR               = visualGO.AddComponent<SpriteRenderer>();
        _visualSR.sortingLayerName = _sr.sortingLayerName;
        _visualSR.sortingOrder     = _sr.sortingOrder;
        _visualSR.material         = _sr.material;
    }

    void Start()
    {
        // Si el boss ya fue derrotado en esta partida, no vuelve a aparecer
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

        // Pre-cargar frames del boomerang en editor Play Mode si no fueron asignados
        // en el Inspector (evita el recuadro naranja de fallback sin necesitar el editor script).
        // En builds, los frames deben estar serializados por WireBossBoomerangFrames o BuildPreProcess.
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
            // El boss NO actúa como plataforma — ignorar colisión física entre cuerpos
            var playerCol = pc.GetComponent<BoxCollider2D>();
            var bossBody  = GetComponent<BoxCollider2D>();
            if (playerCol != null && bossBody != null)
                Physics2D.IgnoreCollision(bossBody, playerCol, true);
        }
    }

    // ── Loop principal ────────────────────────────────────────────────────────

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
        // Proxy visual: copiar sprite/flip/color al hijo "Visual" en todo momento.
        // El Animator actualiza _sr aunque esté disabled; aquí lo propagamos al renderer visible.
        if (_visualSR != null)
        {
            _visualSR.sprite = _sr.sprite;
            _visualSR.flipX  = _sr.flipX;
            _visualSR.color  = _sr.color;
        }

        // Clamp de arena — solo durante fases activas de combate
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

    // ── Despertar ─────────────────────────────────────────────────────────────

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
            _phase = BossPhase.Waking;  // evita que Update re-entre en TryWake
            StartCoroutine(WakeUpSequence());
        }
    }

    private IEnumerator WakeUpSequence()
    {
        // Habilitar barreras físicas de arena y bloquear triggers de salida
        foreach (var ab in FindObjectsOfType<ArenaBarrier>(true))
            ab.gameObject.SetActive(true);
        foreach (var sb in FindObjectsOfType<SceneBoundary>())
            if (sb.TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        // Música del boss arranca inmediatamente
        if (bossMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(bossMusic);

        // Redirigir cámara al boss para que el jugador lo vea despertarse
        var camFollow      = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        Transform origTarget = camFollow != null ? camFollow.target : null;
        if (camFollow != null) camFollow.target = transform;

        // Temblor mientras la cámara viaja al boss y el boss permanece quieto
        StartCoroutine(ShakeCamera(wakeStillDuration, 0.12f));
        yield return new WaitForSeconds(wakeStillDuration);

        _anim.PlayWake();
        yield return new WaitForSeconds(_anim.WakeDuration);

        // Restaurar cámara al jugador (CameraFollow.SmoothDamp hace la transición suave)
        if (camFollow != null) camFollow.target = origTarget;
        yield return new WaitForSeconds(0.35f);  // pausa para que la cámara llegue al jugador

        _phase = BossPhase.Phase1;
        OnPhaseChanged?.Invoke(_phase);
        OnHealthChanged?.Invoke(_hp, maxHP);
        StartCoroutine(AttackLoop());
    }

    // ── Loop de ataque ────────────────────────────────────────────────────────

    private IEnumerator AttackLoop()
    {
        while (_phase != BossPhase.Dead)
        {
            if (_player == null) { yield return null; continue; }

            // Pausa entre ataques con animación Idle
            _anim.PlayIdle();
            yield return new WaitForSeconds(GetIdleDelay());
            if (_phase == BossPhase.Dead) break;

            FacePlayer();

            // Elegir ataque según distancia actual
            string attack = ChooseAttack();

            // Ataques cuerpo a cuerpo: acercarse primero si el jugador está lejos
            if ((attack == "melee" || attack == "super") && DistToPlayer() > meleeRange)
                yield return StartCoroutine(ApproachPlayer());

            if (_phase == BossPhase.Dead) break;

            FacePlayer();
            yield return StartCoroutine(ExecuteAttack(attack));
        }
    }

    // Mueve al boss hacia el jugador hasta estar en rango melee (timeout 5s)
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

    // ── Strategy: elección de ataque ─────────────────────────────────────────

    private string ChooseAttack()
    {
        // Mecánica de Obsesión: repetir el mismo ataque hasta maxRepeat veces
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

        // Pool según distancia al jugador y fase actual
        // "armsweep" = brazos rasantes que el jugador debe saltar (funciona a cualquier distancia)
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

    // ── Ataques individuales ──────────────────────────────────────────────────

    private IEnumerator DoMelee()
    {
        _anim.PlayMelee();
        yield return new WaitForSeconds(0.9f);   // windup: frames 0-10 (frame 11 @ 12fps)

        if (meleeHitbox != null) meleeHitbox.Activate(1);
        yield return new WaitForSeconds(0.45f);  // activo: frames 11-16
        if (meleeHitbox != null) meleeHitbox.Deactivate();

        float rest = _anim.MeleeDuration - 1.35f;
        if (rest > 0f) yield return new WaitForSeconds(rest);
    }

    private IEnumerator DoRange()
    {
        _anim.PlayRange();
        yield return new WaitForSeconds(1.1f);   // windup: frame 11 @ 10fps

        // Hitbox activa solo frames 11-12 = 2 frames @ 10fps = 0.2s
        SpawnProjectile(lifetimeOverride: 0.2f);

        float rest = _anim.RangeDuration - 1.1f;
        if (rest > 0f) yield return new WaitForSeconds(rest);
    }

    // Flujo boomerang: SpinCharge → lanza brazos → boss Idle/Move hasta que vuelvan → SpinEnd
    private IEnumerator DoBoomerang()
    {
        _anim.PlaySpinCharge();
        yield return new WaitForSeconds(_anim.SpinChargeDuration);

        bool returned = false;
        SpawnBoomerang(() => returned = true);

        // Mientras los brazos están fuera: boss en Idle/Move, NO ataca
        while (!returned && _phase != BossPhase.Dead)
        {
            FacePlayer();

            float dist = DistToPlayer();
            if (dist > meleeRange + 1f)
            {
                // Se acerca al jugador caminando
                _rb.velocity = new Vector2(DirToPlayer() * GetMoveSpeed(), _rb.velocity.y);
                _anim.PlayMove();
            }
            else
            {
                // Ya está cerca: se queda quieto en Idle
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
                _anim.PlayIdle();
            }

            yield return null;
        }

        _rb.velocity = new Vector2(0f, _rb.velocity.y);
        _anim.PlaySpinEnd();
        yield return new WaitForSeconds(_anim.SpinEndDuration);
    }

    // Dash hacia el jugador con hitbox de spin activo
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

        // Super usa la misma posición que el range attack (delante del boss)
        DamageAtRangePosition(2);

        yield return new WaitForSeconds(0.45f);

        float rest = _anim.SuperDuration - 1.45f;
        if (rest > 0f) yield return new WaitForSeconds(rest);
    }

    // Barrido rasante de brazos — salen en ambas direcciones a ras del suelo.
    // El jugador debe saltar ~1.5u para esquivarlos.
    // Flujo igual que DoBoomerang: SpinCharge → SpinEnd → spawn → Boomerang body-solo
    private IEnumerator DoArmSweep()
    {
        yield return StartCoroutine(ObsessiveTwitch());   // carga visual

        _anim.PlaySpinCharge();
        yield return new WaitForSeconds(_anim.SpinChargeDuration);

        _anim.PlaySpinEnd();                            // transición antes del lanzamiento
        yield return new WaitForSeconds(_anim.SpinEndDuration);

        // Ambos brazos salen simultáneamente en direcciones opuestas
        SpawnArmSweep(-1f, arenaMinX);
        SpawnArmSweep( 1f, arenaMaxX);

        _anim.PlayBoomerang();                          // body-solo mientras brazos barren la sala
        yield return new WaitForSeconds(_anim.BoomerangDuration);
    }

    // sweepY = -11.5f: Kael parado ocupa y=[-12,-10]; saltar ≥1.5u despeja el brazo.
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

    // ── Transiciones de fase ──────────────────────────────────────────────────

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
            moveSpeed *= 1.5f;   // velocidad frenética en fase 3
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

        moveSpeed *= 1.25f;   // velocidad aumenta con el buff
        StartCoroutine(AttackLoop());
    }

    // ── Secuencia de derrota: static sleep → el jugador mantiene E → flash + death + dash ─────

    private IEnumerator DefeatedSequence()
    {
        _phase          = BossPhase.Defeated;
        _rb.velocity    = Vector2.zero;
        _rb.isKinematic = true;

        // Cámara enfoca al boss en cuanto cae
        var camFollow       = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        Transform origCamTarget = camFollow != null ? camFollow.target : null;
        if (camFollow != null) camFollow.target = transform;

        // Música desaparece con fade suave
        AudioManager.Instance?.FadeOutMusic(2f);

        // Boss permanece en Idle — la animación de muerte se dispara al completar la extracción
        _anim.PlayIdle();

        // Prompt en pantalla
        var prompt = BuildExtractionPrompt();

        // ── Esperar a que el jugador mantenga E con shake progresivo ─────────
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

            // Shake progresivo: intensidad y frecuencia aumentan con el progreso
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

        // ── Al completar la extracción: boss y Kael mueren simultáneamente ────
        StartCoroutine(ShakeCamera(0.5f, 0.28f));
        _anim.PlayDeath();   // animación de muerte del boss

        PlayerController pc  = _player != null ? _player.GetComponent<PlayerController>() : null;
        var kaelAnim         = _player != null ? _player.GetComponent<PlayerAnimator>()   : null;
        var kaelRb           = _player != null ? _player.GetComponent<Rigidbody2D>()      : null;

        if (pc != null)     pc.enabled = false;
        if (kaelRb != null) { kaelRb.velocity = Vector2.zero; kaelRb.isKinematic = true; }
        kaelAnim?.TriggerDie();   // Kael colapsa al absorber el fragmento

        yield return new WaitForSeconds(0.8f);   // deja que las animaciones arranquen

        // Fade out lento — pantalla se oscurece
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FastFadeOutAsync(1.5f);

        // ── En negro: otorgar dash y restablecer a Kael en Idle ──────────────
        if (pc != null) { pc.hasDash = true; pc.enabled = true; }
        if (kaelRb != null) { kaelRb.isKinematic = false; kaelRb.velocity = Vector2.zero; }

        // Volver a Idle para que Kael no aparezca atascado en el último frame de muerte
        kaelAnim?.ResetToIdle();

        // Restaurar cámara al jugador
        if (camFollow != null) camFollow.target = origCamTarget;

        // Desbloquear salidas de arena
        foreach (var ab in FindObjectsOfType<ArenaBarrier>(true))
            ab.gameObject.SetActive(false);
        foreach (var sb in FindObjectsOfType<SceneBoundary>())
            if (sb.TryGetComponent<Collider2D>(out var col)) col.enabled = true;

        // Tutorial de dash arranca antes del fade-in
        if (SceneFader.Instance != null) SceneFader.Instance.StartDashTutorial();

        // Fade in lento — Kael aparece en Idle, tutorial visible
        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FastFadeInAsync(1.5f);

        // Persistir derrota del boss — no reaparecerá al volver a MTN10
        if (SaveManager.ActiveSlot >= 0 && SaveManager.Instance != null)
        {
            var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            if (data != null) { data.bossDefeated = true; SaveManager.Instance.Save(SaveManager.ActiveSlot, data); }
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

        var textGO = new GameObject("PromptText");
        textGO.transform.SetParent(canvasGO.transform, false);
        var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = "[ E ] Mantener para extraer fragmento";
        tmp.fontSize  = 24;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color     = new Color(1f, 0.88f, 0.4f);

        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 120f);
        rt.sizeDelta        = new Vector2(600f, 50f);

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

    // ── Helpers de movimiento ─────────────────────────────────────────────────

    private void FacePlayer()
    {
        if (_player == null) return;
        float   dir = DirToPlayer();
        Vector3 s   = transform.localScale;

        // Flip horizontal: escalar.x negativo = mirando izquierda
        // Los hijos (hitboxes) se mueven automáticamente con el flip de escala
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
        _                => Random.Range(0.1f, 0.4f),   // Phase3: casi sin pausa
    };

    // ── Efectos de obsesión ───────────────────────────────────────────────────

    // Temblor escalado antes de ataques pesados — señal visual de carga
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
        // Tinte muy sutil al recibir daño — apenas perceptible, no distrae del sprite
        _sr.color = _phase switch
        {
            BossPhase.Phase2 => new Color(1f, 0.82f, 0.82f),   // rosa muy suave
            BossPhase.Phase3 => new Color(1f, 0.78f, 0.78f),   // rosa ligeramente más pronunciado
            _                => new Color(1f, 0.80f, 0.80f),   // fase 1: igual de sutil
        };
        yield return new WaitForSeconds(0.12f);
        _sr.color = Color.white;
        _flashing = false;
    }

    // ── Proyectiles / hitboxes a distancia ───────────────────────────────────

    // Posición del impacto ranged — delante del boss en espacio mundo (sin depender de scale).
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

    // Daño instantáneo en la posición del range attack — usado por DoSuper.
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
        // +0.4u sobre los pies del boss → boomerang rasante cerca del suelo.
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
        // Rango de detección
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Rango melee
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        // Límites del arena
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawLine(new Vector3(arenaMinX, -30f, 0), new Vector3(arenaMinX, 30f, 0));
        Gizmos.DrawLine(new Vector3(arenaMaxX, -30f, 0), new Vector3(arenaMaxX, 30f, 0));
    }
}
