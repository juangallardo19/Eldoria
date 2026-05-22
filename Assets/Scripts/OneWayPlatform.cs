using System.Collections;
using UnityEngine;

// Plataforma unidireccional — sistema propio inspirado en Hollow Knight / Celeste.
// Patrón: Strategy — ShouldIgnore() decide cada FixedUpdate si la colisión existe o no,
//         ANTES de que Unity calcule la física (enfoque proactivo, no reactivo).
//
// Lógica:
//   COLISIONA  → fondo del player ENCIMA de la superficie Y player cayendo/quieto
//               Y centro X del player dentro del ancho de la plataforma.
//   IGNORA     → player debajo, lateral (centro X fuera del ancho), subiendo, o en drop-through.
//
// Gestiona TODOS los Collider2D del mismo GameObject para evitar el bug de colisiones
// duplicadas cuando el objeto tiene más de un BoxCollider2D.
[RequireComponent(typeof(Collider2D))]
public class OneWayPlatform : MonoBehaviour
{
    // Tolerancia: el fondo puede estar hasta X unidades "dentro" del borde y aun aterrizar.
    private const float LAND_TOLERANCE  = 0.1f;

    // Umbral: si velocity.y supera esto se considera que el player sube → ignorar.
    private const float RISING_THRESHOLD = 0.4f;

    // Collider de referencia para calcular bounds (el primero); todos se ignoran juntos.
    private Collider2D   _col;
    private Collider2D[] _allCols;   // todos los Collider2D de este GameObject

    private Collider2D  _playerCol;
    private Rigidbody2D _playerRb;
    private bool        _dropping;

    void Awake()
    {
        _col     = GetComponent<Collider2D>();
        _allCols = GetComponents<Collider2D>();
    }

    void Start()    => FindPlayer();
    void OnEnable()
    {
        if (_col == null)    { _col = GetComponent<Collider2D>(); _allCols = GetComponents<Collider2D>(); }
        if (_playerCol == null) FindPlayer();
    }

    // FixedUpdate: proactivo — se ejecuta ANTES de que Unity resuelva la física.
    // Aplica la decisión a TODOS los colliders del objeto, no solo al primero.
    void FixedUpdate()
    {
        if (_playerCol == null) { FindPlayer(); return; }

        bool ignore = ShouldIgnore();
        foreach (var col in _allCols)
            Physics2D.IgnoreCollision(_playerCol, col, ignore);
    }

    // ── Decisión central ──────────────────────────────────────────────────────
    private bool ShouldIgnore()
    {
        if (_dropping) return true;

        float surfaceY      = _col.bounds.max.y;
        float playerBottomY = _playerCol.bounds.min.y;

        // Player por debajo de la superficie → viene de abajo o sube por el costado
        if (playerBottomY < surfaceY - LAND_TOLERANCE) return true;

        // Player a nivel de superficie o encima: verificar que esté ENCIMA, no lateral.
        // Si el centro X del player está fuera del ancho de la plataforma → viene de lado.
        float playerCenterX = _playerCol.bounds.center.x;
        if (playerCenterX < _col.bounds.min.x || playerCenterX > _col.bounds.max.x) return true;

        // Player encima pero subiendo → puede atravesar hacia arriba
        if (_playerRb != null && _playerRb.velocity.y > RISING_THRESHOLD) return true;

        // Player encima, alineado horizontalmente, cayendo o quieto → aterriza
        return false;
    }

    // ── Drop-through (llamado desde PlayerController) ─────────────────────────
    public void TriggerDropThrough(float duration = 0.28f)
    {
        StopAllCoroutines();
        StartCoroutine(DropRoutine(duration));
    }

    private IEnumerator DropRoutine(float duration)
    {
        _dropping = true;
        yield return new WaitForSeconds(duration);
        _dropping = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void FindPlayer()
    {
        var ctrl = FindObjectOfType<PlayerController>();
        if (ctrl != null)
        {
            _playerCol = ctrl.GetComponent<Collider2D>();
            _playerRb  = ctrl.GetComponent<Rigidbody2D>();
            return;
        }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _playerCol = p.GetComponent<Collider2D>();
            _playerRb  = p.GetComponent<Rigidbody2D>();
            return;
        }
        Debug.LogWarning($"[OneWayPlatform] {name}: Player no encontrado.");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_col == null) _col = GetComponent<Collider2D>();
        if (_col == null) return;
        var b = _col.bounds;
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 0.35f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 1f);
        Gizmos.DrawLine(new Vector3(b.min.x, b.max.y), new Vector3(b.max.x, b.max.y));
    }
#endif
}
