using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton DontDestroyOnLoad — gestiona los efectos de color de pantalla (brillo/contraste/saturación).
// Patrón: Singleton (instancia global persistente) + Observer (reacciona a sceneLoaded para
//         adjuntar ScreenColorEffect al Main Camera de cada escena).
// Patrón: Facade — expone Apply() como única API; oculta la gestión del Material y la Camera.
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

    // ── API pública ────────────────────────────────────────────────────────
    // Valores de slider Unity (0-1). 0.5 = neutro.
    public static void Apply(float brightness, float contrast, float saturation)
    {
        Instance?.ApplyValues(brightness, contrast, saturation);
    }

    // Se llama desde SettingsManager para garantizar que el manager exista
    // antes de que el jugador cambie un slider.
    public static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("ScreenEffectsManager");
        go.AddComponent<ScreenEffectsManager>();
    }

    // ── Privados ───────────────────────────────────────────────────────────
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
        float b = PlayerPrefs.GetFloat("Brightness", 0.5f);
        float c = PlayerPrefs.GetFloat("Contrast",   0.5f);
        float s = PlayerPrefs.GetFloat("Saturation", 0.5f);
        ApplyValues(b, c, s);
    }

    void ApplyValues(float b, float c, float s)
    {
        if (_effect == null) return;
        // Mapeo slider (0-1) → shader: 0.5 = neutro → 0; rango ±0.5 para brillo/contraste
        _effect.brightness = b - 0.5f;
        _effect.contrast   = c - 0.5f;
        _effect.saturation = (s - 0.5f) * 2f;   // rango más amplio para saturación
    }
}
