#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Restaura la escena "HV01_Interior" como el Interior de la Casa de Kael.
// Menú: Eldoria → Restore Interior Kael
// EJECUTAR con la escena Game abierta y activa.
public static class InteriorKaelSetup
{
    const string BG_PATH   = "Assets/Sprites/Escenarios/Hub/InteriorCasaKael.png";
    const float  GROUND_Y  = -5f;     // centro del collider del suelo
    const float  DOOR_X    = 10.5f;   // puerta derecha (del log de GameSceneSetup)
    const float  WALL_HALF = 16f;     // mitad del ancho estimado de la sala
    const float  ROOM_H    = 18f;     // altura para las paredes laterales
    const float  PLAYER_X  = -11f;    // posición original del jugador
    const float  PLAYER_Y  = -4.3f;   // posición original del jugador

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Eldoria/Restore Interior Kael")]
    static void RestoreInterior()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "HV01_Interior")
        {
            Debug.LogWarning("[InteriorKaelSetup] Abre la escena 'Game' primero.");
            return;
        }

        RemoveExteriorObjects();   // quita lo que se metió por error
        RestoreCamera();           // FitRoom con fondo interior
        var bgSR = CreateBackground();
        CreateGroundAndWalls();
        CreateInteriorPlatforms();
        CreateDoorExit();
        RestorePlayer();
        AssignRoomBackground(bgSR);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[InteriorKaelSetup] ✓ Interior Casa Kael restaurado. Ajusta plataformas en Room Builder si es necesario.");
    }

    // ── Eliminar objetos del exterior metidos por error ───────────────────────
    static void RemoveExteriorObjects()
    {
        string[] toRemove = {
            "Backgrounds", "Structures", "Platforms",
            "Boundaries", "DayCycle", "BlackGround",
            "Background_Interior" // por si se ejecuta dos veces
        };
        foreach (var n in toRemove)
        {
            var go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ── Cámara: FitRoom centrada en el fondo interior ─────────────────────────
    static void RestoreCamera()
    {
        var camGO = GameObject.FindGameObjectWithTag("MainCamera");
        if (camGO == null) return;

        var cam = camGO.GetComponent<Camera>();
        if (cam != null)
        {
            cam.backgroundColor = Color.black;
            cam.clearFlags      = CameraClearFlags.SolidColor;
        }

        var cf = camGO.GetComponent<CameraFollow>();
        if (cf == null) cf = camGO.AddComponent<CameraFollow>();
        cf.mode      = CameraFollow.CameraMode.FollowBounded;
        cf.boundsMin = new Vector2(-7f, 0f);
        cf.boundsMax = new Vector2( 7f, 4f);
    }

    // ── Fondo interior ────────────────────────────────────────────────────────
    static SpriteRenderer CreateBackground()
    {
        var go = new GameObject("Background_Interior");
        go.transform.position = Vector3.zero;

        var sr         = go.AddComponent<SpriteRenderer>();
        sr.sprite      = AssetDatabase.LoadAssetAtPath<Sprite>(BG_PATH);
        sr.sortingOrder = -10;

        if (sr.sprite == null)
            Debug.LogWarning("[InteriorKaelSetup] No se encontró InteriorCasaKael.png en " + BG_PATH);

        return sr;
    }

    // ── Suelo y paredes ───────────────────────────────────────────────────────
    static void CreateGroundAndWalls()
    {
        int layer = GetGroundLayer();

        MakeStructure("Ground", new Vector3(0, GROUND_Y, 0),
                      new Vector2(WALL_HALF * 2 + 2f, 1f),
                      layer, RoomStructure.StructureType.Ground);

        MakeStructure("WallLeft", new Vector3(-WALL_HALF, 0f, 0f),
                      new Vector2(1f, ROOM_H),
                      layer, RoomStructure.StructureType.Wall);

        MakeStructure("WallRight", new Vector3(WALL_HALF, 0f, 0f),
                      new Vector2(1f, ROOM_H),
                      layer, RoomStructure.StructureType.Wall);
    }

    // ── Plataformas interiores ────────────────────────────────────────────────
    // Posiciones aproximadas — ajústalas en el editor con Room Builder
    static void CreateInteriorPlatforms()
    {
        int layer = GetGroundLayer();

        MakeStructure("Platform1", new Vector3(-6f, -1.5f, 0f),
                      new Vector2(6f, 0.5f), layer, RoomStructure.StructureType.Platform);

        MakeStructure("Platform2", new Vector3(4f, 0.5f, 0f),
                      new Vector2(5f, 0.5f), layer, RoomStructure.StructureType.Platform);

        MakeStructure("Platform3", new Vector3(-2f, 2.5f, 0f),
                      new Vector2(7f, 0.5f), layer, RoomStructure.StructureType.Platform);
    }

    // ── Puerta de salida (derecha) ────────────────────────────────────────────
    static void CreateDoorExit()
    {
        var go = new GameObject("DoorExit_Right");
        go.transform.position = new Vector3(DOOR_X, GROUND_Y + 1.5f, 0f);

        var bc       = go.AddComponent<BoxCollider2D>();
        bc.size      = new Vector2(2f, 4f);
        bc.isTrigger = true;

        var de = go.AddComponent<DoorExit>();
        var so = new SerializedObject(de);
        so.FindProperty("targetScene").stringValue = "HV01_Exterior";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Jugador: posición original ────────────────────────────────────────────
    static void RestorePlayer()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) return;

        playerGO.transform.position = new Vector3(PLAYER_X, PLAYER_Y, 0f);

        var sr = playerGO.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 0;
    }

    // ── Enlazar CameraFollow.roomBackground ──────────────────────────────────
    static void AssignRoomBackground(SpriteRenderer bgSR)
    {
        var camGO = GameObject.FindGameObjectWithTag("MainCamera");
        if (camGO == null || bgSR == null) return;

        var cf = camGO.GetComponent<CameraFollow>();
        if (cf == null) return;

        var so = new SerializedObject(cf);
        so.FindProperty("roomBackground").objectReferenceValue = bgSR;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static int GetGroundLayer()
    {
        int layer = LayerMask.NameToLayer("Ground");
        return layer < 0 ? 8 : layer;
    }

    static void MakeStructure(string goName, Vector3 pos, Vector2 size,
                               int layer, RoomStructure.StructureType type)
    {
        var go = new GameObject(goName);
        go.transform.position = pos;
        go.layer              = layer;

        var bc   = go.AddComponent<BoxCollider2D>();
        bc.size  = size;

        var rs   = go.AddComponent<RoomStructure>();
        rs.type  = type;
    }
}
#endif
