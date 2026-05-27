#if UNITY_EDITOR
// Menú: Eldoria/Setup MTN11
// L-shape: pozo vertical izquierda (x=[-40,0], y=[-14,+57]) +
//          corredor horizontal base-derecha (x=[0,+54], y=[-14,+9]).
// Sprite 1448×1086 px · scale 6.5 · PPU 100 → 94.12u × 70.59u.
// Entrada desde PreMTN11 por la derecha del corredor.
// Salida hacia MTN12 a través del arco superior del pozo.
// El usuario añade plataformas manualmente.
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupMTN11
{
    // Pozo vertical (izquierda)
    const float SHAFT_X_LEFT  = -40f;
    const float SHAFT_X_RIGHT =   0f;
    const float SHAFT_Y_TOP   = +57f;
    // Corredor horizontal (base-derecha)
    const float CORR_X_RIGHT  = +54f;
    const float CORR_Y_CEIL   =  +9f;
    // Común
    const float Y_FLOOR = -14f;

    [MenuItem("Eldoria/Setup MTN11")]
    static void Run()
    {
        const string scenePath = "Assets/Scenes/Montanas/MTN11.unity";

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

        foreach (var go in scene.GetRootGameObjects())
            if (go.name != "Main Camera" && go.name != "Directional Light")
                Object.DestroyImmediate(go);

        // ── 1. Cámara ─────────────────────────────────────────────────────
        var camGO = GameObject.FindObjectOfType<Camera>() is Camera existingCam
                    ? existingCam.gameObject
                    : new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        var cam = camGO.GetComponent<Camera>();
        if (cam == null) cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 9f;
        cam.backgroundColor  = Color.black;
        cam.cullingMask      = -1;

        if (camGO.GetComponent<AudioListener>() == null)
            camGO.AddComponent<AudioListener>();

        var cf = camGO.GetComponent<CameraFollow>();
        if (cf == null) cf = camGO.AddComponent<CameraFollow>();
        cf.mode      = CameraFollow.CameraMode.FollowBounded;
        cf.boundsMin = new Vector2(-38f, -12f);
        cf.boundsMax = new Vector2(+52f, +48f);

        // ── 2. SteppedCameraBounds ────────────────────────────────────────
        // En el pozo la cámara sube hasta y=+48; en el corredor se queda baja.
        var steppedGO = new GameObject("SteppedCameraBounds");
        var stepped   = steppedGO.AddComponent<SteppedCameraBounds>();
        stepped.steps = new BoundsStep[]
        {
            new BoundsStep { x = SHAFT_X_LEFT, yMin = -12f, yMax = +48f },
            new BoundsStep { x = -5f,          yMin = -12f, yMax = +48f },
            new BoundsStep { x = +10f,         yMin = -12f, yMax =  +7f },
            new BoundsStep { x = CORR_X_RIGHT, yMin = -12f, yMax =  +7f },
        };

        // ── 3. Fondo ──────────────────────────────────────────────────────
        // Sprite 94.12u × 70.59u centrado en (+7, +21) para alinear paredes y suelo.
        var bgGO = new GameObject("Background");
        var bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprites/Escenarios/Montanas/MTN11SinPlataformas.png");
        bgSR.sortingOrder = -10;
        bgGO.transform.localScale = new Vector3(6.5f, 6.5f, 1f);
        bgGO.transform.position   = new Vector3(+7f, +21f, 0f);

        if (bgSR.sprite == null)
            Debug.LogWarning("[SetupMTN11] Sprite MTN11SinPlataformas.png no encontrado — asignarlo manualmente.");

        // ── 4. Colisiones ─────────────────────────────────────────────────
        var wallsGO = new GameObject("Walls");

        float shaftH   = SHAFT_Y_TOP - Y_FLOOR;
        float shaftMidY = (Y_FLOOR + SHAFT_Y_TOP) / 2f;
        float corrH    = CORR_Y_CEIL - Y_FLOOR;
        float corrMidY = (Y_FLOOR + CORR_Y_CEIL) / 2f;

        // Suelo completo (pozo + corredor)
        MakeBox(wallsGO, "Wall_Floor",
            cx: +7f, cy: Y_FLOOR - 0.5f,
            w: CORR_X_RIGHT - SHAFT_X_LEFT + 2f, h: 1f);

        // Pared izquierda del pozo
        MakeBox(wallsGO, "Wall_Left",
            cx: SHAFT_X_LEFT - 0.5f, cy: shaftMidY,
            w: 1f, h: shaftH + 2f);

        // Pared derecha del corredor
        MakeBox(wallsGO, "Wall_Right",
            cx: CORR_X_RIGHT + 0.5f, cy: corrMidY,
            w: 1f, h: corrH + 2f);

        // Techo del corredor horizontal
        MakeBox(wallsGO, "Ceiling_Corridor",
            cx: (SHAFT_X_RIGHT + CORR_X_RIGHT) / 2f, cy: CORR_Y_CEIL + 0.5f,
            w: CORR_X_RIGHT - SHAFT_X_RIGHT + 2f, h: 1f);

        // Pared interior del pozo (derecha del pozo, por encima del corredor)
        float innerH   = SHAFT_Y_TOP - CORR_Y_CEIL;
        float innerMidY = (CORR_Y_CEIL + SHAFT_Y_TOP) / 2f;
        MakeBox(wallsGO, "Wall_Inner",
            cx: SHAFT_X_RIGHT + 0.5f, cy: innerMidY,
            w: 1f, h: innerH + 2f);

        // Techo del pozo — izquierda del arco (sólido)
        MakeBox(wallsGO, "Ceiling_ShaftLeft",
            cx: -33f, cy: SHAFT_Y_TOP + 0.5f,
            w: 16f, h: 1f);   // x = -40 a -25

        // Techo del pozo — derecha del arco (sólido)
        MakeBox(wallsGO, "Ceiling_ShaftRight",
            cx: -10f, cy: SHAFT_Y_TOP + 0.5f,
            w: 18f, h: 1f);   // x = -19 a -1

        // ── 5. SpawnPoints ────────────────────────────────────────────────
        // Entrada desde PreMTN11 (derecha del corredor)
        MakeSpawn("SpawnPoint_premtn11_right", "premtn11_right", new Vector3(+48f, -11f, 0f));
        // Entrada desde MTN12 (arco superior del pozo — caída hacia abajo)
        MakeSpawn("SpawnPoint_mtn12_bottom",   "mtn12_bottom",   new Vector3(-20f, +53f, 0f));

        // ── 6. SceneBoundaries ────────────────────────────────────────────
        // Derecha → PreMTN11
        MakeBoundary("SceneBoundary_Right",
            pos:    new Vector3(CORR_X_RIGHT - 1f, corrMidY, 0f),
            size:   new Vector2(1f, corrH - 1f),
            target: "PreMTN11", spawnId: "mtn11_right");

        // Arriba → MTN12 (arco del pozo, x=-29 a -11 → centro -20, ancho 20)
        MakeBoundary("SceneBoundary_Top",
            pos:    new Vector3(-20f, SHAFT_Y_TOP, 0f),
            size:   new Vector2(20f, 2f),
            target: "MTN12", spawnId: "mtn11_top");

        // ── Guardar ───────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupMTN11] ✓ L-shape configurado. " +
                  "Ajusta posición del sprite Background en Inspector si es necesario. " +
                  "Añade plataformas manualmente.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
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
#endif
