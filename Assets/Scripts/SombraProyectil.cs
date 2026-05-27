using UnityEngine;

// Patrón: Command
// Proyectil animado lanzado por SombraMago.
// Los frames (atack sombraprime1-6) muestran el proyectil expandiéndose en vuelo.
// Viaja en línea recta; destruye al tocar al jugador o después de maxLifetime.
// Ignora colisiones con otros enemigos Sombra para no bloquearse entre sí.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SombraProyectil : MonoBehaviour
{
    [Header("Animación (asignado por SetupSombraMago)")]
    public Sprite[] frames;

    [Header("Comportamiento")]
    [SerializeField] private float fps         = 10f;
    [SerializeField] private float speed       = 7f;
    [SerializeField] private float maxLifetime = 3f;
    [SerializeField] private int   damage      = 1;

    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;
    private float          _frameTimer;
    private int            _frameIdx;
    private bool           _hit;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        var col       = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.35f;

        Destroy(gameObject, maxLifetime);
    }

    // Llamado por SombraMago justo después de Instantiate.
    public void Launch(Vector2 direction, int dmg)
    {
        damage       = dmg;
        _rb.velocity = direction.normalized * speed;
        _sr.flipX    = direction.x < 0f;

        // Primer frame visible inmediatamente
        if (frames != null && frames.Length > 0 && frames[0] != null)
            _sr.sprite = frames[0];
    }

    void Update()
    {
        if (_hit || frames == null || frames.Length == 0) return;

        _frameTimer += Time.deltaTime;
        float dur = 1f / Mathf.Max(fps, 1f);
        while (_frameTimer >= dur)
        {
            _frameTimer -= dur;
            _frameIdx = (_frameIdx + 1) % frames.Length;
        }
        if (frames[_frameIdx] != null)
            _sr.sprite = frames[_frameIdx];
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_hit) return;

        // No interactuar con otros enemigos sombra
        if (other.GetComponent<SombraMago>()  != null) return;
        if (other.GetComponent<SombraNinja>() != null) return;

        bool isPlayer = other.CompareTag("Player") ||
                        other.GetComponent<PlayerController>() != null;

        if (isPlayer && CrystalRespawnManager.Instance != null)
            CrystalRespawnManager.Instance.TakeBossDamage(damage);

        _hit = true;
        Destroy(gameObject);
    }
}
