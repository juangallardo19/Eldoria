using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.SceneManagement;

// Pattern: Command (each MenuItem encapsulates a scoped, idempotent action)
//
// Step 1 — Eldoria/SombraMago/1 - Import Sprites
//   · sombra idle movement.png  → 10 frames at 80px stride (PPU=16, pivot 0.25/0)
//   · damage sombra.png         → 5  frames at 74px stride (PPU=16, pivot 0.25/0)
//   · atack sombra1-6.png       → 6 individual sprites for body animation while casting
//   · atack sombraprime1-6.png  → 6 individual sprites for the projectile (PPU=16, pivot 0.5/0.5)
//
// Step 2 — Eldoria/SombraMago/2 - Create Projectile Prefab
//   Creates Assets/Prefabs/Enemies/SombraProyectil.prefab and assigns the 6 frames.
//
// Step 3 — Eldoria/SombraMago/3 - Place in MTN04
//   Requires MTN04 as the active scene. Creates SombraMago_1 at (-12,-7), patrol (-42,18).
public static class SetupSombraMago
{
    const string SpritePath  = "Assets/Sprites/Enemigos/sprite mago/";
    const string PrefabDir   = "Assets/Prefabs/Enemies";
    const string PrefabPath  = "Assets/Prefabs/Enemies/SombraProyectil.prefab";

    // ── Paso 1: importar sprites ───────────────────────────────────────────────
    [MenuItem("Eldoria/SombraMago/1 - Importar Sprites")]
    static void ImportSprites()
    {
        // ── Idle: 10 frames, stride 80px, último frame = 783-720=63px ─────────
        SliceSheet("sombra idle movement", 10, 80, 59, 0.25f, 0f);

        // ── Damage/Hurt: 5 frames, stride 74px, último = 373-296=77px ─────────
        SliceSheet("damage sombra", 5, 74, 50, 0.25f, 0f);

        // ── Ataque cuerpo: 6 archivos individuales, pivot centrado ───────────
        for (int i = 1; i <= 6; i++)
            ImportSingle($"atack sombra{i}");

        // ── Proyectil prime: 6 archivos individuales, pivot centrado ──────────
        for (int i = 1; i <= 6; i++)
            ImportSingle($"atack sombraprime{i}");

        AssetDatabase.Refresh();
        Debug.Log("[SombraMago] Paso 1 completo — sprites importados.");
    }

    // ── Paso 2: crear prefab del proyectil ────────────────────────────────────
    [MenuItem("Eldoria/SombraMago/2 - Crear Prefab Proyectil")]
    static void CreateProjectilePrefab()
    {
        // Cargar frames del proyectil
        var projFrames = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            string p = SpritePath + $"atack sombraprime{i + 1}.png";
            projFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (projFrames[i] == null)
                Debug.LogWarning($"[SombraMago] Frame proyectil no encontrado: {p}. Ejecuta Paso 1 primero.");
        }

