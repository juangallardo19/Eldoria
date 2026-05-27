// Centralized PlayerPrefs key constants — prevents typos and eases future renames.
// Usage: PlayerPrefs.GetString(EldoriaPrefsKeys.SanctuaryScene, "");
public static class EldoriaPrefsKeys
{
    // ── Sanctuary / respawn ───────────────────────────────────────────────────
    public const string SanctuaryScene = "SanctuaryScene";
    public const string SanctuaryX     = "SanctuaryX";
    public const string SanctuaryY     = "SanctuaryY";

    // ── Audio ─────────────────────────────────────────────────────────────────
    public const string MasterVolume = "MasterVolume";
    public const string MusicVolume  = "MusicVolume";
    public const string SFXVolume    = "SFXVolume";
    public const string VoicesVolume = "VoicesVolume";
    public const string UIVolume     = "UIVolume";

    // ── Graphics ──────────────────────────────────────────────────────────────
    public const string Resolution  = "Resolution";
    public const string ScreenMode  = "ScreenMode";
    public const string FPS         = "FPS";
    public const string VSync       = "VSync";
    public const string Quality     = "Quality";
    public const string Brightness  = "Brightness";
    public const string Contrast    = "Contrast";
    public const string Saturation  = "Saturation";

    // ── Accessibility ─────────────────────────────────────────────────────────
    public const string ColorBlind          = "ColorBlind";
    public const string ColorBlindType      = "ColorBlindType";
    public const string ColorBlindIntensity = "ColorBlindIntensity";

    // ── Display / HUD ─────────────────────────────────────────────────────────
    public const string ShowFPS = "ShowFPS";

    // ── Localization ──────────────────────────────────────────────────────────
    public const string Language = "Language";
    public const string LangCode = "LangCode";

    // ── Key rebinding — prefix only; append the actionId ─────────────────────
    // Example: PlayerPrefs.GetString(EldoriaPrefsKeys.KeyPrefix + "Jump", "Z");
    public const string KeyPrefix = "Key_";

    // ── World map — prefix only; append the zoneId ───────────────────────────
    // Example: PlayerPrefs.GetInt(EldoriaPrefsKeys.MapVisitedPrefix + "MTN01", 0);
    public const string MapVisitedPrefix = "MapVisited_";
}
