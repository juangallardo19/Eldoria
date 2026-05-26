using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton DontDestroyOnLoad que controla la música de fondo por zona.
// Patrón: Observer (escucha SceneManager.sceneLoaded y eventos del boss).
//
// Flujo:
//   HV* → Celestial Kingdom
//   MTN01–MTN09 → Enchanted Ruins
//   PreMTN10 → para música, inicia cave ambience (loop)
//   MTN10 → silencio hasta que el boss pase a Phase1
//   Boss Phase1 → Mountain Storm (música de boss)
//   Boss muerto → stop
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
        SceneManager.sceneLoaded         += OnSceneLoaded;
        BossObsesion.OnPhaseChanged      += OnBossPhaseChanged;
        BossObsesion.OnBossDead          += OnBossDead;
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

        if (sceneName.StartsWith("HV") && sceneName != "HV07")
        {
            AudioManager.Instance.PlayMusic(_config.hvMusic);
        }
        else if (sceneName == "HV07")
        {
            AudioManager.Instance.StopMusic();   // corredor silencioso entre Hub y Montañas
        }
        else if (sceneName.StartsWith("MTN") && sceneName != "MTN10")
        {
            AudioManager.Instance.PlayMusic(_config.mtnMusic);
        }
        else if (sceneName == "PreMTN10")
        {
            AudioManager.Instance.PlayMusic(_config.caveAmbience);
        }
        else if (sceneName == "MTN10")
        {
            AudioManager.Instance.StopMusic();
        }
        else if (sceneName == "MainMenu" || sceneName == "SlotsScreen")
        {
            AudioManager.Instance.PlayMusic(_config.menuMusic);
        }
        else if (sceneName == "Intro")
        {
            AudioManager.Instance.StopMusic();
        }
        // Settings no cambia la música — hereda lo que esté sonando (Hub, Montañas o menú)
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