        // Garantizar carpeta Prefabs/Enemies
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");
        }

        // Crear GO temporal para el prefab
        var go = new GameObject("SombraProyectil");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = projFrames[0];
        sr.sortingOrder = 5;

        go.AddComponent<Rigidbody2D>();  // configurado por SombraProyectil.Awake()
        go.AddComponent<CircleCollider2D>();

        var proj       = go.AddComponent<SombraProyectil>();
        var so         = new SerializedObject(proj);
        var framesProp = so.FindProperty("frames");
        framesProp.arraySize = 6;
        for (int i = 0; i < 6; i++)
            framesProp.GetArrayElementAtIndex(i).objectReferenceValue = projFrames[i];
        so.ApplyModifiedProperties();

        // Guardar como prefab
        bool success;
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out success);
        Object.DestroyImmediate(go);

        if (success)
            Debug.Log($"[SombraMago] Prefab proyectil creado en {PrefabPath}");
        else
            Debug.LogError("[SombraMago] Falló la creación del prefab.");

        AssetDatabase.Refresh();
    }

    // ── Paso 3: colocar SombraMago_1 en MTN04 ────────────────────────────────
    [MenuItem("Eldoria/SombraMago/3 - Colocar en MTN04")]
    static void PlaceInScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "MTN04")
        {
            EditorUtility.DisplayDialog("Eldoria",
                "Abre la escena MTN04 antes de ejecutar este menú.", "OK");
            return;
        }

        // Idempotencia
        var old = GameObject.Find("SombraMago_1");
        if (old != null) Undo.DestroyObjectImmediate(old);

        // Cargar sprites
        var idleSprites   = LoadSortedSheet("sombra idle movement");
        var hurtSprites   = LoadSortedSheet("damage sombra");
        var attackSprites = LoadSingleSet("atack sombra", 6);

        if (idleSprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Eldoria",
                "Sprites no encontrados.\nEjecuta primero 'SombraMago/1 - Importar Sprites'.", "OK");
            return;
        }

        var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (projPrefab == null)
        {
            EditorUtility.DisplayDialog("Eldoria",
                "Prefab de proyectil no encontrado.\nEjecuta primero 'SombraMago/2 - Crear Prefab Proyectil'.", "OK");
            return;
        }

        // ── Crear raíz ───────────────────────────────────────────────────────
        var root = new GameObject("SombraMago_1");
        Undo.RegisterCreatedObjectUndo(root, "Crear SombraMago_1");

        // MTN04 cúpula: zona izquierda, lejos de la entrada derecha (x=42)
        root.transform.position   = new Vector3(-12f, -7f, 0f);
        root.transform.localScale = new Vector3(2f, 2f, 1f);

        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite       = idleSprites[0];
        sr.sortingOrder = 0;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 3f;
        rb.freezeRotation         = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var bodyCol    = root.AddComponent<BoxCollider2D>();
        bodyCol.size   = new Vector2(0.7f, 1.3f);
        bodyCol.offset = new Vector2(0f, 0.65f);

        // ── Componente SombraMago ────────────────────────────────────────────
        var mago = root.AddComponent<SombraMago>();
        var so   = new SerializedObject(mago);

        AssignSpriteArray(so, "idleFrames",   idleSprites);
        AssignSpriteArray(so, "attackFrames", attackSprites);
        AssignSpriteArray(so, "hurtFrames",   hurtSprites);
        so.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
        so.FindProperty("patrolLeft") .floatValue = -42f;
        so.FindProperty("patrolRight").floatValue =  18f;
        so.ApplyModifiedProperties();

        // ── CrystalRespawnManager si la escena no lo tiene ───────────────────
        if (Object.FindObjectOfType<CrystalRespawnManager>() == null)
        {
            var mgrsGO = GameObject.Find("GameManagers") ?? new GameObject("GameManagers");
            Undo.AddComponent<CrystalRespawnManager>(mgrsGO);
            Debug.Log("[SombraMago] CrystalRespawnManager añadido a la escena.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SombraMago] SombraMago_1 creado en MTN04 — pos(-12,-7), patrulla (-42,18).");
        EditorUtility.DisplayDialog("Eldoria",
            "SombraMago_1 colocado en MTN04.\n\n" +
            "• Pos (-12, -7) — zona izquierda de la cúpula\n" +
            "• Patrulla x=[-42, 18]\n" +
            "• Detecta jugador a 18u, dispara a 10u\n" +
            "• Attack frames (atack sombra1-6) asignados al cuerpo\n\n" +
            "Ajusta posición y collider en el Inspector si es necesario.", "OK");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static void SliceSheet(string fileName, int frames, int frameW, int frameH,
                           float pivotX, float pivotY)
    {
        string path = SpritePath + fileName + ".png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning($"[SombraMago] No encontrado: {path}"); return; }

        ti.textureType         = TextureImporterType.Sprite;
        ti.spriteImportMode    = SpriteImportMode.Multiple;
        ti.filterMode          = FilterMode.Point;
        ti.textureCompression  = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled       = false;
        ti.spritePixelsPerUnit = 16f;
        ti.maxTextureSize      = 4096;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();

        ti.GetSourceTextureWidthAndHeight(out int texW, out int _);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();

        var rects = new SpriteRect[frames];
        for (int i = 0; i < frames; i++)
        {
            int x = i * frameW;
            int w = (i == frames - 1) ? (texW - x) : frameW;
            rects[i] = new SpriteRect
            {
                name      = $"{fileName}_{i}",
                rect      = new Rect(x, 0, w, frameH),
                pivot     = new Vector2(pivotX, pivotY),
                alignment = SpriteAlignment.Custom,
                spriteID  = GUID.Generate(),
            };
        }

        dp.SetSpriteRects(rects);
        dp.Apply();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[SombraMago] '{fileName}' → {frames} frames importados");
    }

    static void ImportSingle(string fileName)
    {
        string path = SpritePath + fileName + ".png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning($"[SombraMago] No encontrado: {path}"); return; }

        ti.textureType         = TextureImporterType.Sprite;
        ti.spriteImportMode    = SpriteImportMode.Single;
        ti.filterMode          = FilterMode.Point;
        ti.textureCompression  = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled       = false;
        ti.spritePixelsPerUnit = 16f;
        ti.maxTextureSize      = 512;
        // Pivot centrado — proyectil gira alrededor de su centro
        ti.spritePivot = new Vector2(0.5f, 0.5f);
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
        Debug.Log($"[SombraMago] '{fileName}' importado como Single sprite");
    }

    static Sprite[] LoadSortedSheet(string fileName)
    {
        string path = SpritePath + fileName + ".png";
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.rect.x)
            .ToArray();
    }

    static Sprite[] LoadSingleSet(string prefix, int count)
    {
        var result = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string path = SpritePath + $"{prefix}{i + 1}.png";
            result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (result[i] == null)
                Debug.LogWarning($"[SombraMago] No encontrado: {path}");
        }
        return result;
    }

    static void AssignSpriteArray(SerializedObject so, string propName, Sprite[] sprites)
    {
        var prop = so.FindProperty(propName);
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
