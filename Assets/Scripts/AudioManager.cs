using UnityEngine;

// Singleton — persiste entre escenas (DontDestroyOnLoad).
// Gestiona volumen de música y SFX. Cada escena decide si llama PlayMusic() o StopMusic().
// Si musicSource/sfxSource no están asignados en el inspector, se auto-detectan por orden
// entre los AudioSource hijos del GameObject (primero=música, segundo=SFX).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources (se auto-detectan si quedan vacíos)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey   = "SFXVolume";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-detección: si el inspector dejó los campos vacíos los llenamos aquí
        var sources = GetComponentsInChildren<AudioSource>();
        if (musicSource == null && sources.Length > 0) musicSource = sources[0];
        if (sfxSource   == null && sources.Length > 1) sfxSource   = sources[1];

        // Aplica master * canal para inicializar — AudioManager NO escribe en PlayerPrefs,
        // esa responsabilidad pertenece a SettingsManager.
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (musicSource != null) { musicSource.playOnAwake = false; musicSource.volume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f) * master; }
        if (sfxSource   != null) { sfxSource  .playOnAwake = false; sfxSource  .volume = PlayerPrefs.GetFloat(SFXVolumeKey,   1f) * master; }
    }

    // ── Control de música ─────────────────────────────────────────────────
    public void PlayMusic(AudioClip clip = null)
    {
        if (musicSource == null) return;
        if (clip == null) return;   // sin clip explícito: no reproducir basura del frame anterior
        if (musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
        }
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = null;   // limpia el clip para que Play() futuro no reanude música vieja
    }
    public void PauseMusic()  { if (musicSource != null) musicSource.Pause(); }
    public void ResumeMusic() { if (musicSource != null) musicSource.UnPause(); }

    // Fade out progresivo — útil tras muerte del boss para que la música desaparezca suavemente.
    public void FadeOutMusic(float duration = 1.5f)
    {
        if (musicSource == null || !musicSource.isPlaying) return;
        StartCoroutine(FadeOutRoutine(duration));
    }

    private System.Collections.IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = musicSource.volume;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed             += Time.deltaTime;
            musicSource.volume   = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = startVol;   // restaura para la siguiente canción
    }

    // ── Control de volumen ────────────────────────────────────────────────
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = volume;
    }

    public float GetMusicVolume() => musicSource != null ? musicSource.volume : PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    public float GetSFXVolume()   => sfxSource   != null ? sfxSource  .volume : PlayerPrefs.GetFloat(SFXVolumeKey,   1f);

    public void PlaySFX(AudioClip clip) { if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip); }
}
