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
        if (clip != null && musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
        }
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void StopMusic()   { if (musicSource != null) musicSource.Stop(); }
    public void PauseMusic()  { if (musicSource != null) musicSource.Pause(); }
    public void ResumeMusic() { if (musicSource != null) musicSource.UnPause(); }

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
