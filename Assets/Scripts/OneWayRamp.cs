using System.Collections;
using UnityEngine;

// Rampa unidireccional inclinada.
// Patrón: Strategy — ShouldIgnore() decide cada FixedUpdate si la colisión existe o no,
//         igual que OneWayPlatform pero adaptado para superficies rotadas.
//
// La "superficie" se define por transform.up en espacio mundo (la normal de la rampa).
// Si el player está del lado positivo de esa normal → puede aterrizar.
// Si está del lado negativo (debajo/detrás) o más allá de los extremos → atraviesa.
//
// Diferencias clave respecto a OneWayPlatform:
//   · En vez de comparar Y de AABB, proyecta en transform.up (normal rotada).
//   · En vez de comparar X de AABB, proyecta en transform.right (largo rotado).
//   · El chequeo de "subiendo" usa velocidad proyectada sobre la normal, no velocity.y.
[RequireComponent(typeof(BoxCollider2D))]
public class OneWayRamp : MonoBehaviour
{
    // LAND_TOLERANCE: umbral para activar colisión (pies a ≤0.2 de la superficie).
    // LEAVE_TOLERANCE: ventana de histéresis — el jugador debe alejarse (LAND+LEAVE)
    //   por debajo de la superficie antes de que se desactive la colisión.
    //   Evita el toggle rápido on/off que crea la contrafuerza al caminar despacio.
    private const float LAND_TOLERANCE  = 0.2f;
    private const float LEAVE_TOLERANCE = 1.0f;

    private BoxCollider2D  _box;
    private Collider2D[]   _allCols;
    private float          _halfLength;

    private Collider2D   _playerCol;
    private bool         _dropping;
    private bool         _rampColliding;  // estado de histéresis

    void Awake()
    {
        _box        = GetComponent<BoxCollider2D>();
        _allCols    = GetComponents<Collider2D>();
        _halfLength = _box.size.x * 0.5f;

        // Alta fricción para prevenir deslizamiento en pendiente 45° (tan45°=1.0 → necesitamos μ≥1.0)
        var mat = new PhysicsMaterial2D("RampFriction") { friction = 2f, bounciness = 0f };
        _box.sharedMaterial = mat;
    }

    void Start()    => FindPlayer();
    void OnEnable()
    {
        if (_box == null) { _box = GetComponent<BoxCollider2D>(); _allCols = GetComponents<Collider2D>(); }
        if (_playerCol == null) FindPlayer();
    }

    // FixedUpdate proactivo: se ejecuta ANTES de que Unity resuelva la física.
    void FixedUpdate()
    {
        if (_playerCol == null) { FindPlayer(); return; }

        bool ignore = ShouldIgnore();
        foreach (var col in _allCols)
            Physics2D.IgnoreCollision(_playerCol, col, ignore);
    }

    // ── Decisión central ──────────────────────────────────────────────────────
    // Usa los PIES del player (AABB bottom) en vez del centro, y NO verifica velocidad.
    // Ventajas:
    //   · Elimina el bug de "auto-salto": los pies no cambian de posición al soltar una tecla.
    //   · Elimina la "expulsión" en la base: a nivel del suelo los pies quedan siempre
    //     por debajo del plano de la rampa, por lo que nunca se activa la colisión lateral.
    //   · Jump-through desde abajo: los pies pasan de debajo a arriba de la superficie
    //     en menos de un frame (velocidad de salto ≫ zona de transición) → sin bloqueo.
    private bool ShouldIgnore()
    {
        if (_dropping) return true;

        Vector2 rampNormal = transform.up;
        Vector2 rampAlong  = transform.right;
        Vector2 rampCenter = _box.bounds.center;

        // Punto de referencia: centro-X del player, Y en el fondo del AABB (pies).
        Vector2 playerFeet = new Vector2(_playerCol.bounds.center.x, _playerCol.bounds.min.y);
        Vector2 toFeet     = playerFeet - rampCenter;

        // ── ¿El player está dentro del largo de la rampa? ────────────────────
        float along = Vector2.Dot(toFeet, rampAlong);
        if (Mathf.Abs(along) > _halfLength)
        {
            _rampColliding = false;   // salió por los extremos → resetear histéresis
            return true;
        }

        // ── Histéresis: evita el toggle rápido on/off en la superficie ────────
        // Sin histéresis, la penetración física hace oscilar signedDist ±ε cruzando
        // el umbral cada frame, lo que cancela el avance y genera la contrafuerza.
        float signedDist = Vector2.Dot(toFeet, rampNormal);
        if (_rampColliding)
        {
            // Ya colisionando: sólo desactiva si los pies bajan claramente debajo
            if (signedDist < -(LAND_TOLERANCE + LEAVE_TOLERANCE))
                _rampColliding = false;
        }
        else
        {
            // Ignorando: activa cuando los pies alcanzan la superficie
            if (signedDist >= -LAND_TOLERANCE)
                _rampColliding = true;
        }

        return !_rampColliding;
    }

    // ── Drop-through (por si PlayerController lo necesita en el futuro) ───────
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
            return;
        }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _playerCol = p.GetComponent<Collider2D>();
            return;
        }
        Debug.LogWarning($"[OneWayRamp] {name}: Player no encontrado en la escena.");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_box == null) _box = GetComponent<BoxCollider2D>();
        float hl = _box != null ? _box.size.x * 0.5f : _halfLength;

        // Línea de la superficie de la rampa
        Vector3 leftEdge  = transform.position - transform.right * hl;
        Vector3 rightEdge = transform.position + transform.right * hl;
        Gizmos.color = new Color(0.1f, 0.9f, 0.3f, 1f);
        Gizmos.DrawLine(leftEdge, rightEdge);

        // Flecha indicando el lado transitable (normal positiva)
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 1.5f);
    }
#endif
}
