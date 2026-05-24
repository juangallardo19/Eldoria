using System.Collections.Generic;
using UnityEngine;

/// Singleton DontDestroyOnLoad — gestiona el idioma activo y dispara evento global.
///
/// Patrones aplicados:
///   Singleton     — instancia global persistente entre escenas.
///   Observer      — OnLanguageChanged notifica a todos los LocalizedText.
///   Flyweight     — tablas de cadenas compartidas, no duplicadas por objeto.
public class LocalizationManager : MonoBehaviour
{
    // ── Observer ──────────────────────────────────────────────────────────
    public static event System.Action<string> OnLanguageChanged;

    // ── Singleton ─────────────────────────────────────────────────────────
    public static LocalizationManager Instance { get; private set; }

    public static string CurrentLanguage { get; private set; } = "es";

    // ── Flyweight: tablas de cadenas compartidas ───────────────────────────
    static readonly Dictionary<string, Dictionary<string, string>> _tables =
        new Dictionary<string, Dictionary<string, string>>
    {
        ["es"] = new Dictionary<string, string>
        {
            // ── Menú principal ─────────────────────────────────────────────
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

            // ── Pestañas y navegación ─────────────────────────────────────
            ["GRÁFICOS"]  = "GRÁFICOS",
            ["SONIDO"]    = "SONIDO",
            ["CONTROLES"] = "CONTROLES",
            ["AJUSTES"]   = "AJUSTES",
            ["VOLVER"]    = "VOLVER",

            // ── Gráficos · Pantalla ────────────────────────────────────────
            ["Resolución"]      = "Resolución",
            ["Modo pantalla"]   = "Modo pantalla",
            ["FPS"]             = "FPS",
            ["VSync"]           = "VSync",
            ["Calidad"]         = "Calidad",

            // Opciones de selección — pantalla
            ["Pantalla completa"] = "Pantalla completa",
            ["Sin bordes"]        = "Sin bordes",
            ["Ventana"]           = "Ventana",
            ["Sin límite"]        = "Sin límite",
            ["Bajo"]              = "Bajo",
            ["Medio"]             = "Medio",
            ["Alto"]              = "Alto",

            // ── Gráficos · Visual ──────────────────────────────────────────
            ["Brillo"]      = "Brillo",
            ["Contraste"]   = "Contraste",
            ["Saturación"]  = "Saturación",

            // ── Gráficos · Accesibilidad ───────────────────────────────────
            ["Daltonismo"]    = "Daltonismo",
            ["Protanopia"]    = "Protanopia",
            ["Deuteranopia"]  = "Deuteranopia",
            ["Tritanopia"]    = "Tritanopia",
            ["Intensidad"]    = "Intensidad",

            // ── Sonido ─────────────────────────────────────────────────────
            ["Volumen maestro"]     = "Volumen maestro",
            ["Música"]              = "Música",
            ["Efectos de sonido"]   = "Efectos de sonido",
            ["Voces"]               = "Voces",
            ["Interfaz"]            = "Interfaz",

            // ── Ajustes ────────────────────────────────────────────────────
            ["Idioma"]         = "Idioma",
            ["Mostrar FPS"]    = "Mostrar FPS",
            ["Español"]        = "Español",
            ["English"]        = "English",
            ["Sí"]             = "Sí",
            ["No"]             = "No",

            // ── Separadores de sección ─────────────────────────────────────
            ["── Pantalla ──"]        = "── Pantalla ──",
            ["── Visual ──"]          = "── Visual ──",
            ["── Accesibilidad ──"]   = "── Accesibilidad ──",
        },

        ["en"] = new Dictionary<string, string>
        {
            // ── Menú principal ─────────────────────────────────────────────
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

            // ── Pestañas y navegación ─────────────────────────────────────
            ["GRÁFICOS"]  = "VISIONS",
            ["SONIDO"]    = "SOUNDCRAFT",
            ["CONTROLES"] = "COMMANDS",
            ["AJUSTES"]   = "DECREES",
            ["VOLVER"]    = "RETURN",

            // ── Gráficos · Pantalla ────────────────────────────────────────
            ["Resolución"]    = "Canvas Size",
            ["Modo pantalla"] = "Window Form",
            ["FPS"]           = "Frame Count",
            ["VSync"]         = "VSync",
            ["Calidad"]       = "Render Quality",

            // Opciones de selección — pantalla (inglés medieval)
            ["Pantalla completa"] = "Full Expanse",
            ["Sin bordes"]        = "Borderless",
            ["Ventana"]           = "Window",
            ["Sin límite"]        = "Unconstrained",
            ["Bajo"]              = "Lesser",
            ["Medio"]             = "Common",
            ["Alto"]              = "Grand",

            // ── Gráficos · Visual ──────────────────────────────────────────
            ["Brillo"]     = "Luminance",
            ["Contraste"]  = "Shadow Depth",
            ["Saturación"] = "Colour Vividity",

            // ── Gráficos · Accesibilidad ───────────────────────────────────
            ["Daltonismo"]   = "Colour Ailment",
            ["Protanopia"]   = "Protanopia",
            ["Deuteranopia"] = "Deuteranopia",
            ["Tritanopia"]   = "Tritanopia",
            ["Intensidad"]   = "Intensity",

            // ── Sonido ─────────────────────────────────────────────────────
            ["Volumen maestro"]   = "Master Resound",
            ["Música"]            = "Minstrel Tunes",
            ["Efectos de sonido"] = "Battle Clamour",
            ["Voces"]             = "Spoken Word",
            ["Interfaz"]          = "Panel Marks",

            // ── Ajustes ────────────────────────────────────────────────────
            ["Idioma"]      = "Tongue",
            ["Mostrar FPS"] = "Show Frame Count",
            ["Español"]     = "Español",
            ["English"]     = "English",
            ["Sí"]          = "Yea",
            ["No"]          = "Nay",

            // ── Separadores ────────────────────────────────────────────────
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
        CurrentLanguage = PlayerPrefs.GetString("LangCode", "es");
        DontDestroyOnLoad(gameObject);
    }

    // ── API pública ───────────────────────────────────────────────────────
    /// Devuelve la cadena localizada para <paramref name="key"/> en el idioma activo.
    /// Primero intenta coincidencia exacta; si falla, busca ignorando mayúsculas/minúsculas.
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        if (!_tables.TryGetValue(CurrentLanguage, out var table)) return key;
        if (table.TryGetValue(key, out var value)) return value;
        // Fallback case-insensitive: textos del juego pueden tener capitalización distinta a las claves
        foreach (var pair in table)
            if (string.Equals(pair.Key, key, System.StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        return key;
    }

    /// Devuelve true si <paramref name="key"/> está registrada en la tabla de localización.
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
        PlayerPrefs.SetString("LangCode", code);
        OnLanguageChanged?.Invoke(code);
    }

    // Crea la instancia si no existe aún (sin escena de arranque dedicada).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("LocalizationManager");
        go.AddComponent<LocalizationManager>();
    }
}
