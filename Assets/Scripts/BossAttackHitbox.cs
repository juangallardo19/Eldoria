using UnityEngine;

// Patrón Command — hitbox de daño del boss, activada/desactivada por BossObsesion.
//
// BUG Unity: OnTriggerEnter2D NO dispara si el jugador YA ESTÁ dentro del área
// cuando el collider se habilita. Solución: Physics2D.OverlapBox instantáneo en
// Activate() + OnTriggerStay2D como respaldo, ambos usando el mismo flag.
[RequireComponent(typeof(BoxCollider2D))]
public class BossAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private BoxCollider2D _col;
    private bool          _hitThisActivation;

    void Awake()
    {
        _col           = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;
        _col.enabled   = false;
    }

    public void Activate(int dmg)
    {
        damage             = dmg;
        _hitThisActivation = false;
        _col.enabled       = true;

        // Chequeo instantáneo: si el jugador ya está dentro, golpear de inmediato
        CheckOverlap();
    }

    public void Deactivate()
    {
        _col.enabled = false;
    }

    // Jugador entra al trigger mientras está activo
    void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    // Jugador permanece dentro (backup por si OnTriggerEnter2D no disparó)
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void OnDisable() => _hitThisActivation = false;

    private void CheckOverlap()
    {
        if (_hitThisActivation) return;
        var bounds = _col.bounds;
        var hits   = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        foreach (var hit in hits)
        {
            if (hit == _col) continue;
            TryHit(hit);
            if (_hitThisActivation) break;
        }
    }

    private void TryHit(Collider2D other)
    {
        if (_hitThisActivation) return;
        bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;
        if (!isPlayer) return;
        if (CrystalRespawnManager.Instance == null) return;

        _hitThisActivation = true;
        _col.enabled       = false;
        CrystalRespawnManager.Instance.TakeBossDamage(damage);
    }
}
