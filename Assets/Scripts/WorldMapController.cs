using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Singleton DontDestroyOnLoad that manages the world map.
// Pattern: Singleton + Observer + State
//
// Key M     → open/close. Pauses the game and dims the screen.
// Key Tab   → (map open) toggles between HUB and MOUNTAINS tabs (two views only).
// SceneLoaded → marks zone as visited, updates sprites, switches to the correct tab.
public class WorldMapController : MonoBehaviour
{
    public static WorldMapController Instance { get; private set; }
    public bool IsOpen => _mapOpen;

    [Header("References (wired by SetupWorldMap)")]
    [SerializeField] private GameObject mapCanvas;
    [SerializeField] private GameObject hubContainer;
    [SerializeField] private GameObject mtnContainer;
    [SerializeField] private TMP_Text   currentZoneLabel;

    // Zones visible from the start (tutorial — do not need to be visited first)
    private static readonly string[] PreDiscoveredZones = { "HUB01","HUB02","HUB03","HUB04","HUB05","HUB06" };

    private WorldMapSection[] _sections;
    private WorldMapLine[]    _lines;
    private enum TabState { Hub, Mtn }

    private string   _currentZone   = "";
    private bool     _mapOpen       = false;
    private TabState _tabState      = TabState.Hub;
    private bool     _wasTimePaused = false;
    private float    _pulseTimer    = 0f;

    // Tutorial: target zone with yellow pulse + "!"
    private string    _tutorialObjZone  = null;
    private Transform _tutorialMarkerTr = null;
    private float     _tutorialPulseT   = 0f;

    public void SetTutorialObjective(string zoneId) { _tutorialObjZone = zoneId; _tutorialPulseT = 0f; }
    public void ClearTutorialObjective() { _tutorialObjZone = null; HideTutorialMarker(); }

