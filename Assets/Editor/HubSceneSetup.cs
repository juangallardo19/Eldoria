#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Crea la escena "HubCentral" (exterior del Hub) como escena NUEVA.
// No modifica la escena Game (Interior Casa Kael).
// Menú: Eldoria → Create Hub Exterior Scene
public static class HubSceneSetup
{
    // ── Rutas ─────────────────────────────────────────────────────────────────
    const string SCENE_FOLDER = "Assets/Scenes/HubCentral";
    const string SCENE_PATH   = "Assets/Scenes/HubCentral/HV01_Exterior.unity";

    const string PATH_NOCHE     = "Assets/Sprites/Escenarios/Paisajes/Hub/Noche.png";
    const string PATH_AMANECER  = "Assets/Sprites/Escenarios/Paisajes/Hub/Amanecer.png";
    const string PATH_DIA       = "Assets/Sprites/Escenarios/Paisajes/Hub/Dia.png";
    const string PATH_ANOCHECER = "Assets/Sprites/Escenarios/Paisajes/Hub/anochecer.png";
    const string PATH_PLAT_LARGA= "Assets/Sprites/Escenarios/Plataformas/hub/PlataformaLarga.png";
    const string PATH_PLAT_IZQ  = "Assets/Sprites/Escenarios/Plataformas/hub/PlataformaIzquierda.png";
    const string PATH_PLAT_DER  = "Assets/Sprites/Escenarios/Plataformas/hub/PlataformaDerecha.png";
    const string PATH_CASA_KAEL = "Assets/Sprites/Escenarios/Hub/CasaKael.png";
    const string PATH_CASAS2    = "Assets/Sprites/Escenarios/Hub/Casas2.png";

    // ── Layout ────────────────────────────────────────────────────────────────
    const float SCENE_HALF = 30f;
    const float GROUND_Y   = -5f;
    const float SURFACE_Y  = -4.5f;
    const float BG_SCALE   = 1.5f;

    const int ORDER_BG       = -20;
    const int ORDER_HOUSE    =  -3;
    const int ORDER_PLATFORM =   2;

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Eldoria/Create Hub Exterior Scene")]
    static void CreateHubExterior()
    {
        AssetDatabase.Refresh();

        // Crear carpeta si no existe
        if (!AssetDatabase.IsValidFolder(SCENE_FOLDER))
            AssetDatabase.CreateFolder("Assets/Scenes", "HubCentral");

        // Guardar la escena activa actual antes de crear la nueva
        var previousScene = EditorSceneManager.GetActiveScene();
        string previousPath = previousScene.path;

        // Crear la nueva escena en modo aditivo para no cerrar la actual
        var hubScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(hubScene);

        // ── Construir la escena exterior ──────────────────────────────────────
        SetupCamera();
        var backgrounds = CreateBackgrounds();
        CreateStructures();
        CreatePlatforms();
        CreateBoundaries();
        CreateDayCycle(backgrounds);

        // ── Guardar ───────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(hubScene, SCENE_PATH);

        // Agregar a Build Settings
        AddToBuildSettings(SCENE_PATH);

        // Volver a la escena anterior como activa y cerrar la nueva
        if (!string.IsNullOrEmpty(previousPath))
        {
            EditorSceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(hubScene, true);
        }

        AssetDatabase.Refresh();
        Debug.Log("[HubSceneSetup] ✓ Escena HubCentral creada en " + SCENE_PATH +
                  "\n→ PENDIENTE: Añade y configura el Player en esa escena (duplica el de Game o usa un prefab).");
    }

    // ── Cámara ────────────────────────────────────────────────────────────────
    static void SetupCamera()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        var cam             = camGO.AddComponent<Camera>();
        cam.orthographic    = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = Color.black;
        cam.clearFlags      = CameraClearFlags.SolidColor;

