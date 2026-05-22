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
    [Header("Duración de cada golpe — clip / 1.5 (Animator speed = 1.5x)")]
    [SerializeField] private float combo1Duration = 0.45f;   // 0.667 / 1.5
    [SerializeField] private float combo2Duration = 0.61f;   // 0.917 / 1.5
    [SerializeField] private float combo3Duration = 0.72f;   // 1.083 / 1.5

    [Header("Ventana de encadenado (segundos antes del final del golpe)")]
    [SerializeField] private float comboWindow = 0.20f;

    [Header("Hitbox — hijo 'AttackHitbox' (se crea auto si está vacío)")]
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private Vector2 hitboxOffset = new Vector2(1.2f, 0f);
    [SerializeField] private Vector2 hitboxSize   = new Vector2(1.8f, 1.5f);

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

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _ctrl = GetComponent<PlayerController>();
        _anim = GetComponent<Animator>();

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

        // Orientar hitbox según la dirección del jugador
        if (hitbox != null)
        {
            hitbox.offset  = new Vector2(hitboxOffset.x * _ctrl.FacingDir, hitboxOffset.y);
            hitbox.size    = hitboxSize;
            hitbox.enabled = true;
        }
    }

    private void EndCombo()
    {
        _comboStep  = 0;
        _nextQueued = false;
        if (hitbox != null) hitbox.enabled = false;
        // El Animator vuelve a Idle por ExitTime automáticamente; no hace falta reset.
    }

    // ── Daño ──────────────────────────────────────────────────────────────────
    private void ApplyDamage()
    {
        _hitDealt = true;
        if (hitbox == null) return;

        int dmg = DamageOf(_comboStep);
        Vector2 center = (Vector2)transform.position + hitbox.offset;
        var hits = Physics2D.OverlapBoxAll(center, hitbox.size, 0f, enemyLayer);
        foreach (var col in hits)
        {
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
        Vector3 c = transform.position + new Vector3(hitbox.offset.x, hitbox.offset.y, 0);
        Gizmos.DrawCube(c, new Vector3(hitbox.size.x, hitbox.size.y, 0.1f));
    }
#endif
}
