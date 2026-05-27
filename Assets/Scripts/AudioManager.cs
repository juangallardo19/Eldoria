using UnityEngine;

// Singleton — persists across scenes (DontDestroyOnLoad).
// Manages music and SFX volume. Each scene decides whether to call PlayMusic() or StopMusic().
// If musicSource/sfxSource are unassigned in the Inspector, auto-detected by child order
// (first AudioSource = music, second = SFX).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources (auto-detected if left empty)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var sources = GetComponentsInChildren<AudioSource>();
        if (musicSource == null && sources.Length > 0) musicSource = sources[0];
        if (sfxSource   == null && sources.Length > 1) sfxSource   = sources[1];

        // AudioManager reads volume but never writes to PlayerPrefs — that belongs to SettingsManager.
        float master = PlayerPrefs.GetFloat(EldoriaPrefsKeys.MasterVolume, 1f);
        if (musicSource != null) { musicSource.playOnAwake = false; musicSource.volume = PlayerPrefs.GetFloat(EldoriaPrefsKeys.MusicVolume, 1f) * master; }
        if (sfxSource   != null) { sfxSource  .playOnAwake = false; sfxSource  .volume = PlayerPrefs.GetFloat(EldoriaPrefsKeys.SFXVolume,   1f) * master; }
    }

    // ── Music control ─────────────────────────────────────────────────────────
    public void PlayMusic(AudioClip clip = null)
    {
        if (musicSource == null) return;
        if (clip == null) return;
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
        musicSource.clip = null;
    }

    public void PauseMusic()  { if (musicSource != null) musicSource.Pause(); }
    public void ResumeMusic() { if (musicSource != null) musicSource.UnPause(); }

    // Fades out the music gradually — useful after boss death so music disappears smoothly.
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
            elapsed            += Time.deltaTime;
            musicSource.volume  = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = startVol;  // restore for next track
    }

    // ── Volume control ────────────────────────────────────────────────────────
    public void SetMusicVolume(float volume) { if (musicSource != null) musicSource.volume = volume; }
    public void SetSFXVolume(float volume)   { if (sfxSource   != null) sfxSource  .volume = volume; }

    public float GetMusicVolume() => musicSource != null ? musicSource.volume : PlayerPrefs.GetFloat(EldoriaPrefsKeys.MusicVolume, 1f);
    public float GetSFXVolume()   => sfxSource   != null ? sfxSource  .volume : PlayerPrefs.GetFloat(EldoriaPrefsKeys.SFXVolume,   1f);

    public void PlaySFX(AudioClip clip) { if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip); }
}
