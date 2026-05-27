using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton + Observer — persists across scenes and manages automatic saving.
// Pattern Observer: subscribes to SceneManager.sceneLoaded to detect zone changes.
// Pattern State: _tracking indicates whether play time is being counted (only in game zones).
public class GameSaveController : MonoBehaviour
{
    public static GameSaveController Instance { get; private set; }

    private bool  _tracking       = false;
    private float _sessionTime    = 0f;
    private float _autosaveTimer  = 0f;
    private float _savedBaseTime  = 0f;   // playTimeSeconds from the save at slot load time
    private bool  _baseTimeLoaded = false;
    private const float AutosaveInterval = 30f;

    // Total play time for the active slot (persists across sessions).
    public float TotalPlayTime => _savedBaseTime + _sessionTime;

    // Maps scene name → player-visible zone label (values are game content, kept in Spanish).
    // Add each new game scene here.
    private static readonly Dictionary<string, string> ZoneNames =
        new Dictionary<string, string>
    {
        { EldoriaSceneNames.HV01_Interior,     "Casa de Kael"             },
        { EldoriaSceneNames.HV01_Exterior,     "Exterior"                 },
        { EldoriaSceneNames.HV02_PlazaCentral, "Plaza Central"            },
        { EldoriaSceneNames.HV04,              "Zona A"                   },
        { EldoriaSceneNames.HV05,              "Zona B"                   },
        { EldoriaSceneNames.HV06,              "Zona C"                   },
        { EldoriaSceneNames.HV07,              "Camino a las Montañas"    },
        { EldoriaSceneNames.MTN01_Exterior,    "Afueras de las Montañas"  },
        { EldoriaSceneNames.MTN01_Interior,    "Entrada a las Montañas"   },
        { EldoriaSceneNames.MTN02,             "Ruinas de las Laderas"    },
        { EldoriaSceneNames.MTN03,             "La Bifurcación"           },
        { EldoriaSceneNames.MTN04,             "Boca de las Cuevas"       },
        { EldoriaSceneNames.MTN05,             "Galería de Cristal"       },
        { EldoriaSceneNames.MTN06,             "Laboratorio en Ruinas"    },
        { EldoriaSceneNames.MTN08,             "Cruce de Vetas"           },
        { EldoriaSceneNames.MTN09,             "Antesala del Boss"        },
        { EldoriaSceneNames.PreMTN10,          "Pasillo del Boss"         },
        { EldoriaSceneNames.MTN10,             "Sala de la Obsesión"      },
        { EldoriaSceneNames.PreMTN11,          "Pasillo de las Sombras"   },
        { EldoriaSceneNames.MTN11,             "Caverna Profunda"         },
        { EldoriaSceneNames.MTN12,             "Cueva Final"              },
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

        if (_autosaveTimer >= AutosaveInterval)
        {
            _autosaveTimer = 0f;
            Flush();
        }
    }

    // ── Observer: new scene loaded ────────────────────────────────────────────
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _tracking = ZoneNames.TryGetValue(scene.name, out string zoneName);
        if (!_tracking || SaveManager.ActiveSlot < 0) return;

        // Load the saved base time once per gameplay session.
        if (!_baseTimeLoaded && SaveManager.Instance != null)
        {
            var saved = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            _savedBaseTime  = saved?.playTimeSeconds ?? 0f;
            _baseTimeLoaded = true;
        }

        Flush(zoneName, scene.name);
    }

    // ── Flush: writes accumulated time to the active slot ────────────────────
    private void Flush(string zoneOverride = null, string sceneNameOverride = null)
    {
        if (SaveManager.Instance == null || SaveManager.ActiveSlot < 0) return;

        var data              = SaveManager.Instance.Load(SaveManager.ActiveSlot);
        data.isEmpty          = false;
        data.playTimeSeconds += _sessionTime;
        _savedBaseTime       += _sessionTime;  // keeps TotalPlayTime consistent without re-reading disk
        _sessionTime          = 0f;
        _autosaveTimer        = 0f;

        if (zoneOverride      != null) data.zoneName  = zoneOverride;
        if (sceneNameOverride != null) data.sceneName = sceneNameOverride;

        SaveManager.Instance.Save(SaveManager.ActiveSlot, data);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void SaveNow(string zoneName = null) => Flush(zoneName);

    // Called from StartNewGame to prevent counters from a previous session
    // contaminating the play time or state of the new game.
    public void ResetForNewGame()
    {
        _sessionTime    = 0f;
        _autosaveTimer  = 0f;
        _savedBaseTime  = 0f;
        _baseTimeLoaded = false;
    }
}
