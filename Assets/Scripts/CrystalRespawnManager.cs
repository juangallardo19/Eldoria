using System.Collections;
using UnityEngine;

// Singleton escena-local — gestiona respawn al tocar zonas de cristal.
// Patrón Observer: Update rastrea IsGrounded del jugador para guardar última posición segura.
// Patrón State (dos flags independientes):
//   _isRespawning — activo durante FadeOut/Teleport/FadeIn: bloquea cristales Y enemigos.
//   _isBlinking   — activo durante el parpadeo post-respawn: bloquea solo enemigos,
//                   los cristales SIGUEN siendo letales (CrystalHazard ignora este flag).
public class CrystalRespawnManager : MonoBehaviour
{
    public static CrystalRespawnManager Instance { get; private set; }

    [SerializeField] private int   defaultLives    = 5;
    [SerializeField] private float blinkDuration   = 2f;   // segundos de parpadeo tras reaparecer
    [SerializeField] private float blinkInterval   = 0.1f; // velocidad de cada destello

    private int              _lives;
    private Vector3          _lastSafePos;
    private bool             _isRespawning; // bloquea cristales (solo durante fade+teleport)
    private bool             _isBlinking;   // invencibilidad vs enemigos (durante el blink)
    private PlayerController _player;
    private SpriteRenderer   _playerSR;

    // Enemigos consultan este flag para saber si no deben aplicar daño.
    public bool IsBlinking => _isBlinking;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        _lives = defaultLives;
        if (SaveManager.Instance != null && SaveManager.ActiveSlot >= 0)
        {
            var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            if (!data.isEmpty) _lives = Mathf.Max(1, data.health);
        }

        _player = FindObjectOfType<PlayerController>();
        if (_player != null)
        {
            _lastSafePos = _player.transform.position;
            _playerSR    = _player.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (_isRespawning) return;
        if (_player == null)
        {
            _player   = FindObjectOfType<PlayerController>();
            _playerSR = _player != null ? _player.GetComponent<SpriteRenderer>() : null;
        }
        if (_player == null) return;

        if (_player.IsGrounded)
            _lastSafePos = _player.transform.position;
    }

    public void TriggerHazard()
    {
        if (_isRespawning || _player == null) return;
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        _isRespawning = true;

        var rb = _player.GetComponent<Rigidbody2D>();
        _player.enabled = false;
        if (rb != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }

        if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeOutAsync();
        else yield return new WaitForSeconds(0.4f);

        _player.transform.position = _lastSafePos;
        if (rb != null) rb.velocity = Vector2.zero;

        _lives = Mathf.Max(0, _lives - 1);
        PersistHealth();

        if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeInAsync();
        else yield return new WaitForSeconds(0.4f);

        // Sin vidas: ir a pantalla de selección sin parpadeo
        if (_lives <= 0)
        {
            SceneFader.Instance?.LoadScene("SlotsScreen");
            yield break;
        }

        // Restaurar físicas e input antes de parpadear
        if (rb != null) { rb.isKinematic = false; rb.velocity = Vector2.zero; }
        _player.enabled = true;

        // Cristales vuelven a ser letales inmediatamente; solo enemigos quedan bloqueados.
        _isRespawning = false;
        _isBlinking   = true;

        yield return StartCoroutine(BlinkCoroutine());

        _isBlinking = false;
    }

    // Alterna SpriteRenderer.enabled rápidamente para simular invencibilidad post-respawn.
    private IEnumerator BlinkCoroutine()
    {
        if (_playerSR == null) yield break;

        float elapsed = 0f;
        while (elapsed < blinkDuration)
        {
            _playerSR.enabled = !_playerSR.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }
        _playerSR.enabled = true;
    }

    private void PersistHealth()
    {
        if (SaveManager.Instance == null || SaveManager.ActiveSlot < 0) return;
        var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
        data.health = _lives;
        SaveManager.Instance.Save(SaveManager.ActiveSlot, data);
    }

    public int Lives => _lives;
}
