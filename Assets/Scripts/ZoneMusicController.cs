using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton DontDestroyOnLoad that controls background music by zone.
// Pattern: Observer (listens to SceneManager.sceneLoaded and boss events).
//
// Flow:
//   HV* → Celestial Kingdom
//   MTN01–MTN09 → Enchanted Ruins
//   PreMTN10 → stop music, start cave ambience (loop)
//   MTN10 → silence until boss enters Phase1
//   Boss Phase1 → Mountain Storm (boss music)
//   Boss dead → stop
public class ZoneMusicController : MonoBehaviour
{
    public static ZoneMusicController Instance { get; private set; }

    private ZoneMusicConfig _config;
    private bool _bossActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[ZoneMusicController]");
        go.AddComponent<ZoneMusicController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _config = Resources.Load<ZoneMusicConfig>("ZoneMusicConfig");
        SceneManager.sceneLoaded    += OnSceneLoaded;
        BossObsesion.OnPhaseChanged += OnBossPhaseChanged;
        BossObsesion.OnBossDead     += OnBossDead;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded    -= OnSceneLoaded;
        BossObsesion.OnPhaseChanged -= OnBossPhaseChanged;
        BossObsesion.OnBossDead     -= OnBossDead;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _bossActive = false;
        ApplyZoneMusic(scene.name);
    }

    void ApplyZoneMusic(string sceneName)
    {
        if (_config == null || AudioManager.Instance == null) return;

        if (sceneName.StartsWith("HV") && sceneName != EldoriaSceneNames.HV07)
        {
            AudioManager.Instance.PlayMusic(_config.hvMusic);
        }
        else if (sceneName == EldoriaSceneNames.HV07)
        {
            AudioManager.Instance.StopMusic();  // silent corridor between Hub and Mountains
        }
        else if (sceneName.StartsWith("MTN") && sceneName != EldoriaSceneNames.MTN10)
        {
            AudioManager.Instance.PlayMusic(_config.mtnMusic);
        }
        else if (sceneName == EldoriaSceneNames.PreMTN10)
        {
            AudioManager.Instance.PlayMusic(_config.caveAmbience);
        }
        else if (sceneName == EldoriaSceneNames.MTN10)
        {
            AudioManager.Instance.StopMusic();
        }
        else if (sceneName == EldoriaSceneNames.MainMenu || sceneName == EldoriaSceneNames.SlotsScreen)
        {
            AudioManager.Instance.PlayMusic(_config.menuMusic);
        }
        else if (sceneName == EldoriaSceneNames.Intro)
        {
            AudioManager.Instance.StopMusic();
        }
        // Settings does not change music — inherits whatever is currently playing
    }

    void OnBossPhaseChanged(BossObsesion.BossPhase phase)
    {
        if (_bossActive) return;
        if (phase != BossObsesion.BossPhase.Phase1) return;

        _bossActive = true;
        if (_config != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(_config.bossMusic);
    }

    void OnBossDead()
    {
        _bossActive = false;
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();
    }
}
