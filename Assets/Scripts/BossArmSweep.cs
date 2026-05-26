using System.Collections;
using UnityEngine;

// Brazo del boss como barrido rasante — vuela horizontalmente a ras del suelo, ida y vuelta.
// Mecánica: el jugador DEBE SALTAR para esquivarlo. Sin homing: trayectoria puramente horizontal.
// Puede golpear una vez en la ida y una vez en la vuelta.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BossArmSweep : MonoBehaviour
{
    [SerializeField] private float speed  = 11f;
    [SerializeField] private int   damage = 1;

    private float _originX;
    private float _wallX;
    private float _fixedY;
    private bool  _hit;

    // direction: +1 = hacia la derecha, -1 = izquierda
    // wallX: límite del arena hasta donde volar
    // El Y ya debe estar seteado en transform.position antes de llamar Init
    public void Init(float direction, float wallX, Sprite sprite)
    {
        _originX = transform.position.x;
        _wallX   = wallX;
        _fixedY  = transform.position.y;

        var sr = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        else
        {
            var tex = new Texture2D(16, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[64];
            var c = new Color(1f, 0.55f, 0.1f);
            for (int i = 0; i < 64; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 4), new Vector2(0.5f, 0.5f), 2f);
        }
        sr.flipX        = direction < 0f;
        sr.sortingOrder = 5;

        var col       = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        // 8u ancho x 1.8u alto (arm scale=1): cubre el cuerpo central del brazo.
        // Y=-11.5: golpea jugador parado [-12,-10]; free si salta ≥1.5u (centro ≥-9.5).
        col.size      = new Vector2(8.0f, 1.8f);

        StartCoroutine(SweepRoutine(direction));
    }

    private IEnumerator SweepRoutine(float dir)
    {
        // ── Ida: vuela hacia el límite del arena ──────────────────────────────
        while (Mathf.Abs(transform.position.x - _wallX) > 0.4f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(_wallX, _fixedY, 0f),
                speed * Time.deltaTime);
            transform.Rotate(0f, 0f, dir * 540f * Time.deltaTime);
            yield return null;
        }

        // ── Vuelta: regresa al origen ──────────────────────────────────────────
        _hit = false;   // puede volver a golpear en el regreso

        while (Mathf.Abs(transform.position.x - _originX) > 0.4f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(_originX, _fixedY, 0f),
                speed * Time.deltaTime);
            transform.Rotate(0f, 0f, -dir * 540f * Time.deltaTime);
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_hit) return;
        bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;
        if (!isPlayer) return;
        if (CrystalRespawnManager.Instance == null) return;

        _hit = true;
        CrystalRespawnManager.Instance.TakeBossDamage(damage);
    }
}
