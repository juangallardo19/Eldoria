using System.Collections;
using UnityEngine;

// Singleton escena-local — gestiona respawn al tocar cristales o recibir daño de boss.
// Patrón Observer: Update rastrea IsGrounded → _lastSafePos.
// Patrón State (dos flags):
//   _isRespawning — durante FadeOut/Teleport/FadeIn: bloquea cristales Y enemigos.
//   _isBlinking   — parpadeo post-respawn: bloquea solo enemigos (cristales SIGUEN matando).
//
// Flujo de muerte (0 vidas):
//   TriggerDie() → animación death completa (1.92s) → FadeOut → santuario o SlotsScreen
// Flujo de daño normal (>0 vidas):
//   TriggerHurt() → FadeOut → teleport a lastSafePos → FadeIn → blink 2s
public class CrystalRespawnManager : MonoBehaviour
{
    public static CrystalRespawnManager Instance { get; private set; }

    [SerializeField] private int   defaultLives    = 5;
    [SerializeField] private float blinkDuration   = 2f;
    [SerializeField] private float blinkInterval   = 0.1f;
    // Duración animación Death de Kael: 23 frames @ 12fps = 1.916s
    [SerializeField] private float deathAnimDuration = 1.92f;

    private int              _lives;
    private Vector3          _lastSafePos;
    private bool             _isRespawning;
    private bool             _isBlinking;
    private PlayerController _player;
    private SpriteRenderer   _playerSR;
    private PlayerAnimator   _playerAnim;

    public bool IsBlinking   => _isBlinking;
    public bool IsRespawning => _isRespawning;
    public int  Lives        => _lives;

    // ── Ciclo de vida ────────────────────────────────────────────────────────

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
            _playerAnim  = _player.GetComponent<PlayerAnimator>();
        }
    }

    void Update()
    {
        if (_isRespawning) return;

        if (_player == null)
        {
            _player     = FindObjectOfType<PlayerController>();
            _playerSR   = _player != null ? _player.GetComponent<SpriteRenderer>() : null;
            _playerAnim = _player != null ? _player.GetComponent<PlayerAnimator>()  : null;
        }
        if (_player == null) return;

        if (_player.IsGrounded)
            _lastSafePos = _player.transform.position;
    }

    // ── API pública ──────────────────────────────────────────────────────────

    // Llamado por CrystalHazard — ignora _isBlinking (cristales siempre son letales)
    public void TriggerHazard()
    {
        if (_isRespawning || _player == null) return;
        StartCoroutine(RespawnCoroutine(1));
    }

    // Llamado por ataques del boss — respeta _isBlinking (post-respawn)
    public void TakeBossDamage(int livesLost)
    {
        if (_isRespawning || _isBlinking || _player == null) return;
        StartCoroutine(RespawnCoroutine(livesLost));
    }

    // Llamado por SanctuaryFlame al descansar
    public void RestoreLives()
    {
        _lives = defaultLives;
        PersistHealth();
    }

    // ── Respawn principal ────────────────────────────────────────────────────

    private IEnumerator RespawnCoroutine(int livesLost)
    {
        _isRespawning = true;

        int livesAfter = Mathf.Max(0, _lives - livesLost);
        var rb = _player.GetComponent<Rigidbody2D>();

        if (livesAfter <= 0)
        {
            // ── Muerte: animación completa antes del fade ──────────────────
            _playerAnim?.TriggerDie();

            _player.enabled = false;
            if (rb != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }

            // Esperar a que termine la animación de muerte
            yield return new WaitForSeconds(deathAnimDuration);

            if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeOutAsync();
            else yield return new WaitForSeconds(0.4f);

            _lives = 0;
            PersistHealth();

            // Intentar regresar al último santuario de Ara
            string sanctScene = PlayerPrefs.GetString("SanctuaryScene", "");
            if (!string.IsNullOrEmpty(sanctScene))
            {
                _lives = defaultLives;
                PersistHealth();

                float sx = PlayerPrefs.GetFloat("SanctuaryX", 0f);
                float sy = PlayerPrefs.GetFloat("SanctuaryY", 0f);
                PlayerSpawnManager.UsePositionOverride   = true;
                PlayerSpawnManager.OverridePositionValue = new Vector2(sx, sy);

                if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(sanctScene);
                else UnityEngine.SceneManagement.SceneManager.LoadScene(sanctScene);
            }
            else
            {
                // Sin santuario visitado: enviar al inicio de las Montañas con vidas completas
                _lives = defaultLives;
                PersistHealth();
                PlayerSpawnManager.NextSpawnId = "default";
                if (SceneFader.Instance != null) SceneFader.Instance.LoadScene("MTN01_Exterior");
                else UnityEngine.SceneManagement.SceneManager.LoadScene("MTN01_Exterior");
            }
            yield break;
        }

        // ── Daño normal: hurt → fade → teleport → blink ───────────────────
        _playerAnim?.TriggerHurt();

        _player.enabled = false;
        if (rb != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }

        if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeOutAsync();
        else yield return new WaitForSeconds(0.4f);

        _player.transform.position = _lastSafePos;
        if (rb != null) rb.velocity = Vector2.zero;

        _lives = livesAfter;
        PersistHealth();

        if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeInAsync();
        else yield return new WaitForSeconds(0.4f);

        if (rb != null) { rb.isKinematic = false; rb.velocity = Vector2.zero; }
        _player.enabled = true;

        _isRespawning = false;
        _isBlinking   = true;
        yield return StartCoroutine(BlinkCoroutine());
        _isBlinking = false;
    }

    // ── Blink post-respawn ───────────────────────────────────────────────────

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

    // ── Persistencia ─────────────────────────────────────────────────────────

    private void PersistHealth()
    {
        if (SaveManager.Instance == null || SaveManager.ActiveSlot < 0) return;
        var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
        data.health = _lives;
        SaveManager.Instance.Save(SaveManager.ActiveSlot, data);
    }
}
