using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton DontDestroyOnLoad — manages screen colour effects (brightness/contrast/saturation).
// Patterns: Singleton (global persistent instance) + Observer (reacts to sceneLoaded to attach
//           ScreenColorEffect to the Main Camera of each scene).
//           Facade — exposes Apply() as the sole API; hides Material and Camera management.
public class ScreenEffectsManager : MonoBehaviour
{
    public static ScreenEffectsManager Instance { get; private set; }

    ScreenColorEffect _effect;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start() => AttachToCamera();

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachToCamera();

    // ── Public API ────────────────────────────────────────────────────────
    // Unity slider values (0–1). 0.5 = neutral.
    public static void Apply(float brightness, float contrast, float saturation)
    {
        Instance?.ApplyValues(brightness, contrast, saturation);
    }

    // Called from SettingsManager to ensure the manager exists before the player moves a slider.
    public static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("ScreenEffectsManager");
        go.AddComponent<ScreenEffectsManager>();
    }

    // ── Private ───────────────────────────────────────────────────────────
    void AttachToCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        _effect = cam.GetComponent<ScreenColorEffect>();
        if (_effect == null) _effect = cam.gameObject.AddComponent<ScreenColorEffect>();

        ApplyFromPrefs();
    }

    void ApplyFromPrefs()
    {
        float b = PlayerPrefs.GetFloat(EldoriaPrefsKeys.Brightness, 0.5f);
        float c = PlayerPrefs.GetFloat(EldoriaPrefsKeys.Contrast,   0.5f);
        float s = PlayerPrefs.GetFloat(EldoriaPrefsKeys.Saturation, 0.5f);
        ApplyValues(b, c, s);
    }

    void ApplyValues(float b, float c, float s)
    {
        if (_effect == null) return;
        // Slider (0-1) → shader mapping: 0.5 = neutral → 0; ±0.5 range for brightness/contrast
        _effect.brightness = b - 0.5f;
        _effect.contrast   = c - 0.5f;
        _effect.saturation = (s - 0.5f) * 2f;   // wider range for saturation
    }
}
