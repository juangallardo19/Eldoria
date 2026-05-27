using System.Collections;
using UnityEngine;

// Scene-local Singleton — manages respawn when the player touches crystals or takes boss damage.
// Pattern Observer: Update tracks IsGrounded → _lastSafePos.
// Pattern State (two flags):
//   _isRespawning — during FadeOut/Teleport/FadeIn: blocks both crystals AND enemies.
//   _isBlinking   — post-respawn blink: blocks enemies only (crystals STILL kill).
//
// Death flow (0 lives):
//   TriggerDie() → full death animation (1.92s) → FadeOut → sanctuary or SlotsScreen
// Damage flow (>0 lives):
//   TriggerHurt() → FadeOut → teleport to lastSafePos → FadeIn → blink 2s
public class CrystalRespawnManager : MonoBehaviour
{
    public static CrystalRespawnManager Instance { get; private set; }

    [SerializeField] private int   defaultLives      = 5;
    [SerializeField] private float blinkDuration     = 2f;
    [SerializeField] private float blinkInterval     = 0.1f;
    // Kael's Death animation: 23 frames @ 12fps = 1.916s
    [SerializeField] private float deathAnimDuration = 1.92f;

    // Hurt animation: 7 frames @ 12fps = 0.583s — kept as const for clarity
    private const float HurtAnimDuration = 0.5833f;

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

    // Observer — PlayerHUD subscribes to animate the Ara icons
    public static event System.Action<int>      OnLivesChanged;   // (newLives)
    public static event System.Action<int, int> OnDamageTaken;    // (newLives, prevLives)
    public static event System.Action<int>      OnLivesRestored;  // (newLives)

