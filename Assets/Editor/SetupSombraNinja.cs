using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.SceneManagement;

// Patrón: Command (cada MenuItem encapsula una acción reversible y acotada)
//
// Paso 1 — Eldoria/SombraNinja/1 - Importar Sprites
//   Corta los 3 sprite sheets del ninja con ISpriteEditorDataProvider (API Unity 2022.3).
//   Ejecutar UNA sola vez; es idempotente si se repite.
//
// Paso 2 — Eldoria/SombraNinja/2 - Colocar en MTN02
//   Requiere que MTN02 sea la escena activa.
//   Crea "SombraNinja_1" con jerarquía completa y lo cablea con los sprites ya importados.
public static class SetupSombraNinja
{
    const string SpritePath = "Assets/Sprites/Enemigos/sprite ninja/";

    // (fileName sin extensión, frameCount, frameW, frameH)
    static readonly (string file, int frames, int fw, int fh)[] Sheets =
    {
        ("Sprite idle movement",      8,  76, 49),
        ("attack sprite actualizado", 8, 156, 48),
        ("death sprite",              7,  76, 66),
    };

    // ── Paso 1: importar/rebanar sprite sheets ────────────────────────────────
    [MenuItem("Eldoria/SombraNinja/1 - Importar Sprites")]
    static void ImportSprites()
    {
        int ok = 0;
        foreach (var (file, frames, fw, fh) in Sheets)
        {
            string path = SpritePath + file + ".png";
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[SombraNinja] No encontrado: {path}"); continue; }

            // Configuración base
            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Multiple;
            ti.filterMode          = FilterMode.Point;
            ti.textureCompression  = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled       = false;
            ti.spritePixelsPerUnit = 16f;
            ti.maxTextureSize      = 4096;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();   // obligatorio antes de usar ISpriteEditorDataProvider

            // Ancho real de la textura para el último frame
            ti.GetSourceTextureWidthAndHeight(out int texW, out int _);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
            dp.InitSpriteEditorDataProvider();

            var rects = new SpriteRect[frames];
            for (int i = 0; i < frames; i++)
            {
                int x = i * fw;
                int w = (i == frames - 1) ? (texW - x) : fw;   // último frame = píxeles restantes
                rects[i] = new SpriteRect
                {
                    name      = $"{file}_{i}",
                    rect      = new Rect(x, 0, w, fh),
                    pivot     = new Vector2(0.25f, 0f),         // figura visible ~25% desde la izquierda
                    alignment = SpriteAlignment.Custom,
                    spriteID  = GUID.Generate(),
                };
            }

            dp.SetSpriteRects(rects);
            dp.Apply();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[SombraNinja] '{file}' → {frames} frames importados");
            ok++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SombraNinja] Paso 1 completo — {ok}/3 sheets importados.");
    }

