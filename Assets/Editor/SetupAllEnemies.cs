using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Menú: Eldoria/Setup All Enemies
//
// Coloca SombraNinja y SombraMago en todas las escenas MTN según el diseño:
//   MTN02 → 1 ninja  (ya hecho con SetupSombraNinja, se recoloca para idempotencia)
//   MTN04 → 1 mago
//   MTN05 → 1 ninja + 1 mago
//   MTN06 → 1 ninja + 2 magos
//   MTN08 → 2 ninjas + 3 magos
//   MTN09 → ninguno (santuario de Ara — zona libre de enemigos)
//
// Prerequisitos (ejecutar una vez antes):
//   · Eldoria/SombraNinja/1 - Importar Sprites
//   · Eldoria/SombraMago/1  - Importar Sprites
//   · Eldoria/SombraMago/2  - Crear Prefab Proyectil
//
// Carga cada escena en modo Single, coloca los enemigos y la guarda.
// AVISO: cerrará la escena activa actual.
public static class SetupAllEnemies
{
    // ── Rutas ──────────────────────────────────────────────────────────────────
    const string NinjaIdlePath   = "Assets/Sprites/Enemigos/sprite ninja/Sprite idle movement.png";
    const string NinjaAttackPath = "Assets/Sprites/Enemigos/sprite ninja/attack sprite actualizado.png";
    const string NinjaDeathPath  = "Assets/Sprites/Enemigos/sprite ninja/death sprite.png";

    const string MagoIdlePath    = "Assets/Sprites/Enemigos/sprite mago/sombra idle movement.png";
    const string MagoHurtPath    = "Assets/Sprites/Enemigos/sprite mago/damage sombra.png";
    const string ProjPrefabPath  = "Assets/Prefabs/Enemies/SombraProyectil.prefab";
    const string SceneBase       = "Assets/Scenes/Montanas/";

    // ── Definición de colocaciones por escena ─────────────────────────────────
    // Cada entrada: (tipo, posX, posY, patrolLeft, patrolRight)
    struct Spec { public bool isMago; public float x, y, pL, pR; }

    static Spec Ninja(float x, float y, float pL, float pR) =>
        new Spec { isMago = false, x=x, y=y, pL=pL, pR=pR };
    static Spec Mago(float x, float y, float pL, float pR) =>
        new Spec { isMago = true,  x=x, y=y, pL=pL, pR=pR };

    static readonly (string scene, Spec[] enemies)[] Plan =
    {
        // MTN02: suelo y=-16.68, walls x=±43.7
        ("MTN02", new[]
        {
            Ninja(0f, -14f, -20f, 20f),
        }),

        // MTN04: cúpula suelo y=-10, walls x=±50. Entrada desde derecha (x=42) y desde abajo.
        // Mago en zona izquierda para que el jugador que entra por la derecha lo encuentre al avanzar.
        ("MTN04", new[]
        {
            Mago(-12f, -7f, -42f, 18f),
        }),

        // MTN05: suelo y=-12, walls x=±52. Entrada arriba (x=0,y=16) y desde derecha (x=44).
        // Ninja cubre la zona izquierda; mago la zona derecha, esperando al jugador que baja.
        ("MTN05", new[]
        {
            Ninja(-18f, -9f, -46f,  0f),
            Mago(  18f, -9f,   2f, 46f),
        }),

        // MTN06: suelo y=-14.5, walls x=±54. Atraviesa de izquierda (MTN05) a derecha (MTN08).
        // Ninja en el centro; magos a ambos lados para crear fuego cruzado.
        ("MTN06", new[]
        {
            Ninja(  0f, -11f, -28f,  28f),
            Mago( -28f, -11f, -50f,  -5f),
            Mago(  28f, -11f,   5f,  50f),
        }),

        // MTN08: cueva multinivel. Suelo y=-22, plataformas bajas y=-11, medias y=4, altas y=-3.
        // Ninjas en plataformas bajas (más lentos, cuerpo a cuerpo);
        // magos en plataformas medias/altas para hostigar al jugador desde arriba.
        ("MTN08", new[]
        {
            Ninja(-30f,  -9f, -50f,  -3f),   // Low_Left platform (y≈-11)
            Ninja( 30f,  -9f,   3f,  50f),   // Low_Right platform
            Mago( -35f,  -1f, -50f, -12f),   // Upper_Left platform (y≈-3)
            Mago(   0f,   8f, -12f,  12f),   // High_Center platform (y≈11)
            Mago(  35f,  -1f,  12f,  50f),   // Upper_Right platform
        }),

        // MTN09: Antesala del Boss — santuario de Ara, sin enemigos (zona de descanso).
    };

