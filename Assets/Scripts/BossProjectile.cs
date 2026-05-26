using UnityEngine;

// Proyectil simple del ataque a distancia del boss.
// Viaja horizontalmente, aplica -1 vida al tocar al jugador, se destruye.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private float speed    = 12f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private int   damage   = 1;

    private float _dir;
    private bool  _hit;

    public void Init(float direction, Sprite sprite)
    {
        _dir = direction;
        var sr = GetComponent<SpriteRenderer>();
        if (sprite != null) sr.sprite = sprite;
        sr.flipX = direction < 0f;

        var col       = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(0.5f, 0.5f);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (_hit) return;
        transform.position += Vector3.right * _dir * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_hit) return;
        bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;
        if (!isPlayer) return;
        if (CrystalRespawnManager.Instance == null) return;

        _hit = true;
        CrystalRespawnManager.Instance.TakeBossDamage(damage);
        Destroy(gameObject);
    }
}
