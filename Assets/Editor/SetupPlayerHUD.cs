#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// Menú: Eldoria/Setup Player HUD
// Crea Assets/Resources/PlayerHUDConfig.asset y asigna todos los sprites de Ara y Kael.
// Ejecutar UNA vez después de importar los sprites.
public static class SetupPlayerHUD
{
    [MenuItem("Eldoria/Setup Player HUD")]
    static void Execute()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        const string ASSET_PATH = "Assets/Resources/PlayerHUDConfig.asset";
        var cfg = AssetDatabase.LoadAssetAtPath<PlayerHUDConfig>(ASSET_PATH);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<PlayerHUDConfig>();
            AssetDatabase.CreateAsset(cfg, ASSET_PATH);
        }

        // ── Ara animations ────────────────────────────────────────────────
        const string HUD = "Assets/UI/Sprites/Hud/Health/";
        cfg.araIdle   = LoadSprites(HUD + "AraIdle/",   "AraIdle1",   "AraIdle2",   "AraIdle3",   "AraIdle4");
        cfg.araDamage = LoadSprites(HUD + "AraDamage/", "AraDamage1", "AraDamage2", "AraDamage3", "AraDamage4");
        cfg.araLow    = LoadSprites(HUD + "AraLow/",    "AraLow1",    "AraLow2",    "AraLow3",    "AraLow4");
        cfg.araDeath  = LoadSprites(HUD + "AraDeath/",  "AraDeath1",  "AraDeath2",  "AraDeath3",  "AraDeath4");
        cfg.araHeal   = LoadSprites(HUD + "ArHeal/",    "AraHeal1",   "AraHeal2",   "AraHeal3",   "AraHeal4");

        // ── HUD visuals ───────────────────────────────────────────────────
        cfg.araContainer = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/UI/Sprites/Hud/Container/Container.png");
        if (cfg.araContainer == null) Debug.LogWarning("[SetupPlayerHUD] araContainer no encontrado.");

        // ── Kael idle — sub-sprites del sprite sheet ──────────────────────
        const string KAEL_PATH = "Assets/Sprites/Kael/male_hero_template-idle.png";
        var all = AssetDatabase.LoadAllAssetsAtPath(KAEL_PATH);
        cfg.kaelIdle = all
            .OfType<Sprite>()
            .OrderBy(s => s.name, System.StringComparer.OrdinalIgnoreCase)
            .ToArray();

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Reporte ───────────────────────────────────────────────────────
        Debug.Log($"[SetupPlayerHUD] ✓ PlayerHUDConfig actualizado en {ASSET_PATH}");
        if (cfg.araIdle   == null || cfg.araIdle.Length   == 0) Debug.LogWarning("[SetupPlayerHUD] araIdle no encontrado.");
        if (cfg.araDamage == null || cfg.araDamage.Length == 0) Debug.LogWarning("[SetupPlayerHUD] araDamage no encontrado.");
        if (cfg.araLow    == null || cfg.araLow.Length    == 0) Debug.LogWarning("[SetupPlayerHUD] araLow no encontrado.");
        if (cfg.araDeath  == null || cfg.araDeath.Length  == 0) Debug.LogWarning("[SetupPlayerHUD] araDeath no encontrado.");
        if (cfg.kaelIdle  == null || cfg.kaelIdle.Length  == 0) Debug.LogWarning("[SetupPlayerHUD] kaelIdle no encontrado.");
        else Debug.Log($"[SetupPlayerHUD] ✓ {cfg.kaelIdle.Length} frames de Kael cargados.");
    }

    static Sprite[] LoadSprites(string folder, params string[] names)
    {
        var sprites = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(folder + names[i] + ".png");
            if (sprites[i] == null)
                Debug.LogWarning($"[SetupPlayerHUD] No encontrado: {folder}{names[i]}.png");
        }
        return sprites;
    }
}
#endif