    // ── Scene → zoneId mapping ───────────────────────────────────────────
    private static readonly Dictionary<string, string> SceneToZone = new()
    {
        { EldoriaSceneNames.HV01_Interior,     "HUB01" },
        { EldoriaSceneNames.HV01_Exterior,     "HUB01" },
        { EldoriaSceneNames.HV02_PlazaCentral, "HUB02" },
        { EldoriaSceneNames.HV04,              "HUB04" },
        { EldoriaSceneNames.HV05,              "HUB05" },
        { EldoriaSceneNames.HV06,              "HUB06" },
        { EldoriaSceneNames.HV07,              "HUB07" },
        { EldoriaSceneNames.MTN01_Exterior,    "MTN01" },
        { EldoriaSceneNames.MTN01_Interior,    "MTN01" },
        { EldoriaSceneNames.MTN02,             "MTN02" },
        { EldoriaSceneNames.MTN03,             "MTN03" },
        { EldoriaSceneNames.MTN04,             "MTN04" },
        { EldoriaSceneNames.MTN05,             "MTN05" },
        { EldoriaSceneNames.MTN06,             "MTN06" },
        { EldoriaSceneNames.MTN07,             "MTN07" },
        { EldoriaSceneNames.MTN08,             "MTN08" },
        { EldoriaSceneNames.MTN09,             "MTN09" },
        { EldoriaSceneNames.PreMTN10,          "MTN10" },
        { EldoriaSceneNames.MTN10,             "MTN10" },
        { EldoriaSceneNames.PreMTN11,          "MTN11" },
        { EldoriaSceneNames.MTN11,             "MTN11" },
        { EldoriaSceneNames.MTN12,             "MTN12" },
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

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Fallback: search by name if the serialized reference is null
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

        // Tutorial zones are always visible from the very first launch
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

        // Tab toggles between HUB and MTN sections while the map is open
        if (_mapOpen && Input.GetKeyDown(KeyCode.Tab))
            SwitchTab();

        if (_mapOpen)
        {
            _pulseTimer += Time.unscaledDeltaTime * 0.45f;
            float sinVal  = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * Mathf.PI * 2f);
            float scale   = 1f + 0.04f * sinVal;
            float tintAmt = 0.45f * sinVal;
            UpdateCurrentPulse(scale, tintAmt);

            // Yellow pulse for tutorial target zone
            if (!string.IsNullOrEmpty(_tutorialObjZone))
            {
                _tutorialPulseT += Time.unscaledDeltaTime;
                float tSin = 0.5f + 0.5f * Mathf.Sin(_tutorialPulseT * Mathf.PI * 3f);
                EnsureTutorialMarker();
                UpdateTutorialObjPulse(tSin);
            }
        }
    }

    // ── Open / close ──────────────────────────────────────────────────────
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
            HideTutorialMarker();
            _tutorialPulseT = 0f;
            RestoreFullscreen(hubContainer);
            RestoreFullscreen(mtnContainer);
            if (!_wasTimePaused) Time.timeScale = 1f;
            if (mapCanvas != null) mapCanvas.SetActive(false);
        }
    }

    // ── Tabs: Hub ↔ MTN ──────────────────────────────────────────────────
    public void SwitchTab()
    {
        _tabState = _tabState switch
        {
            TabState.Hub => TabState.Mtn,
            TabState.Mtn => TabState.Hub,
            _            => TabState.Hub,
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
        }
    }

    // Restores the container to full-screen when closing the map
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

    // ── Update visibility and sprites ────────────────────────────────────
    void RefreshAll()
    {
        if (_sections == null) _sections = GetComponentsInChildren<WorldMapSection>(true);
        if (_lines    == null) _lines    = GetComponentsInChildren<WorldMapLine>(true);

        // Sections: show only if the zone was discovered; always NormalState
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

        // Lines with WorldMapLine: visible only if BOTH connected zones are discovered
        foreach (var line in _lines)
        {
            if (line == null) continue;
            line.gameObject.SetActive(IsVisited(line.zoneIdA) && IsVisited(line.zoneIdB));
        }
        // Lines without WorldMapLine (unwired extras): always hidden
        HideUnwiredLines(hubContainer);
        HideUnwiredLines(mtnContainer);

        if (currentZoneLabel != null)
        {
            currentZoneLabel.text = (!string.IsNullOrEmpty(_currentZone) &&
                ZoneDisplayNames.TryGetValue(_currentZone, out var name)) ? name : "";
        }
    }

    // Hides direct children of the container that are neither sections nor have WorldMapLine
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

    // Soft pulse on current zone: +4% scale and 45% blue tint
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
        PlayerPrefs.SetInt(EldoriaPrefsKeys.MapVisitedPrefix + zoneId, 1);
        PlayerPrefs.Save();
    }

    public static bool IsVisited(string zoneId) =>
        PlayerPrefs.GetInt(EldoriaPrefsKeys.MapVisitedPrefix + zoneId, 0) == 1;

    public static void ClearAllVisited()
    {
        foreach (var id in new[]
        {
            "HUB01","HUB02","HUB03","HUB04","HUB05","HUB06","HUB07",
            "MTN01","MTN02","MTN03","MTN04","MTN05","MTN06",
            "MTN07","MTN08","MTN09","MTN10","MTN11","MTN12"
        })
            PlayerPrefs.DeleteKey(EldoriaPrefsKeys.MapVisitedPrefix + id);

        foreach (var z in PreDiscoveredZones) MarkVisited(z);
        PlayerPrefs.Save();
    }

    // ── Tutorial: target zone with yellow pulse and "!" ──────────────────

    void EnsureTutorialMarker()
    {
        if (string.IsNullOrEmpty(_tutorialObjZone)) return;
        if (_sections == null) _sections = GetComponentsInChildren<WorldMapSection>(true);

        foreach (var sec in _sections)
        {
            if (sec == null || sec.zoneId != _tutorialObjZone || !sec.gameObject.activeSelf) continue;

            var existing = sec.transform.Find("[!TutObj]");
            if (existing == null)
            {
                var go  = new GameObject("[!TutObj]");
                go.transform.SetParent(sec.transform, false);
                var rt  = go.AddComponent<RectTransform>();
                var secRt = sec.GetComponent<RectTransform>();
                float h = secRt != null ? secRt.rect.height * 0.5f + 14f : 18f;
                rt.anchoredPosition = new Vector2(0f, h);
                rt.sizeDelta        = new Vector2(28f, 28f);
                var txt       = go.AddComponent<TMPro.TextMeshProUGUI>();
                txt.text      = "!";
                txt.fontSize  = 26f;
                txt.fontStyle = TMPro.FontStyles.Bold;
                txt.color     = Color.yellow;
                txt.alignment = TMPro.TextAlignmentOptions.Center;
#if UNITY_EDITOR
                var tutFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                    "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
                var tutFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
                if (tutFont != null) txt.font = tutFont;
                existing = go.transform;
            }
            existing.gameObject.SetActive(true);
            _tutorialMarkerTr = existing;
            return;
        }
    }

    void HideTutorialMarker()
    {
        if (_tutorialMarkerTr != null) _tutorialMarkerTr.gameObject.SetActive(false);
    }

    void UpdateTutorialObjPulse(float t)
    {
        if (_sections == null) return;
        foreach (var sec in _sections)
        {
            if (sec == null || !sec.gameObject.activeSelf || sec.zoneId != _tutorialObjZone) continue;
            // +7% scale and yellow tint
            sec.transform.localScale = Vector3.one * (1f + 0.07f * t);
            if (sec.sectionImage != null)
                sec.sectionImage.color = Color.Lerp(Color.white, new Color(1f, 0.88f, 0f, 1f), 0.35f + 0.4f * t);
            // "!" pulse
            if (_tutorialMarkerTr != null)
                _tutorialMarkerTr.localScale = Vector3.one * (1f + 0.3f * t);
            return;
        }
    }
}
