using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Patrón Command — crea y configura MTN10 (Sala del Boss, arena horizontal amplia).
// Layout: x ∈ [-65, +65], suelo y=-12, fosa de cristales y=-20, techo orgánico en domo y≈+22.
// Entrada DERECHA desde PreMTN10 (spawnId="premtn10_right").
// Salida IZQUIERDA hacia MTN11 placeholder (spawnId="mtn10_left").
public static class SetupMTN10
{
    const float X_LEFT  = -65f;
    const float X_RIGHT = +65f;
    const float Y_FLOOR = -12f;
    const float Y_PIT   = -20f;

    [MenuItem("Eldoria/Setup MTN10 (Boss Arena)")]
    static void Run()
    {
        const string scenePath = "Assets/Scenes/Montanas/MTN10.unity";

        // Crear escena si no existe, abrirla si ya existe
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath) == null)
        {
            var ns = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(ns, scenePath);
            AssetDatabase.Refresh();
            AddToBuildSettings(scenePath);
        }
        else
        {
            EditorSceneManager.OpenScene(scenePath);
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // Limpiar GameObjects previos
        foreach (var go in scene.GetRootGameObjects())
            if (go.name != "Main Camera" && go.name != "Directional Light")
                Object.DestroyImmediate(go);

        // ── 1. Cámara ─────────────────────────────────────────────────────
        var camGO = GameObject.FindObjectOfType<Camera>() is Camera existingCam
                    ? existingCam.gameObject
                    : new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGO.GetComponent<Camera>() ?? camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 9f;
        cam.backgroundColor  = Color.black;
        cam.cullingMask      = -1;

        if (camGO.GetComponent<AudioListener>() == null)
            camGO.AddComponent<AudioListener>();

        var cf = camGO.GetComponent<CameraFollow>() ?? camGO.AddComponent<CameraFollow>();
        cf.mode      = CameraFollow.CameraMode.FollowBounded;
        cf.boundsMin = new Vector2(-60f, -10f);
        cf.boundsMax = new Vector2(+60f, +16f);

        // ── 2. SteppedCameraBounds ────────────────────────────────────────
        // Sigue el domo del techo: cámara más alta en el centro, baja en los extremos.
        var steppedGO = new GameObject("SteppedCameraBounds");
        var stepped   = steppedGO.AddComponent<SteppedCameraBounds>();
        stepped.steps = new BoundsStep[]
        {
            new BoundsStep { x = X_LEFT, yMin = -10f, yMax =  +8f },
            new BoundsStep { x =  -20f,  yMin = -10f, yMax = +16f },
            new BoundsStep { x =  +20f,  yMin = -10f, yMax = +16f },
            new BoundsStep { x = X_RIGHT,yMin = -10f, yMax =  +8f },
        };

        // ── 3. Fondo (MTN10.png, scale=6.5) ──────────────────────────────
        var bgGO = new GameObject("Background");
        var bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite       = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprites/Escenarios/Montanas/MTN10.png");
        bgSR.sortingOrder = -10;
        bgGO.transform.localScale = new Vector3(6.5f, 6.5f, 1f);
        bgGO.transform.position   = Vector3.zero;

        if (bgSR.sprite == null)
            Debug.LogWarning("[SetupMTN10] Sprite MTN10.png no encontrado — asignarlo manualmente.");

        // ── 4. Paredes sólidas (BoxCollider2D) ────────────────────────────
        var wallsGO = new GameObject("Walls");

        float roomH = 8f - Y_FLOOR + 2f; // altura pared lateral ≈ 22u

        // Suelo completo
        MakeBox(wallsGO, "Wall_Floor",
            cx: 0f,               cy: Y_FLOOR - 0.5f,
            w:  X_RIGHT - X_LEFT + 2f, h: 1f);

        // Pared izquierda
        MakeBox(wallsGO, "Wall_Left",
            cx: X_LEFT - 0.5f,   cy: (Y_FLOOR + 8f) / 2f,
            w:  1f,               h:  roomH);

        // Pared derecha
        MakeBox(wallsGO, "Wall_Right",
            cx: X_RIGHT + 0.5f,  cy: (Y_FLOOR + 8f) / 2f,
            w:  1f,               h:  roomH);

        // ── 5. Techo orgánico (EdgeCollider2D) ────────────────────────────
        // Domo con pico y=+22 en el centro. Ajustar puntos en Scene View.
        var ceilGO  = new GameObject("Ceiling");
        ceilGO.transform.SetParent(wallsGO.transform);
        var ceilCol = ceilGO.AddComponent<EdgeCollider2D>();
        ceilCol.points = new Vector2[]
        {
            new Vector2(-65f,  +8f),
            new Vector2(-58f, +10f),
            new Vector2(-50f, +13f),
            new Vector2(-40f, +16f),
            new Vector2(-28f, +19f),
            new Vector2(-14f, +21f),
            new Vector2(  0f, +22f),
            new Vector2(+14f, +21f),
            new Vector2(+28f, +19f),
            new Vector2(+40f, +16f),
            new Vector2(+50f, +13f),
            new Vector2(+58f, +10f),
            new Vector2(+65f,  +8f),
        };

        // ── 6. Plataformas OneWay ─────────────────────────────────────────
        var platSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprites/Escenarios/Plataformas/Montanas/PlataformaLarga.png");

        MakeOneWayPlatform("Plat_Left",    new Vector3(-42f, -3f, 0), 22f, platSprite);
        MakeOneWayPlatform("Plat_Right",   new Vector3(+40f,  0f, 0), 20f, platSprite);
        MakeOneWayPlatform("Plat_CenterL", new Vector3(-15f, -7f, 0), 14f, platSprite);
        MakeOneWayPlatform("Plat_CenterR", new Vector3(+15f, -7f, 0), 14f, platSprite);

        // ── 7. CrystalHazard en la fosa ───────────────────────────────────
        // Polígono auto-generado 5×2u; ajustar forma en Scene View.
        var hazardGO = new GameObject("CrystalHazard_Pit");
        hazardGO.transform.position = new Vector3(0f, Y_PIT + 1f, 0f);
        hazardGO.AddComponent<CrystalHazard>();

        // ── 8. CrystalRespawnManager ──────────────────────────────────────
        new GameObject("CrystalRespawnManager").AddComponent<CrystalRespawnManager>();

        // ── 9. SpawnPoints ────────────────────────────────────────────────
        MakeSpawn("SpawnPoint_premtn10_right", "premtn10_right", new Vector3(+58f, -10f, 0f));
        MakeSpawn("SpawnPoint_mtn11_exit",     "mtn11_exit",     new Vector3(-55f, -10f, 0f));

        // ── 10. SceneBoundaries ───────────────────────────────────────────
        float midY    = (Y_FLOOR + 8f) / 2f;
        float boundH  = roomH;

        // Derecha → PreMTN10
        MakeBoundary("SceneBoundary_Right",
            pos:    new Vector3(X_RIGHT - 1f, midY, 0f),
            size:   new Vector2(1f, boundH),
            target: "PreMTN10", spawnId: "mtn10_exit");

        // Izquierda → MTN11 (placeholder boss)
        MakeBoundary("SceneBoundary_Left",
            pos:    new Vector3(X_LEFT + 1f, midY, 0f),
            size:   new Vector2(1f, boundH),
            target: "MTN11", spawnId: "mtn10_left");

        // ── Guardar ───────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupMTN10] ✓ Sala del Boss configurada. " +
                  "Ajusta Ceiling (EdgeCollider2D) y CrystalHazard_Pit en Scene View.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static void AddToBuildSettings(string scenePath)
    {
        var current = EditorBuildSettings.scenes;
        foreach (var s in current)
            if (s.path == scenePath) return;

        var newList = new EditorBuildSettingsScene[current.Length + 1];
        System.Array.Copy(current, newList, current.Length);
        newList[current.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newList;
    }

    static void MakeBox(GameObject parent, string name,
                        float cx, float cy, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = new Vector3(cx, cy, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);
    }

    static void MakeOneWayPlatform(string name, Vector3 pos, float width, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = -5;

        var col  = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, 0.5f);

        go.AddComponent<OneWayPlatform>();
    }

    static void MakeSpawn(string goName, string id, Vector3 pos)
    {
        var go = new GameObject(goName);
        go.transform.position = pos;
        go.AddComponent<SpawnPoint>().spawnId = id;
    }

    static void MakeBoundary(string goName, Vector3 pos, Vector2 size,
                              string target, string spawnId)
    {
        var go = new GameObject(goName);
        go.transform.position = pos;

        var col       = go.AddComponent<BoxCollider2D>();
        col.size      = size;
        col.isTrigger = true;

        var sb = go.AddComponent<SceneBoundary>();
        var so = new UnityEditor.SerializedObject(sb);
        so.FindProperty("targetScene").stringValue = target;
        so.FindProperty("spawnId").stringValue     = spawnId;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
