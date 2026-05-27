#if UNITY_EDITOR
// Menú: Eldoria/Setup MTN12
// Cueva final con techo orgánico que baja de izquierda (alto) a derecha (bajo).
// Suelo de cristales peligrosos. Escalón/balcón superior-izquierda = entrada desde MTN11.
// Arco en pared derecha = salida futura a PreMTN12.
//
// Sprite: MTN12.png · 1448×1086 px · scale 6.5 · PPU 100 → 94.12u × 70.59u
//
// CADENA COMPLETA VERIFICADA:
//   MTN10  ←→  PreMTN11  ←→  MTN11  ←→  MTN12  (→ PreMTN12 futuro)
//
// Entrada desde MTN11:  SpawnPoint "mtn11_top"    en balcón superior-izq (-24,+31)
// Salida hacia MTN11:   SceneBoundary pared izq   → MTN11, spawnId="mtn12_bottom"
//                       (MTN11 tiene SpawnPoint "mtn12_bottom" en (-20,+53))
// Salida hacia PreMTN12: SceneBoundary arco derecho → PreMTN12, spawnId="mtn12_right"
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupMTN12
{
    // Dimensiones de la sala
    const float X_LEFT  = -33f;
    const float X_RIGHT = +44f;
    const float Y_FLOOR = -14f;

    // Balcón superior-izquierda (entrada desde MTN11)
    const float LEDGE_Y     = +28f;   // superficie del balcón
    const float LEDGE_RIGHT = -18f;   // hasta dónde llega el balcón (x)
    const float LEDGE_CEIL  = +34f;   // techo del nicho del balcón

    // Arco en pared derecha
    const float ARCH_BOTTOM = +4f;
    const float ARCH_TOP    = +12f;

    [MenuItem("Eldoria/Setup MTN12")]
    static void Run()
    {
        const string scenePath = "Assets/Scenes/Montanas/MTN12.unity";

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

        // Cámara fija FitRoom: cubre la sala completa sin desplazarse.
        // Room: x=[-33,+44] (77u) × y=[-14,+34] (48u)
        // Centro: (5.5, 10) — orthoSize=24 → altura exacta 48u; ancho visible ≈85u > 77u ✓
        camGO.transform.position = new Vector3(+5.5f, +10f, -10f);
        cam.orthographicSize     = 24f;

        var cf = camGO.GetComponent<CameraFollow>();
        if (cf == null) cf = camGO.AddComponent<CameraFollow>();
        cf.mode      = CameraFollow.CameraMode.FitRoom;
        cf.boundsMin = new Vector2(X_LEFT, Y_FLOOR);
        cf.boundsMax = new Vector2(X_RIGHT, LEDGE_CEIL);

        // ── 3. Fondo ──────────────────────────────────────────────────────
        // 94.12u × 70.59u: izq-sprite=-47 → pared izq x=-33 (15% desde borde)
        //                  abajo-sprite=-14 → suelo (center_y=+21)
        var bgGO = new GameObject("Background");
        var bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprites/Escenarios/Montanas/MTN12.png");
        bgSR.sortingOrder = -10;
        bgGO.transform.localScale = new Vector3(6.5f, 6.5f, 1f);
        bgGO.transform.position   = new Vector3(0f, +21f, 0f);

        if (bgSR.sprite == null)
            Debug.LogWarning("[SetupMTN12] Sprite MTN12.png no encontrado — asignarlo manualmente.");

        // ── 4. Colisiones ─────────────────────────────────────────────────
        var wallsGO = new GameObject("Walls");

        // Suelo completo
        float roomW = X_RIGHT - X_LEFT + 2f;
        MakeBox(wallsGO, "Wall_Floor",
            cx: (X_LEFT + X_RIGHT) / 2f,  cy: Y_FLOOR - 0.5f,
            w:  roomW,                     h:  1f);

        // Pared izquierda (hasta el balcón)
        float leftH = LEDGE_Y - Y_FLOOR;
        MakeBox(wallsGO, "Wall_Left",
            cx: X_LEFT - 0.5f,            cy: (Y_FLOOR + LEDGE_Y) / 2f,
            w:  1f,                        h:  leftH + 1f);

        // Superficie del balcón superior-izquierda
        MakeBox(wallsGO, "Ledge_Floor",
            cx: (X_LEFT + LEDGE_RIGHT) / 2f, cy: LEDGE_Y - 0.5f,
            w:  LEDGE_RIGHT - X_LEFT,         h:  1f);

        // Pared interior del nicho (lado derecho del balcón, de suelo de balcón a techo del nicho)
        MakeBox(wallsGO, "Wall_LedgeInner",
            cx: LEDGE_RIGHT - 0.5f,  cy: (LEDGE_Y + LEDGE_CEIL) / 2f,
            w:  1f,                   h:  LEDGE_CEIL - LEDGE_Y + 1f);

        // Pared derecha (por debajo del arco)
        float rightH = ARCH_BOTTOM - Y_FLOOR;
        MakeBox(wallsGO, "Wall_Right",
            cx: X_RIGHT + 0.5f,  cy: (Y_FLOOR + ARCH_BOTTOM) / 2f,
            w:  1f,              h:  rightH + 1f);

        // Techo orgánico: baja de izquierda (y≈+34) a derecha (y≈+12).
        // Ajustar puntos en Scene View para que coincidan con el sprite.
        var ceilGO  = new GameObject("Ceiling");
        ceilGO.transform.SetParent(wallsGO.transform);
        var ceilCol = ceilGO.AddComponent<EdgeCollider2D>();
        ceilCol.points = new Vector2[]
        {
            new Vector2(X_LEFT,   LEDGE_CEIL),   // (-33, +34) — techo del nicho del balcón
            new Vector2(-15f,     +28f),
            new Vector2(  0f,     +22f),
            new Vector2(+15f,     +17f),
            new Vector2(+30f,     +13f),
            new Vector2(+40f,     ARCH_TOP),      // (+40, +12)
            new Vector2(X_RIGHT,  ARCH_TOP),      // (+44, +12) — conecta con la pared derecha
        };

        // ── 5. Hazard de cristales en el suelo ────────────────────────────
        // Cubre la mayor parte del suelo (los cristales son el peligro principal de la zona).
        var hazardGO = new GameObject("CrystalHazard_Floor");
        hazardGO.transform.position = new Vector3(0f, Y_FLOOR + 2f, 0f);
        hazardGO.AddComponent<CrystalHazard>();

        new GameObject("CrystalRespawnManager").AddComponent<CrystalRespawnManager>();

        // ── 6. SpawnPoints ────────────────────────────────────────────────
        // Desde MTN11: aparece en el balcón superior-izquierda
        MakeSpawn("SpawnPoint_mtn11_top",     "mtn11_top",     new Vector3(-24f, +31f, 0f));
        // Desde PreMTN12: aparece cerca del arco derecho
        MakeSpawn("SpawnPoint_premtn12_exit", "premtn12_exit", new Vector3(+38f, +10f, 0f));

        // ── 7. SceneBoundaries ────────────────────────────────────────────
        float ledgeNicheMidY  = (LEDGE_Y + LEDGE_CEIL) / 2f;
        float ledgeNicheH     = LEDGE_CEIL - LEDGE_Y;

        // Izquierda → MTN11 (trigger en la pared izq a la altura del balcón)
        MakeBoundary("SceneBoundary_MTN11",
            pos:    new Vector3(X_LEFT + 1f, ledgeNicheMidY, 0f),
            size:   new Vector2(2f, ledgeNicheH - 1f),
            target: "MTN11", spawnId: "mtn12_bottom");

        // Derecha → PreMTN12 (arco — placeholder para la conexión futura)
        float archMidY = (ARCH_BOTTOM + ARCH_TOP) / 2f;
        MakeBoundary("SceneBoundary_Right",
            pos:    new Vector3(X_RIGHT - 1f, archMidY, 0f),
            size:   new Vector2(2f, ARCH_TOP - ARCH_BOTTOM - 1f),
            target: "PreMTN12", spawnId: "mtn12_right");

        // ── Guardar ───────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupMTN12] ✓ Cueva final configurada. " +
                  "Ajusta Ceiling (EdgeCollider2D) en Scene View. " +
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