        var cf         = camGO.AddComponent<CameraFollow>();
        cf.mode        = CameraFollow.CameraMode.FollowBounded;
        cf.boundsMin   = new Vector2(-SCENE_HALF + 9f, -10f);
        cf.boundsMax   = new Vector2( SCENE_HALF - 9f,  6f);
        // cf.target se asigna cuando el jugador exista en la escena
    }

    // ── Fondos parallax ───────────────────────────────────────────────────────
    static SpriteRenderer[] CreateBackgrounds()
    {
        var parent = new GameObject("Backgrounds");

        var data = new[]
        {
            (PATH_NOCHE,     "BG_Noche",     1f),
            (PATH_AMANECER,  "BG_Amanecer",  0f),
            (PATH_DIA,       "BG_Dia",       0f),
            (PATH_ANOCHECER, "BG_Anochecer", 0f),
        };

        var renderers = new SpriteRenderer[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            var (path, goName, alpha) = data[i];
            var go = new GameObject(goName);
            go.transform.SetParent(parent.transform);
            go.transform.position   = new Vector3(0f, 0f, 10f);
            go.transform.localScale = new Vector3(BG_SCALE, BG_SCALE, 1f);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = LoadSprite(path);
            sr.sortingOrder = ORDER_BG;
            Color c = Color.white; c.a = alpha; sr.color = c;

            var par              = go.AddComponent<ParallaxBackground>();
            par.parallaxFactor   = 0.12f;

            renderers[i] = sr;
        }
        return renderers;
    }

    // ── Casas ─────────────────────────────────────────────────────────────────
    static void CreateStructures()
    {
        var parent = new GameObject("Structures");

        CreateHouse(parent.transform, "CasaKael",   PATH_CASA_KAEL, new Vector3(-4f,  SURFACE_Y, 0f));
        CreateHouse(parent.transform, "CasasRight",  PATH_CASAS2,    new Vector3(16f, SURFACE_Y, 0f));
    }

    static void CreateHouse(Transform parent, string goName, string path, Vector3 pos)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = LoadSprite(path);
        sr.sortingOrder = ORDER_HOUSE;
    }

    // ── Plataformas ───────────────────────────────────────────────────────────
    static void CreatePlatforms()
    {
        var parent = new GameObject("Platforms");

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 8;

        // Collider físico del suelo (invisible)
        var groundCol = new GameObject("Ground_Collider");
        groundCol.transform.SetParent(parent.transform);
        groundCol.transform.position = new Vector3(0f, GROUND_Y, 0f);
        groundCol.layer              = groundLayer;

        var bc      = groundCol.AddComponent<BoxCollider2D>();
        bc.size     = new Vector2(SCENE_HALF * 2f + 5f, 1f);

        var rs      = groundCol.AddComponent<RoomStructure>();
        rs.type     = RoomStructure.StructureType.Ground;

        // Visuales: LeftCap + 4 segmentos centrales + RightCap
        MakePlatVisual(parent.transform, "Ground_Visual_LeftCap",
            PATH_PLAT_IZQ, new Vector3(-SCENE_HALF + 2f, SURFACE_Y, 0f));

        float[] midXs = { -18f, -6f, 6f, 18f };
        foreach (float mx in midXs)
            MakePlatVisual(parent.transform, "Ground_Visual_Mid",
                PATH_PLAT_LARGA, new Vector3(mx, SURFACE_Y, 0f));

        MakePlatVisual(parent.transform, "Ground_Visual_RightCap",
            PATH_PLAT_DER, new Vector3(SCENE_HALF - 2f, SURFACE_Y, 0f));
    }

    static void MakePlatVisual(Transform parent, string goName, string path, Vector3 pos)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = LoadSprite(path);
        sr.sortingOrder = ORDER_PLATFORM;
    }

    // ── Límites de escena ─────────────────────────────────────────────────────
    static void CreateBoundaries()
    {
        var parent = new GameObject("Boundaries");

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 8;

        // Paredes sólidas (detienen físicamente al jugador)
        MakeWall(parent.transform, "WallLeft",
            new Vector3(-SCENE_HALF - 1.5f, GROUND_Y, 0f),
            new Vector2(1f, 20f), groundLayer, false, "");

        MakeWall(parent.transform, "WallRight",
            new Vector3( SCENE_HALF + 1.5f, GROUND_Y, 0f),
            new Vector2(1f, 20f), groundLayer, false, "");

        // Triggers de cambio de escena
        MakeWall(parent.transform, "BoundaryLeft",
            new Vector3(-SCENE_HALF - 0.5f, 0f, 0f),
            new Vector2(1f, 16f), 0, true, "HV07");

        MakeWall(parent.transform, "BoundaryRight",
            new Vector3( SCENE_HALF + 0.5f, 0f, 0f),
            new Vector2(1f, 16f), 0, true, "HV02");
    }

    static void MakeWall(Transform parent, string goName, Vector3 pos,
                          Vector2 size, int layer, bool isTrigger, string targetScene)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.layer              = layer;

        var bc       = go.AddComponent<BoxCollider2D>();
        bc.size      = size;
        bc.isTrigger = isTrigger;

        if (isTrigger && !string.IsNullOrEmpty(targetScene))
        {
            var sb = go.AddComponent<SceneBoundary>();
            var so = new SerializedObject(sb);
            so.FindProperty("targetScene").stringValue = targetScene;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ── DayCycle ──────────────────────────────────────────────────────────────
    static void CreateDayCycle(SpriteRenderer[] bgs)
    {
        var go = new GameObject("DayCycle");
        var dc = go.AddComponent<DayCycleController>();
        if (bgs.Length >= 4)
        {
            dc.bgNight = bgs[0];
            dc.bgDawn  = bgs[1];
            dc.bgDay   = bgs[2];
            dc.bgDusk  = bgs[3];
        }
    }

    // ── Build Settings ────────────────────────────────────────────────────────
    static void AddToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return; // ya está

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[HubSceneSetup] Escena HubCentral agregada a Build Settings.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static Sprite LoadSprite(string path)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null) Debug.LogWarning("[HubSceneSetup] Sprite no encontrado: " + path);
        return sp;
    }
}
#endif
