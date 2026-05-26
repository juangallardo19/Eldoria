using UnityEngine;

// Sistema de combos melee de Kael: X presionado → Combo1 → Combo2 → Combo3.
// Patrón: State Machine — 4 estados (Idle=0, Combo1=1, Combo2=2, Combo3=3).
// · Primer press de X inicia Combo1.
// · Si X se presiona DURANTE la ventana de encadenado del golpe actual, el siguiente
//   combo queda en buffer y se ejecuta automáticamente al terminar la animación.
// · Tras Combo3 o si se deja expirar la ventana, vuelve a Idle.
//
// Integración con Animator (KaelAnimator):
//   Trigger "IsAttacking1" → estado Attack1
//   Trigger "IsAttacking2" → estado Attack2
//   Trigger "IsAttacking3" → estado Attack3
//   Cada estado regresa a Idle por ExitTime (sin condición extra).
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Duración de cada golpe (reducida para ataques rápidos)")]
    [SerializeField] private float combo1Duration = 0.25f;
    [SerializeField] private float combo2Duration = 0.42f;
    [SerializeField] private float combo3Duration = 0.50f;

    [Header("Ventana de encadenado (segundos antes del final del golpe)")]
    [SerializeField] private float comboWindow = 0.18f;

    [Header("Nombre del estado Idle en el Animator (para saltar animación de fin de ataque)")]
    [SerializeField] private string idleStateName = "Idle";

    [Header("Hitbox — hijo 'AttackHitbox' (se crea auto si está vacío)")]
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private Vector2 hitboxOffset = new Vector2(1.2f, 1.5f);
    [SerializeField] private Vector2 hitboxSize   = new Vector2(3f, 3f);

    [Header("Daño por golpe")]
    [SerializeField] private int combo1Damage = 10;
    [SerializeField] private int combo2Damage = 15;
    [SerializeField] private int combo3Damage = 25;

    [Header("Capa de enemigos (para detectar impactos)")]
    [SerializeField] private LayerMask enemyLayer;

    private Animator         _anim;
    private PlayerController _ctrl;

    private int   _comboStep;    // 0=idle, 1/2/3 = combo activo
    private float _comboTimer;   // tiempo restante del golpe actual
    private bool  _nextQueued;   // X presionado durante ventana → ejecutar siguiente
    private bool  _hitDealt;     // para marcar que ya se hizo daño en este swing

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

        // ── Idle: iniciar combo ───────────────────────────────────────────────
        if (_comboStep == 0)
        {
            if (pressed) StartCombo(1);
            return;
        }

        // ── Golpe activo ──────────────────────────────────────────────────────
        _comboTimer -= Time.deltaTime;

        // Ventana de encadenado: última fracción del golpe acepta el input del siguiente
        bool inWindow = _comboTimer <= comboWindow;
        if (pressed && inWindow && _comboStep < 3)
            _nextQueued = true;

        // Aplicar daño en el hit-frame (primer frame del golpe activo)
        if (!_hitDealt)
            ApplyDamage();

        // Fin del golpe
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
                case 3: _anim.SetTrigger(_hashAtk3); break;
            }
        }

        // La hitbox es hijo del jugador y hereda su scale flip — no multiplicar por FacingDir.
        if (hitbox != null)
        {
            hitbox.offset  = hitboxOffset;
            hitbox.size    = hitboxSize;
            hitbox.enabled = true;
        }
    }

    private void EndCombo()
    {
        _comboStep  = 0;
        _nextQueued = false;
        if (hitbox != null) hitbox.enabled = false;
        if (_anim != null) _anim.Play(_hashIdle, 0, 0f);
    }

    // ── Daño ──────────────────────────────────────────────────────────────────
    private void ApplyDamage()
    {
        _hitDealt = true;
        if (hitbox == null) return;

        int dmg = DamageOf(_comboStep);
        Vector2 center = hitbox.bounds.center;

        // Si enemyLayer no está configurado en Inspector (valor 0 = Nothing),
        // usar DefaultRaycastLayers para detectar cualquier IDamageable en escena.
        int mask = (int)enemyLayer != 0 ? (int)enemyLayer : Physics2D.DefaultRaycastLayers;
        var hits = Physics2D.OverlapBoxAll(center, hitbox.size, 0f, mask);

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;  // no dañarse a sí mismo
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

    // ── Estado público ────────────────────────────────────────────────────────
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
