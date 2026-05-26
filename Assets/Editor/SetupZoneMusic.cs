// Menú: Eldoria/Setup Zone Music
// Crea Assets/Resources/ZoneMusicConfig.asset y asigna los 4 clips de zona.
using UnityEditor;
using UnityEngine;

public static class SetupZoneMusic
{
    [MenuItem("Eldoria/Setup Zone Music")]
    static void Execute()
    {
        // ── 1. Crear carpeta Resources si no existe ───────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // ── 2. Cargar (o crear) el ZoneMusicConfig asset ──────────────────────
        const string assetPath = "Assets/Resources/ZoneMusicConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<ZoneMusicConfig>(assetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<ZoneMusicConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
        }

        // ── 3. Asignar clips ──────────────────────────────────────────────────
        config.menuMusic    = Load<AudioClip>("Assets/audio/Lost Temples.ogg");
        config.hvMusic      = Load<AudioClip>("Assets/audio/Celestial Kingdom.ogg");
        config.mtnMusic     = Load<AudioClip>("Assets/audio/Enchanted Ruins.ogg");
        config.caveAmbience = Load<AudioClip>("Assets/UI/Sprites/NewGame/Ambience Cave Sound Effect.mp3");
        config.bossMusic    = Load<AudioClip>("Assets/audio/Medieval Folk Music - Mountain Storm - Brandon Fiechter's Music.ogg");

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Eldoria] ZoneMusicConfig creado/actualizado en Assets/Resources/ZoneMusicConfig.asset");

        // ── 4. Reporte de clips encontrados ───────────────────────────────────
        if (config.menuMusic    == null) Debug.LogWarning("[Eldoria] menuMusic no encontrado.");
        if (config.hvMusic      == null) Debug.LogWarning("[Eldoria] hvMusic no encontrado.");
        if (config.mtnMusic     == null) Debug.LogWarning("[Eldoria] mtnMusic no encontrado.");
        if (config.caveAmbience == null) Debug.LogWarning("[Eldoria] caveAmbience no encontrado.");
        if (config.bossMusic    == null) Debug.LogWarning("[Eldoria] bossMusic no encontrado.");
    }

    static T Load<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path);
}
