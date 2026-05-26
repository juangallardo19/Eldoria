using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;

// Menú: Eldoria/Boss/1 - Importar Sprites Boss1
// Configura todos los sprite sheets del boss como Multiple Sprite (201×94px/frame).
// Usa ISpriteEditorDataProvider (API correcta en Unity 2022.3).
public static class ImportBoss1Sprites
{
    const int    FrameW   = 201;
    const int    FrameH   = 94;
    const string BasePath = "Assets/Sprites/Boss1Obsesion/Sprite Sheets/";

    // (fileName, framesHint)  — framesHint=-1 calcula desde el ancho real
    static readonly (string name, int frames)[] Sheets =
    {
        ("static sleep ",         1),
        ("wake",                 12),
        ("idle",                 24),
        ("move",                 24),
        ("turnaound to right",    6),
        ("turnaround to left",    6),
        ("Melee attack",         23),
        ("range attack",         21),
        ("Spin charge",          -1),
        ("Spin charge end",       3),
        ("boomarang arms",       -1),
        ("body solo for boomarang", -1),
        ("buff",                 13),
        ("super attack",         18),
        ("death",                21),
    };

    [MenuItem("Eldoria/Boss/1 - Importar Sprites Boss1")]
    static void Run()
    {
        int ok = 0, skip = 0;

        foreach (var (name, framesHint) in Sheets)
        {
            string path = BasePath + name + ".png";
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                Debug.LogWarning($"[ImportBoss1] No encontrado: {path}");
                skip++;
                continue;
            }

            // ── 1. Calcular número de frames ─────────────────────────────
            int frames;
            if (framesHint > 0)
            {
                frames = framesHint;
            }
            else
            {
                ti.GetSourceTextureWidthAndHeight(out int tw, out int _);
                frames = Mathf.Max(1, tw / FrameW);
            }

            // ── 2. Configuración base del TextureImporter ────────────────
            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Multiple;
            ti.filterMode          = FilterMode.Point;               // pixel art — sin blur
            ti.textureCompression  = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled       = false;
            ti.spritePixelsPerUnit = 16f;
            ti.maxTextureSize      = 8192;                           // sheets anchos (4824px) sin recorte
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();   // obligatorio antes de usar ISpriteEditorDataProvider

            // ── 3. Cortar frames con la API moderna ──────────────────────
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
            dp.InitSpriteEditorDataProvider();

            var rects = new SpriteRect[frames];
            for (int i = 0; i < frames; i++)
            {
                rects[i] = new SpriteRect
                {
                    name      = $"{name.Trim()}_{i}",
                    rect      = new Rect(i * FrameW, 0, FrameW, FrameH),
                    pivot     = new Vector2(0.5f, 0f),
                    alignment = SpriteAlignment.Custom,
                    spriteID  = GUID.Generate(),
                };
            }

            dp.SetSpriteRects(rects);
            dp.Apply();

            // Reimportar para materializar los cambios
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[ImportBoss1] '{name}' → {frames} frames OK");
            ok++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ImportBoss1] ✓ {ok} sprite sheets importados correctamente. {skip} no encontrados.");
    }
}