    // ── Menú principal ────────────────────────────────────────────────────────
    [MenuItem("Eldoria/Setup All Enemies")]
    static void Run()
    {
        // Verificar prerequisitos
        if (!CheckPrerequisites()) return;

        bool ok = EditorUtility.DisplayDialog("Eldoria — Setup All Enemies",
            "Este script va a:\n" +
            "· Abrir y modificar: MTN02, MTN04, MTN05, MTN06, MTN08\n" +
            "· Reemplazar TODOS los SombraNinja y SombraMago existentes\n" +
            "· Añadir CrystalRespawnManager donde falte\n\n" +
            "La escena activa actual se cerrará.\n¿Continuar?",
            "Sí, colocar enemigos", "Cancelar");
        if (!ok) return;

        // Guardar escena activa antes de empezar
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Cargar assets una sola vez
        var ninjaAssets  = LoadNinjaAssets();
        var magoAssets   = LoadMagoAssets();

        int totalPlaced = 0;
        foreach (var (sceneName, specs) in Plan)
        {
            string path  = SceneBase + sceneName + ".unity";
            var    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogWarning($"[AllEnemies] Escena no encontrada: {path}");
                continue;
            }

            // Limpiar instancias previas (idempotencia)
            ClearExistingEnemies();

            // Asegurar CrystalRespawnManager
            EnsureCrystalRespawnManager();

            // Colocar enemigos
            int ninjaIdx = 1, magoIdx = 1;
            foreach (var s in specs)
            {
                if (s.isMago)
                    PlaceMago(s, magoIdx++, magoAssets);
                else
                    PlaceNinja(s, ninjaIdx++, ninjaAssets);

                totalPlaced++;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AllEnemies] {sceneName}: {specs.Length} enemigos colocados.");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Eldoria",
            $"Setup completado.\n{totalPlaced} enemigos colocados en {Plan.Length} escenas.\n\n" +
            "MTN09 permanece sin enemigos (santuario de Ara).", "OK");
    }

    // ── Prerequisitos ─────────────────────────────────────────────────────────
    static bool CheckPrerequisites()
    {
        var missing = new System.Collections.Generic.List<string>();

        if (AssetDatabase.LoadAllAssetsAtPath(NinjaIdlePath).OfType<Sprite>().Count() == 0)
            missing.Add("Sprites del ninja (ejecuta SombraNinja/1 - Importar Sprites)");
        if (AssetDatabase.LoadAllAssetsAtPath(MagoIdlePath).OfType<Sprite>().Count() == 0)
            missing.Add("Sprites del mago (ejecuta SombraMago/1 - Importar Sprites)");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ProjPrefabPath) == null)
            missing.Add("Prefab proyectil (ejecuta SombraMago/2 - Crear Prefab Proyectil)");

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("Eldoria — Prerequisitos faltantes",
                "Antes de continuar, ejecuta:\n\n" + string.Join("\n", missing), "OK");
            return false;
        }
        return true;
    }

    // ── Limpieza ──────────────────────────────────────────────────────────────
    static void ClearExistingEnemies()
    {
        foreach (var go in Object.FindObjectsOfType<SombraNinja>())
            Object.DestroyImmediate(go.gameObject);
        foreach (var go in Object.FindObjectsOfType<SombraMago>())
            Object.DestroyImmediate(go.gameObject);
    }

    static void EnsureCrystalRespawnManager()
    {
        if (Object.FindObjectOfType<CrystalRespawnManager>() != null) return;
        var mgrs = GameObject.Find("GameManagers") ?? new GameObject("GameManagers");
        mgrs.AddComponent<CrystalRespawnManager>();
        Debug.Log("[AllEnemies] CrystalRespawnManager añadido.");
    }

    // ── Crear SombraNinja ─────────────────────────────────────────────────────
    static void PlaceNinja(Spec s, int idx, NinjaAssets a)
    {
        var root = new GameObject($"SombraNinja_{idx}");
        root.transform.position   = new Vector3(s.x, s.y, 0f);
        root.transform.localScale = new Vector3(2f, 2f, 1f);

        var sr = root.AddComponent<SpriteRenderer>();
        if (a.idleFrames.Length > 0) sr.sprite = a.idleFrames[0];
        sr.sortingOrder = 0;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 3f;
        rb.freezeRotation         = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var body    = root.AddComponent<BoxCollider2D>();
        body.size   = new Vector2(0.75f, 1.4f);
        body.offset = new Vector2(0f, 0.7f);

        // AttackHitbox child
        var hitboxGO             = new GameObject("AttackHitbox");
        hitboxGO.transform.SetParent(root.transform, false);
        hitboxGO.transform.localPosition = Vector3.zero;
        var hCol      = hitboxGO.AddComponent<BoxCollider2D>();
        hCol.isTrigger = true;
        hCol.size      = new Vector2(2.5f, 1.2f);
        hCol.offset    = new Vector2(1.2f, 0.5f);
        hCol.enabled   = false;
        var hitboxComp = hitboxGO.AddComponent<BossAttackHitbox>();

        var ninja = root.AddComponent<SombraNinja>();
        var so    = new SerializedObject(ninja);
        SetSpriteArray(so, "idleFrames",   a.idleFrames);
        SetSpriteArray(so, "attackFrames", a.attackFrames);
        SetSpriteArray(so, "deathFrames",  a.deathFrames);
        so.FindProperty("attackHitbox").objectReferenceValue = hitboxComp;
        so.FindProperty("patrolLeft") .floatValue = s.pL;
        so.FindProperty("patrolRight").floatValue = s.pR;
        so.ApplyModifiedProperties();
    }

    // ── Crear SombraMago ──────────────────────────────────────────────────────
    static void PlaceMago(Spec s, int idx, MagoAssets a)
    {
        var root = new GameObject($"SombraMago_{idx}");
        root.transform.position   = new Vector3(s.x, s.y, 0f);
        root.transform.localScale = new Vector3(2f, 2f, 1f);

        var sr = root.AddComponent<SpriteRenderer>();
        if (a.idleFrames.Length > 0) sr.sprite = a.idleFrames[0];
        sr.sortingOrder = 0;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 3f;
        rb.freezeRotation         = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var body    = root.AddComponent<BoxCollider2D>();
        body.size   = new Vector2(0.7f, 1.3f);
        body.offset = new Vector2(0f, 0.65f);

        var mago = root.AddComponent<SombraMago>();
        var so   = new SerializedObject(mago);
        SetSpriteArray(so, "idleFrames", a.idleFrames);
        SetSpriteArray(so, "hurtFrames", a.hurtFrames);
        so.FindProperty("projectilePrefab").objectReferenceValue = a.projPrefab;
        so.FindProperty("patrolLeft") .floatValue = s.pL;
        so.FindProperty("patrolRight").floatValue = s.pR;
        so.ApplyModifiedProperties();
    }

    // ── Cargar assets ──────────────────────────────────────────────────────────
    struct NinjaAssets
    {
        public Sprite[] idleFrames, attackFrames, deathFrames;
    }
    struct MagoAssets
    {
        public Sprite[] idleFrames, hurtFrames;
        public GameObject projPrefab;
    }

    static NinjaAssets LoadNinjaAssets() => new NinjaAssets
    {
        idleFrames   = LoadSorted(NinjaIdlePath),
        attackFrames = LoadSorted(NinjaAttackPath),
        deathFrames  = LoadSorted(NinjaDeathPath),
    };

    static MagoAssets LoadMagoAssets() => new MagoAssets
    {
        idleFrames = LoadSorted(MagoIdlePath),
        hurtFrames = LoadSorted(MagoHurtPath),
        projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjPrefabPath),
    };

    static Sprite[] LoadSorted(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.rect.x)
            .ToArray();

    static void SetSpriteArray(SerializedObject so, string prop, Sprite[] sprites)
    {
        var p = so.FindProperty(prop);
        p.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
