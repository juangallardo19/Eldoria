#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Crea plataformas elevadas en HV01_Exterior (escena escalada 3x).
// Suelo a y=-22.5 en world space; plataformas en world space directamente.
// Menú: Eldoria → Add Exterior Platforms
public static class ExteriorPlatformSetup
{
    [MenuItem("Eldoria/Add Exterior Platforms")]
    static void AddPlatforms()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "HV01_Exterior")
        {
            Debug.LogWarning("[ExteriorPlatformSetup] Abre HV01_Exterior primero.");
            return;
        }

        // Eliminar grupo previo si se ejecuta dos veces
        var existing = GameObject.Find("ElevatedPlatforms");
        if (existing != null) Object.DestroyImmediate(existing);

        var group = new GameObject("ElevatedPlatforms");

        int layer = LayerMask.NameToLayer("Ground");
        if (layer < 0) layer = 8;

        // Ground world y ≈ -22.5, escena 3x
        // Plataformas en world space — separación escalonada para buena jugabilidad
        var data = new (string name, Vector3 pos, float width)[]
        {
            ("Plat_A_LowLeft",    new Vector3(-38f, -17f, 0f), 18f),
            ("Plat_B_LowRight",   new Vector3( 18f, -17f, 0f), 16f),
            ("Plat_C_MidLeft",    new Vector3(-15f, -13f, 0f), 14f),
            ("Plat_D_MidRight",   new Vector3( 38f, -13f, 0f), 12f),
            ("Plat_E_HighCenter", new Vector3(  0f,  -9f, 0f), 20f),
            ("Plat_F_HighLeft",   new Vector3(-50f,  -9f, 0f), 10f),
            ("Plat_G_HighRight",  new Vector3( 50f,  -9f, 0f), 10f),
        };

        foreach (var (name, pos, width) in data)
        {
            var go = new GameObject(name);
            go.transform.SetParent(group.transform);
            go.transform.position = pos;
            go.layer = layer;

            var bc  = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(width, 0.5f);

            var rs  = go.AddComponent<RoomStructure>();
            rs.type = RoomStructure.StructureType.Platform;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ExteriorPlatformSetup] ✓ 7 plataformas creadas en HV01_Exterior.");
    }
}
#endif
