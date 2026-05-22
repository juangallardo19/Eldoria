using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Genera la escena HV05 completa desde cero.
// Mismas dimensiones que HV04: hub05.png 1672×941px @ PPU=100 → escala 5 → 83.6×47.05u
public static class HV05SceneSetup
{
    const float BG_SCALE = 5f;
    const float SKY_SCALE = 6f;
    const float WALL_X  = 42f;
    const float FLOOR_Y = -23.5f;
    const float CEIL_Y  =  23.5f;
    const float ROOM_W  = 86f;
    const float ROOM_H  = 50f;

    [MenuItem("Eldoria/Setup HV05")]
    static void SetupHV05()
    {
        var scene = SceneManager.GetActiveScene();

        // ── Cargar assets ────────────────────────────────────────────────────
        var spBG        = Load<Sprite>("Assets/Sprites/Escenarios/Hub/hub05.png");
        var spNoche     = Load<Sprite>("Assets/Sprites/Escenarios/Paisajes/Hub/Noche.png");
        var spDia       = Load<Sprite>("Assets/Sprites/Escenarios/Paisajes/Hub/Dia.png");
        var spAmanecer  = Load<Sprite>("Assets/Sprites/Escenarios/Paisajes/Hub/Amanecer.png");
        var spAnochecer = Load<Sprite>("Assets/Sprites/Escenarios/Paisajes/Hub/anochecer.png");

        var prefabPath   = AssetDatabase.GUIDToAssetPath("53a1fe1c4186a9d46a4883334438886c");
        var playerPrefab = string.IsNullOrEmpty(prefabPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        // ── Limpiar escena ───────────────────────────────────────────────────
        foreach (var go in scene.GetRootGameObjects())
            Undo.DestroyObjectImmediate(go);

        // ── Main Camera ──────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        Undo.RegisterCreatedObjectUndo(camGO, "Create Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0, 0, -10);

        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 23.5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = Color.black;
        camGO.AddComponent<AudioListener>();

        var camFollow = camGO.AddComponent<CameraFollow>();
        camFollow.mode         = CameraFollow.CameraMode.FitRoom;
        camFollow.targetOffset = new Vector2(0f, 3f);

        // ── Background (hub05.png) ───────────────────────────────────────────
        var bgGO = Reg(new GameObject("Background"), "BG");
        bgGO.transform.position   = new Vector3(0, 0, 5);
        bgGO.transform.localScale = new Vector3(BG_SCALE, BG_SCALE, 1);
        var bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite       = spBG;
        bgSR.sortingOrder = -10;

        camFollow.roomBackground = bgSR;

        // ── Cielo parallax ───────────────────────────────────────────────────
        var skyGO = Reg(new GameObject("SkyBackground"), "Sky");
        skyGO.transform.position = new Vector3(0, 0, 10);

        var dayCycle = skyGO.AddComponent<DayCycleController>();
        dayCycle.bgNight = SkyLayer(skyGO, "BG_Noche",     spNoche,     -15, 1f);
        dayCycle.bgDawn  = SkyLayer(skyGO, "BG_Amanecer",  spAmanecer,  -13, 0f);
        dayCycle.bgDay   = SkyLayer(skyGO, "BG_Dia",       spDia,       -14, 0f);
        dayCycle.bgDusk  = SkyLayer(skyGO, "BG_Anochecer", spAnochecer, -12, 0f);

        // ── Colisiones de sala (layer 8 = Ground) ────────────────────────────
        MakeWall("Ground",    new Vector3(0,       FLOOR_Y, 0), new Vector2(ROOM_W, 1));
        MakeWall("WallLeft",  new Vector3(-WALL_X, 0,       0), new Vector2(1, ROOM_H));
        MakeWall("WallRight", new Vector3( WALL_X, 0,       0), new Vector2(1, ROOM_H));
        MakeWall("Ceiling",   new Vector3(0,       CEIL_Y,  0), new Vector2(ROOM_W, 1));

        // ── PlayerSpawnManager ───────────────────────────────────────────────
        var psmGO = Reg(new GameObject("PlayerSpawnManager"), "PSM");
        psmGO.AddComponent<PlayerSpawnManager>();

        // ── SpawnPoints ──────────────────────────────────────────────────────
        var spawnGroup = Reg(new GameObject("SpawnPoints"), "SpawnGroup");
        var spawnGO    = Reg(new GameObject("SpawnPoint_DoorHV05"), "Spawn");
        spawnGO.transform.SetParent(spawnGroup.transform);
        spawnGO.transform.position = new Vector3(-38f, FLOOR_Y + 4f, 0);
        var spComp = spawnGO.AddComponent<SpawnPoint>();
        spComp.spawnId = "door_HV05";

        // ── Puerta de retorno a HV02 ─────────────────────────────────────────
        var doorGO = Reg(new GameObject("DoorExit_Return"), "Door");
        doorGO.transform.position = new Vector3(-40f, FLOOR_Y + 3f, 0);
        var doorCol = doorGO.AddComponent<BoxCollider2D>();
        doorCol.isTrigger = true;
        doorCol.size      = new Vector2(2f, 5f);
        var door = doorGO.AddComponent<DoorExit>();
        var so = new SerializedObject(door);
        so.FindProperty("targetScene").stringValue = "HV02_PlazaCentral";
        so.FindProperty("spawnId").stringValue     = "door_HV05";
        so.FindProperty("labelText").stringValue   = "[ E ]  Plaza Central";
        so.ApplyModifiedProperties();

        // ── Player ───────────────────────────────────────────────────────────
        if (playerPrefab != null)
        {
            var playerGO = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            Undo.RegisterCreatedObjectUndo(playerGO, "Create Player");
            playerGO.transform.position = new Vector3(0, FLOOR_Y + 4f, 0);
        }
        else
        {
            Debug.LogWarning("[HV05Setup] Prefab de Player no encontrado — añadir manualmente.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[HV05Setup] ¡Escena HV05 lista! Guarda con Ctrl+S.\n" +
                  "Sala: 83.6u × 47.05u | Paredes x=±42 | Suelo y=-23.5 | Techo y=23.5");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static SpriteRenderer SkyLayer(GameObject parent, string name, Sprite sprite, int order, float alpha)
    {
        var go = Reg(new GameObject(name), name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localScale = new Vector3(SKY_SCALE, SKY_SCALE, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = order;
        sr.color        = new Color(1, 1, 1, alpha);

        var px = go.AddComponent<ParallaxBackground>();
        px.parallaxFactor  = 0.88f;
        px.parallaxFactorY = 1f;
        px.trackPlayer     = true;

        return sr;
    }

    static void MakeWall(string name, Vector3 pos, Vector2 size)
    {
        var go = Reg(new GameObject(name), name);
        go.transform.position = pos;
        go.layer = 8;
        go.AddComponent<BoxCollider2D>().size = size;
    }

    static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogWarning($"[HV05Setup] Asset no encontrado: {path}");
        return asset;
    }

    static GameObject Reg(GameObject go, string label)
    {
        Undo.RegisterCreatedObjectUndo(go, "Create " + label);
        return go;
    }
}
