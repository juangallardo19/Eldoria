using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Patrón: Command — la acción de menú encapsula la creación completa de las rampas como
// una operación atómica registrada en el Undo stack.
// Patrón: Factory Method — CreateRamp() construye y configura cada rampa de forma reutilizable.
public static class AddStaircases
{
    private const string SPRITE_PATH    = "Assets/Sprites/Escenarios/Plataformas/hub/ladrillos.png";
    private const float  RAMP_LENGTH    = 23f;
    private const float  RAMP_THICKNESS = 0.7f;
    private const int    GROUND_LAYER   = 8; // Layer "Ground" definido en Project Settings

    [MenuItem("Eldoria/Add Staircases")]
    private static void AddRamps()
    {
        Undo.SetCurrentGroupName("Añadir Rampas");
        int undoGroup = Undo.GetCurrentGroup();

        // Asegurar que el sprite usa Full Rect (requerido por SpriteDrawMode.Tiled)
        EnsureFullRectMeshType(SPRITE_PATH);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        if (sprite == null)
        {
            Debug.LogError($"[AddStaircases] Sprite no encontrado: {SPRITE_PATH}");
            return;
        }

        // Objeto padre agrupa las dos rampas para moverlas juntas desde Scene View
        var parent = new GameObject("Staircases");
        Undo.RegisterCreatedObjectUndo(parent, "Crear Staircases");

        // Ramp_LeftToRight: +45° — sube de izquierda a derecha
        CreateRamp("Ramp_LeftToRight", parent.transform, +45f, new Vector3(-12f, 0f, 0f), sprite);
        // Ramp_RightToLeft: -45° — sube de derecha a izquierda
        CreateRamp("Ramp_RightToLeft", parent.transform, -45f, new Vector3(+12f, 0f, 0f), sprite);

        Selection.activeGameObject = parent;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("[AddStaircases] Staircases creado. Selecciona cada rampa en Scene View para reposicionarla.");
    }

    // Factory Method: construye y configura una rampa completa (collider + sprite + estructura).
    private static void CreateRamp(string rampName, Transform parent, float zRotation,
                                   Vector3 localPos, Sprite sprite)
    {
        var go = new GameObject(rampName);
        Undo.RegisterCreatedObjectUndo(go, $"Crear {rampName}");

        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        go.layer = GROUND_LAYER;

        // BoxCollider2D — OneWayRamp controla si colisiona o no en cada FixedUpdate
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(RAMP_LENGTH, RAMP_THICKNESS);

        // OneWayRamp: colisión unidireccional — aterriza desde arriba, atraviesa desde abajo
        go.AddComponent<OneWayRamp>();

        // RoomStructure: categoría Ground para gizmos de sala (sin PlatformEffector2D)
        var rs = go.AddComponent<RoomStructure>();
        rs.type = RoomStructure.StructureType.Ground;

        // Visual Tiled: el sprite de ladrillos cubre la superficie completa de la rampa
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.drawMode     = SpriteDrawMode.Tiled;
        sr.tileMode     = SpriteTileMode.Continuous;
        sr.size         = new Vector2(RAMP_LENGTH, RAMP_THICKNESS);
        sr.sortingOrder = -1;
    }

    // Garantiza SpriteMeshType.FullRect en el importer para que Tiled funcione sin warnings.
    // Usa SerializedObject porque la propiedad directa no está expuesta en Unity 2022.3 LTS.
    // m_SpriteMeshType: 0 = FullRect, 1 = Tight
    private static void EnsureFullRectMeshType(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        var so = new SerializedObject(ti);
        var prop = so.FindProperty("m_SpriteMeshType");
        if (prop == null || prop.intValue == 0) return;

        prop.intValue = 0;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}