    // Persists lives across scene changes in memory (not on disk).
    // Prevents lives from resetting to 5 when loading a new scene.
    private static int   _persistedLives     = -1;
    private static float _persistedTimestamp = -1f;
    private const  float PersistWindow       = 15f;  // max seconds between OnDestroy and next Start

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        // Save lives in memory and on disk before the scene unloads.
        _persistedLives     = _lives;
        _persistedTimestamp = Time.realtimeSinceStartup;
        if (_lives > 0) PersistHealth();
        Instance = null;
    }

    void Start()
    {
        // Use in-memory lives if they come from a recent scene change.
        float age = Time.realtimeSinceStartup - _persistedTimestamp;
        if (_persistedLives >= 0 && age < PersistWindow)
        {
            _lives              = _persistedLives;
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

        // Notify HUD immediately — avoids Ara icons appearing as ash because
        // OnSceneLoaded in PlayerHUD fires BEFORE Start() initializes _lives.
        OnLivesChanged?.Invoke(_lives);

        _player = FindObjectOfType<PlayerController>();
        if (_player != null)
        {
            _lastSafePos = _player.transform.position;
            _playerSR    = _player.GetComponent<SpriteRenderer>();
            _playerAnim  = _player.GetComponent<PlayerAnimator>();

            // Ensure the player is active and visible when entering a scene
            // (may arrive deactivated if a scene change interrupted a respawn).
            _player.enabled = true;
            if (_playerAnim != null) _playerAnim.enabled = true;
            if (_playerSR   != null) _playerSR.enabled   = true;
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

    // ── Public API ────────────────────────────────────────────────────────────

    // Called by CrystalHazard — ignores _isBlinking (crystals are always lethal)
    public void TriggerHazard()
    {
        if (_isRespawning || _player == null) return;
        StartCoroutine(RespawnCoroutine(1));
    }

    // Called by boss attacks — respects _isBlinking (post-respawn window).
    // No fade or teleport: only hurt animation + invulnerability blink.
    // If lives reach 0, executes full death + fade to sanctuary.
    public void TakeBossDamage(int livesLost)
    {
        if (_isRespawning || _isBlinking || _player == null) return;
        StartCoroutine(BossDamageCoroutine(livesLost));
    }

    // Called by SanctuaryFlame when the player rests
    public void RestoreLives()
    {
        _lives = defaultLives;
        PersistHealth();
        OnLivesRestored?.Invoke(_lives);
        OnLivesChanged?.Invoke(_lives);
    }

    // Static version: works even when the scene has no CrystalRespawnManager instance.
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

    // ── Main respawn coroutine ────────────────────────────────────────────────

    private IEnumerator RespawnCoroutine(int livesLost)
    {
        _isRespawning = true;

        int livesAfter = Mathf.Max(0, _lives - livesLost);
        var rb = _player.GetComponent<Rigidbody2D>();

        if (livesAfter <= 0)
        {
            yield return StartCoroutine(ExecuteDeathSequence(rb));
            yield break;
        }

        // Damage flow: hurt → wait full animation → fade → teleport → blink
        // Notify HUD BEFORE the fade so the Ara animation is visible
        OnDamageTaken?.Invoke(livesAfter, _lives);

        _player?.PlayHurtSound();

        if (_playerAnim == null && _player != null)
            _playerAnim = _player.GetComponent<PlayerAnimator>();

        _playerAnim?.TriggerHurt();

        // Wait for hurt animation (7 frames @ 12fps = 0.583s)
        yield return new WaitForSeconds(HurtAnimDuration);

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

    // ── Post-respawn blink ────────────────────────────────────────────────────

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

    // ── Boss damage (no teleport) ─────────────────────────────────────────────
    // Lives > 0: hurt animation + blink only, player stays active.
    // Lives = 0: full death sequence with fade to sanctuary.

    private IEnumerator BossDamageCoroutine(int livesLost)
    {
        _isRespawning = true;

        int livesAfter = Mathf.Max(0, _lives - livesLost);
        var rb = _player.GetComponent<Rigidbody2D>();

        if (livesAfter <= 0)
        {
            yield return StartCoroutine(ExecuteDeathSequence(rb));
            yield break;
        }

        // Normal damage: HUD + hurt animation. Player remains active.
        OnDamageTaken?.Invoke(livesAfter, _lives);
        _lives = livesAfter;
        PersistHealth();

        if (_playerAnim == null && _player != null)
            _playerAnim = _player.GetComponent<PlayerAnimator>();
        _playerAnim?.TriggerHurt();
        _player?.PlayHurtSound();

        yield return new WaitForSeconds(HurtAnimDuration);

        OnLivesChanged?.Invoke(_lives);

        _isRespawning = false;
        _isBlinking   = true;
        if (_playerSR == null && _player != null)
            _playerSR = _player.GetComponent<SpriteRenderer>();
        yield return StartCoroutine(BlinkCoroutine());
        _isBlinking = false;
    }

    // ── Shared death sequence (crystals and boss both call this) ─────────────

    private IEnumerator ExecuteDeathSequence(Rigidbody2D rb)
    {
        OnDamageTaken?.Invoke(0, _lives);

        if (_playerAnim == null && _player != null)
            _playerAnim = _player.GetComponent<PlayerAnimator>();
        _playerAnim?.TriggerDie();
        // Disable PlayerAnimator so its Update() doesn't overwrite Animator bools
        // and interrupt the death animation.
        if (_playerAnim != null) _playerAnim.enabled = false;

        _player.enabled = false;
        if (rb != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }

        yield return new WaitForSeconds(deathAnimDuration);

        if (SceneFader.Instance != null) yield return SceneFader.Instance.FadeOutAsync();
        else yield return new WaitForSeconds(0.4f);

        _lives = 0;
        PersistHealth();

        string sanctScene = PlayerPrefs.GetString(EldoriaPrefsKeys.SanctuaryScene, "");
        if (!string.IsNullOrEmpty(sanctScene))
        {
            _lives = defaultLives;
            PersistHealth();
            // Notify HUD before scene change — the canvas is DDOL so Ara icons
            // will appear alive when the new scene loads.
            OnLivesRestored?.Invoke(_lives);
            OnLivesChanged?.Invoke(_lives);

            float sx = PlayerPrefs.GetFloat(EldoriaPrefsKeys.SanctuaryX, 0f);
            float sy = PlayerPrefs.GetFloat(EldoriaPrefsKeys.SanctuaryY, 0f);
            PlayerSpawnManager.UsePositionOverride   = true;
            PlayerSpawnManager.OverridePositionValue = new Vector2(sx, sy);

            // FadeOutAsync left _isFading=true; use LoadSceneAfterFade to bypass the guard.
            if (SceneFader.Instance != null) SceneFader.Instance.LoadSceneAfterFade(sanctScene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(sanctScene);
        }
        else
        {
            // No sanctuary saved: return to the biome start.
            string fallback = FallbackScene();
            _lives = defaultLives;
            PersistHealth();
            OnLivesRestored?.Invoke(_lives);
            OnLivesChanged?.Invoke(_lives);
            PlayerSpawnManager.NextSpawnId = "default";
            if (SceneFader.Instance != null) SceneFader.Instance.LoadSceneAfterFade(fallback);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(fallback);
        }
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    private void PersistHealth()
    {
        if (SaveManager.Instance == null || SaveManager.ActiveSlot < 0) return;
        var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
        data.health = _lives;
        SaveManager.Instance.Save(SaveManager.ActiveSlot, data);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Fallback scene when the player dies without a saved sanctuary.
    // HV* scenes → return to Kael's house. MTN* → return to the mountain exterior.
    private static string FallbackScene()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene.StartsWith("HV") || scene == "Intro" || scene == "MainMenu")
            return EldoriaSceneNames.HV01_Interior;
        return EldoriaSceneNames.MTN01_Exterior;
    }

    // Call from SlotsScreenManager when starting/continuing a save to prevent
    // lives from a previous session contaminating the new one.
    public static void InvalidatePersistedLives()
    {
        _persistedLives     = -1;
        _persistedTimestamp = -1f;
    }
}
