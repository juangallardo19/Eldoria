using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Singleton DontDestroyOnLoad que gestiona el mapa de mundo.
// Patrón: Singleton + Observer + State
//
// Tecla M     → abre/cierra. Pausa el juego y oscurece la pantalla.
// Tecla Tab   → (con mapa abierto) alterna entre tab HUB y MONTAÑAS.
// SceneLoaded → marca zona visitada, actualiza sprites, cambia al tab correcto.
public class WorldMapController : MonoBehaviour
{
    public static WorldMapController Instance { get; private set; }
    public bool IsOpen => _mapOpen;

    [Header("Referencias (cableadas por SetupWorldMap)")]
    [SerializeField] private GameObject mapCanvas;
    [SerializeField] private GameObject hubContainer;
    [SerializeField] private GameObject mtnContainer;
    [SerializeField] private TMP_Text   currentZoneLabel;

    // Zonas visibles desde el inicio (tutorial — no requieren ser visitadas)
    private static readonly string[] PreDiscoveredZones = { "HUB01","HUB02","HUB03","HUB04","HUB05","HUB06" };

    private WorldMapSection[] _sections;
    private WorldMapLine[]    _lines;
    private enum TabState { Hub, Mtn, Overview }

    private string   _currentZone   = "";
    private bool     _mapOpen       = false;
    private TabState _tabState      = TabState.Hub;
    private bool     _wasTimePaused = false;
    private float    _pulseTimer    = 0f;

    // ── Mapeo escena → zoneId ────────────────────────────────────────────
    private static readonly Dictionary<string, string> SceneToZone = new()
    {
        { "HV01_Interior",    "HUB01" },
        { "HV01_Exterior",    "HUB01" },
        { "HV02_PlazaCentral","HUB02" },
        { "HV04",             "HUB04" },
        { "HV05",             "HUB05" },
        { "HV06",             "HUB06" },
        { "HV07",             "HUB07" },
        { "MTN01_Exterior",   "MTN01" },
        { "MTN01_Interior",   "MTN01" },
        { "MTN02",            "MTN02" },
        { "MTN03",            "MTN03" },
        { "MTN04",            "MTN04" },
        { "MTN05",            "MTN05" },
        { "MTN06",            "MTN06" },
        { "MTN07",            "MTN07" },
        { "MTN08",            "MTN08" },
        { "MTN09",            "MTN09" },
        { "PreMTN10",         "MTN10" },
        { "MTN10",            "MTN10" },
        { "MTN11",            "MTN11" },
        { "MTN12",            "MTN12" },
    };

    private static readonly Dictionary<string, string> ZoneDisplayNames = new()
    {
        { "HUB01", "Casa de Kael"            },
        { "HUB02", "Plaza Central"           },
        { "HUB03", "???"                     },
        { "HUB04", "Zona A"                  },
        { "HUB05", "Zona B"                  },
        { "HUB06", "Zona C"                  },
        { "HUB07", "Camino a las Montañas"   },
        { "MTN01", "Afueras de las Montañas" },
        { "MTN02", "Ruinas de las Laderas"   },
        { "MTN03", "La Bifurcación"          },
        { "MTN04", "Boca de las Cuevas"      },
        { "MTN05", "Galería de Cristal"      },
        { "MTN06", "Laboratorio en Ruinas"   },
        { "MTN07", "Zona Bloqueada"          },
        { "MTN08", "Cruce de Vetas"          },
        { "MTN09", "Antesala del Boss"       },
        { "MTN10", "Sala del Boss"           },
        { "MTN11", "???"                     },
        { "MTN12", "???"                     },
    };

    // ── Ciclo de vida ─────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Fallback: buscar por nombre si la referencia serializada es nula
        if (mapCanvas == null)
            mapCanvas = transform.Find("MapCanvas")?.gameObject;
        if (mapCanvas != null)
        {
            if (hubContainer == null)
                hubContainer = mapCanvas.transform.Find("HubContainer")?.gameObject;
            if (mtnContainer == null)
                mtnContainer = mapCanvas.transform.Find("MtnContainer")?.gameObject;
            if (currentZoneLabel == null)
                currentZoneLabel = mapCanvas.transform.Find("ZoneName")?.GetComponent<TMP_Text>();
        }

