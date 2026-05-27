#if UNITY_EDITOR
// Menú: Eldoria/Setup PreMTN11
// Pasillo horizontal de Dash entre MTN11 (izquierda) y MTN10 (derecha).
// El usuario añade las plataformas manualmente.
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupPreMTN11
{
    const float X_LEFT  = -50f;
    const float X_RIGHT = +50f;
    const float Y_FLOOR = -12f;
    const float Y_CEIL  =  +6f;

    [MenuItem("Eldoria/Setup PreMTN11")]
    static void Run()
    {
        const string scenePath = "Assets/Scenes/Montanas/PreMTN11.unity";

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
        cf.boundsMin = new Vector2(-45f, -12f);
        cf.boundsMax = new Vector2(+45f,  +4f);

        // ── 2. Fondo ──────────────────────────────────────────────────────
        var bgGO = new GameObject("Background");
        var bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprites/Escenarios/Montanas/PreMTN11.png");
        bgSR.sortingOrder = -10;
        bgGO.transform.localScale = new Vector3(6.5f, 6.5f, 1f);
        bgGO.transform.position   = new Vector3(0f, -3f, 0f);

        if (bgSR.sprite == null)
            Debug.LogWarning("[SetupPreMTN11] Sprite PreMTN11.png no encontrado — asignarlo manualmente.");

        // ── 3. Colisiones (suelo, techo, paredes) ─────────────────────────
        var wallsGO = new GameObject("Walls");
        float corridorH = Y_CEIL - Y_FLOOR + 2f;
        float corridorW = X_RIGHT - X_LEFT + 2f;
        float midY      = (Y_FLOOR + Y_CEIL) / 2f;

        MakeBox(wallsGO, "Wall_Floor",
            cx: 0f,          cy: Y_FLOOR - 0.5f,
            w:  corridorW,   h:  1f);

        MakeBox(wallsGO, "Wall_Ceiling",
            cx: 0f,          cy: Y_CEIL + 0.5f,
            w:  corridorW,   h:  1f);

        MakeBox(wallsGO, "Wall_Left",
            cx: X_LEFT - 0.5f,  cy: midY,
            w:  1f,              h:  corridorH);

        MakeBox(wallsGO, "Wall_Right",
            cx: X_RIGHT + 0.5f, cy: midY,
            w:  1f,              h:  corridorH);

        // ── 4. SpawnPoints ────────────────────────────────────────────────
        // "mtn10_left"   — el jugador llega desde MTN10 (derecha) y aparece aquí
        MakeSpawn("SpawnPoint_mtn10_left",   "mtn10_left",   new Vector3(+44f, -10.5f, 0f));
        // "mtn11_right"  — el jugador llega desde MTN11 (izquierda) y aparece aquí
        MakeSpawn("SpawnPoint_mtn11_right",  "mtn11_right",  new Vector3(-44f, -10.5f, 0f));

        // ── 5. SceneBoundaries ────────────────────────────────────────────
        float boundH = corridorH - 1f;

        // Izquierda → MTN11
        MakeBoundary("SceneBoundary_Left",
            pos:    new Vector3(X_LEFT + 1f, midY, 0f),
            size:   new Vector2(1f, boundH),
            target: "MTN11", spawnId: "premtn11_right");

        // Derecha → MTN10
        MakeBoundary("SceneBoundary_Right",
            pos:    new Vector3(X_RIGHT - 1f, midY, 0f),
            size:   new Vector2(1f, boundH),
            target: "MTN10", spawnId: "mtn11_exit");

        // ── Guardar ───────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupPreMTN11] ✓ Pasillo configurado. " +
                  "Flujo: MTN10 → PreMTN11 → MTN11. Añade plataformas manualmente.");
    }

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