    // ── Paso 2: crear SombraNinja_1 en MTN02 ─────────────────────────────────
    [MenuItem("Eldoria/SombraNinja/2 - Colocar en MTN02")]
    static void PlaceInScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "MTN02")
        {
            EditorUtility.DisplayDialog("Eldoria",
                "Abre la escena MTN02 antes de ejecutar este menú.", "OK");
            return;
        }

        // Idempotencia: eliminar instancia previa si existe
        var old = GameObject.Find("SombraNinja_1");
        if (old != null) { Undo.DestroyObjectImmediate(old); }

        // ── 1. Cargar sprites (requiere haber ejecutado el Paso 1 antes) ───────
        var idleSprites   = LoadSorted("Sprite idle movement");
        var attackSprites = LoadSorted("attack sprite actualizado");
        var deathSprites  = LoadSorted("death sprite");

        if (idleSprites.Length == 0 || attackSprites.Length == 0 || deathSprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Eldoria",
                "Sprites no encontrados.\nEjecuta primero 'SombraNinja/1 - Importar Sprites'.", "OK");
            return;
        }

        // ── 2. Crear GameObject raíz ─────────────────────────────────────────
        var root = new GameObject("SombraNinja_1");
        Undo.RegisterCreatedObjectUndo(root, "Crear SombraNinja_1");

        root.transform.position   = new Vector3(0f, -14f, 0f);
        root.transform.localScale = new Vector3(2f, 2f, 1f);

        // SpriteRenderer
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite       = idleSprites[0];
        sr.sortingOrder = 0;

        // Rigidbody2D (valores definitivos; Start() los refuerza en runtime)
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 3f;
        rb.freezeRotation         = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // BoxCollider2D del cuerpo (no trigger)
        var bodyCol    = root.AddComponent<BoxCollider2D>();
        bodyCol.size   = new Vector2(0.75f, 1.4f);
        bodyCol.offset = new Vector2(0f, 0.7f);

        // ── 3. Hijo AttackHitbox ─────────────────────────────────────────────
        var hitboxGO = new GameObject("AttackHitbox");
        hitboxGO.transform.SetParent(root.transform, false);
        hitboxGO.transform.localPosition = Vector3.zero;

        // Hitbox amplio para simular arco de arma largo (se voltea en SombraNinja.UpdateFacing).
        // Con scale=(2,2,1): size→(5.0u, 2.4u) world, offset→(2.4u, 1.0u) frente al ninja.
        var hitboxCol      = hitboxGO.AddComponent<BoxCollider2D>();
        hitboxCol.isTrigger = true;
        hitboxCol.size      = new Vector2(2.5f, 1.2f);
        hitboxCol.offset    = new Vector2(1.2f, 0.5f);
        hitboxCol.enabled   = false;   // desactivado hasta que SombraNinja lo active

        var hitboxComp = hitboxGO.AddComponent<BossAttackHitbox>();

        // ── 4. SombraNinja: asignar sprites y parámetros via SerializedObject ─
        var ninja = root.AddComponent<SombraNinja>();
        var so    = new SerializedObject(ninja);

        AssignSpriteArray(so, "idleFrames",   idleSprites);
        AssignSpriteArray(so, "attackFrames", attackSprites);
        AssignSpriteArray(so, "deathFrames",  deathSprites);

        so.FindProperty("attackHitbox").objectReferenceValue = hitboxComp;
        so.FindProperty("patrolLeft") .floatValue = -20f;
        so.FindProperty("patrolRight").floatValue =  20f;

        so.ApplyModifiedProperties();

        // ── 5. CrystalRespawnManager: necesario para que el ninja haga daño ─────
        // BossAttackHitbox.TryHit() requiere CrystalRespawnManager.Instance != null.
        // Si la escena no lo tiene (solo existe en escenas con hazards de cristal),
        // crear un GameManagers GO y añadirlo.
        if (Object.FindObjectOfType<CrystalRespawnManager>() == null)
        {
            var mgrsGO = GameObject.Find("GameManagers") ?? new GameObject("GameManagers");
            Undo.RegisterCreatedObjectUndo(mgrsGO, "Crear GameManagers");
            Undo.AddComponent<CrystalRespawnManager>(mgrsGO);
            Debug.Log("[SombraNinja] CrystalRespawnManager añadido a la escena.");
        }

        // ── 6. Marcar escena como sucia y guardar ────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SombraNinja] SombraNinja_1 creado en MTN02 — pos(0,-14), patrulla ±20u.");
        EditorUtility.DisplayDialog("Eldoria",
            "SombraNinja_1 colocado en MTN02.\n\n" +
            "• CrystalRespawnManager asegurado en escena (daño habilitado)\n" +
            "• El jugador puede atravesarlo (IgnoreCollision en Start)\n\n" +
            "Revisa en Scene View y ajusta los colliders si es necesario.", "OK");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static Sprite[] LoadSorted(string fileName)
    {
        string path = SpritePath + fileName + ".png";
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.rect.x)
            .ToArray();
    }

    static void AssignSpriteArray(SerializedObject so, string propName, Sprite[] sprites)
    {
        var prop = so.FindProperty(propName);
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
