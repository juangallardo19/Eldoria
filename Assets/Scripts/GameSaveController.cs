using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton + Observer — persiste entre escenas y gestiona el guardado automático.
// Patrón Observer: se suscribe a SceneManager.sceneLoaded para detectar cambios de zona.
// Patrón State: _tracking indica si el tiempo se está contando (solo en zonas de juego).
public class GameSaveController : MonoBehaviour
{
    public static GameSaveController Instance { get; private set; }

    private bool  _tracking      = false;
    private float _sessionTime   = 0f;
    private float _autosaveTimer = 0f;
    private float _savedBaseTime = 0f;   // playTimeSeconds del save al cargar el slot
    private bool  _baseTimeLoaded = false;
    private const float AUTOSAVE_INTERVAL = 30f;

    // Tiempo total de partida para el slot activo (persistente entre sesiones).
    public float TotalPlayTime => _savedBaseTime + _sessionTime;

    // Mapeo nombre-de-escena → nombre visible al jugador.
    // Añadir aquí cada escena de juego nueva.
    private static readonly Dictionary<string, string> ZoneNames =
        new Dictionary<string, string>
    {
        { "HV01_Interior",     "Casa de Kael"  },
        { "HV01_Exterior",     "Exterior"      },
        { "HV02_PlazaCentral", "Plaza Central" },
        { "HV04",              "Zona A"        },
        { "HV05",              "Zona B"        },
        { "HV06",              "Zona C"        },
        { "HV07",              "Camino a las Montañas" },
        { "MTN01_Exterior",    "Afueras de las Montañas" },
        { "MTN01_Interior",    "Entrada a las Montañas" },
        { "MTN02",             "Ruinas de las Laderas" },
        { "MTN03",             "La Bifurcación" },
        { "MTN04",             "Boca de las Cuevas" },
        { "MTN05",             "Galería de Cristal" },
    };

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Update()
    {
        if (!_tracking || SaveManager.ActiveSlot < 0) return;

        _sessionTime   += Time.deltaTime;
        _autosaveTimer += Time.deltaTime;

        if (_autosaveTimer >= AUTOSAVE_INTERVAL)
        {
            _autosaveTimer = 0f;
            Flush();
        }
    }

    // ── Observer: nueva escena cargada ────────────────────────────────────────
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _tracking = ZoneNames.TryGetValue(scene.name, out string zoneName);
        if (!_tracking || SaveManager.ActiveSlot < 0) return;

        // Carga la base de tiempo guardado una sola vez por sesión de juego.
        if (!_baseTimeLoaded && SaveManager.Instance != null)
        {
            var saved = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            _savedBaseTime  = saved?.playTimeSeconds ?? 0f;
            _baseTimeLoaded = true;
        }

        Flush(zoneName, scene.name);
    }

    // ── Flush: escribe tiempo acumulado en el slot activo ─────────────────────
    private void Flush(string zoneOverride = null, string sceneNameOverride = null)
    {
        if (SaveManager.Instance == null || SaveManager.ActiveSlot < 0) return;

        var data              = SaveManager.Instance.Load(SaveManager.ActiveSlot);
        data.isEmpty          = false;
        data.playTimeSeconds += _sessionTime;
        _savedBaseTime       += _sessionTime;  // mantiene TotalPlayTime coherente sin releer disco
        _sessionTime          = 0f;
        _autosaveTimer        = 0f;

        if (zoneOverride      != null) data.zoneName  = zoneOverride;
        if (sceneNameOverride != null) data.sceneName = sceneNameOverride;

        SaveManager.Instance.Save(SaveManager.ActiveSlot, data);
    }

    // ── API pública ───────────────────────────────────────────────────────────
    public void SaveNow(string zoneName = null) => Flush(zoneName);
}
