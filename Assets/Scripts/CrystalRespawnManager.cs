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

    // Patrón Observer — PlayerHUD se suscribe a estos eventos para animar los iconos Ara
    public static event System.Action<int>      OnLivesChanged;   // (newLives)
    public static event System.Action<int, int> OnDamageTaken;    // (newLives, prevLives)
    public static event System.Action<int>      OnLivesRestored;  // (newLives)

    // Persiste las vidas entre cambios de escena en memoria (no en disco).
    // Evita que las vidas se restauren a 5 al cargar una nueva escena.
    private static int   _persistedLives     = -1;
    private static float _persistedTimestamp = -1f;
    const float PERSIST_WINDOW = 15f;  // segundos máximos entre OnDestroy y el siguiente Start

    // ── Ciclo de vida ────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        // Guardar vidas en memoria y en disco antes de que la escena se descargue.
        _persistedLives     = _lives;
        _persistedTimestamp = Time.realtimeSinceStartup;
        if (_lives > 0) PersistHealth();
        Instance = null;
    }

    void Start()
    {
        // Usar vidas en memoria si provienen de un cambio de escena reciente.
        float age = Time.realtimeSinceStartup - _persistedTimestamp;
        if (_persistedLives >= 0 && age < PERSIST_WINDOW)
        {
            _lives = _persistedLives;
            _persistedLives     = -1;
            _persistedTimestamp = -1f;
        }
        else
        {
            _lives = defaultLives;
            if (SaveManager.Instance != null && SaveManager.ActiveSlot >= 0)
            {
                var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
                if (data != null && !data.isEmpty) _lives = Mathf.Max(1, data.health);
            }
        }
        // Notifica al HUD inmediatamente — evita que las Aras aparezcan como cenizas
        // porque OnSceneLoaded en PlayerHUD dispara ANTES de que Start() inicialice _lives.
        OnLivesChanged?.Invoke(_lives);

        _player = FindObjectOfType<PlayerController>();
        if (_player != null)
        {
            _lastSafePos = _player.transform.position;
            _playerSR    = _player.GetComponent<SpriteRenderer>();
            _playerAnim  = _player.GetComponent<PlayerAnimator>();

            // Garantiza que el jugador esté activo y visible al entrar en una escena
            // (puede llegar desactivado si el cambio de escena interrumpió un respawn).
            _player.enabled = true;
            if (_playerSR != null) _playerSR.enabled = true;
            var rbStart = _player.GetComponent<Rigidbody2D>();
            if (rbStart != null) rbStart.isKinematic = false;
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
    // Sin fade ni teleporte: solo animación hurt + parpadeo de invulnerabilidad.
    // Si las vidas llegan a 0, sí ejecuta muerte con fade a santuario.
    public void TakeBossDamage(int livesLost)
    {
        if (_isRespawning || _isBlinking || _player == null) return;
        StartCoroutine(BossDamageCoroutine(livesLost));
    }

    // Llamado por SanctuaryFlame al descansar
    public void RestoreLives()
    {
        _lives = defaultLives;
        PersistHealth();
        OnLivesRestored?.Invoke(_lives);
        OnLivesChanged?.Invoke(_lives);
    }

    // Versión estática: funciona aunque la escena no tenga instancia (ej. MTN03 sin cristales).
    public static void RestoreLivesGlobal(int lives = 5)
    {
        if (Instance != null) { Instance.RestoreLives(); return; }
        if (SaveManager.Instance != null && SaveManager.ActiveSlot >= 0)
        {
            var d = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            if (d != null) { d.health = lives; SaveManager.Instance.Save(SaveManager.ActiveSlot, d); }
        }
        OnLivesRestored?.Invoke(lives);
        OnLivesChanged?.Invoke(lives);
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
            OnDamageTaken?.Invoke(0, _lives);  // HUD muestra todas las Aras muertas
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

                // FadeOutAsync dejó _isFading=true; usar LoadSceneAfterFade para saltarse el guard.
                if (SceneFader.Instance != null) SceneFader.Instance.LoadSceneAfterFade(sanctScene);
                else UnityEngine.SceneManagement.SceneManager.LoadScene(sanctScene);
            }
            else
            {
                // Sin santuario: volver al inicio del bioma donde murió el jugador.
                string fallback = FallbackScene();
                _lives = defaultLives;
                PersistHealth();
                PlayerSpawnManager.NextSpawnId = "default";
                if (SceneFader.Instance != null) SceneFader.Instance.LoadSceneAfterFade(fallback);
                else UnityEngine.SceneManagement.SceneManager.LoadScene(fallback);
            }
            yield break;
        }

        // ── Daño normal: hurt → esperar animación completa → fade → teleport → blink ──
        // Notifica al HUD ANTES del fade para que la animación Ara sea visible
        OnDamageTaken?.Invoke(livesAfter, _lives);

        _player?.PlayHurtSound();

        // Re-obtener _playerAnim por si acaso llegó null entre cambios de escena
        if (_playerAnim == null && _player != null)
            _playerAnim = _player.GetComponent<PlayerAnimator>();

        _playerAnim?.TriggerHurt();

        // Esperar que termine la animación Hurt (7 frames @ 12fps = 0.583s)
        // antes de hacer el fade, para que el jugador vea la animación completa.
        yield return new WaitForSeconds(0.5833f);

        _player.enabled = false;
        if (rb != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }

        if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeOutAsync();
        else yield return new WaitForSeconds(0.4f);

        _player.transform.position = _lastSafePos;
        if (rb != null) rb.velocity = Vector2.zero;

        _lives = livesAfter;
        PersistHealth();
        OnLivesChanged?.Invoke(_lives);

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

    // ── Daño del boss sin teleporte ──────────────────────────────────────────
    // Vidas > 0: solo hurt + parpadeo, jugador no se deshabilita ni hay fade.
    // Vidas = 0: muerte completa con fade a santuario (igual que cristales).

    private IEnumerator BossDamageCoroutine(int livesLost)
    {
        _isRespawning = true;

        int livesAfter = Mathf.Max(0, _lives - livesLost);
        var rb = _player.GetComponent<Rigidbody2D>();

        if (livesAfter <= 0)
        {
            // Muerte por boss: animación de muerte + fade a santuario
            OnDamageTaken?.Invoke(0, _lives);
            if (_playerAnim == null && _player != null)
                _playerAnim = _player.GetComponent<PlayerAnimator>();
            _playerAnim?.TriggerDie();

            _player.enabled = false;
            if (rb != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }

            yield return new WaitForSeconds(deathAnimDuration);

            if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeOutAsync();
            else yield return new WaitForSeconds(0.4f);

            _lives = 0;
            PersistHealth();

            string sanctScene = PlayerPrefs.GetString("SanctuaryScene", "");
            if (!string.IsNullOrEmpty(sanctScene))
            {
                _lives = defaultLives;
                PersistHealth();
                float sx = PlayerPrefs.GetFloat("SanctuaryX", 0f);
                float sy = PlayerPrefs.GetFloat("SanctuaryY", 0f);
                PlayerSpawnManager.UsePositionOverride   = true;
                PlayerSpawnManager.OverridePositionValue = new Vector2(sx, sy);
                if (SceneFader.Instance != null) SceneFader.Instance.LoadSceneAfterFade(sanctScene);
                else UnityEngine.SceneManagement.SceneManager.LoadScene(sanctScene);
            }
            else
            {
                string fallback = FallbackScene();
                _lives = defaultLives;
                PersistHealth();
                PlayerSpawnManager.NextSpawnId = "default";
                if (SceneFader.Instance != null) SceneFader.Instance.LoadSceneAfterFade(fallback);
                else UnityEngine.SceneManagement.SceneManager.LoadScene(fallback);
            }
            yield break;
        }

        // Daño normal: HUD + hurt animation. El jugador sigue activo y puede moverse.
        OnDamageTaken?.Invoke(livesAfter, _lives);
        _lives = livesAfter;
        PersistHealth();

        if (_playerAnim == null && _player != null)
            _playerAnim = _player.GetComponent<PlayerAnimator>();
        _playerAnim?.TriggerHurt();
        _player?.PlayHurtSound();

        // Esperar animación hurt (7 frames @ 12fps = 0.583s)
        yield return new WaitForSeconds(0.5833f);

        OnLivesChanged?.Invoke(_lives);

        _isRespawning = false;
        _isBlinking   = true;
        if (_playerSR == null && _player != null)
            _playerSR = _player.GetComponent<SpriteRenderer>();
        yield return StartCoroutine(BlinkCoroutine());
        _isBlinking = false;
    }

    // ── Persistencia ─────────────────────────────────────────────────────────

    private void PersistHealth()
    {
        if (SaveManager.Instance == null || SaveManager.ActiveSlot < 0) return;
        var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
        data.health = _lives;
        SaveManager.Instance.Save(SaveManager.ActiveSlot, data);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Escena de respaldo si el jugador muere sin santuario guardado.
    // HV → vuelve a la casa de Kael. MTN → vuelve al exterior de las Montañas.
    private static string FallbackScene()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene.StartsWith("HV") || scene == "Intro" || scene == "MainMenu")
            return "HV01_Interior";
        return "MTN01_Exterior";
    }

    // Llamar desde SlotsScreenManager al iniciar/continuar partida para que
    // las vidas de una partida anterior no contaminen la nueva.
    public static void InvalidatePersistedLives()
    {
        _persistedLives     = -1;
        _persistedTimestamp = -1f;
    }
}
