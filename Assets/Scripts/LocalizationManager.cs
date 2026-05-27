using System.Collections.Generic;
using UnityEngine;

// Singleton DontDestroyOnLoad — manages the active language and fires a global change event.
// Patterns:
//   Singleton  — global persistent instance across scenes.
//   Observer   — OnLanguageChanged notifies all LocalizedText components.
//   Flyweight  — shared string tables; not duplicated per object.
public class LocalizationManager : MonoBehaviour
{
    // ── Observer ──────────────────────────────────────────────────────────
    public static event System.Action<string> OnLanguageChanged;

    // ── Singleton ─────────────────────────────────────────────────────────
    public static LocalizationManager Instance { get; private set; }

    public static string CurrentLanguage { get; private set; } = "es";

    // ── Flyweight: shared string tables ───────────────────────────────────
    static readonly Dictionary<string, Dictionary<string, string>> _tables =
        new Dictionary<string, Dictionary<string, string>>
    {
        ["es"] = new Dictionary<string, string>
        {
            // ── Main menu ──────────────────────────────────────────────────
            ["JUGAR"]     = "JUGAR",
            ["OPCIONES"]  = "OPCIONES",
            ["SALIR"]     = "SALIR",
            ["¿Seguro que quieres salir?"] = "¿Seguro que quieres salir?",

            // ── SlotsScreen ────────────────────────────────────────────────
            ["Nueva partida"]     = "Nueva partida",
            ["Continuar"]         = "Continuar",
            ["Borrar"]            = "Borrar",
            ["Vacío"]             = "Vacío",
            ["Confirmar"]         = "Confirmar",
            ["Cancelar"]          = "Cancelar",
            ["¿Borrar partida?"]  = "¿Borrar partida?",

            // ── Tabs and navigation ───────────────────────────────────────
            ["GRÁFICOS"]  = "GRÁFICOS",
            ["SONIDO"]    = "SONIDO",
            ["CONTROLES"] = "CONTROLES",
            ["AJUSTES"]   = "AJUSTES",
            ["VOLVER"]    = "VOLVER",

            // ── Graphics · Display ────────────────────────────────────────
            ["Resolución"]      = "Resolución",
            ["Modo pantalla"]   = "Modo pantalla",
            ["FPS"]             = "FPS",
            ["VSync"]           = "VSync",
            ["Calidad"]         = "Calidad",

            // Display selector options
            ["Pantalla completa"] = "Pantalla completa",
            ["Sin bordes"]        = "Sin bordes",
            ["Ventana"]           = "Ventana",
            ["Sin límite"]        = "Sin límite",
            ["Bajo"]              = "Bajo",
            ["Medio"]             = "Medio",
            ["Alto"]              = "Alto",

            // ── Graphics · Visual ──────────────────────────────────────────
            ["Brillo"]      = "Brillo",
            ["Contraste"]   = "Contraste",
            ["Saturación"]  = "Saturación",

            // ── Graphics · Accessibility ───────────────────────────────────
            ["Daltonismo"]    = "Daltonismo",
            ["Protanopia"]    = "Protanopia",
            ["Deuteranopia"]  = "Deuteranopia",
            ["Tritanopia"]    = "Tritanopia",
            ["Intensidad"]    = "Intensidad",

            // ── Sound ──────────────────────────────────────────────────────
            ["Volumen maestro"]     = "Volumen maestro",
            ["Música"]              = "Música",
            ["Efectos de sonido"]   = "Efectos de sonido",
            ["Voces"]               = "Voces",
            ["Interfaz"]            = "Interfaz",

            // ── Settings ──────────────────────────────────────────────────
            ["Idioma"]         = "Idioma",
            ["Mostrar FPS"]    = "Mostrar FPS",
            ["Español"]        = "Español",
            ["English"]        = "English",
            ["Sí"]             = "Sí",
            ["No"]             = "No",

            // ── Section separators ─────────────────────────────────────────
            ["── Pantalla ──"]        = "── Pantalla ──",
            ["── Visual ──"]          = "── Visual ──",
            ["── Accesibilidad ──"]   = "── Accesibilidad ──",
        },

        ["en"] = new Dictionary<string, string>
        {
            // ── Main menu ──────────────────────────────────────────────────
            ["JUGAR"]     = "PLAY",
            ["OPCIONES"]  = "OPTIONS",
            ["SALIR"]     = "QUIT",
            ["¿Seguro que quieres salir?"] = "Are you sure you want to quit?",

            // ── SlotsScreen ────────────────────────────────────────────────
            ["Nueva partida"]     = "New Game",
            ["Continuar"]         = "Continue",
            ["Borrar"]            = "Erase",
            ["Vacío"]             = "Empty",
            ["Confirmar"]         = "Confirm",
            ["Cancelar"]          = "Cancel",
            ["¿Borrar partida?"]  = "Erase save?",

            // ── Tabs and navigation ───────────────────────────────────────
            ["GRÁFICOS"]  = "VISIONS",
            ["SONIDO"]    = "SOUNDCRAFT",
            ["CONTROLES"] = "COMMANDS",
            ["AJUSTES"]   = "DECREES",
            ["VOLVER"]    = "RETURN",

            // ── Graphics · Display ────────────────────────────────────────
            ["Resolución"]    = "Canvas Size",
            ["Modo pantalla"] = "Window Form",
            ["FPS"]           = "Frame Count",
            ["VSync"]         = "VSync",
            ["Calidad"]       = "Render Quality",

            // Display selector options (medieval English flavour)
            ["Pantalla completa"] = "Full Expanse",
            ["Sin bordes"]        = "Borderless",
            ["Ventana"]           = "Window",
            ["Sin límite"]        = "Unconstrained",
            ["Bajo"]              = "Lesser",
            ["Medio"]             = "Common",
            ["Alto"]              = "Grand",

            // ── Graphics · Visual ──────────────────────────────────────────
            ["Brillo"]     = "Luminance",
            ["Contraste"]  = "Shadow Depth",
            ["Saturación"] = "Colour Vividity",

            // ── Graphics · Accessibility ───────────────────────────────────
            ["Daltonismo"]   = "Colour Ailment",
            ["Protanopia"]   = "Protanopia",
            ["Deuteranopia"] = "Deuteranopia",
            ["Tritanopia"]   = "Tritanopia",
            ["Intensidad"]   = "Intensity",

            // ── Sound ──────────────────────────────────────────────────────
            ["Volumen maestro"]   = "Master Resound",
            ["Música"]            = "Minstrel Tunes",
            ["Efectos de sonido"] = "Battle Clamour",
            ["Voces"]             = "Spoken Word",
            ["Interfaz"]          = "Panel Marks",

            // ── Settings ──────────────────────────────────────────────────
            ["Idioma"]      = "Tongue",
            ["Mostrar FPS"] = "Show Frame Count",
            ["Español"]     = "Español",
            ["English"]     = "English",
            ["Sí"]          = "Yea",
            ["No"]          = "Nay",

            // ── Section separators ─────────────────────────────────────────
            ["── Pantalla ──"]      = "── Display ──",
            ["── Visual ──"]        = "── Visual ──",
            ["── Accesibilidad ──"] = "── Accessibility ──",
        }
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance        = this;
        CurrentLanguage = PlayerPrefs.GetString(EldoriaPrefsKeys.LangCode, "es");
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// Returns the localised string for <paramref name="key"/> in the active language.
    /// Tries an exact match first; falls back to a case-insensitive search.
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        if (!_tables.TryGetValue(CurrentLanguage, out var table)) return key;
        if (table.TryGetValue(key, out var value)) return value;
        // Case-insensitive fallback: game text capitalisation may differ from table keys
        foreach (var pair in table)
            if (string.Equals(pair.Key, key, System.StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        return key;
    }

    /// Returns true if <paramref name="key"/> is registered in the localisation table.
    public static bool ContainsKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (!_tables.TryGetValue("es", out var t)) return false;
        if (t.ContainsKey(key)) return true;
        foreach (var k in t.Keys)
            if (string.Equals(k, key, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// 0 = Español, 1 = English.
    public static void SetLanguage(int index)
    {
        string code = index == 0 ? "es" : "en";
        if (code == CurrentLanguage) return;
        CurrentLanguage = code;
        PlayerPrefs.SetString(EldoriaPrefsKeys.LangCode, code);
        OnLanguageChanged?.Invoke(code);
    }

    // Creates the instance if it doesn't exist yet (no dedicated startup scene required).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("LocalizationManager");
        go.AddComponent<LocalizationManager>();
    }
}