        // Las zonas de tutorial siempre están visibles desde el primer arranque
        foreach (var z in PreDiscoveredZones) MarkVisited(z);

        _sections = GetComponentsInChildren<WorldMapSection>(true);
        _lines    = GetComponentsInChildren<WorldMapLine>(true);
        if (mapCanvas != null) mapCanvas.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneToZone.TryGetValue(scene.name, out var zoneId))
        {
            _currentZone = zoneId;
            MarkVisited(zoneId);
        }
        if (_mapOpen) SetMapOpen(false);
    }

    void Update()
    {
        var mapKey = KeyRebindUI.GetKey("MapOpen", KeyCode.M);

        if (Input.GetKeyDown(mapKey))
            SetMapOpen(!_mapOpen);

        // Tab alterna entre sección HUB y sección MTN mientras el mapa está abierto
        if (_mapOpen && Input.GetKeyDown(KeyCode.Tab))
            SwitchTab();

        if (_mapOpen)
        {
            _pulseTimer += Time.unscaledDeltaTime * 0.45f;
            float sinVal  = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * Mathf.PI * 2f); // 0→1
            float scale   = 1f + 0.04f * sinVal;   // +4% en el pico
            float tintAmt = 0.45f * sinVal;         // 45% tinte azul en el pico
            UpdateCurrentPulse(scale, tintAmt);
        }
    }

    // ── Abrir / cerrar ────────────────────────────────────────────────────
    void SetMapOpen(bool open)
    {
        _mapOpen = open;

        if (open)
        {
            _wasTimePaused = Mathf.Approximately(Time.timeScale, 0f);
            if (!_wasTimePaused) Time.timeScale = 0f;

            if (mapCanvas != null) mapCanvas.SetActive(true);
            _pulseTimer = 0f;
            SetTabForCurrentZone();
            RefreshAll();
        }
        else
        {
            RestoreFullscreen(hubContainer);
            RestoreFullscreen(mtnContainer);
            if (!_wasTimePaused) Time.timeScale = 1f;
            if (mapCanvas != null) mapCanvas.SetActive(false);
        }
    }

    // ── Tabs: Hub → MTN → Vista general (ambos lado a lado) → Hub… ──────
    public void SwitchTab()
    {
        _tabState = _tabState switch
        {
            TabState.Hub      => TabState.Mtn,
            TabState.Mtn      => TabState.Overview,
            TabState.Overview => TabState.Hub,
            _                 => TabState.Hub,
        };
        ApplyTab();
    }

    public void ShowHubTab() { _tabState = TabState.Hub; ApplyTab(); }
    public void ShowMtnTab() { _tabState = TabState.Mtn; ApplyTab(); }

    void SetTabForCurrentZone()
    {
        _tabState = (!string.IsNullOrEmpty(_currentZone) && _currentZone.StartsWith("MTN"))
            ? TabState.Mtn : TabState.Hub;
        ApplyTab();
    }

    void ApplyTab()
    {
        switch (_tabState)
        {
            case TabState.Hub:
                RestoreFullscreen(hubContainer);
                hubContainer?.SetActive(true);
                mtnContainer?.SetActive(false);
                break;
            case TabState.Mtn:
                RestoreFullscreen(mtnContainer);
                mtnContainer?.SetActive(true);
                hubContainer?.SetActive(false);
                break;
            case TabState.Overview:
                hubContainer?.SetActive(true);
                mtnContainer?.SetActive(true);
                SetHalfScreen(mtnContainer, rightSide: false);   // MTN izquierda
                SetHalfScreen(hubContainer,  rightSide: true);   // HUB derecha
                break;
        }
    }

    // Ocupa la mitad izquierda o derecha del canvas
    void SetHalfScreen(GameObject container, bool rightSide)
    {
        if (container == null) return;
        var rt = container.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = rightSide ? new Vector2(0.5f, 0f) : Vector2.zero;
        rt.anchorMax = rightSide ? Vector2.one            : new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(6f,  6f);
        rt.offsetMax = new Vector2(-6f, -6f);
    }

    // Restaura el container a pantalla completa (usado al volver de Overview o al cerrar)
    void RestoreFullscreen(GameObject container)
    {
        if (container == null) return;
        var rt = container.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ── Actualizar visibilidad y sprites ──────────────────────────────────
    void RefreshAll()
    {
        if (_sections == null) _sections = GetComponentsInChildren<WorldMapSection>(true);
        if (_lines    == null) _lines    = GetComponentsInChildren<WorldMapLine>(true);

        // Secciones: mostrar solo si la zona fue descubierta; siempre NormalState
        foreach (var sec in _sections)
        {
            if (sec == null) continue;
            if (sec.sectionImage == null) sec.sectionImage = sec.GetComponent<UnityEngine.UI.Image>();

            bool discovered = IsVisited(sec.zoneId);
            sec.gameObject.SetActive(discovered);
            if (!discovered) continue;

            if (sec.sectionImage != null)
            {
                if (sec.normalSprite != null) sec.sectionImage.sprite = sec.normalSprite;
                sec.sectionImage.color = Color.white;
            }
            sec.transform.localScale = Vector3.one;
        }

        // Líneas con WorldMapLine: visibles solo si AMBAS zonas conectadas están descubiertas
        foreach (var line in _lines)
        {
            if (line == null) continue;
            line.gameObject.SetActive(IsVisited(line.zoneIdA) && IsVisited(line.zoneIdB));
        }
        // Líneas sin WorldMapLine (extras no cableadas): siempre ocultas
        HideUnwiredLines(hubContainer);
        HideUnwiredLines(mtnContainer);

        if (currentZoneLabel != null)
        {
            currentZoneLabel.text = (!string.IsNullOrEmpty(_currentZone) &&
                ZoneDisplayNames.TryGetValue(_currentZone, out var name)) ? name : "";
        }
    }

    // Oculta hijos directos del container que no son secciones ni tienen WorldMapLine
    void HideUnwiredLines(GameObject container)
    {
        if (container == null) return;
        var t = container.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            if (child.GetComponent<WorldMapSection>() != null) continue;
            if (child.GetComponent<WorldMapLine>()    == null)
                child.gameObject.SetActive(false);
        }
    }

    // Pulse suave en la zona actual: escala +4% y tinte azul 45%
    void UpdateCurrentPulse(float scale, float tintAmt)
    {
        if (_sections == null) return;
        foreach (var sec in _sections)
        {
            if (sec == null || !sec.gameObject.activeSelf) continue;
            if (sec.zoneId == _currentZone)
            {
                sec.transform.localScale = Vector3.one * scale;
                if (sec.sectionImage != null)
                    sec.sectionImage.color = Color.Lerp(Color.white, new Color(0.5f, 0.8f, 1f, 1f), tintAmt);
            }
            else
            {
                sec.transform.localScale = Vector3.one;
                if (sec.sectionImage != null) sec.sectionImage.color = Color.white;
            }
        }
    }

    // ── PlayerPrefs ───────────────────────────────────────────────────────
    public static void MarkVisited(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId)) return;
        PlayerPrefs.SetInt("MapVisited_" + zoneId, 1);
        PlayerPrefs.Save();
    }

    public static bool IsVisited(string zoneId) =>
        PlayerPrefs.GetInt("MapVisited_" + zoneId, 0) == 1;

    public static void ClearAllVisited()
    {
        foreach (var id in new[]
        {
            "HUB01","HUB02","HUB03","HUB04","HUB05","HUB06","HUB07",
            "MTN01","MTN02","MTN03","MTN04","MTN05","MTN06",
            "MTN07","MTN08","MTN09","MTN10","MTN11","MTN12"
        })
            PlayerPrefs.DeleteKey("MapVisited_" + id);
        PlayerPrefs.Save();
    }
}
